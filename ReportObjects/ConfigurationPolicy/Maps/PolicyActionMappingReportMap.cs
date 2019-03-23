using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class PolicyActionMappingReportMap : ClassMap<PolicyActionMapping>
    {
        public PolicyActionMappingReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;

            Map(m => m.PolicyName).Index(i); i++;
            Map(m => m.PolicyType).Index(i); i++;

            Map(m => m.ActionName).Index(i); i++;
            Map(m => m.ActionType).Index(i); i++;

            Map(m => m.PolicyID).Index(i); i++;
            Map(m => m.ActionID).Index(i); i++;
        }
    }
}