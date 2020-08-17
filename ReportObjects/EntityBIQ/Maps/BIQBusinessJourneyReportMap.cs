using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class BIQBusinessJourneyReportMap : ClassMap<BIQBusinessJourney>
    {
        public BIQBusinessJourneyReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;

            Map(m => m.JourneyName).Index(i); i++;
            Map(m => m.JourneyDescription).Index(i); i++;
            Map(m => m.State).Index(i); i++;
            Map(m => m.IsEnabled).Index(i); i++;
            Map(m => m.KeyField).Index(i); i++;

            Map(m => m.Stages).Index(i); i++;
            Map(m => m.NumStages).Index(i); i++;

            Map(m => m.CreatedBy).Index(i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.CreatedOn), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.CreatedOnUtc), i); i++;

            Map(m => m.UpdatedBy).Index(i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.UpdatedOn), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.UpdatedOnUtc), i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.JourneyID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
        }
    }
}