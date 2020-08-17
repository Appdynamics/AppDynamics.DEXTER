using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class MetricSummary
    {
        public string Controller { get; set; }

        public long ApplicationID { get; set; }
        public string ApplicationName { get; set; }

        public string MetricPrefix { get; set; }
        public int NumAll { get; set; }
        public int NumActivity { get; set; }
        public int NumNoActivity { get; set; }

        public string MetricsListLink { get; set; }

        public override String ToString()
        {
            return String.Format(
                "MetricSummary: Controller={0}, ApplicationName={1}, MetricPrefix={2}",
                this.Controller,
                this.ApplicationName,
                this.MetricPrefix);
        }
    }
}