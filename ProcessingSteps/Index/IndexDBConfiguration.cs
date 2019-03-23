using AppDynamics.Dexter.ReportObjectMaps;
using AppDynamics.Dexter.ReportObjects;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class IndexDBConfiguration : JobStepIndexBase
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

                if (jobConfiguration.Target.Count(t => t.Type == APPLICATION_TYPE_DB) == 0)
                {
                    return true;
                }

                bool reportFolderCleaned = false;

                // Process each Controller once
                int i = 0;
                var controllers = jobConfiguration.Target.Where(t => t.Type == APPLICATION_TYPE_DB).ToList().GroupBy(t => t.Controller);
                foreach (var controllerGroup in controllers)
                {
                    Stopwatch stopWatchTarget = new Stopwatch();
                    stopWatchTarget.Start();

                    JobTarget jobTarget = controllerGroup.ToList()[0];

                    if (jobTarget.Type != null && jobTarget.Type.Length > 0 && jobTarget.Type != APPLICATION_TYPE_DB) continue;

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

                        #region Database Collector Definitions

                        List<DBCollectorDefinition> dbCollectorDefinitionsList = null;

                        JArray dbCollectorDefinitionsArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.DBCollectorDefinitionsDataFilePath(jobTarget));
                        if (dbCollectorDefinitionsArray != null)
                        {
                            loggerConsole.Info("Index List of DB Collector Definitions ({0} entities)", dbCollectorDefinitionsArray.Count);

                            dbCollectorDefinitionsList = new List<DBCollectorDefinition>(dbCollectorDefinitionsArray.Count);

                            foreach (JToken dbCollectorDefinitionToken in dbCollectorDefinitionsArray)
                            {
                                if (isTokenPropertyNull(dbCollectorDefinitionToken, "config") == false)
                                {
                                    JToken dbCollectorDefinitionConfigToken = dbCollectorDefinitionToken["config"];

                                    DBCollectorDefinition dbCollectorDefinition = new DBCollectorDefinition();
                                    dbCollectorDefinition.Controller = jobTarget.Controller;
                                    dbCollectorDefinition.CollectorName = getStringValueFromJToken(dbCollectorDefinitionConfigToken, "name");
                                    dbCollectorDefinition.CollectorType = getStringValueFromJToken(dbCollectorDefinitionConfigToken, "type");
                                    dbCollectorDefinition.CollectorStatus = getStringValueFromJToken(dbCollectorDefinitionToken, "collectorStatus");

                                    dbCollectorDefinition.AgentName = getStringValueFromJToken(dbCollectorDefinitionConfigToken, "agentName");

                                    dbCollectorDefinition.Host = getStringValueFromJToken(dbCollectorDefinitionConfigToken, "hostname");
                                    dbCollectorDefinition.Port = getIntValueFromJToken(dbCollectorDefinitionConfigToken, "port");
                                    dbCollectorDefinition.UserName = getStringValueFromJToken(dbCollectorDefinitionConfigToken, "username");

                                    dbCollectorDefinition.IsEnabled = getBoolValueFromJToken(dbCollectorDefinitionConfigToken, "enabled");
                                    dbCollectorDefinition.IsLoggingEnabled = getBoolValueFromJToken(dbCollectorDefinitionConfigToken, "loggingEnabled");

                                    dbCollectorDefinition.DatabaseName = getStringValueFromJToken(dbCollectorDefinitionConfigToken, "databaseName");
                                    dbCollectorDefinition.FailoverPartner = getStringValueFromJToken(dbCollectorDefinitionConfigToken, "failoverPartner");
                                    dbCollectorDefinition.SID = getStringValueFromJToken(dbCollectorDefinitionConfigToken, "sid");
                                    dbCollectorDefinition.CustomConnectionString = getStringValueFromJToken(dbCollectorDefinitionConfigToken, "customConnectionString");

                                    dbCollectorDefinition.UseWindowsAuth = getBoolValueFromJToken(dbCollectorDefinitionConfigToken, "useWindowsAuth");
                                    dbCollectorDefinition.ConnectAsSysDBA = getBoolValueFromJToken(dbCollectorDefinitionConfigToken, "connectAsSysdba");
                                    dbCollectorDefinition.UseServiceName = getBoolValueFromJToken(dbCollectorDefinitionConfigToken, "useServiceName");
                                    dbCollectorDefinition.UseSSL = getBoolValueFromJToken(dbCollectorDefinitionConfigToken, "useSSL");

                                    dbCollectorDefinition.IsEnterpriseDB = getBoolValueFromJToken(dbCollectorDefinitionConfigToken, "enterpriseDB");

                                    dbCollectorDefinition.IsOSMonitoringEnabled = getBoolValueFromJToken(dbCollectorDefinitionConfigToken, "enableOSMonitor");
                                    dbCollectorDefinition.UseLocalWMI = getBoolValueFromJToken(dbCollectorDefinitionConfigToken, "useLocalWMI");

                                    dbCollectorDefinition.HostOS = getStringValueFromJToken(dbCollectorDefinitionConfigToken, "hostOS");
                                    dbCollectorDefinition.HostDomain = getStringValueFromJToken(dbCollectorDefinitionConfigToken, "hostDomain");
                                    dbCollectorDefinition.HostUserName = getStringValueFromJToken(dbCollectorDefinitionConfigToken, "hostUsername");
                                    dbCollectorDefinition.UseCertificateAuth = getBoolValueFromJToken(dbCollectorDefinitionConfigToken, "certificateAuth");
                                    dbCollectorDefinition.SSHPort = getIntValueFromJToken(dbCollectorDefinitionConfigToken, "sshPort");
                                    dbCollectorDefinition.DBInstanceID = getStringValueFromJToken(dbCollectorDefinitionConfigToken, "dbInstanceIdentifier");
                                    dbCollectorDefinition.Region = getStringValueFromJToken(dbCollectorDefinitionConfigToken, "region");
                                    dbCollectorDefinition.RemoveLiterals = getBoolValueFromJToken(dbCollectorDefinitionConfigToken, "removeLiterals");
                                    dbCollectorDefinition.IsLDAPEnabled = getBoolValueFromJToken(dbCollectorDefinitionConfigToken, "ldapEnabled");

                                    dbCollectorDefinition.CreatedBy = getStringValueFromJToken(dbCollectorDefinitionConfigToken, "createdBy");
                                    dbCollectorDefinition.CreatedOn = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(dbCollectorDefinitionConfigToken, "createdOn"));
                                    dbCollectorDefinition.ModifiedBy = getStringValueFromJToken(dbCollectorDefinitionConfigToken, "modifiedBy");
                                    dbCollectorDefinition.ModifiedOn = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(dbCollectorDefinitionConfigToken, "modifiedOn"));

                                    dbCollectorDefinition.ConfigID = getLongValueFromJToken(dbCollectorDefinitionToken, "configId");

                                    dbCollectorDefinition.ControllerLink = String.Format(DEEPLINK_CONTROLLER, dbCollectorDefinition.Controller, DEEPLINK_TIMERANGE_LAST_15_MINUTES);
                                    dbCollectorDefinition.ApplicationLink = String.Format(DEEPLINK_DB_APPLICATION, dbCollectorDefinition.Controller, DEEPLINK_TIMERANGE_LAST_15_MINUTES);

                                    dbCollectorDefinitionsList.Add(dbCollectorDefinition);
                                }
                            }

                            // Sort them
                            dbCollectorDefinitionsList = dbCollectorDefinitionsList.OrderBy(o => o.CollectorType).ThenBy(o => o.CollectorName).ToList();
                            FileIOHelper.WriteListToCSVFile(dbCollectorDefinitionsList, new DBCollectorDefinitionReportMap(), FilePathMap.DBCollectorDefinitionsIndexFilePath(jobTarget));

                            stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + dbCollectorDefinitionsList.Count;
                        }

                        #endregion

                        #region Custom Metrics

                        List<DBCustomMetric> dbCustomMetricsList = null;

                        JArray dbCustomMetricsArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.DBCustomMetricsDataFilePath(jobTarget));
                        if (dbCustomMetricsArray != null)
                        {
                            loggerConsole.Info("Index List of DB Custom Metrics ({0} entities)", dbCustomMetricsArray.Count);

                            dbCustomMetricsList = new List<DBCustomMetric>(dbCustomMetricsArray.Count);

                            foreach (JObject dbCustomMetricObject in dbCustomMetricsArray)
                            {
                                DBCustomMetric dbCustomMetric = new DBCustomMetric();

                                dbCustomMetric.Controller = jobTarget.Controller;

                                dbCustomMetric.MetricName = getStringValueFromJToken(dbCustomMetricObject, "name");
                                dbCustomMetric.MetricID = getLongValueFromJToken(dbCustomMetricObject, "id");

                                dbCustomMetric.Frequency = getIntValueFromJToken(dbCustomMetricObject, "timeIntervalInMin");

                                dbCustomMetric.IsEvent = getBoolValueFromJToken(dbCustomMetricObject, "isEvent");

                                dbCustomMetric.Query = getStringValueFromJToken(dbCustomMetricObject, "queryText");

                                // Get SQL statement type
                                dbCustomMetric.SQLClauseType = getSQLClauseType(dbCustomMetric.Query, 100);

                                // Check other clauses
                                dbCustomMetric.SQLWhere = doesSQLStatementContain(dbCustomMetric.Query, @"\bWHERE\s");
                                dbCustomMetric.SQLGroupBy = doesSQLStatementContain(dbCustomMetric.Query, @"\bGROUP BY\s");
                                dbCustomMetric.SQLOrderBy = doesSQLStatementContain(dbCustomMetric.Query, @"\bORDER BY\s");
                                dbCustomMetric.SQLHaving = doesSQLStatementContain(dbCustomMetric.Query, @"\bHAVING\s");
                                dbCustomMetric.SQLUnion = doesSQLStatementContain(dbCustomMetric.Query, @"\bUNION\s");

                                // Get join type if present
                                dbCustomMetric.SQLJoinType = getSQLJoinType(dbCustomMetric.Query);

                                // Now lookup the collector assigned
                                if (dbCollectorDefinitionsList != null)
                                {
                                    if (isTokenPropertyNull(dbCustomMetricObject, "dbaemc") == false)
                                    {
                                        if (isTokenPropertyNull(dbCustomMetricObject["dbaemc"], "dbServerIds") == false)
                                        {
                                            JValue configIDValue = null;
                                            try { configIDValue = (JValue)dbCustomMetricObject["dbaemc"]["dbServerIds"].First(); } catch { }
                                            if (configIDValue != null)
                                            {
                                                long configID = (long)configIDValue;

                                                DBCollectorDefinition dbCollectorDefinition = dbCollectorDefinitionsList.Where(d => d.ConfigID == configID).FirstOrDefault();
                                                if (dbCollectorDefinition != null)
                                                {
                                                    dbCustomMetric.CollectorName = dbCollectorDefinition.CollectorName;
                                                    dbCustomMetric.CollectorType = dbCollectorDefinition.CollectorType;

                                                    dbCustomMetric.ConfigID = dbCollectorDefinition.ConfigID;
                                                }
                                            }
                                        }
                                    }
                                }

                                dbCustomMetricsList.Add(dbCustomMetric);
                            }

                            // Sort them
                            dbCustomMetricsList = dbCustomMetricsList.OrderBy(o => o.CollectorType).ThenBy(o => o.CollectorName).ToList();
                            FileIOHelper.WriteListToCSVFile(dbCustomMetricsList, new DBCustomMetricReportMap(), FilePathMap.DBCustomMetricsIndexFilePath(jobTarget));

                            stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + dbCustomMetricsList.Count;
                        }

                        #endregion

                        #region Application

                        loggerConsole.Info("Index Application");

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + 1;

                        DBApplicationConfiguration applicationConfiguration = new DBApplicationConfiguration();
                        applicationConfiguration.Controller = jobTarget.Controller;
                        applicationConfiguration.ControllerLink = String.Format(DEEPLINK_CONTROLLER, applicationConfiguration.Controller, DEEPLINK_TIMERANGE_LAST_15_MINUTES);
                        applicationConfiguration.ApplicationName = jobTarget.Application;
                        applicationConfiguration.ApplicationLink = String.Format(DEEPLINK_DB_APPLICATION, applicationConfiguration.Controller, DEEPLINK_TIMERANGE_LAST_15_MINUTES);
                        applicationConfiguration.ApplicationID = jobTarget.ApplicationID;

                        if (dbCollectorDefinitionsList != null)
                        {
                            applicationConfiguration.NumCollectorDefinitions = dbCollectorDefinitionsList.Count;
                        }

                        if (dbCustomMetricsList != null)
                        {
                            applicationConfiguration.NumCustomMetrics = dbCustomMetricsList.Count;
                        }

                        List<DBApplicationConfiguration> applicationConfigurationsList = new List<DBApplicationConfiguration>(1);
                        applicationConfigurationsList.Add(applicationConfiguration);

                        FileIOHelper.WriteListToCSVFile(applicationConfigurationsList, new DBApplicationConfigurationReportMap(), FilePathMap.DBApplicationConfigurationIndexFilePath(jobTarget));

                        #endregion

                        #region Combine All for Report CSV

                        // If it is the first one, clear out the combined folder
                        if (reportFolderCleaned == false)
                        {
                            FileIOHelper.DeleteFolder(FilePathMap.DBConfigurationReportFolderPath());
                            Thread.Sleep(1000);
                            FileIOHelper.CreateFolder(FilePathMap.DBConfigurationReportFolderPath());
                            reportFolderCleaned = true;
                        }

                        // Append all the individual report files into one
                        if (File.Exists(FilePathMap.DBCollectorDefinitionsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.DBCollectorDefinitionsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.DBCollectorDefinitionsReportFilePath(), FilePathMap.DBCollectorDefinitionsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.DBCustomMetricsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.DBCustomMetricsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.DBCustomMetricsReportFilePath(), FilePathMap.DBCustomMetricsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.DBApplicationConfigurationIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.DBApplicationConfigurationIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.DBApplicationConfigurationReportFilePath(), FilePathMap.DBApplicationConfigurationIndexFilePath(jobTarget));
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
            logger.Trace("Input.Configuration={0}", jobConfiguration.Input.Configuration);
            loggerConsole.Trace("Input.Configuration={0}", jobConfiguration.Input.Configuration);
            if (jobConfiguration.Input.Configuration == false)
            {
                loggerConsole.Trace("Skipping index of configuration");
            }
            return (jobConfiguration.Input.Configuration == true);
        }
    }
}
