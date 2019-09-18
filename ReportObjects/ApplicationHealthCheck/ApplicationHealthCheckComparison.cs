using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class ApplicationHealthCheckComparison
    {
        public double LatestAppAgentVersion { get; set; }
        public double LatestMachineAgentVersion { get; set; }
        public int AgentOldPercent { get; set; }
        public int InfoPointUpper { get; set; }
        public int InfoPointLower { get; set; }
        public int DCUpper { get; set; }
        public int DCLower { get; set; }

    }
}
