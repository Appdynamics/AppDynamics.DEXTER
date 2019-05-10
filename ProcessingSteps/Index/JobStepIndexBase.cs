using AppDynamics.Dexter.ReportObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class JobStepIndexBase : JobStepBase
    {
        #region Constants for Deeplinks

        // APM links
        internal const string DEEPLINK_CONTROLLER = @"{0}/controller/#/location=AD_HOME_OVERVIEW&timeRange={1}";
        internal const string DEEPLINK_APM_APPLICATION = @"{0}/controller/#/location=APP_DASHBOARD&timeRange={2}&application={1}&dashboardMode=force";
        internal const string DEEPLINK_TIER = @"{0}/controller/#/location=APP_COMPONENT_MANAGER&timeRange={3}&application={1}&component={2}&dashboardMode=force";
        internal const string DEEPLINK_NODE = @"{0}/controller/#/location=APP_NODE_MANAGER&timeRange={3}&application={1}&node={2}&dashboardMode=force";
        internal const string DEEPLINK_BACKEND = @"{0}/controller/#/location=APP_BACKEND_DASHBOARD&timeRange={3}&application={1}&backendDashboard={2}&dashboardMode=force";
        internal const string DEEPLINK_BUSINESS_TRANSACTION = @"{0}/controller/#/location=APP_BT_DETAIL&timeRange={3}&application={1}&businessTransaction={2}&dashboardMode=force";
        internal const string DEEPLINK_SERVICE_ENDPOINT = @"{0}/controller/#/location=APP_SERVICE_ENDPOINT_DETAIL&timeRange={4}&application={1}&component={2}&serviceEndpoint={3}";
        internal const string DEEPLINK_ERROR = @"{0}/controller/#/location=APP_ERROR_DASHBOARD&timeRange={3}&application={1}&error={2}";
        internal const string DEEPLINK_INFORMATION_POINT = @"{0}/controller/#/location=APP_INFOPOINT_DASHBOARD&timeRange={3}&application={1}&infoPoint={2}";
        internal const string DEEPLINK_APPLICATION_MOBILE = @"{0}/controller/#/location=EUM_MOBILE_MAIN_DASHBOARD&timeRange={3}&application={1}&mobileApp={2}";
        internal const string DEEPLINK_INCIDENT = @"{0}/controller/#/location=APP_INCIDENT_DETAIL_MODAL&timeRange={4}&application={1}&incident={2}&incidentTime={3}";
        internal const string DEEPLINK_SNAPSHOT_OVERVIEW = @"{0}/controller/#/location=APP_SNAPSHOT_VIEWER&rsdTime={3}&application={1}&requestGUID={2}&tab=overview&dashboardMode=force";
        internal const string DEEPLINK_SNAPSHOT_SEGMENT = @"{0}/controller/#/location=APP_SNAPSHOT_VIEWER&rsdTime={4}&application={1}&requestGUID={2}&tab={3}&dashboardMode=force";

        // SIM links
        internal const string DEEPLINK_SIM_APPLICATION = @"{0}/controller/#/location=SERVER_MONITORING_MACHINE_LIST&timeRange={1}";
        internal const string DEEPLINK_SIM_MACHINE = @"{0}/controller/#/location=SERVER_MONITORING_MACHINE_OVERVIEW&timeRange={2}&machineId={1}";

        internal const string DEEPLINK_METRIC = @"{0}/controller/#/location=METRIC_BROWSER&timeRange={3}&application={1}&metrics={2}";
        internal const string DEEPLINK_TIMERANGE_LAST_15_MINUTES = "last_15_minutes.BEFORE_NOW.-1.-1.15";
        internal const string DEEPLINK_TIMERANGE_BETWEEN_TIMES = "Custom_Time_Range.BETWEEN_TIMES.{0}.{1}.{2}";
        internal const string DEEPLINK_METRIC_APPLICATION_TARGET_METRIC_ID = "APPLICATION.{0}.{1}";
        internal const string DEEPLINK_METRIC_TIER_TARGET_METRIC_ID = "APPLICATION_COMPONENT.{0}.{1}";
        internal const string DEEPLINK_METRIC_NODE_TARGET_METRIC_ID = "APPLICATION_COMPONENT_NODE.{0}.{1}";

        // DB Links
        internal const string DEEPLINK_DB_APPLICATION = @"{0}/controller/#/location=DB_MONITORING_SERVER_LIST&timeRange={1}";
        internal const string DEEPLINK_DB_COLLECTOR = @"{0}/controller/#/location=DB_MONITORING_SERVER_DASHBOARD&timeRange={2}&databaseId={1}";
        internal const string DEEPLINK_DB_QUERY = @"{0}/controller/#/location=DB_MONITORING_QUERY_DETAILS&timeRange={3}&databaseId={1}&&queryHashCode={2}";

        // WEB Links
        internal const string DEEPLINK_WEB_APPLICATION = @"{0}/controller/#/location=EUM_WEB_MAIN_DASHBOARD&timeRange={2}&application={1}";
        internal const string DEEPLINK_WEB_PAGE = @"{0}/controller/#/location=EUM_PAGE_DASHBOARD&timeRange={3}&application={1}&addId={2}";

        // MOBILE Links
        internal const string DEEPLINK_MOBILE_APPLICATION = @"{0}/controller/#/location=EUM_MOBILE_MAIN_DASHBOARD&timeRange={3}&application={1}&mobileApp={2}";
        internal const string DEEPLINK_NETWORK_REQUEST = @"{0}/controller/#/location=EUM_REQUEST_DASHBOARD&timeRange={4}&application={1}&mobileApp={2}&addId={3}";

        // BIQ Links
        internal const string DEEPLINK_BIQ_APPLICATION = @"{0}/controller/#/location=ANALYTICS_SEARCH_LIST&timeRange={1}";
        internal const string DEEPLINK_BIQ_SEARCH = @"{0}/controller/#/location=ANALYTICS_ADQL_SEARCH&timeRange={2}&searchId={1}";

        // Health Rule links
        internal const string DEEPLINK_HEALTH_RULE = @"{0}/controller/#/location=ALERT_RESPOND_HEALTH_RULES&timeRange={3}&application={1}";
        internal const string DEEPLINK_HEALTH_RULES = @"{0}/controller/#/location=ALERT_RESPOND_HEALTH_RULES&timeRange={2}&application={1}";
        internal const string DEEPLINK_POLICIES_RULES = @"{0}/controller/#/location=ALERT_RESPOND_POLICIES&timeRange={2}&application={1}";
        internal const string DEEPLINK_ACTIONS_RULES = @"{0}/controller/#/location=ALERT_RESPOND_ACTIONS&timeRange={2}&application={1}";

        // Dashboard links
        internal const string DEEPLINK_DASHBOARD = @"{0}/controller/#/location=CDASHBOARD_DETAIL&mode=MODE_DASHBOARD&dashboard={1}&timeRange={2}";

        // License links
        internal const string DEEPLINK_LICENSE = @"{0}/controller/#/location=LICENSE_MANAGEMENT_PEAK_USAGE&timeRange={1}";

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
            {ENTITY_TYPE_FLOWMAP_BUSINESS_TRANSACTION, APMBusinessTransaction.ENTITY_TYPE},
            {ENTITY_TYPE_FLOWMAP_BACKEND, APMBackend.ENTITY_TYPE },
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
                entityRow.ApplicationLink = String.Format(DEEPLINK_APM_APPLICATION, entityRow.Controller, entityRow.ApplicationID, DEEPLINK_THIS_TIMERANGE);
            }
            else if (entityRow is APMTier)
            {
                APMTier entity = (APMTier)entityRow;
                entity.ControllerLink = String.Format(DEEPLINK_CONTROLLER, entity.Controller, DEEPLINK_THIS_TIMERANGE);
                entity.ApplicationLink = String.Format(DEEPLINK_APM_APPLICATION, entity.Controller, entity.ApplicationID, DEEPLINK_THIS_TIMERANGE);
                entity.TierLink = String.Format(DEEPLINK_TIER, entity.Controller, entity.ApplicationID, entity.TierID, DEEPLINK_THIS_TIMERANGE);
                deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                entityIdForMetricBrowser = entity.TierID;
            }
            else if (entityRow is APMNode)
            {
                APMNode entity = (APMNode)entityRow;
                entity.ControllerLink = String.Format(DEEPLINK_CONTROLLER, entity.Controller, DEEPLINK_THIS_TIMERANGE);
                entity.ApplicationLink = String.Format(DEEPLINK_APM_APPLICATION, entity.Controller, entity.ApplicationID, DEEPLINK_THIS_TIMERANGE);
                entity.TierLink = String.Format(DEEPLINK_TIER, entity.Controller, entity.ApplicationID, entity.TierID, DEEPLINK_THIS_TIMERANGE);
                entity.NodeLink = String.Format(DEEPLINK_NODE, entity.Controller, entity.ApplicationID, entity.NodeID, DEEPLINK_THIS_TIMERANGE);
                deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_NODE_TARGET_METRIC_ID;
                entityIdForMetricBrowser = entity.NodeID;
            }
            else if (entityRow is APMBackend)
            {
                APMBackend entity = (APMBackend)entityRow;
                entity.ControllerLink = String.Format(DEEPLINK_CONTROLLER, entity.Controller, DEEPLINK_THIS_TIMERANGE);
                entity.ApplicationLink = String.Format(DEEPLINK_APM_APPLICATION, entity.Controller, entity.ApplicationID, DEEPLINK_THIS_TIMERANGE);
                entity.BackendLink = String.Format(DEEPLINK_BACKEND, entity.Controller, entity.ApplicationID, entity.BackendID, DEEPLINK_THIS_TIMERANGE);
            }
            else if (entityRow is APMBusinessTransaction)
            {
                APMBusinessTransaction entity = (APMBusinessTransaction)entityRow;
                entity.ControllerLink = String.Format(DEEPLINK_CONTROLLER, entity.Controller, DEEPLINK_THIS_TIMERANGE);
                entity.ApplicationLink = String.Format(DEEPLINK_APM_APPLICATION, entity.Controller, entity.ApplicationID, DEEPLINK_THIS_TIMERANGE);
                entity.TierLink = String.Format(DEEPLINK_TIER, entity.Controller, entity.ApplicationID, entity.TierID, DEEPLINK_THIS_TIMERANGE);
                entity.BTLink = String.Format(DEEPLINK_BUSINESS_TRANSACTION, entity.Controller, entity.ApplicationID, entity.BTID, DEEPLINK_THIS_TIMERANGE);
                deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                entityIdForMetricBrowser = entity.TierID;
            }
            else if (entityRow is APMServiceEndpoint)
            {
                APMServiceEndpoint entity = (APMServiceEndpoint)entityRow;
                entity.ControllerLink = String.Format(DEEPLINK_CONTROLLER, entity.Controller, DEEPLINK_THIS_TIMERANGE);
                entity.ApplicationLink = String.Format(DEEPLINK_APM_APPLICATION, entity.Controller, entity.ApplicationID, DEEPLINK_THIS_TIMERANGE);
                entity.TierLink = String.Format(DEEPLINK_TIER, entity.Controller, entity.ApplicationID, entity.TierID, DEEPLINK_THIS_TIMERANGE);
                entity.SEPLink = String.Format(DEEPLINK_SERVICE_ENDPOINT, entity.Controller, entity.ApplicationID, entity.TierID, entity.SEPID, DEEPLINK_THIS_TIMERANGE);
            }
            else if (entityRow is APMError)
            {
                APMError entity = (APMError)entityRow;
                entity.ControllerLink = String.Format(DEEPLINK_CONTROLLER, entity.Controller, DEEPLINK_THIS_TIMERANGE);
                entity.ApplicationLink = String.Format(DEEPLINK_APM_APPLICATION, entity.Controller, entity.ApplicationID, DEEPLINK_THIS_TIMERANGE);
                entity.TierLink = String.Format(DEEPLINK_TIER, entity.Controller, entity.ApplicationID, entity.TierID, DEEPLINK_THIS_TIMERANGE);
                if (entity.ErrorID > 0)
                {
                    entity.ErrorLink = String.Format(DEEPLINK_ERROR, entity.Controller, entity.ApplicationID, entity.ErrorID, DEEPLINK_THIS_TIMERANGE);
                }
            }
            else if (entityRow is APMInformationPoint)
            {
                APMInformationPoint entity = (APMInformationPoint)entityRow;
                entity.ControllerLink = String.Format(DEEPLINK_CONTROLLER, entity.Controller, DEEPLINK_THIS_TIMERANGE);
                entity.ApplicationLink = String.Format(DEEPLINK_APM_APPLICATION, entity.Controller, entity.ApplicationID, DEEPLINK_THIS_TIMERANGE);
                entity.IPLink = String.Format(DEEPLINK_INFORMATION_POINT, entity.Controller, entity.ApplicationID, entity.IPID, DEEPLINK_THIS_TIMERANGE);
            }

            if (entityRow.MetricsIDs != null && entityRow.MetricsIDs.Count > 0)
            {
                StringBuilder sb = new StringBuilder(256);
                foreach (long metricID in entityRow.MetricsIDs)
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

        internal static int getIntegerValueFromXmlNode(XmlNode xmlNode)
        {
            if (xmlNode == null)
            {
                return 0;
            }
            else if (((XmlElement)xmlNode).IsEmpty == true)
            {
                return 0;
            }
            else
            {
                int value;
                if (Int32.TryParse(xmlNode.InnerText, out value) == true)
                {
                    return value;
                }
                else
                {
                    double value1;
                    if (Double.TryParse(xmlNode.InnerText, out value1) == true)
                    {
                        return Convert.ToInt32(Math.Floor(value1));
                    }
                    else
                    {
                        return 0;
                    }
                }
            }
        }

        internal static long getLongValueFromXmlNode(XmlNode xmlNode)
        {
            if (xmlNode == null)
            {
                return 0;
            }
            else if (((XmlElement)xmlNode).IsEmpty == true)
            {
                return 0;
            }
            else
            {
                long value;
                if (Int64.TryParse(xmlNode.InnerText, out value) == true)
                {
                    return value;
                }
                else
                {
                    double value1;
                    if (Double.TryParse(xmlNode.InnerText, out value1) == true)
                    {
                        return Convert.ToInt64(Math.Floor(value1));
                    }
                    else
                    {
                        return 0;
                    }
                }
            }
        }

        internal static bool getBoolValueFromXmlNode(XmlNode xmlNode)
        {
            if (xmlNode == null)
            {
                return false;
            }
            else if (((XmlElement)xmlNode).IsEmpty == true)
            {
                return false;
            }
            else
            {
                bool value;
                if (Boolean.TryParse(xmlNode.InnerText, out value) == true)
                {
                    return value;
                }
                else
                {
                    return false;
                }
            }
        }

        internal static string getStringValueFromXmlNode(XmlNode xmlNode)
        {
            if (xmlNode == null)
            {
                return String.Empty;
            }
            else if (((XmlElement)xmlNode).IsEmpty == true)
            {
                return String.Empty;
            }
            else
            {
                return xmlNode.InnerText;
            }
        }

        /// <summary>
        /// https://stackoverflow.com/questions/1123718/format-xml-string-to-print-friendly-xml-string
        /// </summary>
        /// <param name="XML"></param>
        /// <returns></returns>
        internal static string makeXMLFormattedAndIndented(string XML)
        {
            string Result = "";

            MemoryStream MS = new MemoryStream();
            XmlTextWriter W = new XmlTextWriter(MS, Encoding.Unicode);
            XmlDocument D = new XmlDocument();

            try
            {
                // Load the XmlDocument with the XML.
                D.LoadXml(XML);

                W.Formatting = System.Xml.Formatting.Indented;

                // Write the XML into a formatting XmlTextWriter
                D.WriteContentTo(W);
                W.Flush();
                MS.Flush();

                // Have to rewind the MemoryStream in order to read
                // its contents.
                MS.Position = 0;

                // Read MemoryStream contents into a StreamReader.
                StreamReader SR = new StreamReader(MS);

                // Extract the text from the StreamReader.
                String FormattedXML = SR.ReadToEnd();

                Result = FormattedXML;
            }
            catch (XmlException)
            {
            }

            MS.Close();
            W.Close();

            return Result;
        }

        internal static string makeXMLFormattedAndIndented(XmlNode xmlNode)
        {
            if (xmlNode != null)
            {
                return makeXMLFormattedAndIndented(xmlNode.OuterXml);
            }
            else
            {
                return String.Empty;
            }
        }

        internal static string makeXMLFormattedAndIndented(XmlNodeList xmlNodeList)
        {
            if (xmlNodeList.Count > 0)
            {
                StringBuilder sb = new StringBuilder(128 * xmlNodeList.Count);
                foreach (XmlNode xmlNode in xmlNodeList)
                {
                    sb.Append(makeXMLFormattedAndIndented(xmlNode));
                    sb.AppendLine();
                }
                sb.Remove(sb.Length - 1, 1);
                return sb.ToString();
            }
            else
            {
                return String.Empty;
            }
        }
    }
}
