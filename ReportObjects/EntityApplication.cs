using System;

namespace AppDynamics.OfflineData.ReportObjects
{
    public class EntityApplication : EntityBase
    {
        public int NumBackends { get; set; }
        public int NumBTs { get; set; }
        public int NumErrors { get; set; }
        public int NumNodes { get; set; }
        public int NumSEPs { get; set; }
        public int NumTiers { get; set; }

        public int? NumEntryRules { get; set; }
        public int? NumExitRules { get; set; }
        public int? NumAgentProps { get; set; }
        public int? NumHealthRules { get; set; }
        public int? NumErrorRules { get; set; }
        public int? NumHTTPDCs { get; set; }
        public int? NumMIDCs { get; set; }

        public EntityApplication Clone()
        {
            return (EntityApplication)this.MemberwiseClone();
        }

        public override String ToString()
        {
            return String.Format(
                "EntityApplication: {0}/{1}({2})",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID);
        }
    }
}
