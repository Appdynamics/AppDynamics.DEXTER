using AppDynamics.Dexter.ReportObjectMaps;
using AppDynamics.Dexter.ReportObjects;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class IndexBIQEntities : JobStepIndexBase
    {
        public override bool Execute(ProgramOptions programOptions, JobConfiguration jobConfiguration)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            StepTiming stepTimingFunction = new StepTiming();
            stepTimingFunction.JobFileName = programOptions.OutputJobFilePath;
            stepTimingFunction.StepName = jobConfiguration.Status.ToString();
            stepTimingFunction.StepID = (int)jobConfiguration.Status;
            stepTimingFunction.StartTime = DateTime.Now;
            stepTimingFunction.NumEntities = jobConfiguration.Target.Count;

            this.DisplayJobStepStartingStatus(jobConfiguration);

            FilePathMap = new FilePathMap(programOptions, jobConfiguration);

            try
            {
                if (this.ShouldExecute(programOptions, jobConfiguration) == false)
                {
                    return true;
                }

                if (jobConfiguration.Target.Count(t => t.Type == APPLICATION_TYPE_BIQ) == 0)
                {
                    logger.Warn("No {0} targets to process", APPLICATION_TYPE_BIQ);
                    loggerConsole.Warn("No {0} targets to process", APPLICATION_TYPE_BIQ);

                    return true;
                }

                bool reportFolderCleaned = false;

                // Process each target
                for (int i = 0; i < jobConfiguration.Target.Count; i++)
                {
                    Stopwatch stopWatchTarget = new Stopwatch();
                    stopWatchTarget.Start();

                    JobTarget jobTarget = jobConfiguration.Target[i];

                    if (jobTarget.Type != null && jobTarget.Type.Length > 0 && jobTarget.Type != APPLICATION_TYPE_BIQ) continue;

                    StepTiming stepTimingTarget = new StepTiming();
                    stepTimingTarget.Controller = jobTarget.Controller;
                    stepTimingTarget.ApplicationName = jobTarget.Application;
                    stepTimingTarget.ApplicationID = jobTarget.ApplicationID;
                    stepTimingTarget.JobFileName = programOptions.OutputJobFilePath;
                    stepTimingTarget.StepName = jobConfiguration.Status.ToString();
                    stepTimingTarget.StepID = (int)jobConfiguration.Status;
                    stepTimingTarget.StartTime = DateTime.Now;

                    try
                    {
                        this.DisplayJobTargetStartingStatus(jobConfiguration, jobTarget, i + 1);

                        #region Saved Searches

                        loggerConsole.Info("Saved Searches and Their Widgets");

                        List<BIQSearch> biqSearchesList = null;
                        List<BIQWidget> biqWidgetsList = null;

                        JArray savedSearchesArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.BIQSearchesDataFilePath(jobTarget));
                        if (savedSearchesArray != null)
                        {
                            biqSearchesList = new List<BIQSearch>(savedSearchesArray.Count);
                            biqWidgetsList = new List<BIQWidget>(savedSearchesArray.Count * 8);

                            foreach (JObject savedSearchObject in savedSearchesArray)
                            {
                                BIQSearch search = new BIQSearch();
                                search.Controller = jobTarget.Controller;
                                search.ControllerLink = String.Format(DEEPLINK_CONTROLLER, jobTarget.Controller, DEEPLINK_TIMERANGE_LAST_15_MINUTES);
                                search.ApplicationName = jobTarget.Application;
                                search.ApplicationID = jobTarget.ApplicationID;
                                search.ApplicationLink = String.Format(DEEPLINK_BIQ_APPLICATION, search.Controller, DEEPLINK_TIMERANGE_LAST_15_MINUTES);

                                search.SearchName = getStringValueFromJToken(savedSearchObject, "searchName");
                                search.InternalName = getStringValueFromJToken(savedSearchObject, "name");
                                search.Description = getStringValueFromJToken(savedSearchObject, "searchDescription");
                                search.SearchType = getStringValueFromJToken(savedSearchObject, "searchType");
                                search.SearchMode = getStringValueFromJToken(savedSearchObject, "searchMode");
                                search.ViewMode = getStringValueFromJToken(savedSearchObject, "viewMode");
                                search.Visualization = getStringValueFromJToken(savedSearchObject, "visualization");
                                search.SearchID = getLongValueFromJToken(savedSearchObject, "id");
                                search.SearchLink = String.Format(DEEPLINK_BIQ_SEARCH, search.Controller, search.SearchID, DEEPLINK_TIMERANGE_LAST_15_MINUTES);

                                search.CreatedBy = getStringValueFromJToken(savedSearchObject, "createdBy");
                                search.CreatedOnUtc = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(savedSearchObject, "createdOn"));
                                try { search.CreatedOn = search.CreatedOnUtc.ToLocalTime(); } catch { }
                                search.UpdatedBy = getStringValueFromJToken(savedSearchObject, "modifiedBy");
                                search.UpdatedOnUtc = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(savedSearchObject, "modifiedOn"));
                                try { search.UpdatedOn = search.UpdatedOnUtc.ToLocalTime(); } catch { }

                                if (isTokenPropertyNull(savedSearchObject, "adqlQueries") == false)
                                {
                                    try
                                    {
                                        if (savedSearchObject["adqlQueries"].Count() == 0)
                                        {
                                            search.Query = String.Empty;
                                        }
                                        else if (savedSearchObject["adqlQueries"].Count() == 1)
                                        {
                                            search.Query = savedSearchObject["adqlQueries"][0].ToString();
                                        }
                                        else
                                        {
                                            search.Query = getStringValueOfObjectFromJToken(savedSearchObject, "adqlQueries", false);
                                        }
                                    }
                                    catch { }
                                }
                                if (search.Query.Length > 0)
                                {
                                    Regex regexVersion = new Regex(@"(?i).*FROM\s(\S*)\s?.*", RegexOptions.IgnoreCase);
                                    Match match = regexVersion.Match(search.Query);
                                    if (match != null)
                                    {
                                        if (match.Groups.Count > 1)
                                        {
                                            search.DataSource = match.Groups[1].Value;
                                        }
                                    }
                                }

                                if (isTokenPropertyNull(savedSearchObject, "widgets") == false)
                                {
                                    foreach (JObject searchWidget in savedSearchObject["widgets"])
                                    {
                                        BIQWidget widget = new BIQWidget();
                                        widget.Controller = search.Controller;
                                        widget.ControllerLink = search.ControllerLink;
                                        widget.ApplicationName = search.ApplicationName;
                                        widget.ApplicationID = search.ApplicationID;
                                        widget.ApplicationLink = search.ApplicationLink;

                                        widget.SearchName = search.SearchName;
                                        widget.SearchType = search.SearchType;
                                        widget.SearchMode = search.SearchMode;
                                        widget.SearchID = search.SearchID;
                                        widget.SearchLink = search.SearchLink;

                                        widget.InternalName = getStringValueFromJToken(searchWidget, "name");
                                        widget.WidgetID = getLongValueFromJToken(searchWidget, "id");

                                        if (isTokenPropertyNull(searchWidget, "adqlQueries") == false)
                                        {
                                            try
                                            {
                                                if (searchWidget["adqlQueries"].Count() == 0)
                                                {
                                                    widget.Query = String.Empty;
                                                }
                                                else if (searchWidget["adqlQueries"].Count() == 1)
                                                {
                                                    widget.Query = searchWidget["adqlQueries"][0].ToString();
                                                }
                                                else
                                                {
                                                    widget.Query = getStringValueOfObjectFromJToken(searchWidget, "adqlQueries", false);
                                                }
                                            }
                                            catch { }
                                        }
                                        if (widget.Query.Length > 0)
                                        {
                                            Regex regexVersion = new Regex(@"(?i).*FROM\s(\S*)\s?.*", RegexOptions.IgnoreCase);
                                            Match match = regexVersion.Match(widget.Query);
                                            if (match != null)
                                            {
                                                if (match.Groups.Count > 1)
                                                {
                                                    widget.DataSource = match.Groups[1].Value;
                                                }
                                            }
                                        }

                                        if (isTokenPropertyNull(searchWidget, "properties") == false)
                                        {
                                            JObject searchWidgetPropertiesObject = (JObject)searchWidget["properties"];

                                            widget.WidgetName = getStringValueFromJToken(searchWidgetPropertiesObject, "title");
                                            widget.LegendLayout = getStringValueFromJToken(searchWidgetPropertiesObject, "legendsLayout");
                                            widget.WidgetType = getStringValueFromJToken(searchWidgetPropertiesObject, "type");
                                            widget.Resolution = getStringValueFromJToken(searchWidgetPropertiesObject, "resolution");

                                            widget.Width = getIntValueFromJToken(searchWidgetPropertiesObject, "sizeX");
                                            widget.Height = getIntValueFromJToken(searchWidgetPropertiesObject, "sizeY");
                                            widget.MinWidth = getIntValueFromJToken(searchWidgetPropertiesObject, "minSizeX");
                                            widget.MinHeight = getIntValueFromJToken(searchWidgetPropertiesObject, "minSizeY");
                                            widget.Column = getIntValueFromJToken(searchWidgetPropertiesObject, "col");
                                            widget.Row = getIntValueFromJToken(searchWidgetPropertiesObject, "row");

                                            widget.IsStacking = getBoolValueFromJToken(searchWidgetPropertiesObject, "isStackingEnabled");
                                            widget.IsDrilledDown = getBoolValueFromJToken(searchWidgetPropertiesObject, "isDrilledDown");

                                            widget.FontSize = getIntValueFromJToken(searchWidgetPropertiesObject, "fontSize");

                                            widget.Color = getIntValueFromJToken(searchWidgetPropertiesObject, "color").ToString("X6");
                                            widget.BackgroundColor = getIntValueFromJToken(searchWidgetPropertiesObject, "backgroundColor").ToString("X6");
                                        }

                                        if (isTokenPropertyNull(searchWidget, "timeRangeSpecifier") == false)
                                        {
                                            JObject searchWidgetTimeRangeObject = (JObject)searchWidget["timeRangeSpecifier"];

                                            widget.TimeRangeType = getStringValueFromJToken(searchWidgetTimeRangeObject, "type");
                                            widget.TimeRangeDuration = getIntValueFromJToken(searchWidgetTimeRangeObject, "durationInMinutes");

                                            if (isTokenPropertyNull(searchWidgetTimeRangeObject, "timeRange") == false)
                                            {
                                                widget.StartTimeUtc = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(searchWidgetTimeRangeObject["timeRange"], "startTime"));
                                                try { widget.StartTime = widget.StartTimeUtc.ToLocalTime(); } catch { }
                                                widget.EndTimeUtc = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(searchWidgetTimeRangeObject["timeRange"], "endTime"));
                                                try { widget.EndTime = widget.EndTimeUtc.ToLocalTime(); } catch { }
                                            }
                                        }

                                        search.NumWidgets++;

                                        biqWidgetsList.Add(widget);
                                    }
                                }

                                biqSearchesList.Add(search);
                            }

                            // Sort them
                            biqSearchesList = biqSearchesList.OrderBy(o => o.SearchName).ToList();
                            FileIOHelper.WriteListToCSVFile(biqSearchesList, new BIQSearchReportMap(), FilePathMap.BIQSearchesIndexFilePath(jobTarget));

                            biqWidgetsList = biqWidgetsList.OrderBy(o => o.SearchName).ThenBy(o => o.WidgetName).ToList();
                            FileIOHelper.WriteListToCSVFile(biqWidgetsList, new BIQWidgetReportMap(), FilePathMap.BIQWidgetsIndexFilePath(jobTarget));

                            stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + biqSearchesList.Count;
                        }

                        #endregion

                        #region Saved Metrics

                        loggerConsole.Info("Saved Metrics");

                        List<BIQMetric> biqMetricsList = null;

                        JArray savedMetricsArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.BIQMetricsDataFilePath(jobTarget));
                        if (savedMetricsArray != null)
                        {
                            biqMetricsList = new List<BIQMetric>(savedMetricsArray.Count);

                            foreach (JObject savedMetricObject in savedMetricsArray)
                            {
                                BIQMetric metric = new BIQMetric();
                                metric.Controller = jobTarget.Controller;
                                metric.ControllerLink = String.Format(DEEPLINK_CONTROLLER, jobTarget.Controller, DEEPLINK_TIMERANGE_LAST_15_MINUTES);
                                metric.ApplicationName = jobTarget.Application;
                                metric.ApplicationID = jobTarget.ApplicationID;
                                metric.ApplicationLink = String.Format(DEEPLINK_BIQ_APPLICATION, metric.Controller, DEEPLINK_TIMERANGE_LAST_15_MINUTES);

                                metric.MetricName = getStringValueFromJToken(savedMetricObject, "queryName");
                                metric.MetricDescription = getStringValueFromJToken(savedMetricObject, "queryDescription");

                                metric.Query = getStringValueFromJToken(savedMetricObject, "adqlQueryString");
                                if (metric.Query.Length > 0)
                                {
                                    Regex regexVersion = new Regex(@"(?i).*FROM\s(\S*)\s?.*", RegexOptions.IgnoreCase);
                                    Match match = regexVersion.Match(metric.Query);
                                    if (match != null)
                                    {
                                        if (match.Groups.Count > 1)
                                        {
                                            metric.DataSource = match.Groups[1].Value;
                                        }
                                    }
                                }
                                metric.EventType = getStringValueFromJToken(savedMetricObject, "eventType");

                                metric.IsEnabled = getBoolValueFromJToken(savedMetricObject, "queryExecutionEnabled");

                                metric.LastExecStatus = getStringValueFromJToken(savedMetricObject, "recentExecutionStatus");
                                metric.LastExecDuration = getIntValueFromJToken(savedMetricObject, "recentQueryExecutionDuration");
                                metric.SuccessCount = getIntValueFromJToken(savedMetricObject, "totalSuccessCount");
                                metric.FailureCount = getIntValueFromJToken(savedMetricObject, "totalFailuresCount");

                                metric.CreatedBy = getStringValueFromJToken(savedMetricObject, "createdBy");
                                metric.CreatedOnUtc = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(savedMetricObject, "queryCreationTime"));
                                try { metric.CreatedOn = metric.CreatedOnUtc.ToLocalTime(); } catch { }

                                if (isTokenPropertyNull(savedMetricObject, "metricIds") == false)
                                {
                                    metric.MetricsIDs = new List<long>(10);
                                    foreach (JValue metricIDValue in savedMetricObject["metricIds"])
                                    {
                                        long metricID = (long)metricIDValue;
                                        metric.MetricsIDs.Add(metricID);
                                    }
                                }

                                // Create metric link
                                if (metric.MetricsIDs != null && metric.MetricsIDs.Count > 0)
                                {
                                    StringBuilder sb = new StringBuilder(256);
                                    foreach (long metricID in metric.MetricsIDs)
                                    {
                                        if (metricID > 0)
                                        {
                                            sb.Append(String.Format(DEEPLINK_METRIC_APPLICATION_TARGET_METRIC_ID, metric.ApplicationID, metricID));
                                            sb.Append(",");
                                        }
                                    }
                                    sb.Remove(sb.Length - 1, 1);
                                    metric.MetricLink = String.Format(DEEPLINK_METRIC, metric.Controller, metric.ApplicationID, sb.ToString(), DEEPLINK_TIMERANGE_LAST_15_MINUTES);
                                }

                                metric.MetricID = String.Join(",", metric.MetricsIDs.ToArray());

                                biqMetricsList.Add(metric);
                            }

                            // Sort them
                            biqMetricsList = biqMetricsList.OrderBy(o => o.MetricName).ToList();
                            FileIOHelper.WriteListToCSVFile(biqMetricsList, new BIQMetricReportMap(), FilePathMap.BIQMetricsIndexFilePath(jobTarget));

                            stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + biqMetricsList.Count;
                        }

                        #endregion

                        #region Business Journeys

                        loggerConsole.Info("Business Journeys");

                        List<BIQBusinessJourney> businessJourneysList = null;

                        JArray businessJourneysArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.BIQBusinessJourneysDataFilePath(jobTarget));

                        if (businessJourneysArray != null)
                        {
                            businessJourneysList = new List<BIQBusinessJourney>(businessJourneysArray.Count);

                            foreach (JObject businessJourneyObject in businessJourneysArray)
                            {
                                BIQBusinessJourney businessJourney = new BIQBusinessJourney();
                                businessJourney.Controller = jobTarget.Controller;
                                businessJourney.ControllerLink = String.Format(DEEPLINK_CONTROLLER, jobTarget.Controller, DEEPLINK_TIMERANGE_LAST_15_MINUTES);
                                businessJourney.ApplicationName = jobTarget.Application;
                                businessJourney.ApplicationID = jobTarget.ApplicationID;
                                businessJourney.ApplicationLink = String.Format(DEEPLINK_BIQ_APPLICATION, businessJourney.Controller, DEEPLINK_TIMERANGE_LAST_15_MINUTES);

                                businessJourney.JourneyName = getStringValueFromJToken(businessJourneyObject, "name");
                                businessJourney.JourneyDescription = getStringValueFromJToken(businessJourneyObject, "description");
                                businessJourney.JourneyID = getStringValueFromJToken(businessJourneyObject, "id");

                                businessJourney.State = getStringValueFromJToken(businessJourneyObject, "state");
                                businessJourney.KeyField = getStringValueFromJToken(businessJourneyObject, "keyFieldName");

                                businessJourney.IsEnabled = getBoolValueFromJToken(businessJourneyObject, "enabled");

                                businessJourney.CreatedBy = getStringValueFromJToken(businessJourneyObject, "createdBy");
                                businessJourney.CreatedOnUtc = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(businessJourneyObject, "createdAt"));
                                try { businessJourney.CreatedOn = businessJourney.CreatedOnUtc.ToLocalTime(); } catch { }
                                businessJourney.UpdatedBy = getStringValueFromJToken(businessJourneyObject, "lastModifiedBy");
                                businessJourney.UpdatedOnUtc = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(businessJourneyObject, "lastModifiedAt"));
                                try { businessJourney.UpdatedOn = businessJourney.UpdatedOnUtc.ToLocalTime(); } catch { }

                                if (isTokenPropertyNull(businessJourneyObject, "aggregateGraph") == false &&
                                    isTokenPropertyNull(businessJourneyObject["aggregateGraph"], "root") == false)
                                {
                                    StringBuilder sb = new StringBuilder(32 * 10);
                                    getStagesRecursive((JObject)businessJourneyObject["aggregateGraph"]["root"], sb);
                                    businessJourney.Stages = sb.ToString();
                                    businessJourney.NumStages = businessJourney.Stages.Split('>').Length;
                                }

                                businessJourneysList.Add(businessJourney);
                            }

                            // Sort them
                            businessJourneysList = businessJourneysList.OrderBy(o => o.JourneyName).ToList();
                            FileIOHelper.WriteListToCSVFile(businessJourneysList, new BIQBusinessJourneyReportMap(), FilePathMap.BIQBusinessJourneysIndexFilePath(jobTarget));

                            stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + businessJourneysList.Count;
                        }

                        #endregion

                        #region Experience Levels

                        loggerConsole.Info("Experience Levels");

                        List<BIQExperienceLevel> experienceLevelsList = null;

                        JObject experienceLevelsContainerObject = FileIOHelper.LoadJObjectFromFile(FilePathMap.BIQExperienceLevelsDataFilePath(jobTarget));

                        if (experienceLevelsContainerObject != null)
                        {
                            if (isTokenPropertyNull(experienceLevelsContainerObject, "items") == false)
                            {
                                JArray experienceLevelsArray = (JArray)experienceLevelsContainerObject["items"];

                                experienceLevelsList = new List<BIQExperienceLevel>(experienceLevelsArray.Count);

                                foreach (JObject experienceLevelObject in experienceLevelsArray)
                                {
                                    BIQExperienceLevel experienceLevel = new BIQExperienceLevel();
                                    experienceLevel.Controller = jobTarget.Controller;
                                    experienceLevel.ControllerLink = String.Format(DEEPLINK_CONTROLLER, jobTarget.Controller, DEEPLINK_TIMERANGE_LAST_15_MINUTES);
                                    experienceLevel.ApplicationName = jobTarget.Application;
                                    experienceLevel.ApplicationID = jobTarget.ApplicationID;
                                    experienceLevel.ApplicationLink = String.Format(DEEPLINK_BIQ_APPLICATION, experienceLevel.Controller, DEEPLINK_TIMERANGE_LAST_15_MINUTES);

                                    experienceLevel.ExperienceLevelName = getStringValueFromJToken(experienceLevelObject, "configurationName");
                                    experienceLevel.ExperienceLevelID = getStringValueFromJToken(experienceLevelObject, "id");

                                    experienceLevel.DataSource = getStringValueFromJToken(experienceLevelObject, "eventType");
                                    experienceLevel.EventField = getStringValueFromJToken(experienceLevelObject, "eventField");
                                    experienceLevel.Criteria = getStringValueFromJToken(experienceLevelObject, "criteria");
                                    experienceLevel.ThresholdOperator = getStringValueFromJToken(experienceLevelObject, "thresholdOperator");
                                    experienceLevel.ThresholdValue = getStringValueFromJToken(experienceLevelObject, "thresholdValue");

                                    experienceLevel.Period = getStringValueFromJToken(experienceLevelObject, "compliancePeriod");
                                    experienceLevel.Timezone = getStringValueFromJToken(experienceLevelObject, "timeZone");

                                    experienceLevel.IsActive = getBoolValueFromJToken(experienceLevelObject, "active");
                                    experienceLevel.IsIncludeErrors = getBoolValueFromJToken(experienceLevelObject, "includeErrors");

                                    experienceLevel.NormalThreshold = getIntValueFromJToken(experienceLevelObject, "normalThresholdPercent");
                                    experienceLevel.WarningThreshold = getIntValueFromJToken(experienceLevelObject, "warningThresholdPercent");
                                    //experienceLevel.CriticalThreshold = getIntValueFromJToken(experienceLevelObject, "this needs to be calculated somehow");

                                    experienceLevel.StartOnUtc = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(experienceLevelObject, "startDate"));
                                    try { experienceLevel.StartOn = experienceLevel.StartOnUtc.ToLocalTime(); } catch { }

                                    if (isTokenPropertyNull(experienceLevelObject, "metadata") == false)
                                    {
                                        JObject experienceLevelMetadataObject = (JObject)experienceLevelObject["metadata"];

                                        experienceLevel.CreatedBy = getStringValueFromJToken(experienceLevelMetadataObject, "createdByResolvedName");
                                        experienceLevel.CreatedOnUtc = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(experienceLevelMetadataObject, "creationTime"));
                                        try { experienceLevel.CreatedOn = experienceLevel.CreatedOnUtc.ToLocalTime(); } catch { }
                                        experienceLevel.UpdatedBy = getStringValueFromJToken(experienceLevelMetadataObject, "lastModifiedByResolvedName");
                                        experienceLevel.UpdatedOnUtc = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(experienceLevelMetadataObject, "lastModifiedTime"));
                                        try { experienceLevel.UpdatedOn = experienceLevel.UpdatedOnUtc.ToLocalTime(); } catch { }
                                    }

                                    if (isTokenPropertyNull(experienceLevelObject, "exclusionPeriodList") == false)
                                    {
                                        JArray exclusionPeriodsArray = (JArray)experienceLevelObject["exclusionPeriodList"];
                                        experienceLevel.NumExclusionPeriods = exclusionPeriodsArray.Count;

                                        experienceLevel.ExclusionPeriodsRaw = getStringValueOfObjectFromJToken(experienceLevelObject, "exclusionPeriodList", false);
                                    }

                                    experienceLevelsList.Add(experienceLevel);
                                }

                                // Sort them
                                experienceLevelsList = experienceLevelsList.OrderBy(o => o.ExperienceLevelName).ToList();
                                FileIOHelper.WriteListToCSVFile(experienceLevelsList, new BIQExperienceLevelReportMap(), FilePathMap.BIQExperienceLevelsIndexFilePath(jobTarget));

                                stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + experienceLevelsList.Count;
                            }
                        }

                        #endregion

                        #region Schema Fields

                        List<BIQSchema> schemasList = new List<BIQSchema>(16);
                        List<BIQField> schemaFieldsList = new List<BIQField>(16 * 32);

                        List<string> analyticsSchemas = new List<string>(BIQ_SCHEMA_TYPES.Count + 10);
                        analyticsSchemas.AddRange(BIQ_SCHEMA_TYPES);

                        // Add custom schemas if any
                        JObject customSchemasContainer = FileIOHelper.LoadJObjectFromFile(FilePathMap.BIQCustomSchemasDataFilePath(jobTarget));
                        if (customSchemasContainer != null)
                        {
                            JArray customSchemas = JArray.Parse(getStringValueFromJToken(customSchemasContainer, "rawResponse"));
                            foreach (JToken customSchemaToken in customSchemas)
                            {
                                string schemaName = customSchemaToken.ToString();

                                analyticsSchemas.Add(schemaName);
                            }
                        }

                        // First get known schemas
                        foreach (string schemaName in analyticsSchemas)
                        {
                            loggerConsole.Info("Fields for Schema {0}", schemaName);

                            JArray schemaFieldsArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.BIQSchemaFieldsDataFilePath(jobTarget, schemaName));
                            if (schemaFieldsArray != null)
                            {
                                List<BIQField> schemaFieldsInThisSchemaList = new List<BIQField>(schemaFieldsArray.Count);

                                foreach (JObject schemaFieldObject in schemaFieldsArray)
                                {
                                    BIQField field = new BIQField();

                                    field.Controller = jobTarget.Controller;
                                    field.ControllerLink = String.Format(DEEPLINK_CONTROLLER, jobTarget.Controller, DEEPLINK_TIMERANGE_LAST_15_MINUTES);
                                    field.ApplicationName = jobTarget.Application;
                                    field.ApplicationID = jobTarget.ApplicationID;
                                    field.ApplicationLink = String.Format(DEEPLINK_BIQ_APPLICATION, field.Controller, DEEPLINK_TIMERANGE_LAST_15_MINUTES);

                                    field.SchemaName = schemaName;

                                    field.FieldName = getStringValueFromJToken(schemaFieldObject, "fieldName");
                                    field.FieldType = getStringValueFromJToken(schemaFieldObject, "fieldType");
                                    field.Category = getStringValueFromJToken(schemaFieldObject, "category");

                                    if (isTokenPropertyNull(schemaFieldObject, "parents") == false)
                                    {
                                        string[] parents = schemaFieldObject["parents"].Select(a => a["fieldName"].ToString()).ToArray();

                                        field.Parents = String.Join(";", parents);
                                        field.NumParents = parents.Length;
                                    }

                                    field.IsSortable = getBoolValueFromJToken(schemaFieldObject, "sortingAllowed");
                                    field.IsAggregatable = getBoolValueFromJToken(schemaFieldObject, "aggregationsAllowed");
                                    field.IsHidden = getBoolValueFromJToken(schemaFieldObject, "hidden");
                                    field.IsDeleted = getBoolValueFromJToken(schemaFieldObject, "softDeleted");

                                    schemaFieldsInThisSchemaList.Add(field);
                                }


                                // Now the schema
                                BIQSchema schema = new BIQSchema();

                                schema.Controller = jobTarget.Controller;
                                schema.ControllerLink = String.Format(DEEPLINK_CONTROLLER, jobTarget.Controller, DEEPLINK_TIMERANGE_LAST_15_MINUTES);
                                schema.ApplicationName = jobTarget.Application;
                                schema.ApplicationID = jobTarget.ApplicationID;
                                schema.ApplicationLink = String.Format(DEEPLINK_BIQ_APPLICATION, schema.Controller, DEEPLINK_TIMERANGE_LAST_15_MINUTES);

                                schema.SchemaName = schemaName;

                                schema.IsCustom = (BIQ_SCHEMA_TYPES.Contains(schemaName) == false);

                                schema.NumFields = schemaFieldsInThisSchemaList.Count;
                                schema.NumStringFields = schemaFieldsInThisSchemaList.Count(s => s.FieldType == "STRING");
                                schema.NumIntegerFields = schemaFieldsInThisSchemaList.Count(s => s.FieldType == "INTEGER");
                                schema.NumLongFields = schemaFieldsInThisSchemaList.Count(s => s.FieldType == "LONG");
                                schema.NumFloatFields = schemaFieldsInThisSchemaList.Count(s => s.FieldType == "FLOAT");
                                schema.NumDoubleFields = schemaFieldsInThisSchemaList.Count(s => s.FieldType == "DOUBLE");
                                schema.NumBooleanFields = schemaFieldsInThisSchemaList.Count(s => s.FieldType == "BOOLEAN");
                                schema.NumDateFields = schemaFieldsInThisSchemaList.Count(s => s.FieldType == "DATE");
                                schema.NumObjectFields = schemaFieldsInThisSchemaList.Count(s => s.FieldType == "OBJECT");

                                schemaFieldsList.AddRange(schemaFieldsInThisSchemaList);
                                schemasList.Add(schema);
                            }
                        }


                        // Sort them
                        schemasList = schemasList.OrderBy(o => o.SchemaName).ToList();
                        FileIOHelper.WriteListToCSVFile(schemasList, new BIQSchemaReportMap(), FilePathMap.BIQSchemasIndexFilePath(jobTarget));

                        schemaFieldsList = schemaFieldsList.OrderBy(o => o.SchemaName).ThenBy(o => o.FieldName).ToList();
                        FileIOHelper.WriteListToCSVFile(schemaFieldsList, new BIQFieldReportMap(), FilePathMap.BIQSchemaFieldsIndexFilePath(jobTarget));

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + schemaFieldsList.Count;

                        #endregion

                        #region Application

                        loggerConsole.Info("Index Application");

                        BIQApplication application = new BIQApplication();

                        application.Controller = jobTarget.Controller;
                        application.ControllerLink = String.Format(DEEPLINK_CONTROLLER, jobTarget.Controller, DEEPLINK_TIMERANGE_LAST_15_MINUTES);
                        application.ApplicationName = jobTarget.Application;
                        application.ApplicationID = jobTarget.ApplicationID;
                        application.ApplicationLink = String.Format(DEEPLINK_BIQ_APPLICATION, application.Controller, DEEPLINK_TIMERANGE_LAST_15_MINUTES);

                        if (biqSearchesList != null)
                        {
                            application.NumSearches = biqSearchesList.Count;
                            application.NumSingleSearches = biqSearchesList.Count(p => p.SearchType == "SINGLE");
                            application.NumMultiSearches = biqSearchesList.Count(p => p.SearchType == "MULTI");
                            application.NumLegacySearches = biqSearchesList.Count(p => p.SearchType == "LEGACY42");
                        }
                        if (biqMetricsList != null)
                        {
                            application.NumSavedMetrics = biqMetricsList.Count;
                        }
                        if (businessJourneysList != null)
                        {
                            application.NumBusinessJourneys = businessJourneysList.Count;
                        }
                        if (experienceLevelsList != null)
                        {
                            application.NumExperienceLevels = experienceLevelsList.Count;
                        }
                        if (schemasList != null)
                        {
                            application.NumSchemas = schemasList.Count;
                        }
                        if (schemaFieldsList != null)
                        {
                            application.NumFields = schemaFieldsList.Count;
                        }
                        List<BIQApplication> applicationsList = new List<BIQApplication>(1);
                        applicationsList.Add(application);

                        FileIOHelper.WriteListToCSVFile(applicationsList, new BIQApplicationReportMap(), FilePathMap.BIQApplicationsIndexFilePath(jobTarget));

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + 1;

                        #endregion

                        #region Combine All for Report CSV

                        // If it is the first one, clear out the combined folder
                        if (reportFolderCleaned == false)
                        {
                            FileIOHelper.DeleteFolder(FilePathMap.BIQEntitiesReportFolderPath());
                            Thread.Sleep(1000);
                            FileIOHelper.CreateFolder(FilePathMap.BIQEntitiesReportFolderPath());
                            reportFolderCleaned = true;
                        }

                        // Append all the individual report files into one
                        if (File.Exists(FilePathMap.BIQApplicationsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.BIQApplicationsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.BIQApplicationsReportFilePath(), FilePathMap.BIQApplicationsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.BIQSearchesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.BIQSearchesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.BIQSearchesReportFilePath(), FilePathMap.BIQSearchesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.BIQWidgetsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.BIQWidgetsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.BIQWidgetsReportFilePath(), FilePathMap.BIQWidgetsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.BIQMetricsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.BIQMetricsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.BIQMetricsReportFilePath(), FilePathMap.BIQMetricsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.BIQBusinessJourneysIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.BIQBusinessJourneysIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.BIQBusinessJourneysReportFilePath(), FilePathMap.BIQBusinessJourneysIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.BIQExperienceLevelsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.BIQExperienceLevelsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.BIQExperienceLevelsReportFilePath(), FilePathMap.BIQExperienceLevelsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.BIQSchemasIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.BIQSchemasIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.BIQSchemasReportFilePath(), FilePathMap.BIQSchemasIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.BIQSchemaFieldsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.BIQSchemaFieldsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.BIQSchemaFieldsReportFilePath(), FilePathMap.BIQSchemaFieldsIndexFilePath(jobTarget));
                        }

                        #endregion
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex);
                        loggerConsole.Warn(ex);

                        return false;
                    }
                    finally
                    {
                        stopWatchTarget.Stop();

                        this.DisplayJobTargetEndedStatus(jobConfiguration, jobTarget, i + 1, stopWatchTarget);

                        stepTimingTarget.EndTime = DateTime.Now;
                        stepTimingTarget.Duration = stopWatchTarget.Elapsed;
                        stepTimingTarget.DurationMS = stopWatchTarget.ElapsedMilliseconds;

                        List<StepTiming> stepTimings = new List<StepTiming>(1);
                        stepTimings.Add(stepTimingTarget);
                        FileIOHelper.WriteListToCSVFile(stepTimings, new StepTimingReportMap(), FilePathMap.StepTimingReportFilePath(), true);
                    }
                }

                // Let's append all Applications
                var controllers = jobConfiguration.Target.GroupBy(t => t.Controller);
                foreach (var controllerGroup in controllers)
                {

                }

                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                loggerConsole.Error(ex);

                return false;
            }
            finally
            {
                stopWatch.Stop();

                this.DisplayJobStepEndedStatus(jobConfiguration, stopWatch);

                stepTimingFunction.EndTime = DateTime.Now;
                stepTimingFunction.Duration = stopWatch.Elapsed;
                stepTimingFunction.DurationMS = stopWatch.ElapsedMilliseconds;

                List<StepTiming> stepTimings = new List<StepTiming>(1);
                stepTimings.Add(stepTimingFunction);
                FileIOHelper.WriteListToCSVFile(stepTimings, new StepTimingReportMap(), FilePathMap.StepTimingReportFilePath(), true);
            }
        }

        public override bool ShouldExecute(ProgramOptions programOptions, JobConfiguration jobConfiguration)
        {
            logger.Trace("LicensedReports.DetectedEntities={0}", programOptions.LicensedReports.DetectedEntities);
            loggerConsole.Trace("LicensedReports.DetectedEntities={0}", programOptions.LicensedReports.DetectedEntities);
            if (programOptions.LicensedReports.DetectedEntities == false)
            {
                loggerConsole.Warn("Not licensed for detected entities");
                return false;
            }

            logger.Trace("Input.DetectedEntities={0}", jobConfiguration.Input.DetectedEntities);
            loggerConsole.Trace("Input.DetectedEntities={0}", jobConfiguration.Input.DetectedEntities);
            if (jobConfiguration.Input.DetectedEntities == false)
            {
                loggerConsole.Trace("Skipping index of detected entities");
            }
            return (jobConfiguration.Input.DetectedEntities == true);
        }

        private static void getStagesRecursive(JToken stageObject, StringBuilder sb)
        {
            if (stageObject != null)
            {
                if (isTokenPropertyNull(stageObject, "name") == false)
                {
                    if (sb.Length > 0) sb.Append("->");
                    sb.Append(getStringValueFromJToken(stageObject, "name"));
                }
                if (isTokenPropertyNull(stageObject, "next") == false)
                {
                    getStagesRecursive(stageObject["next"], sb);
                }
            }
            return;
        }
    }
}
