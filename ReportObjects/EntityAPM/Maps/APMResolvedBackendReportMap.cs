using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class APMResolvedBackendReportMap : ClassMap<APMResolvedBackend>
    {
        public APMResolvedBackendReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.TierName).Index(i); i++;
            Map(m => m.NodeName).Index(i); i++;
            Map(m => m.BackendName).Index(i); i++;
            Map(m => m.BackendType).Index(i); i++;

            Map(m => m.NumProps).Index(i); i++;
            Map(m => m.Prop1Name).Index(i); i++;
            Map(m => m.Prop1Value).Index(i); i++;
            Map(m => m.Prop2Name).Index(i); i++;
            Map(m => m.Prop2Value).Index(i); i++;
            Map(m => m.Prop3Name).Index(i); i++;
            Map(m => m.Prop3Value).Index(i); i++;
            Map(m => m.Prop4Name).Index(i); i++;
            Map(m => m.Prop4Value).Index(i); i++;
            Map(m => m.Prop5Name).Index(i); i++;
            Map(m => m.Prop5Value).Index(i); i++;
            Map(m => m.Prop6Name).Index(i); i++;
            Map(m => m.Prop6Value).Index(i); i++;
            Map(m => m.Prop7Name).Index(i); i++;
            Map(m => m.Prop7Value).Index(i); i++;

            Map(m => m.CreatedOn).Index(i); i++;
            Map(m => m.CreatedOnUtc).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.TierID).Index(i); i++;
            Map(m => m.NodeID).Index(i); i++;
            Map(m => m.BackendID).Index(i); i++;
        }
    }
}
