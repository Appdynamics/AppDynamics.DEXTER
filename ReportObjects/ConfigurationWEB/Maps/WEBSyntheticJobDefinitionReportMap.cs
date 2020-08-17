using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class WEBSyntheticJobDefinitionReportMap : ClassMap<WEBSyntheticJobDefinition>
    {
        public WEBSyntheticJobDefinitionReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;

            Map(m => m.JobName).Index(i); i++;
            Map(m => m.JobType).Index(i); i++;

            Map(m => m.IsUserEnabled).Index(i); i++;
            Map(m => m.IsSystemEnabled).Index(i); i++;
            Map(m => m.FailOnError).Index(i); i++;
            Map(m => m.IsPrivateAgent).Index(i); i++;

            Map(m => m.Rate).Index(i); i++;
            Map(m => m.RateUnit).Index(i); i++;
            Map(m => m.Timeout).Index(i); i++;

            Map(m => m.Days).Index(i); i++;
            Map(m => m.Browsers).Index(i); i++;
            Map(m => m.Locations).Index(i); i++;
            Map(m => m.NumLocations).Index(i); i++;
            Map(m => m.ScheduleMode).Index(i); i++;

            Map(m => m.URL).Index(i); i++;
            Map(m => m.Script).Index(i); i++;

            Map(m => m.Network).Index(i); i++;
            Map(m => m.Config).Index(i); i++;
            Map(m => m.PerfCriteria).Index(i); i++;

            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.CreatedOn), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.CreatedOnUtc), i); i++;

            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.UpdatedOn), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.UpdatedOnUtc), i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.JobID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
        }
    }
}