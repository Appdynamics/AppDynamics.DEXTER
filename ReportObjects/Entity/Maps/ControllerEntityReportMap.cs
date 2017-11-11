using CsvHelper.Configuration;

namespace AppDynamics.Dexter.DataObjects
{
    public class ControllerEntityReportMap: ClassMap<EntityController>
    {
        public ControllerEntityReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.UserName).Index(i); i++;
            Map(m => m.Version).Index(i); i++;
            Map(m => m.NumApps).Index(i); i++;
            //Map(m => m.NumSettings).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
        }
    }
}
