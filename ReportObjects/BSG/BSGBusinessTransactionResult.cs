using System;
namespace AppDynamics.Dexter.ReportObjects
{
    public class BSGBusinessTransactionResult
    {
        public string Controller { get; set; }

        public string ApplicationName { get; set; }

        public int NumBTs { get; set; }
        public int NumBTsWithLoad { get; set; }
        public int NumBTsOverflow { get; set; }

        public int NumBTCustomEntryRules{ get; set; }
        public int NumBTCustomExcludeRules { get; set; }
        public int NumBTScopes { get; set; }
        public int NumBTEntryRules { get; set; }
        public int NumBTExcludeRules { get; set; }
        public int NumBTDiscoveryRules { get; set; }

        public int NumBT20CustomEntryRules { get; set; }
        public int NumBT20CustomExcludeRules { get; set; }

        public int NumBT20Scopes { get; set; }
        public int NumBT20EntryRules { get; set; }
        public int NumBT20ExcludeRules { get; set; }
        public int NumBT20DiscoveryRules { get; set; }


        public bool BTLockdownEnabled { get; set; }
        public bool BTCleanupEnabled { get; set; }
        public bool BTLimitExceeded { get; set; }



        public BSGBusinessTransactionResult Clone()
        {
            return (BSGBusinessTransactionResult)this.MemberwiseClone();
        }

        public override String ToString()
        {
            return String.Format(
                "BSGBusinessTransactionResult: {0}/{1}:({2})",
                this.Controller,
                this.ApplicationName,
                this.NumBTs);
        }
    }
}

