using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class ApplicationHealthCheckComparisonMap : ClassMap<ApplicationHealthCheckComparisonMapping>
    {
        public ApplicationHealthCheckComparisonMap()
        {
            int i = 0;
            Map(m => m.Name).Index(i); i++;
            Map(m => m.Pass).Index(i); i++;
            Map(m => m.Fail).Index(i); i++;
            Map(m => m.EvalCondition).Index(i); i++;
            Map(m => m.Description).Index(i); i++;

            /*    Map(m => m.LatestAppAgentVersion).Index(i); i++;
                Map(m => m.LatestMachineAgentVersion).Index(i); i++;
                Map(m => m.AgentOldPercent).Index(i); i++;
                Map(m => m.AgentPriorSupportedVersions).Index(i); i++;
                Map(m => m.InfoPointUpper).Index(i); i++;
                Map(m => m.InfoPointLower).Index(i); i++;
                Map(m => m.InfoPointLower).Index(i); i++;
                Map(m => m.DCUpper).Index(i); i++;
                Map(m => m.DCLower).Index(i); i++;
                Map(m => m.MachineAgentEnabledUpper).Index(i); i++;
                Map(m => m.MachineAgentEnabledLower).Index(i); i++;
                Map(m => m.SEPCountUpper).Index(i); i++;
                Map(m => m.SEPCountLower).Index(i); i++;
                Map(m => m.BTErrorRateUpper).Index(i); i++;
                Map(m => m.BTErrorRateLower).Index(i); i++;
                Map(m => m.HRViolationRateUpper).Index(i); i++;
                Map(m => m.HRViolationRateLower).Index(i); i++;
                Map(m => m.PoliciesActionUpper).Index(i); i++;
                Map(m => m.PoliciesActionLower).Index(i); i++;
                Map(m => m.TierActiveUpper).Index(i); i++;
                Map(m => m.TierActiveLower).Index(i); i++;
                Map(m => m.NodeActiveUpper).Index(i); i++;
                Map(m => m.NodeActiveLower).Index(i); i++;  */
        }
    }
}
