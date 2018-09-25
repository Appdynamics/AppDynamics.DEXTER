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
    public class IndexControllerSIMApplicationsAndEntities : JobStepIndexBase
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

                if (jobConfiguration.Target.Count(t => t.Type == APPLICATION_TYPE_SIM) == 0)
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

                    if (jobTarget.Type != null && jobTarget.Type.Length > 0 && jobTarget.Type != APPLICATION_TYPE_SIM) continue;

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

                        #region Tiers

                        List<SIMTier> tiersList = null;

                        JArray tiersRESTList = FileIOHelper.LoadJArrayFromFile(FilePathMap.SIMTiersDataFilePath(jobTarget));
                        if (tiersRESTList != null)
                        {
                            loggerConsole.Info("Index List of Tiers ({0} entities)", tiersRESTList.Count);

                            tiersList = new List<SIMTier>(tiersRESTList.Count);

                            foreach (JToken tierREST in tiersRESTList)
                            {
                                SIMTier tier = new SIMTier();
                                tier.ApplicationID = jobTarget.ApplicationID;
                                tier.ApplicationName = jobTarget.Application;
                                tier.Controller = jobTarget.Controller;
                                tier.TierID = (long)tierREST["id"];
                                tier.TierName = tierREST["name"].ToString();
                                tier.NumSegments = tier.TierName.Split('|').Length;
                                // Will be filled later
                                tier.NumNodes = 0;

                                updateEntityWithDeeplinks(tier, jobConfiguration.Input.TimeRange);

                                tiersList.Add(tier);
                            }

                            // Sort them
                            tiersList = tiersList.OrderBy(o => o.TierName).ToList();

                            stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + tiersList.Count;
                        }

                        #endregion

                        #region Nodes

                        List<SIMNode> nodesList = null;

                        JArray nodesRESTList = FileIOHelper.LoadJArrayFromFile(FilePathMap.SIMNodesDataFilePath(jobTarget));
                        if (nodesRESTList != null)
                        {
                            loggerConsole.Info("Index List of Nodes ({0} entities)", nodesRESTList.Count);

                            nodesList = new List<SIMNode>(nodesRESTList.Count);

                            foreach (JToken nodeREST in nodesRESTList)
                            {
                                SIMNode node = new SIMNode();
                                node.ApplicationID = jobTarget.ApplicationID;
                                node.ApplicationName = jobTarget.Application;
                                node.Controller = jobTarget.Controller;
                                node.NodeID = (long)nodeREST["id"];
                                node.NodeName = nodeREST["name"].ToString();
                                if (tiersList != null)
                                {
                                    SIMTier tier = tiersList.Where(t => t.TierName == nodeREST["tierName"].ToString()).FirstOrDefault();
                                    if (tier != null)
                                    {
                                        node.TierID = tier.TierID;
                                        node.TierName = tier.TierName;
                                        tier.NumNodes++;
                                    }
                                }

                                updateEntityWithDeeplinks(node, jobConfiguration.Input.TimeRange);

                                nodesList.Add(node);
                            }

                            // Sort them
                            nodesList = nodesList.OrderBy(o => o.TierName).ThenBy(o => o.NodeName).ToList();

                            stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + nodesList.Count;

                            FileIOHelper.WriteListToCSVFile(tiersList, new SIMTierReportMap(), FilePathMap.SIMTiersIndexFilePath(jobTarget));
                            FileIOHelper.WriteListToCSVFile(nodesList, new SIMNodeReportMap(), FilePathMap.SIMNodesIndexFilePath(jobTarget));
                        }

                        #endregion

                        #region Machines

                        List<Machine> machinesList = null;
                        List<MachineProperty> machinePropertiesAndTagsList = null;
                        List<MachineCPU> machineCPUsList = null;
                        List<MachineVolume> machineVolumesList = null;
                        List<MachineNetwork> machineNetworksList = null;
                        List<MachineContainer> machineContainersList = null;
                        List<MachineProcess> machineProcessesList = null;

                        JArray machinesRESTList = FileIOHelper.LoadJArrayFromFile(FilePathMap.SIMMachinesDataFilePath(jobTarget));
                        if (machinesRESTList != null)
                        {
                            loggerConsole.Info("Index Machines Configuration ({0} entities)", machinesRESTList.Count);

                            machinesList = new List<Machine>(machinesRESTList.Count);
                            machinePropertiesAndTagsList = new List<MachineProperty>(machinesRESTList.Count * 20);
                            machineCPUsList = new List<MachineCPU>(machinesRESTList.Count);
                            machineVolumesList = new List<MachineVolume>(machinesRESTList.Count * 3);
                            machineNetworksList = new List<MachineNetwork>(machinesRESTList.Count * 3);
                            machineContainersList = new List<MachineContainer>(machinesRESTList.Count * 10);
                            machineProcessesList = new List<MachineProcess>(machinesRESTList.Count * 20);

                            int j = 0;

                            foreach (JToken machineRESTinList in machinesRESTList)
                            {
                                JObject machineREST = FileIOHelper.LoadJObjectFromFile(FilePathMap.SIMMachineDataFilePath(jobTarget, machineRESTinList["name"].ToString(), (long)machineRESTinList["id"]));
                                JArray machineContainerREST = FileIOHelper.LoadJArrayFromFile(FilePathMap.SIMMachineDockerContainersDataFilePath(jobTarget, machineRESTinList["name"].ToString(), (long)machineRESTinList["id"]));
                                JArray machineProcessesREST = FileIOHelper.LoadJArrayFromFile(FilePathMap.SIMMachineProcessesDataFilePath(jobTarget, machineRESTinList["name"].ToString(), (long)machineRESTinList["id"], jobConfiguration.Input.TimeRange));

                                if (machineREST != null)
                                {
                                    #region Machine summary

                                    Machine machine = new Machine();
                                    machine.ApplicationID = jobTarget.ApplicationID;
                                    machine.ApplicationName = jobTarget.Application;
                                    machine.Controller = jobTarget.Controller;
                                    machine.MachineID = (long)machineREST["id"];
                                    machine.MachineName = machineREST["name"].ToString();
                                    if (nodesList != null)
                                    {
                                        try
                                        {
                                            SIMNode nodeSIM = nodesList.Where(t => t.NodeID == (long)machineREST["simNodeId"]).FirstOrDefault();
                                            if (nodeSIM != null)
                                            {
                                                machine.TierID = nodeSIM.TierID;
                                                machine.TierName = nodeSIM.TierName;
                                                machine.NodeID = nodeSIM.NodeID;
                                                machine.NodeName = nodeSIM.NodeName;
                                            }
                                        }
                                        catch { }
                                    }
                                    try {machine.MachineType = machineREST["type"].ToString(); } catch { }
                                    try {machine.IsHistorical = (bool)machineREST["historical"]; } catch { }
                                    try {machine.IsEnabled = (bool)machineREST["simEnabled"]; } catch { }
                                    try {machine.DynamicMonitoringMode = machineREST["dynamicMonitoringMode"].ToString(); } catch { }

                                    try { machine.HostMachineID = (long)machineREST["agentConfig"]["rawConfig"]["_agentRegistrationSupplementalConfig"]["hostSimMachineId"]; } catch { }
                                    try { machine.DotnetCompatibilityMode = (bool)machineREST["agentConfig"]["rawConfig"]["_dotnetRegistrationRequestConfig"]["dotnetCompatibilityMode"]; } catch { }
                                    try { machine.ForceMachineInstanceRegistration = (bool)machineREST["agentConfig"]["rawConfig"]["_machineInstanceRegistrationRequestConfig"]["forceMachineInstanceRegistration"]; } catch { }

                                    try { machine.AgentConfigFeatures = machineREST["agentConfig"]["rawConfig"]["_features"]["features"].ToString(Newtonsoft.Json.Formatting.None); } catch { }
                                    try { machine.ControllerConfigFeatures = machineREST["controllerConfig"]["rawConfig"]["_features"]["features"].ToString(Newtonsoft.Json.Formatting.None); } catch { }

                                    if (machineREST["memory"].HasValues == true)
                                    {
                                        try { machine.MemPhysical = (int)machineREST["memory"]["Physical"]["sizeMb"]; } catch { }
                                        try { machine.MemSwap = (int)machineREST["memory"]["Swap"]["sizeMb"]; } catch { }
                                    }

                                    try {machine.MachineInfo = machineREST["agentConfig"]["rawConfig"]["_agentRegistrationRequestConfig"]["machineInfo"].ToString(); } catch { }
                                    try {machine.JVMInfo = machineREST["agentConfig"]["rawConfig"]["_agentRegistrationRequestConfig"]["jvmInfo"].ToString(); } catch { }
                                    try {machine.InstallDirectory = machineREST["agentConfig"]["rawConfig"]["_agentRegistrationRequestConfig"]["installDirectory"].ToString(); } catch { }
                                    try {machine.AgentVersionRaw = machineREST["agentConfig"]["rawConfig"]["_agentRegistrationRequestConfig"]["agentVersion"].ToString(); } catch { }
                                    if (machine.AgentVersionRaw != String.Empty)
                                    {
                                        // Machine agent looks like that 
                                        //Machine Agent v4.2.3.2 GA Build Date 2016 - 07 - 11 10:26:01
                                        //Machine Agent v3.7.16.0 GA Build Date 2014 - 02 - 26 21:20:29
                                        //Machine Agent v4.2.3.2 GA Build Date 2016 - 07 - 11 10:17:54
                                        //Machine Agent v4.0.6.0 GA Build Date 2015 - 05 - 11 20:56:44
                                        //Machine Agent v3.8.3.0 GA Build Date 2014 - 06 - 06 17:09:13
                                        //Machine Agent v4.1.7.1 GA Build Date 2015 - 11 - 24 20:49:24

                                        Regex regexVersion = new Regex(@"(?i).*v(\d*\.\d*\.\d*(\.\d*)?).*", RegexOptions.IgnoreCase);
                                        Match match = regexVersion.Match(machine.AgentVersionRaw);
                                        if (match != null)
                                        {
                                            if (match.Groups.Count > 1)
                                            {
                                                machine.AgentVersion = match.Groups[1].Value;
                                                if (machine.AgentVersion.Count(v => v == '.') < 3)
                                                {
                                                    machine.AgentVersion = String.Format("{0}.0", machine.AgentVersion);
                                                }
                                            }
                                        }
                                    }
                                    try { machine.AutoRegisterAgent = (bool)machineREST["agentConfig"]["rawConfig"]["_agentRegistrationRequestConfig"]["autoRegisterAgent"]; } catch { }
                                    try { machine.AgentType = machineREST["agentConfig"]["rawConfig"]["_agentRegistrationRequestConfig"]["agentType"].ToString(); } catch { }
                                    try
                                    {
                                        JToken jToken = machineREST["agentConfig"]["rawConfig"]["_agentRegistrationRequestConfig"]["applicationNames"];
                                        if (jToken.Type == JTokenType.Array && jToken.First != null)
                                        {
                                            if (jToken.Count() > 0)
                                            {
                                                if (jToken.Count() == 1)
                                                {
                                                    machine.APMApplicationName = jToken.First.ToString();
                                                }
                                                else
                                                {
                                                    machine.APMApplicationName = jToken.ToString(Newtonsoft.Json.Formatting.None);
                                                }
                                            }
                                        }
                                    }
                                    catch { }
                                    try {machine.APMTierName = machineREST["agentConfig"]["rawConfig"]["_agentRegistrationRequestConfig"]["tierName"].ToString(); } catch { }
                                    try {machine.APMNodeName = machineREST["agentConfig"]["rawConfig"]["_agentRegistrationRequestConfig"]["nodeName"].ToString(); } catch { }

                                    #endregion

                                    #region Properties

                                    if (machineREST["properties"] != null)
                                    {
                                        foreach (JProperty property in machineREST["properties"])
                                        {
                                            MachineProperty machineProp = new MachineProperty();
                                            machineProp.ApplicationID = machine.ApplicationID;
                                            machineProp.ApplicationName = machine.ApplicationName;
                                            machineProp.Controller = machine.Controller;
                                            machineProp.TierID = machine.TierID;
                                            machineProp.TierName = machine.TierName;
                                            machineProp.NodeID = machine.NodeID;
                                            machineProp.NodeName = machine.NodeName;
                                            machineProp.MachineID = machine.MachineID;
                                            machineProp.MachineName = machine.MachineName;

                                            machineProp.PropType = "Property";
                                            machineProp.PropName = property.Name;
                                            machineProp.PropValue = property.Value.ToString();

                                            machine.NumProps++;

                                            machinePropertiesAndTagsList.Add(machineProp);
                                        }
                                    }

                                    #endregion

                                    #region Tags

                                    if (machineREST["tags"] != null)
                                    {
                                        foreach (JProperty property in machineREST["tags"])
                                        {
                                            foreach (JToken propertyValue in property.Value)
                                            {
                                                MachineProperty machineProp = new MachineProperty();
                                                machineProp.ApplicationID = machine.ApplicationID;
                                                machineProp.ApplicationName = machine.ApplicationName;
                                                machineProp.Controller = machine.Controller;
                                                machineProp.TierID = machine.TierID;
                                                machineProp.TierName = machine.TierName;
                                                machineProp.NodeID = machine.NodeID;
                                                machineProp.NodeName = machine.NodeName;
                                                machineProp.MachineID = machine.MachineID;
                                                machineProp.MachineName = machine.MachineName;

                                                machineProp.PropType = "Tag";
                                                machineProp.PropName = property.Name;
                                                machineProp.PropValue = propertyValue.ToString();

                                                machine.NumTags++;

                                                machinePropertiesAndTagsList.Add(machineProp);
                                            }
                                        }
                                    }
                                    #endregion

                                    #region CPUs

                                    if (machineREST["cpus"] != null)
                                    {
                                        foreach (JObject cpu in machineREST["cpus"])
                                        {
                                            MachineCPU machineCPU = new MachineCPU();
                                            machineCPU.ApplicationID = machine.ApplicationID;
                                            machineCPU.ApplicationName = machine.ApplicationName;
                                            machineCPU.Controller = machine.Controller;
                                            machineCPU.TierID = machine.TierID;
                                            machineCPU.TierName = machine.TierName;
                                            machineCPU.NodeID = machine.NodeID;
                                            machineCPU.NodeName = machine.NodeName;
                                            machineCPU.MachineID = machine.MachineID;
                                            machineCPU.MachineName = machine.MachineName;

                                            machineCPU.CPUID = cpu["cpuId"].ToString();
                                            try { machineCPU.NumCores = (int)cpu["coreCount"]; } catch { }
                                            try { machineCPU.NumLogical = (int)cpu["logicalCount"]; } catch { }
                                            try { machineCPU.Vendor = cpu["properties"]["Vendor"].ToString(); } catch { }
                                            machineCPU.Flags = String.Empty;
                                            try { machineCPU.Flags = cpu["properties"]["Flags"].ToString(); } catch { }
                                            machineCPU.NumFlags = machineCPU.Flags.Split(' ').Length;
                                            try { machineCPU.Model = cpu["properties"]["Model Name"].ToString(); } catch { }
                                            try { machineCPU.Speed = cpu["properties"]["Max Speed MHz"].ToString(); } catch { }

                                            machine.NumCPUs++;

                                            machineCPUsList.Add(machineCPU);
                                        }
                                    }

                                    #endregion

                                    #region Volumes

                                    if (machineREST["volumes"] != null)
                                    {
                                        foreach (JObject volume in machineREST["volumes"])
                                        {
                                            MachineVolume machineVolume = new MachineVolume();
                                            machineVolume.ApplicationID = machine.ApplicationID;
                                            machineVolume.ApplicationName = machine.ApplicationName;
                                            machineVolume.Controller = machine.Controller;
                                            machineVolume.TierID = machine.TierID;
                                            machineVolume.TierName = machine.TierName;
                                            machineVolume.NodeID = machine.NodeID;
                                            machineVolume.NodeName = machine.NodeName;
                                            machineVolume.MachineID = machine.MachineID;
                                            machineVolume.MachineName = machine.MachineName;

                                            machineVolume.MountPoint = volume["mountPoint"].ToString();
                                            machineVolume.Partition = volume["partition"].ToString();
                                            try { machineVolume.SizeMB = (int)volume["properties"]["Size (MB)"]; } catch { }
                                            try { machineVolume.PartitionMetricName = volume["properties"]["PartitionMetricName"].ToString(); } catch { }
                                            try { machineVolume.VolumeMetricName = volume["properties"]["VolumeMetricName"].ToString(); } catch { }

                                            machine.NumVolumes++;

                                            machineVolumesList.Add(machineVolume);
                                        }
                                    }

                                    #endregion

                                    #region Networks

                                    if (machineREST["networkInterfaces"] != null)
                                    {
                                        foreach (JObject network in machineREST["networkInterfaces"])
                                        {
                                            MachineNetwork machineNetwork = new MachineNetwork();
                                            machineNetwork.ApplicationID = machine.ApplicationID;
                                            machineNetwork.ApplicationName = machine.ApplicationName;
                                            machineNetwork.Controller = machine.Controller;
                                            machineNetwork.TierID = machine.TierID;
                                            machineNetwork.TierName = machine.TierName;
                                            machineNetwork.NodeID = machine.NodeID;
                                            machineNetwork.NodeName = machine.NodeName;
                                            machineNetwork.MachineID = machine.MachineID;
                                            machineNetwork.MachineName = machine.MachineName;

                                            machineNetwork.NetworkName = network["name"].ToString();
                                            machineNetwork.MacAddress = network["macAddress"].ToString();
                                            machineNetwork.IP4Address = network["ip4Address"].ToString();
                                            machineNetwork.IP6Address = network["ip6Address"].ToString();
                                            try { machineNetwork.IP4Gateway = network["properties"]["IPv4 Default Gateway"].ToString(); } catch { }
                                            try { machineNetwork.IP6Gateway = network["properties"]["IPv6 Default Gateway"].ToString(); } catch { }
                                            try { machineNetwork.PluggedIn = network["properties"]["Plugged In"].ToString(); } catch { }
                                            try { machineNetwork.Enabled = network["properties"]["Enabled"].ToString(); } catch { }
                                            try { machineNetwork.State = network["properties"]["Operational State"].ToString(); } catch { }
                                            try { machineNetwork.Speed = (int)network["properties"]["Speed"]; } catch { }
                                            try { machineNetwork.Duplex = network["properties"]["Duplex"].ToString(); } catch { }
                                            try { machineNetwork.MTU = network["properties"]["MTU"].ToString(); } catch { }
                                            try { machineNetwork.NetworkMetricName = network["properties"]["MetricName"].ToString(); } catch { }

                                            machine.NumNetworks++;

                                            machineNetworksList.Add(machineNetwork);
                                        }
                                    }

                                    #endregion

                                    #region Containers

                                    if (machineContainerREST != null)
                                    {
                                        foreach (JObject container in machineContainerREST)
                                        {
                                            MachineContainer machineContainer = new MachineContainer();
                                            machineContainer.ApplicationID = machine.ApplicationID;
                                            machineContainer.ApplicationName = machine.ApplicationName;
                                            machineContainer.Controller = machine.Controller;
                                            machineContainer.TierID = machine.TierID;
                                            machineContainer.TierName = machine.TierName;
                                            machineContainer.NodeID = machine.NodeID;
                                            machineContainer.NodeName = machine.NodeName;
                                            machineContainer.MachineID = machine.MachineID;
                                            machineContainer.MachineName = machine.MachineName;

                                            // This worked in 4.4.1, the format is 
                                            //   {
	                                        //	"containerId": "e25bf24eb981bd10d0ef22b78b4b4a73b4cc242813371c43849635c77b3be95f",
	                                        //	"containerName": "settlementServices",
	                                        //	"imageName": "fin-java-services",
	                                        //	"containerSimMachineId": 224
	                                        // }
                                            try { machineContainer.ContainerID = container["containerId"].ToString(); } catch { }
                                            try { machineContainer.ContainerName = container["containerName"].ToString(); } catch { }
                                            try { machineContainer.ImageName = container["imageName"].ToString(); } catch { }
                                            try { machineContainer.ContainerMachineID = (long)container["containerSimMachineId"]; } catch { }

                                            // This worked in 4.4.3.4
                                            // form is identical to what is returned in machine
                                            try { machineContainer.ContainerID = container["properties"]["Container|Full Id"].ToString(); } catch { }
                                            try { machineContainer.ContainerName = container["properties"]["Container|Name"].ToString(); } catch { }
                                            try { machineContainer.ImageName = container["properties"]["Container|Image|Name"].ToString(); } catch { }
                                            try { machineContainer.StartedAt = container["properties"]["Container|Started At"].ToString(); } catch { }

                                            machine.NumContainers++;

                                            machineContainersList.Add(machineContainer);
                                        }
                                    }

                                    #endregion

                                    #region Processes

                                    if (machineProcessesREST != null)
                                    {
                                        foreach (JObject machineProcessGroup in machineProcessesREST)
                                        {
                                            foreach (JObject process in machineProcessGroup["processes"])
                                            {
                                                MachineProcess machineProcess = new MachineProcess();
                                                machineProcess.ApplicationID = machine.ApplicationID;
                                                machineProcess.ApplicationName = machine.ApplicationName;
                                                machineProcess.Controller = machine.Controller;
                                                machineProcess.TierID = machine.TierID;
                                                machineProcess.TierName = machine.TierName;
                                                machineProcess.NodeID = machine.NodeID;
                                                machineProcess.NodeName = machine.NodeName;
                                                machineProcess.MachineID = machine.MachineID;
                                                machineProcess.MachineName = machine.MachineName;

                                                machineProcess.Class = process["processClass"].ToString();
                                                machineProcess.ClassID = process["classId"].ToString();
                                                machineProcess.Name = process["name"].ToString();

                                                machineProcess.CommandLine = process["commandLine"].ToString();
                                                try { machineProcess.RealUser = process["properties"]["realUser"].ToString(); } catch { }
                                                try { machineProcess.RealGroup = process["properties"]["realGroup"].ToString(); } catch { }
                                                try { machineProcess.EffectiveUser = process["effectiveUser"].ToString(); } catch { }
                                                try { machineProcess.EffectiveGroup = process["properties"]["effectiveGroup"].ToString(); } catch { }
                                                machineProcess.State = process["state"].ToString();
                                                try { machineProcess.NiceLevel = (int)process["properties"]["niceLevel"]; } catch { }

                                                machineProcess.PID = (int)process["processId"];
                                                try { machineProcess.ParentPID = (int)process["parentProcessId"]; } catch { }
                                                try { machineProcess.PGID = (int)process["properties"]["pgid"]; } catch { }

                                                try
                                                {
                                                    long timeStamp = (long)process["startTime"];
                                                    machineProcess.StartTime = UnixTimeHelper.ConvertFromUnixTimestamp(timeStamp);
                                                }
                                                catch { }
                                                try
                                                {
                                                    long timeStamp = (long)process["endTime"];
                                                    machineProcess.EndTime = UnixTimeHelper.ConvertFromUnixTimestamp(timeStamp);
                                                }
                                                catch { }

                                                machineProcessesList.Add(machineProcess);
                                            }
                                        }
                                    }

                                    #endregion

                                    updateEntityWithDeeplinks(machine, jobConfiguration.Input.TimeRange);

                                    machinesList.Add(machine);
                                }

                                j++;
                                if (j % 50 == 0)
                                {
                                    Console.Write("[{0}].", j);
                                }
                            }

                            // Sort them
                            machinesList = machinesList.OrderBy(o => o.TierName).ThenBy(o => o.MachineName).ToList();
                            machinePropertiesAndTagsList = machinePropertiesAndTagsList.OrderBy(o => o.TierName).ThenBy(o => o.MachineName).ThenBy(o => o.PropType).ThenBy(o => o.PropName).ToList();
                            machineCPUsList = machineCPUsList.OrderBy(o => o.TierName).ThenBy(o => o.MachineName).ToList();
                            machineVolumesList = machineVolumesList.OrderBy(o => o.TierName).ThenBy(o => o.MachineName).ThenBy(o => o.MountPoint).ToList();
                            machineNetworksList = machineNetworksList.OrderBy(o => o.TierName).ThenBy(o => o.MachineName).ThenBy(o => o.NetworkName).ToList();
                            machineContainersList = machineContainersList.OrderBy(o => o.TierName).ThenBy(o => o.MachineName).ThenBy(o => o.ContainerName).ToList();
                            machineProcessesList = machineProcessesList.OrderBy(o => o.TierName).ThenBy(o => o.MachineName).ThenBy(o => o.Class).ToList();

                            stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + machinesList.Count;

                            FileIOHelper.WriteListToCSVFile(machinesList, new MachineReportMap(), FilePathMap.SIMMachinesIndexFilePath(jobTarget));
                            FileIOHelper.WriteListToCSVFile(machinePropertiesAndTagsList, new MachinePropertyReportMap(), FilePathMap.SIMMachinePropertiesIndexFilePath(jobTarget));
                            FileIOHelper.WriteListToCSVFile(machineCPUsList, new MachineCPUReportMap(), FilePathMap.SIMMachineCPUsIndexFilePath(jobTarget));
                            FileIOHelper.WriteListToCSVFile(machineVolumesList, new MachineVolumeReportMap(), FilePathMap.SIMMachineVolumesIndexFilePath(jobTarget));
                            FileIOHelper.WriteListToCSVFile(machineNetworksList, new MachineNetworkReportMap(), FilePathMap.SIMMachineNetworksIndexFilePath(jobTarget));
                            FileIOHelper.WriteListToCSVFile(machineContainersList, new MachineContainerReportMap(), FilePathMap.SIMMachineContainersIndexFilePath(jobTarget));
                            FileIOHelper.WriteListToCSVFile(machineProcessesList, new MachineProcessReportMap(), FilePathMap.SIMMachineProcessesIndexFilePath(jobTarget));

                            Console.WriteLine("{0} done", machinesRESTList.Count);
                        }

                        #endregion

                        #region Application

                        loggerConsole.Info("Index Application");

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + 1;

                        List<SIMApplication> applicationsList = new List<SIMApplication>(1);
                        SIMApplication applicationRow = new SIMApplication();
                        applicationRow.ApplicationID = jobTarget.ApplicationID;
                        applicationRow.ApplicationName = jobTarget.Application;
                        applicationRow.Controller = jobTarget.Controller;
                        if (tiersList != null)
                        {
                            applicationRow.NumTiers = tiersList.Count;
                        }
                        if (nodesList != null)
                        {
                            applicationRow.NumNodes = nodesList.Count;
                        }
                        if (machinesList != null)
                        {
                            applicationRow.NumMachines = machinesList.Count;
                        }

                        updateEntityWithDeeplinks(applicationRow, jobConfiguration.Input.TimeRange);

                        applicationsList.Add(applicationRow);

                        FileIOHelper.WriteListToCSVFile(applicationsList, new SIMApplicationReportMap(), FilePathMap.SIMApplicationIndexFilePath(jobTarget));

                        #endregion

                        #region Combine All for Report CSV

                        // If it is the first one, clear out the combined folder
                        if (reportFolderCleaned == false)
                        {
                            FileIOHelper.DeleteFolder(FilePathMap.SIMEntitiesReportFolderPath());
                            Thread.Sleep(1000);
                            FileIOHelper.CreateFolder(FilePathMap.SIMEntitiesReportFolderPath());
                            reportFolderCleaned = true;
                        }

                        // Append all the individual application files into one
                        if (File.Exists(FilePathMap.SIMApplicationIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.SIMApplicationIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.SIMApplicationsReportFilePath(), FilePathMap.SIMApplicationIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.SIMTiersIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.SIMTiersIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.SIMTiersReportFilePath(), FilePathMap.SIMTiersIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.SIMNodesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.SIMNodesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.SIMNodesReportFilePath(), FilePathMap.SIMNodesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.SIMMachinesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.SIMMachinesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.SIMMachinesReportFilePath(), FilePathMap.SIMMachinesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.SIMMachinePropertiesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.SIMMachinePropertiesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.SIMMachinePropertiesReportFilePath(), FilePathMap.SIMMachinePropertiesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.SIMMachineCPUsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.SIMMachineCPUsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.SIMMachineCPUsReportFilePath(), FilePathMap.SIMMachineCPUsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.SIMMachineVolumesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.SIMMachineVolumesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.SIMMachineVolumesReportFilePath(), FilePathMap.SIMMachineVolumesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.SIMMachineNetworksIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.SIMMachineNetworksIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.SIMMachineNetworksReportFilePath(), FilePathMap.SIMMachineNetworksIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.SIMMachineContainersIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.SIMMachineContainersIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.SIMMachineContainersReportFilePath(), FilePathMap.SIMMachineContainersIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.SIMMachineProcessesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.SIMMachineProcessesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.SIMMachineProcessesReportFilePath(), FilePathMap.SIMMachineProcessesIndexFilePath(jobTarget));
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
            logger.Trace("{0} is always executed", jobConfiguration.Status);
            loggerConsole.Trace("{0} is always executed", jobConfiguration.Status);
            return true;
        }

        private void updateEntityWithDeeplinks(SIMEntityBase entityRow)
        {
            updateEntityWithDeeplinks(entityRow, null);
        }

        private void updateEntityWithDeeplinks(SIMEntityBase entityRow, JobTimeRange jobTimeRange)
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
            if (entityRow is SIMApplication)
            {
                entityRow.ControllerLink = String.Format(DEEPLINK_CONTROLLER, entityRow.Controller, DEEPLINK_THIS_TIMERANGE);
                entityRow.ApplicationLink = String.Format(DEEPLINK_SIMAPPLICATION, entityRow.Controller, DEEPLINK_THIS_TIMERANGE);
            }
            else if (entityRow is SIMTier)
            {
                SIMTier entity = (SIMTier)entityRow;
                entity.ControllerLink = String.Format(DEEPLINK_CONTROLLER, entity.Controller, DEEPLINK_THIS_TIMERANGE);
                entityRow.ApplicationLink = String.Format(DEEPLINK_SIMAPPLICATION, entityRow.Controller, DEEPLINK_THIS_TIMERANGE);
            }
            else if (entityRow is SIMNode)
            {
                SIMNode entity = (SIMNode)entityRow;
                entity.ControllerLink = String.Format(DEEPLINK_CONTROLLER, entity.Controller, DEEPLINK_THIS_TIMERANGE);
                entityRow.ApplicationLink = String.Format(DEEPLINK_SIMAPPLICATION, entityRow.Controller, DEEPLINK_THIS_TIMERANGE);
            }
            else if (entityRow is Machine)
            {
                Machine entity = (Machine)entityRow;
                entity.ControllerLink = String.Format(DEEPLINK_CONTROLLER, entity.Controller, DEEPLINK_THIS_TIMERANGE);
                entityRow.ApplicationLink = String.Format(DEEPLINK_SIMAPPLICATION, entityRow.Controller, DEEPLINK_THIS_TIMERANGE);
                entity.MachineLink = String.Format(DEEPLINK_SIMMACHINE, entity.Controller, entity.ApplicationID, entity.MachineID, DEEPLINK_THIS_TIMERANGE);
            }
        }
    }
}
