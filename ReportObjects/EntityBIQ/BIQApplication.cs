using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class BIQApplication : BIQEntityBase
    {
        public int NumSearches { get; set; }
        public int NumSingleSearches { get; set; }
        public int NumMultiSearches { get; set; }
        public int NumLegacySearches { get; set; }

        public int NumSavedMetrics{ get; set; }

        public int NumBusinessJourneys { get; set; }

        public int NumExperienceLevels { get; set; }

        public int NumSchemas { get; set; }
        public int NumFields { get; set; }

        public override String ToString()
        {
            return String.Format(
                "BIQApplication: {0}/{1}({2})",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID);
        }
    }
}
