using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class EventReportMap : ClassMap<Event>
    {
        public EventReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;

            Map(m => m.EventID).Index(i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.Occurred), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.OccurredUtc), i); i++;
            Map(m => m.Summary).Index(i); i++;
            Map(m => m.NumDetails).Index(i); i++;
            Map(m => m.Type).Index(i); i++;
            Map(m => m.SubType).Index(i); i++;
            Map(m => m.Severity).Index(i); i++;

            Map(m => m.TierName).Index(i); i++;
            Map(m => m.NodeName).Index(i); i++;
            Map(m => m.MachineName).Index(i); i++;
            Map(m => m.BTName).Index(i); i++;

            Map(m => m.TriggeredEntityType).Index(i); i++;
            Map(m => m.TriggeredEntityName).Index(i); i++;
            Map(m => m.TriggeredEntityID).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.TierID).Index(i); i++;
            Map(m => m.NodeID).Index(i); i++;
            Map(m => m.MachineID).Index(i); i++;
            Map(m => m.BTID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
            Map(m => m.TierLink).Index(i); i++;
            Map(m => m.NodeLink).Index(i); i++;
            Map(m => m.BTLink).Index(i); i++;
            Map(m => m.EventLink).Index(i); i++;
        }
    }
}