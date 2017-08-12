using System;
using System.Collections.Generic;

namespace AppDynamics.Dexter.DataObjects
{
    /// <summary>
    ///[{
    ///"frequency": "ONE_MIN",
    ///    "metricId": 56202,
    ///    "metricName": "BTM|Application Summary|Component:28|Calls per Minute",
    ///    "metricPath": "Overall Application Performance|Web|Calls per Minute",
    ///    "metricValues":   [
    ///        {
    ///    "count": 16,
    ///        "current": 38307,
    ///        "max": 11851,
    ///        "min": 0,
    ///        "occurrences": 1,
    ///        "standardDeviation": 0,
    ///        "startTimeInMillis": 1464283920000,
    ///        "sum": 38307,
    ///        "useRange": false,
    ///        "value": 38307
    ///},
    /// </summary>
    public class AppDRESTMetric
    {
        public string frequency { get; set; }
        public int metricId { get; set; }
        public string metricName { get; set; }
        public string metricPath { get; set; }
        public List<AppDRESTMetricValue> metricValues { get; set; }

        public override String ToString()
        {
            return String.Format(
                "AppDRESTMetric: {0}({1})",
                this.metricName,
                this.metricId);
        }
    }
}
