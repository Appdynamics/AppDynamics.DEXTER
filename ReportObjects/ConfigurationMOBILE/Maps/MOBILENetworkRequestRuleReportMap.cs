using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class MOBILENetworkRequestRuleReportMap : ClassMap<MOBILENetworkRequestRule>
    {
        public MOBILENetworkRequestRuleReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;

            Map(m => m.RuleName).Index(i); i++;
            Map(m => m.DetectionType).Index(i); i++;

            Map(m => m.IsEnabled).Index(i); i++;
            Map(m => m.IsDefault).Index(i); i++;
            Map(m => m.Priority).Index(i); i++;

            Map(m => m.MatchURL).Index(i); i++;
            Map(m => m.MatchIPAddress).Index(i); i++;
            Map(m => m.MatchMobileApp).Index(i); i++;
            Map(m => m.MatchUserAgent).Index(i); i++;
            Map(m => m.MatchUserAgentType).Index(i); i++;

            Map(m => m.NamingType).Index(i); i++;
            Map(m => m.AnchorType).Index(i); i++;
            Map(m => m.UrlSegments).Index(i); i++;
            Map(m => m.AnchorSegments).Index(i); i++;
            Map(m => m.RegexGroups).Index(i); i++;
            Map(m => m.QueryStrings).Index(i); i++;
            Map(m => m.DomainNameType).Index(i); i++;
            Map(m => m.UseProtocol).Index(i); i++;
            Map(m => m.UseDomain).Index(i); i++;
            Map(m => m.UseURL).Index(i); i++;
            Map(m => m.UseHTTP).Index(i); i++;
            Map(m => m.UseRegex).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
        }
    }
}