using System;

namespace AppDynamics.Dexter
{
    public class TimeFrame
    {
        public string MarkDate { get; set; }
        public string MarkTime { get; set; }
        public string Duration { get; set; }
        
        public override string ToString()
        {
            return String.Format(
                "TimeFrame: {0} {1} {2}",
                this.MarkDate, 
                this.MarkTime,
                this.Duration);
        }
    }
}
