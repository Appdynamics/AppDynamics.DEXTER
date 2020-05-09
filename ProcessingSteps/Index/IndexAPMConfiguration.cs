using AppDynamics.Dexter.ReportObjectMaps;
using AppDynamics.Dexter.ReportObjects;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class IndexAPMConfiguration : JobStepIndexBase
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Compiler", "CS0168", Justification = "Hiding JsonReaderException over reading potentially incorrect or empty JSON")]
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

                #region Template comparisons 

                // Check to see if the reference application is the template or specific application, and add one of them to the 
                if (jobConfiguration.Input.ConfigurationComparisonReferenceAPM.Controller == BLANK_APPLICATION_CONTROLLER &&
                    jobConfiguration.Input.ConfigurationComparisonReferenceAPM.Application == BLANK_APPLICATION_APM)
                {
                    jobConfiguration.Target.Add(jobConfiguration.Input.ConfigurationComparisonReferenceAPM);
                }
                else
                {
                    // Check if there is a valid reference application
                    JobTarget jobTargetReferenceApp = jobConfiguration.Target.Where(t =>
                        t.Type == APPLICATION_TYPE_APM &&
                        String.Compare(t.Controller, jobConfiguration.Input.ConfigurationComparisonReferenceAPM.Controller, StringComparison.InvariantCultureIgnoreCase) == 0 &&
                        String.Compare(t.Application, jobConfiguration.Input.ConfigurationComparisonReferenceAPM.Application, StringComparison.InvariantCultureIgnoreCase) == 0).FirstOrDefault();
                    if (jobTargetReferenceApp == null)
                    {
                        // No valid reference, fall back to comparing against template
                        logger.Warn("Unable to find reference target {0}, will index default template", jobConfiguration.Input.ConfigurationComparisonReferenceAPM);
                        loggerConsole.Warn("Unable to find reference target {0}, will index default template", jobConfiguration.Input.ConfigurationComparisonReferenceAPM);

                        jobConfiguration.Input.ConfigurationComparisonReferenceAPM.Controller = BLANK_APPLICATION_CONTROLLER;
                        jobConfiguration.Input.ConfigurationComparisonReferenceAPM.Application = BLANK_APPLICATION_APM;
                        jobConfiguration.Input.ConfigurationComparisonReferenceAPM.Type = APPLICATION_TYPE_APM;

                        jobConfiguration.Target.Add(jobConfiguration.Input.ConfigurationComparisonReferenceAPM);
                    }
                }

                #endregion

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

                        #region Preload list of detected entities

                        // For later cross-reference
                        List<APMTier> tiersList = FileIOHelper.ReadListFromCSVFile<APMTier>(FilePathMap.APMTiersReportFilePath(), new APMTierReportMap());
                        List<APMBackend> backendsList = FileIOHelper.ReadListFromCSVFile<APMBackend>(FilePathMap.APMBackendsReportFilePath(), new APMBackendReportMap());
                        List<APMBusinessTransaction> businessTransactionsList = FileIOHelper.ReadListFromCSVFile<APMBusinessTransaction>(FilePathMap.APMBusinessTransactionsReportFilePath(), new APMBusinessTransactionReportMap());
                        List<APMServiceEndpoint> serviceEndpointsList = FileIOHelper.ReadListFromCSVFile<APMServiceEndpoint>(FilePathMap.APMServiceEndpointsReportFilePath(), new APMServiceEndpointReportMap());

                        List<APMTier> tiersThisAppList = null;
                        List<APMBackend> backendsThisAppList = null;
                        List<APMBusinessTransaction> businessTransactionsThisAppList = null;
                        List<APMServiceEndpoint> serviceEndpointsThisAppList = null;

                        if (tiersList != null) tiersThisAppList = tiersList.Where(t => t.Controller.StartsWith(jobTarget.Controller) == true && t.ApplicationID == jobTarget.ApplicationID).ToList<APMTier>();
                        if (backendsList != null) backendsThisAppList = backendsList.Where(b => b.Controller.StartsWith(jobTarget.Controller) == true && b.ApplicationID == jobTarget.ApplicationID).ToList<APMBackend>();
                        if (businessTransactionsList != null) businessTransactionsThisAppList = businessTransactionsList.Where(b => b.Controller.StartsWith(jobTarget.Controller) == true && b.ApplicationID == jobTarget.ApplicationID).ToList<APMBusinessTransaction>();
                        if (serviceEndpointsList != null) serviceEndpointsThisAppList = serviceEndpointsList.Where(b => b.Controller.StartsWith(jobTarget.Controller) == true && b.ApplicationID == jobTarget.ApplicationID).ToList<APMServiceEndpoint>();

                        #endregion

                        #region Application Summary

                        loggerConsole.Info("Load Configuration file");

                        XmlDocument configXml = FileIOHelper.LoadXmlDocumentFromFile(FilePathMap.APMApplicationConfigurationXMLDataFilePath(jobTarget));
                        if (configXml == null)
                        {
                            logger.Warn("No application configuration in {0} file", FilePathMap.APMApplicationConfigurationXMLDataFilePath(jobTarget));
                            loggerConsole.Warn("No application configuration in {0} file", FilePathMap.APMApplicationConfigurationXMLDataFilePath(jobTarget));
                            continue;
                        }

                        loggerConsole.Info("Application Summary");

                        APMApplicationConfiguration applicationConfiguration = new APMApplicationConfiguration();
                        applicationConfiguration.Controller = jobTarget.Controller;
                        applicationConfiguration.ControllerLink = String.Format(DEEPLINK_CONTROLLER, applicationConfiguration.Controller, DEEPLINK_TIMERANGE_LAST_15_MINUTES);
                        //applicationConfiguration.ApplicationName = configXml.SelectSingleNode("application/name").InnerText;
                        applicationConfiguration.ApplicationName = jobTarget.Application;
                        applicationConfiguration.ApplicationID = jobTarget.ApplicationID;
                        applicationConfiguration.ApplicationLink = String.Format(DEEPLINK_APM_APPLICATION, applicationConfiguration.Controller, applicationConfiguration.ApplicationID, DEEPLINK_TIMERANGE_LAST_15_MINUTES);
                        applicationConfiguration.ApplicationDescription = configXml.SelectSingleNode("application/description").InnerText;

                        XmlAttribute mdsEnabledAttribute = configXml.SelectSingleNode("application").Attributes["mds-config-enabled"];
                        if (mdsEnabledAttribute != null)
                        {
                            applicationConfiguration.IsBT20ConfigEnabled = Convert.ToBoolean(mdsEnabledAttribute.Value);
                        }

                        if (configXml.SelectSingleNode("application/configuration/application-instrumentation-level").InnerText != "PRODUCTION")
                        {
                            applicationConfiguration.IsDeveloperModeEnabled = true;
                        }

                        applicationConfiguration.SnapshotEvalInterval = getIntegerValueFromXmlNode(configXml.SelectSingleNode("application/configuration/snapshot-evaluation-interval"));
                        applicationConfiguration.SnapshotQuietTime = getIntegerValueFromXmlNode(configXml.SelectSingleNode("application/configuration/snapshot-quiet-time-post-sla-failure"));
                        applicationConfiguration.IsHREngineEnabled = getBoolValueFromXmlNode(configXml.SelectSingleNode("application/configuration/policy-engine-enabled"));
                        applicationConfiguration.IsBTLockdownEnabled = getBoolValueFromXmlNode(configXml.SelectSingleNode("application/configuration/bt-discovery-locked"));
                        applicationConfiguration.IsAsyncSupported = getBoolValueFromXmlNode(configXml.SelectSingleNode("application/configuration/async-activity-supported"));

                        applicationConfiguration.BTSLAConfig = makeXMLFormattedAndIndented(configXml.SelectSingleNode("application/configuration/sla"));
                        applicationConfiguration.BTSnapshotCollectionConfig = makeXMLFormattedAndIndented(configXml.SelectSingleNode("application/configuration/business-transaction-config/snapshot-collection-policy"));
                        applicationConfiguration.BTRequestThresholdConfig = makeXMLFormattedAndIndented(configXml.SelectSingleNode("application/configuration/business-transaction-config/bt-request-thresholds"));
                        applicationConfiguration.BTBackgroundSnapshotCollectionConfig = makeXMLFormattedAndIndented(configXml.SelectSingleNode("application/configuration/background-business-transaction-config/snapshot-collection-policy"));
                        applicationConfiguration.BTBackgroundRequestThresholdConfig = makeXMLFormattedAndIndented(configXml.SelectSingleNode("application/configuration/background-business-transaction-config/bt-request-thresholds"));

                        applicationConfiguration.EUMConfigExclude = makeXMLFormattedAndIndented(configXml.SelectSingleNode("application/eum-cloud-config/exclude-config"));

                        try
                        {
                            JObject ruleSetting = JObject.Parse(getStringValueFromXmlNode(configXml.SelectSingleNode("application/eum-cloud-config/page-config")));
                            if (ruleSetting != null)
                            {
                                applicationConfiguration.EUMConfigPage = ruleSetting.ToString();
                            }
                        }
                        catch (JsonReaderException ex) { }
                        try
                        {
                            JObject ruleSetting = JObject.Parse(getStringValueFromXmlNode(configXml.SelectSingleNode("application/eum-cloud-config/mobile-page-config")));
                            if (ruleSetting != null)
                            {
                                applicationConfiguration.EUMConfigMobilePage = ruleSetting.ToString();
                            }
                        }
                        catch (JsonReaderException ex) { }
                        try
                        {
                            JObject ruleSetting = JObject.Parse(getStringValueFromXmlNode(configXml.SelectSingleNode("application/eum-cloud-config/eum-mobile-agent-config")));
                            if (ruleSetting != null)
                            {
                                applicationConfiguration.EUMConfigMobileAgent = ruleSetting.ToString();
                            }
                        }
                        catch (JsonReaderException ex) { }

                        applicationConfiguration.AnalyticsConfig = makeXMLFormattedAndIndented(configXml.SelectSingleNode("application/analytics-dynamic-service-configurations"));
                        applicationConfiguration.WorkflowsConfig = makeXMLFormattedAndIndented(configXml.SelectSingleNode("application/workflows"));
                        applicationConfiguration.TasksConfig = makeXMLFormattedAndIndented(configXml.SelectSingleNode("application/tasks"));
                        applicationConfiguration.BTGroupsConfig = makeXMLFormattedAndIndented(configXml.SelectSingleNode("application/business-transaction-groups"));

                        applicationConfiguration.MetricBaselinesConfig = makeXMLFormattedAndIndented(configXml.SelectSingleNode("application/metric-baselines"));
                        applicationConfiguration.NumBaselines = configXml.SelectNodes("application/metric-baselines/metric-baseline").Count;

                        JObject applicationConfigurationDetailsObject = FileIOHelper.LoadJObjectFromFile(FilePathMap.APMApplicationConfigurationDetailsDataFilePath(jobTarget));
                        if (applicationConfigurationDetailsObject != null)
                        {
                            applicationConfiguration.BTCleanupInterval = getIntValueFromJToken(applicationConfigurationDetailsObject, "btCleanupTimeframeInMinutes");
                            applicationConfiguration.BTCleanupCallCount = getLongValueFromJToken(applicationConfigurationDetailsObject, "btCleanupCallCountThreshold");
                            applicationConfiguration.IsBTCleanupEnabled = (applicationConfiguration.BTCleanupInterval > 0);
                        }

                        #endregion

                        #region Business Transaction Detection Rules

                        loggerConsole.Info("Business Transaction Detection Rules");

                        List<BusinessTransactionDiscoveryRule> businessTransactionDiscoveryRulesList = new List<BusinessTransactionDiscoveryRule>();

                        // Application level
                        // application
                        //      entry-match-point-configurations
                        //          entry-match-point-configuration[agentType=AGENT]
                        //              transaction-configurations
                        //                  configuration[transaction-entry-point-type=TYPE]
                        foreach (XmlNode entryMatchPointConfigurationNode in configXml.SelectNodes("application/entry-match-point-configurations/entry-match-point-configuration"))
                        {
                            foreach (XmlNode entryMatchPointTransactionConfigurationNode in entryMatchPointConfigurationNode.SelectNodes("transaction-configurations/configuration"))
                            {
                                BusinessTransactionDiscoveryRule businessTransactionDiscoveryRule = fillBusinessTransactionDiscoveryRule(entryMatchPointConfigurationNode, entryMatchPointTransactionConfigurationNode, applicationConfiguration, null);
                                businessTransactionDiscoveryRulesList.Add(businessTransactionDiscoveryRule);
                            }
                        }

                        // Tier overrides
                        // application
                        //      application-components
                        //          application-component
                        //              entry-match-point-configurations
                        //                  entry-match-point-configuration[agentType=AGENT]
                        //                      transaction-configurations
                        //                          configuration[transaction-entry-point-type=TYPE]
                        //                              override=true
                        foreach (XmlNode applicationComponentNode in configXml.SelectNodes("application/application-components/application-component"))
                        {
                            foreach (XmlNode entryMatchPointConfigurationNode in applicationComponentNode.SelectNodes("entry-match-point-configurations/entry-match-point-configuration"))
                            {
                                foreach (XmlNode entryMatchPointTransactionConfigurationNode in entryMatchPointConfigurationNode.SelectNodes("transaction-configurations/configuration"))
                                {
                                    if (Convert.ToBoolean(entryMatchPointConfigurationNode.SelectSingleNode("override").InnerText) == true)
                                    {
                                        BusinessTransactionDiscoveryRule businessTransactionDiscoveryRule = fillBusinessTransactionDiscoveryRule(entryMatchPointConfigurationNode, entryMatchPointTransactionConfigurationNode, applicationConfiguration, applicationComponentNode);
                                        businessTransactionDiscoveryRulesList.Add(businessTransactionDiscoveryRule);
                                    }
                                }
                            }
                        }

                        applicationConfiguration.NumBTDiscoveryRules = businessTransactionDiscoveryRulesList.Count;

                        businessTransactionDiscoveryRulesList = businessTransactionDiscoveryRulesList.OrderBy(b => b.TierName).ThenBy(b => b.AgentType).ThenBy(b => b.EntryPointType).ToList();
                        FileIOHelper.WriteListToCSVFile(businessTransactionDiscoveryRulesList, new BusinessTransactionDiscoveryRuleReportMap(), FilePathMap.APMBusinessTransactionDiscoveryRulesIndexFilePath(jobTarget));

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + businessTransactionDiscoveryRulesList.Count;

                        #endregion

                        #region Business Transaction Rules

                        loggerConsole.Info("Business Transaction Include and Exclude Rules");

                        List<BusinessTransactionEntryRule> businessTransactionEntryRulesList = new List<BusinessTransactionEntryRule>();

                        // Exclude rules first

                        // Application level
                        // application
                        //      entry-match-point-configurations
                        //          entry-match-point-configuration[agentType=AGENT]
                        //              transaction-configurations
                        //                  configuration[transaction-entry-point-type=TYPE]
                        //                      discovery-config
                        //                          excludes
                        foreach (XmlNode entryMatchPointConfigurationNode in configXml.SelectNodes("application/entry-match-point-configurations/entry-match-point-configuration"))
                        {
                            foreach (XmlNode entryMatchPointTransactionConfigurationNode in entryMatchPointConfigurationNode.SelectNodes("transaction-configurations/configuration"))
                            {
                                foreach (XmlNode entryMatchPointCustomMatchPointConfigurationNode in entryMatchPointTransactionConfigurationNode.SelectNodes("discovery-config/excludes/exclude"))
                                {
                                    BusinessTransactionEntryRule businessTransactionEntryRule = fillBusinessTransactionExcludeRule(entryMatchPointConfigurationNode, entryMatchPointTransactionConfigurationNode, entryMatchPointCustomMatchPointConfigurationNode, applicationConfiguration, null);
                                    businessTransactionEntryRulesList.Add(businessTransactionEntryRule);
                                }
                            }
                        }

                        // Tier overrides
                        // application
                        //      application-components
                        //          application-component
                        //              entry-match-point-configurations
                        //                  entry-match-point-configuration[agentType=AGENT]
                        //                      transaction-configurations
                        //                          configuration[transaction-entry-point-type=TYPE]
                        //                              override=true
                        //                              discovery-config
                        //                                  excludes
                        foreach (XmlNode applicationComponentNode in configXml.SelectNodes("application/application-components/application-component"))
                        {
                            foreach (XmlNode entryMatchPointConfigurationNode in applicationComponentNode.SelectNodes("entry-match-point-configurations/entry-match-point-configuration"))
                            {
                                foreach (XmlNode entryMatchPointTransactionConfigurationNode in entryMatchPointConfigurationNode.SelectNodes("transaction-configurations/configuration"))
                                {
                                    foreach (XmlNode entryMatchPointCustomMatchPointConfigurationNode in entryMatchPointTransactionConfigurationNode.SelectNodes("discovery-config/excludes/exclude"))
                                    {
                                        if (Convert.ToBoolean(entryMatchPointConfigurationNode.SelectSingleNode("override").InnerText) == true)
                                        {
                                            BusinessTransactionEntryRule businessTransactionEntryRule = fillBusinessTransactionExcludeRule(entryMatchPointConfigurationNode, entryMatchPointTransactionConfigurationNode, entryMatchPointCustomMatchPointConfigurationNode, applicationConfiguration, applicationComponentNode);
                                            businessTransactionEntryRulesList.Add(businessTransactionEntryRule);
                                        }
                                    }
                                }
                            }
                        }

                        // Include rules

                        // Application level
                        // application
                        //      entry-match-point-configurations
                        //          entry-match-point-configuration[agentType=AGENT]
                        //              custom-match-point-definitions
                        //                  custom-match-point-definition[transaction-entry-point-type=TYPE]
                        //                      transaction-configurations
                        foreach (XmlNode entryMatchPointConfigurationNode in configXml.SelectNodes("application/entry-match-point-configurations/entry-match-point-configuration"))
                        {
                            foreach (XmlNode entryMatchPointCustomMatchPointConfigurationNode in entryMatchPointConfigurationNode.SelectNodes("custom-match-point-definitions/custom-match-point-definition"))
                            {
                                BusinessTransactionEntryRule businessTransactionEntryRule = fillBusinessTransactionEntryRule(entryMatchPointConfigurationNode, entryMatchPointCustomMatchPointConfigurationNode, applicationConfiguration, null, businessTransactionsThisAppList);
                                businessTransactionEntryRulesList.Add(businessTransactionEntryRule);
                            }
                        }

                        // Tier overrides
                        // application
                        //      application-components
                        //          application-component
                        //              entry-match-point-configurations
                        //                  entry-match-point-configuration[agentType=AGENT]
                        //                      custom-match-point-definitions
                        //                          custom-match-point-definition[transaction-entry-point-type=TYPE]
                        //                              transaction-configurations
                        foreach (XmlNode applicationComponentNode in configXml.SelectNodes("application/application-components/application-component"))
                        {
                            foreach (XmlNode entryMatchPointConfigurationNode in applicationComponentNode.SelectNodes("entry-match-point-configurations/entry-match-point-configuration"))
                            {
                                foreach (XmlNode entryMatchPointCustomMatchPointConfigurationNode in entryMatchPointConfigurationNode.SelectNodes("custom-match-point-definitions/custom-match-point-definition"))
                                {
                                    if (Convert.ToBoolean(entryMatchPointConfigurationNode.SelectSingleNode("override").InnerText) == true)
                                    {
                                        BusinessTransactionEntryRule businessTransactionEntryRule = fillBusinessTransactionEntryRule(entryMatchPointConfigurationNode, entryMatchPointCustomMatchPointConfigurationNode, applicationConfiguration, applicationComponentNode, businessTransactionsThisAppList);
                                        businessTransactionEntryRulesList.Add(businessTransactionEntryRule);
                                    }
                                }
                            }
                        }

                        applicationConfiguration.NumBTEntryRules = businessTransactionEntryRulesList.Count(b => b.IsExclusion == false);

                        applicationConfiguration.NumBTExcludeRules = businessTransactionEntryRulesList.Count - applicationConfiguration.NumBTEntryRules;

                        businessTransactionEntryRulesList = businessTransactionEntryRulesList.OrderBy(b => b.TierName).ThenBy(b => b.AgentType).ThenBy(b => b.EntryPointType).ThenBy(b => b.RuleName).ToList();
                        FileIOHelper.WriteListToCSVFile(businessTransactionEntryRulesList, new BusinessTransactionEntryRuleReportMap(), FilePathMap.APMBusinessTransactionEntryRulesIndexFilePath(jobTarget));

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + businessTransactionEntryRulesList.Count;

                        #endregion

                        #region Service Endpoint Discovery and Entry Rules

                        loggerConsole.Info("Service Endpoint Discovery Rules");

                        List<ServiceEndpointDiscoveryRule> serviceEndpointDiscoveryRulesList = new List<ServiceEndpointDiscoveryRule>();

                        // SEP Autodetection Rules for App
                        JArray serviceEndpointsArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.APMApplicationConfigurationSEPDetectionRulesDataFilePath(jobTarget));
                        if (serviceEndpointsArray != null)
                        {
                            foreach (JObject serviceEndpointObject in serviceEndpointsArray)
                            {
                                ServiceEndpointDiscoveryRule serviceEndpointDiscoveryRule = fillServiceEnpointDiscoveryRule(serviceEndpointObject, null, applicationConfiguration);
                                serviceEndpointDiscoveryRulesList.Add(serviceEndpointDiscoveryRule);
                            }
                        }

                        // SEP Autodetection Rules for Tiers overrides
                        if (tiersThisAppList != null)
                        {
                            foreach (APMTier tier in tiersThisAppList)
                            {
                                JArray serviceEndpointsTierArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.APMApplicationConfigurationSEPTierDetectionRulesDataFilePath(jobTarget, tier));
                                if (serviceEndpointsTierArray != null)
                                {
                                    foreach (JObject serviceEndpointRuleObject in serviceEndpointsTierArray)
                                    {
                                        ServiceEndpointDiscoveryRule serviceEndpointDiscoveryRule = fillServiceEnpointDiscoveryRule(serviceEndpointRuleObject, tier, applicationConfiguration);
                                        serviceEndpointDiscoveryRulesList.Add(serviceEndpointDiscoveryRule);
                                    }
                                }
                            }
                        }

                        applicationConfiguration.NumSEPDiscoveryRules = serviceEndpointDiscoveryRulesList.Count;

                        serviceEndpointDiscoveryRulesList = serviceEndpointDiscoveryRulesList.OrderBy(b => b.TierName).ThenBy(b => b.AgentType).ThenBy(b => b.EntryPointType).ToList();
                        FileIOHelper.WriteListToCSVFile(serviceEndpointDiscoveryRulesList, new ServiceEndpointDiscoveryRuleReportMap(), FilePathMap.APMServiceEndpointDiscoveryRulesIndexFilePath(jobTarget));

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + serviceEndpointDiscoveryRulesList.Count;


                        loggerConsole.Info("Custom Service Endpoint Entry Rules");

                        List<ServiceEndpointEntryRule> serviceEndpointEntryRulesList = new List<ServiceEndpointEntryRule>();

                        if (tiersThisAppList != null)
                        {
                            foreach (APMTier tier in tiersThisAppList)
                            {
                                JArray serviceEndpointRulesInTierArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.APMApplicationConfigurationSEPTierExplicitRulesDataFilePath(jobTarget, tier));
                                if (serviceEndpointRulesInTierArray != null)
                                {
                                    foreach (JObject serviceEndpointRuleObject in serviceEndpointRulesInTierArray)
                                    {
                                        ServiceEndpointEntryRule serviceEndpointEntryRule = fillServiceEnpointEntryRule(serviceEndpointRuleObject, tier, applicationConfiguration, serviceEndpointsThisAppList);
                                        serviceEndpointEntryRulesList.Add(serviceEndpointEntryRule);
                                    }
                                }
                            }
                        }

                        applicationConfiguration.NumSEPEntryRules = serviceEndpointEntryRulesList.Count;

                        serviceEndpointEntryRulesList = serviceEndpointEntryRulesList.OrderBy(b => b.TierName).ThenBy(b => b.AgentType).ThenBy(b => b.EntryPointType).ToList();
                        FileIOHelper.WriteListToCSVFile(serviceEndpointEntryRulesList, new ServiceEndpointEntryRuleReportMap(), FilePathMap.APMServiceEndpointEntryRulesIndexFilePath(jobTarget));

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + serviceEndpointEntryRulesList.Count;

                        #endregion

                        #region MDS/Config 2.0 Scopes, BT Detection and BT Rules

                        if (applicationConfiguration.IsBT20ConfigEnabled == true)
                        {
                            loggerConsole.Info("Business Transaction Include and Exclude Rules - MDS 2.0");

                            List<BusinessTransactionEntryScope> businessTransactionEntryScopeList = new List<BusinessTransactionEntryScope>();

                            XmlNode scopeToRuleMappingConfigurationNode = configXml.SelectSingleNode("application/mds-data/mds-config-data/scope-rule-mapping-list");

                            foreach (XmlNode scopeConfigurationNode in configXml.SelectNodes("application/mds-data/mds-config-data/scope-list/scope"))
                            {
                                BusinessTransactionEntryScope businessTransactionEntryRuleScope = fillBusinessTransactionEntryScope(scopeConfigurationNode, scopeToRuleMappingConfigurationNode, applicationConfiguration);
                                businessTransactionEntryScopeList.Add(businessTransactionEntryRuleScope);
                            }

                            applicationConfiguration.NumBT20Scopes = businessTransactionEntryScopeList.Count;

                            businessTransactionEntryScopeList = businessTransactionEntryScopeList.OrderBy(b => b.ScopeType).ThenBy(b => b.ScopeName).ToList();
                            FileIOHelper.WriteListToCSVFile(businessTransactionEntryScopeList, new BusinessTransactionEntryScopeReportMap(), FilePathMap.APMBusinessTransactionEntryScopesIndexFilePath(jobTarget));

                            stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + businessTransactionEntryScopeList.Count;


                            List<BusinessTransactionEntryRule20> businessTransactionEntryRules20List = new List<BusinessTransactionEntryRule20>();

                            foreach (XmlNode ruleConfigurationNode in configXml.SelectNodes("application/mds-data/mds-config-data/rule-list/rule"))
                            {
                                BusinessTransactionEntryRule20 businessTransactionEntryRule = fillBusinessTransactionEntryRule20(ruleConfigurationNode, scopeToRuleMappingConfigurationNode, applicationConfiguration, businessTransactionsThisAppList);
                                if (businessTransactionEntryRule != null)
                                {
                                    businessTransactionEntryRules20List.Add(businessTransactionEntryRule);
                                }
                            }

                            applicationConfiguration.NumBT20EntryRules = businessTransactionEntryRules20List.Count(b => b.IsExclusion == false);
                            applicationConfiguration.NumBT20ExcludeRules = businessTransactionEntryRules20List.Count - applicationConfiguration.NumBT20EntryRules;

                            businessTransactionEntryRules20List = businessTransactionEntryRules20List.OrderBy(b => b.ScopeName).ThenBy(b => b.AgentType).ThenBy(b => b.EntryPointType).ThenBy(b => b.RuleName).ToList();
                            FileIOHelper.WriteListToCSVFile(businessTransactionEntryRules20List, new BusinessTransactionEntryRule20ReportMap(), FilePathMap.APMBusinessTransactionEntryRules20IndexFilePath(jobTarget));

                            stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + businessTransactionEntryRules20List.Count;


                            List<BusinessTransactionDiscoveryRule20> businessTransactionDiscoveryRule20List = new List<BusinessTransactionDiscoveryRule20>();

                            foreach (XmlNode ruleConfigurationNode in configXml.SelectNodes("application/mds-data/mds-config-data/rule-list/rule"))
                            {
                                List<BusinessTransactionDiscoveryRule20> businessTransactionDiscoveryRuleList = fillBusinessTransactionDiscoveryRule20(ruleConfigurationNode, scopeToRuleMappingConfigurationNode, applicationConfiguration, businessTransactionsThisAppList);
                                if (businessTransactionDiscoveryRuleList != null)
                                {
                                    businessTransactionDiscoveryRule20List.AddRange(businessTransactionDiscoveryRuleList);
                                }
                            }

                            applicationConfiguration.NumBT20DiscoveryRules = businessTransactionDiscoveryRule20List.Count;

                            businessTransactionEntryRules20List = businessTransactionEntryRules20List.OrderBy(b => b.ScopeName).ThenBy(b => b.AgentType).ThenBy(b => b.EntryPointType).ThenBy(b => b.RuleName).ToList();
                            FileIOHelper.WriteListToCSVFile(businessTransactionDiscoveryRule20List, new BusinessTransactionDiscoveryRule20ReportMap(), FilePathMap.APMBusinessTransactionDiscoveryRules20IndexFilePath(jobTarget));

                            stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + businessTransactionDiscoveryRule20List.Count;
                        }

                        #endregion

                        #region Backend Rules 

                        loggerConsole.Info("Backend Detection Rules");

                        List<BackendDiscoveryRule> backendDiscoveryRulesList = new List<BackendDiscoveryRule>();

                        // Application level
                        // application
                        //      backend-match-point-configurations
                        //          backend-match-point-configuration[agentType=AGENT]
                        //              backend-discovery-configurations
                        //                  backend-discovery-configuration
                        foreach (XmlNode backendDiscoveryMatchPointConfigurationNode in configXml.SelectNodes("application/backend-match-point-configurations/backend-match-point-configuration"))
                        {
                            foreach (XmlNode backendDiscoveryConfigurationNode in backendDiscoveryMatchPointConfigurationNode.SelectNodes("backend-discovery-configurations/backend-discovery-configuration"))
                            {
                                BackendDiscoveryRule backendDiscoveryRule = fillBackendDiscoveryRule(backendDiscoveryMatchPointConfigurationNode, backendDiscoveryConfigurationNode, applicationConfiguration, null, backendsThisAppList);
                                backendDiscoveryRulesList.Add(backendDiscoveryRule);
                            }
                        }

                        // Tier overrides
                        // application
                        //      application-components
                        //          application-component
                        //              backend-match-point-configurations
                        //                  backend-match-point-configuration[agentType=AGENT]
                        //                      backend-discovery-configurations
                        //                          backend-discovery-configuration
                        //                              override=true
                        foreach (XmlNode applicationComponentNode in configXml.SelectNodes("application/application-components/application-component"))
                        {
                            foreach (XmlNode backendDiscoveryMatchPointConfigurationNode in applicationComponentNode.SelectNodes("backend-match-point-configurations/backend-match-point-configuration"))
                            {
                                foreach (XmlNode backendDiscoveryConfigurationNode in backendDiscoveryMatchPointConfigurationNode.SelectNodes("backend-discovery-configurations/backend-discovery-configuration"))
                                {
                                    if (Convert.ToBoolean(backendDiscoveryMatchPointConfigurationNode.SelectSingleNode("override").InnerText) == true)
                                    {
                                        BackendDiscoveryRule backendDiscoveryRule = fillBackendDiscoveryRule(backendDiscoveryMatchPointConfigurationNode, backendDiscoveryConfigurationNode, applicationConfiguration, applicationComponentNode, backendsThisAppList);
                                        backendDiscoveryRulesList.Add(backendDiscoveryRule);
                                    }
                                }
                            }
                        }

                        applicationConfiguration.NumBackendRules = backendDiscoveryRulesList.Count;

                        backendDiscoveryRulesList = backendDiscoveryRulesList.OrderBy(b => b.TierName).ThenBy(b => b.AgentType).ThenBy(b => b.ExitType).ThenBy(b => b.RuleName).ToList();
                        FileIOHelper.WriteListToCSVFile(backendDiscoveryRulesList, new BackendDiscoveryRuleReportMap(), FilePathMap.APMBackendDiscoveryRulesIndexFilePath(jobTarget));

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + backendDiscoveryRulesList.Count;

                        #endregion

                        #region Custom Exit Rules 

                        loggerConsole.Info("Custom Exit Rules");

                        List<CustomExitRule> customExitRulesList = new List<CustomExitRule>();

                        // Application level
                        // application
                        //      backend-match-point-configurations
                        //          backend-match-point-configuration[agentType=AGENT]
                        //              custom-exit-point-definitions
                        //                  custom-exit-point-definition
                        foreach (XmlNode backendDiscoveryMatchPointConfigurationNode in configXml.SelectNodes("application/backend-match-point-configurations/backend-match-point-configuration"))
                        {
                            foreach (XmlNode customExitConfigurationNode in backendDiscoveryMatchPointConfigurationNode.SelectNodes("custom-exit-point-definitions/custom-exit-point-definition"))
                            {
                                CustomExitRule customExitRule = fillCustomExitRule(backendDiscoveryMatchPointConfigurationNode, customExitConfigurationNode, applicationConfiguration, null, backendsThisAppList);
                                customExitRulesList.Add(customExitRule);
                            }
                        }

                        // Tier overrides
                        // application
                        //      application-components
                        //          application-component
                        //              backend-match-point-configurations
                        //                  backend-match-point-configuration[agentType=AGENT]
                        //                      custom-exit-point-definition
                        //                          custom-exit-point-definition
                        //                              override=true
                        foreach (XmlNode applicationComponentNode in configXml.SelectNodes("application/application-components/application-component"))
                        {
                            foreach (XmlNode backendDiscoveryMatchPointConfigurationNode in applicationComponentNode.SelectNodes("backend-match-point-configurations/backend-match-point-configuration"))
                            {
                                foreach (XmlNode customExitConfigurationNode in backendDiscoveryMatchPointConfigurationNode.SelectNodes("custom-exit-point-definitions/custom-exit-point-definition"))
                                {
                                    if (Convert.ToBoolean(backendDiscoveryMatchPointConfigurationNode.SelectSingleNode("override").InnerText) == true)
                                    {
                                        CustomExitRule customExitRule = fillCustomExitRule(backendDiscoveryMatchPointConfigurationNode, customExitConfigurationNode, applicationConfiguration, applicationComponentNode, backendsThisAppList);
                                        customExitRulesList.Add(customExitRule);
                                    }
                                }
                            }
                        }

                        applicationConfiguration.NumBackendRules = applicationConfiguration.NumBackendRules + customExitRulesList.Count;

                        customExitRulesList = customExitRulesList.OrderBy(b => b.TierName).ThenBy(b => b.AgentType).ThenBy(b => b.ExitType).ThenBy(b => b.RuleName).ToList();
                        FileIOHelper.WriteListToCSVFile(customExitRulesList, new CustomExitRuleReportMap(), FilePathMap.APMCustomExitRulesIndexFilePath(jobTarget));

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + customExitRulesList.Count;

                        #endregion

                        #region Agent Configuration Properties 

                        loggerConsole.Info("Agent Configuration Properties");

                        List<AgentConfigurationProperty> agentConfigurationPropertiesList = new List<AgentConfigurationProperty>();

                        // Application level
                        // application
                        //      agent-configurations
                        //          agent_configuration[agentType=AGENT]
                        //              property-definitions
                        //                  property-definition
                        //              properties
                        //                  property
                        foreach (XmlNode agentConfigurationNode in configXml.SelectNodes("application/agent-configurations/agent_configuration"))
                        {
                            foreach (XmlNode agentPropertyDefinitionConfigurationNode in agentConfigurationNode.SelectNodes("property-definitions/property-definition"))
                            {
                                XmlNode agentPropertyValueConfigurationNode = agentConfigurationNode.SelectSingleNode(String.Format(@"properties/property/property-definition[. = ""{0}""]", agentPropertyDefinitionConfigurationNode.SelectSingleNode("name").InnerText)).ParentNode;

                                AgentConfigurationProperty agentConfigurationProperty = fillAgentConfigurationProperty(agentConfigurationNode, agentPropertyDefinitionConfigurationNode, agentPropertyValueConfigurationNode, applicationConfiguration, null);
                                agentConfigurationPropertiesList.Add(agentConfigurationProperty);
                            }
                        }

                        // Tier overrides
                        // application
                        //      application-components
                        //          application-component
                        //              agent-configurations
                        //                  agent_configuration[agentType=AGENT]
                        //                      property-definitions
                        //                          property-definition
                        //                      properties
                        //                          property
                        foreach (XmlNode applicationComponentNode in configXml.SelectNodes("application/application-components/application-component"))
                        {
                            foreach (XmlNode agentConfigurationNode in applicationComponentNode.SelectNodes("agent-configurations/agent_configuration"))
                            {
                                foreach (XmlNode agentPropertyDefinitionConfigurationNode in agentConfigurationNode.SelectNodes("property-definitions/property-definition"))
                                {
                                    if (Convert.ToBoolean(agentConfigurationNode.SelectSingleNode("override").InnerText) == true)
                                    {
                                        XmlNode agentPropertyValueConfigurationNode = agentConfigurationNode.SelectSingleNode(String.Format(@"properties/property/property-definition[. = ""{0}""]", agentPropertyDefinitionConfigurationNode.SelectSingleNode("name").InnerText)).ParentNode;

                                        AgentConfigurationProperty agentConfigurationProperty = fillAgentConfigurationProperty(agentConfigurationNode, agentPropertyDefinitionConfigurationNode, agentPropertyValueConfigurationNode, applicationConfiguration, applicationComponentNode);
                                        agentConfigurationPropertiesList.Add(agentConfigurationProperty);
                                    }
                                }
                            }
                        }

                        applicationConfiguration.NumAgentProps = agentConfigurationPropertiesList.Count;

                        agentConfigurationPropertiesList = agentConfigurationPropertiesList.OrderBy(p => p.TierName).ThenBy(p => p.AgentType).ThenBy(p => p.PropertyName).ToList();
                        FileIOHelper.WriteListToCSVFile(agentConfigurationPropertiesList, new AgentConfigurationPropertyReportMap(), FilePathMap.APMAgentConfigurationPropertiesIndexFilePath(jobTarget));

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + agentConfigurationPropertiesList.Count;

                        #endregion

                        #region Information Point Rules

                        loggerConsole.Info("Information Point Rules");

                        List<InformationPointRule> informationPointRulesList = new List<InformationPointRule>();

                        // Application level
                        // application
                        //      info-point-gatherer-configs
                        //          info-point-gatherer-config
                        foreach (XmlNode informationPointConfigurationNode in configXml.SelectNodes("application/info-point-gatherer-configs/info-point-gatherer-config"))
                        {
                            InformationPointRule informationPointRule = fillInformationPointRule(informationPointConfigurationNode, applicationConfiguration);
                            informationPointRulesList.Add(informationPointRule);
                        }

                        applicationConfiguration.NumInfoPointRules = informationPointRulesList.Count;

                        informationPointRulesList = informationPointRulesList.OrderBy(b => b.AgentType).ThenBy(b => b.RuleName).ToList();
                        FileIOHelper.WriteListToCSVFile(informationPointRulesList, new InformationPointRuleReportMap(), FilePathMap.APMInformationPointRulesIndexFilePath(jobTarget));

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + informationPointRulesList.Count;

                        #endregion

                        #region Detected Business Transaction and Assigned Data Collectors

                        loggerConsole.Info("Detected Business Transaction and Assigned Data Collectors");

                        List<BusinessTransactionConfiguration> entityBusinessTransactionConfigurationsList = new List<BusinessTransactionConfiguration>();

                        // BT settings
                        // application
                        //      application-components
                        //          application-component
                        //              business-transaction
                        foreach (XmlNode applicationComponentNode in configXml.SelectNodes("application/application-components/application-component"))
                        {
                            foreach (XmlNode businessTransactionConfigurationtNode in applicationComponentNode.SelectNodes("business-transactions/business-transaction"))
                            {
                                BusinessTransactionConfiguration entityBusinessTransactionConfiguration = fillEntityBusinessTransactionConfiguration(applicationComponentNode, businessTransactionConfigurationtNode, applicationConfiguration, tiersThisAppList, businessTransactionsList);
                                entityBusinessTransactionConfigurationsList.Add(entityBusinessTransactionConfiguration);
                            }
                        }

                        applicationConfiguration.NumBTs = entityBusinessTransactionConfigurationsList.Count;

                        entityBusinessTransactionConfigurationsList = entityBusinessTransactionConfigurationsList.OrderBy(b => b.TierName).ThenBy(b => b.BTType).ThenBy(b => b.BTName).ToList();
                        FileIOHelper.WriteListToCSVFile(entityBusinessTransactionConfigurationsList, new BusinessTransactionConfigurationReportMap(), FilePathMap.APMBusinessTransactionConfigurationsIndexFilePath(jobTarget));

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + entityBusinessTransactionConfigurationsList.Count;

                        #endregion

                        #region Tier Settings

                        loggerConsole.Info("Tier Settings");

                        List<TierConfiguration> entityTierConfigurationsList = new List<TierConfiguration>();

                        // Tier settings
                        // application
                        //      application-components
                        //          application-component
                        foreach (XmlNode applicationComponentNode in configXml.SelectNodes("application/application-components/application-component"))
                        {
                            TierConfiguration entityTierConfiguration = fillEntityTierConfiguration(applicationComponentNode, applicationConfiguration, tiersThisAppList, entityBusinessTransactionConfigurationsList);
                            entityTierConfigurationsList.Add(entityTierConfiguration);
                        }

                        applicationConfiguration.NumTiers = entityTierConfigurationsList.Count;

                        entityTierConfigurationsList = entityTierConfigurationsList.OrderBy(p => p.TierName).ToList();
                        FileIOHelper.WriteListToCSVFile(entityTierConfigurationsList, new TierConfigurationReportMap(), FilePathMap.APMTierConfigurationsIndexFilePath(jobTarget));

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + entityTierConfigurationsList.Count;

                        #endregion

                        #region Data Collectors

                        loggerConsole.Info("Data Collectors");

                        // MIDCs
                        List<MethodInvocationDataCollector> methodInvocationDataCollectorsList = new List<MethodInvocationDataCollector>();

                        // Application level
                        // application
                        //      data-gatherer-configs
                        //          pojo-data-gatherer-config
                        foreach (XmlNode methodInvocationDataCollectorConfigurationNode in configXml.SelectNodes("application/data-gatherer-configs/pojo-data-gatherer-config"))
                        {
                            foreach (XmlNode dataGathererConfigurationNode in methodInvocationDataCollectorConfigurationNode.SelectNodes("method-invocation-data-gatherer-config"))
                            {
                                MethodInvocationDataCollector methodInvocationDataCollector = fillMethodInvocationDataCollector(methodInvocationDataCollectorConfigurationNode, dataGathererConfigurationNode, applicationConfiguration, entityBusinessTransactionConfigurationsList);
                                methodInvocationDataCollectorsList.Add(methodInvocationDataCollector);
                            }
                        }

                        applicationConfiguration.NumMIDCVariablesCollected = methodInvocationDataCollectorsList.Count;
                        applicationConfiguration.NumMIDCs = methodInvocationDataCollectorsList.GroupBy(m => m.CollectorName).Count();

                        methodInvocationDataCollectorsList = methodInvocationDataCollectorsList.OrderBy(b => b.CollectorName).ThenBy(b => b.DataGathererName).ToList();
                        FileIOHelper.WriteListToCSVFile(methodInvocationDataCollectorsList, new MethodInvocationDataCollectorReportMap(), FilePathMap.APMMethodInvocationDataCollectorsIndexFilePath(jobTarget));

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + methodInvocationDataCollectorsList.Count;

                        // HTTP DCs

                        // Application level
                        // application
                        //      data-gatherer-configs
                        //          http-data-gatherer-config
                        List<HTTPDataCollector> httpDataCollectorsList = new List<HTTPDataCollector>();

                        // Application level
                        // application
                        //      data-gatherer-configs
                        //          pojo-data-gatherer-config
                        foreach (XmlNode httpDataCollectorConfigurationNode in configXml.SelectNodes("application/data-gatherer-configs/http-data-gatherer-config"))
                        {
                            if (httpDataCollectorConfigurationNode.SelectNodes("parameters/parameter").Count > 0)
                            {
                                foreach (XmlNode dataGathererConfigurationNode in httpDataCollectorConfigurationNode.SelectNodes("parameters/parameter"))
                                {
                                    HTTPDataCollector httpDataCollector = fillHTTPDataCollector(httpDataCollectorConfigurationNode, dataGathererConfigurationNode, applicationConfiguration, entityBusinessTransactionConfigurationsList);
                                    httpDataCollectorsList.Add(httpDataCollector);
                                }
                            }
                            else
                            {
                                HTTPDataCollector httpDataCollector = fillHTTPDataCollector(httpDataCollectorConfigurationNode, null, applicationConfiguration, entityBusinessTransactionConfigurationsList);
                                httpDataCollectorsList.Add(httpDataCollector);
                            }
                        }

                        applicationConfiguration.NumHTTPDCVariablesCollected = httpDataCollectorsList.Count;
                        applicationConfiguration.NumHTTPDCs = httpDataCollectorsList.GroupBy(m => m.CollectorName).Count();

                        httpDataCollectorsList = httpDataCollectorsList.OrderBy(b => b.CollectorName).ThenBy(b => b.DataGathererName).ToList();
                        FileIOHelper.WriteListToCSVFile(httpDataCollectorsList, new HTTPDataCollectorReportMap(), FilePathMap.APMHttpDataCollectorsIndexFilePath(jobTarget));

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + httpDataCollectorsList.Count;

                        #endregion

                        #region Call Graph Settings

                        loggerConsole.Info("Call Graph Settings");

                        // MIDCs
                        List<AgentCallGraphSetting> agentCallGraphSettingCollectorsList = new List<AgentCallGraphSetting>();

                        // Application level
                        // application
                        //      configuration
                        //          call-graph
                        foreach (XmlNode agentCallGraphSettingConfigurationNode in configXml.SelectNodes("application/configuration/call-graph"))
                        {
                            AgentCallGraphSetting agentCallGraphSetting = fillAgentCallGraphSetting(agentCallGraphSettingConfigurationNode, applicationConfiguration);
                            agentCallGraphSettingCollectorsList.Add(agentCallGraphSetting);
                        }

                        agentCallGraphSettingCollectorsList = agentCallGraphSettingCollectorsList.OrderBy(a => a.AgentType).ToList();
                        FileIOHelper.WriteListToCSVFile(agentCallGraphSettingCollectorsList, new AgentCallGraphSettingReportMap(), FilePathMap.APMAgentCallGraphSettingsIndexFilePath(jobTarget));

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + agentCallGraphSettingCollectorsList.Count;

                        #endregion

                        #region Developer Mode Nodes

                        loggerConsole.Info("Developer Mode Nodes");

                        List<DeveloperModeNode> developerModeSettingsList = new List<DeveloperModeNode>();

                        JArray developerModeTiersArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.APMApplicationDeveloperModeNodesDataFilePath(jobTarget));
                        if (developerModeTiersArray != null && developerModeTiersArray.Count > 0)
                        {
                            foreach (JToken developerModeBusinessTransactionToken in developerModeTiersArray)
                            {
                                if (isTokenPropertyNull(developerModeBusinessTransactionToken, "children") == false)
                                {
                                    foreach (JToken developerModeNodeToken in developerModeBusinessTransactionToken["children"])
                                    {
                                        DeveloperModeNode developerModeSetting = new DeveloperModeNode();

                                        if (getBoolValueFromJToken(developerModeNodeToken, "enabled") == true)
                                        {
                                            developerModeSetting.Controller = applicationConfiguration.Controller;
                                            developerModeSetting.ApplicationName = applicationConfiguration.ApplicationName;
                                            developerModeSetting.ApplicationID = applicationConfiguration.ApplicationID;

                                            developerModeSetting.TierName = getStringValueFromJToken(developerModeBusinessTransactionToken, "componentName");
                                            developerModeSetting.TierID = getLongValueFromJToken(developerModeBusinessTransactionToken, "componentId");

                                            developerModeSetting.BTName = getStringValueFromJToken(developerModeBusinessTransactionToken, "name");
                                            developerModeSetting.BTID = getLongValueFromJToken(developerModeBusinessTransactionToken, "id");

                                            developerModeSetting.NodeName = getStringValueFromJToken(developerModeNodeToken, "name");
                                            developerModeSetting.NodeID = getLongValueFromJToken(developerModeNodeToken, "id");

                                            developerModeSettingsList.Add(developerModeSetting);
                                        }
                                    }
                                }
                            }
                        }

                        developerModeSettingsList = developerModeSettingsList.OrderBy(d => d.TierName).ThenBy(d => d.BTName).ThenBy(d => d.NodeName).ToList();
                        FileIOHelper.WriteListToCSVFile(developerModeSettingsList, new DeveloperModeNodeReportMap(), FilePathMap.APMDeveloperModeNodesIndexFilePath(jobTarget));

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + developerModeSettingsList.Count;


                        #endregion

                        #region Error Detection rules, loggers, ignore exceptions, messages, HTTP codes and redirect pages

                        loggerConsole.Info("Error Detection Settings");

                        // 6 types of agent. Java has 6 checkboxes, others have less
                        List<ErrorDetectionRule> errorDetectionRulesList = new List<ErrorDetectionRule>(6 * 5);

                        // 6 types of agent. Let's assume there are 10 rules each, which is probably too generous 
                        List<ErrorDetectionIgnoreMessage> errorDetectionIgnoreMessagesList = new List<ErrorDetectionIgnoreMessage>(6 * 10);

                        // 2 types of agent support it
                        List<ErrorDetectionIgnoreLogger> errorDetectionIgnoreLoggersList = new List<ErrorDetectionIgnoreLogger>(2 * 2);

                        // 2 types of agent support it
                        List<ErrorDetectionLogger> errorDetectionLoggersList = new List<ErrorDetectionLogger>(2 * 2);

                        // 4 types of agent support it
                        List<ErrorDetectionHTTPCode> errorDetectionHTTPCodesList = new List<ErrorDetectionHTTPCode>(4 * 2);

                        // 2 types of agent support it
                        List<ErrorDetectionRedirectPage> errorDetectionRedirectPagesList = new List<ErrorDetectionRedirectPage>(2 * 2);

                        #region Java

                        if (applicationConfigurationDetailsObject != null &&
                            isTokenPropertyNull(applicationConfigurationDetailsObject, "errorConfig") == false)
                        {
                            JObject errorConfigContainer = (JObject)applicationConfigurationDetailsObject["errorConfig"];

                            errorDetectionRulesList.Add(fillErrorDetectionRule(applicationConfiguration, "Java", "Mark BT As Error", getBoolValueFromJToken(errorConfigContainer, "markTransactionAsErrorOnErrorMessageLog").ToString()));
                            errorDetectionRulesList.Add(fillErrorDetectionRule(applicationConfiguration, "Java", "Detect errors from java.util.logging", (getBoolValueFromJToken(errorConfigContainer, "disableJavaLogging") == false).ToString()));
                            errorDetectionRulesList.Add(fillErrorDetectionRule(applicationConfiguration, "Java", "Detect errors from Log4j", (getBoolValueFromJToken(errorConfigContainer, "disableLog4JLogging") == false).ToString()));
                            errorDetectionRulesList.Add(fillErrorDetectionRule(applicationConfiguration, "Java", "Detect errors from SLF4j/Logback", (getBoolValueFromJToken(errorConfigContainer, "disableSLF4JLogging") == false).ToString()));
                            errorDetectionRulesList.Add(fillErrorDetectionRule(applicationConfiguration, "Java", "Detect errors at ERROR or higher", getBoolValueFromJToken(errorConfigContainer, "captureLoggerErrorAndFatalMessages").ToString()));
                            errorDetectionRulesList.Add(fillErrorDetectionRule(applicationConfiguration, "Java", "Detect default HTTP error code", (getBoolValueFromJToken(errorConfigContainer, "disableDefaultHTTPErrorCode") == false).ToString()));

                            if (isTokenPropertyNull(errorConfigContainer, "ignoreExceptions") == false && isTokenPropertyNull(errorConfigContainer, "ignoreExceptionMsgPatterns") == false)
                            {
                                JArray ignoreExceptionsArray = (JArray)errorConfigContainer["ignoreExceptions"];
                                JArray ignoreExceptionsMessagesArray = (JArray)errorConfigContainer["ignoreExceptionMsgPatterns"];

                                if (ignoreExceptionsArray.Count > 0 && ignoreExceptionsMessagesArray.Count > 0)
                                {
                                    for (int j = 0; j < ignoreExceptionsArray.Count; j++)
                                    {
                                        try
                                        {
                                            errorDetectionIgnoreMessagesList.Add(fillErrorDetectionIgnoreException(applicationConfiguration, "Java", ignoreExceptionsArray[j].ToString(), (JObject)ignoreExceptionsMessagesArray[j]));
                                        }
                                        catch { }
                                    }
                                }
                            }
                            if (isTokenPropertyNull(errorConfigContainer, "ignoreLoggerMsgPatterns") == false)
                            {
                                JArray ignoreMessagesArray = (JArray)errorConfigContainer["ignoreLoggerMsgPatterns"];

                                if (ignoreMessagesArray.Count > 0)
                                {
                                    for (int j = 0; j < ignoreMessagesArray.Count; j++)
                                    {
                                        errorDetectionIgnoreMessagesList.Add(fillErrorDetectionIgnoreException(applicationConfiguration, "Java", String.Format("<Message {0}>", j), (JObject)ignoreMessagesArray[j]));
                                    }
                                }
                            }
                            if (isTokenPropertyNull(errorConfigContainer, "ignoreLoggerNames") == false)
                            {
                                JArray ignoreLoggersArray = (JArray)errorConfigContainer["ignoreLoggerNames"];

                                if (ignoreLoggersArray.Count > 0)
                                {
                                    for (int j = 0; j < ignoreLoggersArray.Count; j++)
                                    {
                                        errorDetectionIgnoreLoggersList.Add(fillErrorDetectionIgnoreLogger(applicationConfiguration, "Java", ignoreLoggersArray[j].ToString()));
                                    }
                                }
                            }
                            if (isTokenPropertyNull(errorConfigContainer, "customerLoggerDefinitions") == false)
                            {
                                JArray loggersArray = (JArray)errorConfigContainer["customerLoggerDefinitions"];

                                if (loggersArray.Count > 0)
                                {
                                    foreach (JObject loggerObject in loggersArray)
                                    {
                                        errorDetectionLoggersList.Add(fillErrorDetectionLogger(applicationConfiguration, "Java", loggerObject));
                                    }
                                }
                            }
                            if (isTokenPropertyNull(errorConfigContainer, "httpErrorReturnCodes") == false)
                            {
                                JArray httpErrorCodesArray = (JArray)errorConfigContainer["httpErrorReturnCodes"];

                                if (httpErrorCodesArray.Count > 0)
                                {
                                    foreach (JObject httpErrorCodeObject in httpErrorCodesArray)
                                    {
                                        errorDetectionHTTPCodesList.Add(fillErrorDetectionHTTPCode(applicationConfiguration, "Java", httpErrorCodeObject));
                                    }
                                }
                            }
                            if (isTokenPropertyNull(errorConfigContainer, "errorRedirectPages") == false)
                            {
                                JArray errorRedirectPagesArray = (JArray)errorConfigContainer["errorRedirectPages"];

                                if (errorRedirectPagesArray.Count > 0)
                                {
                                    foreach (JObject errorRedirectPageObject in errorRedirectPagesArray)
                                    {
                                        errorDetectionRedirectPagesList.Add(fillErrorDetectionRedirectPage(applicationConfiguration, "Java", errorRedirectPageObject));
                                    }
                                }
                            }
                        }

                        #endregion

                        #region .NET

                        if (applicationConfigurationDetailsObject != null &&
                            isTokenPropertyNull(applicationConfigurationDetailsObject, "dotNetErrorConfig") == false)
                        {
                            JObject errorConfigContainer = (JObject)applicationConfigurationDetailsObject["dotNetErrorConfig"];

                            errorDetectionRulesList.Add(fillErrorDetectionRule(applicationConfiguration, ".NET", "Mark BT As Error", getBoolValueFromJToken(errorConfigContainer, "markTransactionAsErrorOnErrorMessageLog").ToString()));
                            errorDetectionRulesList.Add(fillErrorDetectionRule(applicationConfiguration, ".NET", "Detect errors from NLog", (getBoolValueFromJToken(errorConfigContainer, "disableNLog") == false).ToString()));
                            errorDetectionRulesList.Add(fillErrorDetectionRule(applicationConfiguration, ".NET", "Detect errors from Log4Net", (getBoolValueFromJToken(errorConfigContainer, "disableLog4NetLogging") == false).ToString()));
                            errorDetectionRulesList.Add(fillErrorDetectionRule(applicationConfiguration, ".NET", "Detect errors from System.Diagnostics.Trace", (getBoolValueFromJToken(errorConfigContainer, "disableSystemTrace") == false).ToString())); ;
                            errorDetectionRulesList.Add(fillErrorDetectionRule(applicationConfiguration, ".NET", "Detect errors from EventLog", (getBoolValueFromJToken(errorConfigContainer, "disableEventLog") == false).ToString())); ;
                            errorDetectionRulesList.Add(fillErrorDetectionRule(applicationConfiguration, ".NET", "Detect errors at ERROR or higher", getBoolValueFromJToken(errorConfigContainer, "captureLoggerErrorAndFatalMessages").ToString()));
                            errorDetectionRulesList.Add(fillErrorDetectionRule(applicationConfiguration, ".NET", "Detect default HTTP error code", (getBoolValueFromJToken(errorConfigContainer, "disableDefaultHTTPErrorCode") == false).ToString()));

                            if (isTokenPropertyNull(errorConfigContainer, "ignoreExceptions") == false && isTokenPropertyNull(errorConfigContainer, "ignoreExceptionMsgPatterns") == false)
                            {
                                JArray ignoreExceptionsArray = (JArray)errorConfigContainer["ignoreExceptions"];
                                JArray ignoreExceptionsMessagesArray = (JArray)errorConfigContainer["ignoreExceptionMsgPatterns"];

                                if (ignoreExceptionsArray.Count > 0 && ignoreExceptionsMessagesArray.Count > 0)
                                {
                                    for (int j = 0; j < ignoreExceptionsArray.Count; j++)
                                    {
                                        try
                                        {
                                            errorDetectionIgnoreMessagesList.Add(fillErrorDetectionIgnoreException(applicationConfiguration, ".NET", ignoreExceptionsArray[j].ToString(), (JObject)ignoreExceptionsMessagesArray[j]));
                                        }
                                        catch { }
                                    }
                                }
                            }
                            if (isTokenPropertyNull(errorConfigContainer, "ignoreLoggerMsgPatterns") == false)
                            {
                                JArray ignoreMessagesArray = (JArray)errorConfigContainer["ignoreLoggerMsgPatterns"];

                                if (ignoreMessagesArray.Count > 0)
                                {
                                    for (int j = 0; j < ignoreMessagesArray.Count; j++)
                                    {
                                        errorDetectionIgnoreMessagesList.Add(fillErrorDetectionIgnoreException(applicationConfiguration, ".NET", String.Format("<Message {0}>", j), (JObject)ignoreMessagesArray[j]));
                                    }
                                }
                            }
                            if (isTokenPropertyNull(errorConfigContainer, "ignoreLoggerNames") == false)
                            {
                                JArray ignoreLoggersArray = (JArray)errorConfigContainer["ignoreLoggerNames"];

                                if (ignoreLoggersArray.Count > 0)
                                {
                                    for (int j = 0; j < ignoreLoggersArray.Count; j++)
                                    {
                                         errorDetectionIgnoreLoggersList.Add(fillErrorDetectionIgnoreLogger(applicationConfiguration, ".NET", ignoreLoggersArray[j].ToString()));
                                    }
                                }
                            }
                            if (isTokenPropertyNull(errorConfigContainer, "customerLoggerDefinitions") == false)
                            {
                                JArray loggersArray = (JArray)errorConfigContainer["customerLoggerDefinitions"];

                                if (loggersArray.Count > 0)
                                {
                                    foreach (JObject loggerObject in loggersArray)
                                    {
                                        errorDetectionLoggersList.Add(fillErrorDetectionLogger(applicationConfiguration, ".NET", loggerObject));
                                    }
                                }
                            }
                            if (isTokenPropertyNull(errorConfigContainer, "httpErrorReturnCodes") == false)
                            {
                                JArray httpErrorCodesArray = (JArray)errorConfigContainer["httpErrorReturnCodes"];

                                if (httpErrorCodesArray.Count > 0)
                                {
                                    foreach (JObject httpErrorCodeObject in httpErrorCodesArray)
                                    {
                                        errorDetectionHTTPCodesList.Add(fillErrorDetectionHTTPCode(applicationConfiguration, ".NET", httpErrorCodeObject));
                                    }
                                }
                            }
                            if (isTokenPropertyNull(errorConfigContainer, "errorRedirectPages") == false)
                            {
                                JArray errorRedirectPagesArray = (JArray)errorConfigContainer["errorRedirectPages"];

                                if (errorRedirectPagesArray.Count > 0)
                                {
                                    foreach (JObject errorRedirectPageObject in errorRedirectPagesArray)
                                    {
                                        errorDetectionRedirectPagesList.Add(fillErrorDetectionRedirectPage(applicationConfiguration, ".NET", errorRedirectPageObject));
                                    }
                                }
                            }
                        }

                        #endregion

                        #region PHP

                        if (applicationConfigurationDetailsObject != null &&
                            isTokenPropertyNull(applicationConfigurationDetailsObject, "phpErrorConfiguration") == false)
                        {
                            JObject errorConfigContainer = (JObject)applicationConfigurationDetailsObject["phpErrorConfiguration"];

                            errorDetectionRulesList.Add(fillErrorDetectionRule(applicationConfiguration, "PHP", "Mark BT As Error", getBoolValueFromJToken(errorConfigContainer, "markTransactionAsErrorOnErrorMessageLog").ToString()));
                            errorDetectionRulesList.Add(fillErrorDetectionRule(applicationConfiguration, "PHP", "Detect errors", getBoolValueFromJToken(errorConfigContainer, "detectPhpErrors").ToString()));
                            errorDetectionRulesList.Add(fillErrorDetectionRule(applicationConfiguration, "PHP", "Detect errors of Level", getStringValueFromJToken(errorConfigContainer, "errorThreshold")));
                            errorDetectionRulesList.Add(fillErrorDetectionRule(applicationConfiguration, "PHP", "Detect default HTTP error code", (getBoolValueFromJToken(errorConfigContainer, "disableDefaultHTTPErrorCode") == false).ToString()));

                            if (isTokenPropertyNull(errorConfigContainer, "ignoreExceptions") == false && isTokenPropertyNull(errorConfigContainer, "ignoreExceptionMsgPatterns") == false)
                            {
                                JArray ignoreExceptionsArray = (JArray)errorConfigContainer["ignoreExceptions"];
                                JArray ignoreExceptionsMessagesArray = (JArray)errorConfigContainer["ignoreExceptionMsgPatterns"];

                                if (ignoreExceptionsArray.Count > 0 && ignoreExceptionsMessagesArray.Count > 0)
                                {
                                    for (int j = 0; j < ignoreExceptionsArray.Count; j++)
                                    {
                                        try
                                        {
                                            errorDetectionIgnoreMessagesList.Add(fillErrorDetectionIgnoreException(applicationConfiguration, "PHP", ignoreExceptionsArray[j].ToString(), (JObject)ignoreExceptionsMessagesArray[j]));
                                        }
                                        catch { }
                                    }
                                }
                            }
                            if (isTokenPropertyNull(errorConfigContainer, "ignoreLoggerMsgPatterns") == false)
                            {
                                JArray ignoreMessagesArray = (JArray)errorConfigContainer["ignoreLoggerMsgPatterns"];

                                if (ignoreMessagesArray.Count > 0)
                                {
                                    for (int j = 0; j < ignoreMessagesArray.Count; j++)
                                    {
                                        errorDetectionIgnoreMessagesList.Add(fillErrorDetectionIgnoreException(applicationConfiguration, "PHP", String.Format("<Message {0}>", j), (JObject)ignoreMessagesArray[j]));
                                    }
                                }
                            }
                        }

                        #endregion

                        #region Node.JS

                        if (applicationConfigurationDetailsObject != null &&
                            isTokenPropertyNull(applicationConfigurationDetailsObject, "nodeJsErrorConfiguration") == false)
                        {
                            JObject errorConfigContainer = (JObject)applicationConfigurationDetailsObject["nodeJsErrorConfiguration"];

                            errorDetectionRulesList.Add(fillErrorDetectionRule(applicationConfiguration, "Node.js", "Mark BT As Error", getBoolValueFromJToken(errorConfigContainer, "markTransactionAsErrorOnErrorMessageLog").ToString()));
                            errorDetectionRulesList.Add(fillErrorDetectionRule(applicationConfiguration, "Node.js", "Detect default HTTP error code", (getBoolValueFromJToken(errorConfigContainer, "disableDefaultHTTPErrorCode") == false).ToString()));

                            if (isTokenPropertyNull(errorConfigContainer, "ignoreExceptions") == false && isTokenPropertyNull(errorConfigContainer, "ignoreExceptionMsgPatterns") == false)
                            {
                                JArray ignoreExceptionsArray = (JArray)errorConfigContainer["ignoreExceptions"];
                                JArray ignoreExceptionsMessagesArray = (JArray)errorConfigContainer["ignoreExceptionMsgPatterns"];

                                if (ignoreExceptionsArray.Count > 0 && ignoreExceptionsMessagesArray.Count > 0)
                                {
                                    for (int j = 0; j < ignoreExceptionsArray.Count; j++)
                                    {
                                        try
                                        {
                                            errorDetectionIgnoreMessagesList.Add(fillErrorDetectionIgnoreException(applicationConfiguration, "Node.js", ignoreExceptionsArray[j].ToString(), (JObject)ignoreExceptionsMessagesArray[j]));
                                        }
                                        catch { }
                                    }
                                }
                            }
                            if (isTokenPropertyNull(errorConfigContainer, "ignoreLoggerMsgPatterns") == false)
                            {
                                JArray ignoreMessagesArray = (JArray)errorConfigContainer["ignoreLoggerMsgPatterns"];

                                if (ignoreMessagesArray.Count > 0)
                                {
                                    for (int j = 0; j < ignoreMessagesArray.Count; j++)
                                    {
                                        errorDetectionIgnoreMessagesList.Add(fillErrorDetectionIgnoreException(applicationConfiguration, "Node.js", String.Format("<Message {0}>", j), (JObject)ignoreMessagesArray[j]));
                                    }
                                }
                            }
                            if (isTokenPropertyNull(errorConfigContainer, "httpErrorReturnCodes") == false)
                            {
                                JArray httpErrorCodesArray = (JArray)errorConfigContainer["httpErrorReturnCodes"];

                                if (httpErrorCodesArray.Count > 0)
                                {
                                    foreach (JObject httpErrorCodeObject in httpErrorCodesArray)
                                    {
                                        errorDetectionHTTPCodesList.Add(fillErrorDetectionHTTPCode(applicationConfiguration, "Node.js", httpErrorCodeObject));
                                    }
                                }
                            }
                        }

                        #endregion

                        #region Python

                        if (applicationConfigurationDetailsObject != null &&
                            isTokenPropertyNull(applicationConfigurationDetailsObject, "pythonErrorConfiguration") == false)
                        {
                            JObject errorConfigContainer = (JObject)applicationConfigurationDetailsObject["pythonErrorConfiguration"];

                            errorDetectionRulesList.Add(fillErrorDetectionRule(applicationConfiguration, "Python", "Mark BT As Error", getBoolValueFromJToken(errorConfigContainer, "markTransactionAsErrorOnErrorMessageLog").ToString()));
                            errorDetectionRulesList.Add(fillErrorDetectionRule(applicationConfiguration, "Python", "Detect errors", getBoolValueFromJToken(errorConfigContainer, "detectPythonErrors").ToString()));
                            errorDetectionRulesList.Add(fillErrorDetectionRule(applicationConfiguration, "Python", "Detect errors of Level", getStringValueFromJToken(errorConfigContainer, "errorThreshold")));
                            errorDetectionRulesList.Add(fillErrorDetectionRule(applicationConfiguration, "Python", "Detect default HTTP error code", (getBoolValueFromJToken(errorConfigContainer, "disableDefaultHTTPErrorCode") == false).ToString()));

                            if (isTokenPropertyNull(errorConfigContainer, "ignoreExceptions") == false && isTokenPropertyNull(errorConfigContainer, "ignoreExceptionMsgPatterns") == false)
                            {
                                JArray ignoreExceptionsArray = (JArray)errorConfigContainer["ignoreExceptions"];
                                JArray ignoreExceptionsMessagesArray = (JArray)errorConfigContainer["ignoreExceptionMsgPatterns"];

                                if (ignoreExceptionsArray.Count > 0 && ignoreExceptionsMessagesArray.Count > 0)
                                {
                                    for (int j = 0; j < ignoreExceptionsArray.Count; j++)
                                    {
                                        try
                                        {
                                            errorDetectionIgnoreMessagesList.Add(fillErrorDetectionIgnoreException(applicationConfiguration, "Python", ignoreExceptionsArray[j].ToString(), (JObject)ignoreExceptionsMessagesArray[j]));
                                        }
                                        catch { }
                                    }
                                }
                            }
                            if (isTokenPropertyNull(errorConfigContainer, "ignoreLoggerMsgPatterns") == false)
                            {
                                JArray ignoreMessagesArray = (JArray)errorConfigContainer["ignoreLoggerMsgPatterns"];

                                if (ignoreMessagesArray.Count > 0)
                                {
                                    for (int j = 0; j < ignoreMessagesArray.Count; j++)
                                    {
                                        errorDetectionIgnoreMessagesList.Add(fillErrorDetectionIgnoreException(applicationConfiguration, "Python", String.Format("<Message {0}>", j), (JObject)ignoreMessagesArray[j]));
                                    }
                                }
                            }
                            if (isTokenPropertyNull(errorConfigContainer, "httpErrorReturnCodes") == false)
                            {
                                JArray httpErrorCodesArray = (JArray)errorConfigContainer["httpErrorReturnCodes"];

                                if (httpErrorCodesArray.Count > 0)
                                {
                                    foreach (JObject httpErrorCodeObject in httpErrorCodesArray)
                                    {
                                        errorDetectionHTTPCodesList.Add(fillErrorDetectionHTTPCode(applicationConfiguration, "Python", httpErrorCodeObject));
                                    }
                                }
                            }
                        }

                        #endregion

                        #region Ruby

                        if (applicationConfigurationDetailsObject != null &&
                            isTokenPropertyNull(applicationConfigurationDetailsObject, "rubyErrorConfiguration") == false)
                        {
                            JObject errorConfigContainer = (JObject)applicationConfigurationDetailsObject["rubyErrorConfiguration"];

                            errorDetectionRulesList.Add(fillErrorDetectionRule(applicationConfiguration, "Ruby", "Mark BT As Error", getBoolValueFromJToken(errorConfigContainer, "markTransactionAsErrorOnErrorMessageLog").ToString()));
                            errorDetectionRulesList.Add(fillErrorDetectionRule(applicationConfiguration, "Ruby", "Detect default HTTP error code", (getBoolValueFromJToken(errorConfigContainer, "disableDefaultHTTPErrorCode") == false).ToString()));

                            if (isTokenPropertyNull(errorConfigContainer, "ignoreExceptions") == false && isTokenPropertyNull(errorConfigContainer, "ignoreExceptionMsgPatterns") == false)
                            {

                                JArray ignoreExceptionsArray = (JArray)errorConfigContainer["ignoreExceptions"];
                                JArray ignoreExceptionsMessagesArray = (JArray)errorConfigContainer["ignoreExceptionMsgPatterns"];

                                if (ignoreExceptionsArray.Count > 0 && ignoreExceptionsMessagesArray.Count > 0)
                                {
                                    for (int j = 0; j < ignoreExceptionsArray.Count; j++)
                                    {
                                        try
                                        {
                                            errorDetectionIgnoreMessagesList.Add(fillErrorDetectionIgnoreException(applicationConfiguration, "Ruby", ignoreExceptionsArray[j].ToString(), (JObject)ignoreExceptionsMessagesArray[j]));
                                        }
                                        catch { }
                                    }
                                }
                            }
                            if (isTokenPropertyNull(errorConfigContainer, "ignoreLoggerMsgPatterns") == false)
                            {
                                JArray ignoreMessagesArray = (JArray)errorConfigContainer["ignoreLoggerMsgPatterns"];

                                if (ignoreMessagesArray.Count > 0)
                                {
                                    for (int j = 0; j < ignoreMessagesArray.Count; j++)
                                    {
                                        errorDetectionIgnoreMessagesList.Add(fillErrorDetectionIgnoreException(applicationConfiguration, "Ruby", String.Format("<Message {0}>", j), (JObject)ignoreMessagesArray[j]));
                                    }
                                }
                            }
                        }

                        #endregion

                        FileIOHelper.WriteListToCSVFile(errorDetectionRulesList, new ErrorDetectionRuleReportMap(), FilePathMap.APMErrorDetectionRulesIndexFilePath(jobTarget));
                        FileIOHelper.WriteListToCSVFile(errorDetectionIgnoreMessagesList, new ErrorDetectionIgnoreMessageReportMap(), FilePathMap.APMErrorDetectionIgnoreMessagesIndexFilePath(jobTarget));
                        FileIOHelper.WriteListToCSVFile(errorDetectionIgnoreLoggersList, new ErrorDetectionIgnoreLoggerReportMap(), FilePathMap.APMErrorDetectionIgnoreLoggersIndexFilePath(jobTarget));
                        FileIOHelper.WriteListToCSVFile(errorDetectionLoggersList, new ErrorDetectionLoggerReportMap(), FilePathMap.APMErrorDetectionLoggersIndexFilePath(jobTarget));
                        FileIOHelper.WriteListToCSVFile(errorDetectionHTTPCodesList, new ErrorDetectionHTTPCodeReportMap(), FilePathMap.APMErrorDetectionHTTPCodesIndexFilePath(jobTarget));
                        FileIOHelper.WriteListToCSVFile(errorDetectionRedirectPagesList, new ErrorDetectionRedirectPageReportMap(), FilePathMap.APMErrorDetectionRedirectPagesIndexFilePath(jobTarget));

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + errorDetectionRulesList.Count + errorDetectionIgnoreMessagesList.Count + errorDetectionIgnoreLoggersList.Count + errorDetectionLoggersList.Count + errorDetectionHTTPCodesList.Count + errorDetectionRedirectPagesList.Count;

                        #endregion

                        #region Application Settings

                        List<APMApplicationConfiguration> applicationConfigurationsList = new List<APMApplicationConfiguration>(1);
                        applicationConfigurationsList.Add(applicationConfiguration);
                        FileIOHelper.WriteListToCSVFile(applicationConfigurationsList, new APMApplicationConfigurationReportMap(), FilePathMap.APMApplicationConfigurationIndexFilePath(jobTarget));

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + applicationConfigurationsList.Count;

                        #endregion

                        #region Save the updated Backends, Business Transactions and Service Endpoints

                        FileIOHelper.WriteListToCSVFile(backendsList, new APMBackendReportMap(), FilePathMap.APMBackendsReportFilePath());
                        FileIOHelper.WriteListToCSVFile(backendsThisAppList, new APMBackendReportMap(), FilePathMap.APMBackendsIndexFilePath(jobTarget));
                        FileIOHelper.WriteListToCSVFile(businessTransactionsList, new APMBusinessTransactionReportMap(), FilePathMap.APMBusinessTransactionsReportFilePath());
                        FileIOHelper.WriteListToCSVFile(businessTransactionsThisAppList, new APMBusinessTransactionReportMap(), FilePathMap.APMBusinessTransactionsIndexFilePath(jobTarget));
                        FileIOHelper.WriteListToCSVFile(serviceEndpointsList, new APMServiceEndpointReportMap(), FilePathMap.APMServiceEndpointsReportFilePath());
                        FileIOHelper.WriteListToCSVFile(serviceEndpointsThisAppList, new APMServiceEndpointReportMap(), FilePathMap.APMServiceEndpointsIndexFilePath(jobTarget));

                        #endregion

                        #region Combine All for Report CSV

                        // If it is the first one, clear out the combined folder
                        if (reportFolderCleaned == false)
                        {
                            FileIOHelper.DeleteFolder(FilePathMap.APMConfigurationReportFolderPath());
                            Thread.Sleep(1000);
                            FileIOHelper.CreateFolder(FilePathMap.APMConfigurationReportFolderPath());
                            reportFolderCleaned = true;
                        }

                        // Append all the individual report files into one
                        if (File.Exists(FilePathMap.APMApplicationConfigurationIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMApplicationConfigurationIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMApplicationConfigurationReportFilePath(), FilePathMap.APMApplicationConfigurationIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.APMBusinessTransactionDiscoveryRulesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMBusinessTransactionDiscoveryRulesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMBusinessTransactionDiscoveryRulesReportFilePath(), FilePathMap.APMBusinessTransactionDiscoveryRulesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.APMBusinessTransactionEntryRulesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMBusinessTransactionEntryRulesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMBusinessTransactionEntryRulesReportFilePath(), FilePathMap.APMBusinessTransactionEntryRulesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.APMServiceEndpointDiscoveryRulesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMServiceEndpointDiscoveryRulesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMServiceEndpointDiscoveryRulesReportFilePath(), FilePathMap.APMServiceEndpointDiscoveryRulesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.APMServiceEndpointEntryRulesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMServiceEndpointEntryRulesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMServiceEndpointEntryRulesReportFilePath(), FilePathMap.APMServiceEndpointEntryRulesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.APMBusinessTransactionEntryScopesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMBusinessTransactionEntryScopesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMBusinessTransactionEntryScopesReportFilePath(), FilePathMap.APMBusinessTransactionEntryScopesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.APMBusinessTransactionDiscoveryRules20IndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMBusinessTransactionDiscoveryRules20IndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMBusinessTransactionDiscoveryRules20ReportFilePath(), FilePathMap.APMBusinessTransactionDiscoveryRules20IndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.APMBusinessTransactionEntryRules20IndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMBusinessTransactionEntryRules20IndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMBusinessTransactionEntryRules20ReportFilePath(), FilePathMap.APMBusinessTransactionEntryRules20IndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.APMBackendDiscoveryRulesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMBackendDiscoveryRulesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMBackendDiscoveryRulesReportFilePath(), FilePathMap.APMBackendDiscoveryRulesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.APMCustomExitRulesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMCustomExitRulesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMCustomExitRulesReportFilePath(), FilePathMap.APMCustomExitRulesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.APMAgentConfigurationPropertiesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMAgentConfigurationPropertiesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMAgentConfigurationPropertiesReportFilePath(), FilePathMap.APMAgentConfigurationPropertiesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.APMInformationPointRulesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMInformationPointRulesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMInformationPointRulesReportFilePath(), FilePathMap.APMInformationPointRulesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.APMBusinessTransactionConfigurationsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMBusinessTransactionConfigurationsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMBusinessTransactionConfigurationsReportFilePath(), FilePathMap.APMBusinessTransactionConfigurationsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.APMTierConfigurationsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMTierConfigurationsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMTierConfigurationsReportFilePath(), FilePathMap.APMTierConfigurationsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.APMMethodInvocationDataCollectorsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMMethodInvocationDataCollectorsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMMethodInvocationDataCollectorsReportFilePath(), FilePathMap.APMMethodInvocationDataCollectorsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.APMHttpDataCollectorsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMHttpDataCollectorsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMHttpDataCollectorsReportFilePath(), FilePathMap.APMHttpDataCollectorsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.APMAgentCallGraphSettingsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMAgentCallGraphSettingsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMAgentCallGraphSettingsReportFilePath(), FilePathMap.APMAgentCallGraphSettingsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.APMDeveloperModeNodesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMDeveloperModeNodesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMDeveloperModeNodesReportFilePath(), FilePathMap.APMDeveloperModeNodesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.APMErrorDetectionRulesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMErrorDetectionRulesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMErrorDetectionRulesReportFilePath(), FilePathMap.APMErrorDetectionRulesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.APMErrorDetectionIgnoreMessagesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMErrorDetectionIgnoreMessagesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMErrorDetectionIgnoreMessagesReportFilePath(), FilePathMap.APMErrorDetectionIgnoreMessagesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.APMErrorDetectionIgnoreLoggersIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMErrorDetectionIgnoreLoggersIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMErrorDetectionIgnoreLoggersReportFilePath(), FilePathMap.APMErrorDetectionIgnoreLoggersIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.APMErrorDetectionLoggersIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMErrorDetectionLoggersIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMErrorDetectionLoggersReportFilePath(), FilePathMap.APMErrorDetectionLoggersIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.APMErrorDetectionHTTPCodesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMErrorDetectionHTTPCodesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMErrorDetectionHTTPCodesReportFilePath(), FilePathMap.APMErrorDetectionHTTPCodesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.APMErrorDetectionRedirectPagesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMErrorDetectionRedirectPagesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMErrorDetectionRedirectPagesReportFilePath(), FilePathMap.APMErrorDetectionRedirectPagesIndexFilePath(jobTarget));
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

                // Remove all templates from the list
                jobConfiguration.Target.RemoveAll(t => t.Controller == BLANK_APPLICATION_CONTROLLER);

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

        public override bool ShouldExecute(ProgramOptions programOptions, JobConfiguration jobConfiguration)
        {
            logger.Trace("LicensedReports.Configuration={0}", programOptions.LicensedReports.Configuration);
            loggerConsole.Trace("LicensedReports.Configuration={0}", programOptions.LicensedReports.Configuration);
            if (programOptions.LicensedReports.Configuration == false)
            {
                loggerConsole.Warn("Not licensed for configuration");
                return false;
            }


            logger.Trace("Input.Configuration={0}", jobConfiguration.Input.Configuration);
            loggerConsole.Trace("Input.Configuration={0}", jobConfiguration.Input.Configuration);
            if (jobConfiguration.Input.Configuration == false)
            {
                loggerConsole.Trace("Skipping index of configuration");
            }
            return (jobConfiguration.Input.Configuration == true);
        }

        private static string getNameValueDetailsFromNameValueCollection(XmlNode xmlNodeWithNameValuePairs)
        {
            if (xmlNodeWithNameValuePairs == null) return String.Empty;

            StringBuilder sb = new StringBuilder();
            foreach (XmlNode xmlNodeNameValue in xmlNodeWithNameValuePairs.SelectNodes("name-values"))
            {
                sb.AppendFormat("{0}={1};", xmlNodeNameValue.SelectSingleNode("name").InnerText, xmlNodeNameValue.SelectSingleNode("value").InnerText);
            }

            return sb.ToString();
        }

        private static string getNameValueDetailsFromParametersCollection(XmlNode xmlNodeWithParameters)
        {
            if (xmlNodeWithParameters == null) return String.Empty;

            StringBuilder sb = new StringBuilder();
            foreach (XmlNode xmlNodeNameValue in xmlNodeWithParameters.SelectNodes("parameter"))
            {
                sb.AppendFormat(
                    "{0}:{1}/{2}={3}/{4};",
                    xmlNodeNameValue.Attributes["match-type"].Value,
                    xmlNodeNameValue.SelectSingleNode("name").Attributes["filter-value"].Value,
                    xmlNodeNameValue.SelectSingleNode("name").Attributes["filter-type"].Value,
                    xmlNodeNameValue.SelectSingleNode("value").Attributes["filter-value"].Value,
                    xmlNodeNameValue.SelectSingleNode("value").Attributes["filter-type"].Value);
            }

            return sb.ToString();
        }

        private static BusinessTransactionDiscoveryRule fillBusinessTransactionDiscoveryRule(XmlNode entryMatchPointConfigurationNode, XmlNode entryMatchPointTransactionConfigurationNode, APMApplicationConfiguration applicationConfiguration, XmlNode applicationComponentNode)
        {
            BusinessTransactionDiscoveryRule businessTransactionDiscoveryRule = new BusinessTransactionDiscoveryRule();

            businessTransactionDiscoveryRule.Controller = applicationConfiguration.Controller;
            businessTransactionDiscoveryRule.ControllerLink = applicationConfiguration.ControllerLink;
            businessTransactionDiscoveryRule.ApplicationName = applicationConfiguration.ApplicationName;
            businessTransactionDiscoveryRule.ApplicationID = applicationConfiguration.ApplicationID;
            businessTransactionDiscoveryRule.ApplicationLink = applicationConfiguration.ApplicationLink;

            businessTransactionDiscoveryRule.AgentType = entryMatchPointConfigurationNode.SelectSingleNode("agent-type").InnerText;
            businessTransactionDiscoveryRule.EntryPointType = entryMatchPointTransactionConfigurationNode.Attributes["transaction-entry-point-type"].Value;
            businessTransactionDiscoveryRule.IsMonitoringEnabled = getBoolValueFromXmlNode(entryMatchPointTransactionConfigurationNode.SelectSingleNode("enable"));
            businessTransactionDiscoveryRule.DiscoveryType = entryMatchPointTransactionConfigurationNode.SelectSingleNode("discovery-config").Attributes["discovery-resolution"].Value;
            businessTransactionDiscoveryRule.IsDiscoveryEnabled = getBoolValueFromXmlNode(entryMatchPointTransactionConfigurationNode.SelectSingleNode("discovery-config/discovery-config-enabled"));
            businessTransactionDiscoveryRule.NamingConfigType = entryMatchPointTransactionConfigurationNode.SelectSingleNode("discovery-config/naming-config").Attributes["scheme"].Value;

            businessTransactionDiscoveryRule.RuleRawValue = makeXMLFormattedAndIndented(entryMatchPointTransactionConfigurationNode);

            if (applicationComponentNode != null)
            {
                businessTransactionDiscoveryRule.TierName = applicationComponentNode.SelectSingleNode("name").InnerText;
            }

            return businessTransactionDiscoveryRule;
        }

        private static BusinessTransactionEntryRule fillBusinessTransactionExcludeRule(XmlNode entryMatchPointConfigurationNode, XmlNode entryMatchPointTransactionConfigurationNode, XmlNode entryMatchPointCustomMatchPointConfigurationNode, APMApplicationConfiguration applicationConfiguration, XmlNode applicationComponentNode)
        {
            BusinessTransactionEntryRule businessTransactionEntryRule = new BusinessTransactionEntryRule();

            businessTransactionEntryRule.Controller = applicationConfiguration.Controller;
            businessTransactionEntryRule.ControllerLink = applicationConfiguration.ControllerLink;
            businessTransactionEntryRule.ApplicationName = applicationConfiguration.ApplicationName;
            businessTransactionEntryRule.ApplicationID = applicationConfiguration.ApplicationID;
            businessTransactionEntryRule.ApplicationLink = applicationConfiguration.ApplicationLink;

            businessTransactionEntryRule.AgentType = getStringValueFromXmlNode(entryMatchPointConfigurationNode.SelectSingleNode("agent-type"));
            businessTransactionEntryRule.EntryPointType = entryMatchPointTransactionConfigurationNode.Attributes["transaction-entry-point-type"].Value;
            businessTransactionEntryRule.RuleName = entryMatchPointCustomMatchPointConfigurationNode.Attributes["name"].Value;
            businessTransactionEntryRule.IsExclusion = true;

            XmlNode matchRule = entryMatchPointCustomMatchPointConfigurationNode.ChildNodes[0];
            fillMatchRuleDetails(businessTransactionEntryRule, matchRule);

            businessTransactionEntryRule.RuleRawValue = makeXMLFormattedAndIndented(entryMatchPointCustomMatchPointConfigurationNode);

            if (applicationComponentNode != null)
            {
                businessTransactionEntryRule.TierName = applicationComponentNode.SelectSingleNode("name").InnerText;
            }

            businessTransactionEntryRule.IsBuiltIn = BUILTIN_BT_MATCH_RULES.Contains(businessTransactionEntryRule.RuleName);

            return businessTransactionEntryRule;
        }

        private static BusinessTransactionEntryRule fillBusinessTransactionEntryRule(XmlNode entryMatchPointConfigurationNode, XmlNode entryMatchPointCustomMatchPointConfigurationNode, APMApplicationConfiguration applicationConfiguration, XmlNode applicationComponentNode, List<APMBusinessTransaction> businessTransactionsList)
        {
            BusinessTransactionEntryRule businessTransactionEntryRule = new BusinessTransactionEntryRule();

            businessTransactionEntryRule.Controller = applicationConfiguration.Controller;
            businessTransactionEntryRule.ControllerLink = applicationConfiguration.ControllerLink;
            businessTransactionEntryRule.ApplicationName = applicationConfiguration.ApplicationName;
            businessTransactionEntryRule.ApplicationID = applicationConfiguration.ApplicationID;
            businessTransactionEntryRule.ApplicationLink = applicationConfiguration.ApplicationLink;

            businessTransactionEntryRule.AgentType = getStringValueFromXmlNode(entryMatchPointConfigurationNode.SelectSingleNode("agent-type"));
            businessTransactionEntryRule.EntryPointType = entryMatchPointCustomMatchPointConfigurationNode.Attributes["transaction-entry-point-type"].Value;
            businessTransactionEntryRule.RuleName = getStringValueFromXmlNode(entryMatchPointCustomMatchPointConfigurationNode.SelectSingleNode("name"));
            businessTransactionEntryRule.IsBackground = getBoolValueFromXmlNode(entryMatchPointCustomMatchPointConfigurationNode.SelectSingleNode("background"));
            businessTransactionEntryRule.IsExclusion = false;

            XmlNode matchRule = entryMatchPointCustomMatchPointConfigurationNode.SelectSingleNode("match-rule").ChildNodes[0];
            fillMatchRuleDetails(businessTransactionEntryRule, matchRule);

            businessTransactionEntryRule.RuleRawValue = makeXMLFormattedAndIndented(entryMatchPointCustomMatchPointConfigurationNode);

            if (applicationComponentNode != null)
            {
                businessTransactionEntryRule.TierName = getStringValueFromXmlNode(applicationComponentNode.SelectSingleNode("name"));
            }

            if (businessTransactionsList != null)
            {
                List<APMBusinessTransaction> businessTransactionsForThisRule = new List<APMBusinessTransaction>();
                businessTransactionsForThisRule.AddRange(businessTransactionsList.Where(b => b.BTName == businessTransactionEntryRule.RuleName).ToList());
                businessTransactionsForThisRule.AddRange(businessTransactionsList.Where(b => b.BTName.StartsWith(String.Format("{0}.", businessTransactionEntryRule.RuleName))).ToList());
                businessTransactionsForThisRule.AddRange(businessTransactionsList.Where(b => b.BTNameOriginal == businessTransactionEntryRule.RuleName).ToList());
                businessTransactionsForThisRule.AddRange(businessTransactionsList.Where(b => b.BTNameOriginal.StartsWith(String.Format("{0}.", businessTransactionEntryRule.RuleName))).ToList());
                businessTransactionsForThisRule = businessTransactionsForThisRule.Distinct().ToList();
                businessTransactionEntryRule.NumDetectedBTs = businessTransactionsForThisRule.Count;
                if (businessTransactionsForThisRule.Count > 0)
                {
                    StringBuilder sb = new StringBuilder(32 * businessTransactionsForThisRule.Count);
                    foreach (APMBusinessTransaction bt in businessTransactionsForThisRule)
                    {
                        sb.AppendFormat("{0}/{1};\n", bt.TierName, bt.BTName);
                    }
                    sb.Remove(sb.Length - 1, 1);

                    businessTransactionEntryRule.DetectedBTs = sb.ToString();

                    // Now update the list of Entities to map which rule we are in
                    foreach (APMBusinessTransaction businessTransaction in businessTransactionsForThisRule)
                    {
                        businessTransaction.IsExplicitRule = true;
                        businessTransaction.RuleName = String.Format("{0}/{1} [{2}]", businessTransactionEntryRule.TierName, businessTransactionEntryRule.RuleName, businessTransactionEntryRule.EntryPointType);
                        businessTransaction.RuleName = businessTransaction.RuleName.TrimStart('/');
                    }
                }
            }

            businessTransactionEntryRule.IsBuiltIn = BUILTIN_BT_MATCH_RULES.Contains(businessTransactionEntryRule.RuleName);

            return businessTransactionEntryRule;
        }

        private static BusinessTransactionEntryScope fillBusinessTransactionEntryScope(XmlNode scopeConfigurationNode, XmlNode scopeToRuleMappingConfigurationNode, APMApplicationConfiguration applicationConfiguration)
        {
            BusinessTransactionEntryScope businessTransactionEntryScope = new BusinessTransactionEntryScope();

            businessTransactionEntryScope.Controller = applicationConfiguration.Controller;
            businessTransactionEntryScope.ControllerLink = applicationConfiguration.ControllerLink;
            businessTransactionEntryScope.ApplicationName = applicationConfiguration.ApplicationName;
            businessTransactionEntryScope.ApplicationID = applicationConfiguration.ApplicationID;
            businessTransactionEntryScope.ApplicationLink = applicationConfiguration.ApplicationLink;

            businessTransactionEntryScope.ScopeName = scopeConfigurationNode.Attributes["scope-name"].Value;
            businessTransactionEntryScope.ScopeType = scopeConfigurationNode.Attributes["scope-type"].Value;
            businessTransactionEntryScope.Description = scopeConfigurationNode.Attributes["scope-description"].Value;
            businessTransactionEntryScope.Version = Convert.ToInt32(scopeConfigurationNode.Attributes["scope-version"].Value);

            XmlNodeList includedTierNodeList = scopeConfigurationNode.SelectNodes("included-tiers/tier-name");
            businessTransactionEntryScope.NumTiers = includedTierNodeList.Count;
            if (businessTransactionEntryScope.NumTiers > 0)
            {
                List<string> includedTiersList = new List<string>(businessTransactionEntryScope.NumTiers);
                foreach (XmlNode includedTierNode in includedTierNodeList)
                {
                    includedTiersList.Add(includedTierNode.InnerText);
                }
                includedTiersList.Sort();

                StringBuilder sb = new StringBuilder(32 * businessTransactionEntryScope.NumTiers);
                foreach (string includedTier in includedTiersList)
                {
                    sb.AppendFormat("{0};\n", includedTier);
                }
                sb.Remove(sb.Length - 1, 1);
                businessTransactionEntryScope.AffectedTiers = sb.ToString();
            }
            else
            {
                XmlNodeList excludedTierNodeList = scopeConfigurationNode.SelectNodes("excluded-tiers/tier-name");
                businessTransactionEntryScope.NumTiers = excludedTierNodeList.Count;
                if (businessTransactionEntryScope.NumTiers > 0)
                {
                    List<string> excludedTiersList = new List<string>(businessTransactionEntryScope.NumTiers);
                    foreach (XmlNode excludedTierNode in excludedTierNodeList)
                    {
                        excludedTiersList.Add(excludedTierNode.InnerText);
                    }
                    excludedTiersList.Sort();

                    StringBuilder sb = new StringBuilder(32 * businessTransactionEntryScope.NumTiers);
                    foreach (string excludedTier in excludedTiersList)
                    {
                        sb.AppendFormat("{0};\n", excludedTier);
                    }
                    sb.Remove(sb.Length - 1, 1);
                    businessTransactionEntryScope.AffectedTiers = sb.ToString();
                }
            }

            XmlNodeList ruleMappingNodeList = scopeToRuleMappingConfigurationNode.SelectNodes(String.Format("scope-rule-mapping[@scope-name='{0}']/rule", businessTransactionEntryScope.ScopeName));
            businessTransactionEntryScope.NumRules = ruleMappingNodeList.Count;
            if (businessTransactionEntryScope.NumRules > 0)
            {
                List<string> ruleMappingList = new List<string>(businessTransactionEntryScope.NumRules);
                foreach (XmlNode ruleMappingNode in ruleMappingNodeList)
                {
                    string ruleName = ruleMappingNode.Attributes["rule-name"].Value;
                    string ruleDescription = ruleMappingNode.Attributes["rule-description"].Value;
                    string ruleNameAndDescription = String.Empty;
                    if (ruleDescription.Length > 0 && ruleDescription != ruleName)
                    {
                        ruleMappingList.Add(String.Format("{0} ({1})", ruleName, ruleDescription));
                    }
                    else
                    {
                        ruleMappingList.Add(ruleName);
                    }
                }
                ruleMappingList.Sort();

                StringBuilder sb = new StringBuilder(32 * businessTransactionEntryScope.NumRules);
                foreach (string ruleMapping in ruleMappingList)
                {
                    sb.AppendFormat("{0};\n", ruleMapping);
                }
                sb.Remove(sb.Length - 1, 1);
                businessTransactionEntryScope.IncludedRules = sb.ToString();
            }

            return businessTransactionEntryScope;
        }

        private static BusinessTransactionEntryRule20 fillBusinessTransactionEntryRule20(XmlNode ruleConfigurationNode, XmlNode scopeToRuleMappingConfigurationNode, APMApplicationConfiguration applicationConfiguration, List<APMBusinessTransaction> businessTransactionsList)
        {
            BusinessTransactionEntryRule20 businessTransactionEntryRule = new BusinessTransactionEntryRule20();

            businessTransactionEntryRule.Controller = applicationConfiguration.Controller;
            businessTransactionEntryRule.ControllerLink = applicationConfiguration.ControllerLink;
            businessTransactionEntryRule.ApplicationName = applicationConfiguration.ApplicationName;
            businessTransactionEntryRule.ApplicationID = applicationConfiguration.ApplicationID;
            businessTransactionEntryRule.ApplicationLink = applicationConfiguration.ApplicationLink;

            businessTransactionEntryRule.AgentType = ruleConfigurationNode.Attributes["agent-type"].Value;
            businessTransactionEntryRule.RuleName = ruleConfigurationNode.Attributes["rule-name"].Value;
            businessTransactionEntryRule.Description = ruleConfigurationNode.Attributes["rule-description"].Value;
            businessTransactionEntryRule.Version = Convert.ToInt32(ruleConfigurationNode.Attributes["version"].Value);

            businessTransactionEntryRule.IsEnabled = Convert.ToBoolean(ruleConfigurationNode.Attributes["enabled"].Value);
            businessTransactionEntryRule.Priority = Convert.ToInt32(ruleConfigurationNode.Attributes["priority"].Value);

            JObject txRuleSettings = JObject.Parse(getStringValueFromXmlNode(ruleConfigurationNode.SelectSingleNode("tx-match-rule")));
            if (txRuleSettings != null)
            {
                if (txRuleSettings["type"].ToString() != "CUSTOM")
                {
                    // This is likely autodiscovery rule, do not fill it out and bail
                    return null;
                }

                JToken txCustomRuleSettings = txRuleSettings["txcustomrule"];
                if (txCustomRuleSettings != null)
                {
                    if (txCustomRuleSettings["type"].ToString() == "EXCLUDE")
                    {
                        businessTransactionEntryRule.IsExclusion = true;
                    }
                    else if (txCustomRuleSettings["type"].ToString() == "INCLUDE")
                    {
                        businessTransactionEntryRule.IsExclusion = false;
                    }

                    businessTransactionEntryRule.EntryPointType = txCustomRuleSettings["txentrypointtype"].ToString();

                    JToken isBackgroundProperty = txCustomRuleSettings["properties"].Where(p => p["name"].ToString() == "BACKGROUND_TASK").FirstOrDefault();
                    if (isBackgroundProperty != null)
                    {
                        businessTransactionEntryRule.IsBackground = (bool)isBackgroundProperty["booleanvalue"];
                    }

                    businessTransactionEntryRule.MatchConditions = txCustomRuleSettings["matchconditions"].ToString();
                    businessTransactionEntryRule.Actions = txCustomRuleSettings["actions"].ToString();
                    businessTransactionEntryRule.Properties = txCustomRuleSettings["properties"].ToString();
                }
            }

            // I really want to do it, but some of our rules have apostrophes
            // Spring WS - Base servlet for Spring's web framework
            // And the query for scope-rule-mapping/rule[@rule-name='Spring WS - Base servlet for Spring's web framework'] breaks
            // So going to do it the hard way
            //XmlNode scopeForThisRuleNode = scopeToRuleMappingConfigurationNode.SelectSingleNode(String.Format("scope-rule-mapping/rule[@rule-name='{0}']", businessTransactionEntryRule.RuleName));
            foreach (XmlNode scopeNode in scopeToRuleMappingConfigurationNode.SelectNodes("scope-rule-mapping/rule"))
            {
                if (scopeNode.Attributes["rule-name"].Value == businessTransactionEntryRule.RuleName)
                {
                    businessTransactionEntryRule.ScopeName = scopeNode.ParentNode.Attributes["scope-name"].Value;
                    break;
                }
            }

            if (businessTransactionsList != null)
            {
                List<APMBusinessTransaction> businessTransactionsForThisRule = new List<APMBusinessTransaction>();
                businessTransactionsForThisRule.AddRange(businessTransactionsList.Where(b => b.BTName == businessTransactionEntryRule.RuleName).ToList());
                businessTransactionsForThisRule.AddRange(businessTransactionsList.Where(b => b.BTName.StartsWith(String.Format("{0}.", businessTransactionEntryRule.RuleName))).ToList());
                businessTransactionsForThisRule.AddRange(businessTransactionsList.Where(b => b.BTNameOriginal == businessTransactionEntryRule.RuleName).ToList());
                businessTransactionsForThisRule.AddRange(businessTransactionsList.Where(b => b.BTNameOriginal.StartsWith(String.Format("{0}.", businessTransactionEntryRule.RuleName))).ToList());
                businessTransactionsForThisRule = businessTransactionsForThisRule.Distinct().ToList();
                businessTransactionEntryRule.NumDetectedBTs = businessTransactionsForThisRule.Count;
                if (businessTransactionsForThisRule.Count > 0)
                {
                    StringBuilder sb = new StringBuilder(32 * businessTransactionsForThisRule.Count);
                    foreach (APMBusinessTransaction bt in businessTransactionsForThisRule)
                    {
                        sb.AppendFormat("{0}/{1};\n", bt.TierName, bt.BTName);
                    }
                    sb.Remove(sb.Length - 1, 1);

                    businessTransactionEntryRule.DetectedBTs = sb.ToString();

                    // Now update the list of Entities to map which rule we are in
                    foreach (APMBusinessTransaction businessTransaction in businessTransactionsForThisRule)
                    {
                        businessTransaction.IsExplicitRule = true;
                        businessTransaction.RuleName = String.Format("{0}/{1} [{2}]", businessTransactionEntryRule.ScopeName, businessTransactionEntryRule.RuleName, businessTransactionEntryRule.EntryPointType);
                        businessTransaction.RuleName = businessTransaction.RuleName.TrimStart('/');
                    }
                }
            }

            businessTransactionEntryRule.RuleRawValue = makeXMLFormattedAndIndented(ruleConfigurationNode);

            businessTransactionEntryRule.IsBuiltIn = BUILTIN_BT_MATCH_RULES.Contains(businessTransactionEntryRule.RuleName);

            return businessTransactionEntryRule;
        }

        private static List<BusinessTransactionDiscoveryRule20> fillBusinessTransactionDiscoveryRule20(XmlNode ruleConfigurationNode, XmlNode scopeToRuleMappingConfigurationNode, APMApplicationConfiguration applicationConfiguration, List<APMBusinessTransaction> businessTransactionsList)
        {
            List<BusinessTransactionDiscoveryRule20> businessTransactionDiscoveryRule20List = new List<BusinessTransactionDiscoveryRule20>();

            JObject txRuleSettings = JObject.Parse(getStringValueFromXmlNode(ruleConfigurationNode.SelectSingleNode("tx-match-rule")));
            if (txRuleSettings != null)
            {
                if (getStringValueFromJToken(txRuleSettings, "type") != "AUTOMATIC_DISCOVERY")
                {
                    // This is not an autodiscovery rule, do not fill it out and bail
                    return null;
                }

                if (isTokenPropertyNull(txRuleSettings, "txautodiscoveryrule") == false &&
                    isTokenPropertyNull(txRuleSettings["txautodiscoveryrule"], "autodiscoveryconfigs") == false)
                {
                    JArray txDiscoveryConfigs = (JArray)txRuleSettings["txautodiscoveryrule"]["autodiscoveryconfigs"];
                    if (txDiscoveryConfigs != null && txDiscoveryConfigs.Count > 0)
                    {
                        foreach (JToken txDiscoveryConfig in txDiscoveryConfigs)
                        {
                            BusinessTransactionDiscoveryRule20 businessTransactionDiscoveryRule20 = new BusinessTransactionDiscoveryRule20();

                            businessTransactionDiscoveryRule20.Controller = applicationConfiguration.Controller;
                            businessTransactionDiscoveryRule20.ControllerLink = applicationConfiguration.ControllerLink;
                            businessTransactionDiscoveryRule20.ApplicationName = applicationConfiguration.ApplicationName;
                            businessTransactionDiscoveryRule20.ApplicationID = applicationConfiguration.ApplicationID;
                            businessTransactionDiscoveryRule20.ApplicationLink = applicationConfiguration.ApplicationLink;

                            businessTransactionDiscoveryRule20.AgentType = ruleConfigurationNode.Attributes["agent-type"].Value;
                            businessTransactionDiscoveryRule20.RuleName = ruleConfigurationNode.Attributes["rule-name"].Value;
                            businessTransactionDiscoveryRule20.Description = ruleConfigurationNode.Attributes["rule-description"].Value;
                            businessTransactionDiscoveryRule20.Version = Convert.ToInt32(ruleConfigurationNode.Attributes["version"].Value);

                            businessTransactionDiscoveryRule20.IsEnabled = Convert.ToBoolean(ruleConfigurationNode.Attributes["enabled"].Value);
                            businessTransactionDiscoveryRule20.Priority = Convert.ToInt32(ruleConfigurationNode.Attributes["priority"].Value);

                            businessTransactionDiscoveryRule20.EntryPointType = getStringValueFromJToken(txDiscoveryConfig, "txentrypointtype");
                            businessTransactionDiscoveryRule20.IsMonitoringEnabled = getBoolValueFromJToken(txDiscoveryConfig, "monitoringenabled");
                            businessTransactionDiscoveryRule20.IsDiscoveryEnabled = getBoolValueFromJToken(txDiscoveryConfig, "discoveryenabled");
                            businessTransactionDiscoveryRule20.NamingConfigType = getStringValueFromJToken(txDiscoveryConfig, "namingschemetype");

                            businessTransactionDiscoveryRule20.HTTPAutoDiscovery = getStringValueOfObjectFromJToken(txDiscoveryConfig, "httpautodiscovery");

                            // I really want to do it, but some of our rules have apostrophes
                            // Spring WS - Base servlet for Spring's web framework
                            // And the query for scope-rule-mapping/rule[@rule-name='Spring WS - Base servlet for Spring's web framework'] breaks
                            // So going to do it the hard way
                            //XmlNode scopeForThisRuleNode = scopeToRuleMappingConfigurationNode.SelectSingleNode(String.Format("scope-rule-mapping/rule[@rule-name='{0}']", businessTransactionEntryRule.RuleName));
                            foreach (XmlNode scopeNode in scopeToRuleMappingConfigurationNode.SelectNodes("scope-rule-mapping/rule"))
                            {
                                if (scopeNode.Attributes["rule-name"].Value == businessTransactionDiscoveryRule20.RuleName)
                                {
                                    businessTransactionDiscoveryRule20.ScopeName = scopeNode.ParentNode.Attributes["scope-name"].Value;
                                    businessTransactionDiscoveryRule20.TierName = businessTransactionDiscoveryRule20.ScopeName;
                                    break;
                                }
                            }

                            businessTransactionDiscoveryRule20List.Add(businessTransactionDiscoveryRule20);
                        }
                    }
                }
            }

            return businessTransactionDiscoveryRule20List;
        }

        private static BackendDiscoveryRule fillBackendDiscoveryRule(XmlNode backendDiscoveryMatchPointConfigurationNode, XmlNode backendDiscoveryConfigurationNode, APMApplicationConfiguration applicationConfiguration, XmlNode applicationComponentNode, List<APMBackend> backendsList)
        {
            BackendDiscoveryRule backendDiscoveryRule = new BackendDiscoveryRule();

            backendDiscoveryRule.Controller = applicationConfiguration.Controller;
            backendDiscoveryRule.ControllerLink = applicationConfiguration.ControllerLink;
            backendDiscoveryRule.ApplicationName = applicationConfiguration.ApplicationName;
            backendDiscoveryRule.ApplicationID = applicationConfiguration.ApplicationID;
            backendDiscoveryRule.ApplicationLink = applicationConfiguration.ApplicationLink;

            backendDiscoveryRule.AgentType = getStringValueFromXmlNode(backendDiscoveryMatchPointConfigurationNode.SelectSingleNode("agent-type"));
            backendDiscoveryRule.ExitType = getStringValueFromXmlNode(backendDiscoveryConfigurationNode.SelectSingleNode("exit-point-type"));
            if (backendDiscoveryRule.ExitType == "CUSTOM")
            {
                backendDiscoveryRule.ExitType = getStringValueFromXmlNode(backendDiscoveryConfigurationNode.SelectSingleNode("exit-point-subtype"));
            }
            backendDiscoveryRule.RuleName = getStringValueFromXmlNode(backendDiscoveryConfigurationNode.SelectSingleNode("name"));
            backendDiscoveryRule.IsEnabled = getBoolValueFromXmlNode(backendDiscoveryConfigurationNode.SelectSingleNode("discovery-enabled"));
            backendDiscoveryRule.IsCorrelationSupported = getBoolValueFromXmlNode(backendDiscoveryConfigurationNode.SelectSingleNode("supports-correlation"));
            backendDiscoveryRule.IsCorrelationEnabled = getBoolValueFromXmlNode(backendDiscoveryConfigurationNode.SelectSingleNode("correlation-enabled"));
            backendDiscoveryRule.Priority = getIntegerValueFromXmlNode(backendDiscoveryConfigurationNode.SelectSingleNode("priority"));

            backendDiscoveryRule.IdentityOptions = makeXMLFormattedAndIndented(backendDiscoveryConfigurationNode.SelectSingleNode("backend-identity-options"));
            backendDiscoveryRule.DiscoveryConditions = makeXMLFormattedAndIndented(backendDiscoveryConfigurationNode.SelectSingleNode("backend-discovery-conditions"));

            backendDiscoveryRule.RuleRawValue = makeXMLFormattedAndIndented(backendDiscoveryConfigurationNode);

            if (applicationComponentNode != null)
            {
                backendDiscoveryRule.TierName = getStringValueFromXmlNode(applicationComponentNode.SelectSingleNode("name"));
            }

            if (backendsList != null)
            {
                List<APMBackend> backendsForThisRule = new List<APMBackend>();

                // Try to find them by match first
                backendsForThisRule.AddRange(backendsList.Where(b => b.BackendName == backendDiscoveryRule.RuleName).ToList());
                backendsForThisRule.AddRange(backendsList.Where(b => b.BackendName.StartsWith(backendDiscoveryRule.RuleName)).ToList());
                backendsForThisRule = backendsForThisRule.Distinct().ToList();
                if (backendsForThisRule.Count == 0)
                {
                    // If by name doesn't match, let's do by type
                    // Nope, this doesn't work. Backend is differentiated by the Agent Type
                    // Because of that the backend matches every darn type starting with Default
                    // backendsForThisRule.AddRange(backendsList.Where(b => b.BackendType == backendDiscoveryRule.ExitType).ToList());
                }
                backendDiscoveryRule.NumDetectedBackends = backendsForThisRule.Count;
                if (backendsForThisRule.Count > 0)
                {
                    StringBuilder sb = new StringBuilder(32 * backendsForThisRule.Count);
                    foreach (APMBackend backend in backendsForThisRule)
                    {
                        sb.AppendFormat("{0} ({1});\n", backend.BackendName, backend.BackendID);
                    }
                    sb.Remove(sb.Length - 1, 1);

                    backendDiscoveryRule.DetectedBackends = sb.ToString();

                    // Now update the list of Entities to map which rule we are in
                    foreach (APMBackend backend in backendsForThisRule)
                    {
                        backend.IsExplicitRule = true;
                        backend.RuleName = String.Format("{0}/{1} [{2}]", backendDiscoveryRule.TierName, backendDiscoveryRule.RuleName, backendDiscoveryRule.ExitType);
                        backend.RuleName = backend.RuleName.TrimStart('/');
                    }
                }
            }

            //Default.NET Azure Service Fabric configuration
            //Default.NET HTTP configuration
            //Default.NET JMS configuration
            //Default.NET Remoting configuration
            //Default.NET WCF configuration
            //Default.NET WebService configuration
            //Default ADO.NET configuration
            //Default Amazon S3 configuration
            //Default Amazon SNS configuration
            //Default Amazon SQS configuration
            //Default AWS Configuration
            //Default Axon configuration
            //Default Cassandra CQL configuration
            //Default Couchbase configuration
            //Default Database configuration
            //Default HTTP configuration
            //Default JDBC configuration
            //Default JMS configuration
            //Default Jolt configuration
            //Default Kafka configuration
            //Default Module configuration
            //Default MongoDB configuration
            //Default MQ configuration
            //Default NodeJS Cache configuration
            //Default Nodejs Cassandra configuration
            //Default Nodejs Couchbase configuration
            //Default NodeJS DB configuration
            //Default NodeJS HTTP configuration
            //Default Nodejs MongoDB configuration
            //Default PHP Cache configuration
            //Default PHP DB configuration
            //Default PHP HTTP configuration
            //Default PHP RabbitMQ configuration
            //Default PHP WebService configuration
            //Default Python Cache configuration
            //Default Python DB configuration
            //Default Python HTTP configuration
            //Default Python MongoDB configuration
            //Default RabbitMQ configuration
            //Default RMI configuration
            //Default Ruby Cache configuration
            //Default Ruby DB configuration
            //Default Ruby HTTP configuration
            //Default Thrift configuration
            //Default Vertx Message configuration
            //Default WebService configuration
            //Default WebSocket configuration

            backendDiscoveryRule.IsBuiltIn = backendDiscoveryRule.RuleName.ToLower().StartsWith("default") && backendDiscoveryRule.RuleName.ToLower().EndsWith("configuration");

            return backendDiscoveryRule;
        }

        private static ServiceEndpointDiscoveryRule fillServiceEnpointDiscoveryRule(JObject serviceEndpointRuleObject, APMTier tier, APMApplicationConfiguration applicationConfiguration)
        {
            ServiceEndpointDiscoveryRule serviceEndpointDiscoveryRule = new ServiceEndpointDiscoveryRule();

            serviceEndpointDiscoveryRule.Controller = applicationConfiguration.Controller;
            serviceEndpointDiscoveryRule.ControllerLink = applicationConfiguration.ControllerLink;
            serviceEndpointDiscoveryRule.ApplicationName = applicationConfiguration.ApplicationName;
            serviceEndpointDiscoveryRule.ApplicationID = applicationConfiguration.ApplicationID;
            serviceEndpointDiscoveryRule.ApplicationLink = applicationConfiguration.ApplicationLink;

            serviceEndpointDiscoveryRule.AgentType = getStringValueFromJToken(serviceEndpointRuleObject, "agentType");
            serviceEndpointDiscoveryRule.RuleName = getStringValueFromJToken(serviceEndpointRuleObject, "name");
            serviceEndpointDiscoveryRule.EntryPointType = getStringValueFromJToken(serviceEndpointRuleObject, "entryPointType");
            serviceEndpointDiscoveryRule.Version = getIntValueFromJToken(serviceEndpointRuleObject, "version");
            serviceEndpointDiscoveryRule.IsEnabled = getBoolValueFromJToken(serviceEndpointRuleObject, "enabled");
            if (isTokenPropertyNull(serviceEndpointRuleObject, "discoveryConfig") == false)
            {
                JObject discoveryConfigObject = (JObject)serviceEndpointRuleObject["discoveryConfig"];
                serviceEndpointDiscoveryRule.NamingConfigType = getStringValueFromJToken(discoveryConfigObject, "namingSchemeType");

                if (isTokenPropertyNull(discoveryConfigObject, "properties") == false)
                {
                    string[] nameValues = discoveryConfigObject["properties"].Select(s => String.Format("{0}={1}", getStringValueFromJToken(s, "name"), getStringValueFromJToken(s, "value"))).ToArray();
                    serviceEndpointDiscoveryRule.DiscoveryType = String.Join(";", nameValues);
                }
            }

            if (tier != null) serviceEndpointDiscoveryRule.TierName = tier.TierName;

            return serviceEndpointDiscoveryRule;
        }

        private static ServiceEndpointEntryRule fillServiceEnpointEntryRule(JObject serviceEndpointRuleObject, APMTier tier, APMApplicationConfiguration applicationConfiguration, List<APMServiceEndpoint> serviceEndpointsList)
        {
            ServiceEndpointEntryRule serviceEndpointEntryRule = new ServiceEndpointEntryRule();

            serviceEndpointEntryRule.Controller = applicationConfiguration.Controller;
            serviceEndpointEntryRule.ControllerLink = applicationConfiguration.ControllerLink;
            serviceEndpointEntryRule.ApplicationName = applicationConfiguration.ApplicationName;
            serviceEndpointEntryRule.ApplicationID = applicationConfiguration.ApplicationID;
            serviceEndpointEntryRule.ApplicationLink = applicationConfiguration.ApplicationLink;

            serviceEndpointEntryRule.AgentType = getStringValueFromJToken(serviceEndpointRuleObject, "agentType");
            serviceEndpointEntryRule.RuleName = getStringValueFromJToken(serviceEndpointRuleObject, "name");
            serviceEndpointEntryRule.EntryPointType = getStringValueFromJToken(serviceEndpointRuleObject, "entryPointType");
            serviceEndpointEntryRule.Version = getIntValueFromJToken(serviceEndpointRuleObject, "version");
            if (isTokenPropertyNull(serviceEndpointRuleObject, "matchPointRule") == false)
            {
                JObject discoveryConfigObject = (JObject)serviceEndpointRuleObject["matchPointRule"];

                serviceEndpointEntryRule.Priority = getIntValueFromJToken(discoveryConfigObject, "priority");
                serviceEndpointEntryRule.IsEnabled = getBoolValueFromJToken(discoveryConfigObject, "enabled");
                serviceEndpointEntryRule.IsExclusion = getBoolValueFromJToken(discoveryConfigObject, "excluded");

                serviceEndpointEntryRule.MatchConditions = getStringValueOfObjectFromJToken(serviceEndpointRuleObject, "matchPointRule", false);
                
                // This is for POCO/POJOs
                serviceEndpointEntryRule.Actions = getStringValueOfObjectFromJToken(discoveryConfigObject, "splitConfig", false);
                
                // This is for ASP.NET/Servlet
                if (serviceEndpointEntryRule.Actions.Length == 0)
                {
                    serviceEndpointEntryRule.Actions = getStringValueOfObjectFromJToken(discoveryConfigObject, "ruleProperties", false);
                }
            }

            serviceEndpointEntryRule.TierName = tier.TierName;

            if (serviceEndpointsList != null)
            {
                List<APMServiceEndpoint> serviceEndpointsForThisRule = new List<APMServiceEndpoint>();

                serviceEndpointsForThisRule.AddRange(serviceEndpointsList.Where(s => s.SEPName == serviceEndpointEntryRule.RuleName).ToList());
                serviceEndpointsForThisRule.AddRange(serviceEndpointsList.Where(s => s.SEPName.StartsWith(String.Format("{0}.", serviceEndpointEntryRule.RuleName))).ToList());
                serviceEndpointsForThisRule = serviceEndpointsForThisRule.Distinct().ToList();
                serviceEndpointEntryRule.NumDetectedSEPs = serviceEndpointsForThisRule.Count;
                if (serviceEndpointsForThisRule.Count > 0)
                {
                    StringBuilder sb = new StringBuilder(32 * serviceEndpointsForThisRule.Count);
                    foreach (APMServiceEndpoint sep in serviceEndpointsForThisRule)
                    {
                        sb.AppendFormat("{0}/{1};\n", sep.TierName, sep.SEPName);
                    }
                    sb.Remove(sb.Length - 1, 1);

                    serviceEndpointEntryRule.DetectedSEPs = sb.ToString();

                    // Now update the list of Entities to map which rule we are in
                    foreach (APMServiceEndpoint serviceEndpoint in serviceEndpointsForThisRule)
                    {
                        serviceEndpoint.IsExplicitRule = true;
                        serviceEndpoint.RuleName = String.Format("{0}/{1} [{2}]", serviceEndpointEntryRule.TierName, serviceEndpointEntryRule.RuleName, serviceEndpointEntryRule.EntryPointType);
                        serviceEndpoint.RuleName = serviceEndpoint.RuleName.TrimStart('/');
                    }
                }
            }
            return serviceEndpointEntryRule;
        }

        private static CustomExitRule fillCustomExitRule(XmlNode backendDiscoveryMatchPointConfigurationNode, XmlNode customExitConfigurationNode, APMApplicationConfiguration applicationConfiguration, XmlNode applicationComponentNode, List<APMBackend> backendsList)
        {
            CustomExitRule customExitRule = new CustomExitRule();

            customExitRule.Controller = applicationConfiguration.Controller;
            customExitRule.ControllerLink = applicationConfiguration.ControllerLink;
            customExitRule.ApplicationName = applicationConfiguration.ApplicationName;
            customExitRule.ApplicationID = applicationConfiguration.ApplicationID;
            customExitRule.ApplicationLink = applicationConfiguration.ApplicationLink;

            customExitRule.AgentType = getStringValueFromXmlNode(backendDiscoveryMatchPointConfigurationNode.SelectSingleNode("agent-type"));
            customExitRule.ExitType = getStringValueFromXmlNode(customExitConfigurationNode.SelectSingleNode("type"));

            customExitRule.RuleName = getStringValueFromXmlNode(customExitConfigurationNode.SelectSingleNode("name"));
            customExitRule.MatchClass = getStringValueFromXmlNode(customExitConfigurationNode.SelectSingleNode("instrumentation-point/pojo-method-definition/class-name"));
            customExitRule.MatchMethod = getStringValueFromXmlNode(customExitConfigurationNode.SelectSingleNode("instrumentation-point/pojo-method-definition/method-name"));
            customExitRule.MatchType = getStringValueFromXmlNode(customExitConfigurationNode.SelectSingleNode("instrumentation-point/pojo-method-definition/match-type"));
            customExitRule.MatchParameterTypes = getStringValueFromXmlNode(customExitConfigurationNode.SelectSingleNode("instrumentation-point/pojo-method-definition/method-parameter-types"));

            customExitRule.IsApplyToAllBTs = getBoolValueFromXmlNode(customExitConfigurationNode.SelectSingleNode("instrumentation-point/apply-to-all-bts"));

            customExitRule.DataCollectorsConfig = makeXMLFormattedAndIndented(String.Format("<method-invocation-data-gatherer-configs>{0}</method-invocation-data-gatherer-configs>", makeXMLFormattedAndIndented(customExitConfigurationNode.SelectNodes("instrumentation-point/method-invocation-data-gatherer-config"))));
            customExitRule.InfoPointsConfig = makeXMLFormattedAndIndented(String.Format("<info-point-metric-definitions>{0}</info-point-metric-definitions>", makeXMLFormattedAndIndented(customExitConfigurationNode.SelectNodes("instrumentation-point/info-point-metric-definition"))));

            customExitRule.RuleRawValue = makeXMLFormattedAndIndented(customExitConfigurationNode);

            if (applicationComponentNode != null)
            {
                customExitRule.TierName = getStringValueFromXmlNode(applicationComponentNode.SelectSingleNode("name"));
            }

            if (backendsList != null)
            {
                List<APMBackend> backendsForThisRule = new List<APMBackend>();

                // Try to find them by match first
                backendsForThisRule.AddRange(backendsList.Where(b => b.BackendName == customExitRule.RuleName).ToList());
                backendsForThisRule.AddRange(backendsList.Where(b => b.BackendName.StartsWith(String.Format("{0}", customExitRule.RuleName))).ToList());
                backendsForThisRule = backendsForThisRule.Distinct().ToList();
                if (backendsForThisRule.Count == 0)
                {
                    // If by name doesn't match, let's do by type
                    // Nope, this doesn't work. Backend is differentiated by the Agent Type
                    // Because of that the backend matches every darn type starting with Default
                    // backendsForThisRule.AddRange(backendsList.Where(b => b.BackendType == customExitRule.ExitType).ToList());
                }
                customExitRule.NumDetectedBackends = backendsForThisRule.Count;
                if (backendsForThisRule.Count > 0)
                {
                    StringBuilder sb = new StringBuilder(32 * backendsForThisRule.Count);
                    foreach (APMBackend backend in backendsForThisRule)
                    {
                        sb.AppendFormat("{0} ({1});\n", backend.BackendName, backend.BackendID);
                    }
                    sb.Remove(sb.Length - 1, 1);

                    customExitRule.DetectedBackends = sb.ToString();

                    // Now update the list of Entities to map which rule we are in
                    foreach (APMBackend backend in backendsForThisRule)
                    {
                        backend.IsExplicitRule = true;
                        backend.RuleName = String.Format("{0}/{1} [{2}]", customExitRule.TierName, customExitRule.RuleName, customExitRule.ExitType);
                        backend.RuleName = backend.RuleName.TrimStart('/');
                    }
                }
            }

            return customExitRule;
        }

        private static InformationPointRule fillInformationPointRule(XmlNode informationPointConfigurationNode, APMApplicationConfiguration applicationConfiguration)
        {
            InformationPointRule informationPointRule = new InformationPointRule();

            informationPointRule.Controller = applicationConfiguration.Controller;
            informationPointRule.ControllerLink = applicationConfiguration.ControllerLink;
            informationPointRule.ApplicationName = applicationConfiguration.ApplicationName;
            informationPointRule.ApplicationID = applicationConfiguration.ApplicationID;
            informationPointRule.ApplicationLink = applicationConfiguration.ApplicationLink;

            informationPointRule.AgentType = getStringValueFromXmlNode(informationPointConfigurationNode.SelectSingleNode("agent-type"));

            informationPointRule.RuleName = getStringValueFromXmlNode(informationPointConfigurationNode.SelectSingleNode("name"));
            informationPointRule.MatchClass = getStringValueFromXmlNode(informationPointConfigurationNode.SelectSingleNode("pojo-method-definition/class-name"));
            informationPointRule.MatchMethod = getStringValueFromXmlNode(informationPointConfigurationNode.SelectSingleNode("pojo-method-definition/method-name"));
            informationPointRule.MatchType = getStringValueFromXmlNode(informationPointConfigurationNode.SelectSingleNode("pojo-method-definition/match-type"));
            informationPointRule.MatchParameterTypes = getStringValueFromXmlNode(informationPointConfigurationNode.SelectSingleNode("pojo-method-definition/method-parameter-types"));
            informationPointRule.MatchCondition = makeXMLFormattedAndIndented(informationPointConfigurationNode.SelectSingleNode("pojo-method-definition/match-condition"));

            informationPointRule.InfoPointsConfig = makeXMLFormattedAndIndented(String.Format("<info-point-metric-definitions>{0}</info-point-metric-definitions>", makeXMLFormattedAndIndented(informationPointConfigurationNode.SelectNodes("info-point-metric-definition"))));

            informationPointRule.RuleRawValue = makeXMLFormattedAndIndented(informationPointConfigurationNode);

            return informationPointRule;
        }

        private static AgentConfigurationProperty fillAgentConfigurationProperty(XmlNode agentConfigurationNode, XmlNode agentPropertyDefinitionConfigurationNode, XmlNode agentPropertyValueConfigurationNode, APMApplicationConfiguration applicationConfiguration, XmlNode applicationComponentNode)
        {
            AgentConfigurationProperty agentConfigurationProperty = new AgentConfigurationProperty();

            agentConfigurationProperty.Controller = applicationConfiguration.Controller;
            agentConfigurationProperty.ControllerLink = applicationConfiguration.ControllerLink;
            agentConfigurationProperty.ApplicationName = applicationConfiguration.ApplicationName;
            agentConfigurationProperty.ApplicationID = applicationConfiguration.ApplicationID;
            agentConfigurationProperty.ApplicationLink = applicationConfiguration.ApplicationLink;

            agentConfigurationProperty.AgentType = getStringValueFromXmlNode(agentConfigurationNode.SelectSingleNode("agent-type"));

            agentConfigurationProperty.PropertyName = getStringValueFromXmlNode(agentPropertyDefinitionConfigurationNode.SelectSingleNode("name"));
            agentConfigurationProperty.PropertyType = getStringValueFromXmlNode(agentPropertyDefinitionConfigurationNode.SelectSingleNode("type"));
            agentConfigurationProperty.Description = getStringValueFromXmlNode(agentPropertyDefinitionConfigurationNode.SelectSingleNode("description")).Trim();
            agentConfigurationProperty.IsRequired = getBoolValueFromXmlNode(agentPropertyDefinitionConfigurationNode.SelectSingleNode("required"));

            switch (agentConfigurationProperty.PropertyType)
            {
                case "STRING":
                    agentConfigurationProperty.StringValue = getStringValueFromXmlNode(agentPropertyValueConfigurationNode.SelectSingleNode("string-value"));
                    agentConfigurationProperty.StringDefaultValue = getStringValueFromXmlNode(agentPropertyDefinitionConfigurationNode.SelectSingleNode("default-string-value"));
                    agentConfigurationProperty.StringMaxLength = getIntegerValueFromXmlNode(agentPropertyDefinitionConfigurationNode.SelectSingleNode("string-max-length"));
                    agentConfigurationProperty.StringAllowedValues = getStringValueFromXmlNode(agentPropertyDefinitionConfigurationNode.SelectSingleNode("allowed-string-values"));

                    agentConfigurationProperty.IsDefault = (agentConfigurationProperty.StringDefaultValue == agentConfigurationProperty.StringDefaultValue);
                    break;

                case "BOOLEAN":
                    agentConfigurationProperty.BooleanValue = getBoolValueFromXmlNode(agentPropertyValueConfigurationNode.SelectSingleNode("string-value"));
                    agentConfigurationProperty.BooleanDefaultValue = getBoolValueFromXmlNode(agentPropertyDefinitionConfigurationNode.SelectSingleNode("default-string-value"));

                    agentConfigurationProperty.IsDefault = (agentConfigurationProperty.BooleanValue == agentConfigurationProperty.BooleanDefaultValue);
                    break;

                case "INTEGER":
                    agentConfigurationProperty.IntegerValue = getIntegerValueFromXmlNode(agentPropertyValueConfigurationNode.SelectSingleNode("string-value"));
                    agentConfigurationProperty.IntegerDefaultValue = getIntegerValueFromXmlNode(agentPropertyDefinitionConfigurationNode.SelectSingleNode("default-string-value"));
                    agentConfigurationProperty.IntegerMinValue = getIntegerValueFromXmlNode(agentPropertyDefinitionConfigurationNode.SelectSingleNode("lower-numeric-bound"));
                    agentConfigurationProperty.IntegerMaxValue = getIntegerValueFromXmlNode(agentPropertyDefinitionConfigurationNode.SelectSingleNode("upper-numeric-bound"));

                    agentConfigurationProperty.IsDefault = (agentConfigurationProperty.IntegerValue == agentConfigurationProperty.IntegerDefaultValue);
                    break;

                default:
                    agentConfigurationProperty.StringValue = getStringValueFromXmlNode(agentPropertyValueConfigurationNode.SelectSingleNode("string-value"));
                    break;
            }

            if (applicationComponentNode != null)
            {
                agentConfigurationProperty.TierName = getStringValueFromXmlNode(applicationComponentNode.SelectSingleNode("name"));
            }

            agentConfigurationProperty.IsBuiltIn = BUILTIN_AGENT_PROPERTIES.Contains(agentConfigurationProperty.PropertyName);

            return agentConfigurationProperty;
        }

        private static MethodInvocationDataCollector fillMethodInvocationDataCollector(XmlNode methodInvocationDataCollectorConfigurationNode, XmlNode dataGathererConfigurationNode, APMApplicationConfiguration applicationConfiguration, List<BusinessTransactionConfiguration> entityBusinessTransactionConfigurationsList)
        {
            MethodInvocationDataCollector methodInvocationDataCollector = new MethodInvocationDataCollector();

            methodInvocationDataCollector.Controller = applicationConfiguration.Controller;
            methodInvocationDataCollector.ControllerLink = applicationConfiguration.ControllerLink;
            methodInvocationDataCollector.ApplicationName = applicationConfiguration.ApplicationName;
            methodInvocationDataCollector.ApplicationID = applicationConfiguration.ApplicationID;
            methodInvocationDataCollector.ApplicationLink = applicationConfiguration.ApplicationLink;

            methodInvocationDataCollector.CollectorName = getStringValueFromXmlNode(methodInvocationDataCollectorConfigurationNode.SelectSingleNode("name"));

            methodInvocationDataCollector.IsAPM = Convert.ToBoolean(methodInvocationDataCollectorConfigurationNode.Attributes["enabled-for-apm"].Value);
            methodInvocationDataCollector.IsAnalytics = Convert.ToBoolean(methodInvocationDataCollectorConfigurationNode.Attributes["enabled-for-analytics"].Value);
            methodInvocationDataCollector.IsAssignedToNewBTs = Convert.ToBoolean(methodInvocationDataCollectorConfigurationNode.Attributes["attach-to-new-bts"].Value);

            methodInvocationDataCollector.MatchClass = getStringValueFromXmlNode(methodInvocationDataCollectorConfigurationNode.SelectSingleNode("pojo-method-definition/class-name"));
            methodInvocationDataCollector.MatchMethod = getStringValueFromXmlNode(methodInvocationDataCollectorConfigurationNode.SelectSingleNode("pojo-method-definition/method-name"));
            methodInvocationDataCollector.MatchType = getStringValueFromXmlNode(methodInvocationDataCollectorConfigurationNode.SelectSingleNode("pojo-method-definition/match-type"));
            methodInvocationDataCollector.MatchParameterTypes = getStringValueFromXmlNode(methodInvocationDataCollectorConfigurationNode.SelectSingleNode("pojo-method-definition/method-parameter-types"));

            methodInvocationDataCollector.DataGathererName = getStringValueFromXmlNode(dataGathererConfigurationNode.SelectSingleNode("name"));
            methodInvocationDataCollector.DataGathererType = getStringValueFromXmlNode(dataGathererConfigurationNode.SelectSingleNode("gatherer-type"));
            methodInvocationDataCollector.DataGathererPosition = getIntegerValueFromXmlNode(dataGathererConfigurationNode.SelectSingleNode("position"));
            methodInvocationDataCollector.DataGathererTransform = getStringValueFromXmlNode(dataGathererConfigurationNode.SelectSingleNode("transformer-type"));
            methodInvocationDataCollector.DataGathererGetter = getStringValueFromXmlNode(dataGathererConfigurationNode.SelectSingleNode("transformer-value"));

            methodInvocationDataCollector.IsAssignedToBTs = false;

            methodInvocationDataCollector.RuleRawValue = makeXMLFormattedAndIndented(methodInvocationDataCollectorConfigurationNode);

            if (entityBusinessTransactionConfigurationsList != null)
            {
                List<BusinessTransactionConfiguration> entityBusinessTransactionConfigurationsForThisDCList = entityBusinessTransactionConfigurationsList.Where(b => b.AssignedMIDCs.Contains(String.Format("{0};", methodInvocationDataCollector.CollectorName)) == true).ToList();

                if (entityBusinessTransactionConfigurationsForThisDCList.Count > 0)
                {
                    methodInvocationDataCollector.IsAssignedToBTs = true;
                    methodInvocationDataCollector.NumAssignedBTs = entityBusinessTransactionConfigurationsForThisDCList.Count;

                    StringBuilder sb = new StringBuilder(32 * entityBusinessTransactionConfigurationsForThisDCList.Count);
                    foreach (BusinessTransactionConfiguration bt in entityBusinessTransactionConfigurationsForThisDCList)
                    {
                        sb.AppendFormat("{0}/{1};\n", bt.TierName, bt.BTName);
                    }
                    sb.Remove(sb.Length - 1, 1);
                    methodInvocationDataCollector.AssignedBTs = sb.ToString();
                }
            }

            return methodInvocationDataCollector;
        }

        private static HTTPDataCollector fillHTTPDataCollector(XmlNode httpDataCollectorConfigurationNode, XmlNode dataGathererConfigurationNode, APMApplicationConfiguration applicationConfiguration, List<BusinessTransactionConfiguration> entityBusinessTransactionConfigurationsList)
        {
            HTTPDataCollector httpDataCollector = new HTTPDataCollector();

            httpDataCollector.Controller = applicationConfiguration.Controller;
            httpDataCollector.ControllerLink = applicationConfiguration.ControllerLink;
            httpDataCollector.ApplicationName = applicationConfiguration.ApplicationName;
            httpDataCollector.ApplicationID = applicationConfiguration.ApplicationID;
            httpDataCollector.ApplicationLink = applicationConfiguration.ApplicationLink;

            httpDataCollector.CollectorName = getStringValueFromXmlNode(httpDataCollectorConfigurationNode.SelectSingleNode("name"));

            httpDataCollector.IsAPM = Convert.ToBoolean(httpDataCollectorConfigurationNode.Attributes["enabled-for-apm"].Value);
            httpDataCollector.IsAnalytics = Convert.ToBoolean(httpDataCollectorConfigurationNode.Attributes["enabled-for-analytics"].Value);
            httpDataCollector.IsAssignedToNewBTs = Convert.ToBoolean(httpDataCollectorConfigurationNode.Attributes["attach-to-new-bts"].Value);

            httpDataCollector.IsURLEnabled = getBoolValueFromXmlNode(httpDataCollectorConfigurationNode.SelectSingleNode("gather-url"));
            httpDataCollector.IsSessionIDEnabled = getBoolValueFromXmlNode(httpDataCollectorConfigurationNode.SelectSingleNode("gather-session-id"));
            httpDataCollector.IsUserPrincipalEnabled = getBoolValueFromXmlNode(httpDataCollectorConfigurationNode.SelectSingleNode("gather-user-principal"));

            if (dataGathererConfigurationNode != null)
            {
                httpDataCollector.DataGathererName = getStringValueFromXmlNode(dataGathererConfigurationNode.SelectSingleNode("display-name"));
                httpDataCollector.DataGathererValue = getStringValueFromXmlNode(dataGathererConfigurationNode.SelectSingleNode("name"));
            }

            StringBuilder sb = new StringBuilder(200);
            foreach (XmlNode headerNode in httpDataCollectorConfigurationNode.SelectNodes("headers"))
            {
                sb.AppendFormat("{0};", getStringValueFromXmlNode(headerNode));
            }
            httpDataCollector.HeadersList = sb.ToString();

            httpDataCollector.IsAssignedToBTs = false;

            httpDataCollector.RuleRawValue = makeXMLFormattedAndIndented(httpDataCollectorConfigurationNode);

            if (entityBusinessTransactionConfigurationsList != null)
            {
                List<BusinessTransactionConfiguration> entityBusinessTransactionConfigurationsForThisDCList = entityBusinessTransactionConfigurationsList.Where(b => b.AssignedMIDCs.Contains(String.Format("{0};", httpDataCollector.CollectorName)) == true).ToList();

                if (entityBusinessTransactionConfigurationsForThisDCList.Count > 0)
                {
                    httpDataCollector.IsAssignedToBTs = true;
                    httpDataCollector.NumAssignedBTs = entityBusinessTransactionConfigurationsForThisDCList.Count;

                    sb = new StringBuilder(32 * entityBusinessTransactionConfigurationsForThisDCList.Count);
                    foreach (BusinessTransactionConfiguration bt in entityBusinessTransactionConfigurationsForThisDCList)
                    {
                        sb.AppendFormat("{0}/{1};\n", bt.TierName, bt.BTName);
                    }
                    sb.Remove(sb.Length - 1, 1);
                    httpDataCollector.AssignedBTs = sb.ToString();
                }
            }

            return httpDataCollector;
        }

        private static TierConfiguration fillEntityTierConfiguration(XmlNode applicationComponentNode, APMApplicationConfiguration applicationConfiguration, List<APMTier> tiersList, List<BusinessTransactionConfiguration> entityBusinessTransactionConfigurationsList)
        {
            TierConfiguration entityTierConfiguration = new TierConfiguration();

            entityTierConfiguration.Controller = applicationConfiguration.Controller;
            entityTierConfiguration.ControllerLink = applicationConfiguration.ControllerLink;
            entityTierConfiguration.ApplicationName = applicationConfiguration.ApplicationName;
            entityTierConfiguration.ApplicationID = applicationConfiguration.ApplicationID;
            entityTierConfiguration.ApplicationLink = applicationConfiguration.ApplicationLink;

            entityTierConfiguration.TierName = getStringValueFromXmlNode(applicationComponentNode.SelectSingleNode("name"));
            entityTierConfiguration.TierDescription = getStringValueFromXmlNode(applicationComponentNode.SelectSingleNode("description"));
            entityTierConfiguration.TierType = getStringValueFromXmlNode(applicationComponentNode.SelectSingleNode("component-type"));
            if (tiersList != null)
            {
                APMTier tier = tiersList.Where(t => t.TierName == entityTierConfiguration.TierName).FirstOrDefault();
                if (tier != null)
                {
                    entityTierConfiguration.TierID = tier.TierID;
                }
            }

            entityTierConfiguration.IsDynamicScalingEnabled = getBoolValueFromXmlNode(applicationComponentNode.SelectSingleNode("dynamic-scaling-enabled"));

            entityTierConfiguration.MemoryConfig = makeXMLFormattedAndIndented(applicationComponentNode.SelectSingleNode("memory-configuration"));
            entityTierConfiguration.CacheConfig = makeXMLFormattedAndIndented(applicationComponentNode.SelectSingleNode("cache-configuration"));
            entityTierConfiguration.CustomCacheConfig = makeXMLFormattedAndIndented(applicationComponentNode.SelectSingleNode("custom-cache-configurations"));

            if (entityBusinessTransactionConfigurationsList != null)
            {
                List<BusinessTransactionConfiguration> businessTransactionsList = entityBusinessTransactionConfigurationsList.Where(b => b.TierID == entityTierConfiguration.TierID).ToList();
                entityTierConfiguration.NumBTs = businessTransactionsList.Count;

                var businessTransactionsListGroup = businessTransactionsList.GroupBy(b => b.BTType);
                entityTierConfiguration.NumBTTypes = businessTransactionsListGroup.Count();
            }

            return entityTierConfiguration;
        }

        private static BusinessTransactionConfiguration fillEntityBusinessTransactionConfiguration(XmlNode applicationComponentNode, XmlNode businessTransactionConfigurationtNode, APMApplicationConfiguration applicationConfiguration, List<APMTier> tiersList, List<APMBusinessTransaction> businessTransactionsList)
        {
            BusinessTransactionConfiguration entityBusinessTransactionConfiguration = new BusinessTransactionConfiguration();

            entityBusinessTransactionConfiguration.Controller = applicationConfiguration.Controller;
            entityBusinessTransactionConfiguration.ControllerLink = applicationConfiguration.ControllerLink;
            entityBusinessTransactionConfiguration.ApplicationName = applicationConfiguration.ApplicationName;
            entityBusinessTransactionConfiguration.ApplicationID = applicationConfiguration.ApplicationID;
            entityBusinessTransactionConfiguration.ApplicationLink = applicationConfiguration.ApplicationLink;

            entityBusinessTransactionConfiguration.TierName = getStringValueFromXmlNode(applicationComponentNode.SelectSingleNode("name"));
            if (tiersList != null)
            {
                APMTier tier = tiersList.Where(t => t.TierName == entityBusinessTransactionConfiguration.TierName).FirstOrDefault();
                if (tier != null)
                {
                    entityBusinessTransactionConfiguration.TierID = tier.TierID;
                }
            }

            entityBusinessTransactionConfiguration.BTName = getStringValueFromXmlNode(businessTransactionConfigurationtNode.SelectSingleNode("name"));
            entityBusinessTransactionConfiguration.BTType = businessTransactionConfigurationtNode.Attributes["transaction-entry-point-type"].Value;
            if (businessTransactionsList != null)
            {
                APMBusinessTransaction businessTransaction = businessTransactionsList.Where(b => b.BTName == entityBusinessTransactionConfiguration.BTName && b.TierName == entityBusinessTransactionConfiguration.TierName).FirstOrDefault();
                if (businessTransaction != null)
                {
                    entityBusinessTransactionConfiguration.BTID = businessTransaction.BTID;
                }
            }

            entityBusinessTransactionConfiguration.IsExcluded = Convert.ToBoolean(businessTransactionConfigurationtNode.Attributes["excluded"].Value);
            entityBusinessTransactionConfiguration.IsBackground = getBoolValueFromXmlNode(businessTransactionConfigurationtNode.SelectSingleNode("background"));

            entityBusinessTransactionConfiguration.IsEUMEnabled = getBoolValueFromXmlNode(businessTransactionConfigurationtNode.SelectSingleNode("enabled-for-eum"));
            entityBusinessTransactionConfiguration.IsEUMPossible = getStringValueFromXmlNode(businessTransactionConfigurationtNode.SelectSingleNode("eum-auto-enable-possible"));
            entityBusinessTransactionConfiguration.IsAnalyticsEnabled = getBoolValueFromXmlNode(businessTransactionConfigurationtNode.SelectSingleNode("analytics-enabled"));

            entityBusinessTransactionConfiguration.BTSLAConfig = makeXMLFormattedAndIndented(businessTransactionConfigurationtNode.SelectSingleNode("sla"));
            entityBusinessTransactionConfiguration.BTSnapshotCollectionConfig = makeXMLFormattedAndIndented(businessTransactionConfigurationtNode.SelectSingleNode("business-transaction-config/snapshot-collection-policy"));
            entityBusinessTransactionConfiguration.BTRequestThresholdConfig = makeXMLFormattedAndIndented(businessTransactionConfigurationtNode.SelectSingleNode("business-transaction-config/bt-request-thresholds"));
            entityBusinessTransactionConfiguration.BTBackgroundSnapshotCollectionConfig = makeXMLFormattedAndIndented(businessTransactionConfigurationtNode.SelectSingleNode("background-business-transaction-config/snapshot-collection-policy"));
            entityBusinessTransactionConfiguration.BTBackgroundRequestThresholdConfig = makeXMLFormattedAndIndented(businessTransactionConfigurationtNode.SelectSingleNode("background-business-transaction-config/bt-request-thresholds"));

            entityBusinessTransactionConfiguration.NumAssignedMIDCs = businessTransactionConfigurationtNode.SelectNodes("data-gatherer-config").Count;
            entityBusinessTransactionConfiguration.AssignedMIDCs = String.Empty;
            if (entityBusinessTransactionConfiguration.NumAssignedMIDCs > 0)
            {
                StringBuilder sb = new StringBuilder(32 * entityBusinessTransactionConfiguration.NumAssignedMIDCs);
                foreach (XmlNode dataGathererXmlNode in businessTransactionConfigurationtNode.SelectNodes("data-gatherer-config"))
                {
                    sb.AppendFormat("{0};\n", dataGathererXmlNode.InnerText);
                }
                sb.Remove(sb.Length - 1, 1);
                entityBusinessTransactionConfiguration.AssignedMIDCs = sb.ToString();
            }

            return entityBusinessTransactionConfiguration;
        }

        private static AgentCallGraphSetting fillAgentCallGraphSetting(XmlNode agentCallGraphSettingConfigurationNode, APMApplicationConfiguration applicationConfiguration)
        {
            AgentCallGraphSetting agentCallGraphSetting = new AgentCallGraphSetting();

            agentCallGraphSetting.Controller = applicationConfiguration.Controller;
            agentCallGraphSetting.ControllerLink = applicationConfiguration.ControllerLink;
            agentCallGraphSetting.ApplicationName = applicationConfiguration.ApplicationName;
            agentCallGraphSetting.ApplicationID = applicationConfiguration.ApplicationID;
            agentCallGraphSetting.ApplicationLink = applicationConfiguration.ApplicationLink;

            agentCallGraphSetting.AgentType = agentCallGraphSettingConfigurationNode.Attributes["agent-type"].Value;

            agentCallGraphSetting.SamplingRate = getIntegerValueFromXmlNode(agentCallGraphSettingConfigurationNode.SelectSingleNode("sampling-rate"));
            agentCallGraphSetting.IncludePackages = getStringValueFromXmlNode(agentCallGraphSettingConfigurationNode.SelectSingleNode("include-packages"));
            agentCallGraphSetting.NumIncludePackages = agentCallGraphSetting.IncludePackages.Split('|').Count();
            agentCallGraphSetting.IncludePackages = agentCallGraphSetting.IncludePackages.Replace("|", ";\n");
            agentCallGraphSetting.ExcludePackages = getStringValueFromXmlNode(agentCallGraphSettingConfigurationNode.SelectSingleNode("exclude-packages"));
            agentCallGraphSetting.NumExcludePackages = agentCallGraphSetting.ExcludePackages.Split('|').Count();
            agentCallGraphSetting.ExcludePackages = agentCallGraphSetting.ExcludePackages.Replace("|", ";\n");
            agentCallGraphSetting.MinSQLDuration = getIntegerValueFromXmlNode(agentCallGraphSettingConfigurationNode.SelectSingleNode("min-duration-for-db-calls"));
            agentCallGraphSetting.IsRawSQLEnabled = getBoolValueFromXmlNode(agentCallGraphSettingConfigurationNode.SelectSingleNode("raw-sql"));
            agentCallGraphSetting.IsHotSpotEnabled = getBoolValueFromXmlNode(agentCallGraphSettingConfigurationNode.SelectSingleNode("hotspots-enabled"));

            return agentCallGraphSetting;
        }

        private static ErrorDetectionRule fillErrorDetectionRule(APMApplicationConfiguration applicationConfiguration, string agentType, string ruleName, string ruleValue)
        {
            ErrorDetectionRule errorDetectionRule = new ErrorDetectionRule();

            errorDetectionRule.Controller = applicationConfiguration.Controller;
            errorDetectionRule.ControllerLink = applicationConfiguration.ControllerLink;
            errorDetectionRule.ApplicationName = applicationConfiguration.ApplicationName;
            errorDetectionRule.ApplicationID = applicationConfiguration.ApplicationID;
            errorDetectionRule.ApplicationLink = applicationConfiguration.ApplicationLink;

            errorDetectionRule.AgentType = agentType;
            errorDetectionRule.RuleName = ruleName;
            errorDetectionRule.RuleValue = ruleValue;

            return errorDetectionRule;
        }

        private static ErrorDetectionIgnoreMessage fillErrorDetectionIgnoreException(APMApplicationConfiguration applicationConfiguration, string agentType, string exceptionClass, JObject messageMatchObject)
        {
            ErrorDetectionIgnoreMessage errorDetectionIgnoreException = new ErrorDetectionIgnoreMessage();

            errorDetectionIgnoreException.Controller = applicationConfiguration.Controller;
            errorDetectionIgnoreException.ControllerLink = applicationConfiguration.ControllerLink;
            errorDetectionIgnoreException.ApplicationName = applicationConfiguration.ApplicationName;
            errorDetectionIgnoreException.ApplicationID = applicationConfiguration.ApplicationID;
            errorDetectionIgnoreException.ApplicationLink = applicationConfiguration.ApplicationLink;

            errorDetectionIgnoreException.AgentType = agentType;
            errorDetectionIgnoreException.ExceptionClass = exceptionClass;
            errorDetectionIgnoreException.MatchType = getStringValueFromJToken(messageMatchObject, "matchType");
            if (getBoolValueFromJToken(messageMatchObject, "inverse") == true)
            {
                errorDetectionIgnoreException.MatchType = String.Format("NOT {0}", errorDetectionIgnoreException.MatchType);
            }
            switch (errorDetectionIgnoreException.MatchType)
            {
                case "INLIST":
                    errorDetectionIgnoreException.MessagePattern = getStringValueOfObjectFromJToken(messageMatchObject, "inList", true);

                    break;

                default:
                    errorDetectionIgnoreException.MessagePattern = getStringValueFromJToken(messageMatchObject, "matchPattern");

                    break;
            }

            return errorDetectionIgnoreException;
        }

        private static ErrorDetectionIgnoreLogger fillErrorDetectionIgnoreLogger(APMApplicationConfiguration applicationConfiguration, string agentType, string loggerName)
        {
            ErrorDetectionIgnoreLogger errorDetectionIgnoreLogger = new ErrorDetectionIgnoreLogger();

            errorDetectionIgnoreLogger.Controller = applicationConfiguration.Controller;
            errorDetectionIgnoreLogger.ControllerLink = applicationConfiguration.ControllerLink;
            errorDetectionIgnoreLogger.ApplicationName = applicationConfiguration.ApplicationName;
            errorDetectionIgnoreLogger.ApplicationID = applicationConfiguration.ApplicationID;
            errorDetectionIgnoreLogger.ApplicationLink = applicationConfiguration.ApplicationLink;

            errorDetectionIgnoreLogger.AgentType = agentType;
            errorDetectionIgnoreLogger.LoggerName = loggerName;

            return errorDetectionIgnoreLogger;
        }

        private static ErrorDetectionLogger fillErrorDetectionLogger(APMApplicationConfiguration applicationConfiguration, string agentType, JObject loggerObject)
        {
            ErrorDetectionLogger errorDetectionLogger = new ErrorDetectionLogger();

            errorDetectionLogger.Controller = applicationConfiguration.Controller;
            errorDetectionLogger.ControllerLink = applicationConfiguration.ControllerLink;
            errorDetectionLogger.ApplicationName = applicationConfiguration.ApplicationName;
            errorDetectionLogger.ApplicationID = applicationConfiguration.ApplicationID;
            errorDetectionLogger.ApplicationLink = applicationConfiguration.ApplicationLink;

            errorDetectionLogger.AgentType = agentType;
            errorDetectionLogger.LoggerName = getStringValueFromJToken(loggerObject, "name");
            errorDetectionLogger.IsEnabled = !getBoolValueFromJToken(loggerObject, "disable");

            errorDetectionLogger.ExceptionParam = getIntValueFromJToken(loggerObject, "methodParamExceptionIndex");
            errorDetectionLogger.MessageParam = getIntValueFromJToken(loggerObject, "methodParamMessageIndex");

            if (isTokenPropertyNull(loggerObject, "definition") == false)
            {
                errorDetectionLogger.MatchClass = getStringValueFromJToken(loggerObject["definition"], "className");
                errorDetectionLogger.MatchMethod = getStringValueFromJToken(loggerObject["definition"], "methodName");
                errorDetectionLogger.MatchType = getStringValueFromJToken(loggerObject["definition"], "matchType");
                errorDetectionLogger.MatchParameterTypes = getStringValueOfObjectFromJToken(loggerObject["definition"], "methodParameterTypes", true);
            }
            
            return errorDetectionLogger;
        }

        private static ErrorDetectionHTTPCode fillErrorDetectionHTTPCode(APMApplicationConfiguration applicationConfiguration, string agentType, JObject loggerObject)
        {
            ErrorDetectionHTTPCode errorDetectionHTTPCode = new ErrorDetectionHTTPCode();

            errorDetectionHTTPCode.Controller = applicationConfiguration.Controller;
            errorDetectionHTTPCode.ControllerLink = applicationConfiguration.ControllerLink;
            errorDetectionHTTPCode.ApplicationName = applicationConfiguration.ApplicationName;
            errorDetectionHTTPCode.ApplicationID = applicationConfiguration.ApplicationID;
            errorDetectionHTTPCode.ApplicationLink = applicationConfiguration.ApplicationLink;

            errorDetectionHTTPCode.AgentType = agentType;
            errorDetectionHTTPCode.RangeName = getStringValueFromJToken(loggerObject, "name");
            errorDetectionHTTPCode.IsEnabled = !getBoolValueFromJToken(loggerObject, "disable");
            errorDetectionHTTPCode.CaptureURL = !getBoolValueFromJToken(loggerObject, "captureURL");

            errorDetectionHTTPCode.CodeFrom = getIntValueFromJToken(loggerObject, "lowerBound");
            errorDetectionHTTPCode.CodeTo = getIntValueFromJToken(loggerObject, "upperBound");

            return errorDetectionHTTPCode;
        }

        private static ErrorDetectionRedirectPage fillErrorDetectionRedirectPage(APMApplicationConfiguration applicationConfiguration, string agentType, JObject errorRedirectPageObject)
        {
            ErrorDetectionRedirectPage errorDetectionRedirectPage = new ErrorDetectionRedirectPage();

            errorDetectionRedirectPage.Controller = applicationConfiguration.Controller;
            errorDetectionRedirectPage.ControllerLink = applicationConfiguration.ControllerLink;
            errorDetectionRedirectPage.ApplicationName = applicationConfiguration.ApplicationName;
            errorDetectionRedirectPage.ApplicationID = applicationConfiguration.ApplicationID;
            errorDetectionRedirectPage.ApplicationLink = applicationConfiguration.ApplicationLink;

            errorDetectionRedirectPage.AgentType = agentType;
            errorDetectionRedirectPage.PageName = getStringValueFromJToken(errorRedirectPageObject, "name");
            errorDetectionRedirectPage.IsEnabled = !getBoolValueFromJToken(errorRedirectPageObject, "disable");

            if (isTokenPropertyNull(errorRedirectPageObject, "match") == false)
            {
                errorDetectionRedirectPage.MatchType = getStringValueFromJToken(errorRedirectPageObject["match"], "matchType");
                errorDetectionRedirectPage.MatchPattern = getStringValueFromJToken(errorRedirectPageObject["match"], "matchPattern");
                if (getBoolValueFromJToken(errorRedirectPageObject["match"], "inverse") == true)
                {
                    errorDetectionRedirectPage.MatchType = String.Format("NOT {0}", errorDetectionRedirectPage.MatchType);
                }

                switch (errorDetectionRedirectPage.MatchType)
                {
                    case "INLIST":
                        errorDetectionRedirectPage.MatchPattern = getStringValueOfObjectFromJToken(errorRedirectPageObject["match"], "inList", true);

                        break;

                    default:
                        errorDetectionRedirectPage.MatchPattern = getStringValueFromJToken(errorRedirectPageObject["match"], "matchPattern");

                        break;
                }
            }

            return errorDetectionRedirectPage;
        }

        private static void fillMatchRuleDetails(BusinessTransactionEntryRule businessTransactionEntryRule, XmlNode matchRule)
        {
            // Enabled seems to be set inside of the match-rule, not couple of levels up
            businessTransactionEntryRule.IsEnabled = Convert.ToBoolean(matchRule.SelectSingleNode("enabled").InnerText);
            businessTransactionEntryRule.Priority = Convert.ToInt32(matchRule.SelectSingleNode("priority").InnerText);
            businessTransactionEntryRule.IsExcluded = Convert.ToBoolean(matchRule.SelectSingleNode("excluded").InnerText);

            switch (businessTransactionEntryRule.EntryPointType)
            {
                case "ASP_DOTNET":
                    businessTransactionEntryRule.MatchURI = makeXMLFormattedAndIndented(matchRule.SelectSingleNode("uri"));
                    //businessTransactionEntryRule.Parameters = getNameValueDetailsFromParametersCollection(matchRule.SelectSingleNode("parameters"));
                    businessTransactionEntryRule.Parameters = makeXMLFormattedAndIndented(matchRule.SelectSingleNode("parameters"));
                    businessTransactionEntryRule.SplitConfig = makeXMLFormattedAndIndented(matchRule.SelectSingleNode("split-config"));
                    businessTransactionEntryRule.MatchClass = makeXMLFormattedAndIndented(matchRule.SelectSingleNode("class-name"));

                    break;

                case "NODEJS_WEB":
                    businessTransactionEntryRule.MatchURI = makeXMLFormattedAndIndented(matchRule.SelectSingleNode("uri"));
                    //businessTransactionEntryRule.Parameters = getNameValueDetailsFromParametersCollection(matchRule.SelectSingleNode("parameters"));
                    businessTransactionEntryRule.Parameters = makeXMLFormattedAndIndented(matchRule.SelectSingleNode("parameters"));
                    businessTransactionEntryRule.SplitConfig = makeXMLFormattedAndIndented(matchRule.SelectSingleNode("split-config"));
                    businessTransactionEntryRule.MatchClass = makeXMLFormattedAndIndented(matchRule.SelectSingleNode("http-method"));

                    break;

                case "PYTHON_WEB":
                    businessTransactionEntryRule.MatchURI = makeXMLFormattedAndIndented(matchRule.SelectSingleNode("uri"));
                    //businessTransactionEntryRule.Parameters = getNameValueDetailsFromParametersCollection(matchRule.SelectSingleNode("parameters"));
                    businessTransactionEntryRule.Parameters = makeXMLFormattedAndIndented(matchRule.SelectSingleNode("parameters"));
                    businessTransactionEntryRule.SplitConfig = makeXMLFormattedAndIndented(matchRule.SelectSingleNode("split-config"));
                    businessTransactionEntryRule.MatchClass = makeXMLFormattedAndIndented(matchRule.SelectSingleNode("http-method"));

                    break;

                case "POCO":
                    // Background is really only set for POCOs
                    businessTransactionEntryRule.IsBackground = Convert.ToBoolean(matchRule.SelectSingleNode("background").InnerText);
                    businessTransactionEntryRule.MatchClass = makeXMLFormattedAndIndented(matchRule.SelectSingleNode("match-class"));
                    businessTransactionEntryRule.MatchMethod = makeXMLFormattedAndIndented(matchRule.SelectSingleNode("match-method"));
                    businessTransactionEntryRule.SplitConfig = makeXMLFormattedAndIndented(matchRule.SelectSingleNode("split-config"));

                    break;

                case "POJO":
                    // Background is really only set for POJOs
                    businessTransactionEntryRule.IsBackground = Convert.ToBoolean(matchRule.SelectSingleNode("background").InnerText);
                    businessTransactionEntryRule.MatchClass = makeXMLFormattedAndIndented(matchRule.SelectSingleNode("match-class"));
                    businessTransactionEntryRule.MatchMethod = makeXMLFormattedAndIndented(matchRule.SelectSingleNode("match-method"));
                    businessTransactionEntryRule.SplitConfig = makeXMLFormattedAndIndented(matchRule.SelectSingleNode("split-config"));

                    break;

                case "SERVLET":
                    businessTransactionEntryRule.MatchURI = makeXMLFormattedAndIndented(matchRule.SelectSingleNode("uri"));
                    //businessTransactionEntryRule.Parameters = getNameValueDetailsFromParametersCollection(matchRule.SelectSingleNode("parameters"));
                    businessTransactionEntryRule.Parameters = makeXMLFormattedAndIndented(matchRule.SelectSingleNode("parameters"));
                    businessTransactionEntryRule.SplitConfig = makeXMLFormattedAndIndented(matchRule.SelectSingleNode("split-config"));
                    businessTransactionEntryRule.MatchClass = makeXMLFormattedAndIndented(matchRule.SelectSingleNode("class-name"));

                    break;

                case "STRUTS_ACTION":
                    businessTransactionEntryRule.MatchClass = makeXMLFormattedAndIndented(matchRule.SelectSingleNode("action-class-name"));
                    businessTransactionEntryRule.MatchMethod = makeXMLFormattedAndIndented(matchRule.SelectSingleNode("action-method-name"));
                    // There is also Struts Action Name in the UI, but I don't know how it shows up

                    break;

                case "WCF":
                    businessTransactionEntryRule.MatchClass = makeXMLFormattedAndIndented(matchRule.SelectSingleNode("web-service-name"));
                    businessTransactionEntryRule.MatchMethod = makeXMLFormattedAndIndented(matchRule.SelectSingleNode("operation-name"));

                    break;

                case "WEB":
                    businessTransactionEntryRule.MatchURI = makeXMLFormattedAndIndented(matchRule.SelectSingleNode("uri"));
                    //businessTransactionEntryRule.Parameters = getNameValueDetailsFromParametersCollection(matchRule.SelectSingleNode("parameters"));
                    businessTransactionEntryRule.Parameters = makeXMLFormattedAndIndented(matchRule.SelectSingleNode("parameters"));
                    businessTransactionEntryRule.SplitConfig = makeXMLFormattedAndIndented(matchRule.SelectSingleNode("split-config"));

                    break;

                default:
                    break;
            }
        }
    }
}
