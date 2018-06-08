using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjects
{
    public class MethodCallLineClassTypeMappingReportMap : ClassMap<MethodCallLineClassTypeMapping>
    {
        public MethodCallLineClassTypeMappingReportMap()
        {
            int i = 0;
            Map(m => m.ClassPrefix).Index(i); i++;
            Map(m => m.FrameworkType).Index(i); i++;
            Map(m => m.FlameGraphColorStart).Index(i); i++;
            Map(m => m.FlameGraphColorEnd).Index(i); i++;
        }
    }
}