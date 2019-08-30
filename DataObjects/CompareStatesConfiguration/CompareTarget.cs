using System;

namespace AppDynamics.Dexter
{
    public class CompareTarget
    {
        public string ReferenceConroller { get; set; }
        public string ReferenceApplication { get; set; }
        public long ReferenceApplicationID { get; set; }
        public string DifferenceController { get; set; }
        public string DifferenceApplication { get; set; }
        public long DifferenceApplicationID { get; set; }
        public bool NameRegex { get; set; }
        public string Type { get; set; }

        public CompareTarget Clone()
        {
            return (CompareTarget)this.MemberwiseClone();
        }

        public override string ToString()
        {
            return String.Format(
                "CompareTarget: {0}/{1}({2})[{3}] vs {4}/{5}({6})[{7}]",
                this.ReferenceConroller,
                this.ReferenceApplication,
                this.ReferenceApplicationID,
                this.Type,
                this.DifferenceController,
                this.DifferenceApplication,
                this.DifferenceApplicationID,
                this.Type);
        }
    }
}
