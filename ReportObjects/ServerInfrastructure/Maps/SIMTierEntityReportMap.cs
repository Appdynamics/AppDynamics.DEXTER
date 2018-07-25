using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjects
{
    public class SIMTierEntityReportMap : ClassMap<EntitySIMTier>
    {
        public SIMTierEntityReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.TierName).Index(i); i++;
            Map(m => m.NumSegments).Index(i); i++;
            Map(m => m.NumNodes).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.TierID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
        }
    }
}
