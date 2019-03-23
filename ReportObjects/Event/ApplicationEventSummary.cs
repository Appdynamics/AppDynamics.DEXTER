using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class ApplicationEventSummary
    {
        public string Controller { get; set; }
        public string ControllerLink { get; set; }

        public long ApplicationID { get; set; }
        public string ApplicationLink { get; set; }
        public string ApplicationName { get; set; }

        public string Type { get; set; }

        public int NumEvents { get; set; }
        public int NumEventsInfo { get; set; }
        public int NumEventsWarning { get; set; }
        public int NumEventsError { get; set; }
        public int NumHRViolations { get; set; }
        public int NumHRViolationsWarning { get; set; }
        public int NumHRViolationsCritical { get; set; }

        public int Duration { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public DateTime FromUtc { get; set; }
        public DateTime ToUtc { get; set; }

        public bool HasActivity { get; set; }

        public override String ToString()
        {
            return String.Format(
                "ApplicationEventSummary: {0}/{1}({2})",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID);
        }
    }
}
