using System;

namespace AppDynamics.Dexter.DataObjects
{
    public class ExitCall
    {
        public string Controller { get; set; }
        public string ControllerLink { get; set; }

        public long ApplicationID { get; set; }
        public string ApplicationLink { get; set; }
        public string ApplicationName { get; set; }

        public long TierID { get; set; }
        public string TierLink { get; set; }
        public string TierName { get; set; }

        public long NodeID { get; set; }
        public string NodeLink { get; set; }
        public string NodeName { get; set; }

        public long BTID { get; set; }
        public string BTLink { get; set; }
        public string BTName { get; set; }

        public long ToEntityID { get; set; }
        public string ToLink { get; set; }
        public string ToEntityName { get; set; }
        public string ToEntityType { get; set; }
        public long ToSegmentID { get; set; }

        public DateTime Occurred { get; set; }
        public DateTime OccurredUtc { get; set; }

        public string RequestID { get; set; }
        public long SegmentID { get; set; }
        public string SequenceNumber { get; set; }

        public string ExitType { get; set; }
        public string Detail { get; set; }
        public string Framework { get; set; }
        public string Method { get; set; }
        public bool IsAsync { get; set; }

        public long Duration { get; set; }
        public string DurationRange { get; set; }

        public string CallChain { get; set; }

        public bool HasErrors { get; set; }
        public string ErrorDetail { get; set; }

        public string PropsAll { get; set; }
        public string PropAsync { get; set; }
        public string PropContinuation { get; set; }
        public string PropName { get; set; }
        public string PropQueryType { get; set; }
        public string PropStatementType { get; set; }
        public string PropURL { get; set; }
        public string PropServiceName { get; set; }
        public string PropOperationName { get; set; }
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
        public int NumProps { get; set; }

        public int NumCalls { get; set; }
        public int NumErrors { get; set; }

        public string SegmentLink { get; set; }
        public override String ToString()
        {
            return String.Format(
                "ExitCall: {0}:{1}->{2}:{3}/{4}/{5}",
                this.RequestID,
                this.SegmentID,
                this.ToEntityName,
                this.ToEntityType,
                this.ExitType,
                this.Detail);
        }
    }
}
