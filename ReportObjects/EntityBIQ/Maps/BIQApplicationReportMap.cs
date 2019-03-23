using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class BIQApplicationReportMap : ClassMap<BIQApplication>
    {
        public BIQApplicationReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;

            Map(m => m.NumSearches).Index(i); i++;
            Map(m => m.NumMultiSearches).Index(i); i++;
            Map(m => m.NumSingleSearches).Index(i); i++;
            Map(m => m.NumLegacySearches).Index(i); i++;

            Map(m => m.NumSavedMetrics).Index(i); i++;

            Map(m => m.NumBusinessJourneys).Index(i); i++;

            Map(m => m.NumExperienceLevels).Index(i); i++;

            Map(m => m.NumSchemas).Index(i); i++;
            Map(m => m.NumFields).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
        }
    }
}
