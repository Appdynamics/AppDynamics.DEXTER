using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AppDynamics.Dexter.ReportObjects
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

        public string[] ExitCallsArray { get; set; }

        public string ExitCalls
        {
            get
            {
                return String.Join(";", this.ExitCallsArray);
            }
            set
            {
                string[] exitCallsArray = value.Split(';');
                this.ExitCallsArray = new string[exitCallsArray.Length];
                for (int i = 0; i < exitCallsArray.Length; i++)
                {
                    this.ExitCallsArray[i] = exitCallsArray[i];
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

            // Allocate the array for the timings
            int numberOfSpecialSlotsForTime = 0;
            if (includeTimeOfEvent == true) numberOfSpecialSlotsForTime = 3;
            this.StackTimingArray = new long[methodCallLine.Depth + 1 + numberOfSpecialSlotsForTime];

            // Allocate some characters for each exit call, assume only 1% of call stack will have exits
            this.ExitCallsArray = new string[methodCallLine.Depth + 1 + numberOfSpecialSlotsForTime];
            for (int i = 0; i < this.ExitCallsArray.Length; i++)
            {
                this.ExitCallsArray[i] = String.Empty;
            }

            // Walk from the leaf up to the parent, building the flattened stack with frames separated by ;
            while (methodCallLine != null)
            {
                // Add stack frame to the beginning of the string
                if (sbFoldedCallStack.Length > 0) sbFoldedCallStack.Insert(0, ";");
                sbFoldedCallStack.Insert(0, methodCallLine.FullName);

                // Add method timing to the array
                this.StackTimingArray[methodCallLine.Depth + numberOfSpecialSlotsForTime] = methodCallLine.Exec;

                // Add exit calls to the array
                if (methodCallLine.NumExits > 0)
                {
                    // Encode the exits because they may contain carriage returns
                    string exitCallsWithTiming = methodCallLine.ExitCalls;

                    Regex regexDuration = new Regex(@"\[\d+ ms( async)?\]", RegexOptions.IgnoreCase);
                    exitCallsWithTiming = regexDuration.Replace(exitCallsWithTiming, "[## ms]");

                    Regex regexSegment = new Regex(@"\/\d+\/ ", RegexOptions.IgnoreCase);
                    exitCallsWithTiming = regexSegment.Replace(exitCallsWithTiming, "");

                    this.ExitCallsArray[methodCallLine.Depth + numberOfSpecialSlotsForTime] = Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes(exitCallsWithTiming));
                }

                // Move to parent
                methodCallLine = methodCallLine.Parent;
            }

            if (includeTimeOfEvent == true)
            {
                // Add synthetic frames for Flame Chart
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
            // Add number of samples
            this.NumSamples = this.NumSamples + foldedStackLineToAdd.NumSamples;

            // Add timings for each of the folded stack sections
            for (int i = 0; i < this.StackTimingArray.Length; i++)
            {
                if (i < foldedStackLineToAdd.StackTimingArray.Length)
                {
                    this.StackTimingArray[i] = this.StackTimingArray[i] + foldedStackLineToAdd.StackTimingArray[i];
                }
            }

            // Add exit calls from each of the frames
            for (int i = 0; i < this.ExitCallsArray.Length; i++)
            {
                if (i < foldedStackLineToAdd.ExitCallsArray.Length)
                {
                    if (this.ExitCallsArray[i] != null &&
                        this.ExitCallsArray[i].Length > 0 &&
                        foldedStackLineToAdd.ExitCallsArray[i] != null &&
                        foldedStackLineToAdd.ExitCallsArray[i].Length > 0)
                    {
                        // Append two exit calls by decoding them, squishing them together, and then reencoding them
                        string exitsInThisFoldedStack = Encoding.UTF8.GetString(Convert.FromBase64String(this.ExitCallsArray[i]));

                        // Only append if the exit size hasn't grown larger then what we want to see
                        if (exitsInThisFoldedStack.Length < 400)
                        {
                            string exitsInIncomingFoldedStack = Encoding.UTF8.GetString(Convert.FromBase64String(foldedStackLineToAdd.ExitCallsArray[i]));

                            List<string> allExits = new List<string>();
                            allExits.AddRange(exitsInThisFoldedStack.Split('\n').ToList());
                            allExits.AddRange(exitsInIncomingFoldedStack.Split('\n').ToList());

                            // Only append unique values
                            List<string> uniqueExits = allExits.Distinct().ToList();
                            this.ExitCallsArray[i] = Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes(String.Join("\n", uniqueExits)));
                        }
                    }
                    else if (
                        this.ExitCallsArray[i] != null &&
                        this.ExitCallsArray[i].Length > 0 &&
                        (foldedStackLineToAdd.ExitCallsArray[i] == null || foldedStackLineToAdd.ExitCallsArray[i].Length == 0))
                    {
                        // Do nothing, nothing to add from the incoming folded stack
                    }
                    else if (
                        (this.ExitCallsArray[i] == null || this.ExitCallsArray[i].Length == 0) &&
                        foldedStackLineToAdd.ExitCallsArray[i] != null &&
                        foldedStackLineToAdd.ExitCallsArray[i].Length > 0)
                    {
                        // Move the target exit call into here
                        this.ExitCallsArray[i] = foldedStackLineToAdd.ExitCallsArray[i];
                    }
                    else if (
                        (this.ExitCallsArray[i] == null || this.ExitCallsArray[i].Length == 0) &&
                        (foldedStackLineToAdd.ExitCallsArray[i] == null || foldedStackLineToAdd.ExitCallsArray[i].Length == 0))
                    {
                        // Do nothing, nothing to join
                    }
                }
            }
        }

        public override String ToString()
        {
            return String.Format(
                "{0} {1} {2}",
                this.NumSamples,
                this.StackTiming,
                this.FoldedStack);
        }

        public FoldedStackLine Clone()
        {
            FoldedStackLine foldedStackLineClone = (FoldedStackLine)this.MemberwiseClone();
            foldedStackLineClone.StackTimingArray = new long[this.StackTimingArray.Length];
            this.StackTimingArray.CopyTo(foldedStackLineClone.StackTimingArray, 0);
            this.ExitCallsArray.CopyTo(foldedStackLineClone.ExitCallsArray, 0);
            return foldedStackLineClone;
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
