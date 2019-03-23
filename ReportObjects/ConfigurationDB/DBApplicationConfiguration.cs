using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class DBApplicationConfiguration : ConfigurationEntityBase
    {
        public int NumCollectorDefinitions { get; set; }
        public int NumCustomMetrics { get; set; }
        
        public override String ToString()
        {
            return String.Format(
                "DBApplicationConfiguration: {0} {1}",
                this.Controller,
                this.NumCollectorDefinitions);
        }
    }
}
