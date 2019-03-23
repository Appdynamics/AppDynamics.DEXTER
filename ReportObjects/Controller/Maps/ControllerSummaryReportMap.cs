using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class ControllerSummaryReportMap: ClassMap<ControllerSummary>
    {
        public ControllerSummaryReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.Version).Index(i); i++;
            Map(m => m.VersionDetail).Index(i); i++;
            Map(m => m.NumApps).Index(i); i++;
            Map(m => m.NumAPMApps).Index(i); i++;
            Map(m => m.NumWEBApps).Index(i); i++;
            Map(m => m.NumMOBILEApps).Index(i); i++;
            Map(m => m.NumIOTApps).Index(i); i++;
            Map(m => m.NumSIMApps).Index(i); i++;
            Map(m => m.NumBIQApps).Index(i); i++;
            Map(m => m.NumDBApps).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
        }
    }
}
