using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class BIQFieldReportMap : ClassMap<BIQField>
    {
        public BIQFieldReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;

            Map(m => m.SchemaName).Index(i); i++;

            Map(m => m.FieldName).Index(i); i++;
            Map(m => m.FieldType).Index(i); i++;
            Map(m => m.Category).Index(i); i++;
            Map(m => m.Parents).Index(i); i++;
            Map(m => m.NumParents).Index(i); i++;

            Map(m => m.IsSortable).Index(i); i++;
            Map(m => m.IsAggregatable).Index(i); i++;
            Map(m => m.IsHidden).Index(i); i++;
            Map(m => m.IsDeleted).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
        }
    }
}