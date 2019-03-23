using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class ControllerApplicationReportMap: ClassMap<ControllerApplication>
    {
        public ControllerApplicationReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.Description).Index(i); i++;
            Map(m => m.Type).Index(i); i++;
            Map(m => m.Types).Index(i); i++;

            Map(m => m.CreatedBy).Index(i); i++;
            Map(m => m.CreatedOn).Index(i); i++;
            Map(m => m.CreatedOnUtc).Index(i); i++;

            Map(m => m.UpdatedBy).Index(i); i++;
            Map(m => m.UpdatedOn).Index(i); i++;
            Map(m => m.UpdatedOnUtc).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.ParentApplicationID).Index(i); i++;
        }
    }
}
