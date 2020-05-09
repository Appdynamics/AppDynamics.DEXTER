using System;

namespace AppDynamics.Dexter
{
    public class CompareTimeRange
    {
        public int ReferenceSkip { get; set; }
        public int ReferenceCompareRanges { get; set; }
        public int DifferenceSkipRanges { get; set; }
        public int DifferenceConsiderRanges { get; set; }
    }
}
