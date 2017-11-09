using System;

namespace AppDynamics.Dexter.DataObjects
{
    public class BusinessTransactionEntryScope
    {
        public string Controller { get; set; }
        public string ControllerLink { get; set; }

        public long ApplicationID { get; set; }
        public string ApplicationLink { get; set; }
        public string ApplicationName { get; set; }

        public string ScopeName { get; set; }
        public string ScopeType { get; set; }
        public string Description { get; set; }
        public int Version { get; set; }

        public string IncludedTiers { get; set; }
        public int NumTiers { get; set; }

        public string IncludedRules { get; set; }
        public int NumRules { get; set; }

        public override String ToString()
        {
            return String.Format(
                "BusinessTransactionEntryScope: {0}/{1} {2} {3}",
                this.Controller,
                this.ApplicationName,
                this.ScopeName,
                this.ScopeType);
        }
    }
}
