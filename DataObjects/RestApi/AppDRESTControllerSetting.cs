using System;

namespace AppDynamics.Dexter.DataObjects
{
    /// <summary>
    ///{
    ///  "updateable": true,
    ///  "scope": "cluster",
    ///  "name": "disable.dashboard.metrics.caching",
    ///  "description": "Disable metrics caching for dashboards? (true|false)",
    ///  "value": "false"
    ///}
    /// </summary>
    public class AppDRESTControllerSetting
    {
        public string name { get; set; }
        public string value { get; set; }
        public string description { get; set; }
        public string scope { get; set; }
        public bool updateable { get; set; }

        public override String ToString()
        {
            return String.Format(
                "AppDRESTControllerSetting: {0}={1}",
                this.name,
                this.value);
        }
    }
}
