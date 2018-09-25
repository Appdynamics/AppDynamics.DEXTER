using AppDynamics.Dexter.ReportObjects;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class JobStepIndexBase : JobStepBase
    {
        #region Constants for Deeplinks

        // APM links
        internal const string DEEPLINK_CONTROLLER = @"{0}/controller/#/location=AD_HOME_OVERVIEW&timeRange={1}";
        internal const string DEEPLINK_APPLICATION = @"{0}/controller/#/location=APP_DASHBOARD&timeRange={2}&application={1}&dashboardMode=force";
        internal const string DEEPLINK_TIER = @"{0}/controller/#/location=APP_COMPONENT_MANAGER&timeRange={3}&application={1}&component={2}&dashboardMode=force";
        internal const string DEEPLINK_NODE = @"{0}/controller/#/location=APP_NODE_MANAGER&timeRange={3}&application={1}&node={2}&dashboardMode=force";
        internal const string DEEPLINK_BACKEND = @"{0}/controller/#/location=APP_BACKEND_DASHBOARD&timeRange={3}&application={1}&backendDashboard={2}&dashboardMode=force";
        internal const string DEEPLINK_BUSINESS_TRANSACTION = @"{0}/controller/#/location=APP_BT_DETAIL&timeRange={3}&application={1}&businessTransaction={2}&dashboardMode=force";
        internal const string DEEPLINK_SERVICE_ENDPOINT = @"{0}/controller/#/location=APP_SERVICE_ENDPOINT_DETAIL&timeRange={4}&application={1}&component={2}&serviceEndpoint={3}";
        internal const string DEEPLINK_ERROR = @"{0}/controller/#/location=APP_ERROR_DASHBOARD&timeRange={3}&application={1}&error={2}";
        internal const string DEEPLINK_INFORMATION_POINT = @"{0}/controller/#/location=APP_INFOPOINT_DASHBOARD&timeRange={3}&application={1}&infoPoint={2}";
        internal const string DEEPLINK_APPLICATION_MOBILE = @"{0}/controller/#/location=EUM_MOBILE_MAIN_DASHBOARD&timeRange={3}&application={1}&mobileApp={2}";
        internal const string DEEPLINK_HEALTH_RULE = @"{0}/controller/#/location=ALERT_RESPOND_HEALTH_RULES&timeRange={3}&application={1}";
        internal const string DEEPLINK_INCIDENT = @"{0}/controller/#/location=APP_INCIDENT_DETAIL_MODAL&timeRange={4}&application={1}&incident={2}&incidentTime={3}";
        internal const string DEEPLINK_SNAPSHOT_OVERVIEW = @"{0}/controller/#/location=APP_SNAPSHOT_VIEWER&rsdTime={3}&application={1}&requestGUID={2}&tab=overview&dashboardMode=force";
        internal const string DEEPLINK_SNAPSHOT_SEGMENT = @"{0}/controller/#/location=APP_SNAPSHOT_VIEWER&rsdTime={4}&application={1}&requestGUID={2}&tab={3}&dashboardMode=force";

        // SIM links
        internal const string DEEPLINK_SIMAPPLICATION = @"{0}/controller/#/location=SERVER_MONITORING_MACHINE_LIST&timeRange={1}";
        internal const string DEEPLINK_SIMMACHINE = @"{0}/controller/#/location=SERVER_MONITORING_MACHINE_OVERVIEW&timeRange={2}&machineId={1}";

        internal const string DEEPLINK_METRIC = @"{0}/controller/#/location=METRIC_BROWSER&timeRange={3}&application={1}&metrics={2}";
        internal const string DEEPLINK_TIMERANGE_LAST_15_MINUTES = "last_15_minutes.BEFORE_NOW.-1.-1.15";
        internal const string DEEPLINK_TIMERANGE_BETWEEN_TIMES = "Custom_Time_Range.BETWEEN_TIMES.{0}.{1}.{2}";
        internal const string DEEPLINK_METRIC_APPLICATION_TARGET_METRIC_ID = "APPLICATION.{0}.{1}";
        internal const string DEEPLINK_METRIC_TIER_TARGET_METRIC_ID = "APPLICATION_COMPONENT.{0}.{1}";
        internal const string DEEPLINK_METRIC_NODE_TARGET_METRIC_ID = "APPLICATION_COMPONENT_NODE.{0}.{1}";

        // DB Links
        internal const string DEEPLINK_DBAPPLICATION = @"{0}/controller/#/location=DB_MONITORING_SERVER_LIST&timeRange={1}";
        internal const string DEEPLINK_DBCOLLECTOR = @"{0}/controller/#/location=DB_MONITORING_SERVER_DASHBOARD&timeRange={2}&databaseId={1}";
        internal const string DEEPLINK_DBQUERY = @"{0}/controller/#/location=DB_MONITORING_QUERY_DETAILS&timeRange={3}&databaseId={1}&&queryHashCode={2}";

        #endregion

        #region Constants for various strings mapping to Entity types in Flowmaps and Events

        internal const string ENTITY_TYPE_FLOWMAP_APPLICATION = "APPLICATION";
        internal const string ENTITY_TYPE_FLOWMAP_APPLICATION_MOBILE = "MOBILE_APPLICATION";
        internal const string ENTITY_TYPE_FLOWMAP_TIER = "APPLICATION_COMPONENT";
        internal const string ENTITY_TYPE_FLOWMAP_NODE = "APPLICATION_COMPONENT_NODE";
        internal const string ENTITY_TYPE_FLOWMAP_MACHINE = "MACHINE_INSTANCE";
        internal const string ENTITY_TYPE_FLOWMAP_BUSINESS_TRANSACTION = "BUSINESS_TRANSACTION";
        internal const string ENTITY_TYPE_FLOWMAP_BACKEND = "BACKEND";
        internal const string ENTITY_TYPE_FLOWMAP_HEALTH_RULE = "POLICY";

        // Mapping of long entity types to human readable ones
        internal static Dictionary<string, string> entityTypeStringMapping = new Dictionary<string, string>
        {
            {ENTITY_TYPE_FLOWMAP_APPLICATION, APMApplication.ENTITY_TYPE},
            {ENTITY_TYPE_FLOWMAP_APPLICATION_MOBILE, "Mobile App"},
            {ENTITY_TYPE_FLOWMAP_TIER, APMTier.ENTITY_TYPE},
            {ENTITY_TYPE_FLOWMAP_NODE, APMNode.ENTITY_TYPE},
            {ENTITY_TYPE_FLOWMAP_MACHINE, "Machine"},
            {ENTITY_TYPE_FLOWMAP_BUSINESS_TRANSACTION, BusinessTransaction.ENTITY_TYPE},
            {ENTITY_TYPE_FLOWMAP_BACKEND, Backend.ENTITY_TYPE },
            {ENTITY_TYPE_FLOWMAP_HEALTH_RULE, "Health Rule"}
        };

        #endregion

        internal void updateEntityWithDeeplinks(APMEntityBase entityRow)
        {
            updateEntityWithDeeplinks(entityRow, null);
        }

        internal void updateEntityWithDeeplinks(APMEntityBase entityRow, JobTimeRange jobTimeRange)
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
            string deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_APPLICATION_TARGET_METRIC_ID;
            long entityIdForMetricBrowser = entityRow.ApplicationID;
            if (entityRow is APMApplication)
            {
                entityRow.ControllerLink = String.Format(DEEPLINK_CONTROLLER, entityRow.Controller, DEEPLINK_THIS_TIMERANGE);
                entityRow.ApplicationLink = String.Format(DEEPLINK_APPLICATION, entityRow.Controller, entityRow.ApplicationID, DEEPLINK_THIS_TIMERANGE);
            }
            else if (entityRow is APMTier)
            {
                APMTier entity = (APMTier)entityRow;
                entity.ControllerLink = String.Format(DEEPLINK_CONTROLLER, entity.Controller, DEEPLINK_THIS_TIMERANGE);
                entity.ApplicationLink = String.Format(DEEPLINK_APPLICATION, entity.Controller, entity.ApplicationID, DEEPLINK_THIS_TIMERANGE);
                entity.TierLink = String.Format(DEEPLINK_TIER, entity.Controller, entity.ApplicationID, entity.TierID, DEEPLINK_THIS_TIMERANGE);
                deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                entityIdForMetricBrowser = entity.TierID;
            }
            else if (entityRow is APMNode)
            {
                APMNode entity = (APMNode)entityRow;
                entity.ControllerLink = String.Format(DEEPLINK_CONTROLLER, entity.Controller, DEEPLINK_THIS_TIMERANGE);
                entity.ApplicationLink = String.Format(DEEPLINK_APPLICATION, entity.Controller, entity.ApplicationID, DEEPLINK_THIS_TIMERANGE);
                entity.TierLink = String.Format(DEEPLINK_TIER, entity.Controller, entity.ApplicationID, entity.TierID, DEEPLINK_THIS_TIMERANGE);
                entity.NodeLink = String.Format(DEEPLINK_NODE, entity.Controller, entity.ApplicationID, entity.NodeID, DEEPLINK_THIS_TIMERANGE);
                deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_NODE_TARGET_METRIC_ID;
                entityIdForMetricBrowser = entity.NodeID;
            }
            else if (entityRow is Backend)
            {
                Backend entity = (Backend)entityRow;
                entity.ControllerLink = String.Format(DEEPLINK_CONTROLLER, entity.Controller, DEEPLINK_THIS_TIMERANGE);
                entity.ApplicationLink = String.Format(DEEPLINK_APPLICATION, entity.Controller, entity.ApplicationID, DEEPLINK_THIS_TIMERANGE);
                entity.BackendLink = String.Format(DEEPLINK_BACKEND, entity.Controller, entity.ApplicationID, entity.BackendID, DEEPLINK_THIS_TIMERANGE);
            }
            else if (entityRow is BusinessTransaction)
            {
                BusinessTransaction entity = (BusinessTransaction)entityRow;
                entity.ControllerLink = String.Format(DEEPLINK_CONTROLLER, entity.Controller, DEEPLINK_THIS_TIMERANGE);
                entity.ApplicationLink = String.Format(DEEPLINK_APPLICATION, entity.Controller, entity.ApplicationID, DEEPLINK_THIS_TIMERANGE);
                entity.TierLink = String.Format(DEEPLINK_TIER, entity.Controller, entity.ApplicationID, entity.TierID, DEEPLINK_THIS_TIMERANGE);
                entity.BTLink = String.Format(DEEPLINK_BUSINESS_TRANSACTION, entity.Controller, entity.ApplicationID, entity.BTID, DEEPLINK_THIS_TIMERANGE);
                deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                entityIdForMetricBrowser = entity.TierID;
            }
            else if (entityRow is ServiceEndpoint)
            {
                ServiceEndpoint entity = (ServiceEndpoint)entityRow;
                entity.ControllerLink = String.Format(DEEPLINK_CONTROLLER, entity.Controller, DEEPLINK_THIS_TIMERANGE);
                entity.ApplicationLink = String.Format(DEEPLINK_APPLICATION, entity.Controller, entity.ApplicationID, DEEPLINK_THIS_TIMERANGE);
                entity.TierLink = String.Format(DEEPLINK_TIER, entity.Controller, entity.ApplicationID, entity.TierID, DEEPLINK_THIS_TIMERANGE);
                entity.SEPLink = String.Format(DEEPLINK_SERVICE_ENDPOINT, entity.Controller, entity.ApplicationID, entity.TierID, entity.SEPID, DEEPLINK_THIS_TIMERANGE);
            }
            else if (entityRow is Error)
            {
                Error entity = (Error)entityRow;
                entity.ControllerLink = String.Format(DEEPLINK_CONTROLLER, entity.Controller, DEEPLINK_THIS_TIMERANGE);
                entity.ApplicationLink = String.Format(DEEPLINK_APPLICATION, entity.Controller, entity.ApplicationID, DEEPLINK_THIS_TIMERANGE);
                entity.TierLink = String.Format(DEEPLINK_TIER, entity.Controller, entity.ApplicationID, entity.TierID, DEEPLINK_THIS_TIMERANGE);
                entity.ErrorLink = String.Format(DEEPLINK_ERROR, entity.Controller, entity.ApplicationID, entity.ErrorID, DEEPLINK_THIS_TIMERANGE);
            }
            else if (entityRow is InformationPoint)
            {
                InformationPoint entity = (InformationPoint)entityRow;
                entity.ControllerLink = String.Format(DEEPLINK_CONTROLLER, entity.Controller, DEEPLINK_THIS_TIMERANGE);
                entity.ApplicationLink = String.Format(DEEPLINK_APPLICATION, entity.Controller, entity.ApplicationID, DEEPLINK_THIS_TIMERANGE);
                entity.IPLink = String.Format(DEEPLINK_INFORMATION_POINT, entity.Controller, entity.ApplicationID, entity.IPID, DEEPLINK_THIS_TIMERANGE);
            }

            if (entityRow.MetricsIDs != null && entityRow.MetricsIDs.Count > 0)
            {
                StringBuilder sb = new StringBuilder(128);
                foreach (int metricID in entityRow.MetricsIDs)
                {
                    sb.Append(String.Format(deepLinkMetricTemplateInMetricBrowser, entityIdForMetricBrowser, metricID));
                    sb.Append(",");
                }
                sb.Remove(sb.Length - 1, 1);
                entityRow.MetricLink = String.Format(DEEPLINK_METRIC, entityRow.Controller, entityRow.ApplicationID, sb.ToString(), DEEPLINK_THIS_TIMERANGE);
            }
        }

        internal string getDurationRangeAsString(long duration)
        {
            if (duration < 0)
            {
                return "-1: t<0";
            }
            else if (duration == 0)
            {
                return "00: t=0";
            }
            else if (duration > 0 && duration <= 10)
            {
                return "01: 0<t<=10";
            }
            else if (duration > 10 && duration <= 50)
            {
                return "02: 10<t<=50";
            }
            else if (duration > 50 && duration <= 100)
            {
                return "03: 50<t<=100";
            }
            else if (duration > 100 && duration <= 200)
            {
                return "04: 100<t<=200";
            }
            else if (duration > 200 && duration <= 500)
            {
                return "05: 200<t<=500";
            }
            else if (duration > 500 && duration <= 1000)
            {
                return "06: 500<t<=1000";
            }
            else if (duration > 1000 && duration <= 2000)
            {
                return "07: 1000<t<=2000";
            }
            else if (duration > 2000 && duration <= 5000)
            {
                return "08: 2000<t<=5000";
            }
            else if (duration > 5000 && duration <= 10000)
            {
                return "09: 5000<t<=10000";
            }
            else if (duration > 10000 && duration <= 15000)
            {
                return "10: 10000<t<=15000";
            }
            else if (duration > 15000 && duration <= 20000)
            {
                return "11: 15000<t<=20000";
            }
            else if (duration > 20000 && duration <= 50000)
            {
                return "12: 20000<t<=50000";
            }
            else if (duration > 50000 && duration <= 100000)
            {
                return "13: 50000<t<=100000";
            }
            else if (duration > 100000 && duration <= 150000)
            {
                return "14: 100000<t<=150000";
            }
            else if (duration > 150000 && duration <= 200000)
            {
                return "15: 150000<t<=200000";
            }
            else if (duration > 200000 && duration <= 300000)
            {
                return "16: 200000<t<=300000";
            }
            return "17: t>300000"; ;
        }

        internal string getSQLClauseType(string sqlStatement, int lengthToSeekThrough)
        {
            if (sqlStatement.Length < lengthToSeekThrough) lengthToSeekThrough = sqlStatement.Length;

            if (new Regex(@"\bCREATE\s", RegexOptions.IgnoreCase).Match(sqlStatement, 0, lengthToSeekThrough).Success == true)
            {
                return "CREATE";
            }
            else if (new Regex(@"\bALTER\s", RegexOptions.IgnoreCase).Match(sqlStatement, 0, lengthToSeekThrough).Success == true)
            {
                return "ALTER";
            }
            else if (new Regex(@"\bTRUNCATE\s", RegexOptions.IgnoreCase).Match(sqlStatement, 0, lengthToSeekThrough).Success == true)
            {
                return "TRUNCATE";
            }
            else if (new Regex(@"\bDROP\s", RegexOptions.IgnoreCase).Match(sqlStatement, 0, lengthToSeekThrough).Success == true)
            {
                return "DROP";
            }
            else if (new Regex(@"\bGRANT\s", RegexOptions.IgnoreCase).Match(sqlStatement, 0, lengthToSeekThrough).Success == true)
            {
                return "GRANT";
            }
            else if (new Regex(@"\bREVOKE\s", RegexOptions.IgnoreCase).Match(sqlStatement, 0, lengthToSeekThrough).Success == true)
            {
                return "REVOKE";
            }
            else if (new Regex(@"\bSELECT\s", RegexOptions.IgnoreCase).Match(sqlStatement, 0, lengthToSeekThrough).Success == true)
            {
                return "SELECT";
            }
            else if (new Regex(@"\bINSERT\s", RegexOptions.IgnoreCase).Match(sqlStatement, 0, lengthToSeekThrough).Success == true)
            {
                return "INSERT";
            }
            else if (new Regex(@"\bUPDATE\s", RegexOptions.IgnoreCase).Match(sqlStatement, 0, lengthToSeekThrough).Success == true)
            {
                return "UPDATE";
            }
            else if (new Regex(@"\bDELETE\s", RegexOptions.IgnoreCase).Match(sqlStatement, 0, lengthToSeekThrough).Success == true)
            {
                return "DELETE";
            }
            else if (new Regex(@"\bEXEC\s", RegexOptions.IgnoreCase).Match(sqlStatement, 0, lengthToSeekThrough).Success == true)
            {
                return "PROCCALL";
            }
            else if (new Regex(@"\bCALL\s", RegexOptions.IgnoreCase).Match(sqlStatement, 0, lengthToSeekThrough).Success == true)
            {
                return "PROCCALL";
            }
            else if (new Regex(@"\bSET\s", RegexOptions.IgnoreCase).Match(sqlStatement, 0, lengthToSeekThrough).Success == true)
            {
                return "SET";
            }
            else if (new Regex(@"\bPREPARED STATEMENT\s", RegexOptions.IgnoreCase).Match(sqlStatement, 0, lengthToSeekThrough).Success == true)
            {
                return "PREPSTMT";
            }

            return String.Empty;
        }

        internal string getSQLJoinType(string sqlStatement)
        {
            if (new Regex(@"\bINNER JOIN\s", RegexOptions.IgnoreCase).Match(sqlStatement).Success == true)
            {
                return "INNER";
            }
            else if (new Regex(@"\bLEFT JOIN\s", RegexOptions.IgnoreCase).Match(sqlStatement).Success == true || new Regex(@"\bLEFT OUTER JOIN", RegexOptions.IgnoreCase).Match(sqlStatement).Success == true)
            {
                return "LEFT";
            }
            else if (new Regex(@"\bRIGHT JOIN\s", RegexOptions.IgnoreCase).Match(sqlStatement).Success == true || new Regex(@"\bRIGHT OUTER JOIN", RegexOptions.IgnoreCase).Match(sqlStatement).Success == true)
            {
                return "RIGHT";
            }
            else if (new Regex(@"\bFULL JOIN\s", RegexOptions.IgnoreCase).Match(sqlStatement).Success == true || new Regex(@"\bFULL OUTER JOIN", RegexOptions.IgnoreCase).Match(sqlStatement).Success == true)
            {
                return "FULL";
            }
            else if (new Regex(@"\bSELF JOIN\s", RegexOptions.IgnoreCase).Match(sqlStatement).Success == true)
            {
                return "SELF";
            }
            else if (new Regex(@"\bCARTESIAN JOIN\s", RegexOptions.IgnoreCase).Match(sqlStatement).Success == true || new Regex(@"\bCROSS JOIN", RegexOptions.IgnoreCase).Match(sqlStatement).Success == true)
            {
                return "CROSS";
            }
            else if (new Regex(@"\bJOIN\s", RegexOptions.IgnoreCase).Match(sqlStatement).Success == true)
            {
                return "INNER";
            }

            return String.Empty;
        }

        internal bool doesSQLStatementContain(string sqlStatement, string valueToCheckFor)
        {
            if (new Regex(valueToCheckFor, RegexOptions.IgnoreCase).Match(sqlStatement).Success == true)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}
