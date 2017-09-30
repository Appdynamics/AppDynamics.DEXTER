namespace AppDynamics.Dexter.DataObjects
{
    /// <summary>
    /// Frequency of metric values
    /// </summary>
    public enum MetricResolution
    {
        ONE_MIN = 1,
        TEN_MIN = 10,
        SIXTY_MIN = 60
    }

    /// <summary>
    /// Type of metric, count of events or duration of events
    /// </summary>
    public enum MetricType
    {
        /// <summary>
        /// Calls per minute, Errors per minute
        /// </summary>
        Count,
        /// <summary>
        /// Average Response Time
        /// </summary>
        Duration
    }

}
