using System;

namespace AppDynamics.Dexter.DataObjects
{
    /// <summary>
    ///  {
    ///    "agentType": "DOT_NET_APP_AGENT",
    ///    "appAgentPresent": true,
    ///    "appAgentVersion": "4.2.11.0",
    ///    "id": 171,
    ///    "ipAddresses": null,
    ///    "machineAgentPresent": true,
    ///    "machineAgentVersion": "4.2.11.0",
    ///    "machineId": 30,
    ///    "machineName": "CRM2016FULL",
    ///    "machineOSType": "Other",
    ///    "name": "CRM2016FULL-Web-Microsoft Dynamics CRM",
    ///    "nodeUniqueLocalId": "",
    ///    "tierId": 31,
    ///    "tierName": "Web",
    ///    "type": "Other"
    ///  }
    /// </summary>
    public class AppDRESTNode
    {
        public string agentType { get; set; }
        public bool appAgentPresent { get; set; }
        public string appAgentVersion { get; set; }
        public long id { get; set; }
        //public string ipAddresses { get; set; }
        public bool machineAgentPresent { get; set; }
        public string machineAgentVersion { get; set; }
        public long machineId { get; set; }
        public string machineName { get; set; }
        public string machineOSType { get; set; }
        public string name { get; set; }
        public string nodeUniqueLocalId { get; set; }
        public long tierId { get; set; }
        public string tierName { get; set; }
        public string type { get; set; }

        public override String ToString()
        {
            return String.Format(
                "AppDRESTNode: {0}({1})",
                this.name,
                this.id);
        }
    }
}
