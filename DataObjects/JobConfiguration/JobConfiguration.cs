using System.Collections.Generic;

namespace AppDynamics.Dexter
{
    public class JobConfiguration
    {
        /// <summary>
        //{
        //    "Input": {
        //        "TimeRange" : {
        //            "From": "2017-06-26T18:00:00",
        //            "To": "2017-06-26T19:59:00"
        //        },
        //        "Flowmaps": true,
        //        "Metrics": true,
        //        "MetricsSelectionCriteria": [ "Transaction", "AgentStats", "OSHardware", "CLRStats", "IISStats", "JVMStats", "BusinessTransactionPercentiles" ],
        //        "Snapshots": true,
        //        "SnapshotSelectionCriteria" : {complex object, look at DefaultJob.json}
        //        "Events": false,
        //        "Configuration": true
        //    },
        //    "Output": 
        //    {
        //      "DetectedEntities": true,
        //      "EntityMetrics": false,
        //      "EntityMetricGraphs": true,
        //      "EntityDetails": false,
        //      "Snapshots": false,
        //      "FlameGraphs": false,
        //      "Events": false,
        //      "Configuration": false
        //    },
        //    "Target": [
        //        {
        //            "Controller": "http://your.controller.here",
        //            "UserName": "username@customer1",
        //            "UserPassword": "yourpassword",
        //            "Application": "YourApplication",
        //            "NameRegex": false
        //        }
        //    ]
        //}
        /// </summary>

        public JobInput Input { get; set; }
        public JobOutput Output { get; set; }
        public List<JobTarget> Target { get; set; }
        public JobStatus Status { get; set; }
    }
}
