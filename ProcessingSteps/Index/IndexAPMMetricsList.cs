using AppDynamics.Dexter.DataObjects;
using AppDynamics.Dexter.ReportObjectMaps;
using AppDynamics.Dexter.ReportObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class IndexAPMMetricsList : JobStepIndexBase
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Compiler", "CS0168", Justification = "Hiding IndexOutOfRangeException that may occur when parsing an array delimited by |")]
        public override bool Execute(ProgramOptions programOptions, JobConfiguration jobConfiguration)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            StepTiming stepTimingFunction = new StepTiming();
            stepTimingFunction.JobFileName = programOptions.OutputJobFilePath;
            stepTimingFunction.StepName = jobConfiguration.Status.ToString();
            stepTimingFunction.StepID = (int)jobConfiguration.Status;
            stepTimingFunction.StartTime = DateTime.Now;
            stepTimingFunction.NumEntities = jobConfiguration.Target.Count;

            this.DisplayJobStepStartingStatus(jobConfiguration);

            FilePathMap = new FilePathMap(programOptions, jobConfiguration);

            try
            {
                if (this.ShouldExecute(programOptions, jobConfiguration) == false)
                {
                    return true;
                }

                if (jobConfiguration.Target.Count(t => t.Type == APPLICATION_TYPE_APM) == 0)
                {
                    logger.Warn("No {0} targets to process", APPLICATION_TYPE_APM);
                    loggerConsole.Warn("No {0} targets to process", APPLICATION_TYPE_APM);

                    return true;
                }

                bool reportFolderCleaned = false;

                // Process each target
                for (int i = 0; i < jobConfiguration.Target.Count; i++)
                {
                    Stopwatch stopWatchTarget = new Stopwatch();
                    stopWatchTarget.Start();

                    JobTarget jobTarget = jobConfiguration.Target[i];

                    if (jobTarget.Type != null && jobTarget.Type.Length > 0 && jobTarget.Type != APPLICATION_TYPE_APM) continue;

                    StepTiming stepTimingTarget = new StepTiming();
                    stepTimingTarget.Controller = jobTarget.Controller;
                    stepTimingTarget.ApplicationName = jobTarget.Application;
                    stepTimingTarget.ApplicationID = jobTarget.ApplicationID;
                    stepTimingTarget.JobFileName = programOptions.OutputJobFilePath;
                    stepTimingTarget.StepName = jobConfiguration.Status.ToString();
                    stepTimingTarget.StepID = (int)jobConfiguration.Status;
                    stepTimingTarget.StartTime = DateTime.Now;

                    try
                    {
                        this.DisplayJobTargetStartingStatus(jobConfiguration, jobTarget, i + 1);

                        #region Preload lists of entities

                        List<APMTier> tiersList = FileIOHelper.ReadListFromCSVFile<APMTier>(FilePathMap.APMTiersIndexFilePath(jobTarget), new APMTierReportMap());
                        List<APMNode> nodesList = FileIOHelper.ReadListFromCSVFile<APMNode>(FilePathMap.APMNodesIndexFilePath(jobTarget), new APMNodeReportMap());
                        List<APMBusinessTransaction> businessTransactionsList = FileIOHelper.ReadListFromCSVFile<APMBusinessTransaction>(FilePathMap.APMBusinessTransactionsIndexFilePath(jobTarget), new APMBusinessTransactionReportMap());
                        List<APMServiceEndpoint> serviceEndpointsList = FileIOHelper.ReadListFromCSVFile<APMServiceEndpoint>(FilePathMap.APMServiceEndpointsIndexFilePath(jobTarget), new APMServiceEndpointReportMap());
                        List<APMError> errorsList = FileIOHelper.ReadListFromCSVFile<APMError>(FilePathMap.APMErrorsIndexFilePath(jobTarget), new APMErrorReportMap());
                        List<APMBackend> backendsList = FileIOHelper.ReadListFromCSVFile<APMBackend>(FilePathMap.APMBackendsIndexFilePath(jobTarget), new APMBackendReportMap());
                        List<APMInformationPoint> informationPointsList = FileIOHelper.ReadListFromCSVFile<APMInformationPoint>(FilePathMap.APMInformationPointsIndexFilePath(jobTarget), new APMInformationPointReportMap());

                        Dictionary<string, APMTier> tiersDictionary = new Dictionary<string, APMTier>();
                        if (tiersList != null)
                        {
                            tiersDictionary = tiersList.ToDictionary(e => e.TierName, e => e);
                        }
                        Dictionary<string, APMNode> nodesDictionary = new Dictionary<string, APMNode>();
                        if (nodesList != null)
                        {
                            nodesDictionary = nodesList.ToDictionary(e => String.Format(@"{0}\{1}", e.TierName, e.NodeName), e => e);
                        }
                        Dictionary<string, APMBusinessTransaction> businessTransactionsDictionary = new Dictionary<string, APMBusinessTransaction>();
                        if (businessTransactionsList != null)
                        {
                            businessTransactionsDictionary = businessTransactionsList.ToDictionary(e => String.Format(@"{0}\{1}", e.TierName, e.BTName), e => e);
                        }
                        Dictionary<string, APMServiceEndpoint> serviceEndpointDictionary = new Dictionary<string, APMServiceEndpoint>();
                        if (serviceEndpointsList != null)
                        {
                            serviceEndpointDictionary = serviceEndpointsList.ToDictionary(e => String.Format(@"{0}\{1}", e.TierName, e.SEPName), e => e);
                        }
                        Dictionary<string, APMError> errorDictionary = new Dictionary<string, APMError>();
                        if (errorsList != null)
                        {
                            errorDictionary = errorsList.ToDictionary(e => String.Format(@"{0}\{1}", e.TierName, e.ErrorName), e => e);
                        }
                        Dictionary<string, APMBackend> backendDictionary = new Dictionary<string, APMBackend>();
                        if (backendsList != null)
                        {
                            backendDictionary = backendsList.ToDictionary(e => e.BackendName, e => e);
                        }
                        Dictionary<string, APMInformationPoint> informationPointDictionary = new Dictionary<string, APMInformationPoint>();
                        if (informationPointsList != null)
                        {
                            informationPointDictionary = informationPointsList.ToDictionary(e => e.IPName, e => e);
                        }

                        #endregion

                        #region Parse metrics into lists

                        loggerConsole.Info("Parse metrics");

                        List<Metric> metricsINFRAList = parseListOfMetrics(jobTarget, jobConfiguration, "INFRA", 14);
                        List<Metric> metricsAPPList = parseListOfMetrics(jobTarget, jobConfiguration, "APP", 12);
                        List<Metric> metricsBACKENDList = parseListOfMetrics(jobTarget, jobConfiguration, "BACKEND", 6);
                        List<Metric> metricsBTList = parseListOfMetrics(jobTarget, jobConfiguration, "BT", 22);
                        List<Metric> metricsSEPList = parseListOfMetrics(jobTarget, jobConfiguration, "SEP", 5);
                        List<Metric> metricsERRList = parseListOfMetrics(jobTarget, jobConfiguration, "ERR", 7);
                        List<Metric> metricsIPList = parseListOfMetrics(jobTarget, jobConfiguration, "IP", 6);
                        List<Metric> metricsWEBList = parseListOfMetrics(jobTarget, jobConfiguration, "WEB", 6);
                        List<Metric> metricsMOBILEList = parseListOfMetrics(jobTarget, jobConfiguration, "MOBILE", 8);

                        #endregion

                        #region Map the APM Entities into Metrics

                        #region Application Infrastructure Performance

                        if (metricsINFRAList != null)
                        {
                            foreach (Metric metric in metricsINFRAList)
                            {
                                // Application Infrastructure Performance|ECommerce-Services|JVM|Process CPU Burnt (ms/min)
                                //                                        ^^^^^^^^^^^^^^^^^^
                                // Tier name is in second segment
                                APMTier tier = null;
                                if (tiersDictionary.TryGetValue(metric.Segment2, out tier) == true)
                                {
                                    metric.TierName = tier.TierName;
                                    metric.TierID = tier.TierID;
                                    metric.TierAgentType = tier.AgentType;

                                    metric.EntityName = metric.TierName;
                                    metric.EntityID = metric.TierID;
                                    metric.EntityType = metric.TierAgentType;
                                }

                                // Application Infrastructure Performance|ECommerce-Services|Individual Nodes|ECommerce_WEB1_NODE|JVM|Process CPU Burnt (ms/min)
                                //                                                           ^^^^^^^^^^^^^^^^
                                //                                                                            ^^^^^^^^^^^^^^^^^^^
                                // Node name is behind Individual Nodes
                                if (String.Compare(metric.Segment3, "Individual Nodes", true) == 0)
                                {
                                    APMNode node = null;
                                    if (nodesDictionary.TryGetValue(String.Format(@"{0}\{1}", metric.TierName, metric.Segment4), out node) == true)
                                    {
                                        metric.NodeName = node.NodeName;
                                        metric.NodeID = node.NodeID;
                                        metric.NodeAgentType = node.AgentType;

                                        metric.EntityName = metric.NodeName;
                                        metric.EntityID = metric.NodeID;
                                        metric.EntityType = metric.NodeAgentType;
                                    }
                                }
                            }
                        }

                        #endregion

                        #region Overall Application Performance

                        if (metricsAPPList != null)
                        {
                            foreach (Metric metric in metricsAPPList)
                            {
                                if (metric.NumSegments == 2)
                                {
                                    // Overall Application Performance|Calls per Minute
                                    metric.EntityName = metric.ApplicationName;
                                    metric.EntityID = metric.ApplicationID;
                                    metric.EntityType = "Application";
                                }
                                else
                                {
                                    // Overall Application Performance|ECommerce-Services|Average Response Time (ms)
                                    //                                 ^^^^^^^^^^^^^^^^^^
                                    // Tier name is in second segment
                                    APMTier tier = null;
                                    if (tiersDictionary.TryGetValue(metric.Segment2, out tier) == true)
                                    {
                                        metric.TierName = tier.TierName;
                                        metric.TierID = tier.TierID;
                                        metric.TierAgentType = tier.AgentType;

                                        metric.EntityName = metric.TierName;
                                        metric.EntityID = metric.TierID;
                                        metric.EntityType = metric.TierAgentType;
                                    }

                                    // Overall Application Performance|Order-Processing-Services|Individual Nodes|ECommerce_JMS_NODE|Calls per Minute
                                    //                                                           ^^^^^^^^^^^^^^^^
                                    //                                                                            ^^^^^^^^^^^^^^^^^^
                                    // Node name is behind Individual Nodes
                                    if (String.Compare(metric.Segment3, "Individual Nodes", true) == 0)
                                    {
                                        APMNode node = null;
                                        if (nodesDictionary.TryGetValue(String.Format(@"{0}\{1}", metric.TierName, metric.Segment4), out node) == true)
                                        {
                                            metric.NodeName = node.NodeName;
                                            metric.NodeID = node.NodeID;
                                            metric.NodeAgentType = node.AgentType;

                                            metric.EntityName = metric.NodeName;
                                            metric.EntityID = metric.NodeID;
                                            metric.EntityType = metric.NodeAgentType;
                                        }

                                        // Overall Application Performance|ECommerce-Services|Individual Nodes|ECommerce_WEB2_NODE|External Calls|Call-JMS to Discovered backend call - Active MQ-fulfillmentQueue|Calls per Minute
                                        //                                                    ^^^^^^^^^^^^^^^^
                                        //                                                                                         ^^^^^^^^^^^^^^
                                        //                                                                                                                                              ^^^^^^^^^^^^^^^^^^^^^^^^^^
                                        // Backend name is behind External Calls
                                        if (String.Compare(metric.Segment5, "External Calls", true) == 0)
                                        {
                                            // Call to another application:
                                            // Overall Application Performance|Order-Processing-Services|Individual Nodes|ECommerce_JMS_NODE|External Calls|Call-HTTP to External Application - ECommerce-Fulfillment|Calls per Minute
                                            if (metric.Segment6.Contains(" to External Application") == true)
                                            {
                                                int splitterIndex = metric.Segment6.IndexOf(" - ");
                                                if (splitterIndex > 0)
                                                {
                                                    string targetApplicationName = metric.Segment6.Substring(splitterIndex + 3);
                                                    metric.BackendName = targetApplicationName;
                                                    metric.BackendID = -1;
                                                    metric.BackendType = "Application";

                                                    metric.EntityName = metric.BackendName;
                                                    metric.EntityID = metric.BackendID;
                                                    metric.EntityType = metric.BackendType;
                                                }
                                            }
                                            // Call to backend:
                                            // Overall Application Performance|ECommerce-Services|Individual Nodes|ECommerce_WEB1_NODE|External Calls|Call-HTTP to Discovered backend call - api.shipping.com|Errors per Minute
                                            else if (metric.Segment6.Contains(" to Discovered backend call") == true)
                                            {
                                                int splitterIndex = metric.Segment6.IndexOf(" - ");
                                                if (splitterIndex > 0)
                                                {
                                                    string targetBackendName = metric.Segment6.Substring(splitterIndex + 3);
                                                    APMBackend backend = null;
                                                    if (backendDictionary.TryGetValue(targetBackendName, out backend) == true)
                                                    {
                                                        metric.BackendName = backend.BackendName;
                                                        metric.BackendID = backend.BackendID;
                                                        metric.BackendType = backend.BackendType;

                                                        metric.EntityName = metric.BackendName;
                                                        metric.EntityID = metric.BackendID;
                                                        metric.EntityType = metric.BackendType;
                                                    }
                                                }
                                            }
                                            // Call to another tier:
                                            // Overall Application Performance|Order-Processing-Services|Individual Nodes|ECommerce_JMS_NODE|External Calls|Call-HTTP to Fulfillment-Services|Errors per Minute
                                            else
                                            {
                                                int splitterIndex = metric.Segment6.LastIndexOf(" to ");
                                                if (splitterIndex > 0)
                                                {
                                                    string targetTierName = metric.Segment6.Substring(splitterIndex + 4);
                                                    if (tiersDictionary.TryGetValue(targetTierName, out tier) == true)
                                                    {
                                                        metric.BackendName = tier.TierName;
                                                        metric.BackendID = tier.TierID;
                                                        metric.BackendType = "Tier";

                                                        metric.EntityName = metric.BackendName;
                                                        metric.EntityID = metric.BackendID;
                                                        metric.EntityType = tier.AgentType;
                                                    }
                                                }
                                            }

                                        }
                                    }

                                    // Overall Application Performance|ECommerce-Services|External Calls|Call-JMS to Discovered backend call - Active MQ-fulfillmentQueue|Average Response Time (ms)
                                    //                                                    ^^^^^^^^^^^^^^
                                    //                                                                                                         ^^^^^^^^^^^^^^^^^^^^^^^^^^
                                    // Backend name is behind External Calls
                                    if (String.Compare(metric.Segment3, "External Calls", true) == 0)
                                    {
                                        // Call to another application:
                                        // Overall Application Performance|Order-Processing-Services|External Calls|Call-HTTP to External Application - ECommerce-Fulfillment|Average Response Time (ms)
                                        if (metric.Segment4.Contains(" to External Application") == true)
                                        {
                                            int splitterIndex = metric.Segment4.IndexOf(" - ");
                                            if (splitterIndex > 0)
                                            {
                                                string targetApplicationName = metric.Segment4.Substring(splitterIndex + 3);
                                                metric.BackendName = targetApplicationName;
                                                metric.BackendID = -1;
                                                metric.BackendType = "Application";

                                                metric.EntityName = metric.BackendName;
                                                metric.EntityID = metric.BackendID;
                                                metric.EntityType = metric.BackendType;
                                            }
                                        }
                                        // Call to backend:
                                        // Overall Application Performance|ECommerce-Services|External Calls|Call-JDBC to Discovered backend call - APPDY-MySQL DB-DB-5.5.5-10.1.43-MariaDB-1~bionic|Calls per Minute
                                        else if (metric.Segment4.Contains(" to Discovered backend call") == true)
                                        {
                                            int splitterIndex = metric.Segment4.IndexOf(" - ");
                                            if (splitterIndex > 0)
                                            {
                                                string targetBackendName = metric.Segment4.Substring(splitterIndex + 3);
                                                APMBackend backend = null;
                                                if (backendDictionary.TryGetValue(targetBackendName, out backend) == true)
                                                {
                                                    metric.BackendName = backend.BackendName;
                                                    metric.BackendID = backend.BackendID;
                                                    metric.BackendType = backend.BackendType;

                                                    metric.EntityName = metric.BackendName;
                                                    metric.EntityID = metric.BackendID;
                                                    metric.EntityType = metric.BackendType;
                                                }
                                            }
                                        }
                                        // Call to another tier:
                                        // Overall Application Performance|Order-Processing-Services|External Calls|Call-HTTP to Fulfillment-Services|Calls per Minute
                                        else
                                        {
                                            int splitterIndex = metric.Segment4.LastIndexOf(" to ");
                                            if (splitterIndex > 0)
                                            {
                                                string targetTierName = metric.Segment4.Substring(splitterIndex + 4);
                                                if (tiersDictionary.TryGetValue(targetTierName, out tier) == true)
                                                {
                                                    metric.BackendName = tier.TierName;
                                                    metric.BackendID = tier.TierID;
                                                    metric.BackendType = "Tier";

                                                    metric.EntityName = metric.BackendName;
                                                    metric.EntityID = metric.BackendID;
                                                    metric.EntityType = tier.AgentType;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        #endregion

                        #region Business Transaction Performance

                        if (metricsBTList != null)
                        {
                            foreach (Metric metric in metricsBTList)
                            {
                                // Ignore Business Transaction Groups, only focus on Business Transactions
                                if (metric.Segment2 == "Business Transactions")
                                {
                                    // Business Transaction Performance|Business Transactions|ECommerce-Services|Shipping Address|Calls per Minute
                                    //                                                        ^^^^^^^^^^^^^^^^^^
                                    // Tier name is in third segment
                                    APMTier tier = null;
                                    if (tiersDictionary.TryGetValue(metric.Segment3, out tier) == true)
                                    {
                                        metric.TierName = tier.TierName;
                                        metric.TierID = tier.TierID;
                                        metric.TierAgentType = tier.AgentType;
                                    }

                                    // Business Transaction Performance|Business Transactions|ECommerce-Services|Shipping Address|Calls per Minute
                                    //                                                                           ^^^^^^^^^^^^^^^^
                                    // Business Transaction name is in fourth segment
                                    APMBusinessTransaction businessTransaction = null;
                                    if (businessTransactionsDictionary.TryGetValue(String.Format(@"{0}\{1}", metric.TierName, metric.Segment4), out businessTransaction) == true)
                                    {
                                        metric.BTName = businessTransaction.BTName;
                                        metric.BTID = businessTransaction.BTID;
                                        metric.BTType = businessTransaction.BTType;

                                        metric.EntityName = metric.BTName;
                                        metric.EntityID = metric.BTID;
                                        metric.EntityType = metric.BTType;
                                    }

                                    // Business Transaction Performance|Business Transactions|ECommerce-Services|/appdynamicspilot/..;/manager/|Individual Nodes|ECommerce_WEB2_NODE|Calls per Minute
                                    //                                                                                                          ^^^^^^^^^^^^^^^^
                                    //                                                                                                                           ^^^^^^^^^^^^^^^^^^^
                                    // Node name is behind Individual Nodes
                                    if (String.Compare(metric.Segment5, "Individual Nodes", true) == 0)
                                    {
                                        APMNode node = null;
                                        if (nodesDictionary.TryGetValue(String.Format(@"{0}\{1}", metric.TierName, metric.Segment6), out node) == true)
                                        {
                                            metric.NodeName = node.NodeName;
                                            metric.NodeID = node.NodeID;
                                            metric.NodeAgentType = node.AgentType;

                                            metric.EntityName = metric.NodeName;
                                            metric.EntityID = metric.NodeID;
                                            metric.EntityType = metric.NodeAgentType;
                                        }

                                        // Business Transaction Performance|Business Transactions|ECommerce-Services|/.GET|Individual Nodes|ECommerce_WEB2_NODE|External Calls|Call-JDBC to Discovered backend call - XE-Oracle-ORACLE-DB-Oracle Database 11g Express Edition Release 11.2.0.2.0 - 64bit Production|Errors per Minute
                                        //                                                                                 ^^^^^^^^^^^^^^^^
                                        //                                                                                                  ^^^^^^^^^^^^^^^^^^^
                                        //                                                                                                                                                                            ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
                                        // Backend name is behind External Calls
                                        if (String.Compare(metric.Segment7, "External Calls", true) == 0)
                                        {
                                            // Call to another application:
                                            // Business Transaction Performance|Business Transactions|Order-Processing-Services|_APPDYNAMICS_DEFAULT_TX_|Individual Nodes|ECommerce_JMS_NODE|External Calls|Call-HTTP to External Application - ECommerce-Fulfillment|Calls per Minute
                                            if (metric.Segment8.Contains(" to External Application") == true)
                                            {
                                                int splitterIndex = metric.Segment8.IndexOf(" - ");
                                                if (splitterIndex > 0)
                                                {
                                                    string targetApplicationName = metric.Segment8.Substring(splitterIndex + 3);
                                                    metric.BackendName = targetApplicationName;
                                                    metric.BackendID = -1;
                                                    metric.BackendType = "Application";

                                                    metric.EntityName = metric.BackendName;
                                                    metric.EntityID = metric.BackendID;
                                                    metric.EntityType = metric.BackendType;
                                                }
                                            }
                                            // Call to backend:
                                            // Business Transaction Performance|Business Transactions|ECommerce-Services|/.GET|Individual Nodes|ECommerce_WEB1_NODE|External Calls|Call-JDBC to Discovered backend call - XE-Oracle-ORACLE-DB-Oracle Database 11g Express Edition Release 11.2.0.2.0 - 64bit Production|Calls per Minute
                                            else if (metric.Segment8.Contains(" to Discovered backend call") == true)
                                            {
                                                int splitterIndex = metric.Segment8.IndexOf(" - ");
                                                if (splitterIndex > 0)
                                                {
                                                    string targetBackendName = metric.Segment8.Substring(splitterIndex + 3);
                                                    APMBackend backend = null;
                                                    if (backendDictionary.TryGetValue(targetBackendName, out backend) == true)
                                                    {
                                                        metric.BackendName = backend.BackendName;
                                                        metric.BackendID = backend.BackendID;
                                                        metric.BackendType = backend.BackendType;

                                                        metric.EntityName = metric.BackendName;
                                                        metric.EntityID = metric.BackendID;
                                                        metric.EntityType = metric.BackendType;
                                                    }
                                                }
                                            }
                                            // Call to another tier:
                                            // Overall Application Performance|Order-Processing-Services|Individual Nodes|ECommerce_JMS_NODE|External Calls|Call-HTTP to Fulfillment-Services|Errors per Minute
                                            else
                                            {
                                                int splitterIndex = metric.Segment8.LastIndexOf(" to ");
                                                if (splitterIndex > 0)
                                                {
                                                    string targetTierName = metric.Segment8.Substring(splitterIndex + 4);
                                                    if (tiersDictionary.TryGetValue(targetTierName, out tier) == true)
                                                    {
                                                        metric.BackendName = tier.TierName;
                                                        metric.BackendID = tier.TierID;
                                                        metric.BackendType = "Tier";

                                                        metric.EntityName = metric.BackendName;
                                                        metric.EntityID = metric.BackendID;
                                                        metric.EntityType = tier.AgentType;
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    // Business Transaction Performance|Business Transactions|ECommerce-Services|/user/.POST|External Calls|Call-JDBC to Discovered backend call - APPDY-MySQL-DB-5.5.5-10.1.41-MariaDB|Errors per Minute
                                    //                                                                                       ^^^^^^^^^^^^^^
                                    //                                                                                                                                             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
                                    // Backend name is behind External Calls
                                    if (String.Compare(metric.Segment5, "External Calls", true) == 0)
                                    {
                                        // Call to another application:
                                        // Business Transaction Performance|Business Transactions|Order-Processing-Services|FulfillmentConsumer:fulfillmentQueue|External Calls|Call-HTTP to External Application - ECommerce-Fulfillment|Calls per Minute
                                        if (metric.Segment6.Contains(" to External Application") == true)
                                        {
                                            int splitterIndex = metric.Segment6.IndexOf(" - ");
                                            if (splitterIndex > 0)
                                            {
                                                string targetApplicationName = metric.Segment6.Substring(splitterIndex + 3);
                                                metric.BackendName = targetApplicationName;
                                                metric.BackendID = -1;
                                                metric.BackendType = "Application";

                                                metric.EntityName = metric.BackendName;
                                                metric.EntityID = metric.BackendID;
                                                metric.EntityType = metric.BackendType;
                                            }
                                        }

                                        // Call to backend:
                                        // Business Transaction Performance|Business Transactions|ECommerce-Services|/{id}.GET|External Calls|Call-JDBC to Discovered backend call - APPDY-MySQL-DB-5.5.5-10.1.38-MariaDB|Calls per Minute
                                        else if (metric.Segment6.Contains(" to Discovered backend call") == true)
                                        {
                                            int splitterIndex = metric.Segment6.IndexOf(" - ");
                                            if (splitterIndex > 0)
                                            {
                                                string targetBackendName = metric.Segment6.Substring(splitterIndex + 3);
                                                APMBackend backend = null;
                                                if (backendDictionary.TryGetValue(targetBackendName, out backend) == true)
                                                {
                                                    metric.BackendName = backend.BackendName;
                                                    metric.BackendID = backend.BackendID;
                                                    metric.BackendType = backend.BackendType;

                                                    metric.EntityName = metric.BackendName;
                                                    metric.EntityID = metric.BackendID;
                                                    metric.EntityType = metric.BackendType;
                                                }
                                            }
                                        }

                                        // Call to another tier:
                                        // Business Transaction Performance|Business Transactions|ECommerce-Services|Checkout|External Calls|Call-HTTP to Inventory-Services|Calls per Minute
                                        // or
                                        // Business Transaction Performance|Business Transactions|ECommerce-Services|Checkout|External Calls|Call-HTTP to Inventory-Services|Inventory-Services|Calls per Minute
                                        else
                                        {
                                            int splitterIndex = metric.Segment6.LastIndexOf(" to ");
                                            if (splitterIndex > 0)
                                            {
                                                string targetTierName = metric.Segment6.Substring(splitterIndex + 4);
                                                if (tiersDictionary.TryGetValue(targetTierName, out tier) == true)
                                                {
                                                    metric.BackendName = tier.TierName;
                                                    metric.BackendID = tier.TierID;
                                                    metric.BackendType = "Tier";

                                                    metric.EntityName = metric.BackendName;
                                                    metric.EntityID = metric.BackendID;
                                                    metric.EntityType = tier.AgentType;
                                                }
                                            }
                                        }

                                        // Business Transaction Performance|Business Transactions|ECommerce-Services|Checkout|External Calls|Call-WEB_SERVICE to Inventory-Services|Inventory-Services|Individual Nodes|ECommerce_WS_NODE|Calls per Minute
                                        // Getting deep here
                                        if (String.Compare(metric.Segment8, "Individual Nodes", true) == 0)
                                        {
                                            APMNode node = null;
                                            if (nodesDictionary.TryGetValue(String.Format(@"{0}\{1}", metric.Segment7, metric.Segment9), out node) == true)
                                            {
                                                metric.NodeName = node.NodeName;
                                                metric.NodeID = node.NodeID;
                                                metric.NodeAgentType = node.AgentType;

                                                metric.EntityName = metric.NodeName;
                                                metric.EntityID = metric.NodeID;
                                                metric.EntityType = metric.NodeAgentType;
                                            }
                                        }

                                        if (String.Compare(metric.Segment8, "External Calls", true) == 0)
                                        {
                                            // Call to another application:
                                            // ??
                                            if (metric.Segment9.Contains(" to External Application") == true)
                                            {
                                                int splitterIndex = metric.Segment9.IndexOf(" - ");
                                                if (splitterIndex > 0)
                                                {
                                                    string targetApplicationName = metric.Segment9.Substring(splitterIndex + 3);
                                                    metric.BackendName = targetApplicationName;
                                                    metric.BackendID = -1;
                                                    metric.BackendType = "Application";

                                                    metric.EntityName = metric.BackendName;
                                                    metric.EntityID = metric.BackendID;
                                                    metric.EntityType = metric.BackendType;
                                                }
                                            }

                                            // Call to backend:
                                            // Business Transaction Performance|Business Transactions|ECommerce-Services|Checkout|External Calls|Call-WEB_SERVICE to Inventory-Services|Inventory-Services|External Calls|Call-JDBC to Discovered backend call - XE-Oracle-ORACLE-DB-Oracle Database 11g Express Edition Release 11.2.0.2.0 - 64bit Production|Calls per Minute
                                            else if (metric.Segment9.Contains(" to Discovered backend call") == true)
                                            {
                                                int splitterIndex = metric.Segment9.IndexOf(" - ");
                                                if (splitterIndex > 0)
                                                {
                                                    string targetBackendName = metric.Segment9.Substring(splitterIndex + 3);
                                                    APMBackend backend = null;
                                                    if (backendDictionary.TryGetValue(targetBackendName, out backend) == true)
                                                    {
                                                        metric.BackendName = backend.BackendName;
                                                        metric.BackendID = backend.BackendID;
                                                        metric.BackendType = backend.BackendType;

                                                        metric.EntityName = metric.BackendName;
                                                        metric.EntityID = metric.BackendID;
                                                        metric.EntityType = metric.BackendType;
                                                    }
                                                }
                                            }

                                            // Call to another tier:
                                            // Business Transaction Performance|Business Transactions|ECommerce-Services|Checkout|External Calls|Call-JMS to Discovered backend call - Active MQ-fulfillmentQueue|Discovered backend call - Active MQ-fulfillmentQueue|External Calls|Call-JMS to Order-Processing-Services|Order-Processing-Services|Calls per Minute
                                            else
                                            {
                                                int splitterIndex = metric.Segment9.LastIndexOf(" to ");
                                                if (splitterIndex > 0)
                                                {
                                                    string targetTierName = metric.Segment9.Substring(splitterIndex + 4);
                                                    if (tiersDictionary.TryGetValue(targetTierName, out tier) == true)
                                                    {
                                                        metric.BackendName = tier.TierName;
                                                        metric.BackendID = tier.TierID;
                                                        metric.BackendType = "Tier";

                                                        metric.EntityName = metric.BackendName;
                                                        metric.EntityID = metric.BackendID;
                                                        metric.EntityType = tier.AgentType;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        #endregion

                        #region Backends

                        if (metricsBACKENDList != null)
                        {
                            foreach (Metric metric in metricsBACKENDList)
                            {
                                // Backends|Discovered backend call - INVENTORY-MySQL DB-DB-5.5.5-10.1.45-MariaDB-1~bionic|Calls per Minute
                                //                                    ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
                                // Backend name is here
                                if (metric.Segment2.StartsWith("Discovered backend call - ") == true)
                                {
                                    int splitterIndex = metric.Segment2.IndexOf(" - ");
                                    if (splitterIndex > 0)
                                    {
                                        string targetBackendName = metric.Segment2.Substring(splitterIndex + 3);
                                        APMBackend backend = null;
                                        if (backendDictionary.TryGetValue(targetBackendName, out backend) == true)
                                        {
                                            metric.BackendName = backend.BackendName;
                                            metric.BackendID = backend.BackendID;
                                            metric.BackendType = backend.BackendType;

                                            metric.EntityName = metric.BackendName;
                                            metric.EntityID = metric.BackendID;
                                            metric.EntityType = metric.BackendType;
                                        }
                                    }
                                }
                                // Backends|External Application - ECommerce-Fulfillment|Calls per Minute
                                else if (metric.Segment2.StartsWith("External Application - ") == true)
                                {
                                    int splitterIndex = metric.Segment2.IndexOf(" - ");
                                    if (splitterIndex > 0)
                                    {
                                        string targetApplicationName = metric.Segment2.Substring(splitterIndex + 3);
                                        metric.BackendName = targetApplicationName;
                                        metric.BackendID = -1;
                                        metric.BackendType = "Application";

                                        metric.EntityName = metric.BackendName;
                                        metric.EntityID = metric.BackendID;
                                        metric.EntityType = metric.BackendType;
                                    }
                                }
                                // Backends|Inventory-Services|Calls per Minute
                                //          ^^^^^^^^^^^^^^^^^^
                                // Tier name is here
                                else
                                {
                                    APMTier tier = null;
                                    if (tiersDictionary.TryGetValue(metric.Segment2, out tier) == true)
                                    {
                                        metric.TierName = tier.TierName;
                                        metric.TierID = tier.TierID;
                                        metric.TierAgentType = tier.AgentType;

                                        metric.EntityName = metric.TierName;
                                        metric.EntityID = metric.TierID;
                                        metric.EntityType = metric.TierAgentType;
                                    }
                                }
                            }
                        }

                        #endregion

                        #region Service Endpoints

                        if (metricsSEPList != null)
                        {
                            foreach (Metric metric in metricsSEPList)
                            {
                                // Service Endpoints|ECommerce-Services|ViewCart.paymentinfo|Calls per Minute
                                //                   ^^^^^^^^^^^^^^^^^^
                                // Tier name is in second segment
                                APMTier tier = null;
                                if (tiersDictionary.TryGetValue(metric.Segment2, out tier) == true)
                                {
                                    metric.TierName = tier.TierName;
                                    metric.TierID = tier.TierID;
                                    metric.TierAgentType = tier.AgentType;
                                }

                                // Service Endpoints|ECommerce-Services|ViewCart.paymentinfo|Calls per Minute
                                //                                      ^^^^^^^^^^^^^^^^^^^^
                                // Service endpoint name is in third segment
                                APMServiceEndpoint serviceEndpoint = null;
                                if (serviceEndpointDictionary.TryGetValue(String.Format(@"{0}\{1}", metric.TierName, metric.Segment3), out serviceEndpoint) == true)
                                {
                                    metric.SEPName = serviceEndpoint.SEPName;
                                    metric.SEPID = serviceEndpoint.SEPID;
                                    metric.SEPType = serviceEndpoint.SEPType;

                                    metric.EntityName = metric.SEPName;
                                    metric.EntityID = metric.SEPID;
                                    metric.EntityType = metric.SEPType;
                                }

                                // Service Endpoints|ECommerce-Services|/appdynamicspilot/rest/items/sddqsdq|Individual Nodes|ECommerce_WEB1_NODE|Calls per Minute
                                //                                                                           ^^^^^^^^^^^^^^^^
                                //                                                                                            ^^^^^^^^^^^^^^^^^^^
                                // Node name is behind Individual Nodes
                                if (String.Compare(metric.Segment4, "Individual Nodes", true) == 0)
                                {
                                    APMNode node = null;
                                    if (nodesDictionary.TryGetValue(String.Format(@"{0}\{1}", metric.TierName, metric.Segment5), out node) == true)
                                    {
                                        metric.NodeName = node.NodeName;
                                        metric.NodeID = node.NodeID;
                                        metric.NodeAgentType = node.AgentType;

                                        metric.EntityName = metric.NodeName;
                                        metric.EntityID = metric.NodeID;
                                        metric.EntityType = metric.NodeAgentType;
                                    }
                                }
                            }
                        }

                        #endregion

                        #region Errors

                        if (metricsERRList != null)
                        {
                            foreach (Metric metric in metricsERRList)
                            {
                                // Errors|ECommerce-Services|ServletException : DatabaseException : CommunicationsException : EOFException|Number of Errors
                                //        ^^^^^^^^^^^^^^^^^^
                                // Tier name is in second segment
                                APMTier tier = null;
                                if (tiersDictionary.TryGetValue(metric.Segment2, out tier) == true)
                                {
                                    metric.TierName = tier.TierName;
                                    metric.TierID = tier.TierID;
                                    metric.TierAgentType = tier.AgentType;
                                }

                                // Errors|ECommerce-Services|ServletException : DatabaseException : CommunicationsException : EOFException|Number of Errors
                                //                                      ^^^^^^^^^^^^^^^^^^^^
                                // Error name is in third segment
                                APMError error = null;
                                if (errorDictionary.TryGetValue(String.Format(@"{0}\{1}", metric.TierName, metric.Segment3), out error) == true)
                                {
                                    metric.ErrorName = error.ErrorName;
                                    metric.ErrorID = error.ErrorID;
                                    metric.ErrorType = error.ErrorType;

                                    metric.EntityName = metric.ErrorName;
                                    metric.EntityID = metric.ErrorID;
                                    metric.EntityType = metric.ErrorType;
                                }

                                // Errors|ECommerce-Services|PersistenceException : DatabaseException : SQLNestedException : NoSuchElementException|Individual Nodes|ECommerce_WEB1_NODE|Business Transactions|Login|Number of Errors
                                //                                                                                                                  ^^^^^^^^^^^^^^^^
                                //                                                                                                                                   ^^^^^^^^^^^^^^^^^^^
                                // Node name is behind Individual Nodes
                                if (String.Compare(metric.Segment4, "Individual Nodes", true) == 0)
                                {
                                    APMNode node = null;
                                    if (nodesDictionary.TryGetValue(String.Format(@"{0}\{1}", metric.TierName, metric.Segment5), out node) == true)
                                    {
                                        metric.NodeName = node.NodeName;
                                        metric.NodeID = node.NodeID;
                                        metric.NodeAgentType = node.AgentType;

                                        metric.EntityName = metric.NodeName;
                                        metric.EntityID = metric.NodeID;
                                        metric.EntityType = metric.NodeAgentType;
                                    }

                                    if (String.Compare(metric.Segment6, "Business Transactions", true) == 0)
                                    {
                                        // Errors|ECommerce-Services|PersistenceException : DatabaseException : SQLNestedException : NoSuchElementException|Individual Nodes|ECommerce_WEB1_NODE|Business Transactions|Login|Number of Errors
                                        //                                                                                                                                                                             ^^^^^
                                        // Business Transaction name is in seventh segment
                                        APMBusinessTransaction businessTransaction = null;
                                        if (businessTransactionsDictionary.TryGetValue(String.Format(@"{0}\{1}", metric.TierName, metric.Segment7), out businessTransaction) == true)
                                        {
                                            // BT Originated in this tier
                                            metric.BTName = businessTransaction.BTName;
                                            metric.BTID = businessTransaction.BTID;
                                            metric.BTType = businessTransaction.BTType;

                                            metric.EntityName = metric.BTName;
                                            metric.EntityID = metric.BTID;
                                            metric.EntityType = metric.BTType;
                                        }
                                        else if (businessTransactionsDictionary.TryGetValue(businessTransactionsDictionary.Keys.Where(k => k.EndsWith(String.Format(@"\{0}", metric.Segment7))).FirstOrDefault(), out businessTransaction) == true)
                                        {
                                            // BT is originated from another tier
                                            metric.BTName = businessTransaction.BTName;
                                            metric.BTID = businessTransaction.BTID;
                                            metric.BTType = businessTransaction.BTType;

                                            metric.EntityName = metric.BTName;
                                            metric.EntityID = metric.BTID;
                                            metric.EntityType = metric.BTType;
                                        }
                                    }
                                }

                                if (String.Compare(metric.Segment4, "Business Transactions", true) == 0)
                                {
                                    // Errors|Inventory-Services|WebServiceException : WstxIOException : ClientAbortException : SocketException|Business Transactions|Checkout|Errors per Minute
                                    //                                                                                                                                                                             ^^^^^
                                    // Business Transaction name is in fourthsegment
                                    APMBusinessTransaction businessTransaction = null;
                                    if (businessTransactionsDictionary.TryGetValue(String.Format(@"{0}\{1}", metric.TierName, metric.Segment5), out businessTransaction) == true)
                                    {
                                        metric.BTName = businessTransaction.BTName;
                                        metric.BTID = businessTransaction.BTID;
                                        metric.BTType = businessTransaction.BTType;

                                        metric.EntityName = metric.BTName;
                                        metric.EntityID = metric.BTID;
                                        metric.EntityType = metric.BTType;
                                    }
                                }

                            }
                        }

                        #endregion

                        #region Information Points

                        if (metricsIPList != null)
                        {
                            foreach (Metric metric in metricsIPList)
                            {
                                // Information Points|HTTP 5xx Errors|Calls per Minute
                                //                    ^^^^^^^^^^^^^^^
                                // Information Point is here
                                APMInformationPoint informationPoint = null;
                                if (informationPointDictionary.TryGetValue(metric.Segment2, out informationPoint) == true)
                                {
                                    metric.IPName = informationPoint.IPName;
                                    metric.IPID = informationPoint.IPID;
                                    metric.IPType = informationPoint.IPType;

                                    metric.EntityName = metric.IPName;
                                    metric.EntityID = metric.IPID;
                                    metric.EntityType = metric.IPType;
                                }
                            }
                        }

                        #endregion
                        #endregion

                        #region Create summary rollups

                        loggerConsole.Info("Count metric summaries");

                        // We create WAY too many metrics, sometimes too many to fit into a single sheet
                        // Let's summarize them by the root segment

                        List<MetricSummary> metricSummaryList = new List<MetricSummary>(9);
                        if (metricsINFRAList != null && metricsINFRAList.Count > 0) metricSummaryList.Add(getMetricSummary(metricsINFRAList));
                        if (metricsAPPList != null && metricsAPPList.Count > 0) metricSummaryList.Add(getMetricSummary(metricsAPPList));
                        if (metricsBACKENDList != null && metricsBACKENDList.Count > 0) metricSummaryList.Add(getMetricSummary(metricsBACKENDList));
                        if (metricsBTList != null && metricsBTList.Count > 0) metricSummaryList.Add(getMetricSummary(metricsBTList));
                        if (metricsSEPList != null && metricsSEPList.Count > 0) metricSummaryList.Add(getMetricSummary(metricsSEPList));
                        if (metricsERRList != null && metricsERRList.Count > 0) metricSummaryList.Add(getMetricSummary(metricsERRList));
                        if (metricsIPList != null && metricsIPList.Count > 0) metricSummaryList.Add(getMetricSummary(metricsIPList));
                        if (metricsWEBList != null && metricsWEBList.Count > 0) metricSummaryList.Add(getMetricSummary(metricsWEBList));
                        if (metricsMOBILEList != null && metricsMOBILEList.Count > 0) metricSummaryList.Add(getMetricSummary(metricsMOBILEList));

                        foreach (MetricSummary metricSummary in metricSummaryList)
                        {
                            metricSummary.MetricsListLink = String.Format(@"=HYPERLINK(""{0}"", ""<MetricsList>"")", FilePathMap.MetricsListApplicationExcelReportFilePath(jobTarget, false));
                        }

                        FileIOHelper.WriteListToCSVFile(metricSummaryList, new MetricSummaryReportMap(), FilePathMap.MetricPrefixSummaryIndexFilePath(jobTarget));

                        #endregion

                        #region Save results

                        loggerConsole.Info("Save metrics");

                        List<Metric> metricsList = new List<Metric>(metricSummaryList.Sum(m => m.NumActivity) + metricSummaryList.Sum(m => m.NumNoActivity));
                        if (metricsINFRAList != null) metricsList.AddRange(metricsINFRAList);
                        if (metricsAPPList != null) metricsList.AddRange(metricsAPPList);
                        if (metricsBACKENDList != null) metricsList.AddRange(metricsBACKENDList);
                        if (metricsBTList != null) metricsList.AddRange(metricsBTList);
                        if (metricsSEPList != null) metricsList.AddRange(metricsSEPList);
                        if (metricsERRList != null) metricsList.AddRange(metricsERRList);
                        if (metricsIPList != null) metricsList.AddRange(metricsIPList);
                        if (metricsWEBList != null) metricsList.AddRange(metricsWEBList);
                        if (metricsMOBILEList != null) metricsList.AddRange(metricsMOBILEList);

                        FileIOHelper.WriteListToCSVFile(metricsList, new MetricReportMap(), FilePathMap.MetricsListIndexFilePath(jobTarget));
                        FileIOHelper.WriteListToCSVFile(metricsList, new MetricReportMap(), FilePathMap.MetricsListApplicationReportFilePath(jobTarget));

                        #endregion

                        #region Combine All for Report CSV

                        // If it is the first one, clear out the combined folder
                        if (reportFolderCleaned == false)
                        {
                            FileIOHelper.DeleteFolder(FilePathMap.APMMetricsListReportFolderPath());
                            Thread.Sleep(1000);
                            FileIOHelper.CreateFolder(FilePathMap.APMMetricsListReportFolderPath());
                            reportFolderCleaned = true;
                        }

                        // Append all the individual report files into one
                        if (File.Exists(FilePathMap.MetricsListIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.MetricsListIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.MetricsListReportFilePath(), FilePathMap.MetricsListIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.MetricPrefixSummaryIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.MetricPrefixSummaryIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.MetricPrefixSummaryReportFilePath(), FilePathMap.MetricPrefixSummaryIndexFilePath(jobTarget));
                        }

                        #endregion
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex);
                        loggerConsole.Warn(ex);

                        return false;
                    }
                    finally
                    {
                        stopWatchTarget.Stop();

                        this.DisplayJobTargetEndedStatus(jobConfiguration, jobTarget, i + 1, stopWatchTarget);

                        stepTimingTarget.EndTime = DateTime.Now;
                        stepTimingTarget.Duration = stopWatchTarget.Elapsed;
                        stepTimingTarget.DurationMS = stopWatchTarget.ElapsedMilliseconds;

                        List<StepTiming> stepTimings = new List<StepTiming>(1);
                        stepTimings.Add(stepTimingTarget);
                        FileIOHelper.WriteListToCSVFile(stepTimings, new StepTimingReportMap(), FilePathMap.StepTimingReportFilePath(), true);
                    }
                }

                 return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                loggerConsole.Error(ex);

                return false;
            }
            finally
            {
                stopWatch.Stop();

                this.DisplayJobStepEndedStatus(jobConfiguration, stopWatch);

                stepTimingFunction.EndTime = DateTime.Now;
                stepTimingFunction.Duration = stopWatch.Elapsed;
                stepTimingFunction.DurationMS = stopWatch.ElapsedMilliseconds;

                List<StepTiming> stepTimings = new List<StepTiming>(1);
                stepTimings.Add(stepTimingFunction);
                FileIOHelper.WriteListToCSVFile(stepTimings, new StepTimingReportMap(), FilePathMap.StepTimingReportFilePath(), true);
            }
        }

        private List<Metric> parseListOfMetrics(
            JobTarget jobTarget,
            JobConfiguration jobConfiguration,
            string fileNamePrefix,
            int maxDepth)
        {
            loggerConsole.Info("Prefix {0} with depth {1}", fileNamePrefix, maxDepth);

            List<Metric> metricsList = new List<Metric>(10240);

            for (int currentDepth = 0; currentDepth <= maxDepth; currentDepth++)
            {
                List<AppDRESTMetric> metricData = FileIOHelper.LoadListOfObjectsFromFile<AppDRESTMetric>(FilePathMap.MetricsListDataFilePath(jobTarget, fileNamePrefix, currentDepth));
                if (metricData != null)
                {
                    foreach (AppDRESTMetric appDRESTMetric in metricData)
                    {
                        #region Get metric path components and metric name

                        // Analyze metric path returned by the call to controller
                        string[] metricPathComponents = appDRESTMetric.metricPath.Split('|');

                        if (metricPathComponents.Length == 0)
                        {
                            // Metric name was no good
                            logger.Warn("Metric path='{0}' could not be parsed into individual components", appDRESTMetric.metricPath);
                            continue;
                        }

                        string[] metricNameComponents = appDRESTMetric.metricName.Split('|');

                        if (metricNameComponents.Length == 0)
                        {
                            // Metric name was no good
                            logger.Warn("Metric name='{0}' could not be parsed into individual components", appDRESTMetric.metricName);
                            // continue;
                        }

                        #endregion

                        Metric metric = new Metric();
                        metric.Controller = jobTarget.Controller;
                        metric.ApplicationName = jobTarget.Application;
                        metric.ApplicationID = jobTarget.ApplicationID;

                        metric.MetricPath = appDRESTMetric.metricPath;
                        // Name of the metric is always the last one in the metric path
                        metric.MetricName = metricPathComponents[metricPathComponents.Length - 1];
                        metric.MetricID = appDRESTMetric.metricId;
                        
                        if (appDRESTMetric.metricValues.Count() > 0)
                        {
                            AppDRESTMetricValue appDRESTMetricValue = appDRESTMetric.metricValues[0];
                            metric.HasActivity = appDRESTMetricValue.value > 0;
                        }

                        metric.NumSegments = metricPathComponents.Length;

                        for (int i = 0; i < metricPathComponents.Length; i++)
                        {
                            switch (i)
                            { 
                                case 0:
                                    metric.Segment1 = metricPathComponents[i];
                                    break;

                                case 1:
                                    metric.Segment2 = metricPathComponents[i];
                                    break;

                                case 2:
                                    metric.Segment3 = metricPathComponents[i];
                                    break;

                                case 3:
                                    metric.Segment4 = metricPathComponents[i];
                                    break;

                                case 4:
                                    metric.Segment5 = metricPathComponents[i];
                                    break;

                                case 5:
                                    metric.Segment6 = metricPathComponents[i];
                                    break;

                                case 6:
                                    metric.Segment7 = metricPathComponents[i];
                                    break;

                                case 7:
                                    metric.Segment8 = metricPathComponents[i];
                                    break;

                                case 8:
                                    metric.Segment9 = metricPathComponents[i];
                                    break;

                                case 9:
                                    metric.Segment10 = metricPathComponents[i];
                                    break;

                                case 10:
                                    metric.Segment11 = metricPathComponents[i];
                                    break;

                                case 11:
                                    metric.Segment12 = metricPathComponents[i];
                                    break;

                                case 12:
                                    metric.Segment13 = metricPathComponents[i];
                                    break;

                                case 13:
                                    metric.Segment14 = metricPathComponents[i];
                                    break;

                                case 14:
                                    metric.Segment15 = metricPathComponents[i];
                                    break;

                                case 15:
                                    metric.Segment16 = metricPathComponents[i];
                                    break;

                                case 16:
                                    metric.Segment17 = metricPathComponents[i];
                                    break;

                                case 17:
                                    metric.Segment18 = metricPathComponents[i];
                                    break;

                                case 18:
                                    metric.Segment19 = metricPathComponents[i];
                                    break;

                                case 19:
                                    metric.Segment20 = metricPathComponents[i];
                                    break;

                                case 20:
                                    metric.Segment21 = metricPathComponents[i];
                                    break;

                                case 21:
                                    metric.Segment22 = metricPathComponents[i];
                                    break;

                                default:
                                    break;
                            }
                        }

                        switch (metric.Segment1)
                        {
                            case "Overall Application Performance":

                                #region Overall Application Performance - App, Tier, Node


                                #endregion

                                break;

                            case "Application Infrastructure Performance":

                                #region Aplication Infrastructure Performance - Tier, Node

                                #endregion

                                break;

                            case "Business Transaction Performance":

                                #region Business Transaction Performance

                                #endregion

                                break;

                            case "Backends":

                                #region Backends
                                #endregion

                                break;

                            case "Errors":

                                #region Errors
                                
                                #endregion

                                break;

                            case "Service Endpoints":

                                #region Service Endpoints

                                #endregion

                                break;

                            case "Information Points":

                                #region Information Points

                                #endregion

                                break;

                            case "Mobile":

                                #region Mobile End User Experience

                                #endregion

                                break;

                            case "End User Experience":

                                #region Web End User Experience

                                #endregion

                                break;

                            default:
                                break;
                        }

                        metricsList.Add(metric);
                    }
                }
            }

            loggerConsole.Trace("Prefix {0} with depth {1} produced {2} metrics", fileNamePrefix, maxDepth, metricsList.Count);

            return metricsList;
        }

        private MetricSummary getMetricSummary(List<Metric> listOfMetrics)
        {
            MetricSummary metricSummary = new MetricSummary();
            metricSummary.Controller = listOfMetrics[0].Controller;
            metricSummary.ApplicationName = listOfMetrics[0].ApplicationName;
            metricSummary.ApplicationID = listOfMetrics[0].ApplicationID;

            metricSummary.MetricPrefix = listOfMetrics[0].Segment1;

            metricSummary.NumAll = listOfMetrics.Count;
            metricSummary.NumActivity = listOfMetrics.Count(m => m.HasActivity == true);
            metricSummary.NumNoActivity = metricSummary.NumAll - metricSummary.NumActivity;

            return metricSummary;
        }

        public override bool ShouldExecute(ProgramOptions programOptions, JobConfiguration jobConfiguration)
        {
            logger.Trace("LicensedReports.EntityMetrics={0}", programOptions.LicensedReports.MetricsList);
            loggerConsole.Trace("LicensedReports.EntityMetrics={0}", programOptions.LicensedReports.MetricsList);
            if (programOptions.LicensedReports.MetricsList == false)
            {
                loggerConsole.Warn("Not licensed for list of metrics");
                return false;
            }

            logger.Trace("Input.MetricsList={0}", jobConfiguration.Input.MetricsList);
            loggerConsole.Trace("Input.MetricsList={0}", jobConfiguration.Input.MetricsList);
            if (jobConfiguration.Input.MetricsList == false)
            {
                loggerConsole.Trace("Skipping index of list of metrics");
            }
            return (jobConfiguration.Input.MetricsList == true);
        }
    }
}
