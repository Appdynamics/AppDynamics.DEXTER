using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class BIQSchemaReportMap : ClassMap<BIQSchema>
    {
        public BIQSchemaReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;

            Map(m => m.SchemaName).Index(i); i++;

            Map(m => m.IsCustom).Index(i); i++;

            Map(m => m.NumFields).Index(i); i++;
            Map(m => m.NumStringFields).Index(i); i++;
            Map(m => m.NumIntegerFields).Index(i); i++;
            Map(m => m.NumLongFields).Index(i); i++;
            Map(m => m.NumFloatFields).Index(i); i++;
            Map(m => m.NumDoubleFields).Index(i); i++;
            Map(m => m.NumBooleanFields).Index(i); i++;
            Map(m => m.NumDateFields).Index(i); i++;
            Map(m => m.NumObjectFields).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
        }
    }
}