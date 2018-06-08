using System;

namespace AppDynamics.Dexter
{
    public enum FieldComparisonType
    {
        ValueComparison,
        SemicolonMultiLineValueComparison,
        XmlValueComparison,
        JSONValueComparison
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class FieldComparisonAttribute : Attribute
    {
        public FieldComparisonType FieldComparison { get; set; }

        public FieldComparisonAttribute(FieldComparisonType fieldComparisonType)
        {
            this.FieldComparison = fieldComparisonType;
        }
    }
}
