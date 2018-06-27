using AppDynamics.Dexter.DataObjects;
using AppDynamics.Dexter.ReportObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class IndexApplicationAndEntityMetrics : JobStepIndexBase
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

                List<MetricExtractMapping> entityMetricExtractMappingList = getMetricsExtractMappingList(jobConfiguration);

                // Process each target
                for (int i = 0; i < jobConfiguration.Target.Count; i++)
                {
                    Stopwatch stopWatchTarget = new Stopwatch();
                    stopWatchTarget.Start();

                    JobTarget jobTarget = jobConfiguration.Target[i];

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

                        #region Target state check

                        if (jobTarget.Status != JobTargetStatus.ConfigurationValid)
                        {
                            loggerConsole.Trace("Target in invalid state {0}, skipping", jobTarget.Status);

                            continue;
                        }

                        #endregion

                        #region Target step variables

                        int numEntitiesTotal = 0;

                        #endregion

                        Parallel.Invoke(
                            () =>
                            {
                                #region Application

                                List<EntityApplication> applicationsList = FileIOHelper.ReadListFromCSVFile<EntityApplication>(FilePathMap.ApplicationIndexFilePath(jobTarget), new ApplicationEntityReportMap());
                                if (applicationsList != null)
                                {
                                    loggerConsole.Info("Index Metrics for Applications ({0} entities, {1} timeranges)", applicationsList.Count, jobConfiguration.Input.HourlyTimeRanges.Count);

                                    #region Process Full Range Metrics

                                    if (File.Exists(FilePathMap.EntitiesFullIndexFilePath(jobTarget, EntityApplication.ENTITY_FOLDER)) == false)
                                    {
                                        // Prepare copies of entities indexed for fast access by their entity ID
                                        Dictionary<long, EntityBase> entitiesFullDictionary = applicationsList.ToDictionary(e => e.EntityID, e => (EntityBase)(e.Clone()));

                                        readRolledUpRangeOfMetricsIntoEntities(entitiesFullDictionary, null, jobConfiguration.Input.TimeRange, jobTarget, entityMetricExtractMappingList, EntityApplication.ENTITY_FOLDER, EntityApplication.ENTITY_TYPE);

                                        foreach (EntityBase entity in entitiesFullDictionary.Values)
                                        {
                                            updateEntityWithDeeplinks(entity, jobConfiguration.Input.TimeRange);
                                            updateEntityRowWithDurationAndActivityStatus(entity, jobConfiguration.Input.TimeRange);
                                            entity.ARTRange = getDurationRangeAsString(entity.ART);
                                        }

                                        // Sort them
                                        List<EntityApplication> applicationsFullList = entitiesFullDictionary.Values.OfType<EntityApplication>().ToList().OrderBy(o => o.ApplicationName).ThenBy(o => o.From).ToList();

                                        FileIOHelper.WriteListToCSVFile(applicationsFullList, new ApplicationMetricReportMap(), FilePathMap.EntitiesFullIndexFilePath(jobTarget, EntityApplication.ENTITY_FOLDER));
                                    }

                                    #endregion

                                    #region Process Hourly Ranges Metrics

                                    if (File.Exists(FilePathMap.EntitiesHourIndexFilePath(jobTarget, EntityApplication.ENTITY_FOLDER)) == false)
                                    {
                                        List<EntityApplication> applicationsHourlyAllList = new List<EntityApplication>(applicationsList.Count * jobConfiguration.Input.HourlyTimeRanges.Count);

                                        Dictionary<string, List<MetricValue>> metricValuesDictionary = new Dictionary<string, List<MetricValue>>();

                                        for (int j = 0; j < jobConfiguration.Input.HourlyTimeRanges.Count; j++)
                                        {
                                            JobTimeRange jobTimeRange = jobConfiguration.Input.HourlyTimeRanges[j];

                                            // Prepare copies of entities indexed for fast access by their entity ID
                                            Dictionary<long, EntityBase> entitiesHourlyDictionary = applicationsList.ToDictionary(e => e.EntityID, e => (EntityBase)(e.Clone()));

                                            readGranularRangeOfMetricsIntoEntities(entitiesHourlyDictionary, null, jobTimeRange, jobTarget, entityMetricExtractMappingList, EntityApplication.ENTITY_FOLDER, EntityApplication.ENTITY_TYPE, metricValuesDictionary);

                                            foreach (EntityBase entity in entitiesHourlyDictionary.Values)
                                            {
                                                updateEntityWithDeeplinks(entity, jobTimeRange);
                                                updateEntityRowWithDurationAndActivityStatus(entity, jobTimeRange);
                                                entity.ARTRange = getDurationRangeAsString(entity.ART);
                                            }

                                            List<EntityApplication> applicationsHourlyList = entitiesHourlyDictionary.Values.OfType<EntityApplication>().ToList();
                                            applicationsHourlyAllList.AddRange(applicationsHourlyList);
                                        }

                                        // Sort them
                                        applicationsHourlyAllList = applicationsHourlyAllList.OrderBy(o => o.ApplicationName).ThenBy(o => o.From).ToList();

                                        FileIOHelper.WriteListToCSVFile(applicationsHourlyAllList, new ApplicationMetricReportMap(), FilePathMap.EntitiesHourIndexFilePath(jobTarget, EntityApplication.ENTITY_FOLDER));

                                        // Save individual metric files and create index of their internal structure
                                        List<EntityHourlyMetricValueLocation> entityMetricValuesLocations = new List<EntityHourlyMetricValueLocation>(metricValuesDictionary.Count * applicationsList.Count * jobConfiguration.Input.HourlyTimeRanges.Count);
                                        foreach (KeyValuePair<string, List<MetricValue>> metricValuesListContainer in metricValuesDictionary)
                                        {
                                            if (metricValuesListContainer.Value.Count > 0)
                                            {
                                                List<MetricValue> metricValuesSorted = metricValuesListContainer.Value.OrderBy(o => o.EntityID).ThenBy(o => o.MetricID).ThenBy(o => o.EventTimeStampUtc).ToList();

                                                FileIOHelper.WriteListToCSVFile(metricValuesSorted, new MetricValueReportMap(), FilePathMap.MetricValuesIndexFilePath(jobTarget, EntityApplication.ENTITY_FOLDER, metricValuesListContainer.Key));
                                                FileIOHelper.WriteListToCSVFile(metricValuesSorted, new MetricValueReportMap(), FilePathMap.MetricReportPerAppFilePath(jobTarget, EntityApplication.ENTITY_FOLDER, metricValuesListContainer.Key));

                                                List<EntityHourlyMetricValueLocation> entityMetricValuesLocationsForSingleMetric = getEntityHourlyMetricValueLocationsInTable(metricValuesSorted, jobConfiguration.Input.HourlyTimeRanges);
                                                if (entityMetricValuesLocationsForSingleMetric != null)
                                                {
                                                    entityMetricValuesLocations.AddRange(entityMetricValuesLocationsForSingleMetric);
                                                }
                                            }
                                        }

                                        // Save entity and metric index lookup
                                        FileIOHelper.WriteListToCSVFile(entityMetricValuesLocations, new EntityHourlyMetricValueLocationReportMap(), FilePathMap.MetricsLocationIndexFilePath(jobTarget, EntityApplication.ENTITY_FOLDER));
                                    }

                                    #endregion

                                    loggerConsole.Info("Completed {0} Applications", applicationsList.Count);

                                    Interlocked.Add(ref numEntitiesTotal, applicationsList.Count);
                                }

                                #endregion
                            },
                            () =>
                            {
                                #region Tier

                                List<EntityTier> tiersList = FileIOHelper.ReadListFromCSVFile<EntityTier>(FilePathMap.TiersIndexFilePath(jobTarget), new TierEntityReportMap());
                                if (tiersList != null)
                                {
                                    loggerConsole.Info("Index Metrics for Tiers ({0} entities, {1} timeranges)", tiersList.Count, jobConfiguration.Input.HourlyTimeRanges.Count);

                                    #region Process Full Range Metrics

                                    if (File.Exists(FilePathMap.EntitiesFullIndexFilePath(jobTarget, EntityTier.ENTITY_FOLDER)) == false)
                                    {
                                        // Prepare copies of entities indexed for fast access by their entity ID
                                        Dictionary<long, EntityBase> entitiesFullDictionary = tiersList.ToDictionary(e => e.EntityID, e => (EntityBase)(e.Clone()));
                                        Dictionary<string, EntityBase> entitiesFullDictionaryByName = new Dictionary<string, EntityBase>(entitiesFullDictionary.Count);
                                        foreach (KeyValuePair<long, EntityBase> kvp in entitiesFullDictionary)
                                        {
                                            entitiesFullDictionaryByName.Add(kvp.Value.EntityName, kvp.Value);
                                        }

                                        readRolledUpRangeOfMetricsIntoEntities(entitiesFullDictionary, entitiesFullDictionaryByName, jobConfiguration.Input.TimeRange, jobTarget, entityMetricExtractMappingList, EntityTier.ENTITY_FOLDER, EntityTier.ENTITY_TYPE);

                                        foreach (EntityBase entity in entitiesFullDictionary.Values)
                                        {
                                            updateEntityWithDeeplinks(entity, jobConfiguration.Input.TimeRange);
                                            updateEntityRowWithDurationAndActivityStatus(entity, jobConfiguration.Input.TimeRange);
                                            entity.ARTRange = getDurationRangeAsString(entity.ART);
                                        }

                                        // Sort them
                                        List<EntityTier> tiersFullList = entitiesFullDictionary.Values.OfType<EntityTier>().ToList().OrderBy(o => o.TierName).ThenBy(o => o.From).ToList();

                                        FileIOHelper.WriteListToCSVFile(tiersFullList, new TierMetricReportMap(), FilePathMap.EntitiesFullIndexFilePath(jobTarget, EntityTier.ENTITY_FOLDER));
                                    }

                                    #endregion

                                    #region Process Hourly Ranges Metrics

                                    if (File.Exists(FilePathMap.EntitiesHourIndexFilePath(jobTarget, EntityTier.ENTITY_FOLDER)) == false)
                                    {
                                        List<EntityTier> tiersHourlyAllList = new List<EntityTier>(tiersList.Count * jobConfiguration.Input.HourlyTimeRanges.Count);

                                        Dictionary<string, List<MetricValue>> metricValuesDictionary = new Dictionary<string, List<MetricValue>>();

                                        for (int j = 0; j < jobConfiguration.Input.HourlyTimeRanges.Count; j++)
                                        {
                                            JobTimeRange jobTimeRange = jobConfiguration.Input.HourlyTimeRanges[j];

                                            // Prepare copies of entities indexed for fast access by their entity ID
                                            Dictionary<long, EntityBase> entitiesHourlyDictionary = tiersList.ToDictionary(e => e.EntityID, e => (EntityBase)(e.Clone()));
                                            Dictionary<string, EntityBase> entitiesHourlyDictionaryByName = new Dictionary<string, EntityBase>(entitiesHourlyDictionary.Count);
                                            foreach (KeyValuePair<long, EntityBase> kvp in entitiesHourlyDictionary)
                                            {
                                                entitiesHourlyDictionaryByName.Add(kvp.Value.EntityName, kvp.Value);
                                            }

                                            readGranularRangeOfMetricsIntoEntities(entitiesHourlyDictionary, entitiesHourlyDictionaryByName, jobTimeRange, jobTarget, entityMetricExtractMappingList, EntityTier.ENTITY_FOLDER, EntityTier.ENTITY_TYPE, metricValuesDictionary);

                                            foreach (EntityBase entity in entitiesHourlyDictionary.Values)
                                            {
                                                updateEntityWithDeeplinks(entity, jobTimeRange);
                                                updateEntityRowWithDurationAndActivityStatus(entity, jobTimeRange);
                                                entity.ARTRange = getDurationRangeAsString(entity.ART);
                                            }

                                            List<EntityTier> tiersHourlyList = entitiesHourlyDictionary.Values.OfType<EntityTier>().ToList();
                                            tiersHourlyAllList.AddRange(tiersHourlyList);
                                        }

                                        // Sort them
                                        tiersHourlyAllList = tiersHourlyAllList.OrderBy(o => o.TierName).ThenBy(o => o.From).ToList();

                                        FileIOHelper.WriteListToCSVFile(tiersHourlyAllList, new TierMetricReportMap(), FilePathMap.EntitiesHourIndexFilePath(jobTarget, EntityTier.ENTITY_FOLDER));

                                        // Save individual metric files and create index of their internal structure
                                        List<EntityHourlyMetricValueLocation> entityMetricValuesLocations = new List<EntityHourlyMetricValueLocation>(metricValuesDictionary.Count * tiersList.Count * jobConfiguration.Input.HourlyTimeRanges.Count);
                                        foreach (KeyValuePair<string, List<MetricValue>> metricValuesListContainer in metricValuesDictionary)
                                        {
                                            if (metricValuesListContainer.Value.Count > 0)
                                            {
                                                List<MetricValue> metricValuesSorted = metricValuesListContainer.Value.OrderBy(o => o.EntityID).ThenBy(o => o.MetricID).ThenBy(o => o.EventTimeStampUtc).ToList();

                                                FileIOHelper.WriteListToCSVFile(metricValuesSorted, new MetricValueReportMap(), FilePathMap.MetricValuesIndexFilePath(jobTarget, EntityTier.ENTITY_FOLDER, metricValuesListContainer.Key));
                                                FileIOHelper.WriteListToCSVFile(metricValuesSorted, new MetricValueReportMap(), FilePathMap.MetricReportPerAppFilePath(jobTarget, EntityTier.ENTITY_FOLDER, metricValuesListContainer.Key));

                                                List<EntityHourlyMetricValueLocation> entityMetricValuesLocationsForSingleMetric = getEntityHourlyMetricValueLocationsInTable(metricValuesSorted, jobConfiguration.Input.HourlyTimeRanges);
                                                if (entityMetricValuesLocationsForSingleMetric != null)
                                                {
                                                    entityMetricValuesLocations.AddRange(entityMetricValuesLocationsForSingleMetric);
                                                }
                                            }
                                        }

                                        // Save entity and metric index lookup
                                        FileIOHelper.WriteListToCSVFile(entityMetricValuesLocations, new EntityHourlyMetricValueLocationReportMap(), FilePathMap.MetricsLocationIndexFilePath(jobTarget, EntityTier.ENTITY_FOLDER));
                                    }

                                    #endregion

                                    loggerConsole.Info("Completed {0} Tiers", tiersList.Count);

                                    Interlocked.Add(ref numEntitiesTotal, tiersList.Count);
                                }

                                #endregion
                            },
                            () =>
                            {
                                #region Nodes

                                List<EntityNode> nodesList = FileIOHelper.ReadListFromCSVFile<EntityNode>(FilePathMap.NodesIndexFilePath(jobTarget), new NodeEntityReportMap());
                                if (nodesList != null)
                                {
                                    loggerConsole.Info("Index Metrics for Nodes ({0} entities, {1} timeranges)", nodesList.Count, jobConfiguration.Input.HourlyTimeRanges.Count);

                                    #region Process Full Range Metrics

                                    if (File.Exists(FilePathMap.EntitiesFullIndexFilePath(jobTarget, EntityNode.ENTITY_FOLDER)) == false)
                                    {
                                        // Prepare copies of entities indexed for fast access by their entity ID
                                        Dictionary<long, EntityBase> entitiesFullDictionary = nodesList.ToDictionary(e => e.EntityID, e => (EntityBase)(e.Clone()));
                                        Dictionary<string, EntityBase> entitiesFullDictionaryByName = new Dictionary<string, EntityBase>(entitiesFullDictionary.Count);
                                        foreach (KeyValuePair<long, EntityBase> kvp in entitiesFullDictionary)
                                        {
                                            try
                                            {
                                                entitiesFullDictionaryByName.Add(String.Format("{0}-{1}", ((EntityNode)(kvp.Value)).TierName, kvp.Value.EntityName), kvp.Value);
                                            }
                                            catch { }
                                        }

                                        readRolledUpRangeOfMetricsIntoEntities(entitiesFullDictionary, entitiesFullDictionaryByName, jobConfiguration.Input.TimeRange, jobTarget, entityMetricExtractMappingList, EntityNode.ENTITY_FOLDER, EntityNode.ENTITY_TYPE);

                                        foreach (EntityBase entity in entitiesFullDictionary.Values)
                                        {
                                            updateEntityWithDeeplinks(entity, jobConfiguration.Input.TimeRange);
                                            updateEntityRowWithDurationAndActivityStatus(entity, jobConfiguration.Input.TimeRange);
                                            entity.ARTRange = getDurationRangeAsString(entity.ART);
                                        }

                                        // Sort them
                                        List<EntityNode> nodesFullList = entitiesFullDictionary.Values.OfType<EntityNode>().ToList().OrderBy(o => o.TierName).ThenBy(o => o.NodeName).ThenBy(o => o.From).ToList();

                                        FileIOHelper.WriteListToCSVFile(nodesFullList, new NodeMetricReportMap(), FilePathMap.EntitiesFullIndexFilePath(jobTarget, EntityNode.ENTITY_FOLDER));
                                    }

                                    #endregion

                                    #region Process Hourly Ranges Metrics

                                    if (File.Exists(FilePathMap.EntitiesHourIndexFilePath(jobTarget, EntityNode.ENTITY_FOLDER)) == false)
                                    {
                                        List<EntityNode> nodesHourlyAllList = new List<EntityNode>(nodesList.Count * jobConfiguration.Input.HourlyTimeRanges.Count);

                                        Dictionary<string, List<MetricValue>> metricValuesDictionary = new Dictionary<string, List<MetricValue>>();

                                        for (int j = 0; j < jobConfiguration.Input.HourlyTimeRanges.Count; j++)
                                        {
                                            JobTimeRange jobTimeRange = jobConfiguration.Input.HourlyTimeRanges[j];

                                            // Prepare copies of entities indexed for fast access by their entity ID
                                            Dictionary<long, EntityBase> entitiesHourlyDictionary = nodesList.ToDictionary(e => e.EntityID, e => (EntityBase)(e.Clone()));
                                            Dictionary<string, EntityBase> entitiesHourlyDictionaryByName = new Dictionary<string, EntityBase>(entitiesHourlyDictionary.Count);
                                            foreach (KeyValuePair<long, EntityBase> kvp in entitiesHourlyDictionary)
                                            {
                                                try
                                                {
                                                    entitiesHourlyDictionaryByName.Add(String.Format("{0}-{1}", ((EntityNode)(kvp.Value)).TierName, kvp.Value.EntityName), kvp.Value);
                                                }
                                                catch { }
                                            }

                                            readGranularRangeOfMetricsIntoEntities(entitiesHourlyDictionary, entitiesHourlyDictionaryByName, jobTimeRange, jobTarget, entityMetricExtractMappingList, EntityNode.ENTITY_FOLDER, EntityNode.ENTITY_TYPE, metricValuesDictionary);

                                            foreach (EntityBase entity in entitiesHourlyDictionary.Values)
                                            {
                                                updateEntityWithDeeplinks(entity, jobTimeRange);
                                                updateEntityRowWithDurationAndActivityStatus(entity, jobTimeRange);
                                                entity.ARTRange = getDurationRangeAsString(entity.ART);
                                            }

                                            List<EntityNode> nodesHourlyList = entitiesHourlyDictionary.Values.OfType<EntityNode>().ToList();
                                            nodesHourlyAllList.AddRange(nodesHourlyList);
                                        }

                                        // Sort them
                                        nodesHourlyAllList = nodesHourlyAllList.OrderBy(o => o.TierName).ThenBy(o => o.NodeName).ThenBy(o => o.From).ToList();

                                        FileIOHelper.WriteListToCSVFile(nodesHourlyAllList, new NodeMetricReportMap(), FilePathMap.EntitiesHourIndexFilePath(jobTarget, EntityNode.ENTITY_FOLDER));

                                        // Save individual metric files and create index of their internal structure
                                        List<EntityHourlyMetricValueLocation> entityMetricValuesLocations = new List<EntityHourlyMetricValueLocation>(metricValuesDictionary.Count * nodesList.Count * jobConfiguration.Input.HourlyTimeRanges.Count);
                                        foreach (KeyValuePair<string, List<MetricValue>> metricValuesListContainer in metricValuesDictionary)
                                        {
                                            if (metricValuesListContainer.Value.Count > 0)
                                            {
                                                List<MetricValue> metricValuesSorted = metricValuesListContainer.Value.OrderBy(o => o.EntityID).ThenBy(o => o.MetricID).ThenBy(o => o.EventTimeStampUtc).ToList();

                                                FileIOHelper.WriteListToCSVFile(metricValuesSorted, new MetricValueReportMap(), FilePathMap.MetricValuesIndexFilePath(jobTarget, EntityNode.ENTITY_FOLDER, metricValuesListContainer.Key));
                                                FileIOHelper.WriteListToCSVFile(metricValuesSorted, new MetricValueReportMap(), FilePathMap.MetricReportPerAppFilePath(jobTarget, EntityNode.ENTITY_FOLDER, metricValuesListContainer.Key));

                                                List<EntityHourlyMetricValueLocation> entityMetricValuesLocationsForSingleMetric = getEntityHourlyMetricValueLocationsInTable(metricValuesSorted, jobConfiguration.Input.HourlyTimeRanges);
                                                if (entityMetricValuesLocationsForSingleMetric != null)
                                                {
                                                    entityMetricValuesLocations.AddRange(entityMetricValuesLocationsForSingleMetric);
                                                }
                                            }
                                        }

                                        // Save entity and metric index lookup
                                        FileIOHelper.WriteListToCSVFile(entityMetricValuesLocations, new EntityHourlyMetricValueLocationReportMap(), FilePathMap.MetricsLocationIndexFilePath(jobTarget, EntityNode.ENTITY_FOLDER));
                                    }

                                    #endregion

                                    loggerConsole.Info("Completed {0} Nodes", nodesList.Count);

                                    Interlocked.Add(ref numEntitiesTotal, nodesList.Count);
                                }

                                #endregion
                            },
                            () =>
                            {
                                #region Backends

                                List<EntityBackend> backendsList = FileIOHelper.ReadListFromCSVFile<EntityBackend>(FilePathMap.BackendsIndexFilePath(jobTarget), new BackendEntityReportMap());
                                if (backendsList != null)
                                {
                                    loggerConsole.Info("Index Metrics for Backends ({0} entities, {1} timeranges)", backendsList.Count, jobConfiguration.Input.HourlyTimeRanges.Count);

                                    #region Process Full Range Metrics

                                    if (File.Exists(FilePathMap.EntitiesFullIndexFilePath(jobTarget, EntityBackend.ENTITY_FOLDER)) == false)
                                    {
                                        // Prepare copies of entities indexed for fast access by their entity ID
                                        Dictionary<long, EntityBase> entitiesFullDictionary = backendsList.ToDictionary(e => e.EntityID, e => (EntityBase)(e.Clone()));

                                        readRolledUpRangeOfMetricsIntoEntities(entitiesFullDictionary, null, jobConfiguration.Input.TimeRange, jobTarget, entityMetricExtractMappingList, EntityBackend.ENTITY_FOLDER, EntityBackend.ENTITY_TYPE);

                                        foreach (EntityBase entity in entitiesFullDictionary.Values)
                                        {
                                            updateEntityWithDeeplinks(entity, jobConfiguration.Input.TimeRange);
                                            updateEntityRowWithDurationAndActivityStatus(entity, jobConfiguration.Input.TimeRange);
                                            entity.ARTRange = getDurationRangeAsString(entity.ART);
                                        }

                                        // Sort them
                                        List<EntityBackend> backendsFullList = entitiesFullDictionary.Values.OfType<EntityBackend>().ToList().OrderBy(o => o.BackendType).OrderBy(o => o.BackendName).ThenBy(o => o.From).ToList();

                                        FileIOHelper.WriteListToCSVFile(backendsFullList, new BackendMetricReportMap(), FilePathMap.EntitiesFullIndexFilePath(jobTarget, EntityBackend.ENTITY_FOLDER));
                                    }

                                    #endregion

                                    #region Process Hourly Ranges Metrics

                                    if (File.Exists(FilePathMap.EntitiesHourIndexFilePath(jobTarget, EntityBackend.ENTITY_FOLDER)) == false)
                                    {
                                        List<EntityBackend> backendsHourlyAllList = new List<EntityBackend>(backendsList.Count * jobConfiguration.Input.HourlyTimeRanges.Count);

                                        Dictionary<string, List<MetricValue>> metricValuesDictionary = new Dictionary<string, List<MetricValue>>();

                                        for (int j = 0; j < jobConfiguration.Input.HourlyTimeRanges.Count; j++)
                                        {
                                            JobTimeRange jobTimeRange = jobConfiguration.Input.HourlyTimeRanges[j];

                                            // Prepare copies of entities indexed for fast access by their entity ID
                                            Dictionary<long, EntityBase> entitiesHourlyDictionary = backendsList.ToDictionary(e => e.EntityID, e => (EntityBase)(e.Clone()));

                                            readGranularRangeOfMetricsIntoEntities(entitiesHourlyDictionary, null, jobTimeRange, jobTarget, entityMetricExtractMappingList, EntityBackend.ENTITY_FOLDER, EntityBackend.ENTITY_TYPE, metricValuesDictionary);

                                            foreach (EntityBase entity in entitiesHourlyDictionary.Values)
                                            {
                                                updateEntityWithDeeplinks(entity, jobTimeRange);
                                                updateEntityRowWithDurationAndActivityStatus(entity, jobTimeRange);
                                                entity.ARTRange = getDurationRangeAsString(entity.ART);
                                            }

                                            List<EntityBackend> backendsHourlyList = entitiesHourlyDictionary.Values.OfType<EntityBackend>().ToList();
                                            backendsHourlyAllList.AddRange(backendsHourlyList);
                                        }

                                        // Sort them
                                        backendsHourlyAllList = backendsHourlyAllList.OrderBy(o => o.BackendType).OrderBy(o => o.BackendName).ThenBy(o => o.From).ToList();

                                        FileIOHelper.WriteListToCSVFile(backendsHourlyAllList, new BackendMetricReportMap(), FilePathMap.EntitiesHourIndexFilePath(jobTarget, EntityBackend.ENTITY_FOLDER));

                                        // Save individual metric files and create index of their internal structure
                                        List<EntityHourlyMetricValueLocation> entityMetricValuesLocations = new List<EntityHourlyMetricValueLocation>(metricValuesDictionary.Count * backendsList.Count * jobConfiguration.Input.HourlyTimeRanges.Count);
                                        foreach (KeyValuePair<string, List<MetricValue>> metricValuesListContainer in metricValuesDictionary)
                                        {
                                            if (metricValuesListContainer.Value.Count > 0)
                                            {
                                                List<MetricValue> metricValuesSorted = metricValuesListContainer.Value.OrderBy(o => o.EntityID).ThenBy(o => o.MetricID).ThenBy(o => o.EventTimeStampUtc).ToList();

                                                FileIOHelper.WriteListToCSVFile(metricValuesSorted, new MetricValueReportMap(), FilePathMap.MetricValuesIndexFilePath(jobTarget, EntityBackend.ENTITY_FOLDER, metricValuesListContainer.Key));
                                                FileIOHelper.WriteListToCSVFile(metricValuesSorted, new MetricValueReportMap(), FilePathMap.MetricReportPerAppFilePath(jobTarget, EntityBackend.ENTITY_FOLDER, metricValuesListContainer.Key));

                                                List<EntityHourlyMetricValueLocation> entityMetricValuesLocationsForSingleMetric = getEntityHourlyMetricValueLocationsInTable(metricValuesSorted, jobConfiguration.Input.HourlyTimeRanges);
                                                if (entityMetricValuesLocationsForSingleMetric != null)
                                                {
                                                    entityMetricValuesLocations.AddRange(entityMetricValuesLocationsForSingleMetric);
                                                }
                                            }
                                        }

                                        // Save entity and metric index lookup
                                        FileIOHelper.WriteListToCSVFile(entityMetricValuesLocations, new EntityHourlyMetricValueLocationReportMap(), FilePathMap.MetricsLocationIndexFilePath(jobTarget, EntityBackend.ENTITY_FOLDER));
                                    }

                                    #endregion

                                    loggerConsole.Info("Completed {0} Backends", backendsList.Count);

                                    Interlocked.Add(ref numEntitiesTotal, backendsList.Count);
                                }

                                #endregion
                            },
                            () =>
                            {
                                #region Business Transactions

                                List<EntityBusinessTransaction> businessTransactionsList = FileIOHelper.ReadListFromCSVFile<EntityBusinessTransaction>(FilePathMap.BusinessTransactionsIndexFilePath(jobTarget), new BusinessTransactionEntityReportMap());
                                if (businessTransactionsList != null)
                                {
                                    loggerConsole.Info("Index Metrics for Business Transactions ({0} entities, {1} timeranges)", businessTransactionsList.Count, jobConfiguration.Input.HourlyTimeRanges.Count);

                                    #region Process Full Range Metrics

                                    if (File.Exists(FilePathMap.EntitiesFullIndexFilePath(jobTarget, EntityBusinessTransaction.ENTITY_FOLDER)) == false)
                                    {
                                        // Prepare copies of entities indexed for fast access by their entity ID
                                        Dictionary<long, EntityBase> entitiesFullDictionary = businessTransactionsList.ToDictionary(e => e.EntityID, e => (EntityBase)(e.Clone()));

                                        readRolledUpRangeOfMetricsIntoEntities(entitiesFullDictionary, null, jobConfiguration.Input.TimeRange, jobTarget, entityMetricExtractMappingList, EntityBusinessTransaction.ENTITY_FOLDER, EntityBusinessTransaction.ENTITY_TYPE);

                                        foreach (EntityBase entity in entitiesFullDictionary.Values)
                                        {
                                            updateEntityWithDeeplinks(entity, jobConfiguration.Input.TimeRange);
                                            updateEntityRowWithDurationAndActivityStatus(entity, jobConfiguration.Input.TimeRange);
                                            entity.ARTRange = getDurationRangeAsString(entity.ART);
                                        }

                                        // Sort them
                                        List<EntityBusinessTransaction> businessTransactionsFullList = entitiesFullDictionary.Values.OfType<EntityBusinessTransaction>().ToList().OrderBy(o => o.TierName).OrderBy(o => o.BTName).ThenBy(o => o.From).ToList();

                                        FileIOHelper.WriteListToCSVFile(businessTransactionsFullList, new BusinessTransactionMetricReportMap(), FilePathMap.EntitiesFullIndexFilePath(jobTarget, EntityBusinessTransaction.ENTITY_FOLDER));
                                    }

                                    #endregion

                                    #region Process Hourly Ranges Metrics

                                    if (File.Exists(FilePathMap.EntitiesHourIndexFilePath(jobTarget, EntityBusinessTransaction.ENTITY_FOLDER)) == false)
                                    {
                                        List<EntityBusinessTransaction> businessTransactionsHourlyAllList = new List<EntityBusinessTransaction>(businessTransactionsList.Count * jobConfiguration.Input.HourlyTimeRanges.Count);

                                        Dictionary<string, List<MetricValue>> metricValuesDictionary = new Dictionary<string, List<MetricValue>>();

                                        for (int j = 0; j < jobConfiguration.Input.HourlyTimeRanges.Count; j++)
                                        {
                                            JobTimeRange jobTimeRange = jobConfiguration.Input.HourlyTimeRanges[j];

                                            // Prepare copies of entities indexed for fast access by their entity ID
                                            Dictionary<long, EntityBase> entitiesHourlyDictionary = businessTransactionsList.ToDictionary(e => e.EntityID, e => (EntityBase)(e.Clone()));

                                            readGranularRangeOfMetricsIntoEntities(entitiesHourlyDictionary, null, jobTimeRange, jobTarget, entityMetricExtractMappingList, EntityBusinessTransaction.ENTITY_FOLDER, EntityBusinessTransaction.ENTITY_TYPE, metricValuesDictionary);

                                            foreach (EntityBase entity in entitiesHourlyDictionary.Values)
                                            {
                                                updateEntityWithDeeplinks(entity, jobTimeRange);
                                                updateEntityRowWithDurationAndActivityStatus(entity, jobTimeRange);
                                                entity.ARTRange = getDurationRangeAsString(entity.ART);
                                            }

                                            List<EntityBusinessTransaction> businessTransactionsHourlyList = entitiesHourlyDictionary.Values.OfType<EntityBusinessTransaction>().ToList();
                                            businessTransactionsHourlyAllList.AddRange(businessTransactionsHourlyList);
                                        }

                                        // Sort them
                                        businessTransactionsHourlyAllList = businessTransactionsHourlyAllList.OrderBy(o => o.TierName).OrderBy(o => o.BTName).ThenBy(o => o.From).ToList();

                                        FileIOHelper.WriteListToCSVFile(businessTransactionsHourlyAllList, new BusinessTransactionMetricReportMap(), FilePathMap.EntitiesHourIndexFilePath(jobTarget, EntityBusinessTransaction.ENTITY_FOLDER));

                                        // Save individual metric files and create index of their internal structure
                                        List<EntityHourlyMetricValueLocation> entityMetricValuesLocations = new List<EntityHourlyMetricValueLocation>(metricValuesDictionary.Count * businessTransactionsList.Count * jobConfiguration.Input.HourlyTimeRanges.Count);
                                        foreach (KeyValuePair<string, List<MetricValue>> metricValuesListContainer in metricValuesDictionary)
                                        {
                                            if (metricValuesListContainer.Value.Count > 0)
                                            {
                                                List<MetricValue> metricValuesSorted = metricValuesListContainer.Value.OrderBy(o => o.EntityID).ThenBy(o => o.MetricID).ThenBy(o => o.EventTimeStampUtc).ToList();

                                                FileIOHelper.WriteListToCSVFile(metricValuesSorted, new MetricValueReportMap(), FilePathMap.MetricValuesIndexFilePath(jobTarget, EntityBusinessTransaction.ENTITY_FOLDER, metricValuesListContainer.Key));
                                                FileIOHelper.WriteListToCSVFile(metricValuesSorted, new MetricValueReportMap(), FilePathMap.MetricReportPerAppFilePath(jobTarget, EntityBusinessTransaction.ENTITY_FOLDER, metricValuesListContainer.Key));

                                                List<EntityHourlyMetricValueLocation> entityMetricValuesLocationsForSingleMetric = getEntityHourlyMetricValueLocationsInTable(metricValuesSorted, jobConfiguration.Input.HourlyTimeRanges);
                                                if (entityMetricValuesLocationsForSingleMetric != null)
                                                {
                                                    entityMetricValuesLocations.AddRange(entityMetricValuesLocationsForSingleMetric);
                                                }
                                            }
                                        }

                                        // Save entity and metric index lookup
                                        FileIOHelper.WriteListToCSVFile(entityMetricValuesLocations, new EntityHourlyMetricValueLocationReportMap(), FilePathMap.MetricsLocationIndexFilePath(jobTarget, EntityBusinessTransaction.ENTITY_FOLDER));
                                    }

                                    #endregion

                                    loggerConsole.Info("Completed {0} Business Transactions", businessTransactionsList.Count);

                                    Interlocked.Add(ref numEntitiesTotal, businessTransactionsList.Count);
                                }

                                #endregion
                            },
                            () =>
                            {
                                #region Service Endpoints

                                List<EntityServiceEndpoint> serviceEndpointsList = FileIOHelper.ReadListFromCSVFile<EntityServiceEndpoint>(FilePathMap.ServiceEndpointsIndexFilePath(jobTarget), new ServiceEndpointEntityReportMap());
                                if (serviceEndpointsList != null)
                                {
                                    loggerConsole.Info("Index Metrics for Service Endpoints ({0} entities, {1} timeranges)", serviceEndpointsList.Count, jobConfiguration.Input.HourlyTimeRanges.Count);

                                    #region Process Full Range Metrics

                                    if (File.Exists(FilePathMap.EntitiesFullIndexFilePath(jobTarget, EntityServiceEndpoint.ENTITY_FOLDER)) == false)
                                    {
                                        // Prepare copies of entities indexed for fast access by their entity ID
                                        Dictionary<long, EntityBase> entitiesFullDictionary = serviceEndpointsList.ToDictionary(e => e.EntityID, e => (EntityBase)(e.Clone()));

                                        readRolledUpRangeOfMetricsIntoEntities(entitiesFullDictionary, null, jobConfiguration.Input.TimeRange, jobTarget, entityMetricExtractMappingList, EntityServiceEndpoint.ENTITY_FOLDER, EntityServiceEndpoint.ENTITY_TYPE);

                                        foreach (EntityBase entity in entitiesFullDictionary.Values)
                                        {
                                            updateEntityWithDeeplinks(entity, jobConfiguration.Input.TimeRange);
                                            updateEntityRowWithDurationAndActivityStatus(entity, jobConfiguration.Input.TimeRange);
                                            entity.ARTRange = getDurationRangeAsString(entity.ART);
                                        }

                                        // Sort them
                                        List<EntityServiceEndpoint> serviceEndpointsFullList = entitiesFullDictionary.Values.OfType<EntityServiceEndpoint>().ToList().OrderBy(o => o.TierName).OrderBy(o => o.SEPName).ThenBy(o => o.From).ToList();

                                        FileIOHelper.WriteListToCSVFile(serviceEndpointsFullList, new ServiceEndpointMetricReportMap(), FilePathMap.EntitiesFullIndexFilePath(jobTarget, EntityServiceEndpoint.ENTITY_FOLDER));
                                    }

                                    #endregion

                                    #region Process Hourly Ranges Metrics

                                    if (File.Exists(FilePathMap.EntitiesHourIndexFilePath(jobTarget, EntityServiceEndpoint.ENTITY_FOLDER)) == false)
                                    {
                                        List<EntityServiceEndpoint> serviceEndpointsHourlyAllList = new List<EntityServiceEndpoint>(serviceEndpointsList.Count * jobConfiguration.Input.HourlyTimeRanges.Count);

                                        Dictionary<string, List<MetricValue>> metricValuesDictionary = new Dictionary<string, List<MetricValue>>();

                                        for (int j = 0; j < jobConfiguration.Input.HourlyTimeRanges.Count; j++)
                                        {
                                            JobTimeRange jobTimeRange = jobConfiguration.Input.HourlyTimeRanges[j];

                                            // Prepare copies of entities indexed for fast access by their entity ID
                                            Dictionary<long, EntityBase> entitiesHourlyDictionary = serviceEndpointsList.ToDictionary(e => e.EntityID, e => (EntityBase)(e.Clone()));

                                            readGranularRangeOfMetricsIntoEntities(entitiesHourlyDictionary, null, jobTimeRange, jobTarget, entityMetricExtractMappingList, EntityServiceEndpoint.ENTITY_FOLDER, EntityServiceEndpoint.ENTITY_TYPE, metricValuesDictionary);

                                            foreach (EntityBase entity in entitiesHourlyDictionary.Values)
                                            {
                                                updateEntityWithDeeplinks(entity, jobTimeRange);
                                                updateEntityRowWithDurationAndActivityStatus(entity, jobTimeRange);
                                                entity.ARTRange = getDurationRangeAsString(entity.ART);
                                            }

                                            List<EntityServiceEndpoint> serviceEndpointsHourlyList = entitiesHourlyDictionary.Values.OfType<EntityServiceEndpoint>().ToList();
                                            serviceEndpointsHourlyAllList.AddRange(serviceEndpointsHourlyList);
                                        }

                                        // Sort them
                                        serviceEndpointsHourlyAllList = serviceEndpointsHourlyAllList.OrderBy(o => o.TierName).OrderBy(o => o.SEPName).ThenBy(o => o.From).ToList();

                                        FileIOHelper.WriteListToCSVFile(serviceEndpointsHourlyAllList, new ServiceEndpointMetricReportMap(), FilePathMap.EntitiesHourIndexFilePath(jobTarget, EntityServiceEndpoint.ENTITY_FOLDER));

                                        // Save individual metric files and create index of their internal structure
                                        List<EntityHourlyMetricValueLocation> entityMetricValuesLocations = new List<EntityHourlyMetricValueLocation>(metricValuesDictionary.Count * serviceEndpointsList.Count * jobConfiguration.Input.HourlyTimeRanges.Count);
                                        foreach (KeyValuePair<string, List<MetricValue>> metricValuesListContainer in metricValuesDictionary)
                                        {
                                            if (metricValuesListContainer.Value.Count > 0)
                                            {
                                                List<MetricValue> metricValuesSorted = metricValuesListContainer.Value.OrderBy(o => o.EntityID).ThenBy(o => o.MetricID).ThenBy(o => o.EventTimeStampUtc).ToList();

                                                FileIOHelper.WriteListToCSVFile(metricValuesSorted, new MetricValueReportMap(), FilePathMap.MetricValuesIndexFilePath(jobTarget, EntityServiceEndpoint.ENTITY_FOLDER, metricValuesListContainer.Key));
                                                FileIOHelper.WriteListToCSVFile(metricValuesSorted, new MetricValueReportMap(), FilePathMap.MetricReportPerAppFilePath(jobTarget, EntityServiceEndpoint.ENTITY_FOLDER, metricValuesListContainer.Key));

                                                List<EntityHourlyMetricValueLocation> entityMetricValuesLocationsForSingleMetric = getEntityHourlyMetricValueLocationsInTable(metricValuesSorted, jobConfiguration.Input.HourlyTimeRanges);
                                                if (entityMetricValuesLocationsForSingleMetric != null)
                                                {
                                                    entityMetricValuesLocations.AddRange(entityMetricValuesLocationsForSingleMetric);
                                                }
                                            }
                                        }

                                        // Save entity and metric index lookup
                                        FileIOHelper.WriteListToCSVFile(entityMetricValuesLocations, new EntityHourlyMetricValueLocationReportMap(), FilePathMap.MetricsLocationIndexFilePath(jobTarget, EntityServiceEndpoint.ENTITY_FOLDER));
                                    }

                                    #endregion

                                    loggerConsole.Info("Completed {0} Service Endpoints", serviceEndpointsList.Count);

                                    Interlocked.Add(ref numEntitiesTotal, serviceEndpointsList.Count);
                                }

                                #endregion
                            },
                            () =>
                            {
                                #region Errors

                                List<EntityError> errorsList = FileIOHelper.ReadListFromCSVFile<EntityError>(FilePathMap.ErrorsIndexFilePath(jobTarget), new ErrorEntityReportMap());
                                if (errorsList != null)
                                {
                                    loggerConsole.Info("Index Metrics for Errors ({0} entities, {1} timeranges)", errorsList.Count, jobConfiguration.Input.HourlyTimeRanges.Count);

                                    #region Process Full Range Metrics

                                    if (File.Exists(FilePathMap.EntitiesFullIndexFilePath(jobTarget, EntityError.ENTITY_FOLDER)) == false)
                                    {
                                        // Prepare copies of entities indexed for fast access by their entity ID
                                        Dictionary<long, EntityBase> entitiesFullDictionary = errorsList.ToDictionary(e => e.EntityID, e => (EntityBase)(e.Clone()));

                                        readRolledUpRangeOfMetricsIntoEntities(entitiesFullDictionary, null, jobConfiguration.Input.TimeRange, jobTarget, entityMetricExtractMappingList, EntityError.ENTITY_FOLDER, EntityError.ENTITY_TYPE);

                                        foreach (EntityBase entity in entitiesFullDictionary.Values)
                                        {
                                            updateEntityWithDeeplinks(entity, jobConfiguration.Input.TimeRange);
                                            updateEntityRowWithDurationAndActivityStatus(entity, jobConfiguration.Input.TimeRange);
                                            entity.ARTRange = getDurationRangeAsString(entity.ART);
                                        }

                                        // Sort them
                                        List<EntityError> errorsFullList = entitiesFullDictionary.Values.OfType<EntityError>().ToList().OrderBy(o => o.TierName).ThenBy(o => o.ErrorName).ThenBy(o => o.From).ToList();

                                        FileIOHelper.WriteListToCSVFile(errorsFullList, new ErrorMetricReportMap(), FilePathMap.EntitiesFullIndexFilePath(jobTarget, EntityError.ENTITY_FOLDER));
                                    }

                                    #endregion

                                    #region Process Hourly Ranges Metrics

                                    if (File.Exists(FilePathMap.EntitiesHourIndexFilePath(jobTarget, EntityError.ENTITY_FOLDER)) == false)
                                    {
                                        List<EntityError> errorsHourlyAllList = new List<EntityError>(errorsList.Count * jobConfiguration.Input.HourlyTimeRanges.Count);

                                        Dictionary<string, List<MetricValue>> metricValuesDictionary = new Dictionary<string, List<MetricValue>>();

                                        for (int j = 0; j < jobConfiguration.Input.HourlyTimeRanges.Count; j++)
                                        {
                                            JobTimeRange jobTimeRange = jobConfiguration.Input.HourlyTimeRanges[j];

                                            // Prepare copies of entities indexed for fast access by their entity ID
                                            Dictionary<long, EntityBase> entitiesHourlyDictionary = errorsList.ToDictionary(e => e.EntityID, e => (EntityBase)(e.Clone()));

                                            readGranularRangeOfMetricsIntoEntities(entitiesHourlyDictionary, null, jobTimeRange, jobTarget, entityMetricExtractMappingList, EntityError.ENTITY_FOLDER, EntityError.ENTITY_TYPE, metricValuesDictionary);

                                            foreach (EntityBase entity in entitiesHourlyDictionary.Values)
                                            {
                                                updateEntityWithDeeplinks(entity, jobTimeRange);
                                                updateEntityRowWithDurationAndActivityStatus(entity, jobTimeRange);
                                                entity.ARTRange = getDurationRangeAsString(entity.ART);
                                            }

                                            List<EntityError> errorsHourlyList = entitiesHourlyDictionary.Values.OfType<EntityError>().ToList();
                                            errorsHourlyAllList.AddRange(errorsHourlyList);
                                        }

                                        // Sort them
                                        errorsHourlyAllList = errorsHourlyAllList.OrderBy(o => o.TierName).ThenBy(o => o.ErrorName).ThenBy(o => o.From).ToList();

                                        FileIOHelper.WriteListToCSVFile(errorsHourlyAllList, new ErrorMetricReportMap(), FilePathMap.EntitiesHourIndexFilePath(jobTarget, EntityError.ENTITY_FOLDER));

                                        // Save individual metric files and create index of their internal structure
                                        List<EntityHourlyMetricValueLocation> entityMetricValuesLocations = new List<EntityHourlyMetricValueLocation>(metricValuesDictionary.Count * errorsList.Count * jobConfiguration.Input.HourlyTimeRanges.Count);
                                        foreach (KeyValuePair<string, List<MetricValue>> metricValuesListContainer in metricValuesDictionary)
                                        {
                                            if (metricValuesListContainer.Value.Count > 0)
                                            {
                                                List<MetricValue> metricValuesSorted = metricValuesListContainer.Value.OrderBy(o => o.EntityID).ThenBy(o => o.MetricID).ThenBy(o => o.EventTimeStampUtc).ToList();

                                                FileIOHelper.WriteListToCSVFile(metricValuesSorted, new MetricValueReportMap(), FilePathMap.MetricValuesIndexFilePath(jobTarget, EntityError.ENTITY_FOLDER, metricValuesListContainer.Key));
                                                FileIOHelper.WriteListToCSVFile(metricValuesSorted, new MetricValueReportMap(), FilePathMap.MetricReportPerAppFilePath(jobTarget, EntityError.ENTITY_FOLDER, metricValuesListContainer.Key));

                                                List<EntityHourlyMetricValueLocation> entityMetricValuesLocationsForSingleMetric = getEntityHourlyMetricValueLocationsInTable(metricValuesSorted, jobConfiguration.Input.HourlyTimeRanges);
                                                if (entityMetricValuesLocationsForSingleMetric != null)
                                                {
                                                    entityMetricValuesLocations.AddRange(entityMetricValuesLocationsForSingleMetric);
                                                }
                                            }
                                        }

                                        // Save entity and metric index lookup
                                        FileIOHelper.WriteListToCSVFile(entityMetricValuesLocations, new EntityHourlyMetricValueLocationReportMap(), FilePathMap.MetricsLocationIndexFilePath(jobTarget, EntityError.ENTITY_FOLDER));
                                    }

                                    #endregion

                                    loggerConsole.Info("Completed {0} Errors", errorsList.Count);

                                    Interlocked.Add(ref numEntitiesTotal, errorsList.Count);
                                }

                                #endregion
                            },
                            () =>
                            {
                                #region Information Points

                                List<EntityInformationPoint> informationPointsList = FileIOHelper.ReadListFromCSVFile<EntityInformationPoint>(FilePathMap.InformationPointsIndexFilePath(jobTarget), new InformationPointEntityReportMap());
                                if (informationPointsList != null)
                                {
                                    loggerConsole.Info("Index Metrics for Information Points ({0} entities, {1} timeranges)", informationPointsList.Count, jobConfiguration.Input.HourlyTimeRanges.Count);

                                    #region Process Full Range Metrics

                                    if (File.Exists(FilePathMap.EntitiesFullIndexFilePath(jobTarget, EntityInformationPoint.ENTITY_FOLDER)) == false)
                                    {
                                        // Prepare copies of entities indexed for fast access by their entity ID
                                        Dictionary<long, EntityBase> entitiesFullDictionary = informationPointsList.ToDictionary(e => e.EntityID, e => (EntityBase)(e.Clone()));

                                        readRolledUpRangeOfMetricsIntoEntities(entitiesFullDictionary, null, jobConfiguration.Input.TimeRange, jobTarget, entityMetricExtractMappingList, EntityInformationPoint.ENTITY_FOLDER, EntityInformationPoint.ENTITY_TYPE);

                                        foreach (EntityBase entity in entitiesFullDictionary.Values)
                                        {
                                            updateEntityWithDeeplinks(entity, jobConfiguration.Input.TimeRange);
                                            updateEntityRowWithDurationAndActivityStatus(entity, jobConfiguration.Input.TimeRange);
                                            entity.ARTRange = getDurationRangeAsString(entity.ART);
                                        }

                                        // Sort them
                                        List<EntityInformationPoint> informationPointsFullList = entitiesFullDictionary.Values.OfType<EntityInformationPoint>().ToList().OrderBy(o => o.IPName).ThenBy(o => o.From).ToList();

                                        FileIOHelper.WriteListToCSVFile(informationPointsFullList, new InformationPointMetricReportMap(), FilePathMap.EntitiesFullIndexFilePath(jobTarget, EntityInformationPoint.ENTITY_FOLDER));
                                    }

                                    #endregion

                                    #region Process Hourly Ranges Metrics

                                    if (File.Exists(FilePathMap.EntitiesHourIndexFilePath(jobTarget, EntityInformationPoint.ENTITY_FOLDER)) == false)
                                    {
                                        List<EntityInformationPoint> informationPointsHourlyAllList = new List<EntityInformationPoint>(informationPointsList.Count * jobConfiguration.Input.HourlyTimeRanges.Count);

                                        Dictionary<string, List<MetricValue>> metricValuesDictionary = new Dictionary<string, List<MetricValue>>();

                                        for (int j = 0; j < jobConfiguration.Input.HourlyTimeRanges.Count; j++)
                                        {
                                            JobTimeRange jobTimeRange = jobConfiguration.Input.HourlyTimeRanges[j];

                                            // Prepare copies of entities indexed for fast access by their entity ID
                                            Dictionary<long, EntityBase> entitiesHourlyDictionary = informationPointsList.ToDictionary(e => e.EntityID, e => (EntityBase)(e.Clone()));

                                            readGranularRangeOfMetricsIntoEntities(entitiesHourlyDictionary, null, jobTimeRange, jobTarget, entityMetricExtractMappingList, EntityInformationPoint.ENTITY_FOLDER, EntityInformationPoint.ENTITY_TYPE, metricValuesDictionary);

                                            foreach (EntityBase entity in entitiesHourlyDictionary.Values)
                                            {
                                                updateEntityWithDeeplinks(entity, jobTimeRange);
                                                updateEntityRowWithDurationAndActivityStatus(entity, jobTimeRange);
                                                entity.ARTRange = getDurationRangeAsString(entity.ART);
                                            }

                                            List<EntityInformationPoint> informationPointsHourlyList = entitiesHourlyDictionary.Values.OfType<EntityInformationPoint>().ToList();
                                            informationPointsHourlyAllList.AddRange(informationPointsHourlyList);
                                        }

                                        // Sort them
                                        informationPointsHourlyAllList = informationPointsHourlyAllList.OrderBy(o => o.IPName).ThenBy(o => o.From).ToList();

                                        FileIOHelper.WriteListToCSVFile(informationPointsHourlyAllList, new InformationPointMetricReportMap(), FilePathMap.EntitiesHourIndexFilePath(jobTarget, EntityInformationPoint.ENTITY_FOLDER));

                                        // Save individual metric files and create index of their internal structure
                                        List<EntityHourlyMetricValueLocation> entityMetricValuesLocations = new List<EntityHourlyMetricValueLocation>(metricValuesDictionary.Count * informationPointsList.Count * jobConfiguration.Input.HourlyTimeRanges.Count);
                                        foreach (KeyValuePair<string, List<MetricValue>> metricValuesListContainer in metricValuesDictionary)
                                        {
                                            if (metricValuesListContainer.Value.Count > 0)
                                            {
                                                List<MetricValue> metricValuesSorted = metricValuesListContainer.Value.OrderBy(o => o.EntityID).ThenBy(o => o.MetricID).ThenBy(o => o.EventTimeStampUtc).ToList();

                                                FileIOHelper.WriteListToCSVFile(metricValuesSorted, new MetricValueReportMap(), FilePathMap.MetricValuesIndexFilePath(jobTarget, EntityInformationPoint.ENTITY_FOLDER, metricValuesListContainer.Key));
                                                FileIOHelper.WriteListToCSVFile(metricValuesSorted, new MetricValueReportMap(), FilePathMap.MetricReportPerAppFilePath(jobTarget, EntityInformationPoint.ENTITY_FOLDER, metricValuesListContainer.Key));

                                                List<EntityHourlyMetricValueLocation> entityMetricValuesLocationsForSingleMetric = getEntityHourlyMetricValueLocationsInTable(metricValuesSorted, jobConfiguration.Input.HourlyTimeRanges);
                                                if (entityMetricValuesLocationsForSingleMetric != null)
                                                {
                                                    entityMetricValuesLocations.AddRange(entityMetricValuesLocationsForSingleMetric);
                                                }
                                            }
                                        }

                                        // Save entity and metric index lookup
                                        FileIOHelper.WriteListToCSVFile(entityMetricValuesLocations, new EntityHourlyMetricValueLocationReportMap(), FilePathMap.MetricsLocationIndexFilePath(jobTarget, EntityInformationPoint.ENTITY_FOLDER));
                                    }

                                    #endregion

                                    loggerConsole.Info("Completed {0} Information Points", informationPointsList.Count);

                                    Interlocked.Add(ref numEntitiesTotal, informationPointsList.Count);
                                }

                                #endregion
                            }
                        );

                        stepTimingTarget.NumEntities = numEntitiesTotal;

                        #region Combine All for Report CSV

                        // If it is the first one, clear out the combined folder
                        if (i == 0)
                        {
                            FileIOHelper.DeleteFolder(FilePathMap.MetricsReportFolderPath());
                            Thread.Sleep(1000);
                            FileIOHelper.CreateFolder(FilePathMap.MetricsReportFolderPath());
                        }

                        // Append all the individual application files into one
                        FileIOHelper.AppendTwoCSVFiles(FilePathMap.EntitiesFullReportFilePath(EntityApplication.ENTITY_FOLDER), FilePathMap.EntitiesFullIndexFilePath(jobTarget, EntityApplication.ENTITY_FOLDER));
                        FileIOHelper.AppendTwoCSVFiles(FilePathMap.EntitiesHourReportFilePath(EntityApplication.ENTITY_FOLDER), FilePathMap.EntitiesHourIndexFilePath(jobTarget, EntityApplication.ENTITY_FOLDER));
                        FileIOHelper.AppendTwoCSVFiles(FilePathMap.EntitiesFullReportFilePath(EntityTier.ENTITY_FOLDER), FilePathMap.EntitiesFullIndexFilePath(jobTarget, EntityTier.ENTITY_FOLDER));
                        FileIOHelper.AppendTwoCSVFiles(FilePathMap.EntitiesHourReportFilePath(EntityTier.ENTITY_FOLDER), FilePathMap.EntitiesHourIndexFilePath(jobTarget, EntityTier.ENTITY_FOLDER));
                        FileIOHelper.AppendTwoCSVFiles(FilePathMap.EntitiesFullReportFilePath(EntityNode.ENTITY_FOLDER), FilePathMap.EntitiesFullIndexFilePath(jobTarget, EntityNode.ENTITY_FOLDER));
                        FileIOHelper.AppendTwoCSVFiles(FilePathMap.EntitiesHourReportFilePath(EntityNode.ENTITY_FOLDER), FilePathMap.EntitiesHourIndexFilePath(jobTarget, EntityNode.ENTITY_FOLDER));
                        FileIOHelper.AppendTwoCSVFiles(FilePathMap.EntitiesFullReportFilePath(EntityBackend.ENTITY_FOLDER), FilePathMap.EntitiesFullIndexFilePath(jobTarget, EntityBackend.ENTITY_FOLDER));
                        FileIOHelper.AppendTwoCSVFiles(FilePathMap.EntitiesHourReportFilePath(EntityBackend.ENTITY_FOLDER), FilePathMap.EntitiesHourIndexFilePath(jobTarget, EntityBackend.ENTITY_FOLDER));
                        FileIOHelper.AppendTwoCSVFiles(FilePathMap.EntitiesFullReportFilePath(EntityBusinessTransaction.ENTITY_FOLDER), FilePathMap.EntitiesFullIndexFilePath(jobTarget, EntityBusinessTransaction.ENTITY_FOLDER));
                        FileIOHelper.AppendTwoCSVFiles(FilePathMap.EntitiesHourReportFilePath(EntityBusinessTransaction.ENTITY_FOLDER), FilePathMap.EntitiesHourIndexFilePath(jobTarget, EntityBusinessTransaction.ENTITY_FOLDER));
                        FileIOHelper.AppendTwoCSVFiles(FilePathMap.EntitiesFullReportFilePath(EntityServiceEndpoint.ENTITY_FOLDER), FilePathMap.EntitiesFullIndexFilePath(jobTarget, EntityServiceEndpoint.ENTITY_FOLDER));
                        FileIOHelper.AppendTwoCSVFiles(FilePathMap.EntitiesHourReportFilePath(EntityServiceEndpoint.ENTITY_FOLDER), FilePathMap.EntitiesHourIndexFilePath(jobTarget, EntityServiceEndpoint.ENTITY_FOLDER));
                        FileIOHelper.AppendTwoCSVFiles(FilePathMap.EntitiesFullReportFilePath(EntityError.ENTITY_FOLDER), FilePathMap.EntitiesFullIndexFilePath(jobTarget, EntityError.ENTITY_FOLDER));
                        FileIOHelper.AppendTwoCSVFiles(FilePathMap.EntitiesHourReportFilePath(EntityError.ENTITY_FOLDER), FilePathMap.EntitiesHourIndexFilePath(jobTarget, EntityError.ENTITY_FOLDER));
                        FileIOHelper.AppendTwoCSVFiles(FilePathMap.EntitiesFullReportFilePath(EntityInformationPoint.ENTITY_FOLDER), FilePathMap.EntitiesFullIndexFilePath(jobTarget, EntityInformationPoint.ENTITY_FOLDER));
                        FileIOHelper.AppendTwoCSVFiles(FilePathMap.EntitiesHourReportFilePath(EntityInformationPoint.ENTITY_FOLDER), FilePathMap.EntitiesHourIndexFilePath(jobTarget, EntityInformationPoint.ENTITY_FOLDER));

                        // Combine the generated detailed metric value files
                        foreach (MetricExtractMapping metricExtractMapping in entityMetricExtractMappingList)
                        {
                            switch (metricExtractMapping.EntityType)
                            {
                                case EntityApplication.ENTITY_TYPE:
                                    FileIOHelper.AppendTwoCSVFiles(
                                        FilePathMap.MetricReportFilePath(EntityApplication.ENTITY_FOLDER, metricExtractMapping.FolderName), 
                                        FilePathMap.MetricValuesIndexFilePath(jobTarget, EntityApplication.ENTITY_FOLDER, metricExtractMapping.FolderName));
                                    break;

                                case EntityTier.ENTITY_TYPE:
                                    FileIOHelper.AppendTwoCSVFiles(
                                        FilePathMap.MetricReportFilePath(EntityTier.ENTITY_FOLDER, metricExtractMapping.FolderName),
                                        FilePathMap.MetricValuesIndexFilePath(jobTarget, EntityTier.ENTITY_FOLDER, metricExtractMapping.FolderName));
                                    break;

                                case EntityNode.ENTITY_TYPE:
                                    FileIOHelper.AppendTwoCSVFiles(
                                        FilePathMap.MetricReportFilePath(EntityNode.ENTITY_FOLDER, metricExtractMapping.FolderName),
                                        FilePathMap.MetricValuesIndexFilePath(jobTarget, EntityNode.ENTITY_FOLDER, metricExtractMapping.FolderName));
                                    break;

                                case EntityBackend.ENTITY_TYPE:
                                    FileIOHelper.AppendTwoCSVFiles(
                                        FilePathMap.MetricReportFilePath(EntityBackend.ENTITY_FOLDER, metricExtractMapping.FolderName),
                                        FilePathMap.MetricValuesIndexFilePath(jobTarget, EntityBackend.ENTITY_FOLDER, metricExtractMapping.FolderName));
                                    break;

                                case EntityBusinessTransaction.ENTITY_TYPE:
                                    FileIOHelper.AppendTwoCSVFiles(
                                        FilePathMap.MetricReportFilePath(EntityBusinessTransaction.ENTITY_FOLDER, metricExtractMapping.FolderName),
                                        FilePathMap.MetricValuesIndexFilePath(jobTarget, EntityBusinessTransaction.ENTITY_FOLDER, metricExtractMapping.FolderName));
                                    break;

                                case EntityServiceEndpoint.ENTITY_TYPE:
                                    FileIOHelper.AppendTwoCSVFiles(
                                        FilePathMap.MetricReportFilePath(EntityServiceEndpoint.ENTITY_FOLDER, metricExtractMapping.FolderName),
                                        FilePathMap.MetricValuesIndexFilePath(jobTarget, EntityServiceEndpoint.ENTITY_FOLDER, metricExtractMapping.FolderName));
                                    break;

                                case EntityError.ENTITY_TYPE:
                                    FileIOHelper.AppendTwoCSVFiles(
                                        FilePathMap.MetricReportFilePath(EntityError.ENTITY_FOLDER, metricExtractMapping.FolderName),
                                        FilePathMap.MetricValuesIndexFilePath(jobTarget, EntityError.ENTITY_FOLDER, metricExtractMapping.FolderName));
                                    break;

                                case EntityInformationPoint.ENTITY_TYPE:
                                    FileIOHelper.AppendTwoCSVFiles(
                                        FilePathMap.MetricReportFilePath(EntityInformationPoint.ENTITY_FOLDER, metricExtractMapping.FolderName),
                                        FilePathMap.MetricValuesIndexFilePath(jobTarget, EntityInformationPoint.ENTITY_FOLDER, metricExtractMapping.FolderName));
                                    break;

                                default:
                                    break;
                            }
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
            logger.Trace("Input.Metrics={0}", jobConfiguration.Input.Metrics);
            loggerConsole.Trace("Input.Metrics={0}", jobConfiguration.Input.Metrics);
            if (jobConfiguration.Input.Metrics == false)
            {
                loggerConsole.Trace("Skipping index of entity metrics");
            }
            return (jobConfiguration.Input.Metrics == true);
        }

        private void readRolledUpRangeOfMetricsIntoEntities(
            Dictionary<long, EntityBase> entitiesDictionaryByID,
            Dictionary<string, EntityBase> entitiesDictionaryByName,
            JobTimeRange jobTimeRange,
            JobTarget jobTarget,
            List<MetricExtractMapping> entityMetricExtractMappingList,
            string entityFolderName,
            string entityType)
        {
            List<MetricExtractMapping> entityMetricExtractMappingListFiltered = entityMetricExtractMappingList.Where(m => m.EntityType == entityType).ToList();
            foreach (MetricExtractMapping metricExtractMapping in entityMetricExtractMappingListFiltered)
            {
                List<AppDRESTMetric> metricData = FileIOHelper.LoadListOfObjectsFromFile<AppDRESTMetric>(FilePathMap.MetricFullRangeDataFilePath(jobTarget, entityFolderName, metricExtractMapping.FolderName, jobTimeRange));
                if (metricData != null)
                {
                    readMetricsIntoEntities(metricData, entitiesDictionaryByID, entitiesDictionaryByName, jobTimeRange);
                }
            }
        }

        private void readGranularRangeOfMetricsIntoEntities(
            Dictionary<long, EntityBase> entitiesDictionaryByID,
            Dictionary<string, EntityBase> entitiesDictionaryByName,
            JobTimeRange jobTimeRange,
            JobTarget jobTarget,
            List<MetricExtractMapping> entityMetricExtractMappingList,
            string entityFolderName,
            string entityType,
            Dictionary<string, List<MetricValue>> metricValuesDictionary)
        {

            List<MetricExtractMapping> entityMetricExtractMappingListFiltered = entityMetricExtractMappingList.Where(m => m.EntityType == entityType).ToList();
            foreach (MetricExtractMapping metricExtractMapping in entityMetricExtractMappingListFiltered)
            {
                List<AppDRESTMetric> metricData = FileIOHelper.LoadListOfObjectsFromFile<AppDRESTMetric>(FilePathMap.MetricHourRangeDataFilePath(jobTarget, entityFolderName, metricExtractMapping.FolderName, jobTimeRange));
                if (metricData != null)
                {
                    List<MetricValue> metricValues = readMetricsIntoEntities(metricData, entitiesDictionaryByID, entitiesDictionaryByName, jobTimeRange);
                    if (metricValues != null)
                    {
                        if (metricValuesDictionary.ContainsKey(metricExtractMapping.FolderName) == false)
                        {
                            metricValuesDictionary.Add(metricExtractMapping.FolderName, metricValues);
                        }
                        else
                        {
                            metricValuesDictionary[metricExtractMapping.FolderName].AddRange(metricValues);
                        }
                    }
                }
            }

            return;
        }

        private List<MetricValue> readMetricsIntoEntities(
            List<AppDRESTMetric> metricData,
            Dictionary<long, EntityBase> entitiesDictionaryByID,
            Dictionary<string, EntityBase> entitiesDictionaryByName,
            JobTimeRange jobTimeRange)
        {
            EntityBase entity;

            int timerangeDuration = (int)(jobTimeRange.To - jobTimeRange.From).Duration().TotalMinutes;

            List<MetricValue> metricValues = new List<MetricValue>(metricData.Count * timerangeDuration);

            foreach (AppDRESTMetric appDRESTMetric in metricData)
            {
                if (appDRESTMetric.metricValues.Count == 0)
                {
                    // No metrics in this chunk
                    continue;
                }

                #region Get metric path components and metric name

                // Analyze metric path returned by the call to controller
                string[] metricPathComponents = appDRESTMetric.metricPath.Split('|');

                if (metricPathComponents.Length == 0)
                {
                    // Metric name was no good
                    logger.Warn("Metric path='{0}' could not be parsed into individual components", appDRESTMetric.metricPath);
                    continue;
                }

                string[] metricNameComponents = appDRESTMetric.metricName.Split('|');

                if (metricNameComponents.Length == 0)
                {
                    // Metric name was no good
                    logger.Warn("Metric name='{0}' could not be parsed into individual components", appDRESTMetric.metricName);
                    continue;
                }

                // Name of the metric is always the last one in the metric path
                string metricName = metricPathComponents[metricPathComponents.Length - 1];

                #endregion

                #region Determine metric entity type, scope and name from metric path

                if (String.Compare(metricPathComponents[0], "Overall Application Performance", true) == 0)
                {
                    #region Overall Application Performance - App, Tier, Node

                    switch (metricPathComponents.Length)
                    {
                        case 2:
                            // MetricPath = Overall Application Performance|Calls per Minute
                            // MetricName = BTM|Application Summary|Calls per Minute
                            // Application
                            {
                                entity = entitiesDictionaryByID.FirstOrDefault().Value;
                                List<MetricValue> metricValuesConverted = readMetricValuesIntoEntity(entity, metricName, appDRESTMetric, timerangeDuration);
                                metricValues.AddRange(metricValuesConverted);
                                break;
                            }
                        case 3:
                            // MetricPath = Overall Application Performance|ECommerce-Services|Calls per Minute
                            // MetricName = BTM|Application Summary|Component:184|Calls per Minute
                            // Tier
                            {
                                long tierID = -1;
                                if (metricNameComponents.Length >= 3)
                                {
                                    string[] tierIDComponents = metricNameComponents[2].Split(':');
                                    if (tierIDComponents.Length >= 2 && Int64.TryParse(tierIDComponents[1], out tierID) == true)
                                    {
                                        if (entitiesDictionaryByID.TryGetValue(tierID, out entity) == true)
                                        {
                                            List<MetricValue> metricValuesConverted = readMetricValuesIntoEntity(entity, metricName, appDRESTMetric, timerangeDuration);
                                            metricValues.AddRange(metricValuesConverted);
                                        }
                                    }
                                }
                                break;
                            }
                        case 5:
                            // Node in a Tier 
                            // or
                            // Backend exit from Tier
                            {
                                // MetricPath = Overall Application Performance|ECommerce-Services|Individual Nodes|ECommerce_WEB1_NODE|Calls per Minute
                                // MetricName = BTM|Application Summary|Component:184|Calls per Minute
                                // Node in a Tier
                                if (String.Compare(metricPathComponents[2], "Individual Nodes", true) == 0)
                                {
                                    if (metricPathComponents.Length >= 4 && entitiesDictionaryByName.TryGetValue(String.Format("{0}-{1}", metricPathComponents[1], metricPathComponents[3]), out entity) == true)
                                    {
                                        List<MetricValue> metricValuesConverted = readMetricValuesIntoEntity(entity, metricName, appDRESTMetric, timerangeDuration);
                                        metricValues.AddRange(metricValuesConverted);
                                    }
                                }
                                // MetricPath = Overall Application Performance|ECommerce-Services|External Calls|Call-HTTP to Discovered backend call - api.shipping.com|Calls per Minute
                                // MetricName = BTM|Application Summary|Component:184|Exit Call:HTTP|To:{[UNRESOLVED][824]}|Calls per Minute
                                // Backend exit from Tier
                                else if (String.Compare(metricPathComponents[2], "External Calls", true) == 0)
                                {
                                    long tierID = -1;
                                    if (metricNameComponents.Length >= 3)
                                    {
                                        string[] tierIDComponents = metricNameComponents[2].Split(':');
                                        if (tierIDComponents.Length >= 2 && Int64.TryParse(tierIDComponents[1], out tierID) == true)
                                        {
                                            if (entitiesDictionaryByID.TryGetValue(tierID, out entity) == true)
                                            {
                                                metricName = String.Join("|", metricPathComponents, 3, metricPathComponents.Length - 3);
                                                List<MetricValue> metricValuesConverted = readMetricValuesIntoEntity(entity, metricName, appDRESTMetric, timerangeDuration);
                                                metricValues.AddRange(metricValuesConverted);
                                            }
                                        }
                                    }
                                }
                                break;
                            }
                        case 7:
                            // MetricPath = Overall Application Performance|ECommerce-Services|Individual Nodes|ECommerce_WEB2_NODE|External Calls|Call-HTTP to Discovered backend call - api.shipping.com|Calls per Minute
                            // MetricName = BTM|Application Summary|Component:184|Exit Call:HTTP|To:{[UNRESOLVED][824]}|Calls per Minute
                            // Backend exit from Node
                            {
                                if (metricPathComponents.Length >= 4 && entitiesDictionaryByName.TryGetValue(String.Format("{0}-{1}", metricPathComponents[1], metricPathComponents[3]), out entity) == true)
                                {
                                    metricName = String.Join("|", metricPathComponents, 5, metricPathComponents.Length - 5);
                                    List<MetricValue> metricValuesConverted = readMetricValuesIntoEntity(entity, metricName, appDRESTMetric, timerangeDuration);
                                    metricValues.AddRange(metricValuesConverted);
                                }
                                break;
                            }
                        default:
                            {
                                logger.Warn("Metric path='{0}' is not of supported type of metric for processing", appDRESTMetric.metricPath);
                                break;
                            }
                    }

                    #endregion
                }
                else if (String.Compare(metricPathComponents[0], "Application Infrastructure Performance", true) == 0)
                {
                    #region Aplication Infrastructure Performance - Tier, Node

                    // MetricPath = Application Infrastructure Performance|ECommerce-Services|Individual Nodes|ECommerce_WEB1_NODE|Agent|App|Availability
                    // MetricName = Agent|App|Availability
                    // or
                    // MetricPath = Application Infrastructure Performance|ECommerce-Services|Individual Nodes|ECommerce_WEB1_NODE|Agent|App|Availability
                    // MetricName = Agent|App|Availability
                    // Node in a Tier
                    if (String.Compare(metricPathComponents[2], "Individual Nodes", true) == 0)
                    {
                        if (metricPathComponents.Length >= 4 && entitiesDictionaryByName.TryGetValue(String.Format("{0}-{1}", metricPathComponents[1], metricPathComponents[3]), out entity) == true)
                        {
                            metricName = String.Join("|", metricPathComponents, 4, metricPathComponents.Length - 4);
                            List<MetricValue> metricValuesConverted = readMetricValuesIntoEntity(entity, metricName, appDRESTMetric, timerangeDuration);
                            metricValues.AddRange(metricValuesConverted);
                        }
                    }
                    // MetricPath = Application Infrastructure Performance|ECommerce-Services|Hardware Resources|CPU|%Busy	
                    // MetricName = Hardware Resources|CPU|%Busy
                    // or
                    // MetricPath = Application Infrastructure Performance|ECommerce-Services|Agent|App|Availability
                    // MetricName = Agent|App|Availability
                    // Tier
                    else
                    {
                        if (metricPathComponents.Length >= 2 && entitiesDictionaryByName.TryGetValue(metricPathComponents[1], out entity) == true)
                        {
                            metricName = String.Join("|", metricPathComponents, 2, metricPathComponents.Length - 2);
                            List<MetricValue> metricValuesConverted = readMetricValuesIntoEntity(entity, metricName, appDRESTMetric, timerangeDuration);
                            metricValues.AddRange(metricValuesConverted);
                        }
                    }

                    #endregion
                }
                else if (String.Compare(metricPathComponents[0], "Business Transaction Performance", true) == 0)
                {
                    #region Business Transaction Performance

                    switch (metricPathComponents.Length)
                    {
                        case 5:
                            // MetricPath = Business Transaction Performance|Business Transactions|ECommerce-Services|Homepage|Calls per Minute
                            // MetricName = BTM|BTs|BT:1008|Component:184|Calls per Minute
                            // or
                            // MetricPath = Business Transaction Performance|Business Transactions|ECommerce-Services|Login - Mobile|95th Percentile Response Time (ms)
                            // MetricName = BTM|BTs|BT:557|Component:184|95th Percentile Response Time (ms)
                            // Business Transaction
                            {
                                long businessTransactionID = -1;
                                if (metricNameComponents.Length >= 3)
                                {
                                    string[] businessTransactionIDComponents = metricNameComponents[2].Split(':');
                                    if (businessTransactionIDComponents.Length >= 2 && Int64.TryParse(businessTransactionIDComponents[1], out businessTransactionID) == true)
                                    {
                                        if (entitiesDictionaryByID.TryGetValue(businessTransactionID, out entity) == true)
                                        {
                                            List<MetricValue> metricValuesConverted = readMetricValuesIntoEntity(entity, metricName, appDRESTMetric, timerangeDuration);
                                            metricValues.AddRange(metricValuesConverted);
                                        }
                                    }
                                }
                                break;
                            }
                        case 7:
                            // Business Transaction calls by Individual Nodes 
                            // or
                            // Business Transaction calls to Backends
                            {
                                // MetricPath = Business Transaction Performance|Business Transactions|ECommerce-Services|Homepage|Individual Nodes|ECommerce_WEB2_NODE|Calls per Minute
                                // MetricName = BTM|BTs|BT:1008|Component:184|Calls per Minute
                                // Business Transaction calls by Individual Nodes 
                                if (String.Compare(metricPathComponents[4], "Individual Nodes", true) == 0)
                                {
                                    long businessTransactionID = -1;
                                    if (metricNameComponents.Length >= 3)
                                    {
                                        string[] businessTransactionIDComponents = metricNameComponents[2].Split(':');
                                        if (businessTransactionIDComponents.Length >= 2 && Int64.TryParse(businessTransactionIDComponents[1], out businessTransactionID) == true)
                                        {
                                            if (entitiesDictionaryByID.TryGetValue(businessTransactionID, out entity) == true)
                                            {
                                                metricName = String.Join("|", metricPathComponents, 5, metricPathComponents.Length - 5);
                                                List<MetricValue> metricValuesConverted = readMetricValuesIntoEntity(entity, metricName, appDRESTMetric, timerangeDuration);
                                                metricValues.AddRange(metricValuesConverted);
                                            }
                                        }
                                    }
                                }
                                // MetricPath = Business Transaction Performance|Business Transactions|ECommerce-Services|Fetch Catalog|External Calls|Call-JDBC to Discovered backend call - XE-Oracle-ORACLE-DB-Oracle Database 11g Express Edition Release 11.2.0.2.0 - 64bit Production|Calls per Minute
                                // MetricName = BTM|BTs|BT:1010|Component:184|Exit Call:JDBC|To:{[UNRESOLVED][925]}|Calls per Minute
                                // Business Transaction calls to Backends
                                else if (String.Compare(metricPathComponents[4], "External Calls", true) == 0)
                                {
                                    long businessTransactionID = -1;
                                    if (metricNameComponents.Length >= 3)
                                    {
                                        string[] businessTransactionIDComponents = metricNameComponents[2].Split(':');
                                        if (businessTransactionIDComponents.Length >= 2 && Int64.TryParse(businessTransactionIDComponents[1], out businessTransactionID) == true)
                                        {
                                            if (entitiesDictionaryByID.TryGetValue(businessTransactionID, out entity) == true)
                                            {
                                                metricName = String.Join("|", metricPathComponents, 5, metricPathComponents.Length - 5);
                                                List<MetricValue> metricValuesConverted = readMetricValuesIntoEntity(entity, metricName, appDRESTMetric, timerangeDuration);
                                                metricValues.AddRange(metricValuesConverted);
                                            }
                                        }
                                    }
                                }
                                break;
                            }
                        case 9:
                            // MetricPath = Business Transaction Performance|Business Transactions|ECommerce-Services|Login - Mobile|Individual Nodes|ECommerce_WEB2_NODE|External Calls|Call-JDBC to Discovered backend call - APPDY-MySQL-DB-5.7.13-0ubuntu0.16.04.2|Calls per Minute
                            // MetricName = BTM|BTs|BT:557|Component:184|Exit Call:JDBC|To:{[UNRESOLVED][2348]}|Calls per Minute
                            // Business Transaction calls to Backends by Individual Nodes
                            {
                                long businessTransactionID = -1;
                                if (metricNameComponents.Length >= 3)
                                {
                                    string[] businessTransactionIDComponents = metricNameComponents[2].Split(':');
                                    if (businessTransactionIDComponents.Length >= 2 && Int64.TryParse(businessTransactionIDComponents[1], out businessTransactionID) == true)
                                    {
                                        if (entitiesDictionaryByID.TryGetValue(businessTransactionID, out entity) == true)
                                        {
                                            metricName = String.Join("|", metricPathComponents, 5, metricPathComponents.Length - 5);
                                            List<MetricValue> metricValuesConverted = readMetricValuesIntoEntity(entity, metricName, appDRESTMetric, timerangeDuration);
                                            metricValues.AddRange(metricValuesConverted);
                                        }
                                    }
                                }
                                break;
                            }
                        default:
                            {
                                logger.Warn("Metric path='{0}' is not of supported type of metric for processing", appDRESTMetric.metricPath);
                                break;
                            }
                    }

                    #endregion
                }
                else if (String.Compare(metricPathComponents[0], "Backends", true) == 0)
                {
                    #region Backends

                    switch (metricPathComponents.Length)
                    {
                        case 3:
                            // MetricPath = Backends|Discovered backend call - api.shipping.com|Calls per Minute
                            // MetricName = BTM|Backends|Component:{[UNRESOLVED][824]}|Calls per Minute
                            // Backends
                            {
                                long backendID = -1;
                                if (metricNameComponents.Length >= 3)
                                {
                                    string[] backendIDComponents = metricNameComponents[2].Split(':');
                                    if (backendIDComponents.Length >= 2)
                                    {
                                        backendIDComponents = backendIDComponents[1].Split(new char[] { '[', ']' });
                                        if (backendIDComponents.Length >= 4 && Int64.TryParse(backendIDComponents[3], out backendID) == true)
                                        {
                                            if (entitiesDictionaryByID.TryGetValue(backendID, out entity) == true)
                                            {
                                                List<MetricValue> metricValuesConverted = readMetricValuesIntoEntity(entity, metricName, appDRESTMetric, timerangeDuration);
                                                metricValues.AddRange(metricValuesConverted);
                                            }
                                        }
                                    }
                                }
                                // Backends
                                break;
                            }
                        default:
                            {
                                logger.Warn("Metric path='{0}' is not of supported type of metric for processing", appDRESTMetric.metricPath);
                                break;
                            }
                    }

                    #endregion
                }
                else if (String.Compare(metricPathComponents[0], "Errors", true) == 0)
                {
                    #region Errors

                    switch (metricPathComponents.Length)
                    {
                        case 4:
                            // MetrichPath = Errors|ECommerce-Services|ServletException : CannotCreateTransactionException : DatabaseException : SQLNestedException : NoSuchElementException|Errors per Minute
                            // MetricName = BTM|Application Diagnostic Data|Error:12611|Errors per Minute
                            // Error
                            {
                                long errorID = -1;
                                if (metricNameComponents.Length >= 3)
                                {
                                    string[] errorIDComponents = metricNameComponents[2].Split(':');
                                    if (errorIDComponents.Length >= 2 && Int64.TryParse(errorIDComponents[1], out errorID) == true)
                                    {
                                        if (entitiesDictionaryByID.TryGetValue(errorID, out entity) == true)
                                        {
                                            List<MetricValue> metricValuesConverted = readMetricValuesIntoEntity(entity, metricName, appDRESTMetric, timerangeDuration);
                                            metricValues.AddRange(metricValuesConverted);
                                        }
                                    }
                                }
                                break;
                            }
                        case 6:
                            // Error calls by Individual Node
                            {
                                // MetricPath = Errors|ECommerce-Services|ServletException : PersistenceException : DatabaseException : SQLNestedException : NoSuchElementException|Individual Nodes|ECommerce_WEB1_NODE|Errors per Minute
                                // MetricName = BTM|Application Diagnostic Data|Error:12605|Errors per Minute
                                // Error calls by Individual Node
                                if (String.Compare(metricPathComponents[3], "Individual Nodes", true) == 0)
                                {
                                    long errorID = -1;
                                    if (metricNameComponents.Length >= 3)
                                    {
                                        string[] errorIDComponents = metricNameComponents[2].Split(':');
                                        if (errorIDComponents.Length >= 2 && Int64.TryParse(errorIDComponents[1], out errorID) == true)
                                        {
                                            if (entitiesDictionaryByID.TryGetValue(errorID, out entity) == true)
                                            {
                                                metricName = String.Join("|", metricPathComponents, 4, metricPathComponents.Length - 4);
                                                List<MetricValue> metricValuesConverted = readMetricValuesIntoEntity(entity, metricName, appDRESTMetric, timerangeDuration);
                                                metricValues.AddRange(metricValuesConverted);
                                            }
                                        }
                                    }
                                }
                                break;
                            }
                        default:
                            {
                                logger.Warn("Metric path='{0}' is not of supported type of metric for processing", appDRESTMetric.metricPath);
                                break;
                            }
                    }

                    #endregion
                }
                else if (String.Compare(metricPathComponents[0], "Service Endpoints", true) == 0)
                {
                    #region Service End Points

                    switch (metricPathComponents.Length)
                    {
                        case 4:
                            // MetricPath = Service Endpoints|ECommerce-Services|ViewItems.getAllItems|Calls per Minute
                            // MetricName = BTM|Application Diagnostic Data|SEP:4859|Calls per Minute
                            // Service Endpoint
                            {
                                long serviceEndpointID = -1;
                                if (metricNameComponents.Length >= 3)
                                {
                                    string[] serviceEndpointIDComponents = metricNameComponents[2].Split(':');
                                    if (serviceEndpointIDComponents.Length >= 2 && Int64.TryParse(serviceEndpointIDComponents[1], out serviceEndpointID) == true)
                                    {
                                        if (entitiesDictionaryByID.TryGetValue(serviceEndpointID, out entity) == true)
                                        {
                                            List<MetricValue> metricValuesConverted = readMetricValuesIntoEntity(entity, metricName, appDRESTMetric, timerangeDuration);
                                            metricValues.AddRange(metricValuesConverted);
                                        }
                                    }
                                }
                                break;
                            }
                        case 6:
                            // MetricPath = Service Endpoints|ECommerce-Services|ViewItems.getAllItems|Individual Nodes|ECommerce_WEB2_NODE|Calls per Minute
                            // MetricName = BTM|Application Diagnostic Data|SEP:4859|Calls per Minute
                            // Service Endpoint calls by Individual Nodes
                            {
                                if (String.Compare(metricPathComponents[3], "Individual Nodes", true) == 0)
                                {
                                    long serviceEndpointID = -1;
                                    if (metricNameComponents.Length >= 3)
                                    {
                                        string[] serviceEndpointIDComponents = metricNameComponents[2].Split(':');
                                        if (serviceEndpointIDComponents.Length >= 2 && Int64.TryParse(serviceEndpointIDComponents[1], out serviceEndpointID) == true)
                                        {
                                            if (entitiesDictionaryByID.TryGetValue(serviceEndpointID, out entity) == true)
                                            {
                                                metricName = String.Join("|", metricPathComponents, 4, metricPathComponents.Length - 4);
                                                List<MetricValue> metricValuesConverted = readMetricValuesIntoEntity(entity, metricName, appDRESTMetric, timerangeDuration);
                                                metricValues.AddRange(metricValuesConverted);
                                            }
                                        }
                                    }
                                }
                                break;
                            }
                        default:
                            {
                                logger.Warn("Metric path='{0}' is not of supported type of metric for processing", appDRESTMetric.metricPath);
                                break;
                            }
                    }

                    #endregion
                }
                else if (String.Compare(metricPathComponents[0], "Information Points", true) == 0)
                {
                    #region Information Points

                    switch (metricPathComponents.Length)
                    {
                        case 3:
                            // MetricPath = Information Points|CartTotal|Calls per Minute
                            // MetricName = BTM|IPs|IP:8|Calls per Minute
                            // Information Point
                            {
                                long informationPointID = -1;
                                if (metricNameComponents.Length >= 3)
                                {
                                    string[] informationPointIDComponents = metricNameComponents[2].Split(':');
                                    if (informationPointIDComponents.Length >= 2 && Int64.TryParse(informationPointIDComponents[1], out informationPointID) == true)
                                    {
                                        if (entitiesDictionaryByID.TryGetValue(informationPointID, out entity) == true)
                                        {
                                            List<MetricValue> metricValuesConverted = readMetricValuesIntoEntity(entity, metricName, appDRESTMetric, timerangeDuration);
                                            metricValues.AddRange(metricValuesConverted);
                                        }
                                    }
                                }
                                break;
                            }
                        default:
                            {
                                logger.Warn("Metric path='{0}' is not of supported type of metric for processing", appDRESTMetric.metricPath);
                                break;
                            }
                    }

                    #endregion
                }
                else
                {
                    // Unsupported type of metric
                    logger.Warn("Metric path='{0}' is not of supported type of metric for processing", appDRESTMetric.metricPath);
                }

                #endregion
            }

            return metricValues;
        }

        private List<MetricValue> readMetricValuesIntoEntity(EntityBase entity, string metricName, AppDRESTMetric appDRESTMetric, int timerangeDuration)
        {
            List<MetricValue> metricValues = new List<MetricValue>(appDRESTMetric.metricValues.Count);
            if (entity.MetricsIDs == null)
            {
                entity.MetricsIDs = new List<long>();
            }
            entity.MetricsIDs.Add(appDRESTMetric.metricId);
            foreach (AppDRESTMetricValue appDRESTMetricValue in appDRESTMetric.metricValues)
            {
                // Populate metrics into the list for output into CSV
                MetricValue metricValue = new MetricValue();
                metricValue.Controller = entity.Controller;
                metricValue.ApplicationID = entity.ApplicationID;
                metricValue.ApplicationName = entity.ApplicationName;

                metricValue.EntityID = entity.EntityID;
                metricValue.EntityName = entity.EntityName;
                metricValue.EntityType = entity.EntityType;

                metricValue.EventTimeStampUtc = UnixTimeHelper.ConvertFromUnixTimestamp(appDRESTMetricValue.startTimeInMillis);
                metricValue.EventTimeStamp = metricValue.EventTimeStampUtc.ToLocalTime();
                metricValue.EventTime = metricValue.EventTimeStamp;

                metricValue.MetricName = metricName;
                metricValue.MetricID = appDRESTMetric.metricId;
                switch (appDRESTMetric.frequency)
                {
                    case "SIXTY_MIN":
                        {
                            metricValue.MetricResolution = 60;
                            break;
                        }
                    case "TEN_MIN":
                        {
                            metricValue.MetricResolution = 10;
                            break;
                        }
                    case "ONE_MIN":
                        {
                            metricValue.MetricResolution = 1;
                            break;
                        }
                    default:
                        {
                            metricValue.MetricResolution = 1;
                            break;
                        }
                }

                metricValue.Count = appDRESTMetricValue.count;
                metricValue.Min = appDRESTMetricValue.min;
                metricValue.Max = appDRESTMetricValue.max;
                metricValue.Occurrences = appDRESTMetricValue.occurrences;
                metricValue.Sum = appDRESTMetricValue.sum;
                metricValue.Value = appDRESTMetricValue.value;

                metricValues.Add(metricValue);
            }

            // Update the entity with the calculated values if there were more than 1 metric value in the list
            // The speed of this can be improved if I were to keep a rolling total rather than a lambda but I'll do it later
            switch (metricName)
            {
                case METRIC_ART_FULLNAME:
                    double intermediateART = (double)metricValues.Sum(mv => mv.Sum) / (double)metricValues.Sum(mv => mv.Count);
                    if (Double.IsNaN(intermediateART) == true)
                    {
                        entity.ART = 0;
                    }
                    else
                    {
                        entity.ART = (long)Math.Round(intermediateART, 0);
                    }
                    entity.TimeTotal = metricValues.Sum(mv => mv.Sum);
                    break;

                case METRIC_CPM_FULLNAME:
                    entity.CPM = (long)Math.Round((double)((double)metricValues.Sum(mv => mv.Sum) / (double)timerangeDuration), 0);
                    entity.Calls = metricValues.Sum(mv => mv.Sum);
                    break;

                case METRIC_EPM_FULLNAME:
                    entity.EPM = (long)Math.Round((double)((double)metricValues.Sum(mv => mv.Sum) / (double)timerangeDuration), 0);
                    entity.Errors = metricValues.Sum(mv => mv.Sum);
                    entity.ErrorsPercentage = Math.Round((double)(double)entity.Errors / (double)entity.Calls * 100, 2);
                    if (Double.IsNaN(entity.ErrorsPercentage) == true) entity.ErrorsPercentage = 0;
                    break;

                case METRIC_EXCPM_FULLNAME:
                    entity.EXCPM = (long)Math.Round((double)((double)metricValues.Sum(mv => mv.Sum) / (double)timerangeDuration), 0);
                    entity.Exceptions = metricValues.Sum(mv => mv.Sum);
                    break;

                case METRIC_HTTPEPM_FULLNAME:
                    entity.HTTPEPM = (long)Math.Round((double)((double)metricValues.Sum(mv => mv.Sum) / (double)timerangeDuration), 0);
                    entity.HttpErrors = metricValues.Sum(mv => mv.Sum);
                    break;

                default:
                    break;
            }

            return metricValues;
        }

        private List<EntityHourlyMetricValueLocation> getEntityHourlyMetricValueLocationsInTable(List<MetricValue> metricValues, List<JobTimeRange> jobTimeRanges)
        {
            // Pre-size this to about 100 items
            List<EntityHourlyMetricValueLocation> entityMetricsValueLocationList = new List<EntityHourlyMetricValueLocation>(100 * jobTimeRanges.Count);

            EntityHourlyMetricValueLocation currentEntityMetricsValueLocation = null;

            for (int i = 0; i < metricValues.Count; i++)
            {
                MetricValue metricValue = metricValues[i];

                bool startNewLocation = true;

                if (currentEntityMetricsValueLocation != null)
                {
                    if (currentEntityMetricsValueLocation.Controller == metricValue.Controller &&
                        currentEntityMetricsValueLocation.ApplicationID == metricValue.ApplicationID &&
                        currentEntityMetricsValueLocation.EntityType == metricValue.EntityType &&
                        currentEntityMetricsValueLocation.EntityID == metricValue.EntityID)
                    {
                        // Still rolling through the same hourly range?
                        if (currentEntityMetricsValueLocation.ToUtc.Hour == metricValue.EventTimeStampUtc.Hour)
                        {
                            // Yes, still in the same hour
                            currentEntityMetricsValueLocation.ToUtc = metricValue.EventTimeStampUtc;
                            currentEntityMetricsValueLocation.RowEnd = i;

                            startNewLocation = false;
                        }
                    }
                }

                if (startNewLocation)
                {
                    currentEntityMetricsValueLocation = new EntityHourlyMetricValueLocation();
                    currentEntityMetricsValueLocation.Controller = metricValue.Controller;
                    currentEntityMetricsValueLocation.ApplicationName = metricValue.ApplicationName;
                    currentEntityMetricsValueLocation.ApplicationID = metricValue.ApplicationID;
                    currentEntityMetricsValueLocation.EntityType = metricValue.EntityType;
                    currentEntityMetricsValueLocation.EntityName = metricValue.EntityName;
                    currentEntityMetricsValueLocation.EntityID = metricValue.EntityID;
                    currentEntityMetricsValueLocation.MetricName = metricValue.MetricName;
                    currentEntityMetricsValueLocation.MetricID = metricValue.MetricID;
                    currentEntityMetricsValueLocation.FromUtc = metricValue.EventTimeStampUtc;
                    currentEntityMetricsValueLocation.ToUtc = metricValue.EventTimeStampUtc;
                    currentEntityMetricsValueLocation.RowStart = i;
                    currentEntityMetricsValueLocation.RowEnd = i;

                    entityMetricsValueLocationList.Add(currentEntityMetricsValueLocation);
                }
            }

            return entityMetricsValueLocationList;
        }

        private void updateEntityRowWithDurationAndActivityStatus(EntityBase entityRow, JobTimeRange jobTimeRange)
        {
            // Duration
            entityRow.Duration = (int)(jobTimeRange.To - jobTimeRange.From).Duration().TotalMinutes;
            entityRow.From = jobTimeRange.From.ToLocalTime();
            entityRow.To = jobTimeRange.To.ToLocalTime();
            entityRow.FromUtc = jobTimeRange.From;
            entityRow.ToUtc = jobTimeRange.To;

            // Has Activity
            if (entityRow.ART == 0 && entityRow.TimeTotal == 0 &&
                entityRow.CPM == 0 && entityRow.Calls == 0 &&
                entityRow.EPM == 0 && entityRow.Errors == 0 &&
                entityRow.EXCPM == 0 && entityRow.Exceptions == 0 &&
                entityRow.HTTPEPM == 0 && entityRow.HttpErrors == 0)
            {
                entityRow.HasActivity = false;
            }
            else
            {
                entityRow.HasActivity = true;
            }
        }
    }
}
