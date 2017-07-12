using System.Collections.Generic;

namespace AppDynamics.OfflineData.JobParameters
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
        //        "Snapshots": true
        //    }
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
        public List<JobTarget> Target { get; set; }
        public JobStatus Status { get; set; }
    }
}
