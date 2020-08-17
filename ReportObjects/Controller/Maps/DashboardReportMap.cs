using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class DashboardReportMap : ClassMap<Dashboard>
    {
        public DashboardReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.DashboardName).Index(i); i++;
            Map(m => m.Description).Index(i); i++;

            Map(m => m.NumWidgets).Index(i); i++;
            Map(m => m.NumAnalyticsWidgets).Index(i); i++;
            Map(m => m.NumEventListWidgets).Index(i); i++;
            Map(m => m.NumGaugeWidgets).Index(i); i++;
            Map(m => m.NumGraphWidgets).Index(i); i++;
            Map(m => m.NumHealthListWidgets).Index(i); i++;
            Map(m => m.NumIFrameWidgets).Index(i); i++;
            Map(m => m.NumImageWidgets).Index(i); i++;
            Map(m => m.NumMetricLabelWidgets).Index(i); i++;
            Map(m => m.NumPieWidgets).Index(i); i++;
            Map(m => m.NumTextWidgets).Index(i); i++;

            Map(m => m.CanvasType).Index(i); i++;
            Map(m => m.TemplateEntityType).Index(i); i++;
            Map(m => m.SecurityToken).Index(i); i++;
            Map(m => m.IsShared).Index(i); i++;
            Map(m => m.IsSharingRevoked).Index(i); i++;
            Map(m => m.IsTemplate).Index(i); i++;

            Map(m => m.Width).Index(i); i++;
            Map(m => m.Height).Index(i); i++;

            Map(m => m.BackgroundColor).Index(i); i++;

            Map(m => m.MinutesBefore).Index(i); i++;
            Map(m => m.RefreshInterval).Index(i); i++;
            Map(m => m.StartTime).Index(i); i++;
            Map(m => m.StartTimeUtc).Index(i); i++;
            Map(m => m.EndTime).Index(i); i++;
            Map(m => m.EndTimeUtc).Index(i); i++;

            Map(m => m.CreatedBy).Index(i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.CreatedOn), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.CreatedOnUtc), i); i++;

            Map(m => m.UpdatedBy).Index(i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.UpdatedOn), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.UpdatedOnUtc), i); i++;

            Map(m => m.DashboardID).Index(i); i++;
            Map(m => m.DashboardLink).Index(i); i++;
        }
    }
}