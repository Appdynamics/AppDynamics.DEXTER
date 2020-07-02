using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class APMNode : APMEntityBase
    {
        public const string ENTITY_TYPE = "Node";
        public const string ENTITY_FOLDER = "NODE";

        public long TierID { get; set; }
        public string TierLink { get; set; }
        public string TierName { get; set; }

        public long NodeID { get; set; }
        public string NodeLink { get; set; }
        public string NodeName { get; set; }

        public string AgentType { get; set; }
        public bool AgentPresent { get; set; }
        public string AgentVersion { get; set; }
        public string AgentVersionRaw { get; set; }

        public bool MachineAgentPresent { get; set; }
        public string MachineAgentVersion { get; set; }
        public string MachineAgentVersionRaw { get; set; }
        public long MachineID { get; set; }
        public string MachineName { get; set; }
        public string MachineOSType { get; set; }
        public string MachineType { get; set; }

        public bool IsDisabled { get; set; }
        public bool IsMonitoringDisabled { get; set; }

        public string InstallDirectory { get; set; }
        public DateTime LastStartTime { get; set; }
        public DateTime InstallTime { get; set; }

        public int NumStartupOptions { get; set; }
        public int NumProperties { get; set; }
        public int NumEnvVariables { get; set; }

        public string AgentRuntime { get; set; }

        public bool IsAPMAgentUsed { get; set; }
        public bool IsMachineAgentUsed { get; set; }

        // IIS, Self-hosted, Tomcat, JBoss, WebSphere, WebLogic, GlassFish, Jetty
        public string WebHostContainerType { get; set; }
        // AWS, Azure, GCP, OnPrem
        public string CloudHostType { get; set; }
        public string CloudRegion { get; set; }
        // Kubernetes, Openshift, Pivotal
        public string ContainerRuntimeType { get; set; }

        //java.class.path
        public string ClassPath { get; set; }
        //java.class.version
        public string ClassVersion { get; set; }

        //java.home
        public string Home { get; set; }

        //java.runtime.name
        public string RuntimeName { get; set; }
        //java.runtime.version
        public string RuntimeVersion { get; set; }

        //java.vendor
        //or Microsoft for CLR
        public string Vendor { get; set; }
        //java.vendor.version
        public string VendorVersion { get; set; }
        //java.version
        //CLR Version
        public string Version { get; set; }

        //java.vm.info
        public string VMInfo { get; set; }
        //java.vm.name
        public string VMName { get; set; }
        //java.vm.vendor
        //or Microsoft for CLR
        public string VMVendor { get; set; }
        //java.vm.version
        public string VMVersion { get; set; }

        //os.arch
        //PROCESSOR_ARCHITECTURE
        public string OSArchitecture { get; set; }
        //os.name
        //OS
        public string OSName { get; set; }
        //os.version
        public string OSVersion { get; set; }
        //COMPUTERNAME, HOSTNAME, HOST_NAME
        public string OSComputerName { get; set; }
        
        //PROCESSOR_IDENTIFIER
        public string OSProcessorType { get; set; }
        //PROCESSOR_REVISION
        public string OSProcessorRevision { get; set; }
        // NUMBER_OF_PROCESSORS
        public int? OSNumberOfProcs { get; set; }

        //USER, USERNAME
        public string UserName { get; set; }
        //DOMAIN, DOMAINNAME
        public string Domain { get; set; }

        // -Xmnsize
        // -XX:NewSize
        public double? HeapYoungInitialSizeMB { get; set; }
        // -XX:MaxNewSize
        public double? HeapYoungMaxSizeMB { get; set; }
        // -Xmssize
        public double? HeapInitialSizeMB { get; set; }
        // -XX:MaxHeapSize
        // -Xmxsize
        public double? HeapMaxSizeMB { get; set; }

        public override long EntityID
        {
            get
            {
                return this.NodeID;
            }
        }
        public override string EntityName
        {
            get
            {
                return this.NodeName;
            }
        }
        public override string EntityType
        {
            get
            {
                return ENTITY_TYPE;
            }
        }
        public override string FolderName
        {
            get
            {
                return ENTITY_FOLDER;
            }
        }

        public APMNode Clone()
        {
            return (APMNode)this.MemberwiseClone();
        }

        public override String ToString()
        {
            return String.Format(
                "APMNode: {0}/{1}({2})/{3}({4})/{5}({6})",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID,
                this.TierName,
                this.TierID,
                this.NodeName,
                this.NodeID);
        }
    }
}
