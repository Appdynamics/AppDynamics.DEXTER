using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class MethodCallLine
    {
        public string Controller { get; set; }

        public long ApplicationID { get; set; }
        public string ApplicationName { get; set; }

        public long TierID { get; set; }
        public string TierName { get; set; }
        public string TierType { get; set; }

        public long NodeID { get; set; }
        public string NodeName { get; set; }
        public string AgentType { get; set; }

        public long BTID { get; set; }
        public string BTName { get; set; }
        public string BTType { get; set; }

        public string RequestID { get; set; }
        public long SegmentID { get; set; }

        public DateTime Occurred { get; set; }
        public DateTime OccurredUtc { get; set; }

        public string SegmentUserExperience { get; set; }
        public string SnapshotUserExperience { get; set; }

        public string Type { get; set; }
        public string Framework { get; set; }
        public string FullNameIndent { get; set; }
        public string FullName { get; set; }
        public string PrettyName { get; set; }
        public string Class { get; set; }
        public string Method { get; set; }
        public int LineNumber { get; set; }

        public long Exec { get; set; }
        public long ExecTotal { get; set; }
        public long ExecToHere { get; set; }
        public long Wait { get; set; }
        public long WaitTotal { get; set; }
        public long Block { get; set; }
        public long BlockTotal { get; set; }
        public long CPU { get; set; }
        public long CPUTotal { get; set; }
        public bool ExecAdjusted { get; set; }

        public string ExecRange { get; set; }

        public int Depth { get; set; }
        public MethodCallLineElementType ElementType { get; set; }
        public int NumChildren { get; set; }

        public string SEPs { get; set; }
        public int NumSEPs { get; set; }

        public string ExitCalls { get; set; }
        public int NumExits { get; set; }
        public bool HasErrors { get; set; }

        public string MIDCs { get; set; }
        public int NumMIDCs { get; set; }

        public int SequenceNumber { get; set; }
        public int NumCalls { get; set; }

        public MethodCallLine Parent { get; set; }

        public MethodCallLine Clone()
        {
            return (MethodCallLine)this.MemberwiseClone();
        }

        public override String ToString()
        {
            return String.Format(
                "MethodCallLine: {0}, {1} ms, {2} deep, {3} exits, #{4}",
                this.FullName,
                this.Exec,
                this.Depth,
                this.NumExits,
                this.SequenceNumber);
        }
    }
}
