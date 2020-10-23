using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class BSGOverheadResultMap : ClassMap<BSGOverheadResult>
    {
        public BSGOverheadResultMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.ApplicationID).Index(i); i++;

            Map(m => m.DeveloperModeEnabled).Index(i); i++;
            Map(m => m.FindEntryPointsEnabled).Index(i); i++;
            Map(m => m.SlowSnapshotCollectionEnabled).Index(i); i++;
        }
    }
}
