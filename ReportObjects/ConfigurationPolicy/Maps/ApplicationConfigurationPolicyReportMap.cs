using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class ApplicationConfigurationPolicyReportMap : ClassMap<ApplicationConfigurationPolicy>
    {
        public ApplicationConfigurationPolicyReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.Type).Index(i); i++;

            Map(m => m.NumHealthRules).Index(i); i++;
            Map(m => m.NumPolicies).Index(i); i++;
            Map(m => m.NumActions).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
        }
    }
}