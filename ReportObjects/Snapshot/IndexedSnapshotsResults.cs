using System;
using System.Collections.Generic;

namespace AppDynamics.Dexter.ReportObjects
{
    public class IndexedSnapshotsResults
    {
        private const int AVERAGE_NUM_SEGMENTS = 2;

        public IndexedSnapshotsResults(int numSnapshots)
        {
            // Number of Snapshots is known
            this.Snapshots = new List<Snapshot>(numSnapshots);
            // Assume that each Snapshot has at least 2 segments
            this.Segments = new List<Segment>(numSnapshots * AVERAGE_NUM_SEGMENTS);
            // Eyeball each segment to have 3 exits on average
            this.ExitCalls = new List<ExitCall>(numSnapshots * AVERAGE_NUM_SEGMENTS * 3);
            // Let's assume that each segment has a SEP
            this.ServiceEndpointCalls = new List<ServiceEndpointCall>(numSnapshots * AVERAGE_NUM_SEGMENTS);
            // Let's assume that at least 10% of segments contain an error
            this.DetectedErrors = new List<DetectedError>(numSnapshots * AVERAGE_NUM_SEGMENTS / 10);
            // Let's assume that each segment has 3 business data pieces
            this.BusinessData = new List<BusinessData>(numSnapshots * AVERAGE_NUM_SEGMENTS * 3);
            // Assume each call graph is 250 items long
            this.MethodCallLines = new List<MethodCallLine>(numSnapshots * AVERAGE_NUM_SEGMENTS * 250);
            // Assume 100 nodes
            this.FoldedCallStacksNodesNoTiming = new Dictionary<long, Dictionary<string, FoldedStackLine>>(100);
            this.FoldedCallStacksNodesWithTiming = new Dictionary<long, Dictionary<string, FoldedStackLine>>(100);
            // Assume 200 business transactions
            this.FoldedCallStacksBusinessTransactionsNoTiming = new Dictionary<long, Dictionary<string, FoldedStackLine>>(200);
            this.FoldedCallStacksBusinessTransactionsWithTiming = new Dictionary<long, Dictionary<string, FoldedStackLine>>(200);
        }

        public List<Snapshot> Snapshots { get; set; }
        public List<Segment> Segments { get; set; }
        public List<ExitCall> ExitCalls { get; set; }
        public List<ServiceEndpointCall> ServiceEndpointCalls { get; set; }
        public List<DetectedError> DetectedErrors { get; set; }
        public List<BusinessData> BusinessData { get; set; }
        public List<MethodCallLine> MethodCallLines { get; set; }
        public Dictionary<long, Dictionary<string, FoldedStackLine>> FoldedCallStacksNodesNoTiming { get; set; }
        public Dictionary<long, Dictionary<string, FoldedStackLine>> FoldedCallStacksNodesWithTiming { get; set; }
        public Dictionary<long, Dictionary<string, FoldedStackLine>> FoldedCallStacksBusinessTransactionsNoTiming { get; set; }
        public Dictionary<long, Dictionary<string, FoldedStackLine>> FoldedCallStacksBusinessTransactionsWithTiming { get; set; }

        public override string ToString()
        {
            return String.Format("IndexedSnapshotsResults: {0} Snapshots", this.Snapshots.Count);
        }
    }
}
