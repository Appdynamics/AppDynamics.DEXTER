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

        public FoldedStackLine(MethodCallLine methodCallLine, bool includeTimeOfEvent)
        {
            MethodCallLine methodCallLineOriginal = methodCallLine;

            // Allocate some characters per class/method pair
            StringBuilder sbFoldedCallStack = new StringBuilder(128 * methodCallLine.Depth + 1);

            int specialSlotForTime = 0;
            if (includeTimeOfEvent == true) specialSlotForTime = 3;

            // Allocate the array for the timings
            this.StackTimingArray = new long[methodCallLine.Depth + 1 + specialSlotForTime];

            // Walk from the leaf up to the parent, building the flattened stack with frames separated by ;
            while (methodCallLine != null)
            {
                if (sbFoldedCallStack.Length > 0) sbFoldedCallStack.Insert(0, ";");
                sbFoldedCallStack.Insert(0, methodCallLine.FullName);
                this.StackTimingArray[methodCallLine.Depth + specialSlotForTime] = methodCallLine.Exec;
                methodCallLine = methodCallLine.Parent;
            }
            if (includeTimeOfEvent == true)
            {
                if (sbFoldedCallStack.Length > 0) sbFoldedCallStack.Insert(0, ";");
                sbFoldedCallStack.Insert(0, 
                    String.Format("{0:yyyyMMddHH};{1};{2:00}", 
                    methodCallLineOriginal.Occurred, 
                    get10MinuteRange(methodCallLineOriginal.Occurred.Minute),
                    methodCallLineOriginal.Occurred.Minute));
                this.StackTimingArray[0] = 0;
                this.StackTimingArray[1] = 0;
                this.StackTimingArray[2] = 0;
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

        private string get10MinuteRange(int minute)
        {
            if (minute < 10)
            {
                return "00";
            }
            else if (minute < 20)
            {
                return "10";
            }
            else if (minute < 30)
            {
                return "20";
            }
            else if (minute < 40)
            {
                return "30";
            }
            else if (minute < 50)
            {
                return "40";
            }
            else
            {
                return "50";
            }
        }
    }
}
