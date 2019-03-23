using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class BIQField : BIQEntityBase
    {
        public string SchemaName { get; set; }

        public string FieldName { get; set; }
        public string FieldType { get; set; }
        public string Category { get; set; }
        public string Parents { get; set; }
        public int NumParents { get; set; }

        public bool IsSortable { get; set; }
        public bool IsAggregatable { get; set; }
        public bool IsHidden { get; set; }
        public bool IsDeleted { get; set; }

        public override String ToString()
        {
            return String.Format(
                "BIQField: {0}/{1}({2}) {3} {4}({5})",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID,
                this.SchemaName,
                this.FieldName,
                this.FieldType);
        }
    }
}
