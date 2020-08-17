using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class DetectedErrorReportMap : ClassMap<DetectedError>
    {
        public DetectedErrorReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.TierName).Index(i); i++;
            Map(m => m.TierType).Index(i); i++;
            Map(m => m.NodeName).Index(i); i++;
            Map(m => m.AgentType).Index(i); i++;
            Map(m => m.BTName).Index(i); i++;
            Map(m => m.BTType).Index(i); i++;

            Map(m => m.SegmentUserExperience).Index(i); i++;
            Map(m => m.SnapshotUserExperience).Index(i); i++;

            Map(m => m.RequestID).Index(i); i++;
            Map(m => m.SegmentID).Index(i); i++;

            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.Occurred), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.OccurredUtc), i); i++;

            Map(m => m.ErrorName).Index(i); i++;
            Map(m => m.ErrorType).Index(i); i++;

            Map(m => m.ErrorCategory).Index(i); i++;
            Map(m => m.ErrorMessage).Index(i); i++;
            Map(m => m.ErrorStack).Index(i); i++;
            Map(m => m.ErrorDetail).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.TierID).Index(i); i++;
            Map(m => m.NodeID).Index(i); i++;
            Map(m => m.BTID).Index(i); i++;
            Map(m => m.ErrorID).Index(i); i++;
        }
    }
}