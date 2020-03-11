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
    public class IndexDBEntities : JobStepIndexBase
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

                        int differenceInMinutes = (int)(jobConfiguration.Input.TimeRange.To - jobConfiguration.Input.TimeRange.From).Duration().TotalMinutes;

                        #region Database Collectors

                        List<DBCollector> dbCollectorsList = null;

                        if (File.Exists(FilePathMap.DBCollectorsIndexFilePath(jobTarget)) == false)
                        {
                            JObject dbCollectorsCallsContainerObject = FileIOHelper.LoadJObjectFromFile(FilePathMap.DBCollectorsCallsDataFilePath(jobTarget));
                            JObject dbCollectorsTimeSpentContainerObject = FileIOHelper.LoadJObjectFromFile(FilePathMap.DBCollectorsTimeSpentDataFilePath(jobTarget));
                            JArray dbCollectorDefinitionsArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.DBCollectorDefinitionsDataFilePath(jobTarget));

                            JArray dbCollectorsCallsArray = null;
                            JArray dbCollectorsTimeSpentArray = null;
                            if (isTokenPropertyNull(dbCollectorsCallsContainerObject, "data") == false)
                            {
                                dbCollectorsCallsArray = ((JArray)dbCollectorsCallsContainerObject["data"]);
                            }
                            if (isTokenPropertyNull(dbCollectorsTimeSpentContainerObject, "data") == false)
                            {
                                dbCollectorsTimeSpentArray = ((JArray)dbCollectorsTimeSpentContainerObject["data"]);
                            }

                            if (dbCollectorsCallsArray != null)
                            {
                                loggerConsole.Info("Index List of DB Collectors ({0} entities)", dbCollectorsCallsArray.Count);

                                dbCollectorsList = new List<DBCollector>(dbCollectorsCallsArray.Count);

                                foreach (JToken dbCollectorToken in dbCollectorsCallsArray)
                                {
                                    DBCollector dbCollector = new DBCollector();
                                    dbCollector.Controller = jobTarget.Controller;
                                    dbCollector.CollectorName = getStringValueFromJToken(dbCollectorToken, "name");
                                    dbCollector.CollectorType = getStringValueFromJToken(dbCollectorToken, "dbType");

                                    dbCollector.Role = getStringValueFromJToken(dbCollectorToken, "role");

                                    if (dbCollectorDefinitionsArray != null)
                                    {
                                        // Find the Collector Definition for this Collector
                                        JToken dbCollectorDefinitionToken = dbCollectorDefinitionsArray.Where(r => getLongValueFromJToken(r, "configId") == getLongValueFromJToken(dbCollectorToken, "configId")).FirstOrDefault();
                                        if (isTokenNull(dbCollectorDefinitionToken) == false)
                                        {
                                            if (isTokenPropertyNull(dbCollectorDefinitionToken, "config") == false)
                                            {
                                                JToken dbCollectorDefinitionConfigToken = dbCollectorDefinitionToken["config"];

                                                dbCollector.Host = getStringValueFromJToken(dbCollectorDefinitionConfigToken, "hostname");
                                                dbCollector.Port = getIntValueFromJToken(dbCollectorDefinitionConfigToken, "port");
                                                dbCollector.UserName = getStringValueFromJToken(dbCollectorDefinitionConfigToken, "username");

                                                dbCollector.AgentName = getStringValueFromJToken(dbCollectorDefinitionConfigToken, "agentName");
                                                dbCollector.CollectorStatus = getStringValueFromJToken(dbCollectorDefinitionToken, "collectorStatus");
                                            }
                                        }
                                    }

                                    // Performance data
                                    dbCollector.Calls = getLongValueFromJToken(dbCollectorToken["rolledUpMetricDatas"], "DB|KPI|Calls per Minute");

                                    try
                                    {
                                        JToken dbCollectorDefinitionWithTimeREST = dbCollectorsTimeSpentArray.Where(t => (long)t["id"] == (long)dbCollectorToken["id"]).FirstOrDefault();
                                        dbCollector.ExecTime = getLongValueFromJToken(dbCollectorDefinitionWithTimeREST["rolledUpMetricDatas"], "DB|KPI|Time Spent in Executions (s)");
                                        dbCollector.ExecTimeSpan = new TimeSpan(dbCollector.ExecTime * TimeSpan.TicksPerSecond);
                                    }
                                    catch { }

                                    dbCollector.Duration = differenceInMinutes;
                                    dbCollector.From = jobConfiguration.Input.TimeRange.From.ToLocalTime();
                                    dbCollector.To = jobConfiguration.Input.TimeRange.To.ToLocalTime();
                                    dbCollector.FromUtc = jobConfiguration.Input.TimeRange.From;
                                    dbCollector.ToUtc = jobConfiguration.Input.TimeRange.To;

                                    dbCollector.ConfigID = getLongValueFromJToken(dbCollectorToken, "configId");
                                    dbCollector.CollectorID = getLongValueFromJToken(dbCollectorToken, "id");

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

                        if (File.Exists(FilePathMap.DBApplicationsIndexFilePath(jobTarget)) == false)
                        {
                            loggerConsole.Info("Index Application");

                            stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + 1;

                            DBApplication application = new DBApplication();
                            application.Controller = jobTarget.Controller;
                            application.ControllerLink = String.Format(DEEPLINK_CONTROLLER, application.Controller, DEEPLINK_TIMERANGE_LAST_15_MINUTES);
                            application.ApplicationName = "Database Monitoring";
                            application.ApplicationLink = String.Format(DEEPLINK_DB_APPLICATION, application.Controller, DEEPLINK_TIMERANGE_LAST_15_MINUTES);
                            application.ApplicationID = jobTarget.ApplicationID;
                            if (dbCollectorsList != null)
                            {
                                application.NumCollectors = dbCollectorsList.Count;

                                application.NumOracle = dbCollectorsList.Count(d => d.CollectorType == "ORACLE");
                                application.NumSQLServer = dbCollectorsList.Count(d => d.CollectorType == "MSSQL");
                                application.NumMySQL = dbCollectorsList.Count(d => d.CollectorType == "MYSQL");
                                application.NumMongo = dbCollectorsList.Count(d => d.CollectorType == "MONGO");
                                application.NumPostgres = dbCollectorsList.Count(d => d.CollectorType == "POSTGRESQL");
                                application.NumDB2 = dbCollectorsList.Count(d => d.CollectorType == "DB2");
                                application.NumSybase = dbCollectorsList.Count(d => d.CollectorType == "SYBASE");
                                application.NumOther = dbCollectorsList.Count - (application.NumOracle + application.NumSQLServer + application.NumMySQL + application.NumMongo + application.NumPostgres + application.NumDB2 + application.NumSybase);

                            }

                            updateEntityWithDeeplinks(application, jobConfiguration.Input.TimeRange);

                            List<DBApplication> applicationsList = new List<DBApplication>(1);
                            applicationsList.Add(application);

                            FileIOHelper.WriteListToCSVFile(applicationsList, new DBApplicationReportMap(), FilePathMap.DBApplicationsIndexFilePath(jobTarget));
                        }

                        #endregion

                        #region Wait States

                        // Find the Collector Definition for this Collector
                        DBCollector dbCollectorThis = dbCollectorsList.Where(d => d.CollectorID == jobTarget.DBCollectorID).FirstOrDefault();

                        JArray allDBWaitStatesArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.DBAllWaitStatesDataFilePath(jobTarget));
                        JObject currentDBWaitStatesContainerObject = FileIOHelper.LoadJObjectFromFile(FilePathMap.DBCurrentWaitStatesDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));

                        if (allDBWaitStatesArray != null && allDBWaitStatesArray.Count > 0 && currentDBWaitStatesContainerObject != null)
                        {
                            if (dbCollectorThis != null)
                            {
                                loggerConsole.Info("Index List of Wait States");

                                List<DBWaitState> waitStatesList = new List<DBWaitState>(allDBWaitStatesArray.Count / 10);

                                foreach (JToken currentWaitStateToken in currentDBWaitStatesContainerObject["waitStateMap"])
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

                                    dbWaitState.State = ((JProperty)currentWaitStateToken).Name;
                                    dbWaitState.ExecTime = (long)((JProperty)currentWaitStateToken).Value;
                                    dbWaitState.ExecTimeSpan = new TimeSpan(dbWaitState.ExecTime * TimeSpan.TicksPerMillisecond);

                                    try
                                    {
                                        JToken dbWaitStateWithID = allDBWaitStatesArray.Where(w => w["name"].ToString().ToLower() == dbWaitState.State.ToLower()).FirstOrDefault();
                                        dbWaitState.WaitStateID = getLongValueFromJToken(dbWaitStateWithID, "id");
                                    }
                                    catch
                                    {
                                        dbWaitState.WaitStateID = -1;
                                    }

                                    dbWaitState.Duration = differenceInMinutes;
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

                        JArray dbQueriesArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.DBQueriesDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));

                        if (dbQueriesArray != null && dbQueriesArray.Count > 0)
                        {
                            if (dbCollectorThis != null)
                            {
                                loggerConsole.Info("Index List of Queries ({0} entities)", dbQueriesArray.Count);

                                List<DBQuery> dbQueryList = new List<DBQuery>(dbQueriesArray.Count);

                                foreach (JToken dbQueryToken in dbQueriesArray)
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

                                    dbQuery.Calls = getLongValueFromJToken(dbQueryToken, "hits");
                                    try { dbQuery.ExecTime = Convert.ToInt64((Decimal)dbQueryToken["duration"]); } catch { }
                                    dbQuery.ExecTimeSpan = new TimeSpan(dbQuery.ExecTime * TimeSpan.TicksPerMillisecond);
                                    dbQuery.AvgExecTime = 0;
                                    if (dbQuery.Calls != 0) dbQuery.AvgExecTime = dbQuery.ExecTime / dbQuery.Calls;

                                    dbQuery.AvgExecRange = getDurationRangeAsString(dbQuery.AvgExecTime);

                                    try { dbQuery.Weight = Math.Round((Decimal)dbQueryToken["weight"], 2); } catch { }

                                    dbQuery.QueryHash = getStringValueFromJToken(dbQueryToken, "queryHashCode");
                                    dbQuery.Query = getStringValueFromJToken(dbQueryToken, "queryText");

                                    dbQuery.Name = getStringValueFromJToken(dbQueryToken, "name");
                                    dbQuery.Namespace = getStringValueFromJToken(dbQueryToken, "namespace");
                                    dbQuery.Client = getStringValueFromJToken(dbQueryToken, "clientName");

                                    dbQuery.IsSnapWindowData = getBoolValueFromJToken(dbQueryToken, "snapshotWindowData");
                                    dbQuery.IsSnapCorrData = getBoolValueFromJToken(dbQueryToken, "snapshotCorrelationData");

                                    dbQuery.QueryID = getLongValueFromJToken(dbQueryToken, "id");

                                    // Get SQL statement type
                                    dbQuery.SQLClauseType = getSQLClauseType(dbQuery.Query, 100);

                                    // Check other clauses
                                    dbQuery.SQLWhere = doesSQLStatementContain(dbQuery.Query, @"\bWHERE\s");
                                    dbQuery.SQLGroupBy = doesSQLStatementContain(dbQuery.Query, @"\bGROUP BY\s");
                                    dbQuery.SQLOrderBy = doesSQLStatementContain(dbQuery.Query, @"\bORDER BY\s");
                                    dbQuery.SQLHaving = doesSQLStatementContain(dbQuery.Query, @"\bHAVING\s");
                                    dbQuery.SQLUnion = doesSQLStatementContain(dbQuery.Query, @"\bUNION\s");

                                    // Get join type if present
                                    dbQuery.SQLJoinType = getSQLJoinType(dbQuery.Query);

                                    // Now parse the tables from SQL statement
                                    Tuple<string, string, int> parsedTables = null;
                                    switch (dbQuery.SQLClauseType)
                                    {
                                        case "SELECT":
                                            parsedTables = parseSQLTablesFromSELECT(dbQuery.Query);

                                            break;

                                        case "INSERT":
                                            parsedTables = parseSQLTablesFromINSERT(dbQuery.Query);
                                            break;

                                        case "UPDATE":
                                            parsedTables = parseSQLTablesFromUPDATE(dbQuery.Query);
                                            break;

                                        case "DELETE":
                                            parsedTables = parseSQLTablesFromDELETE(dbQuery.Query);
                                            break;

                                        case "DROP":
                                            parsedTables = parseSQLTablesFromDROP(dbQuery.Query);
                                            break;

                                        case "TRUNCATE":
                                            parsedTables = parseSQLTablesFromTRUNCATE(dbQuery.Query);
                                            break;

                                        default:
                                            break;
                                    }
                                    if (parsedTables != null)
                                    {
                                        dbQuery.SQLTable = parsedTables.Item1;
                                        dbQuery.SQLTables = parsedTables.Item2;
                                        dbQuery.NumSQLTables = parsedTables.Item3;
                                    }

                                    dbQuery.Duration = differenceInMinutes;
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

                        JArray dbClientsArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.DBClientsDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));

                        if (dbClientsArray != null && dbClientsArray.Count > 0)
                        {
                            if (dbCollectorThis != null)
                            {
                                loggerConsole.Info("Index List of Clients ({0} entities)", dbClientsArray.Count);

                                List<DBClient> dbClientList = new List<DBClient>(dbClientsArray.Count);

                                foreach (JToken dbClientToken in dbClientsArray)
                                {
                                    if (dbClientToken["clients"] != null)
                                    {
                                        foreach (JToken dbClientDetailJSON in dbClientToken["clients"])
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

                                            try { dbClient.ExecTime = Convert.ToInt64((Decimal)dbClientToken["duration"]); } catch { }
                                            dbClient.ExecTimeSpan = new TimeSpan(dbClient.ExecTime * TimeSpan.TicksPerMillisecond);
                                            try { dbClient.Weight = Math.Round((Decimal)dbClientToken["weight"], 2); } catch { }

                                            dbClient.ClientName = getStringValueFromJToken(dbClientDetailJSON, "name");
                                            dbClient.ClientID = getLongValueFromJToken(dbClientDetailJSON, "id");

                                            dbClient.Duration = differenceInMinutes;
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

                        JArray dbSessionsArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.DBSessionsDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));

                        if (dbSessionsArray != null && dbSessionsArray.Count > 0)
                        {
                            if (dbCollectorThis != null)
                            {
                                loggerConsole.Info("Index List of Sessions ({0} entities)", dbSessionsArray.Count);

                                List<DBSession> dbSessionList = new List<DBSession>(dbSessionsArray.Count);

                                foreach (JToken dbSessionToken in dbSessionsArray)
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

                                    try { dbSession.ExecTime = Convert.ToInt64((Decimal)dbSessionToken["duration"]); } catch { }
                                    dbSession.ExecTimeSpan = new TimeSpan(dbSession.ExecTime * TimeSpan.TicksPerMillisecond);
                                    try { dbSession.Weight = Math.Round((Decimal)dbSessionToken["weight"], 2); } catch { }

                                    dbSession.ClientName = getStringValueFromJToken(dbSessionToken, "clientName");
                                    dbSession.SessionName = getStringValueFromJToken(dbSessionToken, "name");
                                    dbSession.SessionID = getLongValueFromJToken(dbSessionToken, "id");

                                    dbSession.Duration = differenceInMinutes;
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

                        JArray dbBlockingSessionsArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.DBBlockingSessionsDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));

                        if (dbBlockingSessionsArray != null && dbBlockingSessionsArray.Count > 0)
                        {
                            if (dbCollectorThis != null)
                            {
                                loggerConsole.Info("Index List of Blocking Sessions ({0} entities)", dbBlockingSessionsArray.Count);

                                List<DBBlockingSession> dbBlockingSessionList = new List<DBBlockingSession>(dbBlockingSessionsArray.Count);

                                foreach (JToken dbBlockingSessionDetailToken in dbBlockingSessionsArray)
                                {
                                    long blockingSessionID = getLongValueFromJToken(dbBlockingSessionDetailToken, "blockingSessionId");
                                    if (blockingSessionID == 0) continue;

                                    JArray dbBlockingSessionArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.DBBlockingSessionDataFilePath(jobTarget, jobConfiguration.Input.TimeRange, blockingSessionID));

                                    if (dbBlockingSessionArray != null && dbBlockingSessionArray.Count > 0)
                                    {
                                        foreach (JToken dbBlockedQueryDetail in dbBlockingSessionArray)
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

                                            dbBlockingSession.BlockingSessionName = getStringValueFromJToken(dbBlockingSessionDetailToken, "sessionId");
                                            dbBlockingSession.BlockingClientName = getStringValueFromJToken(dbBlockingSessionDetailToken, "client");
                                            dbBlockingSession.BlockingDBUserName = getStringValueFromJToken(dbBlockingSessionDetailToken, "user");
                                            dbBlockingSession.BlockingSessionID = getLongValueFromJToken(dbBlockingSessionDetailToken, "blockingSessionId");

                                            dbBlockingSession.OtherSessionName = getStringValueFromJToken(dbBlockedQueryDetail, "sessionId");
                                            dbBlockingSession.OtherClientName = getStringValueFromJToken(dbBlockedQueryDetail, "client");
                                            dbBlockingSession.OtherDBUserName = getStringValueFromJToken(dbBlockedQueryDetail, "user");

                                            dbBlockingSession.QueryHash = getStringValueFromJToken(dbBlockedQueryDetail, "queryHashCode");
                                            dbBlockingSession.Query = getStringValueFromJToken(dbBlockedQueryDetail, "query");

                                            dbBlockingSession.LockObject = getStringValueFromJToken(dbBlockedQueryDetail, "lockObject");

                                            dbBlockingSession.BlockTime = getLongValueFromJToken(dbBlockedQueryDetail, "duration");
                                            dbBlockingSession.BlockTimeSpan = new TimeSpan(dbBlockingSession.BlockTime * TimeSpan.TicksPerMillisecond);
                                            dbBlockingSession.FirstOccurrenceUtc = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(dbBlockedQueryDetail, "timeStamp"));
                                            try { dbBlockingSession.FirstOccurrence = dbBlockingSession.FirstOccurrenceUtc.ToLocalTime(); } catch { }

                                            dbBlockingSession.Duration = differenceInMinutes;
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

                        JArray dbDatabasesArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.DBDatabasesDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));

                        if (dbDatabasesArray != null && dbDatabasesArray.Count > 0)
                        {
                            if (dbCollectorThis != null)
                            {
                                loggerConsole.Info("Index List of Databases ({0} entities)", dbDatabasesArray.Count);

                                List<DBDatabase> dbDatabaseList = new List<DBDatabase>(dbDatabasesArray.Count);

                                foreach (JToken dbDatabaseToken in dbDatabasesArray)
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

                                    try { dbDatabase.ExecTime = Convert.ToInt64((Decimal)dbDatabaseToken["duration"]); } catch { }
                                    dbDatabase.ExecTimeSpan = new TimeSpan(dbDatabase.ExecTime * TimeSpan.TicksPerMillisecond);
                                    try { dbDatabase.Weight = Math.Round((Decimal)dbDatabaseToken["weight"], 2); } catch { }

                                    dbDatabase.DatabaseName = getStringValueFromJToken(dbDatabaseToken, "name");
                                    dbDatabase.DatabaseID = getLongValueFromJToken(dbDatabaseToken, "id");

                                    dbDatabase.Duration = differenceInMinutes;
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

                        JArray dbUsersArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.DBUsersDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));

                        if (dbUsersArray != null && dbUsersArray.Count > 0)
                        {
                            if (dbCollectorThis != null)
                            {
                                loggerConsole.Info("Index List of Users ({0} entities)", dbUsersArray.Count);

                                List<DBUser> dbUserList = new List<DBUser>(dbUsersArray.Count);

                                foreach (JToken dbUserToken in dbUsersArray)
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

                                    try { dbUser.ExecTime = Convert.ToInt64((Decimal)dbUserToken["duration"]); } catch { }
                                    dbUser.ExecTimeSpan = new TimeSpan(dbUser.ExecTime * TimeSpan.TicksPerMillisecond);
                                    try { dbUser.Weight = Math.Round((Decimal)dbUserToken["weight"], 2); } catch { }

                                    dbUser.DBUserName = getStringValueFromJToken(dbUserToken, "name");
                                    dbUser.UserID = getLongValueFromJToken(dbUserToken, "id");

                                    dbUser.Duration = differenceInMinutes;
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

                        JArray dbModulesArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.DBModulesDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));

                        if (dbModulesArray != null && dbModulesArray.Count > 0)
                        {
                            if (dbCollectorThis != null)
                            {
                                loggerConsole.Info("Index List of Modules ({0} entities)", dbModulesArray.Count);

                                List<DBModule> dbModuleList = new List<DBModule>(dbModulesArray.Count);

                                foreach (JToken dbModuleToken in dbModulesArray)
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

                                    try { dbModule.ExecTime = Convert.ToInt64((Decimal)dbModuleToken["duration"]); } catch { }
                                    dbModule.ExecTimeSpan = new TimeSpan(dbModule.ExecTime * TimeSpan.TicksPerMillisecond);
                                    try { dbModule.Weight = Math.Round((Decimal)dbModuleToken["weight"], 2); } catch { }

                                    dbModule.ModuleName = getStringValueFromJToken(dbModuleToken, "name");
                                    dbModule.ModuleID = getLongValueFromJToken(dbModuleToken, "id");

                                    dbModule.Duration = differenceInMinutes;
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

                        JArray dbProgramsArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.DBProgramsDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));

                        if (dbProgramsArray != null && dbProgramsArray.Count > 0)
                        {
                            if (dbCollectorThis != null)
                            {
                                loggerConsole.Info("Index List of Programs ({0} entities)", dbProgramsArray.Count);

                                List<DBProgram> dbProgramList = new List<DBProgram>(dbProgramsArray.Count);

                                foreach (JToken dbProgramToken in dbProgramsArray)
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

                                    try { dbProgram.ExecTime = Convert.ToInt64((Decimal)dbProgramToken["duration"]); } catch { }
                                    dbProgram.ExecTimeSpan = new TimeSpan(dbProgram.ExecTime * TimeSpan.TicksPerMillisecond);
                                    try { dbProgram.Weight = Math.Round((Decimal)dbProgramToken["weight"], 2); } catch { }

                                    dbProgram.ProgramName = getStringValueFromJToken(dbProgramToken, "name");
                                    dbProgram.ProgramID = getLongValueFromJToken(dbProgramToken, "id");

                                    dbProgram.Duration = differenceInMinutes;
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

                        JArray dbBusinessTransactionsArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.DBBusinessTransactionsDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));

                        if (dbBusinessTransactionsArray != null && dbBusinessTransactionsArray.Count > 0)
                        {
                            if (dbCollectorThis != null)
                            {
                                loggerConsole.Info("Index List of Business Transactions ({0} entities)", dbBusinessTransactionsArray.Count);

                                List<DBBusinessTransaction> dbBusinessTransactionList = new List<DBBusinessTransaction>(dbBusinessTransactionsArray.Count);

                                foreach (JToken dbBusinessTransactionToken in dbBusinessTransactionsArray)
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

                                    dbBusinessTransaction.Calls = getLongValueFromJToken(dbBusinessTransactionToken, "hits");
                                    try { dbBusinessTransaction.ExecTime = Convert.ToInt64((Decimal)dbBusinessTransactionToken["duration"]); } catch { }
                                    dbBusinessTransaction.ExecTimeSpan = new TimeSpan(dbBusinessTransaction.ExecTime * TimeSpan.TicksPerMillisecond);
                                    if (dbBusinessTransaction.Calls != 0) dbBusinessTransaction.AvgExecTime = dbBusinessTransaction.ExecTime / dbBusinessTransaction.Calls;

                                    dbBusinessTransaction.AvgExecRange = getDurationRangeAsString(dbBusinessTransaction.AvgExecTime);

                                    try { dbBusinessTransaction.Weight = Math.Round((Decimal)dbBusinessTransactionToken["weight"], 2); } catch { }

                                    dbBusinessTransaction.ApplicationName = getStringValueFromJToken(dbBusinessTransactionToken, "appName");
                                    dbBusinessTransaction.ApplicationID = getLongValueFromJToken(dbBusinessTransactionToken, "appId");
                                    dbBusinessTransaction.BTName = getStringValueFromJToken(dbBusinessTransactionToken, "name");
                                    dbBusinessTransaction.BTID = getLongValueFromJToken(dbBusinessTransactionToken, "id");

                                    dbBusinessTransaction.Duration = differenceInMinutes;
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

                        // Append all the individual report files into one
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
                var controllers = jobConfiguration.Target.Where(t => t.Type == APPLICATION_TYPE_DB).GroupBy(t => t.Controller);
                foreach (var controllerGroup in controllers)
                {
                    if (File.Exists(FilePathMap.DBCollectorsIndexFilePath(controllerGroup.ToList()[0])) == true && new FileInfo(FilePathMap.DBCollectorsIndexFilePath(controllerGroup.ToList()[0])).Length > 0)
                    {
                        FileIOHelper.AppendTwoCSVFiles(FilePathMap.DBCollectorsReportFilePath(), FilePathMap.DBCollectorsIndexFilePath(controllerGroup.ToList()[0]));
                    }
                    if (File.Exists(FilePathMap.DBApplicationsIndexFilePath(controllerGroup.ToList()[0])) == true && new FileInfo(FilePathMap.DBApplicationsIndexFilePath(controllerGroup.ToList()[0])).Length > 0)
                    {
                        FileIOHelper.AppendTwoCSVFiles(FilePathMap.DBApplicationsReportFilePath(), FilePathMap.DBApplicationsIndexFilePath(controllerGroup.ToList()[0]));
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
            logger.Trace("Input.DetectedEntities={0}", jobConfiguration.Input.DetectedEntities);
            loggerConsole.Trace("Input.DetectedEntities={0}", jobConfiguration.Input.DetectedEntities);
            if (jobConfiguration.Input.DetectedEntities == false)
            {
                loggerConsole.Trace("Skipping index of detected entities");
            }
            return (jobConfiguration.Input.DetectedEntities == true);
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
            if (entityRow is DBApplication)
            {
                entityRow.ControllerLink = String.Format(DEEPLINK_CONTROLLER, entityRow.Controller, DEEPLINK_THIS_TIMERANGE);
                entityRow.ApplicationLink = String.Format(DEEPLINK_DB_APPLICATION, entityRow.Controller, DEEPLINK_THIS_TIMERANGE);
            }
            else if (entityRow is DBCollector)
            {
                entityRow.ControllerLink = String.Format(DEEPLINK_CONTROLLER, entityRow.Controller, DEEPLINK_THIS_TIMERANGE);
                entityRow.ApplicationLink = String.Format(DEEPLINK_DB_APPLICATION, entityRow.Controller, DEEPLINK_THIS_TIMERANGE);
                entityRow.CollectorLink = String.Format(DEEPLINK_DB_COLLECTOR, entityRow.Controller, entityRow.CollectorID, DEEPLINK_THIS_TIMERANGE);
            }
            else if (entityRow is DBQuery)
            {
                DBQuery entity = (DBQuery)entityRow;
                entity.QueryLink = String.Format(DEEPLINK_DB_QUERY, entityRow.Controller, entityRow.CollectorID, entity.QueryHash, DEEPLINK_THIS_TIMERANGE);
            }
        }
    }
}
