using System;

namespace AppDynamics.Dexter.DataObjects
{
    public class AgentConfigurationProperty
    {
        public string Controller { get; set; }
        public string ControllerLink { get; set; }

        public long ApplicationID { get; set; }
        public string ApplicationLink { get; set; }
        public string ApplicationName { get; set; }

        public string TierName { get; set; }

        public string AgentType { get; set; }
        public string PropertyName { get; set; }
        public string PropertyType { get; set; }
        public string Description { get; set; }

        public string StringValue { get; set; }
        public int IntegerValue { get; set; }
        public bool BooleanValue { get; set; }

        public string StringDefaultValue { get; set; }
        public int StringMaxLength { get; set; }
        public string StringAllowedValues { get; set; }

        public int IntegerDefaultValue { get; set; }
        public int IntegerMinValue { get; set; }
        public int IntegerMaxValue { get; set; }

        public bool BooleanDefaultValue { get; set; }

        public bool IsRequired { get; set; }

        public bool IsDefault { get; set; }

        public override String ToString()
        {
            return String.Format(
                "AgentConfigurationProperty: {0}/{1}/{2} {3}",
                this.Controller,
                this.ApplicationName,
                this.TierName,
                this.PropertyName);
        }
    }
}
