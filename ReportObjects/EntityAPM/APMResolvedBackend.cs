using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class APMResolvedBackend
    {
        public string Controller { get; set; }

        public long ApplicationID { get; set; }
        public string ApplicationName { get; set; }

        public long TierID { get; set; }
        public string TierName { get; set; }

        public long NodeID { get; set; }
        public string NodeName { get; set; }

        public long BackendID { get; set; }
        public string BackendName { get; set; }

        public string BackendType { get; set; }

        public DateTime CreatedOn { get; set; }
        public DateTime CreatedOnUtc { get; set; }

        public int? NumProps { get; set; }
        public string Prop1Name { get; set; }
        public string Prop1Value { get; set; }
        public string Prop2Name { get; set; }
        public string Prop2Value { get; set; }
        public string Prop3Name { get; set; }
        public string Prop3Value { get; set; }
        public string Prop4Name { get; set; }
        public string Prop4Value { get; set; }
        public string Prop5Name { get; set; }
        public string Prop5Value { get; set; }
        public string Prop6Name { get; set; }
        public string Prop6Value { get; set; }
        public string Prop7Name { get; set; }
        public string Prop7Value { get; set; }

        public override String ToString()
        {
            return String.Format(
                "APMResolvedBackend:  {0}/{1}({2})/{3}({4})={5}({6})",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID, 
                this.BackendName,
                this.BackendID,
                this.TierName,
                this.TierID);
        }
    }
}
