namespace AppDynamics.OfflineData.DataObjects
{
    /// <summary>
    ///  {
    ///    "agentType": "DOT_NET_APP_AGENT",
    ///    "description": "",
    ///    "id": 92,
    ///    "name": "Async",
    ///    "numberOfNodes": 2,
    ///    "type": ".NET Application Server"
    ///  },
    ///  {
    ///    "agentType": "DOT_NET_APP_AGENT",
    ///    "description": "",
    ///    "id": 91,
    ///    "name": "AsyncMaintenance",
    ///    "numberOfNodes": 2,
    ///    "type": ".NET Application Server"
    ///  }
    /// </summary>
    public class AppDRESTTier
    {
        public string agentType { get; set; }
        public string description { get; set; }
        public int id { get; set; }
        public string name { get; set; }
        public int numberOfNodes { get; set; }
        public string type { get; set; }
    }
}
