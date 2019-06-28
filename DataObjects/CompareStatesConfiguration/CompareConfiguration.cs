using System.Collections.Generic;

namespace AppDynamics.Dexter
{
    public class CompareConfiguration
    {
        public CompareInput Input { get; set; }
        public CompareOutput Output { get; set; }
        public List<CompareTarget> Target { get; set; }
        public JobStatus Status { get; set; }
    }
}
