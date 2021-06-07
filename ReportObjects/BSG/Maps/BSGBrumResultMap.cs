using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class BSGBrumResultMap : ClassMap<BSGBrumResult>
    {
        public BSGBrumResultMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.DataReported).Index(i); i++;
            Map(m => m.NumPages).Index(i); i++;
            Map(m => m.NumAjax).Index(i); i++;
            // Map(m => m.PageLimitHit).Index(i); i++;
            // Map(m => m.AjaxLimitHit).Index(i); i++;
            Map(m => m.NumCustomPageRules).Index(i); i++;
            Map(m => m.NumCustomAjaxRules).Index(i); i++;
            Map(m => m.BrumHealthRules).Index(i); i++;
            Map(m => m.LinkedPolicies).Index(i); i++;
            Map(m => m.LinkedActions).Index(i); i++;
            Map(m => m.WarningViolations).Index(i); i++;
            Map(m => m.CriticalViolations).Index(i); i++;
        }
    }
}
