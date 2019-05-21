using AppDynamics.Dexter.DataObjects;
using AppDynamics.Dexter.ReportObjectMaps;
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
    public class IndexAPMEntities : JobStepIndexBase
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

                if (jobConfiguration.Target.Count(t => t.Type == APPLICATION_TYPE_APM) == 0)
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

                    if (jobTarget.Type != null && jobTarget.Type.Length > 0 && jobTarget.Type != APPLICATION_TYPE_APM) continue;

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

                        #region Nodes and Node Properties

                        List<AppDRESTNode> nodesRESTList = FileIOHelper.LoadListOfObjectsFromFile<AppDRESTNode>(FilePathMap.APMNodesDataFilePath(jobTarget));
                        List<APMNode> nodesList = null;
                        List<APMNodeProperty> entityNodeStartupOptionsList = null;
                        List<APMNodeProperty> entityNodePropertiesList = null;
                        List<APMNodeProperty> entityNodeEnvironmentVariablesList = null;
                        if (nodesRESTList != null)
                        {
                            loggerConsole.Info("Index List of Nodes and Node Properties ({0} entities)", nodesRESTList.Count);

                            stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + nodesRESTList.Count;

                            nodesList = new List<APMNode>(nodesRESTList.Count);
                            entityNodeStartupOptionsList = new List<APMNodeProperty>(nodesRESTList.Count * 25);
                            entityNodePropertiesList = new List<APMNodeProperty>(nodesRESTList.Count * 25);
                            entityNodeEnvironmentVariablesList = new List<APMNodeProperty>(nodesRESTList.Count * 25);

                            foreach (AppDRESTNode nodeREST in nodesRESTList)
                            {
                                APMNode node = new APMNode();
                                node.NodeID = nodeREST.id;
                                node.AgentPresent = nodeREST.appAgentPresent;
                                node.AgentType = nodeREST.agentType;
                                node.AgentVersion = nodeREST.appAgentVersion;
                                node.ApplicationName = jobTarget.Application;
                                node.ApplicationID = jobTarget.ApplicationID;
                                node.Controller = jobTarget.Controller;
                                node.MachineAgentPresent = nodeREST.machineAgentPresent;
                                node.MachineAgentVersion = nodeREST.machineAgentVersion;
                                node.MachineID = nodeREST.machineId;
                                node.MachineName = nodeREST.machineName;
                                node.MachineOSType = nodeREST.machineOSType;
                                node.NodeName = nodeREST.name;
                                node.TierID = nodeREST.tierId;
                                node.TierName = nodeREST.tierName;
                                node.MachineType = nodeREST.type;
                                if (node.AgentVersion != String.Empty)
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
                                    //Server Agent v4.4.2 GA #4.4.2.22394 rnull null

                                    // Apache agent looks like this
                                    // Proxy v4.2.5.1 GA SHA-1:.ad6c804882f518b3350f422489866ea2008cd664 #13146 35-4.2.5.next-build

                                    Regex regexVersion = new Regex(@"(?i)(\d*\.\d*\.\d*(\.\d*)?).*", RegexOptions.IgnoreCase);
                                    Match match = regexVersion.Match(node.AgentVersion);
                                    if (match != null)
                                    {
                                        if (match.Groups.Count > 1)
                                        {
                                            node.AgentVersionRaw = node.AgentVersion;
                                            node.AgentVersion = match.Groups[1].Value;
                                            if (node.AgentVersion.Count(v => v == '.') < 3)
                                            {
                                                node.AgentVersion = String.Format("{0}.0", node.AgentVersion);
                                            }
                                        }
                                    }
                                }
                                if (node.MachineAgentVersion != String.Empty)
                                {
                                    // Machine agent looks like that 
                                    //Machine Agent v4.2.3.2 GA Build Date 2016 - 07 - 11 10:26:01
                                    //Machine Agent v3.7.16.0 GA Build Date 2014 - 02 - 26 21:20:29
                                    //Machine Agent v4.2.3.2 GA Build Date 2016 - 07 - 11 10:17:54
                                    //Machine Agent v4.0.6.0 GA Build Date 2015 - 05 - 11 20:56:44
                                    //Machine Agent v3.8.3.0 GA Build Date 2014 - 06 - 06 17:09:13
                                    //Machine Agent v4.1.7.1 GA Build Date 2015 - 11 - 24 20:49:24

                                    Regex regexVersion = new Regex(@"(?i)(\d*\.\d*\.\d*(\.\d*)?).*", RegexOptions.IgnoreCase);
                                    Match match = regexVersion.Match(node.MachineAgentVersion);
                                    if (match != null)
                                    {
                                        if (match.Groups.Count > 1)
                                        {
                                            node.MachineAgentVersionRaw = node.MachineAgentVersion;
                                            node.MachineAgentVersion = match.Groups[1].Value;
                                            if (node.MachineAgentVersion.Count(v => v == '.') < 3)
                                            {
                                                node.MachineAgentVersion = String.Format("{0}.0", node.MachineAgentVersion);
                                            }
                                        }
                                    }
                                }

                                updateEntityWithDeeplinks(node);
                                updateEntityWithEntityDetailAndFlameGraphLinks(node, jobTarget, jobConfiguration.Input.TimeRange);

                                // Node properties (JVM Tab)
                                JObject nodeProperties = FileIOHelper.LoadJObjectFromFile(FilePathMap.APMNodeRuntimePropertiesDataFilePath(jobTarget, nodeREST));
                                if (nodeProperties != null)
                                {
                                    node.AgentRuntime = getStringValueFromJToken(nodeProperties, "latestAgentRuntime");

                                    node.InstallDirectory = getStringValueFromJToken(nodeProperties, "installDir");
                                    node.InstallTime = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(nodeProperties, "installTime"));

                                    node.LastStartTime = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(nodeProperties, "lastStartTime"));

                                    node.IsDisabled = getBoolValueFromJToken(nodeProperties, "disable");
                                    node.IsMonitoringDisabled = getBoolValueFromJToken(nodeProperties, "disableMonitoring");

                                    if (isTokenPropertyNull(nodeProperties, "latestVmStartupOptions") == false)
                                    {
                                        node.NumStartupOptions = nodeProperties["latestVmStartupOptions"].Count();
                                        foreach (JValue nodeStartupOptionObject in nodeProperties["latestVmStartupOptions"])
                                        {
                                            APMNodeProperty nodePropertyRow = new APMNodeProperty();
                                            nodePropertyRow.NodeID = node.NodeID;
                                            nodePropertyRow.AgentType = node.AgentType;
                                            nodePropertyRow.ApplicationName = node.ApplicationName;
                                            nodePropertyRow.ApplicationID = node.ApplicationID;
                                            nodePropertyRow.Controller = node.Controller;
                                            nodePropertyRow.NodeName = node.NodeName;
                                            nodePropertyRow.TierID = node.TierID;
                                            nodePropertyRow.TierName = node.TierName;

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
                                    if (isTokenPropertyNull(nodeProperties, "latestVmSystemProperties") == false)
                                    {
                                        node.NumProperties = nodeProperties["latestVmSystemProperties"].Count();
                                        foreach (JObject nodePropertyObject in nodeProperties["latestVmSystemProperties"])
                                        {
                                            APMNodeProperty nodePropertyRow = new APMNodeProperty();
                                            nodePropertyRow.NodeID = node.NodeID;
                                            nodePropertyRow.AgentType = node.AgentType;
                                            nodePropertyRow.ApplicationName = node.ApplicationName;
                                            nodePropertyRow.ApplicationID = node.ApplicationID;
                                            nodePropertyRow.Controller = node.Controller;
                                            nodePropertyRow.NodeName = node.NodeName;
                                            nodePropertyRow.TierID = node.TierID;
                                            nodePropertyRow.TierName = node.TierName;

                                            nodePropertyRow.PropName = getStringValueFromJToken(nodePropertyObject, "name");
                                            nodePropertyRow.PropValue = getStringValueFromJToken(nodePropertyObject, "value");

                                            entityNodePropertiesList.Add(nodePropertyRow);
                                        }
                                    }
                                    if (isTokenPropertyNull(nodeProperties, "latestEnvironmentVariables") == false)
                                    {
                                        node.NumEnvVariables = nodeProperties["latestEnvironmentVariables"].Count();
                                        foreach (JObject nodePropertyObject in nodeProperties["latestEnvironmentVariables"])
                                        {
                                            APMNodeProperty nodePropertyRow = new APMNodeProperty();
                                            nodePropertyRow.NodeID = node.NodeID;
                                            nodePropertyRow.AgentType = node.AgentType;
                                            nodePropertyRow.ApplicationName = node.ApplicationName;
                                            nodePropertyRow.ApplicationID = node.ApplicationID;
                                            nodePropertyRow.Controller = node.Controller;
                                            nodePropertyRow.NodeName = node.NodeName;
                                            nodePropertyRow.TierID = node.TierID;
                                            nodePropertyRow.TierName = node.TierName;

                                            nodePropertyRow.PropName = getStringValueFromJToken(nodePropertyObject, "name");
                                            nodePropertyRow.PropValue = getStringValueFromJToken(nodePropertyObject, "value");

                                            entityNodeEnvironmentVariablesList.Add(nodePropertyRow);
                                        }
                                    }
                                }

                                // Node metadata (Agent Tab)
                                JObject nodeMetadata = FileIOHelper.LoadJObjectFromFile(FilePathMap.APMNodeMetadataDataFilePath(jobTarget, nodeREST));
                                if (nodeMetadata != null)
                                {
                                    if (isTokenPropertyNull(nodeMetadata, "applicationComponentNode") == false && isTokenPropertyNull(nodeMetadata["applicationComponentNode"], "metaInfo") == false)
                                    {
                                        node.NumProperties = node.NumProperties + nodeMetadata["applicationComponentNode"]["metaInfo"].Count();

                                        foreach (JObject nodePropertyObject in nodeMetadata["applicationComponentNode"]["metaInfo"])
                                        {
                                            APMNodeProperty nodePropertyRow = new APMNodeProperty();
                                            nodePropertyRow.NodeID = node.NodeID;
                                            nodePropertyRow.AgentType = node.AgentType;
                                            nodePropertyRow.ApplicationName = node.ApplicationName;
                                            nodePropertyRow.ApplicationID = node.ApplicationID;
                                            nodePropertyRow.Controller = node.Controller;
                                            nodePropertyRow.NodeName = node.NodeName;
                                            nodePropertyRow.TierID = node.TierID;
                                            nodePropertyRow.TierName = node.TierName;

                                            nodePropertyRow.PropName = getStringValueFromJToken(nodePropertyObject, "name");
                                            nodePropertyRow.PropValue = getStringValueFromJToken(nodePropertyObject, "value");

                                            entityNodePropertiesList.Add(nodePropertyRow);
                                        }
                                    }
                                }

                                nodesList.Add(node);
                            }

                            // Sort them
                            nodesList = nodesList.OrderBy(o => o.TierName).ThenBy(o => o.NodeName).ToList();
                            entityNodeStartupOptionsList = entityNodeStartupOptionsList.OrderBy(o => o.TierName).ThenBy(o => o.NodeName).ThenBy(o => o.PropName).ToList();
                            entityNodePropertiesList = entityNodePropertiesList.OrderBy(o => o.TierName).ThenBy(o => o.NodeName).ThenBy(o => o.PropName).ToList();
                            entityNodeEnvironmentVariablesList = entityNodeEnvironmentVariablesList.OrderBy(o => o.TierName).ThenBy(o => o.NodeName).ThenBy(o => o.PropName).ToList();

                            FileIOHelper.WriteListToCSVFile(nodesList, new APMNodeReportMap(), FilePathMap.APMNodesIndexFilePath(jobTarget));
                            FileIOHelper.WriteListToCSVFile(entityNodeStartupOptionsList, new APMNodePropertyReportMap(), FilePathMap.APMNodeStartupOptionsIndexFilePath(jobTarget));
                            FileIOHelper.WriteListToCSVFile(entityNodePropertiesList, new APMNodePropertyReportMap(), FilePathMap.APMNodePropertiesIndexFilePath(jobTarget));
                            FileIOHelper.WriteListToCSVFile(entityNodeEnvironmentVariablesList, new APMNodePropertyReportMap(), FilePathMap.APMNodeEnvironmentVariablesIndexFilePath(jobTarget));
                        }

                        #endregion

                        #region Backends

                        List<AppDRESTBackend> backendsRESTList = FileIOHelper.LoadListOfObjectsFromFile<AppDRESTBackend>(FilePathMap.APMBackendsDataFilePath(jobTarget));
                        List<AppDRESTTier> tiersRESTList = FileIOHelper.LoadListOfObjectsFromFile<AppDRESTTier>(FilePathMap.APMTiersDataFilePath(jobTarget));
                        List<APMBackend> backendsList = null;
                        if (backendsRESTList != null)
                        {
                            loggerConsole.Info("Index List of Backends ({0} entities)", backendsRESTList.Count);

                            backendsList = new List<APMBackend>(backendsRESTList.Count);

                            JObject backendsDetailsContainer = FileIOHelper.LoadJObjectFromFile(FilePathMap.APMBackendsDetailDataFilePath(jobTarget));
                            JArray backendsDetails = null;
                            if (isTokenPropertyNull(backendsDetailsContainer, "backendListEntries") == false)
                            {
                                backendsDetails = (JArray)backendsDetailsContainer["backendListEntries"];
                            }

                            foreach (AppDRESTBackend backendREST in backendsRESTList)
                            {
                                APMBackend backend = new APMBackend();
                                backend.ApplicationName = jobTarget.Application;
                                backend.ApplicationID = jobTarget.ApplicationID;
                                backend.BackendID = backendREST.id;
                                backend.BackendName = backendREST.name;
                                backend.BackendType = backendREST.exitPointType;
                                backend.Controller = jobTarget.Controller;
                                backend.NumProps = backendREST.properties.Count;
                                if (backend.NumProps >= 1)
                                {
                                    backend.Prop1Name = backendREST.properties[0].name;
                                    backend.Prop1Value = backendREST.properties[0].value;
                                }
                                if (backend.NumProps >= 2)
                                {
                                    backend.Prop2Name = backendREST.properties[1].name;
                                    backend.Prop2Value = backendREST.properties[1].value;
                                }
                                if (backend.NumProps >= 3)
                                {
                                    backend.Prop3Name = backendREST.properties[2].name;
                                    backend.Prop3Value = backendREST.properties[2].value;
                                }
                                if (backend.NumProps >= 4)
                                {
                                    backend.Prop4Name = backendREST.properties[3].name;
                                    backend.Prop4Value = backendREST.properties[3].value;
                                }
                                if (backend.NumProps >= 5)
                                {
                                    backend.Prop5Name = backendREST.properties[4].name;
                                    backend.Prop5Value = backendREST.properties[4].value;
                                }
                                if (backend.NumProps >= 6)
                                {
                                    backend.Prop6Name = backendREST.properties[5].name;
                                    backend.Prop6Value = backendREST.properties[5].value;
                                }
                                if (backend.NumProps >= 7)
                                {
                                    backend.Prop7Name = backendREST.properties[6].name;
                                    backend.Prop7Value = backendREST.properties[6].value;
                                }

                                // Look up the type in the callInfo\metaInfo
                                if (backendsDetails != null)
                                {
                                    JObject backendDetail = (JObject)backendsDetails.Where(b => (long)b["id"] == backend.BackendID).FirstOrDefault();
                                    if (backendDetail != null)
                                    {
                                        bool additionalInfoLookupSucceeded = false;

                                        try
                                        {
                                            JArray metaInfoArray = (JArray)backendDetail["callInfo"]["metaInfo"];
                                            JToken metaInfoExitPoint = metaInfoArray.Where(m => m["name"].ToString() == "exit-point-type").FirstOrDefault();
                                            if (metaInfoExitPoint != null)
                                            {
                                                string betterBackendType = getStringValueFromJToken(metaInfoExitPoint, "value");
                                                if (betterBackendType.Length > 0 && betterBackendType != backend.BackendType)
                                                {
                                                    backend.BackendType = betterBackendType;
                                                    additionalInfoLookupSucceeded = true;
                                                }
                                            }
                                        } catch { }

                                        if (additionalInfoLookupSucceeded == false)
                                        {
                                            try
                                            {
                                                JObject resolutionInfoObject = (JObject)backendDetail["callInfo"]["resolutionInfo"];
                                                string betterBackendType = getStringValueFromJToken(resolutionInfoObject, "exitPointSubtype");
                                                if (betterBackendType.Length > 0 && betterBackendType != backend.BackendType)
                                                {
                                                    backend.BackendType = betterBackendType;
                                                    additionalInfoLookupSucceeded = true;
                                                }
                                            } catch { }
                                        }
                                    }
                                }

                                JObject backendToDBMonMapping = FileIOHelper.LoadJObjectFromFile(FilePathMap.APMBackendToDBMonMappingDataFilePath(jobTarget, backendREST));
                                if (backendToDBMonMapping != null)
                                {
                                    backend.DBMonCollectorName = getStringValueFromJToken(backendToDBMonMapping, "name");
                                    backend.DBMonCollectorType = getStringValueFromJToken(backendToDBMonMapping, "type");
                                    backend.DBMonCollectorConfigID = getLongValueFromJToken(backendToDBMonMapping, "id");
                                }

                                updateEntityWithDeeplinks(backend);
                                updateEntityWithEntityDetailAndFlameGraphLinks(backend, jobTarget, jobConfiguration.Input.TimeRange);

                                backendsList.Add(backend);
                            }
                            // Sort them
                            backendsList = backendsList.OrderBy(o => o.BackendType).ThenBy(o => o.BackendName).ToList();

                            FileIOHelper.WriteListToCSVFile(backendsList, new APMBackendReportMap(), FilePathMap.APMBackendsIndexFilePath(jobTarget));

                            stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + backendsList.Count;
                        }

                        #endregion

                        #region Business Transactions

                        List<AppDRESTBusinessTransaction> businessTransactionsRESTList = FileIOHelper.LoadListOfObjectsFromFile<AppDRESTBusinessTransaction>(FilePathMap.APMBusinessTransactionsDataFilePath(jobTarget));
                        List<APMBusinessTransaction> businessTransactionsList = null;
                        if (businessTransactionsRESTList != null)
                        {
                            loggerConsole.Info("Index List of Business Transactions ({0} entities)", businessTransactionsRESTList.Count);

                            businessTransactionsList = new List<APMBusinessTransaction>(businessTransactionsRESTList.Count);

                            foreach (AppDRESTBusinessTransaction businessTransactionREST in businessTransactionsRESTList)
                            {
                                APMBusinessTransaction businessTransaction = new APMBusinessTransaction();
                                businessTransaction.ApplicationID = jobTarget.ApplicationID;
                                businessTransaction.ApplicationName = jobTarget.Application;
                                businessTransaction.BTID = businessTransactionREST.id;
                                businessTransaction.BTName = businessTransactionREST.name;
                                businessTransaction.BTNameOriginal = businessTransactionREST.internalName;
                                businessTransaction.IsRenamed = !(businessTransactionREST.name == businessTransactionREST.internalName);
                                if (businessTransaction.BTName == "_APPDYNAMICS_DEFAULT_TX_")
                                {
                                    businessTransaction.BTType = "OVERFLOW";
                                }
                                else
                                {
                                    businessTransaction.BTType = businessTransactionREST.entryPointType;
                                }
                                businessTransaction.Controller = jobTarget.Controller;
                                businessTransaction.TierID = businessTransactionREST.tierId;
                                businessTransaction.TierName = businessTransactionREST.tierName;

                                updateEntityWithDeeplinks(businessTransaction);
                                updateEntityWithEntityDetailAndFlameGraphLinks(businessTransaction, jobTarget, jobConfiguration.Input.TimeRange);

                                businessTransactionsList.Add(businessTransaction);
                            }

                            // Sort them
                            businessTransactionsList = businessTransactionsList.OrderBy(o => o.TierName).ThenBy(o => o.BTName).ToList();

                            FileIOHelper.WriteListToCSVFile(businessTransactionsList, new APMBusinessTransactionReportMap(), FilePathMap.APMBusinessTransactionsIndexFilePath(jobTarget));

                            stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + businessTransactionsList.Count;
                        }

                        #endregion

                        #region Service Endpoints

                        List<APMServiceEndpoint> serviceEndpointsList = null;

                        List<AppDRESTMetric> serviceEndpointsRESTList = FileIOHelper.LoadListOfObjectsFromFile<AppDRESTMetric>(FilePathMap.APMServiceEndpointsDataFilePath(jobTarget));
                        JObject serviceEndpointsDetailsObject = FileIOHelper.LoadJObjectFromFile(FilePathMap.APMServiceEndpointsDetailDataFilePath(jobTarget));
                        if (isTokenPropertyNull(serviceEndpointsDetailsObject, "serviceEndpointListEntries") == false)
                        {
                            JArray serviceEndpointsArray = (JArray)serviceEndpointsDetailsObject["serviceEndpointListEntries"];

                            loggerConsole.Info("Index List of Service Endpoints ({0} entities)", serviceEndpointsArray.Count);

                            serviceEndpointsList = new List<APMServiceEndpoint>(serviceEndpointsArray.Count);

                            foreach (JObject serviceEndpointObject in serviceEndpointsArray)
                            {
                                APMServiceEndpoint serviceEndpoint = new APMServiceEndpoint();
                                serviceEndpoint.ApplicationID = jobTarget.ApplicationID;
                                serviceEndpoint.ApplicationName = jobTarget.Application;
                                serviceEndpoint.Controller = jobTarget.Controller;

                                serviceEndpoint.SEPID = getLongValueFromJToken(serviceEndpointObject, "id");
                                serviceEndpoint.SEPName = getStringValueFromJToken(serviceEndpointObject, "name");
                                serviceEndpoint.SEPType = getStringValueFromJToken(serviceEndpointObject, "type");
                                serviceEndpoint.TierID = getLongValueFromJToken(serviceEndpointObject, "applicationComponentId");
                                if (tiersRESTList != null)
                                {
                                    AppDRESTTier tierForThisEntity = tiersRESTList.Where(tier => tier.id == serviceEndpoint.TierID).FirstOrDefault();
                                    if (tierForThisEntity != null)
                                    {
                                        serviceEndpoint.TierName = tierForThisEntity.name;
                                    }
                                }

                                updateEntityWithDeeplinks(serviceEndpoint);
                                updateEntityWithEntityDetailAndFlameGraphLinks(serviceEndpoint, jobTarget, jobConfiguration.Input.TimeRange);

                                serviceEndpointsList.Add(serviceEndpoint);
                            }

                            // Sort them
                            serviceEndpointsList = serviceEndpointsList.OrderBy(o => o.TierName).ThenBy(o => o.SEPName).ToList();

                            FileIOHelper.WriteListToCSVFile(serviceEndpointsList, new APMServiceEndpointReportMap(), FilePathMap.APMServiceEndpointsIndexFilePath(jobTarget));

                            stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + serviceEndpointsList.Count;
                        }

                        #endregion

                        #region Errors

                        List<AppDRESTMetric> errorsRESTList = FileIOHelper.LoadListOfObjectsFromFile<AppDRESTMetric>(FilePathMap.APMErrorsDataFilePath(jobTarget));
                        List<APMError> errorList = null;
                        if (errorsRESTList != null)
                        {
                            loggerConsole.Info("Index List of Errors ({0} entities)", errorsRESTList.Count);

                            errorList = new List<APMError>(errorsRESTList.Count);

                            foreach (AppDRESTMetric errorREST in errorsRESTList)
                            {
                                APMError error = new APMError();
                                error.ApplicationID = jobTarget.ApplicationID;
                                error.ApplicationName = jobTarget.Application;
                                error.Controller = jobTarget.Controller;

                                if (String.Compare(errorREST.metricName, "METRIC DATA NOT FOUND", true) == 0)
                                {
                                    error.ErrorID = errorREST.metricId * -1;
                                }
                                else
                                {
                                    // Parse ID
                                    // metricName
                                    // BTM|Application Diagnostic Data|Error:11626|Errors per Minute
                                    //                                       ^^^^^
                                    //                                       ID
                                    try
                                    {
                                        error.ErrorID = Convert.ToInt32(errorREST.metricName.Split('|')[2].Split(':')[1]);
                                    }
                                    catch (IndexOutOfRangeException ex)
                                    {
                                        // No error ID in the path. Let's take the MetricID as error number, and make it negative to indicate it is not real
                                        error.ErrorID = errorREST.metricId * -1;
                                    }
                                }

                                string[] metricPathTokens = errorREST.metricPath.Split('|');
                                if (metricPathTokens.Length > 0)
                                {
                                    // Parse Tier
                                    // metricPath
                                    // Errors|ECommerce-Services|CommunicationsException : EOFException|Errors per Minute
                                    //        ^^^^^^^^^^^^^^^^^^
                                    //        Tier
                                    try
                                    {
                                        error.TierName = metricPathTokens[1];
                                    }
                                    catch (IndexOutOfRangeException ex)
                                    {
                                        error.TierName = "COULD NOT PARSE";
                                    }
                                    if (tiersRESTList != null)
                                    {
                                        AppDRESTTier tierForThisEntity = tiersRESTList.Where(tier => tier.name == error.TierName).FirstOrDefault();
                                        if (tierForThisEntity != null)
                                        {
                                            error.TierID = tierForThisEntity.id;
                                        }
                                    }

                                    // Parse Name
                                    // metricPath
                                    // Errors|ECommerce-Services|CommunicationsException : EOFException|Errors per Minute
                                    //                           ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
                                    //                           Error Name
                                    try
                                    {
                                        error.ErrorName = metricPathTokens[2];
                                    }
                                    catch (IndexOutOfRangeException ex)
                                    {
                                        error.ErrorName = "COULD NOT PARSE";
                                    }
                                }

                                error.ErrorType = APMError.ENTITY_TYPE;
                                // Do some analysis of the error type based on their name
                                if (error.ErrorName.IndexOf("exception", 0, StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    error.ErrorType = "Exception";
                                }
                                // For things like 
                                // CommunicationException : IOException : CommunicationException : SocketException
                                // ServletException : RollbackException : DatabaseException : SQLNestedException : NoSuchElementException
                                string[] errorTokens = error.ErrorName.Split(':');
                                for (int j = 0; j < errorTokens.Length; j++)
                                {
                                    errorTokens[j] = errorTokens[j].Trim();
                                }
                                if (errorTokens.Length >= 1)
                                {
                                    error.ErrorLevel1 = errorTokens[0];
                                }
                                if (errorTokens.Length >= 2)
                                {
                                    error.ErrorLevel2 = errorTokens[1];
                                }
                                if (errorTokens.Length >= 3)
                                {
                                    error.ErrorLevel3 = errorTokens[2];
                                }
                                if (errorTokens.Length >= 4)
                                {
                                    error.ErrorLevel4 = errorTokens[3];
                                }
                                if (errorTokens.Length >= 5)
                                {
                                    error.ErrorLevel5 = errorTokens[4];
                                }
                                error.ErrorDepth = errorTokens.Length;

                                // Check if last thing is a 3 digit number, then cast it and see what comes out
                                if (errorTokens[errorTokens.Length - 1].Length == 3)
                                {
                                    int httpCode = -1;
                                    if (Int32.TryParse(errorTokens[errorTokens.Length - 1], out httpCode) == true)
                                    {
                                        // Hmm, likely to be a HTTP code
                                        error.ErrorType = "HTTP";
                                        error.HttpCode = httpCode;
                                    }
                                }

                                updateEntityWithDeeplinks(error);
                                updateEntityWithEntityDetailAndFlameGraphLinks(error, jobTarget, jobConfiguration.Input.TimeRange);

                                errorList.Add(error);
                            }

                            // Sort them
                            errorList = errorList.OrderBy(o => o.TierName).ThenBy(o => o.ErrorName).ToList();

                            FileIOHelper.WriteListToCSVFile(errorList, new APMErrorReportMap(), FilePathMap.APMErrorsIndexFilePath(jobTarget));

                            stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + errorList.Count;
                        }

                        #endregion

                        #region Information Points

                        List<AppDRESTMetric> informationPointsRESTList = FileIOHelper.LoadListOfObjectsFromFile<AppDRESTMetric>(FilePathMap.APMInformationPointsDataFilePath(jobTarget));
                        List<APMInformationPoint> informationPointsList = null;
                        if (informationPointsRESTList != null)
                        {
                            loggerConsole.Info("Index List of Information points ({0} entities)", informationPointsRESTList.Count);

                            informationPointsList = new List<APMInformationPoint>(informationPointsRESTList.Count);

                            JObject informationPointsDetailsContainer = FileIOHelper.LoadJObjectFromFile(FilePathMap.APMInformationPointsDetailDataFilePath(jobTarget));
                            JArray informationPointsDetails = null;
                            if (isTokenPropertyNull(informationPointsDetailsContainer, "informationPointsListViewEntries") == false)
                            {
                                informationPointsDetails = (JArray)informationPointsDetailsContainer["informationPointsListViewEntries"];
                            }

                            foreach (AppDRESTMetric informationPointREST in informationPointsRESTList)
                            {
                                APMInformationPoint informationPoint = new APMInformationPoint();
                                informationPoint.ApplicationID = jobTarget.ApplicationID;
                                informationPoint.ApplicationName = jobTarget.Application;
                                informationPoint.Controller = jobTarget.Controller;

                                if (informationPointREST.metricName == "METRIC DATA NOT FOUND")
                                {
                                    informationPoint.IPID = -1;
                                }
                                else
                                {
                                    // metricName
                                    // BTM|IPs|IP:5|Calls per Minute
                                    //            ^
                                    //            ID

                                    informationPoint.IPID = Convert.ToInt32(informationPointREST.metricName.Split('|')[2].Split(':')[1]);
                                }

                                // metricPath
                                // Information Points|Delete Cart|Calls per Minute
                                //                    ^^^^^^^^^^^
                                //                    IP Name
                                informationPoint.IPName = informationPointREST.metricPath.Split('|')[1];

                                if (informationPoint.IPID != -1)
                                {
                                    JObject informationPointDetail = (JObject)informationPointsDetails.Where(e => (long)e["id"] == informationPoint.IPID).FirstOrDefault();
                                    if (informationPointDetail != null)
                                    {
                                        informationPoint.IPType = getStringValueFromJToken(informationPointDetail, "agentType");
                                    }
                                }
                                else
                                {
                                    JObject informationPointDetail = (JObject)informationPointsDetails.Where(e => (string)e["name"] == informationPoint.IPName).FirstOrDefault();
                                    if (informationPointDetail != null)
                                    {
                                        informationPoint.IPType = getStringValueFromJToken(informationPointDetail, "agentType");
                                        informationPoint.IPID = getLongValueFromJToken(informationPointDetail, "id");
                                    }
                                }

                                updateEntityWithDeeplinks(informationPoint);
                                updateEntityWithEntityDetailAndFlameGraphLinks(informationPoint, jobTarget, jobConfiguration.Input.TimeRange);

                                informationPointsList.Add(informationPoint);
                            }

                            // Sort them
                            informationPointsList = informationPointsList.OrderBy(o => o.IPName).ToList();

                            FileIOHelper.WriteListToCSVFile(informationPointsList, new APMInformationPointReportMap(), FilePathMap.APMInformationPointsIndexFilePath(jobTarget));

                            stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + informationPointsList.Count;
                        }

                        #endregion

                        #region Tiers

                        List<APMTier> tiersList = null;
                        List<APMResolvedBackend> resolvedBackendsList = null;
                        if (tiersRESTList != null)
                        {
                            loggerConsole.Info("Index List of Tiers ({0} entities)", tiersRESTList.Count);


                            tiersList = new List<APMTier>(tiersRESTList.Count);
                            resolvedBackendsList = new List<APMResolvedBackend>(tiersRESTList.Count * 10);

                            foreach (AppDRESTTier tierREST in tiersRESTList)
                            {
                                APMTier tier = new APMTier();
                                tier.AgentType = tierREST.agentType;
                                tier.ApplicationID = jobTarget.ApplicationID;
                                tier.ApplicationName = jobTarget.Application;
                                tier.Description = tierREST.description;
                                tier.Controller = jobTarget.Controller;
                                tier.TierID = tierREST.id;
                                tier.TierName = tierREST.name;
                                tier.TierType = tierREST.type;
                                tier.NumNodes = tierREST.numberOfNodes;
                                if (businessTransactionsRESTList != null)
                                {
                                    tier.NumBTs = businessTransactionsRESTList.Where<AppDRESTBusinessTransaction>(b => b.tierId == tier.TierID).Count();
                                }
                                if (serviceEndpointsList != null)
                                {
                                    tier.NumSEPs = serviceEndpointsList.Where<APMServiceEndpoint>(s => s.TierID == tier.TierID).Count();
                                }
                                if (errorList != null)
                                {
                                    tier.NumErrors = errorList.Where<APMError>(s => s.TierID == tier.TierID).Count();
                                }

                                updateEntityWithDeeplinks(tier);
                                updateEntityWithEntityDetailAndFlameGraphLinks(tier, jobTarget, jobConfiguration.Input.TimeRange);

                                tiersList.Add(tier);

                                JArray resolvedBackends = FileIOHelper.LoadJArrayFromFile(FilePathMap.APMBackendToTierMappingDataFilePath(jobTarget, tierREST));
                                if (resolvedBackends != null)
                                {
                                    foreach (JToken resolvedBackendToken in resolvedBackends)
                                    {
                                        APMResolvedBackend resolvedBackend = new APMResolvedBackend();

                                        resolvedBackend.Controller = jobTarget.Controller;
                                        resolvedBackend.ApplicationID = jobTarget.ApplicationID;
                                        resolvedBackend.ApplicationName = jobTarget.Application;
                                        resolvedBackend.TierName = tier.TierName;
                                        resolvedBackend.TierID = tier.TierID;
                                        resolvedBackend.BackendName = getStringValueFromJToken(resolvedBackendToken, "displayName");
                                        resolvedBackend.BackendID = getLongValueFromJToken(resolvedBackendToken, "id");
                                        try { resolvedBackend.BackendType = getStringValueFromJToken(resolvedBackendToken["resolutionInfo"], "exitPointType"); } catch { }

                                        resolvedBackend.CreatedOnUtc = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(resolvedBackendToken, "createdOn"));
                                        try { resolvedBackend.CreatedOn = resolvedBackend.CreatedOnUtc.ToLocalTime(); } catch { }

                                        if (nodesList != null)
                                        {
                                            APMNode node = nodesList.Where(n => n.TierID == tier.TierID && n.NodeID == getLongValueFromJToken(resolvedBackendToken, "applicationComponentNodeId")).FirstOrDefault();
                                            if (node != null)
                                            {
                                                resolvedBackend.NodeName = node.NodeName;
                                                resolvedBackend.NodeID = node.NodeID;
                                            }
                                        }

                                        if (isTokenPropertyNull(resolvedBackendToken, "resolutionInfo") == false &&
                                            isTokenPropertyNull(resolvedBackendToken["resolutionInfo"], "properties") == false)
                                        {
                                            JToken propertiesToken = resolvedBackendToken["resolutionInfo"]["properties"];
                                            resolvedBackend.NumProps = propertiesToken.Count();
                                            if (resolvedBackend.NumProps >= 1)
                                            {
                                                resolvedBackend.Prop1Name = propertiesToken[0]["name"].ToString();
                                                resolvedBackend.Prop1Value = propertiesToken[0]["value"].ToString();
                                            }
                                            if (resolvedBackend.NumProps >= 2)
                                            {
                                                resolvedBackend.Prop2Name = propertiesToken[1]["name"].ToString();
                                                resolvedBackend.Prop2Value = propertiesToken[1]["value"].ToString();
                                            }
                                            if (resolvedBackend.NumProps >= 3)
                                            {
                                                resolvedBackend.Prop3Name = propertiesToken[2]["name"].ToString();
                                                resolvedBackend.Prop3Value = propertiesToken[2]["value"].ToString();
                                            }
                                            if (resolvedBackend.NumProps >= 4)
                                            {
                                                resolvedBackend.Prop4Name = propertiesToken[3]["name"].ToString();
                                                resolvedBackend.Prop4Value = propertiesToken[3]["value"].ToString();
                                            }
                                            if (resolvedBackend.NumProps >= 5)
                                            {
                                                resolvedBackend.Prop5Name = propertiesToken[4]["name"].ToString();
                                                resolvedBackend.Prop5Value = propertiesToken[4]["value"].ToString();
                                            }
                                            if (resolvedBackend.NumProps >= 6)
                                            {
                                                resolvedBackend.Prop6Name = propertiesToken[5]["name"].ToString();
                                                resolvedBackend.Prop6Value = propertiesToken[5]["value"].ToString();
                                            }
                                            if (resolvedBackend.NumProps >= 7)
                                            {
                                                resolvedBackend.Prop7Name = propertiesToken[6]["name"].ToString();
                                                resolvedBackend.Prop7Value = propertiesToken[6]["value"].ToString();
                                            }
                                        }

                                        resolvedBackendsList.Add(resolvedBackend);
                                    }
                                }
                            }

                            // Sort them
                            tiersList = tiersList.OrderBy(o => o.TierName).ToList();
                            resolvedBackendsList = resolvedBackendsList.OrderBy(o => o.TierName).ThenBy(o => o.BackendType).ThenBy(o => o.BackendName).ToList();

                            FileIOHelper.WriteListToCSVFile(tiersList, new APMTierReportMap(), FilePathMap.APMTiersIndexFilePath(jobTarget));
                            FileIOHelper.WriteListToCSVFile(resolvedBackendsList, new APMResolvedBackendReportMap(), FilePathMap.APMMappedBackendsIndexFilePath(jobTarget));

                            stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + tiersList.Count;
                        }

                        #endregion

                        #region Application

                        JArray applicationsArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.APMApplicationDataFilePath(jobTarget));
                        if (applicationsArray != null && applicationsArray.Count > 0)
                        {
                            JObject applicationObject = (JObject)applicationsArray[0];

                            APMApplication application = new APMApplication();

                            application.Controller = jobTarget.Controller;
                            application.ApplicationName = getStringValueFromJToken(applicationObject, "name");
                            application.Description = getStringValueFromJToken(applicationObject, "description");
                            application.ApplicationID = getLongValueFromJToken(applicationObject, "id");

                            if (tiersList != null) application.NumTiers = tiersList.Count;
                            if (nodesList != null) application.NumNodes = nodesList.Count;
                            if (backendsList != null) application.NumBackends = backendsList.Count;
                            if (businessTransactionsList != null) application.NumBTs = businessTransactionsList.Count;
                            if (serviceEndpointsList != null) application.NumSEPs = serviceEndpointsList.Count;
                            if (errorList != null) application.NumErrors = errorList.Count;
                            if (informationPointsList != null) application.NumIPs = informationPointsList.Count;

                            updateEntityWithDeeplinks(application);
                            updateEntityWithEntityDetailAndFlameGraphLinks(application, jobTarget, jobConfiguration.Input.TimeRange);

                            List<APMApplication> applicationsList = new List<APMApplication>(1);
                            applicationsList.Add(application);

                            FileIOHelper.WriteListToCSVFile(applicationsList, new APMApplicationReportMap(), FilePathMap.APMApplicationsIndexFilePath(jobTarget));
                        }

                        #endregion

                        #region Combine All for Report CSV

                        // If it is the first one, clear out the combined folder
                        if (reportFolderCleaned == false)
                        {
                            FileIOHelper.DeleteFolder(FilePathMap.APMEntitiesReportFolderPath());
                            Thread.Sleep(1000);
                            FileIOHelper.CreateFolder(FilePathMap.APMEntitiesReportFolderPath());
                            reportFolderCleaned = true;
                        }

                        // Append all the individual report files into one
                        if (File.Exists(FilePathMap.APMApplicationsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMApplicationsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMApplicationsReportFilePath(), FilePathMap.APMApplicationsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.APMTiersIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMTiersIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMTiersReportFilePath(), FilePathMap.APMTiersIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.APMNodesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMNodesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMNodesReportFilePath(), FilePathMap.APMNodesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.APMNodeStartupOptionsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMNodeStartupOptionsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMNodeStartupOptionsReportFilePath(), FilePathMap.APMNodeStartupOptionsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.APMNodePropertiesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMNodePropertiesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMNodePropertiesReportFilePath(), FilePathMap.APMNodePropertiesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.APMNodeEnvironmentVariablesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMNodeEnvironmentVariablesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMNodeEnvironmentVariablesReportFilePath(), FilePathMap.APMNodeEnvironmentVariablesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.APMBackendsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMBackendsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMBackendsReportFilePath(), FilePathMap.APMBackendsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.APMMappedBackendsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMMappedBackendsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMMappedBackendsReportFilePath(), FilePathMap.APMMappedBackendsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.APMBusinessTransactionsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMBusinessTransactionsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMBusinessTransactionsReportFilePath(), FilePathMap.APMBusinessTransactionsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.APMServiceEndpointsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMServiceEndpointsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMServiceEndpointsReportFilePath(), FilePathMap.APMServiceEndpointsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.APMErrorsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMErrorsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMErrorsReportFilePath(), FilePathMap.APMErrorsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.APMInformationPointsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMInformationPointsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMInformationPointsReportFilePath(), FilePathMap.APMInformationPointsIndexFilePath(jobTarget));
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
            logger.Trace("Input.DetectedEntities={0}", jobConfiguration.Input.DetectedEntities);
            loggerConsole.Trace("Input.DetectedEntities={0}", jobConfiguration.Input.DetectedEntities);
            if (jobConfiguration.Input.DetectedEntities == false)
            {
                loggerConsole.Trace("Skipping index of detected entities");
            }
            return (jobConfiguration.Input.DetectedEntities == true);
        }

        private void updateEntityWithEntityDetailAndFlameGraphLinks(APMEntityBase entity, JobTarget jobTarget, JobTimeRange jobTimeRange)
        {
            entity.DetailLink = String.Format(@"=HYPERLINK(""{0}"", ""<Detail>"")", FilePathMap.EntityMetricAndDetailExcelReportFilePath(entity, jobTarget, jobTimeRange, false));
            entity.FlameGraphLink = String.Format(@"=HYPERLINK(""{0}"", ""<FlGraph>"")", FilePathMap.FlameGraphReportFilePath(entity, jobTarget, jobTimeRange, false));
            entity.FlameChartLink = String.Format(@"=HYPERLINK(""{0}"", ""<FlChart>"")", FilePathMap.FlameChartReportFilePath(entity, jobTarget, jobTimeRange, false));
            entity.MetricGraphLink = String.Format(@"=HYPERLINK(""{0}"", ""<Metrics>"")", FilePathMap.EntityTypeMetricGraphsExcelReportFilePath(entity, jobTarget, jobTimeRange, false));
        }
    }
}
