using CsvHelper.Configuration;

namespace AppDynamics.Dexter.DataObjects
{
    public class BusinessTransactionEntityReportMap : CsvClassMap<EntityBusinessTransaction>
    {
        public BusinessTransactionEntityReportMap()
        {
            int i = 0; 
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.TierName).Index(i); i++;
            Map(m => m.BTName).Index(i); i++;
            Map(m => m.BTNameOriginal).Index(i); i++;
            Map(m => m.IsRenamed).Index(i); i++;
            Map(m => m.BTType).Index(i); i++;
            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.TierID).Index(i); i++;
            Map(m => m.BTID).Index(i); i++;
            Map(m => m.DetailLink).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
            Map(m => m.TierLink).Index(i); i++;
            Map(m => m.BTLink).Index(i); i++;
        }
    }
}
