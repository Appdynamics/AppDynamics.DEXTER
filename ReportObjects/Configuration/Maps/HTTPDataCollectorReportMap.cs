using CsvHelper.Configuration;

namespace AppDynamics.Dexter.DataObjects
{
    public class HTTPDataCollectorReportMap : ClassMap<HTTPDataCollector>
    {
        public HTTPDataCollectorReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;

            Map(m => m.CollectorName).Index(i); i++;
            Map(m => m.DataGathererName).Index(i); i++;
            Map(m => m.DataGathererValue).Index(i); i++;

            Map(m => m.IsURLEnabled).Index(i); i++;
            Map(m => m.IsSessionIDEnabled).Index(i); i++;
            Map(m => m.IsUserPrincipalEnabled).Index(i); i++;

            Map(m => m.IsAssignedToNewBTs).Index(i); i++;
            Map(m => m.IsAPM).Index(i); i++;
            Map(m => m.IsAnalytics).Index(i); i++;

            Map(m => m.IsAssignedToBTs).Index(i); i++;
            Map(m => m.NumAssignedBTs).Index(i); i++;
            Map(m => m.AssignedBTs).Index(i); i++;

            Map(m => m.RuleRawValue).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
        }
    }
}