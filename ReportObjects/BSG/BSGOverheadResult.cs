using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class BSGOverheadResult
    {
        public string Controller { get; set; }

        public string ApplicationName { get; set; }
        public long ApplicationID { get; set; }

        public bool DeveloperModeEnabled { get; set; }
        public bool FindEntryPointsEnabled { get; set; }
        public bool SlowSnapshotCollectionEnabled { get; set; }


        public BSGOverheadResult Clone()
        {
            return (BSGOverheadResult) this.MemberwiseClone();
        }

        public override String ToString()
        {
            return String.Format(
                "BSGOverhead:  {0}/{1}({2})/{3}",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID,
                this.DeveloperModeEnabled);
        }
    }
}