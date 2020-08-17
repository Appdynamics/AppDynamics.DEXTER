using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class Metric
    {
        public string Controller { get; set; }

        public long ApplicationID { get; set; }
        public string ApplicationName { get; set; }

        public long TierID { get; set; }
        public string TierName { get; set; }
        public string TierAgentType { get; set; }

        public long BTID { get; set; }
        public string BTName { get; set; }
        public string BTType { get; set; }

        public long NodeID { get; set; }
        public string NodeName { get; set; }
        public string NodeAgentType { get; set; }

        public long SEPID { get; set; }
        public string SEPName { get; set; }
        public string SEPType { get; set; }

        public long IPID { get; set; }
        public string IPName { get; set; }
        public string IPType { get; set; }

        public long BackendID { get; set; }
        public string BackendName { get; set; }
        public string BackendType { get; set; }

        public long ErrorID { get; set; }
        public string ErrorName { get; set; }
        public string ErrorType { get; set; }

        public long EntityID { get; set; }
        public string EntityName { get; set; }
        public string EntityType { get; set; }

        public string MetricName { get; set; }
        public long MetricID { get; set; }

        public string MetricPath { get; set; }
        public int NumSegments { get; set; }
        public bool HasActivity { get; set; }

        public string Segment1 { get; set; }
        public string Segment2 { get; set; }
        public string Segment3 { get; set; }
        public string Segment4 { get; set; }
        public string Segment5 { get; set; }
        public string Segment6 { get; set; }
        public string Segment7 { get; set; }
        public string Segment8 { get; set; }
        public string Segment9 { get; set; }
        public string Segment10 { get; set; }
        public string Segment11 { get; set; }
        public string Segment12 { get; set; }
        public string Segment13 { get; set; }
        public string Segment14 { get; set; }
        public string Segment15 { get; set; }
        public string Segment16 { get; set; }
        public string Segment17 { get; set; }
        public string Segment18 { get; set; }
        public string Segment19 { get; set; }
        public string Segment20 { get; set; }
        public string Segment21 { get; set; }
        public string Segment22 { get; set; }

        public override String ToString()
        {
            return String.Format(
                "Metric: Controller={0}, ApplicationName={1}, EntityName={2}, MetricName={3}, MetricPath={4}, HasActivity={5}",
                this.Controller,
                this.ApplicationName,
                this.EntityName,
                this.MetricName,
                this.MetricPath,
                this.HasActivity);
        }
    }
}