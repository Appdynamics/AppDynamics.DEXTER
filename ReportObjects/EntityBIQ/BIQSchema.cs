using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class BIQSchema : BIQEntityBase
    {
        public string SchemaName { get; set; }
        public bool IsCustom { get; set; }

        public int NumFields { get; set; }
        public int NumStringFields { get; set; }
        public int NumIntegerFields { get; set; }
        public int NumLongFields { get; set; }
        public int NumDoubleFields { get; set; }
        public int NumFloatFields { get; set; }
        public int NumBooleanFields { get; set; }
        public int NumDateFields { get; set; }
        public int NumObjectFields { get; set; }

        public override String ToString()
        {
            return String.Format(
                "BIQSchema: {0}/{1}({2}) {3}({4})",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID,
                this.SchemaName,
                this.NumFields);
        }
    }
}
