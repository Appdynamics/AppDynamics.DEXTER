using CsvHelper.Configuration;

namespace AppDynamics.OfflineData.ReportObjects
{
    public class ReportBusinessTransactionRowMap : CsvClassMap<ReportBusinessTransactionRow>
    {
        public ReportBusinessTransactionRowMap()
        {
            int i = 0; 
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.TierName).Index(i); i++;
            Map(m => m.TierID).Index(i); i++;
            Map(m => m.BTName).Index(i); i++;
            Map(m => m.BTType).Index(i); i++;
            Map(m => m.BTID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
            Map(m => m.TierLink).Index(i); i++;
            Map(m => m.BTLink).Index(i); i++;
        }
    }
}
