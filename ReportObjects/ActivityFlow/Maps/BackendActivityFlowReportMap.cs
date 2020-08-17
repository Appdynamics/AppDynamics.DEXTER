using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class BackendActivityFlowReportMap : ClassMap<ActivityFlow>
    {
        public BackendActivityFlowReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.BackendName).Index(i); i++;
            Map(m => m.CallType).Index(i); i++;
            Map(m => m.CallDirection).Index(i); i++;
            Map(m => m.FromName).Index(i); i++;
            Map(m => m.FromType).Index(i); i++;
            Map(m => m.FromEntityID).Index(i); i++;
            Map(m => m.ToName).Index(i); i++;
            Map(m => m.ToType).Index(i); i++;
            Map(m => m.ToEntityID).Index(i); i++;
            Map(m => m.IsCrossApplication).Index(i); i++;
            Map(m => m.ART).Index(i); i++;
            Map(m => m.Calls).Index(i); i++;
            Map(m => m.CPM).Index(i); i++;
            Map(m => m.Errors).Index(i); i++;
            Map(m => m.EPM).Index(i); i++;
            Map(m => m.ErrorsPercentage).Index(i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.From), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.To), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.FromUtc), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.ToUtc), i); i++;
            Map(m => m.Duration).Index(i); i++;
            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.BackendID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
            Map(m => m.BackendLink).Index(i); i++;
            Map(m => m.FromLink).Index(i); i++;
            Map(m => m.ToLink).Index(i); i++;
            Map(m => m.MetricLink).Index(i); i++;
        }
    }
}