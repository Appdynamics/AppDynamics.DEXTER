using CsvHelper.Configuration;

namespace AppDynamics.Dexter.DataObjects
{
    public class AgentConfigurationPropertyReportMap : ClassMap<AgentConfigurationProperty>
    {
        public AgentConfigurationPropertyReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.TierName).Index(i); i++;

            Map(m => m.AgentType).Index(i); i++;
            Map(m => m.PropertyName).Index(i); i++;
            Map(m => m.PropertyType).Index(i); i++;
            Map(m => m.Description).Index(i); i++;
            Map(m => m.IsDefault).Index(i); i++;

            Map(m => m.StringValue).Index(i); i++;
            Map(m => m.IntegerValue).Index(i); i++;
            Map(m => m.BooleanValue).Index(i); i++;

            Map(m => m.StringDefaultValue).Index(i); i++;
            Map(m => m.StringMaxLength).Index(i); i++;
            Map(m => m.StringAllowedValues).Index(i); i++;
            Map(m => m.IntegerDefaultValue).Index(i); i++;
            Map(m => m.IntegerMinValue).Index(i); i++;
            Map(m => m.IntegerMaxValue).Index(i); i++;
            Map(m => m.BooleanDefaultValue).Index(i); i++;
            Map(m => m.IsRequired).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
        }
    }
}