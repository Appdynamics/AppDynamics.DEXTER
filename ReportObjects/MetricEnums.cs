namespace AppDynamics.Dexter.DataObjects
{
    /// <summary>
    /// What type of the entity
    /// </summary>
    public enum EntityType
    {
        /// <summary>
        /// Measuring at application level
        /// </summary>
        Application,
        /// <summary>
        /// Measuring at at tier level
        /// </summary>
        Tier,
        /// <summary>
        /// Measuring at node level
        /// </summary>
        Node,
        /// <summary>
        /// Measuring at backend
        /// </summary>
        Backend,
        /// <summary>
        /// Measuring at business transaction level in tier
        /// </summary>
        BusinessTransaction,
        /// <summary>
        /// Measuring at service endpoint level in tier
        /// </summary>
        ServiceEndpoint,
        /// <summary>
        /// Measuring at error level in tier
        /// </summary>
        Error
    }

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
