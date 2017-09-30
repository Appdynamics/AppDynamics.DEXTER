using System;

namespace AppDynamics.Dexter.DataObjects
{
    public class MetricSummary
    {
        public string PropertyName { get; set; }

        public object PropertyValue { get; set; }

        public object Link { get; set; }

        public override String ToString()
        {
            return String.Format(
                "MetricSummary: {0}={1}",
                this.PropertyName,
                this.PropertyValue);
        }
    }
}