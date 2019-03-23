using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class EmailAlertTemplate
    {
        public string Controller { get; set; }

        public string Name { get; set; }

        public bool OneEmailPerEvent { get; set; }
        public long EventLimit { get; set; }

        public string To { get; set; }
        public string CC { get; set; }
        public string BCC { get; set; }

        public string TestTo { get; set; }
        public string TestCC { get; set; }
        public string TestBCC { get; set; }

        public string TestLogLevel { get; set; }

        public string Headers { get; set; }
        public string Subject { get; set; }
        public string TextBody { get; set; }
        public string HTMLBody { get; set; }
        public bool IncludeHTMLBody { get; set; }

        public string Properties { get; set; }
        public string TestProperties { get; set; }

        public string EventTypes { get; set; }

        public long TemplateID { get; set; }

        public override String ToString()
        {
            return String.Format(
                "EmailAlertTemplate: {0} {1} {2}",
                this.Controller,
                this.Name, 
                this.To);
        }
    }
}
