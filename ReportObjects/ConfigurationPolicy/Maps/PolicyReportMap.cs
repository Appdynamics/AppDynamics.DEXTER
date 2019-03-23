using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class PolicyReportMap : ClassMap<Policy>
    {
        public PolicyReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;

            Map(m => m.PolicyName).Index(i); i++;
            Map(m => m.PolicyType).Index(i); i++;

            Map(m => m.NumActions).Index(i); i++;
            Map(m => m.Actions).Index(i); i++;

            Map(m => m.Duration).Index(i); i++;
            Map(m => m.IsEnabled).Index(i); i++;
            Map(m => m.IsBatchActionsPerMinute).Index(i); i++;

            Map(m => m.CreatedBy).Index(i); i++;
            Map(m => m.ModifiedBy).Index(i); i++;

            Map(m => m.RequestExperiences).Index(i); i++;
            Map(m => m.CustomEventFilters).Index(i); i++;
            Map(m => m.MissingEntities).Index(i); i++;
            Map(m => m.FilterProperties).Index(i); i++;
            Map(m => m.MaxRows).Index(i); i++;

            Map(m => m.ApplicationIDs).Index(i); i++;
            Map(m => m.BTIDs).Index(i); i++;
            Map(m => m.TierIDs).Index(i); i++;
            Map(m => m.NodeIDs).Index(i); i++;
            Map(m => m.ErrorIDs).Index(i); i++;
            Map(m => m.NumHRs).Index(i); i++;
            Map(m => m.HRIDs).Index(i); i++;
            Map(m => m.HRNames).Index(i); i++;

            Map(m => m.HRVStartedWarning).Index(i); i++;
            Map(m => m.HRVStartedCritical).Index(i); i++;
            Map(m => m.HRVWarningToCritical).Index(i); i++;
            Map(m => m.HRVCriticalToWarning).Index(i); i++;
            Map(m => m.HRVContinuesCritical).Index(i); i++;
            Map(m => m.HRVContinuesWarning).Index(i); i++;
            Map(m => m.HRVEndedCritical).Index(i); i++;
            Map(m => m.HRVEndedWarning).Index(i); i++;
            Map(m => m.HRVCanceledCritical).Index(i); i++;
            Map(m => m.HRVCanceledWarning).Index(i); i++;

            Map(m => m.RequestSlow).Index(i); i++;
            Map(m => m.RequestVerySlow).Index(i); i++;
            Map(m => m.RequestStall).Index(i); i++;
            Map(m => m.AllError).Index(i); i++;

            Map(m => m.AppCrash).Index(i); i++;
            Map(m => m.AppCrashCLR).Index(i); i++;
            Map(m => m.AppRestart).Index(i); i++;

            Map(m => m.EventFilterRawValue).Index(i); i++;
            Map(m => m.EntityFiltersRawValue).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.PolicyID).Index(i); i++;

            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
        }
    }
}