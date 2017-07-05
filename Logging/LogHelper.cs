using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace AppDynamics.OfflineData
{
    /// <summary>
    /// Logging class, initialized using singleton pattern
    /// </summary>
    public class LogHelper : IDisposable
    {
        // Names of the trace source used by the solution
        //public const string PROCESS_JOB_TRACE_SOURCE = "AppD.Offline.ProcessJob";
        //public const string OFFLINEDATARENDER_FILE_TRACE_SOURCE = "Render";
        public const string OFFLINE_DATA_TRACE_SOURCE = "AppD.Offline";

        #region Private variables

        /// <summary>
        /// Instance of the logging class
        /// </summary>
        private static volatile LogHelper instance;

        /// <summary>
        /// Object on which to lock the multiple threads trying to initialize the logging class
        /// </summary>
        private static object syncRoot = new Object();

        #endregion

        #region Properties

        /// <summary>
        /// List of sources we're dealing with
        /// </summary>
        public ConcurrentDictionary<string, TraceSource> TraceSources { get; set; }

        #endregion

        #region Constructor, destructor and trace source initialization

        /// <summary>
        /// Singleton pattern method using double-lock to initalize new instance of class in thread-safe manner
        /// Implementation taken from:
        /// Implementing Singleton in C# http://msdn.microsoft.com/en-us/library/ff650316.aspx
        /// </summary>
        public static LogHelper Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new LogHelper();
                    }
                }
                return instance;
            }
            set
            {
                instance = Instance;
            }
        }

        /// <summary>
        /// Private constructor to initialize trace sources
        /// </summary>
        private LogHelper()
        {
            // Initialize trace source collection
            this.TraceSources = new ConcurrentDictionary<string, TraceSource>();
        }

        /// <summary>
        /// Destructor
        /// </summary>
        void IDisposable.Dispose()
        {
            FlushAndCloseAll();
        }

        #endregion

        #region Correlation activity tracing helper

        /// <summary>
        /// Adds an ActivityID to the Trace manager if it does not exist
        /// It could exist if Failed Request Logging is enabled (comes from ETW)
        /// It does not exist (==System.Guid.Empty) if it is not enabled
        /// It is used to put out the correlated events
        /// </summary>
        public void EnsureCorrelationId()
        {
            if (Trace.CorrelationManager.ActivityId == System.Guid.Empty)
            {
                this.NewCorrelationId();
            }
        }

        /// <summary>
        /// Adds an ActivityID to the Trace manager
        /// </summary>
        public void NewCorrelationId()
        {
            Trace.CorrelationManager.ActivityId = System.Guid.NewGuid();
        }

        #endregion

        #region Logging

        /// <summary>
        /// Logs diagnostic message to specified trace source
        /// </summary>
        /// <param name="traceSourceName">Name of trace source to log to</param>
        /// <param name="eventType">Type of event to log</param>
        /// <param name="id">Id of event to log</param>
        /// <param name="message">Message to log</param>
        public void Log(string[] traceSourceNames, TraceEventType eventType, int id, string methodName, string message)
        {
            // Process each of the trace sources hooked up
            foreach (string traceSourceName in traceSourceNames)
            {
                if (this.TraceSources.ContainsKey(traceSourceName) == true)
                {
                    TraceSource loggingTraceSource = this.TraceSources[traceSourceName];

                    // Log it
                    loggingTraceSource.TraceData(
                        eventType,
                        id,
                        new object[] { DateTime.Now.ToString("o"), traceSourceName, methodName, message, Trace.CorrelationManager.ActivityId });

                    // Flush it
                    //loggingTraceSource.Flush();
                }
            }
        }

        /// <summary>
        /// Logs exception to specified trace source
        /// </summary>
        /// <param name="traceSourceName">Name of trace source to log to</param>
        /// <param name="eventType">Type of event to log</param>
        /// <param name="id">Id of event to log</param>
        /// <param name="ex">Exception to log</param>
        public void Log(string[] traceSourceNames, TraceEventType eventType, int id, string methodName, Exception ex)
        {
            foreach (string traceSourceName in traceSourceNames)
            {
                // Process each of the trace sources hooked up
                if (this.TraceSources.ContainsKey(traceSourceName) == true)
                {
                    TraceSource loggingTraceSource = this.TraceSources[traceSourceName];

                    // Log it
                    loggingTraceSource.TraceData(
                        eventType,
                        id,
                        new object[] { DateTime.Now.ToString("o"), traceSourceName, methodName, ex, Trace.CorrelationManager.ActivityId });

                    // Flush it
                    //loggingTraceSource.Flush();
                }
                if (ex.InnerException != null)
                {
                    Log(traceSourceNames, eventType, id, methodName, ex.InnerException);
                }
            }
        }

        public void FlushAndCloseAll()
        {
            // Close all trace sources
            foreach (TraceSource loggingTraceSource in this.TraceSources.Values)
            {
                loggingTraceSource.Flush();
                loggingTraceSource.Close();
            }
        }
        #endregion
    }
}
