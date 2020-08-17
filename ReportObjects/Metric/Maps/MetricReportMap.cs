using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class MetricReportMap : ClassMap<Metric>
    {
        public MetricReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;

            Map(m => m.TierName).Index(i); i++;
            Map(m => m.TierAgentType).Index(i); i++;
            Map(m => m.NodeName).Index(i); i++;
            Map(m => m.NodeAgentType).Index(i); i++;
            Map(m => m.BTName).Index(i); i++;
            Map(m => m.BTType).Index(i); i++;
            Map(m => m.SEPName).Index(i); i++;
            Map(m => m.SEPType).Index(i); i++;
            Map(m => m.IPName).Index(i); i++;
            Map(m => m.IPType).Index(i); i++;
            Map(m => m.BackendName).Index(i); i++;
            Map(m => m.BackendType).Index(i); i++;
            Map(m => m.ErrorName).Index(i); i++;
            Map(m => m.ErrorType).Index(i); i++;

            Map(m => m.EntityName).Index(i); i++;
            Map(m => m.EntityType).Index(i); i++;

            Map(m => m.MetricName).Index(i); i++;
            
            Map(m => m.NumSegments).Index(i); i++;
            Map(m => m.HasActivity).Index(i); i++;

            Map(m => m.Segment1).Index(i); i++;
            Map(m => m.Segment2).Index(i); i++;
            Map(m => m.Segment3).Index(i); i++;
            Map(m => m.Segment4).Index(i); i++;
            Map(m => m.Segment5).Index(i); i++;
            Map(m => m.Segment6).Index(i); i++;
            Map(m => m.Segment7).Index(i); i++;
            Map(m => m.Segment8).Index(i); i++;
            Map(m => m.Segment9).Index(i); i++;
            Map(m => m.Segment10).Index(i); i++;
            Map(m => m.Segment11).Index(i); i++;
            Map(m => m.Segment12).Index(i); i++;
            Map(m => m.Segment13).Index(i); i++;
            Map(m => m.Segment14).Index(i); i++;
            Map(m => m.Segment15).Index(i); i++;
            Map(m => m.Segment16).Index(i); i++;
            Map(m => m.Segment17).Index(i); i++;
            Map(m => m.Segment18).Index(i); i++;
            Map(m => m.Segment19).Index(i); i++;
            Map(m => m.Segment20).Index(i); i++;
            Map(m => m.Segment21).Index(i); i++;
            Map(m => m.Segment22).Index(i); i++;

            Map(m => m.MetricPath).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.TierID).Index(i); i++;
            Map(m => m.NodeID).Index(i); i++;
            Map(m => m.BTID).Index(i); i++;
            Map(m => m.SEPID).Index(i); i++;
            Map(m => m.BackendID).Index(i); i++;
            Map(m => m.ErrorID).Index(i); i++;
            Map(m => m.IPID).Index(i); i++;
            Map(m => m.EntityID).Index(i); i++;
            Map(m => m.MetricID).Index(i); i++;
        }
    }
}
