using System;

namespace AppDynamics.OfflineData.DataObjects
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
    public class AppDRESTMetricValue
    {
        public long count { get; set; }
        public long current { get; set; }
        public long max { get; set; }
        public long min { get; set; }
        public long occurrences { get; set; }
        public double standardDeviation { get; set; }
        public long startTimeInMillis { get; set; }
        public long sum { get; set; }
        public bool useRange { get; set; }
        public long value { get; set; }

        public override String ToString()
        {
            return String.Format(
                "AppDRESTMetricValue: value={0}, count={1}, startTimeInMillis={2}, startTime={3:u}",
                this.value,
                this.count,
                this.startTimeInMillis,
                convertFromUnixTimestamp(this.startTimeInMillis));
        }

        /// <summary>
        /// Converts UNIX timestamp to DateTime
        /// </summary>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        private static DateTime convertFromUnixTimestamp(long timestamp)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return origin.AddMilliseconds(timestamp);
        }

    }
}
