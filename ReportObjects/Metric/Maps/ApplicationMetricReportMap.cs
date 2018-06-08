﻿using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjects
{
    public class ApplicationMetricReportMap: ClassMap<EntityApplication>
    {
        public ApplicationMetricReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.ART).Index(i); i++;
            Map(m => m.ARTRange).Index(i); i++;
            Map(m => m.TimeTotal).Index(i); i++;
            Map(m => m.Calls).Index(i); i++;
            Map(m => m.CPM).Index(i); i++;
            Map(m => m.Errors).Index(i); i++;
            Map(m => m.EPM).Index(i); i++;
            Map(m => m.Exceptions).Index(i); i++;
            Map(m => m.EXCPM).Index(i); i++;
            Map(m => m.HttpErrors).Index(i); i++;
            Map(m => m.HTTPEPM).Index(i); i++;
            Map(m => m.ErrorsPercentage).Index(i); i++;
            Map(m => m.HasActivity).Index(i); i++;
            Map(m => m.From).Index(i); i++;
            Map(m => m.To).Index(i); i++;
            Map(m => m.FromUtc).Index(i); i++;
            Map(m => m.ToUtc).Index(i); i++;
            Map(m => m.Duration).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.DetailLink).Index(i); i++;
            Map(m => m.MetricGraphLink).Index(i); i++;
            Map(m => m.FlameGraphLink).Index(i); i++;
            Map(m => m.FlameChartLink).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
            Map(m => m.MetricLink).Index(i); i++;
        }
    }
}