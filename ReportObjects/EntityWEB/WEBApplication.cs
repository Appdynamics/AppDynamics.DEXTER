using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class WEBApplication : WEBEntityBase
    {
        public const string ENTITY_TYPE = "WEBApplication";
        public const string ENTITY_FOLDER = "WEBAPP";

        public int NumPages { get; set; }
        public int NumAJAXRequests { get; set; }
        public int NumVirtualPages { get; set; }
        public int NumIFrames { get; set; }

        public int NumActivity { get; set; }
        public int NumNoActivity { get; set; }

        public int NumRealGeoLocations { get; set; }
        public int NumRealGeoLocationsRegion { get; set; }

        public int NumSynthGeoLocations { get; set; }
        public int NumSynthGeoLocationsRegion { get; set; }

        public override String ToString()
        {
            return String.Format(
                "WEBApplication: {0}/{1}({2})",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID);
        }
    }
}
