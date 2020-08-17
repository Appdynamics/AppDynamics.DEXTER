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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Compiler", "CS0168", Justification = "Hiding IndexOutOfRangeException that may occur when parsing an array delimited by |")]
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

                if (jobConfiguration.Target.Count(t => t.Type == APPLICATION_TYPE_APM) == 0)
                {
                    logger.Warn("No {0} targets to process", APPLICATION_TYPE_APM);
                    loggerConsole.Warn("No {0} targets to process", APPLICATION_TYPE_APM);

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
                        List<APMNodeProperty> entityNodesStartupOptionsList = null;
                        List<APMNodeProperty> entityNodesPropertiesList = null;
                        List<APMNodeProperty> entityNodesEnvironmentVariablesList = null;
                        if (nodesRESTList != null)
                        {
                            loggerConsole.Info("Index List of Nodes and Node Properties ({0} entities)", nodesRESTList.Count);

                            stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + nodesRESTList.Count;

                            nodesList = new List<APMNode>(nodesRESTList.Count);
                            entityNodesStartupOptionsList = new List<APMNodeProperty>(nodesRESTList.Count * 25);
                            entityNodesPropertiesList = new List<APMNodeProperty>(nodesRESTList.Count * 25);
                            entityNodesEnvironmentVariablesList = new List<APMNodeProperty>(nodesRESTList.Count * 25);

                            foreach (AppDRESTNode nodeREST in nodesRESTList)
                            {
                                List<APMNodeProperty> entityNodeStartupOptionsList = new List<APMNodeProperty>(25);
                                List<APMNodeProperty> entityNodePropertiesList = new List<APMNodeProperty>(25);
                                List<APMNodeProperty> entityNodeEnvironmentVariablesList = new List<APMNodeProperty>(25);

                                #region Parse node basics

                                APMNode node = new APMNode();
                                node.Controller = jobTarget.Controller;
                                node.ApplicationName = jobTarget.Application;
                                node.ApplicationID = jobTarget.ApplicationID;
                                node.AgentType = nodeREST.agentType;
                                node.TierName = nodeREST.tierName;
                                node.TierID = nodeREST.tierId;
                                node.NodeName = nodeREST.name;
                                node.NodeID = nodeREST.id;
                                node.MachineName = nodeREST.machineName;
                                node.MachineID = nodeREST.machineId;
                                node.AgentPresent = nodeREST.appAgentPresent;
                                node.AgentVersion = nodeREST.appAgentVersion;
                                node.MachineAgentPresent = nodeREST.machineAgentPresent;
                                node.MachineAgentVersion = nodeREST.machineAgentVersion;
                                node.MachineOSType = nodeREST.machineOSType;
                                node.MachineType = nodeREST.type;
                                if (node.AgentVersion != String.Empty)
                                {
                                    // Java agent looks like that
                                    // Server Agent v4.2.3.2 GA #12153 r13c5eb6a7acbfea4d6da465a3ae47412715e26fa 59-4.2.3.next-build
                                    // Server Agent v3.7.16.0 GA #2014-02-26_21-19-08 raf61d5f54753290c983f95173e74e6865f6ad123 130-3.7.16
                                    // Server Agent v4.2.7.1 GA #13005 rc04adaef4741dbb8f2e7c206bdb2a6614046798a 11-4.2.7.next-analytics
                                    // Server Agent v4.0.6.0 GA #2015-05-11_20-44-33 r7cb8945756a0779766bf1b4c32e49a96da7b8cfe 10-4.0.6.next
                                    // Server Agent v3.8.3.0 GA #2014-06-06_17-06-05 r34b2744775df248f79ffb2da2b4515b1f629aeb5 7-3.8.3.next
                                    // Server Agent v3.9.3.0 GA #2014-09-23_22-14-15 r05918cd8a4a8a63504a34f0f1c85511e207049b3 20-3.9.3.next
                                    // Server Agent v4.1.7.1 GA #9949 ra4a2721d52322207b626e8d4c88855c846741b3d 18-4.1.7.next-build
                                    // Server Agent v3.7.11.1 GA #2013-10-23_17-07-44 r41149afdb8ce39025051c25382b1cf77e2a7fed0 21
                                    // Server Agent v4.1.8.5 GA #10236 r8eca32e4695e8f6a5902d34a66bfc12da1e12241 45-4.1.8.next-controller
                                    // Server Agent v4.4.2 GA #4.4.2.22394 rnull null
                                    // Server Agent #20.4.0.29862 v20.4.0 GA compatible with 4.4.1.0 r23226cf913828e244d2d32691aac97efccf39724 release/20.4.0


                                    // Apache agent looks like this
                                    // Proxy v4.2.5.1 GA SHA-1:.ad6c804882f518b3350f422489866ea2008cd664 #13146 35-4.2.5.next-build

                                    // .NET agent looks like this
                                    // 20.4.1 compatible with 4.4.1.0
                                    // 4.5.1.0 compatible with 4.4.1.0
                                    // 4.5.13.0 compatible with 4.4.1.0
                                    // 4.5.14.0 compatible with 4.4.1.0

                                    // .NET Core agent looks like that 
                                    // 20.4.1 compatible with 4.4.1.0
                                    // 4.5.17.0 compatible with 4.4.1.0

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

                                #endregion

                                #region Parse extra properties

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
                                                nodePropertyRow.PropValue = String.Empty;

                                                string[] optionValueAdjustedTokens = optionValueAdjusted.Split(new char[] { '=', ':' });
                                                if (optionValueAdjustedTokens.Length > 0)
                                                {
                                                    nodePropertyRow.PropName = optionValueAdjustedTokens[0];
                                                }
                                                if (optionValueAdjustedTokens.Length == 2)
                                                {
                                                    nodePropertyRow.PropValue = optionValueAdjustedTokens[1];
                                                }
                                                else if (optionValueAdjustedTokens.Length > 2)
                                                {
                                                    nodePropertyRow.PropValue = optionValueAdjusted.Substring(optionValueAdjustedTokens[0].Length + 1);
                                                }

                                                // Adjust for + and - in front of the value
                                                // Those break Excel by making it think it is a formula
                                                // Prefixing it with space suffices
                                                if (nodePropertyRow.PropValue.StartsWith("+") == true || nodePropertyRow.PropValue.StartsWith("-") == true)
                                                {
                                                    nodePropertyRow.PropValue = String.Format(" {0}", nodePropertyRow.PropValue);
                                                }

                                                // Parse out well known properties for memory sizes
                                                if (nodePropertyRow.PropName.StartsWith("Xmx", StringComparison.InvariantCultureIgnoreCase) == true ||
                                                    nodePropertyRow.PropName.StartsWith("Xms", StringComparison.InvariantCultureIgnoreCase) == true ||
                                                    nodePropertyRow.PropName.StartsWith("Xmn", StringComparison.InvariantCultureIgnoreCase) == true ||
                                                    nodePropertyRow.PropName.StartsWith("Xss", StringComparison.InvariantCultureIgnoreCase) == true)
                                                {
                                                    nodePropertyRow.PropValue = nodePropertyRow.PropName.Substring(3);
                                                    nodePropertyRow.PropName = nodePropertyRow.PropName.Substring(0, 3);
                                                }

                                                // Parse out the XX: properties
                                                if (String.Compare(nodePropertyRow.PropName, "XX", true) == 0)
                                                { 
                                                    optionValueAdjustedTokens = nodePropertyRow.PropValue.Split('=');
                                                    if (optionValueAdjustedTokens.Length >= 2)
                                                    {
                                                        nodePropertyRow.PropName = String.Format("{0}:{1}", nodePropertyRow.PropName, optionValueAdjustedTokens[0]);
                                                        nodePropertyRow.PropValue = nodePropertyRow.PropValue.Substring(optionValueAdjustedTokens[0].Length + 1);
                                                    }
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

                                #endregion

                                #region Analyze properties for extra information

                                // Parse some versions and properties into Agent-specific lists
                                // Things like VM properties, Container types, Startup parameters, etc

                                #region Loop through each of 3 types of extra properties to populate well known properties to columns

                                // Parse important java VM properties 
                                foreach (APMNodeProperty nodeProp in entityNodePropertiesList)
                                {
                                    switch (nodeProp.PropName.ToLower())
                                    {
                                        case "java.class.path":
                                            node.ClassPath = nodeProp.PropValue;
                                            break;
                                        case "java.class.version":
                                            node.ClassVersion = nodeProp.PropValue;
                                            break;
                                        case "java.home":
                                            node.Home = nodeProp.PropValue;
                                            break;
                                        case "java.runtime.name":
                                            node.RuntimeName = nodeProp.PropValue;
                                            break;
                                        case "java.runtime.version":
                                            node.RuntimeVersion = nodeProp.PropValue;
                                            break;
                                        case "java.vendor":
                                            node.Vendor = nodeProp.PropValue;
                                            break;
                                        case "java.vendor.version":
                                            node.VendorVersion = nodeProp.PropValue;
                                            break;
                                        case "java.version":
                                            node.Version = nodeProp.PropValue;
                                            break;
                                        case "java.vm.info":
                                            node.VMInfo = nodeProp.PropValue;
                                            break;
                                        case "java.vm.name":
                                            node.VMName = nodeProp.PropValue;
                                            break;
                                        case "java.vm.vendor":
                                            node.VMVendor = nodeProp.PropValue;
                                            break;
                                        case "java.vm.version":
                                            node.VMVersion = nodeProp.PropValue;
                                            break;
                                        case "os.arch":
                                            node.OSArchitecture = nodeProp.PropValue;
                                            break;
                                        case "os.name":
                                            node.OSName = nodeProp.PropValue;
                                            break;
                                        case "os.version":
                                            node.OSVersion = nodeProp.PropValue;
                                            break;
                                        case "dotnet-os-ver":
                                            node.OSVersion = nodeProp.PropValue;
                                            if (node.OSVersion.Contains("windows") == true || node.OSVersion.Contains("Windows") == true)
                                            { 
                                                node.OSName = "Windows";
                                            }
                                            break;
                                        case "process-identity":
                                            node.UserName = nodeProp.PropValue;
                                            break;
                                        default:
                                            break;
                                    }
                                }

                                // Parse important java startup properties
                                // https://docs.oracle.com/javase/8/docs/technotes/tools/windows/java.html
                                foreach (APMNodeProperty nodeProp in entityNodeStartupOptionsList)
                                {
                                    double parsedValue = -1;
                                    switch (nodeProp.PropName.ToLower())
                                    {
                                        case "xmn":
                                        case "xx:newsize":
                                            //-Xmnsize
                                            //Sets the initial and maximum size (in bytes) of the heap for the young generation (nursery). Append the letter k or K to indicate kilobytes, m or M to indicate megabytes, g or G to indicate gigabytes.
                                            //The young generation region of the heap is used for new objects. GC is performed in this region more often than in other regions. If the size for the young generation is too small, then a lot of minor garbage collections will be performed. If the size is too large, then only full garbage collections will be performed, which can take a long time to complete. Oracle recommends that you keep the size for the young generation between a half and a quarter of the overall heap size.
                                            //The following examples show how to set the initial and maximum size of young generation to 256 MB using various units:
                                            //-Xmn256m
                                            //-Xmn262144k
                                            //-Xmn268435456
                                            //Instead of the -Xmn option to set both the initial and maximum size of the heap for the young generation, you can use -XX:NewSize to set the initial size and -XX:MaxNewSize to set the maximum size.
                                            //-XX:NewSize=size
                                            //Sets the initial size (in bytes) of the heap for the young generation (nursery). Append the letter k or K to indicate kilobytes, m or M to indicate megabytes, g or G to indicate gigabytes.
                                            //The young generation region of the heap is used for new objects. GC is performed in this region more often than in other regions. If the size for the young generation is too low, then a large number of minor GCs will be performed. If the size is too high, then only full GCs will be performed, which can take a long time to complete. Oracle recommends that you keep the size for the young generation between a half and a quarter of the overall heap size.
                                            //The following examples show how to set the initial size of young generation to 256 MB using various units:
                                            //-XX:NewSize=256m
                                            //-XX:NewSize=262144k
                                            //-XX:NewSize=268435456
                                            //The -XX:NewSize option is equivalent to -Xmn.
                                            parsedValue = convertValueSettingFromJavaStartupMemoryParameterToMB(nodeProp.PropValue);
                                            if (parsedValue >= 0) node.HeapYoungInitialSizeMB = parsedValue;
                                            break;
                                        case "xx:maxnewsize":
                                            //-XX:MaxNewSize=size
                                            //Sets the maximum size (in bytes) of the heap for the young generation (nursery). The default value is set ergonomically.
                                            parsedValue = convertValueSettingFromJavaStartupMemoryParameterToMB(nodeProp.PropValue);
                                            if (parsedValue >= 0) node.HeapYoungMaxSizeMB = parsedValue;
                                            break;
                                        case "xms":
                                            //-Xmssize
                                            //Sets the initial size(in bytes) of the heap.This value must be a multiple of 1024 and greater than 1 MB.Append the letter k or K to indicate kilobytes, m or M to indicate megabytes, g or G to indicate gigabytes.
                                            //The following examples show how to set the size of allocated memory to 6 MB using various units:
                                            //-Xms6291456
                                            //-Xms6144k
                                            //-Xms6m
                                            //If you do not set this option, then the initial size will be set as the sum of the sizes allocated for the old generation and the young generation.The initial size of the heap for the young generation can be set using the -Xmn option or the -XX:NewSize option.
                                            parsedValue = convertValueSettingFromJavaStartupMemoryParameterToMB(nodeProp.PropValue);
                                            if (parsedValue >= 0) node.HeapInitialSizeMB = parsedValue;
                                            break;
                                        case "xmx":
                                        case "xx:maxheapsize":
                                            //-Xmxsize
                                            //Specifies the maximum size(in bytes) of the memory allocation pool in bytes.This value must be a multiple of 1024 and greater than 2 MB.Append the letter k or K to indicate kilobytes, m or M to indicate megabytes, g or G to indicate gigabytes.The default value is chosen at runtime based on system configuration.For server deployments, -Xms and - Xmx are often set to the same value.See the section "Ergonomics" in Java SE HotSpot Virtual Machine Garbage Collection Tuning Guide at http://docs.oracle.com/javase/8/docs/technotes/guides/vm/gctuning/index.html.
                                            //The following examples show how to set the maximum allowed size of allocated memory to 80 MB using various units:
                                            //-Xmx83886080
                                            //-Xmx81920k
                                            //-Xmx80m
                                            //The -Xmx option is equivalent to -XX:MaxHeapSize.
                                            parsedValue = convertValueSettingFromJavaStartupMemoryParameterToMB(nodeProp.PropValue);
                                            if (parsedValue >= 0) node.HeapMaxSizeMB = parsedValue;
                                            break;
                                        default:
                                            break;
                                    }
                                }

                                // Parse important environment properties
                                foreach (APMNodeProperty nodeProp in entityNodeEnvironmentVariablesList)
                                {
                                    switch (nodeProp.PropName.ToLower())
                                    {
                                        case "computername":
                                        case "hostname":
                                        case "host_name":
                                            node.OSComputerName = nodeProp.PropValue;
                                            break;

                                        case "number_of_processors":
                                            int numOfProcessors;
                                            if (Int32.TryParse(nodeProp.PropValue, out numOfProcessors) == true)
                                            {
                                                node.OSNumberOfProcs = numOfProcessors;
                                            }
                                            break;

                                        case "os":
                                            if (node.OSName == null || node.OSName.Length == 0 ) node.OSName = nodeProp.PropValue;
                                            break;
                                        case "processor_architecture":
                                            node.OSArchitecture = nodeProp.PropValue;
                                            break;

                                        case "processor_identifier":
                                            node.OSProcessorType = nodeProp.PropValue;
                                            break;

                                        case "processor_revision":
                                            node.OSProcessorRevision = nodeProp.PropValue;
                                            break;

                                        case "user":
                                        case "username":
                                            if (node.UserName == null || node.UserName.Length == 0) node.UserName = nodeProp.PropValue;
                                            break;

                                        case "domain":
                                        case "userdomain":
                                            node.Domain = nodeProp.PropValue;
                                            break;

                                        default:
                                            break;
                                    }
                                }

                                #endregion

                                #region Container Orchestration Runtime

                                // Pivotal Cloud Foundry
                                if (entityNodeEnvironmentVariablesList.Count(e => e.PropName.StartsWith("CF_INSTANCE_", StringComparison.InvariantCultureIgnoreCase) == true) > 0)
                                {
                                    // Pivotal Cloud Foundry evidence:
                                    //PropName PropValue
                                    //CF_INSTANCE_ADDR    10.60.5.5:60538
                                    //CF_INSTANCE_GUID    13b0a99c - 4eea - 4800 - 7720 - fc9b
                                    //CF_INSTANCE_INDEX   2
                                    //CF_INSTANCE_INTERNAL_IP 10.254.0.70
                                    //CF_INSTANCE_IP  10.60.5.5
                                    //CF_INSTANCE_PORT    60538
                                    //CF_INSTANCE_PORTS[{ "external":60538,"internal":8080}]

                                    node.ContainerRuntimeType = "Pivotal Cloud Foundry (PCF)";
                                }
                                // Kubernetes
                                else if (entityNodeEnvironmentVariablesList.Count(e => e.PropName.StartsWith("KUBERNETES_", StringComparison.InvariantCultureIgnoreCase) == true) > 0)
                                {
                                    // Kubernetes evidence:
                                    //PropName	PropValue
                                    //KUBERNETES_PORT	tcp://10.76.0.1:443
                                    //KUBERNETES_PORT_443_TCP	tcp://10.76.0.1:443
                                    //KUBERNETES_PORT_443_TCP_ADDR	10.76.0.1
                                    //KUBERNETES_PORT_443_TCP_PORT	443
                                    //KUBERNETES_PORT_443_TCP_PROTO	tcp
                                    //KUBERNETES_PORT_53_TCP	tcp://10.76.0.1:53
                                    //KUBERNETES_PORT_53_TCP_ADDR	10.76.0.1
                                    //KUBERNETES_PORT_53_TCP_PORT	53
                                    //KUBERNETES_PORT_53_TCP_PROTO	tcp
                                    //KUBERNETES_PORT_53_UDP	udp://10.76.0.1:53
                                    //KUBERNETES_PORT_53_UDP_ADDR	10.76.0.1
                                    //KUBERNETES_PORT_53_UDP_PORT	53
                                    //KUBERNETES_PORT_53_UDP_PROTO	udp

                                    node.ContainerRuntimeType = "Kubernetes (K8S)";
                                }
                                // Openshift
                                else if (entityNodeEnvironmentVariablesList.Count(e => e.PropName == "OPENSHIFT_OH_WHAT_ARE_YOU") == 1)
                                {
                                    // OpenShift evidence:
                                    // Per SME, there isn't an easy way to get openshift evidence!

                                    node.ContainerRuntimeType = "OpenShift (OSCP)";
                                }

                                #endregion

                                #region Cloud Host

                                // AWS
                                APMNodeProperty nodePropAWS_Region = entityNodeEnvironmentVariablesList.Where(e => e.PropName.Equals("AWS_REGION", StringComparison.InvariantCultureIgnoreCase) || e.PropName.Equals("AWS_REGION_NAME", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                                if (nodePropAWS_Region != null)
                                {
                                    // AWS Evidence
                                    //AWS_REGION	us-gov-west-1
                                    //AWS_REGION_NAME	us-west-2
                                    node.CloudHostType = "AWS";
                                    node.CloudRegion = nodePropAWS_Region.PropValue;
                                }

                                // Azure
                                if (entityNodeEnvironmentVariablesList.Count(e => e.PropName.StartsWith("APPSETTING_", StringComparison.InvariantCultureIgnoreCase) == true) > 0 ||
                                    entityNodeEnvironmentVariablesList.Count(e => e.PropName.StartsWith("AZURE_", StringComparison.InvariantCultureIgnoreCase) == true) > 0)
                                {
                                    // Azure Evidence
                                    //APPSETTING_AZURE_CLIENT_ID
                                    //3eb5ee34-818f-484e-8d4b-1abced4846b0
                                    //46b1a514-3d1b-4e67-9eb7-095b7cd6a66c
                                    //APPSETTING_AZURE_CLIENT_SECRET
                                    //Xlkjasdlkjasdlkasjdalskjda
                                    //aslkdjalkdjaslkdsalkdasj
                                    //APPSETTING_AZURE_TENANT_ID
                                    //46080c3d-3ac0-45d5-bdb0-f66e64e4bef9
                                    //APPSETTING_AzureWebJobs.HttpGetServiceRequestTrigger.Disabled
                                    //APPSETTING_AzureWebJobs.HttpGetServicesRequestTrigger.Disabled
                                    //APPSETTING_AzureWebJobs.HttpPingTrigger.Disabled
                                    //APPSETTING_AzureWebJobs.HttpPostServicesRequestTrigger.Disabled
                                    //APPSETTING_AzureWebJobs.ServiceRequestQueueTrigger.Disabled
                                    //APPSETTING_AzureWebJobsDashboard
                                    //APPSETTING_AzureWebJobsStorage
                                    //AZURE_JETTY9_CMDLINE
                                    //AZURE_JETTY9_HOME
                                    //AZURE_JETTY93_CMDLINE
                                    //AZURE_JETTY93_HOME
                                    //AZURE_TOMCAT7_CMDLINE
                                    //AZURE_TOMCAT7_HOME
                                    //AZURE_TOMCAT8_CMDLINE
                                    //AZURE_TOMCAT8_HOME
                                    //AZURE_TOMCAT85_CMDLINE
                                    //AZURE_TOMCAT85_HOME
                                    //AZURE_TOMCAT90_CMDLINE
                                    //AZURE_TOMCAT90_HOME

                                    node.CloudHostType = "Azure";
                                }
                                APMNodeProperty nodePropAzure_Region = entityNodeEnvironmentVariablesList.Where(e => e.PropName.Equals("REGION_NAME", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                                if (nodePropAzure_Region != null)
                                {
                                    // Azure Evidence
                                    //REGION_NAME	Central US
                                    node.CloudHostType = "Azure";
                                    node.CloudRegion = nodePropAzure_Region.PropValue;
                                }

                                if (node.OSComputerName != null && (node.CloudHostType == null || node.CloudHostType.Length == 0))
                                {
                                    // Azure Evidence
                                    //RD00155DA01778
                                    // Red Dog is the codename of Azure from back in a day
                                    if (node.OSComputerName.StartsWith("RD00", StringComparison.InvariantCultureIgnoreCase) == true)
                                    { 
                                        node.CloudHostType = "Azure";
                                    }
                                }

                                // GCP
                                if (entityNodeEnvironmentVariablesList.Count(e => e.PropName.StartsWith("GCLOUD_", StringComparison.InvariantCultureIgnoreCase) == true) >0 ||
                                    entityNodeEnvironmentVariablesList.Count(e => e.PropName.StartsWith("GCP_", StringComparison.InvariantCultureIgnoreCase) == true) > 0)
                                {
                                    node.CloudHostType = "GCP";
                                }
                                // Can't use Contains() since it complains like that 
                                // https://stackoverflow.com/questions/41415458/why-does-dynamic-tostring-return-something-between-a-string-and-not-a-string/41415574#41415574
                                // will use indexof
                                else if (entityNodeEnvironmentVariablesList.Count(e => e.PropName.IndexOf("GKE_", StringComparison.InvariantCultureIgnoreCase) >= 0) > 0)
                                {
                                    node.CloudHostType = "GCP";
                                }

                                APMNodeProperty nodePropGCP_Region = entityNodeEnvironmentVariablesList.Where(e => e.PropName.Equals("FUNCTION_REGION", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                                if (nodePropGCP_Region != null)
                                {
                                    // GCP Evidence
                                    //???
                                    node.CloudHostType = "GCP";
                                    node.CloudRegion = nodePropGCP_Region.PropValue;
                                }

                                #endregion

                                #region Container Type

                                switch (node.AgentType)
                                {
                                    case "APP_AGENT":
                                        // Determine Tomcat, JBoss, IBM Websphere, Oracle WebLogic, IBM Sterling
                                        // Long tail: Oracle Glassfish, Jetty, SAP NetWeaver 

                                        if (entityNodeEnvironmentVariablesList.Count(e => e.PropName.StartsWith("CATALINA_", StringComparison.InvariantCultureIgnoreCase) == true) > 0)
                                        {
                                            // Tomcat evidence:
                                            //CATALINA_BASE
                                            //CATALINA_HOME
                                            //CATALINA_OPTS
                                            //CATALINA_PID
                                            node.WebHostContainerType = "Tomcat Catalina";
                                        }
                                        else if (entityNodePropertiesList.Count(e => e.PropName.StartsWith("catalina.", StringComparison.InvariantCultureIgnoreCase) == true) > 0)
                                        {
                                            // Tomcat evidence:
                                            //catalina.base
                                            //catalina.home
                                            node.WebHostContainerType = "Tomcat Catalina";
                                        }

                                        if (node.WebHostContainerType != null && node.WebHostContainerType.Length > 0) break;

                                        APMNodeProperty nodePropJBossHome = entityNodeEnvironmentVariablesList.Where(e => e.PropName.Equals("JBOSS_HOME", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                                        if (nodePropJBossHome != null)
                                        {
                                            // JBoss evidence:
                                            //JBOSS_HOME
                                            // Wildfly/EAP evidence - contents of the home path. Not precise but good enough
                                            if (nodePropJBossHome.PropValue.IndexOf("wildfly", StringComparison.InvariantCultureIgnoreCase) >= 0)
                                            {
                                                node.WebHostContainerType = "JBoss Wildfly";
                                            }
                                            else if (nodePropJBossHome.PropValue.IndexOf("eap", StringComparison.InvariantCultureIgnoreCase) >= 0)
                                            {
                                                node.WebHostContainerType = "JBoss EAP";
                                            }
                                            else
                                            {
                                                node.WebHostContainerType = "JBoss";
                                            }
                                        }

                                        if (node.WebHostContainerType != null && node.WebHostContainerType.Length > 0) break;

                                        APMNodeProperty nodePropWASHome = entityNodeEnvironmentVariablesList.Where(e => e.PropName.Equals("WAS_HOME", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                                        if (nodePropWASHome != null)
                                        {
                                            // WebSphere Application Server:
                                            //WAS_HOME
                                            //was.install.root
                                            if (nodePropWASHome.PropValue.IndexOf("websphere", StringComparison.InvariantCultureIgnoreCase) >= 0)
                                            {
                                                node.WebHostContainerType = "IBM WebSphere";
                                            }
                                            else
                                            {
                                                if (entityNodePropertiesList.Count(e => e.PropName.Equals("was.install.root", StringComparison.InvariantCultureIgnoreCase)) > 0)
                                                {
                                                    node.WebHostContainerType = "IBM WebSphere";
                                                }
                                                else
                                                {
                                                    node.WebHostContainerType = "IBM WebSphere ?";
                                                }
                                            }
                                        }

                                        if (node.WebHostContainerType != null && node.WebHostContainerType.Length > 0) break;

                                        if (entityNodeEnvironmentVariablesList.Count(e => e.PropName.Equals("WL_HOME", StringComparison.InvariantCultureIgnoreCase) == true) > 0)
                                        {
                                            // Weblogic
                                            //WL_HOME
                                            node.WebHostContainerType = "Oracle WebLogic";
                                        }
                                        else if (entityNodePropertiesList.Count(e => e.PropName.StartsWith("weblogic.", StringComparison.InvariantCultureIgnoreCase) == true) > 0)
                                        {
                                            // Weblogic
                                            //weblogic.classloader.preprocessor,weblogic.diagnostics.instrumentation.DiagnosticClassPreProcessor,569,4909,2101477
                                            //weblogic.management.server,http://10.102.121.238:8099,569,4909,2101477
                                            //weblogic.Name,ABCDEFTG,569,4909,2101477
                                            //weblogic.nodemanager.ServiceEnabled,true,569,4909,2101477
                                            //weblogic.ReverseDNSAllowed,false,569,4909,2101477
                                            //weblogic.security.SSL.ignoreHostnameVerification,false,569,4909,2101477
                                            //weblogic.system.BootIdentityFile,/apps/ouapps/managed_server/prd/weblogic/....
                                            node.WebHostContainerType = "Oracle WebLogic";
                                        }

                                        if (node.WebHostContainerType != null && node.WebHostContainerType.Length > 0) break;

                                        APMNodeProperty nodePropIBMJavaHome = entityNodeEnvironmentVariablesList.Where(e => e.PropName.Equals("IBM_JAVA_COMMAND_LINE", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                                        if (nodePropIBMJavaHome != null)
                                        {
                                            // IBM Sterling Commerce 
                                            //IBM_JAVA_COMMAND_LINE	/opt/ibm/mft/si/jdk/bin/java -d64 -javaagent:/opt/AppD/AppServerAgent-ibm-4.5.11.26665/javaagent.jar -Dappdynamics.socket.collection.bci.enable=true -Dappdynamics.http.proxyHost=forcepoint.corp.internal.citizensbank.com -Dappdynamics.http.proxyPort=80 -Dappdynamics.agent.uniqueHostId=lsfgrib00001001 -Dappdynamics.agent.applicationName=IBM-MFT-PROD -Dappdynamics.agent.tierName=B2BI_ActiveMQ_EPOC -Dappdynamics.agent.nodeName=ActiveMQ_P_lsfgrib00001001 -Djava.io.tmpdir=/opt/ibm/mft/si/tmp -XX:CompileCommandFile=.hotspot_compiler -d64 -Doracle.jdbc.maxCachedBufferSize=21 -Doracle.jdbc.maxCachedBufferSize=21 -Dssh.maxWindowSpace=4194304 -Dcom.certicom.tls.record.maximumPaddingLength=0 -Dsun.rmi.dgc.server.gcInterval=900000 -Dsun.rmi.dgc.client.gcInterval=900000 -Dmaverick.enableBCProvider=false -Xms2048m -Xmx2048m -Dvendor=shell -DvendorFile=/opt/ibm/mft/si/properties/servers.properties -Dactivemq.base=/opt/ibm/mft/si/activemq -Djava.security.auth.login.config=/opt/ibm/mft/si/activemq/conf/login.conf -classpath /opt/ibm/mft/si/jar/bootstrapper.jar com.sterlingcommerce.woodstock.noapp.NoAppLoader -f /opt/ibm/mft/si/properties/ACTIVEMQDynamicclasspath.cfg -class com.sterlingcommerce.jms.activemq.SCIBrokerFactory -invokeargs /opt/ibm/mft/si/activemq/conf/activemqconfig.xml activemq.txt
                                            if (nodePropIBMJavaHome.PropValue.IndexOf("sterling", StringComparison.InvariantCultureIgnoreCase) >= 0)
                                            {
                                                node.WebHostContainerType = "IBM Sterling";
                                            }
                                        }

                                        break;

                                    case "DOT_NET_APP_AGENT":
                                        // Parse .NET Version
                                        if (node.AgentRuntime != null && node.AgentRuntime.Length > 0)
                                        {
                                            //clr.version=.NET Core 2.0.9|agent.version.assemblyversion=1.0.0.0|agent.version.fileversion=20.4.1-1
                                            //clr.version=.NET Core 2.2.8|agent.version.assemblyversion=1.0.0.0|agent.version.fileversion=20.3.1-1
                                            //clr.version=.NET Core 2.2.8|agent.version.assemblyversion=1.0.0.0|agent.version.fileversion=20.4.1-1
                                            //clr.version=.NET Core 3.0.3|agent.version.assemblyversion=1.0.0.0|agent.version.fileversion=20.4.1-1
                                            //clr.version=.NET Core 3.0.3|agent.version.assemblyversion=1.0.0.0|agent.version.fileversion=20.5.0-0
                                            //clr.version=.NET Core 3.1.3|agent.version.assemblyversion=1.0.0.0|agent.version.fileversion=20.4.1-1
                                            //clr.version=.NET Core 3.1.4|agent.version.assemblyversion=1.0.0.0|agent.version.fileversion=20.4.1-1
                                            //clr.version=.NET Core 3.1.4|agent.version.assemblyversion=1.0.0.0|agent.version.fileversion=20.5.0-0
                                            //clr.version=.NET Framework v4.0.30319|agent.version.assemblyversion=1.0.0.0|agent.version.fileversion=20.3.1-1||clr.releasekey=461814
                                            //clr.version=.NET Framework v4.0.30319|agent.version.assemblyversion=1.0.0.0|agent.version.fileversion=20.4.1-1||clr.releasekey=461814
                                            //clr.version=.NET Framework v4.0.30319|agent.version.assemblyversion=1.0.0.0|agent.version.fileversion=20.5.0-0||clr.releasekey=461814
                                            //clr.version=3.0.3|agent.version.assemblyversion=1.0.0.0|agent.version.fileversion=4.5.18.1|clr.releasekey=461814
                                            //clr.version=4.0.30319.42000|agent.version.assemblyversion=1.0.0.0|agent.version.fileversion=4.5.14.0|clr.releasekey=461814
                                            //clr.version=.NET Framework v4.0.30319|agent.version.assemblyversion=1.0.0.0|agent.version.fileversion=20.5.0-0||clr.releasekey=461814
                                            //clr.version=2.0.50727.8810|agent.version.assemblyversion=1.0.0.0|agent.version.fileversion=4.4.3.0|clr.releasekey=394271
                                            //clr.version=2.0.50727.8813|agent.version.assemblyversion=1.0.0.0|agent.version.fileversion=4.4.3.0|clr.releasekey=394271
                                            //clr.version=2.0.50727.8813|agent.version.assemblyversion=1.0.0.0|agent.version.fileversion=4.4.3.0|clr.releasekey=461814
                                            //clr.version=2.0.50727.8813|agent.version.assemblyversion=1.0.0.0|agent.version.fileversion=4.5.1.0|clr.releasekey=379893
                                            //clr.version=4.0.30319.36627|agent.version.assemblyversion=1.0.0.0|agent.version.fileversion=4.5.1.0|clr.releasekey=379893
                                            //clr.version=4.0.30319.42000|agent.version.assemblyversion=1.0.0.0|agent.version.fileversion=4.4.3.0|clr.releasekey=394271

                                            node.RuntimeName = "Common Language Runtime";
                                            node.Vendor = "Microsoft";
                                            node.VMVendor = "Microsoft";
                                            node.Version = "";

                                            string[] agentRuntimeTokens = node.AgentRuntime.Split('|');
                                            foreach (string agentRuntimeToken in agentRuntimeTokens)
                                            {
                                                if (agentRuntimeToken.StartsWith("clr.version", StringComparison.InvariantCultureIgnoreCase) == true)
                                                {
                                                    string[] versionTokens = agentRuntimeToken.Split('=');
                                                    if (versionTokens.Length > 1)
                                                    {
                                                        string potentialVersion = versionTokens[1];
                                                        if (potentialVersion.StartsWith(".NET Core", StringComparison.InvariantCultureIgnoreCase) == true)
                                                        {
                                                            node.VMVersion = "5.0";
                                                            node.VMName = ".NET Core";
                                                        }
                                                        else if (potentialVersion.StartsWith(".NET Framework", StringComparison.InvariantCultureIgnoreCase) == true)
                                                        {
                                                            node.VMName = ".NET Full";
                                                        }

                                                        // Parse version
                                                        Regex regexVersion = new Regex(@"(?i)(\d*\.\d*\.\d*(\.\d*)?).*", RegexOptions.IgnoreCase);
                                                        Match match = regexVersion.Match(potentialVersion);
                                                        if (match != null)
                                                        {
                                                            if (match.Groups.Count > 1)
                                                            {
                                                                node.Version = match.Groups[1].Value;
                                                            }
                                                        }

                                                        // Decide what kind of runtime is it
                                                        // https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/versions-and-dependencies
                                                        // https://en.wikipedia.org/wiki/.NET_Framework_version_history
                                                        // https://jonathanparker.wordpress.com/2014/12/05/list-of-net-framework-versions/ 
                                                        if (node.Version.StartsWith("1.0.3705") == true)
                                                        {
                                                            node.VMVersion = "1.0";
                                                            node.RuntimeVersion = "1.0";
                                                            if (node.VMName == null || node.VMName.Length == 0) node.VMName = ".NET Full";
                                                        }
                                                        else if (node.Version.StartsWith("1.1.4322") == true)
                                                        {
                                                            node.VMVersion = "1.1";
                                                            node.RuntimeVersion = "1.1";
                                                            if (node.VMName == null || node.VMName.Length == 0) node.VMName = ".NET Full";
                                                        }
                                                        else if (node.Version.StartsWith("2.0.50727") == true)
                                                        {
                                                            node.VMVersion = "2.0";
                                                            node.RuntimeVersion = "2.0";
                                                            if (node.VMName == null || node.VMName.Length == 0) node.VMName = ".NET Full";
                                                        }
                                                        else if (node.Version.StartsWith("3.0.4506") == true)
                                                        {
                                                            node.VMVersion = "2.0";
                                                            node.RuntimeVersion = "3.0";
                                                            if (node.VMName == null || node.VMName.Length == 0) node.VMName = ".NET Full";
                                                        }
                                                        else if (
                                                            node.Version.StartsWith("3.5.21022") == true ||
                                                            node.Version.StartsWith("3.5.30428") == true ||
                                                            node.Version.StartsWith("3.5.30729") == true)
                                                        {
                                                            node.VMVersion = "2.0";
                                                            node.RuntimeVersion = "3.5";
                                                            if (node.VMName == null || node.VMName.Length == 0) node.VMName = ".NET Full";
                                                        }
                                                        else if (
                                                            node.Version.StartsWith("4.0.30319") == true ||
                                                            node.Version.StartsWith("4.5.50709") == true)
                                                        {
                                                            node.VMVersion = "4.0";
                                                            node.RuntimeVersion = "4.0";
                                                            if (node.VMName == null || node.VMName.Length == 0) node.VMName = ".NET Full";
                                                        }
                                                        else if (
                                                            node.Version.StartsWith("4.6") == true ||
                                                            node.Version.StartsWith("4.7") == true ||
                                                            node.Version.StartsWith("4.8") == true)
                                                        {
                                                            node.VMVersion = "4.0";
                                                            node.RuntimeVersion = "4.0";
                                                            if (node.VMName == null || node.VMName.Length == 0) node.VMName = ".NET Full";
                                                        }
                                                        else if (
                                                            node.Version.StartsWith("2.") == true ||
                                                            node.Version.StartsWith("3.") == true)
                                                        {
                                                            node.VMVersion = "5.0";
                                                            node.RuntimeVersion = "5.0";
                                                            if (node.VMName == null || node.VMName.Length == 0) node.VMName = ".NET Core";
                                                        }

                                                        // Assume .NET Core if we haven't caught the VM name
                                                        if (node.VMName == null || node.VMName.Length == 0) node.VMName = ".NET Core";
                                                    }
                                                }
                                            }

                                            //.NET Core 3.1+ (v4.0.30319)
                                            //.NET Core 2.2 (v4.0.30319)
                                            if (node.Version == null || node.Version.Length == 0)
                                            {
                                                if (node.AgentRuntime.StartsWith(".NET Core", StringComparison.InvariantCultureIgnoreCase) == true)
                                                {
                                                    node.VMVersion = "5.0";
                                                    node.VMName = ".NET Core";

                                                    // Parse version
                                                    Regex regexVersion = new Regex(@"(?i)(\d*\.\d*\.\d*(\.\d*)?).*", RegexOptions.IgnoreCase);
                                                    Match match = regexVersion.Match(node.AgentRuntime);
                                                    if (match != null)
                                                    {
                                                        if (match.Groups.Count > 1)
                                                        {
                                                            node.Version = match.Groups[1].Value;
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                        if (entityNodePropertiesList.Count(e => e.PropName.StartsWith("iis-", StringComparison.InvariantCultureIgnoreCase) == true) > 0)
                                        {
                                            // IIS Evidence
                                            //iis-application-id
                                            //iis-physical-path
                                            //iis-site-name
                                            //iis-version
                                            //iis-virtual-path
                                            if (node.CloudHostType == "Azure")
                                            {
                                                node.WebHostContainerType = "Azure App Service (IIS)";
                                            }
                                            else
                                            {
                                                node.WebHostContainerType = "Internet Information Services (IIS)";
                                            }
                                        }

                                        if (node.WebHostContainerType != null && node.WebHostContainerType.Length > 0) break;

                                        APMNodeProperty nodePropAppDomain = entityNodePropertiesList.Where(e => e.PropName.Equals("appdomain", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                                        if (nodePropAppDomain != null)
                                        {
                                            if (nodePropAppDomain.PropValue.EndsWith(".exe", StringComparison.InvariantCultureIgnoreCase) == true)
                                            {
                                                if (node.CloudHostType == "Azure")
                                                {
                                                    if (entityNodeEnvironmentVariablesList.Count(e => e.PropName.StartsWith("WEBJOBS_", StringComparison.InvariantCultureIgnoreCase) == true) > 0)
                                                    {
                                                        node.WebHostContainerType = "Azure WebJob Executable";
                                                    }
                                                    else
                                                    { 
                                                        node.WebHostContainerType = "Standalone Executable in Azure";
                                                    }
                                                }
                                                else
                                                { 
                                                    node.WebHostContainerType = "Standalone Executable";
                                                }
                                            }
                                        }

                                        if (node.WebHostContainerType != null && node.WebHostContainerType.Length > 0) break;

                                        if (entityNodeEnvironmentVariablesList.Count(e => e.PropName.StartsWith("Fabric", StringComparison.InvariantCultureIgnoreCase) == true) > 0)
                                        {
                                            // Service Fabric evidence
                                            //PropName	PropValue
                                            //FabricBinRoot	C:\Program Files\Windows Fabric\bin
                                            //FabricCodePath	C:\Program Files\Windows Fabric\bin\Fabric\Fabric.Code.1.0
                                            //FabricDataRoot	C:\ProgramData\Windows Fabric\
                                            //FabricRoot	C:\Program Files\Windows Fabric\
                                            node.WebHostContainerType = "Service Fabric";
                                        }

                                        break;

                                    default:
                                        break;
                                }

                                #endregion

                                #endregion

                                nodesList.Add(node);
                                entityNodesStartupOptionsList.AddRange(entityNodeStartupOptionsList);
                                entityNodesPropertiesList.AddRange(entityNodePropertiesList);
                                entityNodesEnvironmentVariablesList.AddRange(entityNodeEnvironmentVariablesList);
                            }

                            // Sort them
                            nodesList = nodesList.OrderBy(o => o.TierName).ThenBy(o => o.NodeName).ToList();
                            entityNodesStartupOptionsList = entityNodesStartupOptionsList.OrderBy(o => o.TierName).ThenBy(o => o.NodeName).ThenBy(o => o.PropName).ToList();
                            entityNodesPropertiesList = entityNodesPropertiesList.OrderBy(o => o.TierName).ThenBy(o => o.NodeName).ThenBy(o => o.PropName).ToList();
                            entityNodesEnvironmentVariablesList = entityNodesEnvironmentVariablesList.OrderBy(o => o.TierName).ThenBy(o => o.NodeName).ThenBy(o => o.PropName).ToList();

                            FileIOHelper.WriteListToCSVFile(nodesList, new APMNodeReportMap(), FilePathMap.APMNodesIndexFilePath(jobTarget));
                            FileIOHelper.WriteListToCSVFile(entityNodesStartupOptionsList, new APMNodePropertyReportMap(), FilePathMap.APMNodeStartupOptionsIndexFilePath(jobTarget));
                            FileIOHelper.WriteListToCSVFile(entityNodesPropertiesList, new APMNodePropertyReportMap(), FilePathMap.APMNodePropertiesIndexFilePath(jobTarget));
                            FileIOHelper.WriteListToCSVFile(entityNodesEnvironmentVariablesList, new APMNodePropertyReportMap(), FilePathMap.APMNodeEnvironmentVariablesIndexFilePath(jobTarget));
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
                                        }
                                        catch { }

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
                                            }
                                            catch { }
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
                                if (error.ErrorName.IndexOf("exception", 0, StringComparison.InvariantCultureIgnoreCase) >= 0)
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

                        #region Overflow Business Transactions

                        List<APMBusinessTransaction> unregisteredBusinessTransactionsList = null;
                        Dictionary<string, APMBusinessTransaction> unregisteredBusinessTransactionsDictionary = null;
                        if (tiersRESTList != null && tiersList != null)
                        {
                            loggerConsole.Info("Index List of Unregistered Business Transactions in Tiers ({0} entities)", tiersRESTList.Count);

                            unregisteredBusinessTransactionsList = new List<APMBusinessTransaction>(tiersRESTList.Count * 50);
                            unregisteredBusinessTransactionsDictionary = new Dictionary<string, APMBusinessTransaction>(tiersRESTList.Count * 50);

                            foreach (AppDRESTTier tierREST in tiersRESTList)
                            {
                                APMTier tier = tiersList.Where(t => t.TierID == tierREST.id).FirstOrDefault();

                                if (tier != null)
                                {
                                    JArray droppedBTsArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.APMTierOverflowBusinessTransactionRegularDataFilePath(jobTarget, tierREST));
                                    JArray droppedBTsDebugModeArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.APMTierOverflowBusinessTransactionRegularDataFilePath(jobTarget, tierREST));

                                    if (droppedBTsArray != null)
                                    {
                                        foreach (JObject unregisteredBusinessTransactionObject in droppedBTsArray)
                                        {
                                            string btName = getStringValueFromJToken(unregisteredBusinessTransactionObject, "name");
                                            if (unregisteredBusinessTransactionsDictionary.ContainsKey(btName) == true)
                                            {
                                                APMBusinessTransaction unregisteredBusinessTransaction = unregisteredBusinessTransactionsDictionary[btName];
                                                unregisteredBusinessTransaction.Calls += getLongValueFromJToken(unregisteredBusinessTransactionObject, "count");
                                            }
                                            else
                                            {
                                                APMBusinessTransaction unregisteredBusinessTransaction = new APMBusinessTransaction();
                                                unregisteredBusinessTransaction.ApplicationID = jobTarget.ApplicationID;
                                                unregisteredBusinessTransaction.ApplicationName = jobTarget.Application;
                                                unregisteredBusinessTransaction.BTID = -1;
                                                unregisteredBusinessTransaction.BTName = btName;
                                                unregisteredBusinessTransaction.BTNameOriginal = btName;
                                                unregisteredBusinessTransaction.IsRenamed = false;
                                                unregisteredBusinessTransaction.BTType = getStringValueFromJToken(unregisteredBusinessTransactionObject, "type"); ;
                                                unregisteredBusinessTransaction.Controller = jobTarget.Controller;
                                                unregisteredBusinessTransaction.TierID = tier.TierID;
                                                unregisteredBusinessTransaction.TierName = tier.TierName;

                                                unregisteredBusinessTransaction.Calls = getLongValueFromJToken(unregisteredBusinessTransactionObject, "count");

                                                updateEntityWithDeeplinks(unregisteredBusinessTransaction);

                                                unregisteredBusinessTransactionsList.Add(unregisteredBusinessTransaction);
                                                unregisteredBusinessTransactionsDictionary.Add(unregisteredBusinessTransaction.BTName, unregisteredBusinessTransaction);
                                            }
                                        }
                                    }

                                    #region Debug mode Overflow BT counting

                                    // It appears that the array in debug mode produces the same counts
                                    // It just contains the node name of the APM agent that tried to register this BT is 
                                    //if (droppedBTsDebugModeArray != null)
                                    //{
                                    //    foreach (JObject unregisteredBusinessTransactionObject in droppedBTsArray)
                                    //    {
                                    //        string btName = getStringValueFromJToken(unregisteredBusinessTransactionObject, "name");
                                    //        if (unregisteredBusinessTransactionsDictionary.ContainsKey(btName) == true)
                                    //        {
                                    //            APMBusinessTransaction unregisteredBusinessTransaction = unregisteredBusinessTransactionsDictionary[btName];
                                    //            unregisteredBusinessTransaction.Calls += getLongValueFromJToken(unregisteredBusinessTransactionObject, "count");
                                    //        }
                                    //        else
                                    //        {
                                    //            APMBusinessTransaction unregisteredBusinessTransaction = new APMBusinessTransaction();
                                    //            unregisteredBusinessTransaction.ApplicationID = jobTarget.ApplicationID;
                                    //            unregisteredBusinessTransaction.ApplicationName = jobTarget.Application;
                                    //            unregisteredBusinessTransaction.BTID = -1;
                                    //            unregisteredBusinessTransaction.BTName = btName;
                                    //            unregisteredBusinessTransaction.BTNameOriginal = btName;
                                    //            unregisteredBusinessTransaction.IsRenamed = false;
                                    //            unregisteredBusinessTransaction.BTType = getStringValueFromJToken(unregisteredBusinessTransactionObject, "type"); ;
                                    //            unregisteredBusinessTransaction.Controller = jobTarget.Controller;
                                    //            unregisteredBusinessTransaction.TierID = tier.TierID;
                                    //            unregisteredBusinessTransaction.TierName = tier.TierName;

                                    //            unregisteredBusinessTransaction.Calls = getLongValueFromJToken(unregisteredBusinessTransactionObject, "count");

                                    //            updateEntityWithDeeplinks(unregisteredBusinessTransaction);

                                    //            unregisteredBusinessTransactionsList.Add(unregisteredBusinessTransaction);
                                    //            unregisteredBusinessTransactionsDictionary.Add(unregisteredBusinessTransaction.BTName, unregisteredBusinessTransaction);
                                    //        }
                                    //    }
                                    //}

                                    #endregion
                                }
                            }

                            // Sort them
                            unregisteredBusinessTransactionsList = unregisteredBusinessTransactionsList.OrderBy(o => o.TierName).ThenBy(o => o.BTName).ToList();

                            FileIOHelper.WriteListToCSVFile(unregisteredBusinessTransactionsList, new APMOverflowBusinessTransactionReportMap(), FilePathMap.APMOverflowBusinessTransactionsIndexFilePath(jobTarget));

                            stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + unregisteredBusinessTransactionsList.Count;
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
                        if (File.Exists(FilePathMap.APMOverflowBusinessTransactionsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMOverflowBusinessTransactionsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMOverflowBusinessTransactionsReportFilePath(), FilePathMap.APMOverflowBusinessTransactionsIndexFilePath(jobTarget));
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

        private void updateEntityWithEntityDetailAndFlameGraphLinks(APMEntityBase entity, JobTarget jobTarget, JobTimeRange jobTimeRange)
        {
            entity.DetailLink = String.Format(@"=HYPERLINK(""{0}"", ""<Detail>"")", FilePathMap.EntityMetricAndDetailExcelReportFilePath(entity, jobTarget, jobTimeRange, false));
            entity.FlameGraphLink = String.Format(@"=HYPERLINK(""{0}"", ""<FlGraph>"")", FilePathMap.FlameGraphReportFilePath(entity, jobTarget, jobTimeRange, false));
            entity.FlameChartLink = String.Format(@"=HYPERLINK(""{0}"", ""<FlChart>"")", FilePathMap.FlameChartReportFilePath(entity, jobTarget, jobTimeRange, false));
            entity.MetricGraphLink = String.Format(@"=HYPERLINK(""{0}"", ""<Metrics>"")", FilePathMap.APMEntityTypeMetricGraphsExcelReportFilePath(entity, jobTarget, jobTimeRange, false));
        }

        /// <summary>
        /// https://docs.oracle.com/javase/8/docs/technotes/tools/windows/java.html
        /// If you are expected to specify the size in bytes, you can use no suffix, or use the suffix k or K for kilobytes (KB), m or M for megabytes (MB), 
        /// g or G for gigabytes (GB). For example, to set the size to 8 GB, you can specify either 8g, 8192m, 8388608k, or 8589934592 as the argument.
        /// </summary>
        /// <param name="memorySettingString">Value from Xmx or Xms java param</param>
        /// <returns>Numeric value in megabytes</returns>
        private double convertValueSettingFromJavaStartupMemoryParameterToMB(string memorySettingString)
        {
            if (memorySettingString != null && memorySettingString.Length > 0)
            {
                string suffix = memorySettingString.Substring(memorySettingString.Length - 1, 1);
                string memorySettingValueWithoutSuffix = memorySettingString.Substring(0, memorySettingString.Length - 1);

                long memorySettingValueBytes = -1;
                long memorySettingValueNumeric;
                switch (suffix)
                {
                    case "k":
                    case "K":
                        // Value in kilobytes
                        if (Int64.TryParse(memorySettingValueWithoutSuffix, out memorySettingValueNumeric) == true)
                        {
                            memorySettingValueBytes = memorySettingValueNumeric * 1024;
                        }
                        break;
                    case "m":
                    case "M":
                        // Value in megabytes
                        if (Int64.TryParse(memorySettingValueWithoutSuffix, out memorySettingValueNumeric) == true)
                        {
                            memorySettingValueBytes = memorySettingValueNumeric * 1024 * 1024;
                        }
                        break;
                    case "g":
                    case "G":
                        // Value in gigabytes
                        if (Int64.TryParse(memorySettingValueWithoutSuffix, out memorySettingValueNumeric) == true)
                        {
                            memorySettingValueBytes = memorySettingValueNumeric * 1024 * 1024 * 1024;
                        }
                        break;
                    default:
                        // Value must be numeric, and in bytes
                        if (Int64.TryParse(memorySettingString, out memorySettingValueNumeric) == true)
                        {
                            memorySettingValueBytes = memorySettingValueNumeric;
                        }
                        break;
                }

                // Convert to megabytes
                if (memorySettingValueBytes != -1)
                {
                    double memorySettingValueMegabytes = Math.Round((double)memorySettingValueBytes / 1048576, 2);
                    return memorySettingValueMegabytes;
                }
                else
                {
                    return -1;
                }
            }
            else
            {
                return -1;
            }
        }
    }
}
