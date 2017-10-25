using CsvHelper.Configuration;

namespace AppDynamics.Dexter.DataObjects
{
    public class ControllerSettingReportMap : CsvClassMap<ControllerSetting>
    {
        public ControllerSettingReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.Name).Index(i); i++;
            Map(m => m.Value).Index(i); i++;
            Map(m => m.Description).Index(i); i++;
            Map(m => m.Scope).Index(i); i++;
            Map(m => m.Updateable).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
        }
    }
}