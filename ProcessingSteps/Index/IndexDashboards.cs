using AppDynamics.Dexter.ReportObjectMaps;
using AppDynamics.Dexter.ReportObjects;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class IndexDashboards : JobStepIndexBase
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
                if (this.ShouldExecute(jobConfiguration) == false)
                {
                    return true;
                }

                bool reportFolderCleaned = false;

                // Process each Controller once
                int i = 0;
                var controllers = jobConfiguration.Target.GroupBy(t => t.Controller);
                foreach (var controllerGroup in controllers)
                {
                    Stopwatch stopWatchTarget = new Stopwatch();
                    stopWatchTarget.Start();

                    JobTarget jobTarget = controllerGroup.ToList()[0];

                    StepTiming stepTimingTarget = new StepTiming();
                    stepTimingTarget.Controller = jobTarget.Controller;
                    stepTimingTarget.ApplicationName = jobTarget.Application;
                    stepTimingTarget.ApplicationID = jobTarget.ApplicationID;
                    stepTimingTarget.JobFileName = programOptions.OutputJobFilePath;
                    stepTimingTarget.StepName = jobConfiguration.Status.ToString();
                    stepTimingTarget.StepID = (int)jobConfiguration.Status;
                    stepTimingTarget.StartTime = DateTime.Now;

                    stepTimingTarget.NumEntities = 0;

                    try
                    {
                        this.DisplayJobTargetStartingStatus(jobConfiguration, jobTarget, i + 1);

                        #region Dashboards List and Widgets List

                        loggerConsole.Info("Dashboards and their widgets");

                        List<Dashboard> dashboardsList = new List<Dashboard>(1024);
                        List<DashboardWidget> dashboardWidgetsAllList = new List<DashboardWidget>(10240);
                        List<DashboardMetricSeries> dashboardMetricSeriesAllList = new List<DashboardMetricSeries>(10240);

                        JArray dashboardsArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.ControllerDashboards(jobTarget));
                        if (dashboardsArray != null)
                        {
                            int j = 0;
                            foreach (JObject dashboardObject in dashboardsArray)
                            {
                                Dashboard dashboard = new Dashboard();

                                dashboard.Controller = jobTarget.Controller;

                                dashboard.DashboardName = getStringValueFromJToken(dashboardObject, "name");
                                dashboard.Description = getStringValueFromJToken(dashboardObject, "description");

                                dashboard.CanvasType = getStringValueFromJToken(dashboardObject, "canvasType").Replace("CANVAS_TYPE_", "");
                                dashboard.TemplateEntityType = getStringValueFromJToken(dashboardObject, "templateEntityType");
                                dashboard.SecurityToken = getStringValueFromJToken(dashboardObject, "securityToken");
                                if (dashboard.SecurityToken.Length > 0) dashboard.IsShared = true;
                                dashboard.IsSharingRevoked= getBoolValueFromJToken(dashboardObject, "sharingRevoked");
                                dashboard.IsTemplate = getBoolValueFromJToken(dashboardObject, "template");

                                dashboard.Height = getIntValueFromJToken(dashboardObject, "height");
                                dashboard.Width = getIntValueFromJToken(dashboardObject, "width");

                                dashboard.BackgroundColor = getIntValueFromJToken(dashboardObject, "backgroundColor").ToString("X6");

                                dashboard.MinutesBefore = getIntValueFromJToken(dashboardObject, "minutesBeforeAnchorTime");
                                dashboard.RefreshInterval = getIntValueFromJToken(dashboardObject, "refreshInterval") / 1000;

                                dashboard.StartTimeUtc = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(dashboardObject, "startTime"));
                                try { dashboard.StartTime = dashboard.StartTimeUtc.ToLocalTime(); } catch { }
                                dashboard.EndTimeUtc = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(dashboardObject, "endTime"));
                                try { dashboard.EndTime = dashboard.EndTimeUtc.ToLocalTime();} catch { }

                                dashboard.CreatedBy = getStringValueFromJToken(dashboardObject, "createdBy");
                                dashboard.CreatedOnUtc = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(dashboardObject, "createdOn"));
                                try { dashboard.CreatedOn = dashboard.CreatedOnUtc.ToLocalTime(); } catch { }
                                dashboard.UpdatedBy = getStringValueFromJToken(dashboardObject, "modifiedBy");
                                dashboard.UpdatedOnUtc = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(dashboardObject, "modifiedOn"));
                                try { dashboard.UpdatedOn = dashboard.UpdatedOnUtc.ToLocalTime(); } catch { }

                                dashboard.DashboardID = getLongValueFromJToken(dashboardObject, "id");

                                dashboard.DashboardLink = String.Format(DEEPLINK_DASHBOARD, dashboard.Controller, dashboard.DashboardID, DEEPLINK_TIMERANGE_LAST_15_MINUTES);

                                // Now parse the Widgets
                                JObject dashboardDetailObject = FileIOHelper.LoadJObjectFromFile(FilePathMap.ControllerDashboard(jobTarget, dashboard.DashboardName, dashboard.DashboardID));
                                if (dashboardDetailObject != null && isTokenPropertyNull(dashboardDetailObject, "widgetTemplates") == false)
                                {
                                    List<DashboardWidget> dashboardWidgetsList = new List<DashboardWidget>(dashboardDetailObject["widgetTemplates"].Count());

                                    int dashboardWidgetIndex = 0;
                                    foreach (JObject dashboardWidgetObject in dashboardDetailObject["widgetTemplates"])
                                    {
                                        DashboardWidget dashboardWidget = new DashboardWidget();

                                        dashboardWidget.Controller = dashboard.Controller;
                                        dashboardWidget.DashboardName = dashboard.DashboardName;
                                        dashboardWidget.DashboardID = dashboard.DashboardID;
                                        dashboardWidget.CanvasType = dashboard.CanvasType;
                                        dashboardWidget.WidgetType = getStringValueFromJToken(dashboardWidgetObject, "widgetType");
                                        dashboardWidget.Index = dashboardWidgetIndex;

                                        dashboardWidget.ApplicationName = getStringValueFromJToken(dashboardWidgetObject["applicationReference"], "applicationName");
                                        dashboardWidget.EntityType = getStringValueFromJToken(dashboardWidgetObject, "entityType");
                                        dashboardWidget.EntitySelectionType = getStringValueFromJToken(dashboardWidgetObject, "entitySelectionType");
                                        try
                                        {
                                            if (isTokenPropertyNull(dashboardWidgetObject, "entityReferences") == false)
                                            {
                                                string[] entities = new string[dashboardWidgetObject["entityReferences"].Count()];
                                                int entitiesIndex = 0;
                                                foreach (JToken entityReferenceToken in dashboardWidgetObject["entityReferences"])
                                                {
                                                    string scopingEntityName = getStringValueFromJToken(entityReferenceToken, "scopingEntityName");
                                                    string entityName = getStringValueFromJToken(entityReferenceToken, "entityName");
                                                    string subType = getStringValueFromJToken(entityReferenceToken, "subtype");
                                                    StringBuilder sb = new StringBuilder(100);
                                                    sb.Append(scopingEntityName);
                                                    if (scopingEntityName.Length > 0) sb.Append("/");
                                                    sb.Append(entityName);
                                                    if (subType.Length > 0) sb.AppendFormat(" [{0}]", subType);
                                                    entities[entitiesIndex] = sb.ToString();
                                                    entitiesIndex++; ;
                                                }
                                                dashboardWidget.SelectedEntities = String.Join(";", entities);
                                                dashboardWidget.NumSelectedEntities = entities.Length;
                                            }
                                        }
                                        catch { }

                                        dashboardWidget.Title = getStringValueFromJToken(dashboardWidgetObject, "title");
                                        dashboardWidget.Description = getStringValueFromJToken(dashboardWidgetObject, "description");
                                        dashboardWidget.Label = getStringValueFromJToken(dashboardWidgetObject, "label");
                                        dashboardWidget.Text = getStringValueFromJToken(dashboardWidgetObject, "text");
                                        dashboardWidget.TextAlign = getStringValueFromJToken(dashboardWidgetObject, "textAlign");

                                        dashboardWidget.Width = getIntValueFromJToken(dashboardWidgetObject, "width");
                                        dashboardWidget.Height = getIntValueFromJToken(dashboardWidgetObject, "height");
                                        dashboardWidget.MinWidth = getIntValueFromJToken(dashboardWidgetObject, "minHeight");
                                        dashboardWidget.MinHeight = getIntValueFromJToken(dashboardWidgetObject, "minWidth");
                                        dashboardWidget.X = getIntValueFromJToken(dashboardWidgetObject, "x");
                                        dashboardWidget.Y = getIntValueFromJToken(dashboardWidgetObject, "y");

                                        dashboardWidget.ForegroundColor = getIntValueFromJToken(dashboardWidgetObject, "color").ToString("X6");
                                        dashboardWidget.BackgroundColor = getIntValueFromJToken(dashboardWidgetObject, "backgroundColor").ToString("X6");
                                        dashboardWidget.BackgroundAlpha = getDoubleValueFromJToken(dashboardWidgetObject, "backgroundAlpha");

                                        dashboardWidget.BorderColor = getIntValueFromJToken(dashboardWidgetObject, "borderColor").ToString("X6");
                                        dashboardWidget.BorderSize = getIntValueFromJToken(dashboardWidgetObject, "borderThickness");
                                        dashboardWidget.IsBorderEnabled = getBoolValueFromJToken(dashboardWidgetObject, "borderEnabled");

                                        dashboardWidget.Margin = getIntValueFromJToken(dashboardWidgetObject, "margin");

                                        try { dashboardWidget.NumDataSeries = dashboardWidgetObject["dataSeriesTemplates"].Count(); } catch { }

                                        dashboardWidget.FontSize = getIntValueFromJToken(dashboardWidgetObject, "fontSize");

                                        dashboardWidget.MinutesBeforeAnchor = getIntValueFromJToken(dashboardWidgetObject, "minutesBeforeAnchorTime");

                                        dashboardWidget.VerticalAxisLabel = getStringValueFromJToken(dashboardWidgetObject, "verticalAxisLabel");
                                        dashboardWidget.HorizontalAxisLabel = getStringValueFromJToken(dashboardWidgetObject, "horizontalAxisLabel");
                                        dashboardWidget.AxisType = getStringValueFromJToken(dashboardWidgetObject, "axisType");
                                        dashboardWidget.IsMultipleYAxis = getBoolValueFromJToken(dashboardWidgetObject, "multipleYAxis");
                                        dashboardWidget.StackMode = getStringValueFromJToken(dashboardWidgetObject, "stackMode");

                                        dashboardWidget.AggregationType = getStringValueFromJToken(dashboardWidgetObject, "aggregationType");

                                        dashboardWidget.DrillDownURL = getStringValueFromJToken(dashboardWidgetObject, "drillDownUrl");
                                        dashboardWidget.IsDrillDownMetricBrowser = getBoolValueFromJToken(dashboardWidgetObject, "useMetricBrowserAsDrillDown");

                                        dashboardWidget.IsShowEvents = getBoolValueFromJToken(dashboardWidgetObject, "showEvents");
                                        dashboardWidget.EventFilter = getStringValueOfObjectFromJToken(dashboardWidgetObject, "eventFilterTemplate", false);

                                        dashboardWidget.ImageURL = getStringValueFromJToken(dashboardWidgetObject, "imageURL");
                                        if (dashboardWidget.ImageURL.Length > 0)
                                        {
                                            if (dashboardWidget.ImageURL.StartsWith("data:") == true)
                                            {
                                                dashboardWidget.EmbeddedImageSize = dashboardWidget.ImageURL.Length;
                                                dashboardWidget.ImageURL = "Embedded base64 image";
                                            }
                                        }

                                        dashboardWidget.SourceURL = getStringValueFromJToken(dashboardWidgetObject, "sourceURL");
                                        dashboardWidget.IsSandbox = getBoolValueFromJToken(dashboardWidgetObject, "sandbox");

                                        if (dashboardWidget.WidgetType == "AnalyticsWidget")
                                        {
                                            try
                                            {
                                                if (dashboardWidgetObject["adqlQueryList"].Count() == 0)
                                                {
                                                    dashboardWidget.AnalyticsQueries = String.Empty;
                                                }
                                                else if (dashboardWidgetObject["adqlQueryList"].Count() == 1)
                                                {
                                                    dashboardWidget.AnalyticsQueries = dashboardWidgetObject["adqlQueryList"][0].ToString();
                                                }
                                                else
                                                {
                                                    dashboardWidget.AnalyticsQueries = getStringValueOfObjectFromJToken(dashboardWidgetObject, "adqlQueryList", false);
                                                }
                                            }
                                            catch { }
                                            dashboardWidget.AnalyticsWidgetType = getStringValueFromJToken(dashboardWidgetObject, "analyticsWidgetType");
                                            dashboardWidget.AnalyticsSearchMode = getStringValueFromJToken(dashboardWidgetObject, "searchMode");
                                        }

                                        #region Maybe discern between Widget Types?

                                        //switch (dashboardWidget.WidgetType)
                                        //{
                                        //    case "HealthListWidget":
                                        //        break;

                                        //    case "ImageWidget":
                                        //        break;

                                        //    case "TextWidget":
                                        //        break;

                                        //    case "GraphWidget":
                                        //        break;

                                        //    case "MetricLabelWidget":
                                        //        break;

                                        //    case "EventListWidget":
                                        //        break;

                                        //    case "AnalyticsWidget":
                                        //        break;

                                        //    case "PieWidget":
                                        //        break;

                                        //    case "GaugeWidget":
                                        //        break;

                                        //    case "IFrameWidget":
                                        //        break;

                                        //    default:
                                        //        logger.Warn("Unknown Widget Type {0} in {1}, Widget {2}", dashboardWidget.WidgetType, dashboard, dashboardWidget.Index);
                                        //        loggerConsole.Warn("Unknown Widget Type {0} in {1}, Widget {2}", dashboardWidget.WidgetType, dashboard, dashboardWidget.Index);

                                        //        break;
                                        //}

                                        #endregion

                                        dashboardWidgetsList.Add(dashboardWidget);

                                        // Now process metric data series for widgets that support them
                                        if (dashboardWidget.NumDataSeries > 0)
                                        {
                                            List<DashboardMetricSeries> dashboardMetricSeriesList = new List<DashboardMetricSeries>(dashboardWidget.NumDataSeries);
                                            foreach (JObject dashboardMetricSeriesObject in dashboardWidgetObject["dataSeriesTemplates"])
                                            {
                                                DashboardMetricSeries dashboardMetricSeries = new DashboardMetricSeries();
                                                dashboardMetricSeries.Controller = dashboard.Controller;
                                                dashboardMetricSeries.DashboardName = dashboard.DashboardName;
                                                dashboardMetricSeries.DashboardID = dashboard.DashboardID;
                                                dashboardMetricSeries.CanvasType = dashboard.CanvasType;
                                                dashboardMetricSeries.WidgetType = dashboardWidget.WidgetType;
                                                dashboardMetricSeries.Index = dashboardWidget.Index;

                                                dashboardMetricSeries.SeriesName = getStringValueFromJToken(dashboardMetricSeriesObject, "name");
                                                dashboardMetricSeries.SeriesType = getStringValueFromJToken(dashboardMetricSeriesObject, "seriesType");
                                                dashboardMetricSeries.MetricType = getStringValueFromJToken(dashboardMetricSeriesObject, "metricType");
                                                if (isTokenPropertyNull(dashboardMetricSeriesObject, "colorPalette") == false)
                                                {
                                                    if (isTokenPropertyNull(dashboardMetricSeriesObject["colorPalette"], "colors") == false)
                                                    {
                                                        string[] entities = new string[dashboardMetricSeriesObject["colorPalette"]["colors"].Count()];
                                                        int entitiesIndex = 0;
                                                        foreach (JToken entityReferenceToken in dashboardMetricSeriesObject["colorPalette"]["colors"])
                                                        {
                                                            int color = (int)entityReferenceToken;
                                                            entities[entitiesIndex] = color.ToString("X6");
                                                            entitiesIndex++; ;
                                                        }
                                                        dashboardMetricSeries.Colors = String.Join(";", entities);
                                                        dashboardMetricSeries.NumColors = entities.Length;
                                                    }
                                                }
                                                dashboardMetricSeries.Axis = getStringValueFromJToken(dashboardMetricSeriesObject, "axisPosition");

                                                if (isTokenPropertyNull(dashboardMetricSeriesObject, "metricMatchCriteriaTemplate") == false)
                                                {
                                                    JObject dashboardMetricSeriesMetricMatchCriteriaTemplateObject = (JObject)dashboardMetricSeriesObject["metricMatchCriteriaTemplate"];

                                                    dashboardMetricSeries.MaxResults = getIntValueFromJToken(dashboardMetricSeriesMetricMatchCriteriaTemplateObject, "maxResults");

                                                    dashboardMetricSeries.ApplicationName = getStringValueFromJToken(dashboardMetricSeriesMetricMatchCriteriaTemplateObject, "applicationName");
                                                    dashboardMetricSeries.Expression = getStringValueFromJToken(dashboardMetricSeriesMetricMatchCriteriaTemplateObject, "expressionString");
                                                    dashboardMetricSeries.EvalScopeType = getStringValueFromJToken(dashboardMetricSeriesMetricMatchCriteriaTemplateObject, "evaluationScopeType");
                                                    dashboardMetricSeries.Baseline = getStringValueFromJToken(dashboardMetricSeriesMetricMatchCriteriaTemplateObject, "baselineName");
                                                    dashboardMetricSeries.DisplayStyle = getStringValueFromJToken(dashboardMetricSeriesMetricMatchCriteriaTemplateObject, "metricDisplayNameStyle");
                                                    dashboardMetricSeries.DisplayFormat = getStringValueFromJToken(dashboardMetricSeriesMetricMatchCriteriaTemplateObject, "metricDisplayNameCustomFormat");
                                                    dashboardMetricSeries.IsRollup = getBoolValueFromJToken(dashboardMetricSeriesMetricMatchCriteriaTemplateObject, "rollupMetricData");
                                                    dashboardMetricSeries.UseActiveBaseline = getBoolValueFromJToken(dashboardMetricSeriesMetricMatchCriteriaTemplateObject, "useActiveBaseline");
                                                    if (getBoolValueFromJToken(dashboardMetricSeriesMetricMatchCriteriaTemplateObject, "sortResultsAscending") == true)
                                                    {
                                                        dashboardMetricSeries.SortDirection = "Ascending";
                                                    }
                                                    else
                                                    {
                                                        dashboardMetricSeries.SortDirection = "Descending";
                                                    }

                                                    if (isTokenPropertyNull(dashboardMetricSeriesMetricMatchCriteriaTemplateObject, "entityMatchCriteria") == false)
                                                    {
                                                        JObject dashboardMetricSeriesEntityMatchCriteriaObject = (JObject)dashboardMetricSeriesMetricMatchCriteriaTemplateObject["entityMatchCriteria"];

                                                        dashboardMetricSeries.IsSummary = getBoolValueFromJToken(dashboardMetricSeriesEntityMatchCriteriaObject, "summary");
                                                        dashboardMetricSeries.EntityType = getStringValueFromJToken(dashboardMetricSeriesEntityMatchCriteriaObject, "entityType");
                                                        dashboardMetricSeries.EntitySelectionType = getStringValueFromJToken(dashboardMetricSeriesEntityMatchCriteriaObject, "matchCriteriaType");
                                                        dashboardMetricSeries.AgentType = getStringValueOfObjectFromJToken(dashboardMetricSeriesEntityMatchCriteriaObject, "agentTypes", true);

                                                        if (isTokenPropertyNull(dashboardMetricSeriesEntityMatchCriteriaObject, "entityNames") == false)
                                                        {
                                                            string[] entities = new string[dashboardMetricSeriesEntityMatchCriteriaObject["entityNames"].Count()];
                                                            int entitiesIndex = 0;
                                                            foreach (JToken entityReferenceToken in dashboardMetricSeriesEntityMatchCriteriaObject["entityNames"])
                                                            {
                                                                string scopingEntityName = getStringValueFromJToken(entityReferenceToken, "scopingEntityName");
                                                                string entityName = getStringValueFromJToken(entityReferenceToken, "entityName");
                                                                string subType = getStringValueFromJToken(entityReferenceToken, "subtype");
                                                                StringBuilder sb = new StringBuilder(100);
                                                                sb.Append(scopingEntityName);
                                                                if (scopingEntityName.Length > 0) sb.Append("/");
                                                                sb.Append(entityName);
                                                                if (subType.Length > 0) sb.AppendFormat(" [{0}]", subType);
                                                                entities[entitiesIndex] = sb.ToString();
                                                                entitiesIndex++; ;
                                                            }
                                                            dashboardMetricSeries.SelectedEntities = String.Join(";", entities);
                                                            dashboardMetricSeries.NumSelectedEntities = entities.Length;
                                                        }
                                                    }

                                                    if (isTokenPropertyNull(dashboardMetricSeriesMetricMatchCriteriaTemplateObject, "metricExpressionTemplate") == false)
                                                    {
                                                        JObject dashboardMetricSeriesMetricExpressionTemplateObject = (JObject)dashboardMetricSeriesMetricMatchCriteriaTemplateObject["metricExpressionTemplate"];

                                                        dashboardMetricSeries.MetricExpressionType = getStringValueFromJToken(dashboardMetricSeriesMetricExpressionTemplateObject, "metricExpressionType");
                                                        dashboardMetricSeries.MetricDisplayName = getStringValueFromJToken(dashboardMetricSeriesMetricExpressionTemplateObject, "displayName");
                                                        if (dashboardMetricSeries.MetricDisplayName == "null")
                                                        {
                                                            // yes, sometimes that value is saved as string "null"
                                                            dashboardMetricSeries.MetricDisplayName = String.Empty;
                                                        }
                                                        dashboardMetricSeries.FunctionType = getStringValueFromJToken(dashboardMetricSeriesMetricExpressionTemplateObject, "functionType");

                                                        dashboardMetricSeries.MetricPath = getStringValueFromJToken(dashboardMetricSeriesMetricExpressionTemplateObject, "relativeMetricPath");
                                                        if (dashboardMetricSeries.MetricPath.Length == 0)
                                                        {
                                                            // Must be dashboardMetricSeries.MetricExpressionType = Absolute
                                                            dashboardMetricSeries.MetricPath = getStringValueFromJToken(dashboardMetricSeriesMetricExpressionTemplateObject, "metricPath");
                                                        }

                                                        if (dashboardMetricSeries.NumSelectedEntities == 0)
                                                        {
                                                            // Must be dashboardMetricSeries.MetricExpressionType = Absolute
                                                            if (isTokenPropertyNull(dashboardMetricSeriesMetricExpressionTemplateObject, "scopeEntity") == false)
                                                            {
                                                                JObject dashboardMetricSeriesScopeEntityObject = (JObject)dashboardMetricSeriesMetricExpressionTemplateObject["scopeEntity"];

                                                                string scopingEntityName = getStringValueFromJToken(dashboardMetricSeriesScopeEntityObject, "scopingEntityName");
                                                                string entityName = getStringValueFromJToken(dashboardMetricSeriesScopeEntityObject, "entityName");
                                                                string subType = getStringValueFromJToken(dashboardMetricSeriesScopeEntityObject, "subtype");
                                                                StringBuilder sb = new StringBuilder(100);
                                                                sb.Append(scopingEntityName);
                                                                if (scopingEntityName.Length > 0) sb.Append("/");
                                                                sb.Append(entityName);
                                                                if (subType.Length > 0) sb.AppendFormat(" [{0}]", subType);

                                                                dashboardMetricSeries.SelectedEntities = sb.ToString();
                                                                dashboardMetricSeries.NumSelectedEntities = 1;
                                                            }
                                                        }

                                                        if (dashboardMetricSeries.MetricExpressionType == "Boolean")
                                                        {
                                                            dashboardMetricSeries.ExpressionOperator = getStringValueFromJToken(dashboardMetricSeriesMetricExpressionTemplateObject["operator"], "type");
                                                            dashboardMetricSeries.Expression1 = getStringValueOfObjectFromJToken(dashboardMetricSeriesMetricExpressionTemplateObject, "expression1", false);
                                                            dashboardMetricSeries.Expression2 = getStringValueOfObjectFromJToken(dashboardMetricSeriesMetricExpressionTemplateObject, "expression2", false);
                                                        }
                                                    }
                                                }

                                                dashboardMetricSeriesList.Add(dashboardMetricSeries);
                                            }

                                            dashboardMetricSeriesAllList.AddRange(dashboardMetricSeriesList);
                                        }

                                        dashboardWidgetIndex++;
                                    }

                                    dashboardWidgetsAllList.AddRange(dashboardWidgetsList);

                                    dashboard.NumWidgets = dashboardWidgetsList.Count;
                                    dashboard.NumAnalyticsWidgets = dashboardWidgetsList.Count(d => d.WidgetType == "AnalyticsWidget");
                                    dashboard.NumEventListWidgets = dashboardWidgetsList.Count(d => d.WidgetType == "EventListWidget");
                                    dashboard.NumGaugeWidgets = dashboardWidgetsList.Count(d => d.WidgetType == "GaugeWidget");
                                    dashboard.NumGraphWidgets = dashboardWidgetsList.Count(d => d.WidgetType == "GraphWidget");
                                    dashboard.NumHealthListWidgets = dashboardWidgetsList.Count(d => d.WidgetType == "HealthListWidget");
                                    dashboard.NumIFrameWidgets = dashboardWidgetsList.Count(d => d.WidgetType == "IFrameWidget");
                                    dashboard.NumImageWidgets = dashboardWidgetsList.Count(d => d.WidgetType == "ImageWidget");
                                    dashboard.NumMetricLabelWidgets = dashboardWidgetsList.Count(d => d.WidgetType == "MetricLabelWidget");
                                    dashboard.NumPieWidgets = dashboardWidgetsList.Count(d => d.WidgetType == "PieWidget");
                                    dashboard.NumTextWidgets = dashboardWidgetsList.Count(d => d.WidgetType == "TextWidget");
                                }
                                else
                                {
                                    if (isTokenPropertyNull(dashboardDetailObject, "success") == false && 
                                        getBoolValueFromJToken(dashboardDetailObject, "success") == false)
                                    {
                                        dashboard.NumWidgets = -1;
                                    }
                                }

                                dashboardsList.Add(dashboard);

                                j++;
                                if (j % 100 == 0)
                                {
                                    Console.Write("[{0}].", j);
                                }
                            }
                        }

                        loggerConsole.Info("{0} Dashboards", dashboardsList.Count);
                        loggerConsole.Info("{0} Dashboard Widgets", dashboardWidgetsAllList.Count);
                        loggerConsole.Info("{0} Dashboard Widget Time Series", dashboardMetricSeriesAllList.Count);

                        dashboardsList = dashboardsList.OrderBy(d => d.DashboardName).ToList();
                        FileIOHelper.WriteListToCSVFile(dashboardsList, new DashboardReportMap(), FilePathMap.DashboardsIndexFilePath(jobTarget));

                        dashboardWidgetsAllList = dashboardWidgetsAllList.OrderBy(d => d.DashboardName).ThenBy(d => d.Index).ToList();
                        FileIOHelper.WriteListToCSVFile(dashboardWidgetsAllList, new DashboardWidgetReportMap(), FilePathMap.DashboardWidgetsIndexFilePath(jobTarget));

                        dashboardMetricSeriesAllList = dashboardMetricSeriesAllList.OrderBy(d => d.DashboardName).ThenBy(d => d.Index).ThenBy(d => d.SeriesName).ToList();
                        FileIOHelper.WriteListToCSVFile(dashboardMetricSeriesAllList, new DashboardMetricSeriesReportMap(), FilePathMap.DashboardMetricSeriesIndexFilePath(jobTarget));

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + dashboardsList.Count;

                        #endregion

                        #region Combine All for Report CSV

                        // If it is the first one, clear out the combined folder
                        if (reportFolderCleaned == false)
                        {
                            FileIOHelper.DeleteFolder(FilePathMap.ControllerDashboardsReportFolderPath());
                            Thread.Sleep(1000);
                            FileIOHelper.CreateFolder(FilePathMap.ControllerDashboardsReportFolderPath());
                            reportFolderCleaned = true;
                        }

                        // Append all the individual report files into one
                        if (File.Exists(FilePathMap.DashboardsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.DashboardsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.DashboardsReportFilePath(), FilePathMap.DashboardsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.DashboardWidgetsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.DashboardWidgetsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.DashboardWidgetsReportFilePath(), FilePathMap.DashboardWidgetsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.DashboardMetricSeriesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.DashboardMetricSeriesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.DashboardMetricSeriesReportFilePath(), FilePathMap.DashboardMetricSeriesIndexFilePath(jobTarget));
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

                    i++;
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

        public override bool ShouldExecute(JobConfiguration jobConfiguration)
        {
            logger.Trace("Input.Dashboards={0}", jobConfiguration.Input.Dashboards);
            loggerConsole.Trace("Input.Dashboards={0}", jobConfiguration.Input.Dashboards);
            if (jobConfiguration.Input.Dashboards == false)
            {
                loggerConsole.Trace("Skipping index of dashboards");
            }
            return (jobConfiguration.Input.Dashboards == true);
        }
    }
}
