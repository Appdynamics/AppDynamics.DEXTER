using AppDynamics.Dexter.DataObjects;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class IndexControllersApplicationsAndEntities : JobStepIndexBase
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

                        #region Controller

                        List<AppDRESTApplication> applicationsRESTList = null;

                        loggerConsole.Info("Index List of Controllers");

                        // Create this row 
                        EntityController controller = new EntityController();
                        controller.Controller = jobTarget.Controller;
                        controller.ControllerLink = String.Format(DEEPLINK_CONTROLLER, controller.Controller, DEEPLINK_TIMERANGE_LAST_15_MINUTES);
                        controller.UserName = jobTarget.UserName;

                        // Lookup number of applications
                        // Load JSON file from the file system in case we are continuing the step after stopping
                        applicationsRESTList = FileIOHelper.LoadListOfObjectsFromFile<AppDRESTApplication>(FilePathMap.ApplicationsDataFilePath(jobTarget));
                        if (applicationsRESTList != null)
                        {
                            controller.NumApps = applicationsRESTList.Count;
                        }

                        // Lookup version
                        // Load the configuration.xml from the child to parse the version
                        XmlDocument configXml = FileIOHelper.LoadXmlDocumentFromFile(FilePathMap.ControllerVersionDataFilePath(jobTarget));
                        if (configXml != null)
                        {
                            //<serverstatus version="1" vendorid="">
                            //    <available>true</available>
                            //    <serverid/>
                            //    <serverinfo>
                            //        <vendorname>AppDynamics</vendorname>
                            //        <productname>AppDynamics Application Performance Management</productname>
                            //        <serverversion>004-004-001-000</serverversion>
                            //        <implementationVersion>Controller v4.4.1.0 Build 164 Commit 6e1fd94d18dc87c1ecab2da573f98cea49d31c3a</implementationVersion>
                            //    </serverinfo>
                            //    <startupTimeInSeconds>19</startupTimeInSeconds>
                            //</serverstatus>
                            string controllerVersion = configXml.SelectSingleNode("serverstatus/serverinfo/serverversion").InnerText;
                            string[] controllerVersionArray = controllerVersion.Split('-');
                            int[] controllerVersionArrayNum = new int[controllerVersionArray.Length];
                            for (int j = 0; j < controllerVersionArray.Length; j++)
                            {
                                controllerVersionArrayNum[j] = Convert.ToInt32(controllerVersionArray[j]);
                            }
                            controllerVersion = String.Join(".", controllerVersionArrayNum);
                            controller.Version = controllerVersion;
                            controller.VersionDetail = configXml.SelectSingleNode("serverstatus/serverinfo/implementationVersion").InnerText;
                        }
                        else
                        {
                            controller.Version = "No config data";
                        }

                        // Output single controller report CSV
                        List<EntityController> controllerList = new List<EntityController>(1);
                        controllerList.Add(controller);

                        if (File.Exists(FilePathMap.ControllerIndexFilePath(jobTarget)) == false)
                        {
                            FileIOHelper.WriteListToCSVFile(controllerList, new ControllerEntityReportMap(), FilePathMap.ControllerIndexFilePath(jobTarget));
                        }

                        #endregion

                        #region Nodes and Node Properties

                        List<AppDRESTNode> nodesRESTList = FileIOHelper.LoadListOfObjectsFromFile<AppDRESTNode>(FilePathMap.NodesDataFilePath(jobTarget));
                        List<EntityNode> nodesList = null;
                        List<EntityNodeProperty> entityNodeStartupOptionsList = null;
                        List<EntityNodeProperty> entityNodePropertiesList = null;
                        List<EntityNodeProperty> entityNodeEnvironmentVariablesList = null;
                        if (nodesRESTList != null)
                        {
                            loggerConsole.Info("Index List of Nodes and Node Properties ({0} entities)", nodesRESTList.Count);

                            stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + nodesRESTList.Count;

                            nodesList = new List<EntityNode>(nodesRESTList.Count);
                            entityNodeStartupOptionsList = new List<EntityNodeProperty>(nodesRESTList.Count * 25);
                            entityNodePropertiesList = new List<EntityNodeProperty>(nodesRESTList.Count * 25);
                            entityNodeEnvironmentVariablesList = new List<EntityNodeProperty>(nodesRESTList.Count * 25);

                            foreach (AppDRESTNode node in nodesRESTList)
                            {
                                EntityNode nodeRow = new EntityNode();
                                nodeRow.NodeID = node.id;
                                nodeRow.AgentPresent = node.appAgentPresent;
                                nodeRow.AgentType = node.agentType;
                                nodeRow.AgentVersion = node.appAgentVersion;
                                nodeRow.ApplicationName = jobTarget.Application;
                                nodeRow.ApplicationID = jobTarget.ApplicationID;
                                nodeRow.Controller = jobTarget.Controller;
                                nodeRow.MachineAgentPresent = node.machineAgentPresent;
                                nodeRow.MachineAgentVersion = node.machineAgentVersion;
                                nodeRow.MachineID = node.machineId;
                                nodeRow.MachineName = node.machineName;
                                nodeRow.MachineOSType = node.machineOSType;
                                nodeRow.NodeName = node.name;
                                nodeRow.TierID = node.tierId;
                                nodeRow.TierName = node.tierName;
                                nodeRow.MachineType = node.type;
                                if (nodeRow.AgentVersion != String.Empty)
                                {
                                    // Java agent looks like that
                                    //Server Agent v4.2.3.2 GA #12153 r13c5eb6a7acbfea4d6da465a3ae47412715e26fa 59-4.2.3.next-build
                                    //Server Agent v3.7.16.0 GA #2014-02-26_21-19-08 raf61d5f54753290c983f95173e74e6865f6ad123 130-3.7.16
                                    //Server Agent v4.2.7.1 GA #13005 rc04adaef4741dbb8f2e7c206bdb2a6614046798a 11-4.2.7.next-analytics
                                    //Server Agent v4.0.6.0 GA #2015-05-11_20-44-33 r7cb8945756a0779766bf1b4c32e49a96da7b8cfe 10-4.0.6.next
                                    //Server Agent v3.8.3.0 GA #2014-06-06_17-06-05 r34b2744775df248f79ffb2da2b4515b1f629aeb5 7-3.8.3.next
                                    //Server Agent v3.9.3.0 GA #2014-09-23_22-14-15 r05918cd8a4a8a63504a34f0f1c85511e207049b3 20-3.9.3.next
                                    //Server Agent v4.1.7.1 GA #9949 ra4a2721d52322207b626e8d4c88855c846741b3d 18-4.1.7.next-build
                                    //Server Agent v3.7.11.1 GA #2013-10-23_17-07-44 r41149afdb8ce39025051c25382b1cf77e2a7fed0 21
                                    //Server Agent v4.1.8.5 GA #10236 r8eca32e4695e8f6a5902d34a66bfc12da1e12241 45-4.1.8.next-controller

                                    // Apache agent looks like this
                                    // Proxy v4.2.5.1 GA SHA-1:.ad6c804882f518b3350f422489866ea2008cd664 #13146 35-4.2.5.next-build

                                    Regex regexVersion = new Regex(@"(?i).*v(\d*\.\d*\.\d*\.\d*).*", RegexOptions.IgnoreCase);
                                    Match match = regexVersion.Match(nodeRow.AgentVersion);
                                    if (match != null)
                                    {
                                        if (match.Groups.Count > 1)
                                        {
                                            nodeRow.AgentVersionRaw = nodeRow.AgentVersion;
                                            nodeRow.AgentVersion = match.Groups[1].Value;
                                        }
                                    }
                                }
                                if (nodeRow.MachineAgentVersion != String.Empty)
                                {
                                    // Machine agent looks like that 
                                    //Machine Agent v4.2.3.2 GA Build Date 2016 - 07 - 11 10:26:01
                                    //Machine Agent v3.7.16.0 GA Build Date 2014 - 02 - 26 21:20:29
                                    //Machine Agent v4.2.3.2 GA Build Date 2016 - 07 - 11 10:17:54
                                    //Machine Agent v4.0.6.0 GA Build Date 2015 - 05 - 11 20:56:44
                                    //Machine Agent v3.8.3.0 GA Build Date 2014 - 06 - 06 17:09:13
                                    //Machine Agent v4.1.7.1 GA Build Date 2015 - 11 - 24 20:49:24

                                    Regex regexVersion = new Regex(@"(?i).*Machine Agent.*v(\d*\.\d*\.\d*\.\d*).*", RegexOptions.IgnoreCase);
                                    Match match = regexVersion.Match(nodeRow.MachineAgentVersion);
                                    if (match != null)
                                    {
                                        if (match.Groups.Count > 1)
                                        {
                                            nodeRow.MachineAgentVersionRaw = nodeRow.MachineAgentVersion;
                                            nodeRow.MachineAgentVersion = match.Groups[1].Value;
                                        }
                                    }
                                }

                                updateEntityWithDeeplinks(nodeRow);
                                updateEntityWithEntityDetailAndFlameGraphLinks(nodeRow, jobTarget, jobConfiguration.Input.TimeRange);

                                JObject nodeProperties = FileIOHelper.LoadJObjectFromFile(FilePathMap.NodeRuntimePropertiesDataFilePath(jobTarget, jobConfiguration.Input.TimeRange, node));
                                if (nodeProperties != null)
                                {
                                    nodeRow.AgentRuntime = nodeProperties["latestAgentRuntime"].ToString();

                                    nodeRow.InstallDirectory = nodeProperties["installDir"].ToString();
                                    nodeRow.InstallTime = UnixTimeHelper.ConvertFromUnixTimestamp((long)nodeProperties["installTime"]);

                                    nodeRow.LastStartTime = UnixTimeHelper.ConvertFromUnixTimestamp((long)nodeProperties["lastStartTime"]);

                                    nodeRow.IsDisabled = (bool)nodeProperties["disable"];
                                    nodeRow.IsMonitoringDisabled = (bool)nodeProperties["disableMonitoring"];

                                    if (nodeProperties["latestVmStartupOptions"].HasValues == true)
                                    {
                                        nodeRow.NumStartupOptions = nodeProperties["latestVmStartupOptions"].Count();
                                        foreach (JValue nodeStartupOptionObject in nodeProperties["latestVmStartupOptions"])
                                        {
                                            EntityNodeProperty nodePropertyRow = new EntityNodeProperty();
                                            nodePropertyRow.NodeID = nodeRow.NodeID;
                                            nodePropertyRow.AgentType = nodeRow.AgentType;
                                            nodePropertyRow.ApplicationName = nodeRow.ApplicationName;
                                            nodePropertyRow.ApplicationID = nodeRow.ApplicationID;
                                            nodePropertyRow.Controller = nodeRow.Controller;
                                            nodePropertyRow.NodeName = nodeRow.NodeName;
                                            nodePropertyRow.TierID = nodeRow.TierID;
                                            nodePropertyRow.TierName = nodeRow.TierName;

                                            string optionValue = nodeStartupOptionObject.Value.ToString();
                                            string optionValueAdjusted = String.Empty;
                                            if (optionValue.StartsWith("-D") == true)
                                            {
                                                optionValueAdjusted = optionValue.Substring(2);
                                            }
                                            else if (optionValue.StartsWith("-") == true)
                                            {
                                                optionValueAdjusted = optionValue.Substring(1);
                                            }

                                            if (optionValueAdjusted.Length > 0)
                                            {
                                                string[] optionValueAdjustedTokens = optionValueAdjusted.Split(new char[] { '=' , ':'});
                                                if (optionValueAdjustedTokens.Length > 0)
                                                {
                                                    nodePropertyRow.PropName = optionValueAdjustedTokens[0];
                                                }
                                                if (optionValueAdjustedTokens.Length > 1)
                                                {
                                                    nodePropertyRow.PropValue = optionValueAdjustedTokens[1];
                                                }
                                            }
                                            else
                                            {
                                                nodePropertyRow.PropName = "Unknown";
                                                nodePropertyRow.PropValue = optionValue;
                                            }

                                            entityNodeStartupOptionsList.Add(nodePropertyRow);
                                        }
                                    }
                                    if (nodeProperties["latestVmSystemProperties"].HasValues == true)
                                    {
                                        nodeRow.NumProperties = nodeProperties["latestVmSystemProperties"].Count();
                                        foreach (JObject nodePropertyObject in nodeProperties["latestVmSystemProperties"])
                                        {
                                            EntityNodeProperty nodePropertyRow = new EntityNodeProperty();
                                            nodePropertyRow.NodeID = nodeRow.NodeID;
                                            nodePropertyRow.AgentType = nodeRow.AgentType;
                                            nodePropertyRow.ApplicationName = nodeRow.ApplicationName;
                                            nodePropertyRow.ApplicationID = nodeRow.ApplicationID;
                                            nodePropertyRow.Controller = nodeRow.Controller;
                                            nodePropertyRow.NodeName = nodeRow.NodeName;
                                            nodePropertyRow.TierID = nodeRow.TierID;
                                            nodePropertyRow.TierName = nodeRow.TierName;

                                            nodePropertyRow.PropName = nodePropertyObject["name"].ToString();
                                            nodePropertyRow.PropValue = nodePropertyObject["value"].ToString();

                                            entityNodePropertiesList.Add(nodePropertyRow);
                                        }
                                    }
                                    if (nodeProperties["latestEnvironmentVariables"].HasValues == true)
                                    {
                                        nodeRow.NumEnvVariables = nodeProperties["latestEnvironmentVariables"].Count();
                                        foreach (JObject nodePropertyObject in nodeProperties["latestEnvironmentVariables"])
                                        {
                                            EntityNodeProperty nodePropertyRow = new EntityNodeProperty();
                                            nodePropertyRow.NodeID = nodeRow.NodeID;
                                            nodePropertyRow.AgentType = nodeRow.AgentType;
                                            nodePropertyRow.ApplicationName = nodeRow.ApplicationName;
                                            nodePropertyRow.ApplicationID = nodeRow.ApplicationID;
                                            nodePropertyRow.Controller = nodeRow.Controller;
                                            nodePropertyRow.NodeName = nodeRow.NodeName;
                                            nodePropertyRow.TierID = nodeRow.TierID;
                                            nodePropertyRow.TierName = nodeRow.TierName;

                                            nodePropertyRow.PropName = nodePropertyObject["name"].ToString();
                                            nodePropertyRow.PropValue = nodePropertyObject["value"].ToString();

                                            entityNodeEnvironmentVariablesList.Add(nodePropertyRow);
                                        }
                                    }
                                }

                                nodesList.Add(nodeRow);
                            }

                            // Sort them
                            nodesList = nodesList.OrderBy(o => o.TierName).ThenBy(o => o.NodeName).ToList();
                            entityNodeStartupOptionsList = entityNodeStartupOptionsList.OrderBy(o => o.TierName).ThenBy(o => o.NodeName).ThenBy(o => o.PropName).ToList();
                            entityNodePropertiesList = entityNodePropertiesList.OrderBy(o => o.TierName).ThenBy(o => o.NodeName).ThenBy(o => o.PropName).ToList();
                            entityNodeEnvironmentVariablesList = entityNodeEnvironmentVariablesList.OrderBy(o => o.TierName).ThenBy(o => o.NodeName).ThenBy(o => o.PropName).ToList();

                            FileIOHelper.WriteListToCSVFile(nodesList, new NodeEntityReportMap(), FilePathMap.NodesIndexFilePath(jobTarget));
                            FileIOHelper.WriteListToCSVFile(entityNodeStartupOptionsList, new NodePropertyReportMap(), FilePathMap.NodeStartupOptionsIndexFilePath(jobTarget));
                            FileIOHelper.WriteListToCSVFile(entityNodePropertiesList, new NodePropertyReportMap(), FilePathMap.NodePropertiesIndexFilePath(jobTarget));
                            FileIOHelper.WriteListToCSVFile(entityNodeEnvironmentVariablesList, new NodePropertyReportMap(), FilePathMap.NodeEnvironmentVariablesIndexFilePath(jobTarget));
                        }

                        #endregion

                        #region Backends

                        List<AppDRESTBackend> backendsRESTList = FileIOHelper.LoadListOfObjectsFromFile<AppDRESTBackend>(FilePathMap.BackendsDataFilePath(jobTarget));
                        List<AppDRESTTier> tiersRESTList = FileIOHelper.LoadListOfObjectsFromFile<AppDRESTTier>(FilePathMap.TiersDataFilePath(jobTarget));
                        List<EntityBackend> backendsList = null;
                        if (backendsRESTList != null)
                        {
                            loggerConsole.Info("Index List of Backends ({0} entities)", backendsRESTList.Count);

                            stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + backendsRESTList.Count;

                            backendsList = new List<EntityBackend>(backendsRESTList.Count);

                            JObject backendsDetailsContainer = FileIOHelper.LoadJObjectFromFile(FilePathMap.BackendsDetailDataFilePath(jobTarget));
                            JArray backendsDetails = null;
                            if (backendsDetailsContainer != null)
                            {
                                backendsDetails = (JArray)backendsDetailsContainer["backendListEntries"];
                            }

                            foreach (AppDRESTBackend backend in backendsRESTList)
                            {
                                EntityBackend backendRow = new EntityBackend();
                                backendRow.ApplicationName = jobTarget.Application;
                                backendRow.ApplicationID = jobTarget.ApplicationID;
                                backendRow.BackendID = backend.id;
                                backendRow.BackendName = backend.name;
                                backendRow.BackendType = backend.exitPointType;
                                backendRow.Controller = jobTarget.Controller;
                                backendRow.NumProps = backend.properties.Count;
                                if (backend.properties.Count >= 1)
                                {
                                    backendRow.Prop1Name = backend.properties[0].name;
                                    backendRow.Prop1Value = backend.properties[0].value;
                                }
                                if (backend.properties.Count >= 2)
                                {
                                    backendRow.Prop2Name = backend.properties[1].name;
                                    backendRow.Prop2Value = backend.properties[1].value;
                                }
                                if (backend.properties.Count >= 3)
                                {
                                    backendRow.Prop3Name = backend.properties[2].name;
                                    backendRow.Prop3Value = backend.properties[2].value;
                                }
                                if (backend.properties.Count >= 4)
                                {
                                    backendRow.Prop4Name = backend.properties[3].name;
                                    backendRow.Prop4Value = backend.properties[3].value;
                                }
                                if (backend.properties.Count >= 5)
                                {
                                    backendRow.Prop5Name = backend.properties[4].name;
                                    backendRow.Prop5Value = backend.properties[4].value;
                                }
                                if (backend.properties.Count >= 6)
                                {
                                    backendRow.Prop6Name = backend.properties[5].name;
                                    backendRow.Prop6Value = backend.properties[5].value;
                                }
                                if (backend.properties.Count >= 7)
                                {
                                    backendRow.Prop7Name = backend.properties[6].name;
                                    backendRow.Prop7Value = backend.properties[6].value;
                                }

                                // Look up the type in the callInfo\metaInfo
                                if (backendsDetails != null)
                                {
                                    JObject backendDetail = (JObject)backendsDetails.Where(b => (long)b["id"] == backendRow.BackendID).FirstOrDefault();
                                    if (backendDetail != null)
                                    {
                                        bool additionalInfoLookupSucceeded = false;

                                        JArray metaInfoArray = (JArray)backendDetail["callInfo"]["metaInfo"];
                                        JToken metaInfoExitPoint = metaInfoArray.Where(m => m["name"].ToString() == "exit-point-type").FirstOrDefault();
                                        if (metaInfoExitPoint != null)
                                        {
                                            string betterBackendType = metaInfoExitPoint["value"].ToString();
                                            if (betterBackendType.Length > 0 && betterBackendType != backendRow.BackendType)
                                            {
                                                backendRow.BackendType = betterBackendType;
                                                additionalInfoLookupSucceeded = true;
                                            }
                                        }

                                        if (additionalInfoLookupSucceeded == false)
                                        {
                                            JObject resolutionInfoObject = (JObject)backendDetail["callInfo"]["resolutionInfo"];
                                            string betterBackendType = resolutionInfoObject["exitPointSubtype"].ToString();
                                            if (betterBackendType.Length > 0 && betterBackendType != backendRow.BackendType)
                                            {
                                                backendRow.BackendType = betterBackendType;
                                                additionalInfoLookupSucceeded = true;
                                            }
                                        }
                                    }
                                }

                                updateEntityWithDeeplinks(backendRow);
                                updateEntityWithEntityDetailAndFlameGraphLinks(backendRow, jobTarget, jobConfiguration.Input.TimeRange);

                                backendsList.Add(backendRow);
                            }
                            // Sort them
                            backendsList = backendsList.OrderBy(o => o.BackendType).ThenBy(o => o.BackendName).ToList();

                            FileIOHelper.WriteListToCSVFile(backendsList, new BackendEntityReportMap(), FilePathMap.BackendsIndexFilePath(jobTarget));
                        }

                        #endregion

                        #region Business Transactions

                        List<AppDRESTBusinessTransaction> businessTransactionsRESTList = FileIOHelper.LoadListOfObjectsFromFile<AppDRESTBusinessTransaction>(FilePathMap.BusinessTransactionsDataFilePath(jobTarget));
                        List<EntityBusinessTransaction> businessTransactionList = null;
                        if (businessTransactionsRESTList != null)
                        {
                            loggerConsole.Info("Index List of Business Transactions ({0} entities)", businessTransactionsRESTList.Count);

                            stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + businessTransactionsRESTList.Count;

                            businessTransactionList = new List<EntityBusinessTransaction>(businessTransactionsRESTList.Count);

                            foreach (AppDRESTBusinessTransaction businessTransaction in businessTransactionsRESTList)
                            {
                                EntityBusinessTransaction businessTransactionRow = new EntityBusinessTransaction();
                                businessTransactionRow.ApplicationID = jobTarget.ApplicationID;
                                businessTransactionRow.ApplicationName = jobTarget.Application;
                                businessTransactionRow.BTID = businessTransaction.id;
                                businessTransactionRow.BTName = businessTransaction.name;
                                businessTransactionRow.BTNameOriginal = businessTransaction.internalName;
                                businessTransactionRow.IsRenamed = !(businessTransaction.name == businessTransaction.internalName);
                                if (businessTransactionRow.BTName == "_APPDYNAMICS_DEFAULT_TX_")
                                {
                                    businessTransactionRow.BTType = "OVERFLOW";
                                }
                                else
                                {
                                    businessTransactionRow.BTType = businessTransaction.entryPointType;
                                }
                                businessTransactionRow.Controller = jobTarget.Controller;
                                businessTransactionRow.TierID = businessTransaction.tierId;
                                businessTransactionRow.TierName = businessTransaction.tierName;

                                updateEntityWithDeeplinks(businessTransactionRow);
                                updateEntityWithEntityDetailAndFlameGraphLinks(businessTransactionRow, jobTarget, jobConfiguration.Input.TimeRange);

                                businessTransactionList.Add(businessTransactionRow);
                            }

                            // Sort them
                            businessTransactionList = businessTransactionList.OrderBy(o => o.TierName).ThenBy(o => o.BTName).ToList();

                            FileIOHelper.WriteListToCSVFile(businessTransactionList, new BusinessTransactionEntityReportMap(), FilePathMap.BusinessTransactionsIndexFilePath(jobTarget));
                        }

                        #endregion

                        #region Service Endpoints

                        List<AppDRESTMetric> serviceEndpointsRESTList = FileIOHelper.LoadListOfObjectsFromFile<AppDRESTMetric>(FilePathMap.ServiceEndpointsDataFilePath(jobTarget));
                        List<EntityServiceEndpoint> serviceEndpointsList = null;
                        if (serviceEndpointsRESTList != null)
                        {
                            loggerConsole.Info("Index List of Service Endpoints ({0} entities)", serviceEndpointsRESTList.Count);

                            stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + serviceEndpointsRESTList.Count;

                            serviceEndpointsList = new List<EntityServiceEndpoint>(serviceEndpointsRESTList.Count);

                            JObject serviceEndpointsDetailsContainer = FileIOHelper.LoadJObjectFromFile(FilePathMap.ServiceEndpointsDetailDataFilePath(jobTarget));
                            JArray serviceEndpointsDetails = null;
                            if (serviceEndpointsDetailsContainer != null)
                            {
                                serviceEndpointsDetails = (JArray)serviceEndpointsDetailsContainer["serviceEndpointListEntries"];
                            }

                            foreach (AppDRESTMetric serviceEndpoint in serviceEndpointsRESTList)
                            {
                                EntityServiceEndpoint serviceEndpointRow = new EntityServiceEndpoint();
                                serviceEndpointRow.ApplicationID = jobTarget.ApplicationID;
                                serviceEndpointRow.ApplicationName = jobTarget.Application;
                                serviceEndpointRow.Controller = jobTarget.Controller;

                                // metricName
                                // BTM|Application Diagnostic Data|SEP:4855|Calls per Minute
                                //                                     ^^^^
                                //                                     ID
                                serviceEndpointRow.SEPID = Convert.ToInt32(serviceEndpoint.metricName.Split('|')[2].Split(':')[1]);

                                // metricPath
                                // Service Endpoints|ECommerce-Services|/appdynamicspilot/rest|Calls per Minute
                                //                                      ^^^^^^^^^^^^^^^^^^^^^^
                                //                                      SEP Name
                                serviceEndpointRow.SEPName = serviceEndpoint.metricPath.Split('|')[2];

                                serviceEndpointRow.TierName = serviceEndpoint.metricPath.Split('|')[1];
                                if (tiersRESTList != null)
                                {
                                    // metricPath
                                    // Service Endpoints|ECommerce-Services|/appdynamicspilot/rest|Calls per Minute
                                    //                   ^^^^^^^^^^^^^^^^^^
                                    //                   Tier
                                    AppDRESTTier tierForThisEntity = tiersRESTList.Where(tier => tier.name == serviceEndpointRow.TierName).FirstOrDefault();
                                    if (tierForThisEntity != null)
                                    {
                                        serviceEndpointRow.TierID = tierForThisEntity.id;
                                    }
                                }

                                JObject serviceEndpointDetail = (JObject)serviceEndpointsDetails.Where(s => (long)s["id"] == serviceEndpointRow.SEPID).FirstOrDefault();
                                if (serviceEndpointDetail != null)
                                {
                                    serviceEndpointRow.SEPType = serviceEndpointDetail["type"].ToString();
                                }

                                updateEntityWithDeeplinks(serviceEndpointRow);
                                updateEntityWithEntityDetailAndFlameGraphLinks(serviceEndpointRow, jobTarget, jobConfiguration.Input.TimeRange);

                                serviceEndpointsList.Add(serviceEndpointRow);
                            }

                            // Sort them
                            serviceEndpointsList = serviceEndpointsList.OrderBy(o => o.TierName).ThenBy(o => o.SEPName).ToList();

                            FileIOHelper.WriteListToCSVFile(serviceEndpointsList, new ServiceEndpointEntityReportMap(), FilePathMap.ServiceEndpointsIndexFilePath(jobTarget));
                        }

                        #endregion

                        #region Errors

                        List<AppDRESTMetric> errorsRESTList = FileIOHelper.LoadListOfObjectsFromFile<AppDRESTMetric>(FilePathMap.ErrorsDataFilePath(jobTarget));
                        List<EntityError> errorList = null;
                        if (errorsRESTList != null)
                        {
                            loggerConsole.Info("Index List of Errors ({0} entities)", errorsRESTList.Count);

                            stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + errorsRESTList.Count;

                            errorList = new List<EntityError>(errorsRESTList.Count);

                            foreach (AppDRESTMetric error in errorsRESTList)
                            {
                                EntityError errorRow = new EntityError();
                                errorRow.ApplicationID = jobTarget.ApplicationID;
                                errorRow.ApplicationName = jobTarget.Application;
                                errorRow.Controller = jobTarget.Controller;

                                // metricName
                                // BTM|Application Diagnostic Data|Error:11626|Errors per Minute
                                //                                       ^^^^^
                                //                                       ID
                                errorRow.ErrorID = Convert.ToInt32(error.metricName.Split('|')[2].Split(':')[1]);

                                // metricPath
                                // Errors|ECommerce-Services|CommunicationsException : EOFException|Errors per Minute
                                //                           ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
                                //                           Error Name
                                errorRow.ErrorName = error.metricPath.Split('|')[2];

                                errorRow.ErrorType = EntityError.ENTITY_TYPE;
                                // Do some analysis of the error type based on their name
                                if (errorRow.ErrorName.IndexOf("exception", 0, StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    errorRow.ErrorType = "Exception";
                                }
                                // For things like 
                                // CommunicationException : IOException : CommunicationException : SocketException
                                // ServletException : RollbackException : DatabaseException : SQLNestedException : NoSuchElementException
                                string[] errorTokens = errorRow.ErrorName.Split(':');
                                for (int j = 0; j < errorTokens.Length; j++)
                                {
                                    errorTokens[j] = errorTokens[j].Trim();
                                }
                                if (errorTokens.Length >= 1)
                                {
                                    errorRow.ErrorLevel1 = errorTokens[0];
                                }
                                if (errorTokens.Length >= 2)
                                {
                                    errorRow.ErrorLevel2 = errorTokens[1];
                                }
                                if (errorTokens.Length >= 3)
                                {
                                    errorRow.ErrorLevel3 = errorTokens[2];
                                }
                                if (errorTokens.Length >= 4)
                                {
                                    errorRow.ErrorLevel4 = errorTokens[3];
                                }
                                if (errorTokens.Length >= 5)
                                {
                                    errorRow.ErrorLevel5 = errorTokens[4];
                                }
                                errorRow.ErrorDepth = errorTokens.Length;

                                // Check if last thing is a 3 digit number, then cast it and see what comes out
                                if (errorTokens[errorTokens.Length - 1].Length == 3)
                                {
                                    int httpCode = -1;
                                    if (Int32.TryParse(errorTokens[errorTokens.Length - 1], out httpCode) == true)
                                    {
                                        // Hmm, likely to be a HTTP code
                                        errorRow.ErrorType = "HTTP";
                                        errorRow.HttpCode = httpCode;
                                    }
                                }

                                errorRow.TierName = error.metricPath.Split('|')[1];
                                if (tiersRESTList != null)
                                {
                                    // metricPath
                                    // Errors|ECommerce-Services|CommunicationsException : EOFException|Errors per Minute
                                    //        ^^^^^^^^^^^^^^^^^^
                                    //        Tier
                                    AppDRESTTier tierForThisEntity = tiersRESTList.Where(tier => tier.name == errorRow.TierName).FirstOrDefault();
                                    if (tierForThisEntity != null)
                                    {
                                        errorRow.TierID = tierForThisEntity.id;
                                    }
                                }

                                updateEntityWithDeeplinks(errorRow);
                                updateEntityWithEntityDetailAndFlameGraphLinks(errorRow, jobTarget, jobConfiguration.Input.TimeRange);

                                errorList.Add(errorRow);
                            }

                            // Sort them
                            errorList = errorList.OrderBy(o => o.TierName).ThenBy(o => o.ErrorName).ToList();

                            FileIOHelper.WriteListToCSVFile(errorList, new ErrorEntityReportMap(), FilePathMap.ErrorsIndexFilePath(jobTarget));
                        }

                        #endregion

                        #region Information Points

                        List<AppDRESTMetric> informationPointsRESTList = FileIOHelper.LoadListOfObjectsFromFile<AppDRESTMetric>(FilePathMap.InformationPointsDataFilePath(jobTarget));
                        List<EntityInformationPoint> informationPointsList = null;
                        if (informationPointsRESTList != null)
                        {
                            loggerConsole.Info("Index List of Information points ({0} entities)", informationPointsRESTList.Count);

                            stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + informationPointsRESTList.Count;

                            informationPointsList = new List<EntityInformationPoint>(informationPointsRESTList.Count);

                            JObject informationPointsDetailsContainer = FileIOHelper.LoadJObjectFromFile(FilePathMap.InformationPointsDetailDataFilePath(jobTarget));
                            JArray informationPointsDetails = null;
                            if (informationPointsDetailsContainer != null)
                            {
                                informationPointsDetails = (JArray)informationPointsDetailsContainer["informationPointsListViewEntries"];
                            }

                            foreach (AppDRESTMetric informationPoint in informationPointsRESTList)
                            {
                                EntityInformationPoint informationPointRow = new EntityInformationPoint();
                                informationPointRow.ApplicationID = jobTarget.ApplicationID;
                                informationPointRow.ApplicationName = jobTarget.Application;
                                informationPointRow.Controller = jobTarget.Controller;

                                if (informationPoint.metricName == "METRIC DATA NOT FOUND")
                                {
                                    informationPointRow.IPID = -1;
                                }
                                else
                                {
                                    // metricName
                                    // BTM|IPs|IP:5|Calls per Minute
                                    //            ^
                                    //            ID

                                    informationPointRow.IPID = Convert.ToInt32(informationPoint.metricName.Split('|')[2].Split(':')[1]);
                                }

                                // metricPath
                                // Information Points|Delete Cart|Calls per Minute
                                //                    ^^^^^^^^^^^
                                //                    IP Name
                                informationPointRow.IPName = informationPoint.metricPath.Split('|')[1];

                                if (informationPointRow.IPID != -1)
                                {
                                    JObject informationPointDetail = (JObject)informationPointsDetails.Where(e => (long)e["id"] == informationPointRow.IPID).FirstOrDefault();
                                    if (informationPointDetail != null)
                                    {
                                        informationPointRow.IPType = informationPointDetail["agentType"].ToString();
                                    }
                                }
                                else
                                {
                                    JObject informationPointDetail = (JObject)informationPointsDetails.Where(e => (string)e["name"] == informationPointRow.IPName).FirstOrDefault();
                                    if (informationPointDetail != null)
                                    {
                                        informationPointRow.IPType = informationPointDetail["agentType"].ToString();
                                        informationPointRow.IPID = (long)informationPointDetail["id"];
                                    }
                                }

                                updateEntityWithDeeplinks(informationPointRow);
                                updateEntityWithEntityDetailAndFlameGraphLinks(informationPointRow, jobTarget, jobConfiguration.Input.TimeRange);

                                informationPointsList.Add(informationPointRow);
                            }

                            // Sort them
                            informationPointsList = informationPointsList.OrderBy(o => o.IPName).ToList();

                            FileIOHelper.WriteListToCSVFile(informationPointsList, new InformationPointEntityReportMap(), FilePathMap.InformationPointsIndexFilePath(jobTarget));
                        }

                        #endregion

                        #region Tiers

                        List<EntityTier> tiersList = null;
                        if (tiersRESTList != null)
                        {
                            loggerConsole.Info("Index List of Tiers ({0} entities)", tiersRESTList.Count);

                            stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + tiersRESTList.Count;

                            tiersList = new List<EntityTier>(tiersRESTList.Count);

                            foreach (AppDRESTTier tier in tiersRESTList)
                            {
                                EntityTier tierRow = new EntityTier();
                                tierRow.AgentType = tier.agentType;
                                tierRow.ApplicationID = jobTarget.ApplicationID;
                                tierRow.ApplicationName = jobTarget.Application;
                                tierRow.Description = tier.description;
                                tierRow.Controller = jobTarget.Controller;
                                tierRow.TierID = tier.id;
                                tierRow.TierName = tier.name;
                                tierRow.TierType = tier.type;
                                tierRow.NumNodes = tier.numberOfNodes;
                                if (businessTransactionsRESTList != null)
                                {
                                    tierRow.NumBTs = businessTransactionsRESTList.Where<AppDRESTBusinessTransaction>(b => b.tierId == tierRow.TierID).Count();
                                }
                                if (serviceEndpointsList != null)
                                {
                                    tierRow.NumSEPs = serviceEndpointsList.Where<EntityServiceEndpoint>(s => s.TierID == tierRow.TierID).Count();
                                }
                                if (errorList != null)
                                {
                                    tierRow.NumErrors = errorList.Where<EntityError>(s => s.TierID == tierRow.TierID).Count();
                                }

                                updateEntityWithDeeplinks(tierRow);
                                updateEntityWithEntityDetailAndFlameGraphLinks(tierRow, jobTarget, jobConfiguration.Input.TimeRange);

                                tiersList.Add(tierRow);
                            }

                            // Sort them
                            tiersList = tiersList.OrderBy(o => o.TierName).ToList();

                            FileIOHelper.WriteListToCSVFile(tiersList, new TierEntityReportMap(), FilePathMap.TiersIndexFilePath(jobTarget));
                        }

                        #endregion

                        #region Application

                        if (applicationsRESTList != null)
                        {
                            loggerConsole.Info("Index List of Applications");

                            stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + 1;

                            List<EntityApplication> applicationsList = FileIOHelper.ReadListFromCSVFile<EntityApplication>(FilePathMap.ApplicationsIndexFilePath(jobTarget), new ApplicationEntityReportMap());

                            if (applicationsList == null || applicationsList.Count == 0)
                            {
                                // First time, let's output these rows
                                applicationsList = new List<EntityApplication>(applicationsRESTList.Count);
                                foreach (AppDRESTApplication application in applicationsRESTList)
                                {
                                    EntityApplication applicationsRow = new EntityApplication();
                                    applicationsRow.ApplicationName = application.name;
                                    applicationsRow.Description = application.description;
                                    applicationsRow.ApplicationID = application.id;
                                    applicationsRow.Controller = jobTarget.Controller;

                                    updateEntityWithDeeplinks(applicationsRow);
                                    updateEntityWithEntityDetailAndFlameGraphLinks(applicationsRow, jobTarget, jobConfiguration.Input.TimeRange);

                                    applicationsList.Add(applicationsRow);
                                }
                            }

                            // Update counts of entities for this application row
                            EntityApplication applicationRow = applicationsList.Where(a => a.ApplicationID == jobTarget.ApplicationID).FirstOrDefault();
                            if (applicationRow != null)
                            {
                                if (tiersList != null) applicationRow.NumTiers = tiersList.Count;
                                if (nodesList != null) applicationRow.NumNodes = nodesList.Count;
                                if (backendsList != null) applicationRow.NumBackends = backendsList.Count;
                                if (businessTransactionList != null) applicationRow.NumBTs = businessTransactionList.Count;
                                if (serviceEndpointsList != null) applicationRow.NumSEPs = serviceEndpointsList.Count;
                                if (errorList != null) applicationRow.NumErrors = errorList.Count;
                                if (informationPointsList != null) applicationRow.NumIPs = informationPointsList.Count;

                                List<EntityApplication> applicationRows = new List<EntityApplication>(1);
                                applicationRows.Add(applicationRow);

                                // Write just this row for this application
                                FileIOHelper.WriteListToCSVFile(applicationRows, new ApplicationEntityReportMap(), FilePathMap.ApplicationIndexFilePath(jobTarget));
                            }

                            // Sort them
                            applicationsList = applicationsList.OrderBy(o => o.Controller).ThenBy(o => o.ApplicationName).ToList();

                            FileIOHelper.WriteListToCSVFile(applicationsList, new ApplicationEntityReportMap(), FilePathMap.ApplicationsIndexFilePath(jobTarget));
                        }

                        #endregion

                        #region Combine All for Report CSV

                        // If it is the first one, clear out the combined folder
                        if (i == 0)
                        {
                            FileIOHelper.DeleteFolder(FilePathMap.EntitiesReportFolderPath());
                            Thread.Sleep(1000);
                            FileIOHelper.CreateFolder(FilePathMap.EntitiesReportFolderPath());
                        }

                        // Append all the individual application files into one
                        if (File.Exists(FilePathMap.TiersIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.TiersIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.TiersReportFilePath(), FilePathMap.TiersIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.NodesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.NodesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.NodesReportFilePath(), FilePathMap.NodesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.NodeStartupOptionsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.NodeStartupOptionsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.NodeStartupOptionsReportFilePath(), FilePathMap.NodeStartupOptionsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.NodePropertiesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.NodePropertiesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.NodePropertiesReportFilePath(), FilePathMap.NodePropertiesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.NodeEnvironmentVariablesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.NodeEnvironmentVariablesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.NodeEnvironmentVariablesReportFilePath(), FilePathMap.NodeEnvironmentVariablesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.BackendsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.BackendsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.BackendsReportFilePath(), FilePathMap.BackendsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.BusinessTransactionsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.BusinessTransactionsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.BusinessTransactionsReportFilePath(), FilePathMap.BusinessTransactionsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.ServiceEndpointsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.ServiceEndpointsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.ServiceEndpointsReportFilePath(), FilePathMap.ServiceEndpointsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.ErrorsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.ErrorsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.ErrorsReportFilePath(), FilePathMap.ErrorsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.InformationPointsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.InformationPointsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.InformationPointsReportFilePath(), FilePathMap.InformationPointsIndexFilePath(jobTarget));
                        }
                        // If it is the last one, let's append all Applications
                        if (i == jobConfiguration.Target.Count - 1)
                        {
                            var controllers = jobConfiguration.Target.GroupBy(t => t.Controller);
                            foreach (var controllerGroup in controllers)
                            {
                                FileIOHelper.AppendTwoCSVFiles(FilePathMap.ApplicationsReportFilePath(), FilePathMap.ApplicationsIndexFilePath(controllerGroup.ToList()[0]));
                                FileIOHelper.AppendTwoCSVFiles(FilePathMap.ControllersReportFilePath(), FilePathMap.ControllerIndexFilePath(controllerGroup.ToList()[0]));
                            }
                        }

                        #endregion
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex);
                        loggerConsole.Warn(ex);
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
            logger.Trace("{0} is always executed", jobConfiguration.Status);
            loggerConsole.Trace("{0} is always executed", jobConfiguration.Status);
            return true;
        }

        private void updateEntityWithEntityDetailAndFlameGraphLinks(EntityBase entity, JobTarget jobTarget, JobTimeRange jobTimeRange)
        {
            entity.DetailLink = String.Format(@"=HYPERLINK(""{0}"", ""<Detail>"")", FilePathMap.EntityMetricAndDetailExcelReportFilePath(entity, jobTarget, jobTimeRange, false));
            entity.FlameGraphLink = String.Format(@"=HYPERLINK(""{0}"", ""<FlGraph>"")", FilePathMap.FlameGraphReportFilePath(entity, jobTarget, jobTimeRange, false));
            entity.FlameChartLink = String.Format(@"=HYPERLINK(""{0}"", ""<FlChart>"")", FilePathMap.FlameChartReportFilePath(entity, jobTarget, jobTimeRange, false));
            entity.MetricGraphLink = String.Format(@"=HYPERLINK(""{0}"", ""<Metrics>"")", FilePathMap.EntityTypeMetricGraphsExcelReportFilePath(entity, jobTarget, jobTimeRange, false));
        }
    }
}
