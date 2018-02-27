using System;
using System.Text;

namespace AppDynamics.Dexter.DataObjects
{
    public class FoldedStackLine
    {
        public string FoldedStack { get; set; }
        public int NumSamples { get; set; }
        public long[] StackTimingArray { get; set; }
        public string StackTiming
        {
            get
            {
                return String.Join(";", this.StackTimingArray);
            }
            set
            {
                string[] stackTimingArray = value.Split(';');
                this.StackTimingArray = new long[stackTimingArray.Length];
                for (int i = 0; i < stackTimingArray.Length; i++)
                {
                    Int64.TryParse(stackTimingArray[i], out this.StackTimingArray[i]);
                }
            }
        }

        public FoldedStackLine()
        {
        }

        public FoldedStackLine(MethodCallLine methodCallLine)
        {
            // Allocate some characters per class/method pair
            StringBuilder sbFoldedCallStack = new StringBuilder(128 * methodCallLine.Depth + 1);

            // Allocate the array for the timings
            this.StackTimingArray = new long[methodCallLine.Depth + 1];

            // Walk from the leaf up to the parent, building the flattened stack with frames separated by ;
            while (methodCallLine != null)
            {
                if (sbFoldedCallStack.Length > 0) sbFoldedCallStack.Insert(0, ";");
                sbFoldedCallStack.Insert(0, methodCallLine.FullName);
                this.StackTimingArray[methodCallLine.Depth] = methodCallLine.Exec;
                methodCallLine = methodCallLine.Parent;
            }

            this.FoldedStack = sbFoldedCallStack.ToString();
            this.NumSamples = 1;
        }

        public void AddFoldedStackLine(FoldedStackLine foldedStackLineToAdd)
        {
            this.NumSamples = this.NumSamples + foldedStackLineToAdd.NumSamples;
            for (int i = 0; i < this.StackTimingArray.Length; i++)
            {
                if (i < foldedStackLineToAdd.StackTiming.Length)
                {
                    this.StackTimingArray[i] = this.StackTimingArray[i] + foldedStackLineToAdd.StackTimingArray[i];
                }
            }
        }

        public override String ToString()
        {
            return String.Format(
                "{0} {1} {2}",                
                this.NumSamples,
                String.Join(";", this.StackTiming),
                this.FoldedStack);
        }
    }
}
