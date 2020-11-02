using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class BSGDataCollectorResult
    {
        public string Controller { get; set; }

        public string ApplicationName { get; set; }
        public long ApplicationID { get; set; }

        public int NumHTTPDCVariablesCollected { get; set; }
        public int NumHTTPDCs { get; set; }
        public int NumMIDCVariablesCollected { get; set; }
        public int NumMIDCs { get; set; }
        public int NumHTTPDCs_BIQEnabled { get; set; }
        public int NumMIDCs_BIQEnabled { get; set; }


        public BSGDataCollectorResult Clone()
        {
            return (BSGDataCollectorResult) this.MemberwiseClone();
        }

        public override String ToString()
        {
            return String.Format(
                "BSGDataCollector:  {0}/{1}({2})/{3}",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID,
                this.NumMIDCs+this.NumHTTPDCs);
        }
    }
}