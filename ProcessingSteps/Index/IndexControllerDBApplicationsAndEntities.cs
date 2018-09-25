using AppDynamics.Dexter.ReportObjects;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class IndexControllerDBApplicationsAndEntities : JobStepIndexBase
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

                // Process each target
                for (int i = 0; i < jobConfiguration.Target.Count; i++)
                {
                    Stopwatch stopWatchTarget = new Stopwatch();
                    stopWatchTarget.Start();

                    JobTarget jobTarget = jobConfiguration.Target[i];

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
                        List<DBCollector> dbCollectorsList = null;

                        if (File.Exists(FilePathMap.DBCollectorDefinitionsIndexFilePath(jobTarget)) == false)
                        {
                            JArray dbCollectorDefinitionsRESTList = FileIOHelper.LoadJArrayFromFile(FilePathMap.DBCollectorDefinitionsDataFilePath(jobTarget));
                            if (dbCollectorDefinitionsRESTList != null)
                            {
                                loggerConsole.Info("Index List of DB Collector Definitions ({0} entities)", dbCollectorDefinitionsRESTList.Count);

                                dbCollectorDefinitionsList = new List<DBCollectorDefinition>(dbCollectorDefinitionsRESTList.Count);

                                foreach (JToken dbCollectorDefinitionREST in dbCollectorDefinitionsRESTList)
                                {
                                    DBCollectorDefinition dbCollectorDefinition = new DBCollectorDefinition();
                                    dbCollectorDefinition.Controller = jobTarget.Controller;
                                    dbCollectorDefinition.CollectorName = dbCollectorDefinitionREST["config"]["name"].ToString();
                                    dbCollectorDefinition.CollectorType = dbCollectorDefinitionREST["config"]["type"].ToString();
                                    dbCollectorDefinition.CollectorStatus = dbCollectorDefinitionREST["collectorStatus"].ToString();

                                    try { dbCollectorDefinition.AgentName = dbCollectorDefinitionREST["config"]["agentName"].ToString(); } catch { }

                                    try { dbCollectorDefinition.Host = dbCollectorDefinitionREST["config"]["hostname"].ToString(); } catch { }
                                    try { dbCollectorDefinition.Port = (int)dbCollectorDefinitionREST["config"]["port"]; } catch { }
                                    try { dbCollectorDefinition.UserName = dbCollectorDefinitionREST["config"]["username"].ToString(); } catch { }

                                    try { dbCollectorDefinition.IsEnabled = (bool)dbCollectorDefinitionREST["config"]["enabled"]; } catch { }
                                    try { dbCollectorDefinition.IsLoggingEnabled = (bool)dbCollectorDefinitionREST["config"]["loggingEnabled"]; } catch { }

                                    try { dbCollectorDefinition.DatabaseName = dbCollectorDefinitionREST["config"]["databaseName"].ToString(); } catch { }
                                    try { dbCollectorDefinition.FailoverPartner = dbCollectorDefinitionREST["config"]["failoverPartner"].ToString(); } catch { }
                                    try { dbCollectorDefinition.SID = dbCollectorDefinitionREST["config"]["sid"].ToString(); } catch { }
                                    try { dbCollectorDefinition.CustomConnectionString = dbCollectorDefinitionREST["config"]["customConnectionString"].ToString(); } catch { }

                                    try { dbCollectorDefinition.UseWindowsAuth = (bool)dbCollectorDefinitionREST["config"]["useWindowsAuth"]; } catch { }
                                    try { dbCollectorDefinition.ConnectAsSysDBA = (bool)dbCollectorDefinitionREST["config"]["connectAsSysdba"]; } catch { }
                                    try { dbCollectorDefinition.UseServiceName = (bool)dbCollectorDefinitionREST["config"]["useServiceName"]; } catch { }
                                    try { dbCollectorDefinition.UseSSL = (bool)dbCollectorDefinitionREST["config"]["useSSL"]; } catch { }

                                    try { dbCollectorDefinition.IsEnterpriseDB = (bool)dbCollectorDefinitionREST["config"]["enterpriseDB"]; } catch { }

                                    try { dbCollectorDefinition.IsOSMonitoringEnabled = (bool)dbCollectorDefinitionREST["config"]["enableOSMonitor"]; } catch { }
                                    try { dbCollectorDefinition.UseLocalWMI = (bool)dbCollectorDefinitionREST["config"]["useLocalWMI"]; } catch { }

                                    try { dbCollectorDefinition.HostOS = dbCollectorDefinitionREST["config"]["hostOS"].ToString(); } catch { }
                                    try { dbCollectorDefinition.HostDomain = dbCollectorDefinitionREST["config"]["hostDomain"].ToString(); } catch { }
                                    try { dbCollectorDefinition.HostUserName = dbCollectorDefinitionREST["config"]["hostUsername"].ToString(); } catch { }
                                    try { dbCollectorDefinition.UseCertificateAuth = (bool)dbCollectorDefinitionREST["config"]["certificateAuth"]; } catch { }
                                    try { dbCollectorDefinition.SSHPort = (int)dbCollectorDefinitionREST["config"]["sshPort"]; } catch { }
                                    try { dbCollectorDefinition.DBInstanceID = dbCollectorDefinitionREST["config"]["dbInstanceIdentifier"].ToString(); } catch { }
                                    try { dbCollectorDefinition.Region = dbCollectorDefinitionREST["config"]["region"].ToString(); } catch { }
                                    try { dbCollectorDefinition.RemoveLiterals = (bool)dbCollectorDefinitionREST["config"]["removeLiterals"]; } catch { }
                                    try { dbCollectorDefinition.IsLDAPEnabled = (bool)dbCollectorDefinitionREST["config"]["ldapEnabled"]; } catch { }

                                    try { dbCollectorDefinition.CreatedBy = dbCollectorDefinitionREST["config"]["createdBy"].ToString(); } catch { }
                                    try { dbCollectorDefinition.CreatedOn = UnixTimeHelper.ConvertFromUnixTimestamp((long)dbCollectorDefinitionREST["config"]["createdOn"]); } catch { }
                                    try { dbCollectorDefinition.ModifiedBy = dbCollectorDefinitionREST["config"]["modifiedBy"].ToString(); } catch { }
                                    try { dbCollectorDefinition.ModifiedOn = UnixTimeHelper.ConvertFromUnixTimestamp((long)dbCollectorDefinitionREST["config"]["modifiedOn"]); } catch { }

                                    dbCollectorDefinition.ConfigID = (long)dbCollectorDefinitionREST["configId"];

                                    updateEntityWithDeeplinks(dbCollectorDefinition, jobConfiguration.Input.TimeRange);

                                    dbCollectorDefinitionsList.Add(dbCollectorDefinition);
                                }

                                // Sort them
                                dbCollectorDefinitionsList = dbCollectorDefinitionsList.OrderBy(o => o.CollectorType).ThenBy(o => o.CollectorName).ToList();
                                FileIOHelper.WriteListToCSVFile(dbCollectorDefinitionsList, new DBCollectorDefinitionReportMap(), FilePathMap.DBCollectorDefinitionsIndexFilePath(jobTarget));

                                stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + dbCollectorDefinitionsList.Count;
                            }
                        }
                        else
                        {
                            dbCollectorDefinitionsList = FileIOHelper.ReadListFromCSVFile<DBCollectorDefinition>(FilePathMap.DBCollectorDefinitionsIndexFilePath(jobTarget), new DBCollectorDefinitionReportMap());
                        }

                        #endregion

                        #region Database Collectors

                        if (File.Exists(FilePathMap.DBCollectorsIndexFilePath(jobTarget)) == false && dbCollectorDefinitionsList != null)
                        {
                            JObject dbCollectorsCallsRESTContainer = FileIOHelper.LoadJObjectFromFile(FilePathMap.DBCollectorsCallsDataFilePath(jobTarget));
                            JObject dbCollectorsTimeSpentRESTContainer = FileIOHelper.LoadJObjectFromFile(FilePathMap.DBCollectorsTimeSpentDataFilePath(jobTarget));

                            JArray dbCollectorsCallsRESTList = null;
                            JArray dbCollectorsTimeSpentRESTList = null;
                            if (dbCollectorsCallsRESTContainer != null)
                            {
                                dbCollectorsCallsRESTList = ((JArray)dbCollectorsCallsRESTContainer["data"]);
                            }
                            if (dbCollectorsTimeSpentRESTContainer != null)
                            {
                                dbCollectorsTimeSpentRESTList = ((JArray)dbCollectorsTimeSpentRESTContainer["data"]);
                            }

                            if (dbCollectorsCallsRESTList != null)
                            {
                                loggerConsole.Info("Index List of DB Collectors ({0} entities)", dbCollectorsCallsRESTList.Count);

                                dbCollectorsList = new List<DBCollector>(dbCollectorsCallsRESTList.Count);

                                foreach (JToken dbCollectorREST in dbCollectorsCallsRESTList)
                                {
                                    DBCollector dbCollector = new DBCollector();
                                    dbCollector.Controller = jobTarget.Controller;
                                    dbCollector.CollectorName = dbCollectorREST["name"].ToString();
                                    dbCollector.CollectorType = dbCollectorREST["dbType"].ToString();

                                    dbCollector.Role = dbCollectorREST["role"].ToString();

                                    // Find the Collector Definition for this Collector
                                    DBCollectorDefinition dbCollectorDefinition = dbCollectorDefinitionsList.Where(d => d.ConfigID == (long)dbCollectorREST["configId"]).FirstOrDefault();
                                    if (dbCollectorDefinition != null)
                                    {
                                        dbCollector.Host = dbCollectorDefinition.Host;
                                        dbCollector.Port = dbCollectorDefinition.Port;
                                        dbCollector.UserName = dbCollectorDefinition.UserName;

                                        dbCollector.AgentName = dbCollectorDefinition.AgentName;
                                        dbCollector.CollectorStatus = dbCollectorDefinition.CollectorStatus;
                                    }

                                    // Performance data
                                    try { dbCollector.Calls = (long)dbCollectorREST["rolledUpMetricDatas"]["DB|KPI|Calls per Minute"]; } catch { }
                                    
                                    try
                                    {
                                        JToken dbCollectorDefinitionWithTimeREST = dbCollectorsTimeSpentRESTList.Where(t => (long)t["id"] == (long)dbCollectorREST["id"]).FirstOrDefault();
                                        dbCollector.ExecTime = (long)dbCollectorDefinitionWithTimeREST["rolledUpMetricDatas"]["DB|KPI|Time Spent in Executions (s)"];
                                        dbCollector.ExecTimeSpan = new TimeSpan(dbCollector.ExecTime * TimeSpan.TicksPerSecond);
                                    }
                                    catch { }

                                    dbCollector.Duration = (int)(jobConfiguration.Input.TimeRange.To - jobConfiguration.Input.TimeRange.From).Duration().TotalMinutes;
                                    dbCollector.From = jobConfiguration.Input.TimeRange.From.ToLocalTime();
                                    dbCollector.To = jobConfiguration.Input.TimeRange.To.ToLocalTime();
                                    dbCollector.FromUtc = jobConfiguration.Input.TimeRange.From;
                                    dbCollector.ToUtc = jobConfiguration.Input.TimeRange.To;

                                    dbCollector.ConfigID = (long)dbCollectorREST["configId"];
                                    dbCollector.CollectorID = (long)dbCollectorREST["id"];

                                    updateEntityWithDeeplinks(dbCollector, jobConfiguration.Input.TimeRange);

                                    dbCollectorsList.Add(dbCollector);
                                }

                                // Sort them
                                dbCollectorsList = dbCollectorsList.OrderBy(o => o.CollectorType).ThenBy(o => o.CollectorName).ToList();
                                FileIOHelper.WriteListToCSVFile(dbCollectorsList, new DBCollectorReportMap(), FilePathMap.DBCollectorsIndexFilePath(jobTarget));

                                stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + dbCollectorsList.Count;
                            }
                        }
                        else
                        {
                            dbCollectorsList = FileIOHelper.ReadListFromCSVFile<DBCollector>(FilePathMap.DBCollectorsIndexFilePath(jobTarget), new DBCollectorReportMap());
                        }

                        #endregion

                        #region Application

                        if (File.Exists(FilePathMap.DBApplicationIndexFilePath(jobTarget)) == false)
                        {
                            loggerConsole.Info("Index Application");

                            stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + 1;

                            List<DBApplication> applicationsList = new List<DBApplication>(1);
                            DBApplication applicationRow = new DBApplication();
                            applicationRow.Controller = jobTarget.Controller;
                            if (dbCollectorDefinitionsList != null)
                            {
                                applicationRow.NumCollectorDefinitions = dbCollectorDefinitionsList.Count;
                            }
                            if (dbCollectorsList != null)
                            {
                                applicationRow.NumCollectors = dbCollectorsList.Count;

                                applicationRow.NumOracle = dbCollectorsList.Where(d => d.CollectorType == "ORACLE").Count();
                                applicationRow.NumSQLServer = dbCollectorsList.Where(d => d.CollectorType == "MSSQL").Count();
                                applicationRow.NumMySQL = dbCollectorsList.Where(d => d.CollectorType == "MYSQL").Count();
                                applicationRow.NumMongo = dbCollectorsList.Where(d => d.CollectorType == "MONGO").Count();
                                applicationRow.NumPostgres = dbCollectorsList.Where(d => d.CollectorType == "POSTGRESQL").Count();
                                applicationRow.NumDB2 = dbCollectorsList.Where(d => d.CollectorType == "DB2").Count();
                                applicationRow.NumSybase = dbCollectorsList.Where(d => d.CollectorType == "SYBASE").Count();
                                applicationRow.NumOther = dbCollectorsList.Count - (applicationRow.NumOracle + applicationRow.NumSQLServer + applicationRow.NumMySQL + applicationRow.NumMongo + applicationRow.NumPostgres + applicationRow.NumDB2 + applicationRow.NumSybase);

                            }

                            updateEntityWithDeeplinks(applicationRow, jobConfiguration.Input.TimeRange);

                            applicationsList.Add(applicationRow);

                            FileIOHelper.WriteListToCSVFile(applicationsList, new DBApplicationReportMap(), FilePathMap.DBApplicationIndexFilePath(jobTarget));
                        }

                        #endregion

                        #region Wait States

                        // Find the Collector Definition for this Collector
                        DBCollector dbCollectorThis = dbCollectorsList.Where(d => d.CollectorID == jobTarget.ApplicationID).FirstOrDefault();

                        JArray allDBWaitStatesRESTList = FileIOHelper.LoadJArrayFromFile(FilePathMap.DBAllWaitStatesDataFilePath(jobTarget));
                        JObject currentDBWaitStatesRESTContainer = FileIOHelper.LoadJObjectFromFile(FilePathMap.DBCurrentWaitStatesDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));

                        if (allDBWaitStatesRESTList != null && allDBWaitStatesRESTList.Count > 0 && currentDBWaitStatesRESTContainer != null)
                        {
                            if (dbCollectorThis != null)
                            {
                                loggerConsole.Info("Index List of Wait States");

                                List<DBWaitState> waitStatesList = new List<DBWaitState>(allDBWaitStatesRESTList.Count / 10);

                                foreach (JToken currentWaitState in currentDBWaitStatesRESTContainer["waitStateMap"])
                                {
                                    DBWaitState dbWaitState = new DBWaitState();
                                    dbWaitState.Controller = jobTarget.Controller;
                                    dbWaitState.CollectorName = dbCollectorThis.CollectorName;
                                    dbWaitState.CollectorType = dbCollectorThis.CollectorType;

                                    dbWaitState.CollectorStatus = dbCollectorThis.CollectorStatus;

                                    dbWaitState.AgentName = dbCollectorThis.AgentName;

                                    dbWaitState.Host = dbCollectorThis.Host;
                                    dbWaitState.Port = dbCollectorThis.Port;
                                    dbWaitState.UserName = dbCollectorThis.UserName;

                                    dbWaitState.State = ((JProperty)currentWaitState).Name;
                                    dbWaitState.ExecTime = (long)((JProperty)currentWaitState).Value;
                                    dbWaitState.ExecTimeSpan = new TimeSpan(dbWaitState.ExecTime * TimeSpan.TicksPerMillisecond);

                                    try
                                    {
                                        JToken dbWaitStateWithID = allDBWaitStatesRESTList.Where(w => w["name"].ToString().ToLower() == dbWaitState.State.ToLower()).FirstOrDefault();
                                        dbWaitState.WaitStateID = (long)dbWaitStateWithID["id"];
                                    }
                                    catch
                                    {
                                        dbWaitState.WaitStateID = -1;
                                    }

                                    dbWaitState.Duration = (int)(jobConfiguration.Input.TimeRange.To - jobConfiguration.Input.TimeRange.From).Duration().TotalMinutes;
                                    dbWaitState.From = jobConfiguration.Input.TimeRange.From.ToLocalTime();
                                    dbWaitState.To = jobConfiguration.Input.TimeRange.To.ToLocalTime();
                                    dbWaitState.FromUtc = jobConfiguration.Input.TimeRange.From;
                                    dbWaitState.ToUtc = jobConfiguration.Input.TimeRange.To;

                                    dbWaitState.ConfigID = dbCollectorThis.ConfigID;
                                    dbWaitState.CollectorID = dbCollectorThis.CollectorID;

                                    waitStatesList.Add(dbWaitState);
                                }

                                // Sort them
                                waitStatesList = waitStatesList.OrderBy(o => o.State).ToList();
                                FileIOHelper.WriteListToCSVFile(waitStatesList, new DBWaitStateReportMap(), FilePathMap.DBWaitStatesIndexFilePath(jobTarget));

                                stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + waitStatesList.Count;
                            }
                        }

                        #endregion

                        #region Queries

                        JArray dbQueriesRESTList = FileIOHelper.LoadJArrayFromFile(FilePathMap.DBQueriesDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));

                        if (dbQueriesRESTList != null && dbQueriesRESTList.Count > 0)
                        {
                            if (dbCollectorThis != null)
                            {
                                loggerConsole.Info("Index List of Queries ({0} entities)", dbQueriesRESTList.Count);

                                List<DBQuery> dbQueryList = new List<DBQuery>(dbQueriesRESTList.Count);

                                foreach (JToken dbQueryJSON in dbQueriesRESTList)
                                {
                                    DBQuery dbQuery = new DBQuery();
                                    dbQuery.Controller = jobTarget.Controller;
                                    dbQuery.CollectorName = dbCollectorThis.CollectorName;
                                    dbQuery.CollectorType = dbCollectorThis.CollectorType;

                                    dbQuery.CollectorStatus = dbCollectorThis.CollectorStatus;

                                    dbQuery.AgentName = dbCollectorThis.AgentName;

                                    dbQuery.Host = dbCollectorThis.Host;
                                    dbQuery.Port = dbCollectorThis.Port;
                                    dbQuery.UserName = dbCollectorThis.UserName;

                                    try { dbQuery.Calls = (long)dbQueryJSON["hits"]; } catch { }
                                    try { dbQuery.ExecTime = Convert.ToInt64((Decimal)dbQueryJSON["duration"]); } catch { }
                                    try { dbQuery.ExecTimeSpan = new TimeSpan(dbQuery.ExecTime * TimeSpan.TicksPerMillisecond); } catch { }
                                    try { if (dbQuery.Calls != 0) dbQuery.AvgExecTime = dbQuery.ExecTime / dbQuery.Calls; } catch { }

                                    dbQuery.AvgExecRange = getDurationRangeAsString(dbQuery.AvgExecTime);

                                    try { dbQuery.Weight = Math.Round((Decimal)dbQueryJSON["weight"], 2); } catch { }

                                    try { dbQuery.QueryHash = dbQueryJSON["queryHashCode"].ToString(); } catch { }
                                    try { dbQuery.Query = dbQueryJSON["queryText"].ToString(); } catch { }

                                    try { dbQuery.Name = dbQueryJSON["name"].ToString(); } catch { }
                                    try { dbQuery.Namespace = dbQueryJSON["namespace"].ToString(); } catch { }
                                    try { dbQuery.Client = dbQueryJSON["clientName"].ToString(); } catch { }

                                    try { dbQuery.IsSnapWindowData = (bool)dbQueryJSON["snapshotWindowData"]; } catch { }
                                    try { dbQuery.IsSnapCorrData = (bool)dbQueryJSON["snapshotCorrelationData"]; } catch { }

                                    try { dbQuery.QueryID = (long)dbQueryJSON["id"]; } catch { }

                                    int lengthToSeekThrough = 30;
                                    // Get SQL statement type
                                    dbQuery.SQLClauseType = getSQLClauseType(dbQuery.Query, lengthToSeekThrough);

                                    // Check other clauses
                                    dbQuery.SQLWhere = doesSQLStatementContain(dbQuery.Query, @"\bWHERE\s");
                                    dbQuery.SQLGroupBy = doesSQLStatementContain(dbQuery.Query, @"\bGROUP BY\s");
                                    dbQuery.SQLOrderBy = doesSQLStatementContain(dbQuery.Query, @"\bORDER BY\s");
                                    dbQuery.SQLHaving = doesSQLStatementContain(dbQuery.Query, @"\bHAVING\s");
                                    dbQuery.SQLUnion = doesSQLStatementContain(dbQuery.Query, @"\bUNION\s");

                                    // Get join type if present
                                    dbQuery.SQLJoinType = getSQLJoinType(dbQuery.Query);

                                    dbQuery.Duration = (int)(jobConfiguration.Input.TimeRange.To - jobConfiguration.Input.TimeRange.From).Duration().TotalMinutes;
                                    dbQuery.From = jobConfiguration.Input.TimeRange.From.ToLocalTime();
                                    dbQuery.To = jobConfiguration.Input.TimeRange.To.ToLocalTime();
                                    dbQuery.FromUtc = jobConfiguration.Input.TimeRange.From;
                                    dbQuery.ToUtc = jobConfiguration.Input.TimeRange.To;

                                    dbQuery.ConfigID = dbCollectorThis.ConfigID;
                                    dbQuery.CollectorID = dbCollectorThis.CollectorID;

                                    updateEntityWithDeeplinks(dbQuery, jobConfiguration.Input.TimeRange);

                                    dbQueryList.Add(dbQuery);
                                }

                                // Sort them
                                dbQueryList = dbQueryList.OrderByDescending(o => o.ExecTime).ToList();
                                FileIOHelper.WriteListToCSVFile(dbQueryList, new DBQueryReportMap(), FilePathMap.DBQueriesIndexFilePath(jobTarget));

                                stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + dbQueryList.Count;
                            }
                        }

                        #endregion

                        #region Clients

                        JArray dbClientsRESTList = FileIOHelper.LoadJArrayFromFile(FilePathMap.DBClientsDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));

                        if (dbClientsRESTList != null && dbClientsRESTList.Count > 0)
                        {
                            if (dbCollectorThis != null)
                            {
                                loggerConsole.Info("Index List of Clients ({0} entities)", dbClientsRESTList.Count);

                                List<DBClient> dbClientList = new List<DBClient>(dbClientsRESTList.Count);

                                foreach (JToken dbClientJSON in dbClientsRESTList)
                                {
                                    if (dbClientJSON["clients"] != null)
                                    {
                                        foreach (JToken dbClientDetailJSON in dbClientJSON["clients"])
                                        {
                                            DBClient dbClient = new DBClient();
                                            dbClient.Controller = jobTarget.Controller;
                                            dbClient.CollectorName = dbCollectorThis.CollectorName;
                                            dbClient.CollectorType = dbCollectorThis.CollectorType;

                                            dbClient.CollectorStatus = dbCollectorThis.CollectorStatus;

                                            dbClient.AgentName = dbCollectorThis.AgentName;

                                            dbClient.Host = dbCollectorThis.Host;
                                            dbClient.Port = dbCollectorThis.Port;
                                            dbClient.UserName = dbCollectorThis.UserName;

                                            try { dbClient.ExecTime = Convert.ToInt64((Decimal)dbClientJSON["duration"]); } catch { }
                                            try { dbClient.ExecTimeSpan = new TimeSpan(dbClient.ExecTime * TimeSpan.TicksPerMillisecond); } catch { }
                                            try { dbClient.Weight = Math.Round((Decimal)dbClientJSON["weight"], 2); } catch { }

                                            try { dbClient.ClientName = dbClientDetailJSON["name"].ToString(); } catch { }
                                            try { dbClient.ClientID = (long)dbClientDetailJSON["id"]; } catch { }

                                            dbClient.Duration = (int)(jobConfiguration.Input.TimeRange.To - jobConfiguration.Input.TimeRange.From).Duration().TotalMinutes;
                                            dbClient.From = jobConfiguration.Input.TimeRange.From.ToLocalTime();
                                            dbClient.To = jobConfiguration.Input.TimeRange.To.ToLocalTime();
                                            dbClient.FromUtc = jobConfiguration.Input.TimeRange.From;
                                            dbClient.ToUtc = jobConfiguration.Input.TimeRange.To;

                                            dbClient.ConfigID = dbCollectorThis.ConfigID;
                                            dbClient.CollectorID = dbCollectorThis.CollectorID;

                                            dbClientList.Add(dbClient);
                                        }
                                    }
                                }

                                // Sort them
                                dbClientList = dbClientList.OrderBy(o => o.ClientName).ToList();
                                FileIOHelper.WriteListToCSVFile(dbClientList, new DBClientReportMap(), FilePathMap.DBClientsIndexFilePath(jobTarget));

                                stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + dbClientList.Count;
                            }
                        }

                        #endregion

                        #region Sessions

                        JArray dbSessionsRESTList = FileIOHelper.LoadJArrayFromFile(FilePathMap.DBSessionsDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));

                        if (dbSessionsRESTList != null && dbSessionsRESTList.Count > 0)
                        {
                            if (dbCollectorThis != null)
                            {
                                loggerConsole.Info("Index List of Sessions ({0} entities)", dbSessionsRESTList.Count);

                                List<DBSession> dbSessionList = new List<DBSession>(dbSessionsRESTList.Count);

                                foreach (JToken dbSessionJSON in dbSessionsRESTList)
                                {
                                    DBSession dbSession = new DBSession();
                                    dbSession.Controller = jobTarget.Controller;
                                    dbSession.CollectorName = dbCollectorThis.CollectorName;
                                    dbSession.CollectorType = dbCollectorThis.CollectorType;

                                    dbSession.CollectorStatus = dbCollectorThis.CollectorStatus;

                                    dbSession.AgentName = dbCollectorThis.AgentName;

                                    dbSession.Host = dbCollectorThis.Host;
                                    dbSession.Port = dbCollectorThis.Port;
                                    dbSession.UserName = dbCollectorThis.UserName;

                                    try { dbSession.ExecTime = Convert.ToInt64((Decimal)dbSessionJSON["duration"]); } catch { }
                                    try { dbSession.ExecTimeSpan = new TimeSpan(dbSession.ExecTime * TimeSpan.TicksPerMillisecond); } catch { }
                                    try { dbSession.Weight = Math.Round((Decimal)dbSessionJSON["weight"], 2); } catch { }

                                    try { dbSession.ClientName = dbSessionJSON["clientName"].ToString(); } catch { }
                                    try { dbSession.SessionName = dbSessionJSON["name"].ToString(); } catch { }
                                    try { dbSession.SessionID = (long)dbSessionJSON["id"]; } catch { }

                                    dbSession.Duration = (int)(jobConfiguration.Input.TimeRange.To - jobConfiguration.Input.TimeRange.From).Duration().TotalMinutes;
                                    dbSession.From = jobConfiguration.Input.TimeRange.From.ToLocalTime();
                                    dbSession.To = jobConfiguration.Input.TimeRange.To.ToLocalTime();
                                    dbSession.FromUtc = jobConfiguration.Input.TimeRange.From;
                                    dbSession.ToUtc = jobConfiguration.Input.TimeRange.To;

                                    dbSession.ConfigID = dbCollectorThis.ConfigID;
                                    dbSession.CollectorID = dbCollectorThis.CollectorID;

                                    dbSessionList.Add(dbSession);
                                }

                                // Sort them
                                dbSessionList = dbSessionList.OrderBy(o => o.SessionName).ToList();
                                FileIOHelper.WriteListToCSVFile(dbSessionList, new DBSessionReportMap(), FilePathMap.DBSessionsIndexFilePath(jobTarget));

                                stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + dbSessionList.Count;
                            }
                        }

                        #endregion

                        #region Blocking Sessions

                        JArray dbBlockingSessionsRESTList = FileIOHelper.LoadJArrayFromFile(FilePathMap.DBBlockingSessionsDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));

                        if (dbBlockingSessionsRESTList != null && dbBlockingSessionsRESTList.Count > 0)
                        {
                            if (dbCollectorThis != null)
                            {
                                loggerConsole.Info("Index List of Blocking Sessions ({0} entities)", dbBlockingSessionsRESTList.Count);

                                List<DBBlockingSession> dbBlockingSessionList = new List<DBBlockingSession>(dbBlockingSessionsRESTList.Count);

                                foreach (JToken dbBlockingSessionDetail in dbBlockingSessionsRESTList)
                                {
                                    long blockingSessionID = -1;
                                    try { blockingSessionID = (long)dbBlockingSessionDetail["blockingSessionId"]; } catch { }

                                    if (blockingSessionID == -1) continue;

                                    JArray dbBlockingSessionRESTList = FileIOHelper.LoadJArrayFromFile(FilePathMap.DBBlockingSessionDataFilePath(jobTarget, jobConfiguration.Input.TimeRange, blockingSessionID));

                                    if (dbBlockingSessionRESTList != null && dbBlockingSessionRESTList.Count > 0)
                                    {
                                        foreach (JToken dbBlockedQueryDetail in dbBlockingSessionRESTList)
                                        {
                                            DBBlockingSession dbBlockingSession = new DBBlockingSession();
                                            dbBlockingSession.Controller = jobTarget.Controller;
                                            dbBlockingSession.CollectorName = dbCollectorThis.CollectorName;
                                            dbBlockingSession.CollectorType = dbCollectorThis.CollectorType;

                                            dbBlockingSession.CollectorStatus = dbCollectorThis.CollectorStatus;

                                            dbBlockingSession.AgentName = dbCollectorThis.AgentName;

                                            dbBlockingSession.Host = dbCollectorThis.Host;
                                            dbBlockingSession.Port = dbCollectorThis.Port;
                                            dbBlockingSession.UserName = dbCollectorThis.UserName;

                                            try { dbBlockingSession.BlockingSessionName = dbBlockingSessionDetail["sessionId"].ToString(); } catch { }
                                            try { dbBlockingSession.BlockingClientName = dbBlockingSessionDetail["client"].ToString(); } catch { }
                                            try { dbBlockingSession.BlockingDBUserName = dbBlockingSessionDetail["user"].ToString(); } catch { }
                                            try { dbBlockingSession.BlockingSessionID = (long)dbBlockingSessionDetail["blockingSessionId"]; } catch { }

                                            try { dbBlockingSession.OtherSessionName = dbBlockedQueryDetail["sessionId"].ToString(); } catch { }
                                            try { dbBlockingSession.OtherClientName = dbBlockedQueryDetail["client"].ToString(); } catch { }
                                            try { dbBlockingSession.OtherDBUserName = dbBlockedQueryDetail["user"].ToString(); } catch { }

                                            try { dbBlockingSession.QueryHash = dbBlockedQueryDetail["queryHashCode"].ToString(); } catch { }
                                            try { dbBlockingSession.Query = dbBlockedQueryDetail["query"].ToString(); } catch { }

                                            try { dbBlockingSession.LockObject = dbBlockedQueryDetail["lockObject"].ToString(); } catch { }

                                            try { dbBlockingSession.BlockTime = (long)dbBlockedQueryDetail["duration"]; } catch { }
                                            try { dbBlockingSession.BlockTimeSpan = new TimeSpan(dbBlockingSession.BlockTime * TimeSpan.TicksPerMillisecond); } catch { }
                                            try { dbBlockingSession.FirstOccurrenceUtc = UnixTimeHelper.ConvertFromUnixTimestamp((long)dbBlockedQueryDetail["timeStamp"]); } catch { }
                                            try { dbBlockingSession.FirstOccurrence = dbBlockingSession.FirstOccurrenceUtc.ToLocalTime(); } catch { }

                                            dbBlockingSession.Duration = (int)(jobConfiguration.Input.TimeRange.To - jobConfiguration.Input.TimeRange.From).Duration().TotalMinutes;
                                            dbBlockingSession.From = jobConfiguration.Input.TimeRange.From.ToLocalTime();
                                            dbBlockingSession.To = jobConfiguration.Input.TimeRange.To.ToLocalTime();
                                            dbBlockingSession.FromUtc = jobConfiguration.Input.TimeRange.From;
                                            dbBlockingSession.ToUtc = jobConfiguration.Input.TimeRange.To;

                                            dbBlockingSession.ConfigID = dbCollectorThis.ConfigID;
                                            dbBlockingSession.CollectorID = dbCollectorThis.CollectorID;

                                            dbBlockingSessionList.Add(dbBlockingSession);
                                        }
                                    }
                                }

                                // Sort them
                                dbBlockingSessionList = dbBlockingSessionList.OrderBy(o => o.BlockingSessionName).ThenBy(o => o.OtherSessionName).ToList();
                                FileIOHelper.WriteListToCSVFile(dbBlockingSessionList, new DBBlockingSessionReportMap(), FilePathMap.DBBlockingSessionsIndexFilePath(jobTarget));

                                stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + dbBlockingSessionList.Count;
                            }
                        }

                        #endregion

                        #region Databases

                        JArray dbDatabasesRESTList = FileIOHelper.LoadJArrayFromFile(FilePathMap.DBDatabasesDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));

                        if (dbDatabasesRESTList != null && dbDatabasesRESTList.Count > 0)
                        {
                            if (dbCollectorThis != null)
                            {
                                loggerConsole.Info("Index List of Databases ({0} entities)", dbDatabasesRESTList.Count);

                                List<DBDatabase> dbDatabaseList = new List<DBDatabase>(dbDatabasesRESTList.Count);

                                foreach (JToken dbDatabaseJSON in dbDatabasesRESTList)
                                {
                                    DBDatabase dbDatabase = new DBDatabase();
                                    dbDatabase.Controller = jobTarget.Controller;
                                    dbDatabase.CollectorName = dbCollectorThis.CollectorName;
                                    dbDatabase.CollectorType = dbCollectorThis.CollectorType;

                                    dbDatabase.CollectorStatus = dbCollectorThis.CollectorStatus;

                                    dbDatabase.AgentName = dbCollectorThis.AgentName;

                                    dbDatabase.Host = dbCollectorThis.Host;
                                    dbDatabase.Port = dbCollectorThis.Port;
                                    dbDatabase.UserName = dbCollectorThis.UserName;

                                    try { dbDatabase.ExecTime = Convert.ToInt64((Decimal)dbDatabaseJSON["duration"]); } catch { }
                                    try { dbDatabase.ExecTimeSpan = new TimeSpan(dbDatabase.ExecTime * TimeSpan.TicksPerMillisecond); } catch { }
                                    try { dbDatabase.Weight = Math.Round((Decimal)dbDatabaseJSON["weight"], 2); } catch { }

                                    try { dbDatabase.DatabaseName = dbDatabaseJSON["name"].ToString(); } catch { }
                                    try { dbDatabase.DatabaseID = (long)dbDatabaseJSON["id"]; } catch { }

                                    dbDatabase.Duration = (int)(jobConfiguration.Input.TimeRange.To - jobConfiguration.Input.TimeRange.From).Duration().TotalMinutes;
                                    dbDatabase.From = jobConfiguration.Input.TimeRange.From.ToLocalTime();
                                    dbDatabase.To = jobConfiguration.Input.TimeRange.To.ToLocalTime();
                                    dbDatabase.FromUtc = jobConfiguration.Input.TimeRange.From;
                                    dbDatabase.ToUtc = jobConfiguration.Input.TimeRange.To;

                                    dbDatabase.ConfigID = dbCollectorThis.ConfigID;
                                    dbDatabase.CollectorID = dbCollectorThis.CollectorID;

                                    dbDatabaseList.Add(dbDatabase);
                                }

                                // Sort them
                                dbDatabaseList = dbDatabaseList.OrderBy(o => o.DatabaseName).ToList();
                                FileIOHelper.WriteListToCSVFile(dbDatabaseList, new DBDatabaseReportMap(), FilePathMap.DBDatabasesIndexFilePath(jobTarget));

                                stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + dbDatabaseList.Count;
                            }
                        }

                        #endregion

                        #region Users

                        JArray dbUsersRESTList = FileIOHelper.LoadJArrayFromFile(FilePathMap.DBUsersDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));

                        if (dbUsersRESTList != null && dbUsersRESTList.Count > 0)
                        {
                            if (dbCollectorThis != null)
                            {
                                loggerConsole.Info("Index List of Users ({0} entities)", dbUsersRESTList.Count);

                                List<DBUser> dbUserList = new List<DBUser>(dbUsersRESTList.Count);

                                foreach (JToken dbUserJSON in dbUsersRESTList)
                                {
                                    DBUser dbUser = new DBUser();
                                    dbUser.Controller = jobTarget.Controller;
                                    dbUser.CollectorName = dbCollectorThis.CollectorName;
                                    dbUser.CollectorType = dbCollectorThis.CollectorType;

                                    dbUser.CollectorStatus = dbCollectorThis.CollectorStatus;

                                    dbUser.AgentName = dbCollectorThis.AgentName;

                                    dbUser.Host = dbCollectorThis.Host;
                                    dbUser.Port = dbCollectorThis.Port;
                                    dbUser.UserName = dbCollectorThis.UserName;

                                    try { dbUser.ExecTime = Convert.ToInt64((Decimal)dbUserJSON["duration"]); } catch { }
                                    try { dbUser.ExecTimeSpan = new TimeSpan(dbUser.ExecTime * TimeSpan.TicksPerMillisecond); } catch { }
                                    try { dbUser.Weight = Math.Round((Decimal)dbUserJSON["weight"], 2); } catch { }

                                    try { dbUser.DBUserName = dbUserJSON["name"].ToString(); } catch { }
                                    try { dbUser.UserID = (long)dbUserJSON["id"]; } catch { }

                                    dbUser.Duration = (int)(jobConfiguration.Input.TimeRange.To - jobConfiguration.Input.TimeRange.From).Duration().TotalMinutes;
                                    dbUser.From = jobConfiguration.Input.TimeRange.From.ToLocalTime();
                                    dbUser.To = jobConfiguration.Input.TimeRange.To.ToLocalTime();
                                    dbUser.FromUtc = jobConfiguration.Input.TimeRange.From;
                                    dbUser.ToUtc = jobConfiguration.Input.TimeRange.To;

                                    dbUser.ConfigID = dbCollectorThis.ConfigID;
                                    dbUser.CollectorID = dbCollectorThis.CollectorID;

                                    // Some types of databases don't support users
                                    // Those have nothing in the "name" field
                                    // Do not add those
                                    if (dbUser.DBUserName.Length != 0) dbUserList.Add(dbUser);
                                }

                                // Sort them
                                dbUserList = dbUserList.OrderBy(o => o.UserName).ToList();
                                FileIOHelper.WriteListToCSVFile(dbUserList, new DBUserReportMap(), FilePathMap.DBUsersIndexFilePath(jobTarget));

                                stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + dbUserList.Count;
                            }
                        }

                        #endregion

                        #region Modules

                        JArray dbModulesRESTList = FileIOHelper.LoadJArrayFromFile(FilePathMap.DBModulesDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));

                        if (dbModulesRESTList != null && dbModulesRESTList.Count > 0)
                        {
                            if (dbCollectorThis != null)
                            {
                                loggerConsole.Info("Index List of Modules ({0} entities)", dbModulesRESTList.Count);

                                List<DBModule> dbModuleList = new List<DBModule>(dbModulesRESTList.Count);

                                foreach (JToken dbModuleJSON in dbModulesRESTList)
                                {
                                    DBModule dbModule = new DBModule();
                                    dbModule.Controller = jobTarget.Controller;
                                    dbModule.CollectorName = dbCollectorThis.CollectorName;
                                    dbModule.CollectorType = dbCollectorThis.CollectorType;

                                    dbModule.CollectorStatus = dbCollectorThis.CollectorStatus;

                                    dbModule.AgentName = dbCollectorThis.AgentName;

                                    dbModule.Host = dbCollectorThis.Host;
                                    dbModule.Port = dbCollectorThis.Port;
                                    dbModule.UserName = dbCollectorThis.UserName;

                                    try { dbModule.ExecTime = Convert.ToInt64((Decimal)dbModuleJSON["duration"]); } catch { }
                                    try { dbModule.ExecTimeSpan = new TimeSpan(dbModule.ExecTime * TimeSpan.TicksPerMillisecond); } catch { }
                                    try { dbModule.Weight = Math.Round((Decimal)dbModuleJSON["weight"], 2); } catch { }

                                    try { dbModule.ModuleName = dbModuleJSON["name"].ToString(); } catch { }
                                    try { dbModule.ModuleID = (long)dbModuleJSON["id"]; } catch { }

                                    dbModule.Duration = (int)(jobConfiguration.Input.TimeRange.To - jobConfiguration.Input.TimeRange.From).Duration().TotalMinutes;
                                    dbModule.From = jobConfiguration.Input.TimeRange.From.ToLocalTime();
                                    dbModule.To = jobConfiguration.Input.TimeRange.To.ToLocalTime();
                                    dbModule.FromUtc = jobConfiguration.Input.TimeRange.From;
                                    dbModule.ToUtc = jobConfiguration.Input.TimeRange.To;

                                    dbModule.ConfigID = dbCollectorThis.ConfigID;
                                    dbModule.CollectorID = dbCollectorThis.CollectorID;

                                    // Some types of databases don't support Modules
                                    // Those have nothing in the "name" field
                                    // Do not add those
                                    if (dbModule.ModuleName.Length != 0) dbModuleList.Add(dbModule);
                                }

                                // Sort them
                                dbModuleList = dbModuleList.OrderBy(o => o.ModuleName).ToList();
                                FileIOHelper.WriteListToCSVFile(dbModuleList, new DBModuleReportMap(), FilePathMap.DBModulesIndexFilePath(jobTarget));

                                stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + dbModuleList.Count;
                            }
                        }

                        #endregion

                        #region Programs

                        JArray dbProgramsRESTList = FileIOHelper.LoadJArrayFromFile(FilePathMap.DBProgramsDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));

                        if (dbProgramsRESTList != null && dbProgramsRESTList.Count > 0)
                        {
                            if (dbCollectorThis != null)
                            {
                                loggerConsole.Info("Index List of Programs ({0} entities)", dbProgramsRESTList.Count);

                                List<DBProgram> dbProgramList = new List<DBProgram>(dbProgramsRESTList.Count);

                                foreach (JToken dbProgramJSON in dbProgramsRESTList)
                                {
                                    DBProgram dbProgram = new DBProgram();
                                    dbProgram.Controller = jobTarget.Controller;
                                    dbProgram.CollectorName = dbCollectorThis.CollectorName;
                                    dbProgram.CollectorType = dbCollectorThis.CollectorType;

                                    dbProgram.CollectorStatus = dbCollectorThis.CollectorStatus;

                                    dbProgram.AgentName = dbCollectorThis.AgentName;

                                    dbProgram.Host = dbCollectorThis.Host;
                                    dbProgram.Port = dbCollectorThis.Port;
                                    dbProgram.UserName = dbCollectorThis.UserName;

                                    try { dbProgram.ExecTime = Convert.ToInt64((Decimal)dbProgramJSON["duration"]); } catch { }
                                    try { dbProgram.ExecTimeSpan = new TimeSpan(dbProgram.ExecTime * TimeSpan.TicksPerMillisecond); } catch { }
                                    try { dbProgram.Weight = Math.Round((Decimal)dbProgramJSON["weight"], 2); } catch { }

                                    try { dbProgram.ProgramName = dbProgramJSON["name"].ToString(); } catch { }
                                    try { dbProgram.ProgramID = (long)dbProgramJSON["id"]; } catch { }

                                    dbProgram.Duration = (int)(jobConfiguration.Input.TimeRange.To - jobConfiguration.Input.TimeRange.From).Duration().TotalMinutes;
                                    dbProgram.From = jobConfiguration.Input.TimeRange.From.ToLocalTime();
                                    dbProgram.To = jobConfiguration.Input.TimeRange.To.ToLocalTime();
                                    dbProgram.FromUtc = jobConfiguration.Input.TimeRange.From;
                                    dbProgram.ToUtc = jobConfiguration.Input.TimeRange.To;

                                    dbProgram.ConfigID = dbCollectorThis.ConfigID;
                                    dbProgram.CollectorID = dbCollectorThis.CollectorID;

                                    // Some types of databases don't support Programs
                                    // Those have nothing in the "name" field
                                    // Do not add those
                                    if (dbProgram.ProgramName.Length != 0) dbProgramList.Add(dbProgram);
                                }

                                // Sort them
                                dbProgramList = dbProgramList.OrderBy(o => o.ProgramName).ToList();
                                FileIOHelper.WriteListToCSVFile(dbProgramList, new DBProgramReportMap(), FilePathMap.DBProgramsIndexFilePath(jobTarget));

                                stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + dbProgramList.Count;
                            }
                        }

                        #endregion

                        #region BusinessTransactions

                        JArray dbBusinessTransactionsRESTList = FileIOHelper.LoadJArrayFromFile(FilePathMap.DBBusinessTransactionsDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));

                        if (dbBusinessTransactionsRESTList != null && dbBusinessTransactionsRESTList.Count > 0)
                        {
                            if (dbCollectorThis != null)
                            {
                                loggerConsole.Info("Index List of Business Transactions ({0} entities)", dbBusinessTransactionsRESTList.Count);

                                List<DBBusinessTransaction> dbBusinessTransactionList = new List<DBBusinessTransaction>(dbBusinessTransactionsRESTList.Count);

                                foreach (JToken dbBusinessTransactionJSON in dbBusinessTransactionsRESTList)
                                {
                                    DBBusinessTransaction dbBusinessTransaction = new DBBusinessTransaction();
                                    dbBusinessTransaction.Controller = jobTarget.Controller;
                                    dbBusinessTransaction.CollectorName = dbCollectorThis.CollectorName;
                                    dbBusinessTransaction.CollectorType = dbCollectorThis.CollectorType;

                                    dbBusinessTransaction.CollectorStatus = dbCollectorThis.CollectorStatus;

                                    dbBusinessTransaction.AgentName = dbCollectorThis.AgentName;

                                    dbBusinessTransaction.Host = dbCollectorThis.Host;
                                    dbBusinessTransaction.Port = dbCollectorThis.Port;
                                    dbBusinessTransaction.UserName = dbCollectorThis.UserName;

                                    try { dbBusinessTransaction.Calls = (long)dbBusinessTransactionJSON["hits"]; } catch { }
                                    try { dbBusinessTransaction.ExecTime = Convert.ToInt64((Decimal)dbBusinessTransactionJSON["duration"]); } catch { }
                                    try { dbBusinessTransaction.ExecTimeSpan = new TimeSpan(dbBusinessTransaction.ExecTime * TimeSpan.TicksPerMillisecond); } catch { }
                                    try { if (dbBusinessTransaction.Calls != 0) dbBusinessTransaction.AvgExecTime = dbBusinessTransaction.ExecTime / dbBusinessTransaction.Calls; } catch { }

                                    dbBusinessTransaction.AvgExecRange = getDurationRangeAsString(dbBusinessTransaction.AvgExecTime);

                                    try { dbBusinessTransaction.Weight = Math.Round((Decimal)dbBusinessTransactionJSON["weight"], 2); } catch { }

                                    try { dbBusinessTransaction.ApplicationName = dbBusinessTransactionJSON["appName"].ToString(); } catch { }
                                    try { dbBusinessTransaction.ApplicationID = (long)dbBusinessTransactionJSON["appId"]; } catch { }
                                    try { dbBusinessTransaction.BTName = dbBusinessTransactionJSON["name"].ToString(); } catch { }
                                    try { dbBusinessTransaction.BTID = (long)dbBusinessTransactionJSON["id"]; } catch { }

                                    dbBusinessTransaction.Duration = (int)(jobConfiguration.Input.TimeRange.To - jobConfiguration.Input.TimeRange.From).Duration().TotalMinutes;
                                    dbBusinessTransaction.From = jobConfiguration.Input.TimeRange.From.ToLocalTime();
                                    dbBusinessTransaction.To = jobConfiguration.Input.TimeRange.To.ToLocalTime();
                                    dbBusinessTransaction.FromUtc = jobConfiguration.Input.TimeRange.From;
                                    dbBusinessTransaction.ToUtc = jobConfiguration.Input.TimeRange.To;

                                    dbBusinessTransaction.ConfigID = dbCollectorThis.ConfigID;
                                    dbBusinessTransaction.CollectorID = dbCollectorThis.CollectorID;

                                    dbBusinessTransactionList.Add(dbBusinessTransaction);
                                }

                                // Sort them
                                dbBusinessTransactionList = dbBusinessTransactionList.OrderBy(o => o.ApplicationName).ThenBy(o => o.BTName).ToList();
                                FileIOHelper.WriteListToCSVFile(dbBusinessTransactionList, new DBBusinessTransactionReportMap(), FilePathMap.DBBusinessTransactionsIndexFilePath(jobTarget));

                                stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + dbBusinessTransactionList.Count;
                            }
                        }

                        #endregion

                        #region Combine All for Report CSV

                        // If it is the first one, clear out the combined folder
                        if (reportFolderCleaned == false)
                        {
                            FileIOHelper.DeleteFolder(FilePathMap.DBEntitiesReportFolderPath());
                            Thread.Sleep(1000);
                            FileIOHelper.CreateFolder(FilePathMap.DBEntitiesReportFolderPath());
                            reportFolderCleaned = true;
                        }

                        // Append all the individual application files into one
                        if (File.Exists(FilePathMap.DBWaitStatesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.DBWaitStatesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.DBWaitStatesReportFilePath(), FilePathMap.DBWaitStatesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.DBQueriesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.DBQueriesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.DBQueriesReportFilePath(), FilePathMap.DBQueriesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.DBClientsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.DBClientsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.DBClientsReportFilePath(), FilePathMap.DBClientsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.DBSessionsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.DBSessionsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.DBSessionsReportFilePath(), FilePathMap.DBSessionsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.DBBlockingSessionsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.DBBlockingSessionsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.DBBlockingSessionsReportFilePath(), FilePathMap.DBBlockingSessionsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.DBDatabasesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.DBDatabasesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.DBDatabasesReportFilePath(), FilePathMap.DBDatabasesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.DBUsersIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.DBUsersIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.DBUsersReportFilePath(), FilePathMap.DBUsersIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.DBModulesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.DBModulesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.DBModulesReportFilePath(), FilePathMap.DBModulesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.DBProgramsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.DBProgramsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.DBProgramsReportFilePath(), FilePathMap.DBProgramsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.DBBusinessTransactionsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.DBBusinessTransactionsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.DBBusinessTransactionsReportFilePath(), FilePathMap.DBBusinessTransactionsIndexFilePath(jobTarget));
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
                    if (File.Exists(FilePathMap.DBCollectorDefinitionsIndexFilePath(controllerGroup.ToList()[0])) == true && new FileInfo(FilePathMap.DBCollectorDefinitionsIndexFilePath(controllerGroup.ToList()[0])).Length > 0)
                    {
                        FileIOHelper.AppendTwoCSVFiles(FilePathMap.DBCollectorDefinitionsReportFilePath(), FilePathMap.DBCollectorDefinitionsIndexFilePath(controllerGroup.ToList()[0]));
                    }
                    if (File.Exists(FilePathMap.DBCollectorsIndexFilePath(controllerGroup.ToList()[0])) == true && new FileInfo(FilePathMap.DBCollectorsIndexFilePath(controllerGroup.ToList()[0])).Length > 0)
                    {
                        FileIOHelper.AppendTwoCSVFiles(FilePathMap.DBCollectorsReportFilePath(), FilePathMap.DBCollectorsIndexFilePath(controllerGroup.ToList()[0]));
                    }
                    if (File.Exists(FilePathMap.DBApplicationIndexFilePath(controllerGroup.ToList()[0])) == true && new FileInfo(FilePathMap.DBApplicationIndexFilePath(controllerGroup.ToList()[0])).Length > 0)
                    {
                        FileIOHelper.AppendTwoCSVFiles(FilePathMap.DBApplicationsReportFilePath(), FilePathMap.DBApplicationIndexFilePath(controllerGroup.ToList()[0]));
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
            logger.Trace("{0} is always executed", jobConfiguration.Status);
            loggerConsole.Trace("{0} is always executed", jobConfiguration.Status);
            return true;
        }

        private void updateEntityWithDeeplinks(DBEntityBase entityRow)
        {
            updateEntityWithDeeplinks(entityRow, null);
        }

        private void updateEntityWithDeeplinks(DBEntityBase entityRow, JobTimeRange jobTimeRange)
        {
            // Decide what kind of timerange
            string DEEPLINK_THIS_TIMERANGE = DEEPLINK_TIMERANGE_LAST_15_MINUTES;
            if (jobTimeRange != null)
            {
                long fromTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobTimeRange.From);
                long toTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobTimeRange.To);
                long differenceInMinutes = (toTimeUnix - fromTimeUnix) / (60000);
                DEEPLINK_THIS_TIMERANGE = String.Format(DEEPLINK_TIMERANGE_BETWEEN_TIMES, toTimeUnix, fromTimeUnix, differenceInMinutes);
            }

            // Determine what kind of entity we are dealing with and adjust accordingly
            if (entityRow is DBCollectorDefinition)
            {
                entityRow.ControllerLink = String.Format(DEEPLINK_CONTROLLER, entityRow.Controller, DEEPLINK_THIS_TIMERANGE);
                entityRow.ApplicationLink = String.Format(DEEPLINK_DBAPPLICATION, entityRow.Controller, DEEPLINK_THIS_TIMERANGE);
            }
            else if (entityRow is DBApplication)
            {
                entityRow.ControllerLink = String.Format(DEEPLINK_CONTROLLER, entityRow.Controller, DEEPLINK_THIS_TIMERANGE);
                entityRow.ApplicationLink = String.Format(DEEPLINK_DBAPPLICATION, entityRow.Controller, DEEPLINK_THIS_TIMERANGE);
            }
            else if (entityRow is DBCollector)
            {
                entityRow.ControllerLink = String.Format(DEEPLINK_CONTROLLER, entityRow.Controller, DEEPLINK_THIS_TIMERANGE);
                entityRow.ApplicationLink = String.Format(DEEPLINK_DBAPPLICATION, entityRow.Controller, DEEPLINK_THIS_TIMERANGE);
                entityRow.CollectorLink = String.Format(DEEPLINK_DBCOLLECTOR, entityRow.Controller, entityRow.CollectorID, DEEPLINK_THIS_TIMERANGE);
            }
            else if (entityRow is DBQuery)
            {
                DBQuery entity = (DBQuery)entityRow;
                entity.QueryLink = String.Format(DEEPLINK_DBQUERY, entityRow.Controller, entityRow.CollectorID, entity.QueryHash, DEEPLINK_THIS_TIMERANGE);
            }
            //else if (entityRow is Machine)
            //{
            //    Machine entity = (Machine)entityRow;
            //    entity.ControllerLink = String.Format(DEEPLINK_CONTROLLER, entity.Controller, DEEPLINK_THIS_TIMERANGE);
            //    entityRow.ApplicationLink = String.Format(DEEPLINK_SIMAPPLICATION, entityRow.Controller, DEEPLINK_THIS_TIMERANGE);
            //    entity.MachineLink = String.Format(DEEPLINK_SIMMACHINE, entity.Controller, entity.ApplicationID, entity.MachineID, DEEPLINK_THIS_TIMERANGE);
            //}
        }
    }
}
