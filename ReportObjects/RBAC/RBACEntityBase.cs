using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class RBACEntityBase
    {
        public string Controller { get; set; }
    
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }

        public DateTime CreatedOn { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public DateTime UpdatedOn { get; set; }
        public DateTime UpdatedOnUtc { get; set; }
    }
}
