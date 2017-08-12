using CsvHelper.Configuration;

namespace AppDynamics.Dexter.DataObjects
{
    public class ErrorEntityReportMap : CsvClassMap<EntityError>
    {
        public ErrorEntityReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.TierName).Index(i); i++;
            Map(m => m.ErrorName).Index(i); i++;
            Map(m => m.ErrorType).Index(i); i++;
            Map(m => m.HttpCode).Index(i); i++;
            Map(m => m.ErrorDepth).Index(i); i++;
            Map(m => m.ErrorLevel1).Index(i); i++;
            Map(m => m.ErrorLevel2).Index(i); i++;
            Map(m => m.ErrorLevel3).Index(i); i++;
            Map(m => m.ErrorLevel4).Index(i); i++;
            Map(m => m.ErrorLevel5).Index(i); i++;
            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.TierID).Index(i); i++;
            Map(m => m.ErrorID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
            Map(m => m.TierLink).Index(i); i++;
            Map(m => m.ErrorLink).Index(i); i++;
        }
    }
}
