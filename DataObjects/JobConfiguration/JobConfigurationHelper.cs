using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;

namespace AppDynamics.OfflineData.JobParameters
{
    public class JobConfigurationHelper
    {
        internal static JobConfiguration readJobConfigurationFromFile(string configurationFilePath)
        {
            try
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Verbose,
                    EventId.JOBFILE_READ,
                    "JobConfigurationHelper.readJobConfigurationFromFile",
                    String.Format("Reading JSON from job file='{0}'", configurationFilePath));

                return JsonConvert.DeserializeObject<JobConfiguration>(File.ReadAllText(configurationFilePath));
            }
            catch (IOException ex)
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.EXCEPTION_IO,
                    "JobConfigurationHelper.readJobConfigurationFromFile",
                    ex);

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.UNABLE_TO_READ_FILE,
                    "JobConfigurationHelper.readJobConfigurationFromFile",
                    String.Format("Unable to read from job file='{0}'", configurationFilePath));
            }
            catch (JsonReaderException ex)
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.EXCEPTION_JSONREADEREXCEPTION,
                    "JobConfigurationHelper.readJobConfigurationFromFile",
                    ex);

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.INVALID_JSON_FORMAT,
                    "JobConfigurationHelper.readJobConfigurationFromFile",
                    String.Format("Invalid JSON in job file='{0}'", configurationFilePath));
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.EXCEPTION_GENERIC,
                    "JobConfigurationHelper.readJobConfigurationFromFile",
                    ex);

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.INVALID_JSON_FORMAT,
                    "JobConfigurationHelper.readJobConfigurationFromFile",
                    String.Format("Unable to load JSON from job file='{0}'", configurationFilePath));
            }

            return null;
        }

        internal static bool writeJobConfigurationToFile(JobConfiguration jobConfiguration, string configurationFilePath)
        {
            try
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Verbose,
                    EventId.JOBFILE_WRITE,
                    "JobConfigurationHelper.writeJobConfigurationToFile",
                    String.Format("Writing JSON to job file='{0}'", configurationFilePath));

                using (StreamWriter sw = File.CreateText(configurationFilePath))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.NullValueHandling = NullValueHandling.Include;
                    serializer.Formatting = Formatting.Indented;
                    serializer.Serialize(sw, jobConfiguration);
                }

                return true;
            }
            catch (IOException ex)
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.EXCEPTION_IO,
                    "JobConfigurationHelper.writeJobConfigurationToFile",
                    ex);

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.UNABLE_TO_WRITE_FILE,
                    "JobConfigurationHelper.writeJobConfigurationToFile",
                    String.Format("Unable to write to job file='{0}'", configurationFilePath));
            }
            catch (JsonWriterException ex)
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.EXCEPTION_JSONWRITEREXCEPTION,
                    "JobConfigurationHelper.writeJobConfigurationToFile",
                    ex);

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.UNABLE_TO_RENDER_JSON,
                    "JobConfigurationHelper.writeJobConfigurationToFile",
                    String.Format("Unable to serialize JSON to job file='{0}'", configurationFilePath));
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.EXCEPTION_GENERIC,
                    "JobConfigurationHelper.writeJobConfigurationToFile",
                    ex);

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.UNABLE_TO_RENDER_JSON,
                    "JobConfigurationHelper.writeJobConfigurationToFile",
                    String.Format("Unable to write JSON to job file='{0}'", configurationFilePath));
            }

            return false;
        }
    }
}
