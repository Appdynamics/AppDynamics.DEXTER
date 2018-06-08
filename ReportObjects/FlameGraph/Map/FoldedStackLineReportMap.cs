using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjects
{
    public class FoldedStackLineReportMap : ClassMap<FoldedStackLine>
    {
        public FoldedStackLineReportMap()
        {
            int i = 0;
            Map(m => m.FoldedStack).Index(i); i++;
            Map(m => m.NumSamples).Index(i); i++;
            Map(m => m.StackTiming).Index(i); i++;
        }
    }
}