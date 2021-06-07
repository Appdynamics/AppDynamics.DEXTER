using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class BSGDataCollectorResultMap : ClassMap<BSGDataCollectorResult>
    {
        public BSGDataCollectorResultMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.ApplicationID).Index(i); i++;

            Map(m => m.NumHTTPDCs).Index(i); i++;
            Map(m => m.NumHTTPDCVariablesCollected).Index(i); i++;
            Map(m => m.NumHTTPDCs_BIQEnabled).Index(i); i++;

            Map(m => m.NumMIDCs).Index(i); i++;
            Map(m => m.NumMIDCVariablesCollected).Index(i); i++;
            Map(m => m.NumMIDCs_BIQEnabled).Index(i); i++;
        }
    }
}
