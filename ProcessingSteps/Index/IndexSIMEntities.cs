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
    public class IndexSIMEntities : JobStepIndexBase
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
                if (this.ShouldExecute(programOptions, jobConfiguration) == false)
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

                        JArray tiersArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.SIMTiersDataFilePath(jobTarget));
                        if (tiersArray != null)
                        {
                            loggerConsole.Info("Index List of Tiers ({0} entities)", tiersArray.Count);

                            tiersList = new List<SIMTier>(tiersArray.Count);

                            foreach (JToken tierToken in tiersArray)
                            {
                                SIMTier tier = new SIMTier();
                                tier.ApplicationID = jobTarget.ApplicationID;
                                tier.ApplicationName = jobTarget.Application;
                                tier.Controller = jobTarget.Controller;
                                tier.TierID = getLongValueFromJToken(tierToken, "id");
                                tier.TierName = getStringValueFromJToken(tierToken, "name");
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

                        JArray nodesArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.SIMNodesDataFilePath(jobTarget));
                        if (nodesArray != null)
                        {
                            loggerConsole.Info("Index List of Nodes ({0} entities)", nodesArray.Count);

                            nodesList = new List<SIMNode>(nodesArray.Count);

                            foreach (JToken nodeToken in nodesArray)
                            {
                                SIMNode node = new SIMNode();
                                node.ApplicationID = jobTarget.ApplicationID;
                                node.ApplicationName = jobTarget.Application;
                                node.Controller = jobTarget.Controller;
                                node.NodeID = getLongValueFromJToken(nodeToken, "id");
                                node.NodeName = getStringValueFromJToken(nodeToken, "name");
                                if (tiersList != null)
                                {
                                    SIMTier tier = tiersList.Where(t => t.TierName == getStringValueFromJToken(nodeToken, "tierName")).FirstOrDefault();
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

                        #region Machines and all the other goodies

                        List<SIMMachine> machinesList = null;
                        List<SIMMachineProperty> machinePropertiesAndTagsList = null;
                        List<SIMMachineCPU> machineCPUsList = null;
                        List<SIMMachineVolume> machineVolumesList = null;
                        List<SIMMachineNetwork> machineNetworksList = null;
                        List<SIMMachineContainer> machineContainersList = null;
                        List<SIMMachineProcess> machineProcessesList = null;

                        JArray machinesArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.SIMMachinesDataFilePath(jobTarget));
                        if (machinesArray != null)
                        {
                            loggerConsole.Info("Index Machines Configuration ({0} entities)", machinesArray.Count);

                            machinesList = new List<SIMMachine>(machinesArray.Count);
                            machinePropertiesAndTagsList = new List<SIMMachineProperty>(machinesArray.Count * 20);
                            machineCPUsList = new List<SIMMachineCPU>(machinesArray.Count);
                            machineVolumesList = new List<SIMMachineVolume>(machinesArray.Count * 3);
                            machineNetworksList = new List<SIMMachineNetwork>(machinesArray.Count * 3);
                            machineContainersList = new List<SIMMachineContainer>(machinesArray.Count * 10);
                            machineProcessesList = new List<SIMMachineProcess>(machinesArray.Count * 20);

                            int j = 0;

                            foreach (JToken machineToken in machinesArray)
                            {
                                JObject machineObject = FileIOHelper.LoadJObjectFromFile(FilePathMap.SIMMachineDataFilePath(jobTarget, getStringValueFromJToken(machineToken, "name"), getLongValueFromJToken(machineToken, "id")));
                                JArray machineContainersObject = FileIOHelper.LoadJArrayFromFile(FilePathMap.SIMMachineDockerContainersDataFilePath(jobTarget, getStringValueFromJToken(machineToken, "name"), getLongValueFromJToken(machineToken, "id")));
                                JArray machineProcessesObject = FileIOHelper.LoadJArrayFromFile(FilePathMap.SIMMachineProcessesDataFilePath(jobTarget, getStringValueFromJToken(machineToken, "name"), getLongValueFromJToken(machineToken, "id"), jobConfiguration.Input.TimeRange));

                                if (machineObject != null)
                                {
                                    #region Machine summary

                                    SIMMachine machine = new SIMMachine();
                                    machine.ApplicationID = jobTarget.ApplicationID;
                                    machine.ApplicationName = jobTarget.Application;
                                    machine.Controller = jobTarget.Controller;
                                    machine.MachineID = getLongValueFromJToken(machineObject, "id");
                                    machine.MachineName = getStringValueFromJToken(machineObject, "name");
                                    if (nodesList != null)
                                    {
                                        try
                                        {
                                            SIMNode nodeSIM = nodesList.Where(t => t.NodeID == getLongValueFromJToken(machineObject, "simNodeId")).FirstOrDefault();
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
                                    machine.MachineType = getStringValueFromJToken(machineObject, "type");
                                    machine.IsHistorical = getBoolValueFromJToken(machineObject, "historical");
                                    machine.IsEnabled = getBoolValueFromJToken(machineObject, "simEnabled");
                                    machine.DynamicMonitoringMode = getStringValueFromJToken(machineObject, "dynamicMonitoringMode");

                                    try { machine.HostMachineID = getLongValueFromJToken(machineObject["agentConfig"]["rawConfig"]["_agentRegistrationSupplementalConfig"], "hostSimMachineId"); } catch { }
                                    try { machine.DotnetCompatibilityMode = getBoolValueFromJToken(machineObject["agentConfig"]["rawConfig"]["_dotnetRegistrationRequestConfig"], "dotnetCompatibilityMode"); } catch { }
                                    try { machine.ForceMachineInstanceRegistration = getBoolValueFromJToken(machineObject["agentConfig"]["rawConfig"]["_machineInstanceRegistrationRequestConfig"], "forceMachineInstanceRegistration"); } catch { }

                                    try { machine.AgentConfigFeatures = getStringValueOfObjectFromJToken(machineObject["agentConfig"]["rawConfig"]["_features"], "features", true); } catch { }
                                    try { machine.ControllerConfigFeatures = getStringValueOfObjectFromJToken(machineObject["controllerConfig"]["rawConfig"]["_features"], "features", true); } catch { }

                                    if (isTokenPropertyNull(machineObject, "memory") == false)
                                    {
                                        try { machine.MemPhysical = getIntValueFromJToken(machineObject["memory"]["Physical"], "sizeMb"); } catch { }
                                        try { machine.MemSwap = getIntValueFromJToken(machineObject["memory"]["Swap"], "sizeMb"); } catch { }
                                    }

                                    if (isTokenPropertyNull(machineObject, "agentConfig") == false &&
                                        isTokenPropertyNull(machineObject["agentConfig"], "rawConfig") == false &&
                                        isTokenPropertyNull(machineObject["agentConfig"]["rawConfig"], "_agentRegistrationRequestConfig") == false)
                                    {
                                        JToken agentRegistrationRequestConfigToken = machineObject["agentConfig"]["rawConfig"]["_agentRegistrationRequestConfig"];

                                        machine.MachineInfo = getStringValueFromJToken(agentRegistrationRequestConfigToken, "machineInfo");
                                        machine.JVMInfo = getStringValueFromJToken(agentRegistrationRequestConfigToken, "jvmInfo");
                                        machine.InstallDirectory = getStringValueFromJToken(agentRegistrationRequestConfigToken, "installDirectory");
                                        machine.AgentVersionRaw = getStringValueFromJToken(agentRegistrationRequestConfigToken, "agentVersion");
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
                                        machine.AutoRegisterAgent = getBoolValueFromJToken(agentRegistrationRequestConfigToken, "autoRegisterAgent");
                                        machine.AgentType = getStringValueFromJToken(agentRegistrationRequestConfigToken, "agentType");
                                        try
                                        {
                                            JToken jToken = agentRegistrationRequestConfigToken["applicationNames"];
                                            if (jToken != null && jToken.Type == JTokenType.Array && jToken.First != null)
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
                                        machine.APMTierName = getStringValueFromJToken(agentRegistrationRequestConfigToken, "tierName");
                                        machine.APMNodeName = getStringValueFromJToken(agentRegistrationRequestConfigToken, "nodeName");
                                    }


                                    #endregion

                                    #region Properties

                                    if (isTokenPropertyNull(machineObject, "properties") == false)
                                    {
                                        foreach (JProperty propertyProperty in machineObject["properties"])
                                        {
                                            SIMMachineProperty machineProp = new SIMMachineProperty();
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
                                            machineProp.PropName = propertyProperty.Name;
                                            machineProp.PropValue = propertyProperty.Value.ToString();

                                            machine.NumProps++;

                                            machinePropertiesAndTagsList.Add(machineProp);
                                        }
                                    }

                                    #endregion

                                    #region Tags

                                    if (isTokenPropertyNull(machineObject, "tags") == false)
                                    {
                                        foreach (JProperty tagProperty in machineObject["tags"])
                                        {
                                            foreach (JToken propertyValue in tagProperty.Value)
                                            {
                                                SIMMachineProperty machineProp = new SIMMachineProperty();
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
                                                machineProp.PropName = tagProperty.Name;
                                                machineProp.PropValue = propertyValue.ToString();

                                                machine.NumTags++;

                                                machinePropertiesAndTagsList.Add(machineProp);
                                            }
                                        }
                                    }
                                    #endregion

                                    #region CPUs

                                    if (isTokenPropertyNull(machineObject, "cpus") == false)
                                    {
                                        foreach (JObject cpuObject in machineObject["cpus"])
                                        {
                                            SIMMachineCPU machineCPU = new SIMMachineCPU();
                                            machineCPU.ApplicationID = machine.ApplicationID;
                                            machineCPU.ApplicationName = machine.ApplicationName;
                                            machineCPU.Controller = machine.Controller;
                                            machineCPU.TierID = machine.TierID;
                                            machineCPU.TierName = machine.TierName;
                                            machineCPU.NodeID = machine.NodeID;
                                            machineCPU.NodeName = machine.NodeName;
                                            machineCPU.MachineID = machine.MachineID;
                                            machineCPU.MachineName = machine.MachineName;

                                            machineCPU.CPUID = getStringValueFromJToken(cpuObject, "cpuId");
                                            machineCPU.NumCores = getIntValueFromJToken(cpuObject, "coreCount");
                                            machineCPU.NumLogical = getIntValueFromJToken(cpuObject, "logicalCount");
                                            machineCPU.Vendor = getStringValueFromJToken(cpuObject["properties"], "Vendor");
                                            machineCPU.Flags = getStringValueFromJToken(cpuObject["properties"], "Flags");
                                            machineCPU.NumFlags = machineCPU.Flags.Split(' ').Length;
                                            machineCPU.Model = getStringValueFromJToken(cpuObject["properties"], "Model Name");
                                            machineCPU.Speed = getStringValueFromJToken(cpuObject["properties"], "Max Speed MHz");

                                            machine.NumCPUs++;

                                            machineCPUsList.Add(machineCPU);
                                        }
                                    }

                                    #endregion

                                    #region Volumes

                                    if (isTokenPropertyNull(machineObject, "volumes") == false)
                                    {
                                        foreach (JObject volumeObject in machineObject["volumes"])
                                        {
                                            SIMMachineVolume machineVolume = new SIMMachineVolume();
                                            machineVolume.ApplicationID = machine.ApplicationID;
                                            machineVolume.ApplicationName = machine.ApplicationName;
                                            machineVolume.Controller = machine.Controller;
                                            machineVolume.TierID = machine.TierID;
                                            machineVolume.TierName = machine.TierName;
                                            machineVolume.NodeID = machine.NodeID;
                                            machineVolume.NodeName = machine.NodeName;
                                            machineVolume.MachineID = machine.MachineID;
                                            machineVolume.MachineName = machine.MachineName;

                                            machineVolume.MountPoint = getStringValueFromJToken(volumeObject, "mountPoint");
                                            machineVolume.Partition = getStringValueFromJToken(volumeObject, "partition");
                                            machineVolume.SizeMB = getIntValueFromJToken(volumeObject["properties"], "Size (MB)");
                                            machineVolume.PartitionMetricName = getStringValueFromJToken(volumeObject["properties"], "PartitionMetricName");
                                            machineVolume.VolumeMetricName = getStringValueFromJToken(volumeObject["properties"], "VolumeMetricName");

                                            machine.NumVolumes++;

                                            machineVolumesList.Add(machineVolume);
                                        }
                                    }

                                    #endregion

                                    #region Networks

                                    if (isTokenPropertyNull(machineObject, "networkInterfaces") == false)
                                    {
                                        foreach (JObject networkObject in machineObject["networkInterfaces"])
                                        {
                                            SIMMachineNetwork machineNetwork = new SIMMachineNetwork();
                                            machineNetwork.ApplicationID = machine.ApplicationID;
                                            machineNetwork.ApplicationName = machine.ApplicationName;
                                            machineNetwork.Controller = machine.Controller;
                                            machineNetwork.TierID = machine.TierID;
                                            machineNetwork.TierName = machine.TierName;
                                            machineNetwork.NodeID = machine.NodeID;
                                            machineNetwork.NodeName = machine.NodeName;
                                            machineNetwork.MachineID = machine.MachineID;
                                            machineNetwork.MachineName = machine.MachineName;

                                            machineNetwork.NetworkName = getStringValueFromJToken(networkObject, "name");
                                            machineNetwork.MacAddress = getStringValueFromJToken(networkObject, "macAddress");
                                            machineNetwork.IP4Address = getStringValueFromJToken(networkObject, "ip4Address");
                                            machineNetwork.IP6Address = getStringValueFromJToken(networkObject, "ip6Address");
                                            machineNetwork.IP4Gateway = getStringValueFromJToken(networkObject["properties"], "IPv4 Default Gateway");
                                            machineNetwork.IP6Gateway = getStringValueFromJToken(networkObject["properties"], "IPv6 Default Gateway");
                                            machineNetwork.PluggedIn = getStringValueFromJToken(networkObject["properties"], "Plugged In");
                                            machineNetwork.Enabled = getStringValueFromJToken(networkObject["properties"], "Enabled");
                                            machineNetwork.State = getStringValueFromJToken(networkObject["properties"], "Operational State");
                                            machineNetwork.Speed = getLongValueFromJToken(networkObject["properties"], "Speed");
                                            machineNetwork.Duplex = getStringValueFromJToken(networkObject["properties"], "Duplex");
                                            machineNetwork.MTU = getStringValueFromJToken(networkObject["properties"], "MTU");
                                            machineNetwork.NetworkMetricName = getStringValueFromJToken(networkObject["properties"], "MetricName");

                                            machine.NumNetworks++;

                                            machineNetworksList.Add(machineNetwork);
                                        }
                                    }

                                    #endregion

                                    #region Containers

                                    if (machineContainersObject != null)
                                    {
                                        foreach (JObject containerObject in machineContainersObject)
                                        {
                                            SIMMachineContainer machineContainer = new SIMMachineContainer();
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
                                            machineContainer.ContainerID = getStringValueFromJToken(containerObject, "containerId");
                                            machineContainer.ContainerName = getStringValueFromJToken(containerObject, "containerName");
                                            machineContainer.ImageName = getStringValueFromJToken(containerObject, "imageName");
                                            machineContainer.ContainerMachineID = getLongValueFromJToken(containerObject, "containerSimMachineId");

                                            // This worked in 4.4.3.4
                                            // form is identical to what is returned in machine
                                            machineContainer.ContainerID = getStringValueFromJToken(containerObject["properties"], "Container|Full Id");
                                            machineContainer.ContainerName = getStringValueFromJToken(containerObject["properties"], "Container|Name");
                                            machineContainer.ImageName = getStringValueFromJToken(containerObject["properties"], "Container|Image|Name");
                                            machineContainer.StartedAt = getStringValueFromJToken(containerObject["properties"], "Container|Started At");

                                            machine.NumContainers++;

                                            machineContainersList.Add(machineContainer);
                                        }
                                    }

                                    #endregion

                                    #region Processes

                                    if (machineProcessesObject != null)
                                    {
                                        foreach (JObject machineProcessGroupObject in machineProcessesObject)
                                        {
                                            foreach (JObject processObject in machineProcessGroupObject["processes"])
                                            {
                                                SIMMachineProcess machineProcess = new SIMMachineProcess();
                                                machineProcess.ApplicationID = machine.ApplicationID;
                                                machineProcess.ApplicationName = machine.ApplicationName;
                                                machineProcess.Controller = machine.Controller;
                                                machineProcess.TierID = machine.TierID;
                                                machineProcess.TierName = machine.TierName;
                                                machineProcess.NodeID = machine.NodeID;
                                                machineProcess.NodeName = machine.NodeName;
                                                machineProcess.MachineID = machine.MachineID;
                                                machineProcess.MachineName = machine.MachineName;

                                                machineProcess.Class = getStringValueFromJToken(processObject, "processClass");
                                                machineProcess.ClassID = getStringValueFromJToken(processObject, "classId");
                                                machineProcess.Name = getStringValueFromJToken(processObject, "name");

                                                machineProcess.CommandLine = getStringValueFromJToken(processObject, "commandLine");
                                                machineProcess.RealUser = getStringValueFromJToken(processObject["properties"], "realUser");
                                                machineProcess.RealGroup = getStringValueFromJToken(processObject["properties"], "realGroup");
                                                machineProcess.EffectiveUser = getStringValueFromJToken(processObject, "effectiveUser");
                                                machineProcess.EffectiveGroup = getStringValueFromJToken(processObject["properties"], "effectiveGroup");
                                                machineProcess.State = getStringValueFromJToken(processObject, "state");
                                                try { machineProcess.NiceLevel = getIntValueFromJToken(processObject["properties"], "niceLevel"); } catch { }

                                                machineProcess.PID = getIntValueFromJToken(processObject, "processId");
                                                machineProcess.ParentPID = getIntValueFromJToken(processObject, "parentProcessId");
                                                machineProcess.PGID = getIntValueFromJToken(processObject["properties"], "pgid");

                                                machineProcess.StartTime = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(processObject, "startTime"));
                                                machineProcess.EndTime = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(processObject, "endTime"));

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

                            FileIOHelper.WriteListToCSVFile(machinesList, new SIMMachineReportMap(), FilePathMap.SIMMachinesIndexFilePath(jobTarget));
                            FileIOHelper.WriteListToCSVFile(machinePropertiesAndTagsList, new SIMMachinePropertyReportMap(), FilePathMap.SIMMachinePropertiesIndexFilePath(jobTarget));
                            FileIOHelper.WriteListToCSVFile(machineCPUsList, new SIMMachineCPUReportMap(), FilePathMap.SIMMachineCPUsIndexFilePath(jobTarget));
                            FileIOHelper.WriteListToCSVFile(machineVolumesList, new SIMMachineVolumeReportMap(), FilePathMap.SIMMachineVolumesIndexFilePath(jobTarget));
                            FileIOHelper.WriteListToCSVFile(machineNetworksList, new SIMMachineNetworkReportMap(), FilePathMap.SIMMachineNetworksIndexFilePath(jobTarget));
                            FileIOHelper.WriteListToCSVFile(machineContainersList, new SIMMachineContainerReportMap(), FilePathMap.SIMMachineContainersIndexFilePath(jobTarget));
                            FileIOHelper.WriteListToCSVFile(machineProcessesList, new SIMMachineProcessReportMap(), FilePathMap.SIMMachineProcessesIndexFilePath(jobTarget));

                            Console.WriteLine("{0} done", machinesArray.Count);
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

                        FileIOHelper.WriteListToCSVFile(applicationsList, new SIMApplicationReportMap(), FilePathMap.SIMApplicationsIndexFilePath(jobTarget));

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

                        // Append all the individual report files into one
                        if (File.Exists(FilePathMap.SIMApplicationsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.SIMApplicationsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.SIMApplicationsReportFilePath(), FilePathMap.SIMApplicationsIndexFilePath(jobTarget));
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
                entityRow.ApplicationLink = String.Format(DEEPLINK_SIM_APPLICATION, entityRow.Controller, DEEPLINK_THIS_TIMERANGE);
            }
            else if (entityRow is SIMTier)
            {
                SIMTier entity = (SIMTier)entityRow;
                entity.ControllerLink = String.Format(DEEPLINK_CONTROLLER, entity.Controller, DEEPLINK_THIS_TIMERANGE);
                entityRow.ApplicationLink = String.Format(DEEPLINK_SIM_APPLICATION, entityRow.Controller, DEEPLINK_THIS_TIMERANGE);
            }
            else if (entityRow is SIMNode)
            {
                SIMNode entity = (SIMNode)entityRow;
                entity.ControllerLink = String.Format(DEEPLINK_CONTROLLER, entity.Controller, DEEPLINK_THIS_TIMERANGE);
                entityRow.ApplicationLink = String.Format(DEEPLINK_SIM_APPLICATION, entityRow.Controller, DEEPLINK_THIS_TIMERANGE);
            }
            else if (entityRow is SIMMachine)
            {
                SIMMachine entity = (SIMMachine)entityRow;
                entity.ControllerLink = String.Format(DEEPLINK_CONTROLLER, entity.Controller, DEEPLINK_THIS_TIMERANGE);
                entityRow.ApplicationLink = String.Format(DEEPLINK_SIM_APPLICATION, entityRow.Controller, DEEPLINK_THIS_TIMERANGE);
                entity.MachineLink = String.Format(DEEPLINK_SIM_MACHINE, entity.Controller, entity.ApplicationID, entity.MachineID, DEEPLINK_THIS_TIMERANGE);
            }
        }
    }
}
