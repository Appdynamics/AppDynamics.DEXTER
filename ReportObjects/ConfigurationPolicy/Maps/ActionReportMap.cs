using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class ActionReportMap : ClassMap<Action>
    {
        public ActionReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;

            Map(m => m.ActionName).Index(i); i++;
            Map(m => m.ActionType).Index(i); i++;
            Map(m => m.Description).Index(i); i++;

            Map(m => m.Priority).Index(i); i++;

            Map(m => m.IsAdjudicate).Index(i); i++;
            Map(m => m.AdjudicatorEmail).Index(i); i++;

            Map(m => m.ScriptPath).Index(i); i++;
            Map(m => m.LogPaths).Index(i); i++;
            Map(m => m.ScriptOutputPaths).Index(i); i++;
            Map(m => m.CollectScriptOutputs).Index(i); i++;
            Map(m => m.TimeoutMinutes).Index(i); i++;

            Map(m => m.To).Index(i); i++;
            Map(m => m.CC).Index(i); i++;
            Map(m => m.BCC).Index(i); i++;
            Map(m => m.Subject).Index(i); i++;
            Map(m => m.CustomProperties).Index(i); i++;

            Map(m => m.ActionTemplate).Index(i); i++;
            Map(m => m.TemplateID).Index(i); i++;

            Map(m => m.CustomType).Index(i); i++;

            Map(m => m.NumSamples).Index(i); i++;
            Map(m => m.SampleInterval).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.ActionID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
        }
    }
}