using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class EventDetailReportMap : ClassMap<EventDetail>
    {
        public EventDetailReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;

            Map(m => m.EventID).Index(i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.Occurred), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.OccurredUtc), i); i++;
            Map(m => m.Summary).Index(i); i++;
            Map(m => m.Type).Index(i); i++;
            Map(m => m.SubType).Index(i); i++;
            Map(m => m.Severity).Index(i); i++;

            Map(m => m.DetailAction).Index(i); i++;
            Map(m => m.DetailName).Index(i); i++;
            Map(m => m.DetailValue).Index(i); i++;
            Map(m => m.DetailValueOld).Index(i); i++;
            Map(m => m.DataType).Index(i); i++;

            Map(m => m.TierName).Index(i); i++;
            Map(m => m.NodeName).Index(i); i++;
            Map(m => m.MachineName).Index(i); i++;
            Map(m => m.BTName).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.TierID).Index(i); i++;
            Map(m => m.NodeID).Index(i); i++;
            Map(m => m.MachineID).Index(i); i++;
            Map(m => m.BTID).Index(i); i++;
        }
    }
}