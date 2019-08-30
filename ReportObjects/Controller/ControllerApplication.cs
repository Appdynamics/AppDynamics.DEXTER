using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class ControllerApplication
    {
        public string Controller { get; set; }

        public long ApplicationID { get; set; }
        public string ApplicationName { get; set; }
        public string Description { get; set; }

        public long ParentApplicationID { get; set; }

        public string Type { get; set; }
        public string Types { get; set; }

        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }

        public DateTime CreatedOn { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public DateTime UpdatedOn { get; set; }
        public DateTime UpdatedOnUtc { get; set; }

        public ControllerApplication Clone()
        {
            return (ControllerApplication)this.MemberwiseClone();
        }

        public override String ToString()
        {
            return String.Format(
                "ControllerApplication: {0}/{1}({2}) [{3}]",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID,
                this.Type);
        }

    }
}
