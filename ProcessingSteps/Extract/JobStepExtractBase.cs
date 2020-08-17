using AppDynamics.Dexter.ReportObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class JobStepExtractBase : JobStepBase
    {
        internal void getMetricsForEntities(
            JobTarget jobTarget,
            JobConfiguration jobConfiguration,
            List<MetricExtractMapping> entityMetricExtractMappingList,
            string entityFolderName,
            string entityType)
        {
            using (ControllerApi controllerApi = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword)))
            {
                List<MetricExtractMapping> entityMetricExtractMappingListFiltered = entityMetricExtractMappingList.Where(m => m.EntityType == entityType).ToList();

                loggerConsole.Info("Extract {0} ({1} metrics)", entityType, entityMetricExtractMappingListFiltered.Count);
                logger.Info("Extract {0} ({1} metrics)", entityType, entityMetricExtractMappingListFiltered.Count);

                foreach (MetricExtractMapping metricExtractMapping in entityMetricExtractMappingListFiltered)
                {
                    // Get the full range
                    JobTimeRange jobTimeRange = jobConfiguration.Input.TimeRange;

                    loggerConsole.Trace("{0} {1}", metricExtractMapping.EntityType, metricExtractMapping.MetricPath);
                    logger.Info("Retrieving metric summary for Application {0}({1}), Metric='{2}', From {3:o}, To {4:o}", jobTarget.Application, jobTarget.ApplicationID, metricExtractMapping.MetricPath, jobTimeRange.From, jobTimeRange.To);

                    string metricsJson = String.Empty;

                    string metricsDataFilePath = FilePathMap.MetricFullRangeDataFilePath(jobTarget, entityFolderName, metricExtractMapping.FolderName, jobTimeRange);
                    if (File.Exists(metricsDataFilePath) == false)
                    {
                        // First range is the whole thing
                        metricsJson = controllerApi.GetMetricData(
                            jobTarget.ApplicationID,
                            metricExtractMapping.MetricPath,
                            UnixTimeHelper.ConvertToUnixTimestamp(jobTimeRange.From),
                            UnixTimeHelper.ConvertToUnixTimestamp(jobTimeRange.To),
                            true);

                        if (metricsJson != String.Empty && metricsJson != "[ ]") FileIOHelper.SaveFileToPath(metricsJson, metricsDataFilePath);
                    }

                    if (jobConfiguration.Input.MetricsSelectionCriteria.IncludeHourAndMinuteDetail == true)
                    {
                        // Get the hourly time ranges
                        for (int j = 0; j < jobConfiguration.Input.HourlyTimeRanges.Count; j++)
                        {
                            jobTimeRange = jobConfiguration.Input.HourlyTimeRanges[j];

                            logger.Info("Retrieving metric details for Application {0}({1}), Metric='{2}', From {3:o}, To {4:o}", jobTarget.Application, jobTarget.ApplicationID, metricExtractMapping.MetricPath, jobTimeRange.From, jobTimeRange.To);

                            metricsDataFilePath = FilePathMap.MetricHourRangeDataFilePath(jobTarget, entityFolderName, metricExtractMapping.FolderName, jobTimeRange);
                            if (File.Exists(metricsDataFilePath) == false)
                            {
                                // Subsequent ones are details
                                metricsJson = controllerApi.GetMetricData(
                                    jobTarget.ApplicationID,
                                    metricExtractMapping.MetricPath,
                                    UnixTimeHelper.ConvertToUnixTimestamp(jobTimeRange.From),
                                    UnixTimeHelper.ConvertToUnixTimestamp(jobTimeRange.To),
                                    false);

                                if (metricsJson != String.Empty && metricsJson != "[ ]") FileIOHelper.SaveFileToPath(metricsJson, metricsDataFilePath);
                            }
                        }
                    }
                }

                loggerConsole.Info("Completed {0} ({1} metrics)", entityType, entityMetricExtractMappingListFiltered.Count);
            }
        }
    }
}
