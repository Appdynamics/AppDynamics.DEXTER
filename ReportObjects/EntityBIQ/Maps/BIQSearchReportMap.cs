using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class BIQSearchReportMap : ClassMap<BIQSearch>
    {
        public BIQSearchReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;

            Map(m => m.SearchName).Index(i); i++;
            Map(m => m.Description).Index(i); i++;
            Map(m => m.InternalName).Index(i); i++;
            Map(m => m.SearchType).Index(i); i++;
            Map(m => m.SearchMode).Index(i); i++;
            Map(m => m.Visualization).Index(i); i++;
            Map(m => m.ViewMode).Index(i); i++;
            Map(m => m.DataSource).Index(i); i++;

            Map(m => m.NumWidgets).Index(i); i++;

            Map(m => m.Query).Index(i); i++;

            Map(m => m.CreatedBy).Index(i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.CreatedOn), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.CreatedOnUtc), i); i++;

            Map(m => m.UpdatedBy).Index(i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.UpdatedOn), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.UpdatedOnUtc), i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.SearchID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
            Map(m => m.SearchLink).Index(i); i++;
        }
    }
}