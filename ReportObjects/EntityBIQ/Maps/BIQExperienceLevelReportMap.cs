using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class BIQExperienceLevelReportMap : ClassMap<BIQExperienceLevel>
    {
        public BIQExperienceLevelReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;

            Map(m => m.ExperienceLevelName).Index(i); i++;

            Map(m => m.DataSource).Index(i); i++;
            Map(m => m.EventField).Index(i); i++;
            Map(m => m.Criteria).Index(i); i++;
            Map(m => m.ThresholdOperator).Index(i); i++;
            Map(m => m.ThresholdValue).Index(i); i++;

            Map(m => m.Period).Index(i); i++;
            Map(m => m.Timezone).Index(i); i++;

            Map(m => m.IsActive).Index(i); i++;
            Map(m => m.IsIncludeErrors).Index(i); i++;

            Map(m => m.NormalThreshold).Index(i); i++;
            Map(m => m.WarningThreshold).Index(i); i++;
            //Map(m => m.CriticalThreshold).Index(i); i++;

            Map(m => m.NumExclusionPeriods).Index(i); i++;
            Map(m => m.ExclusionPeriodsRaw).Index(i); i++;

            Map(m => m.StartOn).Index(i); i++;
            Map(m => m.StartOnUtc).Index(i); i++;

            Map(m => m.CreatedBy).Index(i); i++;
            Map(m => m.CreatedOn).Index(i); i++;
            Map(m => m.CreatedOnUtc).Index(i); i++;

            Map(m => m.UpdatedBy).Index(i); i++;
            Map(m => m.UpdatedOn).Index(i); i++;
            Map(m => m.UpdatedOnUtc).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.ExperienceLevelID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
        }
    }
}