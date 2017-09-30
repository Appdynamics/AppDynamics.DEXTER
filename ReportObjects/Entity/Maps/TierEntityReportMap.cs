using CsvHelper.Configuration;

namespace AppDynamics.Dexter.DataObjects
{
    public class TierEntityReportMap : CsvClassMap<EntityTier>
    {
        public TierEntityReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.TierName).Index(i); i++;
            Map(m => m.TierType).Index(i); i++;
            Map(m => m.AgentType).Index(i); i++;
            Map(m => m.NumNodes).Index(i); i++;
            Map(m => m.NumBTs).Index(i); i++;
            Map(m => m.NumSEPs).Index(i); i++;
            Map(m => m.NumErrors).Index(i); i++;
            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.TierID).Index(i); i++;
            Map(m => m.DetailLink).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
            Map(m => m.TierLink).Index(i); i++;
        }
    }
}
