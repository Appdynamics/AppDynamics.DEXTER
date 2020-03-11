using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class HealthCheckRuleDescription
    {
        public string Category { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }

        public HealthCheckRuleDescription(string category, string code, string name)
        {
            this.Category = category;
            this.Code = code;
            this.Name = name;
        }

        public override String ToString()
        {
            return String.Format(
                "{0}/{1} ({2})",
                this.Category,
                this.Name,
                this.Code);
        }
    }
}
