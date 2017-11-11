using CsvHelper.Configuration;

namespace AppDynamics.Dexter.DataObjects
{
    public class ApplicationEventReportMap: ClassMap<EntityApplication>
    {
        public ApplicationEventReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.NumEvents).Index(i); i++;
            Map(m => m.NumEventsInfo).Index(i); i++;
            Map(m => m.NumEventsWarning).Index(i); i++;
            Map(m => m.NumEventsError).Index(i); i++;
            Map(m => m.NumHRViolations).Index(i); i++;
            Map(m => m.NumHRViolationsWarning).Index(i); i++;
            Map(m => m.NumHRViolationsCritical).Index(i); i++;
            Map(m => m.HasActivity).Index(i); i++;
            Map(m => m.From).Index(i); i++;
            Map(m => m.To).Index(i); i++;
            Map(m => m.FromUtc).Index(i); i++;
            Map(m => m.ToUtc).Index(i); i++;
            Map(m => m.Duration).Index(i); i++;
            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.DetailLink).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
        }
    }
}