using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class WEBGeoLocation : WEBEntityBase
    {
        public string GeoCode { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public string LocationType { get; set; }
        public string LocationName { get; set; }

        public string Country { get; set; }
        public string Region { get; set; }
        public string City { get; set; }

        public string ARTRange { get; set; }
        public long ART { get; set; }
        public long Calls { get; set; }
        public long CPM { get; set; }
        public long DOMReady { get; set; }
        public long FirstByte { get; set; }
        public long ServerConnection { get; set; }
        public long HTMLDownload { get; set; }
        public long DOMBuild { get; set; }
        public long ResourceFetch { get; set; }

        public long JSErrors { get; set; }
        public long JSEPM { get; set; }
        public long AJAXEPM { get; set; }

        public bool HasActivity { get; set; }

        public override String ToString()
        {
            return String.Format(
                "WEBGeoLocation: {0}/{1}({2}) {3}[{4}]",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID,
                this.LocationName,
                this.LocationType);
        }
    }
}
