using CsvHelper.Configuration;

namespace AppDynamics.Dexter.DataObjects
{
    public class FoldedCallStackReportMap : ClassMap<FoldedCallStack>
    {
        public FoldedCallStackReportMap()
        {
            int i = 0;
            Map(m => m.CallStack).Index(i); i++;
            Map(m => m.Count).Index(i); i++;
        }
    }
}