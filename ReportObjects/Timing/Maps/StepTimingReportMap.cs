using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class StepTimingReportMap : ClassMap<StepTiming>
    {
        public StepTimingReportMap()
        {
            int i = 0;
            Map(m => m.StepName).Index(i); i++;
            Map(m => m.StepID).Index(i); i++;

            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.NumEntities).Index(i); i++;

            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.StartTime), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.EndTime), i); i++;
            Map(m => m.Duration).Index(i); i++;
            Map(m => m.DurationMS).Index(i); i++;

            Map(m => m.JobFileName).Index(i); i++;
        }
    }
}