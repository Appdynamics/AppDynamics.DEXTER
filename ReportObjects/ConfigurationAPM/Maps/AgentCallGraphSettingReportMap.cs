using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class AgentCallGraphSettingReportMap : ClassMap<AgentCallGraphSetting>
    {
        public AgentCallGraphSettingReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;

            Map(m => m.AgentType).Index(i); i++;
            Map(m => m.SamplingRate).Index(i); i++;
            Map(m => m.IncludePackages).Index(i); i++;
            Map(m => m.NumIncludePackages).Index(i); i++;
            Map(m => m.ExcludePackages).Index(i); i++;
            Map(m => m.NumExcludePackages).Index(i); i++;
            Map(m => m.MinSQLDuration).Index(i); i++;
            Map(m => m.IsRawSQLEnabled).Index(i); i++;
            Map(m => m.IsHotSpotEnabled).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
        }
    }
}