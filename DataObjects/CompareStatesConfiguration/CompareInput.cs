using System.Collections.Generic;

namespace AppDynamics.Dexter
{
    public class CompareInput
    {
        public bool DetectedEntities { get; set; }
        public bool Flowmaps { get; set; }
        public bool Metrics { get; set; }
        public bool Snapshots { get; set; }
        public bool Configuration { get; set; }
    }
}
