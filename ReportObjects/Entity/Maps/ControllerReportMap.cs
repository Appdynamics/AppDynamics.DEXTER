using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjects
{
    public class ControllerReportMap: ClassMap<Controller>
    {
        public ControllerReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.UserName).Index(i); i++;
            Map(m => m.Version).Index(i); i++;
            Map(m => m.VersionDetail).Index(i); i++;
            Map(m => m.NumApps).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
        }
    }
}
