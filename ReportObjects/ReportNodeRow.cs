namespace AppDynamics.OfflineData.ReportObjects
{
    public class ReportNodeRow
    {
        public string ApplicationName { get; set; }
        public int ApplicationID { get; set; }
        public string AgentType { get; set; }
        public bool AgentPresent { get; set; }
        public string AgentVersion { get; set; }
        public int AgentID { get; set; }
        public string Controller { get; set; }
        public bool MachineAgentPresent { get; set; }
        public string MachineAgentVersion { get; set; }
        public int MachineID { get; set; }
        public string MachineName { get; set; }
        public string MachineOSType { get; set; }
        public string NodeName { get; set; }
        public int TierID { get; set; }
        public string Type { get; set; }
        public string TierName { get; set; }
    }
}
