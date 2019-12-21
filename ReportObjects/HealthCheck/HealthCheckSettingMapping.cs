using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class HealthCheckSettingMapping
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string DataType { get; set; }

        public override String ToString()
        {
            return String.Format(
                "Name: {0}={1} ({2})",
                this.Name,
                this.Value,
                this.DataType);
        }
    }
}
