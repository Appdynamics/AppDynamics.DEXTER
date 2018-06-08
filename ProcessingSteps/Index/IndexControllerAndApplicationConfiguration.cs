using AppDynamics.Dexter.DataObjects;
using AppDynamics.Dexter.ReportObjects;
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
    public class IndexControllerAndApplicationConfiguration : JobStepIndexBase
    {
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
                if (this.ShouldExecute(jobConfiguration) == false)
                {
                    return true;
                }

                // Check to see if the reference application is the template
                bool compareAgainstTemplateConfiguration = false;
                if (jobConfiguration.Input.ConfigurationComparisonReferenceCriteria.Controller == BLANK_APPLICATION_CONTROLLER &&
                    jobConfiguration.Input.ConfigurationComparisonReferenceCriteria.Application == BLANK_APPLICATION_APPLICATION)
                {
                    compareAgainstTemplateConfiguration = true;
                }
                else
                {
                    // Check if there is a valid reference application
                    JobTarget jobTargetReferenceApp = jobConfiguration.Target.Where(t => String.Compare(t.Controller, jobConfiguration.Input.ConfigurationComparisonReferenceCriteria.Controller, true) == 0 && String.Compare(t.Application, jobConfiguration.Input.ConfigurationComparisonReferenceCriteria.Application, true) == 0).FirstOrDefault();
                    if (jobTargetReferenceApp == null)
                    {
                        // No valid reference, fall back to comparing against template
                        logger.Warn("Unable to find reference target {0}, will index default template", jobConfiguration.Input.ConfigurationComparisonReferenceCriteria);
                        loggerConsole.Warn("Unable to find reference target {0}, will index default template", jobConfiguration.Input.ConfigurationComparisonReferenceCriteria);

                        compareAgainstTemplateConfiguration = true;
                    }
                }
                if (compareAgainstTemplateConfiguration == true)
                {
                    // Copy the template file into the Data folder to prepare for index
                    string templateConfigValue = FileIOHelper.ReadFileFromPath(FilePathMap.TemplateApplicationConfigurationFilePath());
                    FileIOHelper.SaveFileToPath(templateConfigValue, FilePathMap.ApplicationConfigurationDataFilePath(jobConfiguration.Input.ConfigurationComparisonReferenceCriteria));

                    // Add this target to the list of targets to unwrap
                    jobConfiguration.Input.ConfigurationComparisonReferenceCriteria.Status = JobTargetStatus.ConfigurationValid;
                    jobConfiguration.Target.Add(jobConfiguration.Input.ConfigurationComparisonReferenceCriteria);
                }

                List<string> listOfControllersAlreadyProcessed = new List<string>(jobConfiguration.Target.Count);

                // Process each target
                for (int i = 0; i < jobConfiguration.Target.Count; i++)
                {
                    Stopwatch stopWatchTarget = new Stopwatch();
                    stopWatchTarget.Start();

                    JobTarget jobTarget = jobConfiguration.Target[i];

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

                        #region Target state check

                        if (jobTarget.Status != JobTargetStatus.ConfigurationValid)
                        {
                            loggerConsole.Trace("Target in invalid state {0}, skipping", jobTarget.Status);

                            continue;
                        }

                        #endregion

                        #region Controller Settings

                        // Only output this once per controller
                        if (listOfControllersAlreadyProcessed.Contains(jobTarget.Controller) == false)
                        {
                            listOfControllersAlreadyProcessed.Add(jobTarget.Controller);

                            loggerConsole.Info("Controller Settings");

                            List<ControllerSetting> controllerSettingsList = new List<ControllerSetting>();
                            List<AppDRESTControllerSetting> controllerSettingsRESTList = FileIOHelper.LoadListOfObjectsFromFile<AppDRESTControllerSetting>(FilePathMap.ControllerSettingsDataFilePath(jobTarget));
                            if (controllerSettingsRESTList != null)
                            {
                                foreach (AppDRESTControllerSetting controllerSetting in controllerSettingsRESTList)
                                {
                                    ControllerSetting controllerSettingRow = new ControllerSetting();

                                    controllerSettingRow.Controller = jobTarget.Controller;
                                    controllerSettingRow.ControllerLink = String.Format(DEEPLINK_CONTROLLER, controllerSettingRow.Controller, DEEPLINK_TIMERANGE_LAST_15_MINUTES);
                                    controllerSettingRow.Name = controllerSetting.name;
                                    controllerSettingRow.Description = controllerSetting.description;
                                    controllerSettingRow.Value = controllerSetting.value;
                                    controllerSettingRow.Updateable = controllerSetting.updateable;
                                    controllerSettingRow.Scope = controllerSetting.scope;

                                    controllerSettingsList.Add(controllerSettingRow);
                                }
                            }

                            controllerSettingsList = controllerSettingsList.OrderBy(c => c.Name).ToList();
                            FileIOHelper.WriteListToCSVFile(controllerSettingsList, new ControllerSettingReportMap(), FilePathMap.ControllerSettingsIndexFilePath(jobTarget));

                            stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + controllerSettingsList.Count;
                        }

                        #endregion

                        #region Preload list of detected entities

                        // For later cross-reference
                        List<EntityTier> tiersList = FileIOHelper.ReadListFromCSVFile<EntityTier>(FilePathMap.TiersIndexFilePath(jobTarget), new TierEntityReportMap());
                        List<EntityBackend> backendsList = FileIOHelper.ReadListFromCSVFile<EntityBackend>(FilePathMap.BackendsIndexFilePath(jobTarget), new BackendEntityReportMap());
                        List<EntityBusinessTransaction> businessTransactionsList = FileIOHelper.ReadListFromCSVFile<EntityBusinessTransaction>(FilePathMap.BusinessTransactionsIndexFilePath(jobTarget), new BusinessTransactionEntityReportMap());

                        #endregion

                        #region Application Summary

                        loggerConsole.Info("Load Configuration file");

                        XmlDocument configXml = FileIOHelper.LoadXmlDocumentFromFile(FilePathMap.ApplicationConfigurationDataFilePath(jobTarget));
                        if (configXml == null)
                        {
                            logger.Warn("No application configuration in {0} file", FilePathMap.ApplicationConfigurationDataFilePath(jobTarget));
                            continue;
                        }

                        loggerConsole.Info("Application Summary");

                        EntityApplicationConfiguration applicationConfiguration = new EntityApplicationConfiguration();
                        applicationConfiguration.Controller = jobTarget.Controller;
                        applicationConfiguration.ControllerLink = String.Format(DEEPLINK_CONTROLLER, applicationConfiguration.Controller, DEEPLINK_TIMERANGE_LAST_15_MINUTES);
                        //applicationConfiguration.ApplicationName = configXml.SelectSingleNode("application/name").InnerText;
                        applicationConfiguration.ApplicationName = jobTarget.Application;
                        applicationConfiguration.ApplicationID = jobTarget.ApplicationID;
                        applicationConfiguration.ApplicationLink = String.Format(DEEPLINK_APPLICATION, applicationConfiguration.Controller, applicationConfiguration.ApplicationID, DEEPLINK_TIMERANGE_LAST_15_MINUTES);
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

                        JObject ruleSetting = JObject.Parse(getStringValueFromXmlNode(configXml.SelectSingleNode("application/eum-cloud-config/page-config")));
                        if (ruleSetting != null)
                        {
                            applicationConfiguration.EUMConfigPage = ruleSetting.ToString();
                        }
                        ruleSetting = JObject.Parse(getStringValueFromXmlNode(configXml.SelectSingleNode("application/eum-cloud-config/mobile-page-config")));
                        if (ruleSetting != null)
                        {
                            applicationConfiguration.EUMConfigMobilePage = ruleSetting.ToString();
                        }
                        ruleSetting = JObject.Parse(getStringValueFromXmlNode(configXml.SelectSingleNode("application/eum-cloud-config/eum-mobile-agent-config")));
                        if (ruleSetting != null)
                        {
                            applicationConfiguration.EUMConfigMobileAgent = ruleSetting.ToString();
                        }

                        applicationConfiguration.AnalyticsConfig = makeXMLFormattedAndIndented(configXml.SelectSingleNode("application/analytics-dynamic-service-configurations"));
                        applicationConfiguration.WorkflowsConfig = makeXMLFormattedAndIndented(configXml.SelectSingleNode("application/workflows"));
                        applicationConfiguration.TasksConfig = makeXMLFormattedAndIndented(configXml.SelectSingleNode("application/tasks"));
                        applicationConfiguration.BTGroupsConfig = makeXMLFormattedAndIndented(configXml.SelectSingleNode("application/business-transaction-groups"));

                        applicationConfiguration.MetricBaselinesConfig = makeXMLFormattedAndIndented(configXml.SelectNodes("application/metric-baselines"));
                        applicationConfiguration.NumBaselines = configXml.SelectNodes("application/metric-baselines/metric-baseline").Count;

                        applicationConfiguration.ErrorAgentConfig = makeXMLFormattedAndIndented(String.Format("<error-configurations>{0}</error-configurations>", makeXMLFormattedAndIndented(configXml.SelectNodes("application/configuration/error-configuration"))));
                        applicationConfiguration.NumErrorRules = configXml.SelectNodes("application/configuration/error-configuration").Count;

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
                        FileIOHelper.WriteListToCSVFile(businessTransactionDiscoveryRulesList, new BusinessTransactionDiscoveryRuleReportMap(), FilePathMap.BusinessTransactionDiscoveryRulesIndexFilePath(jobTarget));

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
                                BusinessTransactionEntryRule businessTransactionEntryRule = fillBusinessTransactionEntryRule(entryMatchPointConfigurationNode, entryMatchPointCustomMatchPointConfigurationNode, applicationConfiguration, null, businessTransactionsList);
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
                                        BusinessTransactionEntryRule businessTransactionEntryRule = fillBusinessTransactionEntryRule(entryMatchPointConfigurationNode, entryMatchPointCustomMatchPointConfigurationNode, applicationConfiguration, applicationComponentNode, businessTransactionsList);
                                        businessTransactionEntryRulesList.Add(businessTransactionEntryRule);
                                    }
                                }
                            }
                        }

                        applicationConfiguration.NumBTEntryRules = businessTransactionEntryRulesList.Where(b => b.IsExclusion == false).Count();
                        applicationConfiguration.NumBTExcludeRules = businessTransactionEntryRulesList.Count - applicationConfiguration.NumBTEntryRules;

                        businessTransactionEntryRulesList = businessTransactionEntryRulesList.OrderBy(b => b.TierName).ThenBy(b => b.AgentType).ThenBy(b => b.EntryPointType).ThenBy(b => b.RuleName).ToList();
                        FileIOHelper.WriteListToCSVFile(businessTransactionEntryRulesList, new BusinessTransactionEntryRuleReportMap(), FilePathMap.BusinessTransactionEntryRulesIndexFilePath(jobTarget));

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + businessTransactionEntryRulesList.Count;

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
                            FileIOHelper.WriteListToCSVFile(businessTransactionEntryScopeList, new BusinessTransactionEntryRuleScopeReportMap(), FilePathMap.BusinessTransactionEntryScopesIndexFilePath(jobTarget));

                            stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + businessTransactionEntryScopeList.Count;


                            List<BusinessTransactionEntryRule20> businessTransactionEntryRules20List = new List<BusinessTransactionEntryRule20>();

                            foreach (XmlNode ruleConfigurationNode in configXml.SelectNodes("application/mds-data/mds-config-data/rule-list/rule"))
                            {
                                BusinessTransactionEntryRule20 businessTransactionEntryRule = fillBusinessTransactionEntryRule20(ruleConfigurationNode, scopeToRuleMappingConfigurationNode, applicationConfiguration, businessTransactionsList);
                                if (businessTransactionEntryRule != null)
                                {
                                    businessTransactionEntryRules20List.Add(businessTransactionEntryRule);
                                }
                            }

                            applicationConfiguration.NumBT20EntryRules = businessTransactionEntryRules20List.Where(b => b.IsExclusion == false).Count();
                            applicationConfiguration.NumBT20ExcludeRules = businessTransactionEntryRules20List.Count - applicationConfiguration.NumBT20EntryRules;

                            businessTransactionEntryRules20List = businessTransactionEntryRules20List.OrderBy(b => b.ScopeName).ThenBy(b => b.AgentType).ThenBy(b => b.EntryPointType).ThenBy(b => b.RuleName).ToList();
                            FileIOHelper.WriteListToCSVFile(businessTransactionEntryRules20List, new BusinessTransactionEntryRule20ReportMap(), FilePathMap.BusinessTransactionEntryRules20IndexFilePath(jobTarget));

                            stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + businessTransactionEntryRules20List.Count;


                            List<BusinessTransactionDiscoveryRule20> businessTransactionDiscoveryRule20List = new List<BusinessTransactionDiscoveryRule20>();

                            foreach (XmlNode ruleConfigurationNode in configXml.SelectNodes("application/mds-data/mds-config-data/rule-list/rule"))
                            {
                                List<BusinessTransactionDiscoveryRule20> businessTransactionDiscoveryRuleList = fillBusinessTransactionDiscoveryRule20(ruleConfigurationNode, scopeToRuleMappingConfigurationNode, applicationConfiguration, businessTransactionsList);
                                if (businessTransactionDiscoveryRuleList != null)
                                {
                                    businessTransactionDiscoveryRule20List.AddRange(businessTransactionDiscoveryRuleList);
                                }
                            }

                            applicationConfiguration.NumBT20DiscoveryRules = businessTransactionEntryRules20List.Count;

                            businessTransactionEntryRules20List = businessTransactionEntryRules20List.OrderBy(b => b.ScopeName).ThenBy(b => b.AgentType).ThenBy(b => b.EntryPointType).ThenBy(b => b.RuleName).ToList();
                            FileIOHelper.WriteListToCSVFile(businessTransactionDiscoveryRule20List, new BusinessTransactionDiscoveryRule20ReportMap(), FilePathMap.BusinessTransactionDiscoveryRules20IndexFilePath(jobTarget));

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
                                BackendDiscoveryRule backendDiscoveryRule = fillBackendDiscoveryRule(backendDiscoveryMatchPointConfigurationNode, backendDiscoveryConfigurationNode, applicationConfiguration, null, backendsList);
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
                                        BackendDiscoveryRule backendDiscoveryRule = fillBackendDiscoveryRule(backendDiscoveryMatchPointConfigurationNode, backendDiscoveryConfigurationNode, applicationConfiguration, applicationComponentNode, backendsList);
                                        backendDiscoveryRulesList.Add(backendDiscoveryRule);
                                    }
                                }
                            }
                        }

                        applicationConfiguration.NumBackendRules = backendDiscoveryRulesList.Count;

                        backendDiscoveryRulesList = backendDiscoveryRulesList.OrderBy(b => b.TierName).ThenBy(b => b.AgentType).ThenBy(b => b.ExitType).ThenBy(b => b.RuleName).ToList();
                        FileIOHelper.WriteListToCSVFile(backendDiscoveryRulesList, new BackendDiscoveryRuleReportMap(), FilePathMap.BackendDiscoveryRulesIndexFilePath(jobTarget));

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
                                CustomExitRule customExitRule = fillCustomExitRule(backendDiscoveryMatchPointConfigurationNode, customExitConfigurationNode, applicationConfiguration, null, backendsList);
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
                                        CustomExitRule customExitRule = fillCustomExitRule(backendDiscoveryMatchPointConfigurationNode, customExitConfigurationNode, applicationConfiguration, applicationComponentNode, backendsList);
                                        customExitRulesList.Add(customExitRule);
                                    }
                                }
                            }
                        }

                        applicationConfiguration.NumBackendRules = applicationConfiguration.NumBackendRules + customExitRulesList.Count;

                        customExitRulesList = customExitRulesList.OrderBy(b => b.TierName).ThenBy(b => b.AgentType).ThenBy(b => b.ExitType).ThenBy(b => b.RuleName).ToList();
                        FileIOHelper.WriteListToCSVFile(customExitRulesList, new CustomExitRuleReportMap(), FilePathMap.CustomExitRulesIndexFilePath(jobTarget));

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
                        FileIOHelper.WriteListToCSVFile(agentConfigurationPropertiesList, new AgentConfigurationPropertyReportMap(), FilePathMap.AgentConfigurationPropertiesIndexFilePath(jobTarget));

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
                        FileIOHelper.WriteListToCSVFile(informationPointRulesList, new InformationPointRuleReportMap(), FilePathMap.InformationPointRulesIndexFilePath(jobTarget));

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + informationPointRulesList.Count;

                        #endregion

                        #region Detected Business Transaction and Assigned Data Collectors

                        loggerConsole.Info("Detected Business Transaction and Assigned Data Collectors");

                        List<EntityBusinessTransactionConfiguration> entityBusinessTransactionConfigurationsList = new List<EntityBusinessTransactionConfiguration>();

                        // Tier settings
                        // application
                        //      application-components
                        //          application-component
                        //              business-transaction
                        foreach (XmlNode applicationComponentNode in configXml.SelectNodes("application/application-components/application-component"))
                        {
                            foreach (XmlNode businessTransactionConfigurationtNode in applicationComponentNode.SelectNodes("business-transactions/business-transaction"))
                            {
                                EntityBusinessTransactionConfiguration entityBusinessTransactionConfiguration = fillEntityBusinessTransactionConfiguration(applicationComponentNode, businessTransactionConfigurationtNode, applicationConfiguration, tiersList, businessTransactionsList);
                                entityBusinessTransactionConfigurationsList.Add(entityBusinessTransactionConfiguration);
                            }
                        }

                        applicationConfiguration.NumBTs = entityBusinessTransactionConfigurationsList.Count;

                        entityBusinessTransactionConfigurationsList = entityBusinessTransactionConfigurationsList.OrderBy(b => b.TierName).ThenBy(b => b.BTType).ThenBy(b => b.BTName).ToList();
                        FileIOHelper.WriteListToCSVFile(entityBusinessTransactionConfigurationsList, new EntityBusinessTransactionConfigurationReportMap(), FilePathMap.EntityBusinessTransactionConfigurationsIndexFilePath(jobTarget));

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + entityBusinessTransactionConfigurationsList.Count;

                        #endregion

                        #region Tier Settings

                        loggerConsole.Info("Tier Settings");

                        List<EntityTierConfiguration> entityTierConfigurationsList = new List<EntityTierConfiguration>();

                        // Tier settings
                        // application
                        //      application-components
                        //          application-component
                        foreach (XmlNode applicationComponentNode in configXml.SelectNodes("application/application-components/application-component"))
                        {
                            EntityTierConfiguration entityTierConfiguration = fillEntityTierConfiguration(applicationComponentNode, applicationConfiguration, tiersList, entityBusinessTransactionConfigurationsList);
                            entityTierConfigurationsList.Add(entityTierConfiguration);
                        }

                        applicationConfiguration.NumTiers = entityTierConfigurationsList.Count;

                        entityTierConfigurationsList = entityTierConfigurationsList.OrderBy(p => p.TierName).ToList();
                        FileIOHelper.WriteListToCSVFile(entityTierConfigurationsList, new EntityTierConfigurationReportMap(), FilePathMap.EntityTierConfigurationsIndexFilePath(jobTarget));

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
                        FileIOHelper.WriteListToCSVFile(methodInvocationDataCollectorsList, new MethodInvocationDataCollectorReportMap(), FilePathMap.MethodInvocationDataCollectorsIndexFilePath(jobTarget));

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
                        FileIOHelper.WriteListToCSVFile(httpDataCollectorsList, new HTTPDataCollectorReportMap(), FilePathMap.HttpDataCollectorsIndexFilePath(jobTarget));

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
                        FileIOHelper.WriteListToCSVFile(agentCallGraphSettingCollectorsList, new AgentCallGraphSettingReportMap(), FilePathMap.AgentCallGraphSettingsIndexFilePath(jobTarget));

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + agentCallGraphSettingCollectorsList.Count;

                        #endregion

                        #region Health Rules

                        loggerConsole.Info("Health Rules");

                        List<HealthRule> healthRulesList = new List<HealthRule>();

                        // Application level
                        // application
                        //      configuration
                        //          call-graph
                        foreach (XmlNode healthRuleConfigurationNode in configXml.SelectNodes("application/health-rules/health-rule"))
                        {
                            HealthRule healthRule = fillHealthRule(healthRuleConfigurationNode, applicationConfiguration);
                            healthRulesList.Add(healthRule);
                        }

                        applicationConfiguration.NumHealthRules = healthRulesList.Count;

                        healthRulesList = healthRulesList.OrderBy(h => h.RuleType).ThenBy(h => h.RuleName).ToList();
                        FileIOHelper.WriteListToCSVFile(healthRulesList, new HealthRuleReportMap(), FilePathMap.HealthRulesIndexFilePath(jobTarget));

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + healthRulesList.Count;

                        #endregion

                        #region Application Settings

                        List<EntityApplicationConfiguration> applicationConfigurationsList = new List<EntityApplicationConfiguration>(1);
                        applicationConfigurationsList.Add(applicationConfiguration);
                        FileIOHelper.WriteListToCSVFile(applicationConfigurationsList, new EntityApplicationConfigurationReportMap(), FilePathMap.ApplicationConfigurationIndexFilePath(jobTarget));

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + applicationConfigurationsList.Count;

                        #endregion

                        #region Combine All for Report CSV

                        // If it is the first one, clear out the combined folder
                        if (i == 0)
                        {
                            FileIOHelper.DeleteFolder(FilePathMap.ConfigurationReportFolderPath());
                            Thread.Sleep(1000);
                            FileIOHelper.CreateFolder(FilePathMap.ConfigurationReportFolderPath());
                        }

                        // Append all the individual application files into one
                        if (File.Exists(FilePathMap.ApplicationConfigurationIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.ApplicationConfigurationIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.ApplicationConfigurationReportFilePath(), FilePathMap.ApplicationConfigurationIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.BusinessTransactionDiscoveryRulesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.BusinessTransactionDiscoveryRulesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.BusinessTransactionDiscoveryRulesReportFilePath(), FilePathMap.BusinessTransactionDiscoveryRulesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.BusinessTransactionEntryRulesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.BusinessTransactionEntryRulesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.BusinessTransactionEntryRulesReportFilePath(), FilePathMap.BusinessTransactionEntryRulesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.BusinessTransactionEntryScopesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.BusinessTransactionEntryScopesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.BusinessTransactionEntryScopesReportFilePath(), FilePathMap.BusinessTransactionEntryScopesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.BusinessTransactionDiscoveryRules20IndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.BusinessTransactionDiscoveryRules20IndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.BusinessTransactionDiscoveryRules20ReportFilePath(), FilePathMap.BusinessTransactionDiscoveryRules20IndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.BusinessTransactionEntryRules20IndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.BusinessTransactionEntryRules20IndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.BusinessTransactionEntryRules20ReportFilePath(), FilePathMap.BusinessTransactionEntryRules20IndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.BackendDiscoveryRulesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.BackendDiscoveryRulesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.BackendDiscoveryRulesReportFilePath(), FilePathMap.BackendDiscoveryRulesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.CustomExitRulesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.CustomExitRulesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.CustomExitRulesReportFilePath(), FilePathMap.CustomExitRulesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.AgentConfigurationPropertiesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.AgentConfigurationPropertiesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.AgentConfigurationPropertiesReportFilePath(), FilePathMap.AgentConfigurationPropertiesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.InformationPointRulesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.InformationPointRulesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.InformationPointRulesReportFilePath(), FilePathMap.InformationPointRulesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.EntityBusinessTransactionConfigurationsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.EntityBusinessTransactionConfigurationsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.EntityBusinessTransactionConfigurationsReportFilePath(), FilePathMap.EntityBusinessTransactionConfigurationsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.EntityTierConfigurationsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.EntityTierConfigurationsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.EntityTierConfigurationsReportFilePath(), FilePathMap.EntityTierConfigurationsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.MethodInvocationDataCollectorsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.MethodInvocationDataCollectorsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.MethodInvocationDataCollectorsReportFilePath(), FilePathMap.MethodInvocationDataCollectorsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.HttpDataCollectorsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.HttpDataCollectorsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.HttpDataCollectorsReportFilePath(), FilePathMap.HttpDataCollectorsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.AgentCallGraphSettingsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.AgentCallGraphSettingsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.AgentCallGraphSettingsReportFilePath(), FilePathMap.AgentCallGraphSettingsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.HealthRulesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.HealthRulesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.HealthRulesReportFilePath(), FilePathMap.HealthRulesIndexFilePath(jobTarget));
                        }

                        // If it is the last one, let's append all Controller settings
                        if (i == jobConfiguration.Target.Count - 1)
                        {
                            var controllers = jobConfiguration.Target.GroupBy(t => t.Controller);
                            foreach (var controllerGroup in controllers)
                            {
                                FileIOHelper.AppendTwoCSVFiles(FilePathMap.ControllerSettingsReportFilePath(), FilePathMap.ControllerSettingsIndexFilePath(controllerGroup.ToList()[0]));
                            }
                        }

                        #endregion
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex);
                        loggerConsole.Warn(ex);
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

                // Remove the last item from job configuration that was put there earlier if comparing against template
                if (compareAgainstTemplateConfiguration == true)
                {
                    jobConfiguration.Target.RemoveAt(jobConfiguration.Target.Count - 1);
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

        public override bool ShouldExecute(JobConfiguration jobConfiguration)
        {
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

        private static int getIntegerValueFromXmlNode(XmlNode xmlNode)
        {
            if (xmlNode == null) return 0;

            if (((XmlElement)xmlNode).IsEmpty == true)
            {
                return 0;
            }
            else
            {
                int value;
                if (Int32.TryParse(xmlNode.InnerText, out value) == true)
                {
                    return value;
                }
                else
                {
                    double value1;
                    if (Double.TryParse(xmlNode.InnerText, out value1) == true)
                    {
                        return Convert.ToInt32(Math.Floor(value1));
                    }
                    else
                    {
                        return 0;
                    }
                }
            }
        }

        private static long getLongValueFromXmlNode(XmlNode xmlNode)
        {
            if (xmlNode == null) return 0;

            if (((XmlElement)xmlNode).IsEmpty == true)
            {
                return 0;
            }
            else
            {
                long value;
                if (Int64.TryParse(xmlNode.InnerText, out value) == true)
                {
                    return value;
                }
                else
                {
                    double value1;
                    if (Double.TryParse(xmlNode.InnerText, out value1) == true)
                    {
                        return Convert.ToInt64(Math.Floor(value1));
                    }
                    else
                    {
                        return 0;
                    }
                }
            }
        }

        private static bool getBoolValueFromXmlNode(XmlNode xmlNode)
        {
            if (xmlNode == null) return false;

            if (((XmlElement)xmlNode).IsEmpty == true)
            {
                return false;
            }
            else
            {
                bool value;
                if (Boolean.TryParse(xmlNode.InnerText, out value) == true)
                {
                    return value;
                }
                else
                {
                    return false;
                }
            }
        }

        private static string getStringValueFromXmlNode(XmlNode xmlNode)
        {
            if (xmlNode == null) return String.Empty;

            if (((XmlElement)xmlNode).IsEmpty == true)
            {
                return String.Empty;
            }
            else
            {
                return xmlNode.InnerText;
            }
        }

        /// <summary>
        /// https://stackoverflow.com/questions/1123718/format-xml-string-to-print-friendly-xml-string
        /// </summary>
        /// <param name="XML"></param>
        /// <returns></returns>
        private static string makeXMLFormattedAndIndented(String XML)
        {
            string Result = "";

            MemoryStream MS = new MemoryStream();
            XmlTextWriter W = new XmlTextWriter(MS, Encoding.Unicode);
            XmlDocument D = new XmlDocument();

            try
            {
                // Load the XmlDocument with the XML.
                D.LoadXml(XML);

                W.Formatting = Formatting.Indented;

                // Write the XML into a formatting XmlTextWriter
                D.WriteContentTo(W);
                W.Flush();
                MS.Flush();

                // Have to rewind the MemoryStream in order to read
                // its contents.
                MS.Position = 0;

                // Read MemoryStream contents into a StreamReader.
                StreamReader SR = new StreamReader(MS);

                // Extract the text from the StreamReader.
                String FormattedXML = SR.ReadToEnd();

                Result = FormattedXML;
            }
            catch (XmlException)
            {
            }

            MS.Close();
            W.Close();

            return Result;
        }

        private static string makeXMLFormattedAndIndented(XmlNode xmlNode)
        {
            if (xmlNode != null)
            {
                return makeXMLFormattedAndIndented(xmlNode.OuterXml);
            }
            else
            {
                return String.Empty;
            }
        }

        private static string makeXMLFormattedAndIndented(XmlNodeList xmlNodeList)
        {
            if (xmlNodeList.Count > 0)
            {
                StringBuilder sb = new StringBuilder(128 * xmlNodeList.Count);
                foreach (XmlNode xmlNode in xmlNodeList)
                {
                    sb.Append(makeXMLFormattedAndIndented(xmlNode));
                    sb.AppendLine();
                }
                sb.Remove(sb.Length - 1, 1);
                return sb.ToString();
            }
            else
            {
                return String.Empty;
            }
        }

        private static BusinessTransactionDiscoveryRule fillBusinessTransactionDiscoveryRule(XmlNode entryMatchPointConfigurationNode, XmlNode entryMatchPointTransactionConfigurationNode, EntityApplicationConfiguration applicationConfiguration, XmlNode applicationComponentNode)
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

        private static BusinessTransactionEntryRule fillBusinessTransactionExcludeRule(XmlNode entryMatchPointConfigurationNode, XmlNode entryMatchPointTransactionConfigurationNode, XmlNode entryMatchPointCustomMatchPointConfigurationNode, EntityApplicationConfiguration applicationConfiguration, XmlNode applicationComponentNode)
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

            return businessTransactionEntryRule;
        }

        private static BusinessTransactionEntryRule fillBusinessTransactionEntryRule(XmlNode entryMatchPointConfigurationNode, XmlNode entryMatchPointCustomMatchPointConfigurationNode, EntityApplicationConfiguration applicationConfiguration, XmlNode applicationComponentNode, List<EntityBusinessTransaction> businessTransactionsList)
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
                List<EntityBusinessTransaction> businessTransactionsForThisRule = new List<EntityBusinessTransaction>();
                businessTransactionsForThisRule.AddRange(businessTransactionsList.Where(b => b.BTName == businessTransactionEntryRule.RuleName).ToList());
                businessTransactionsForThisRule.AddRange(businessTransactionsList.Where(b => b.BTName.StartsWith(String.Format("{0}.", businessTransactionEntryRule.RuleName))).ToList());
                businessTransactionsForThisRule.AddRange(businessTransactionsList.Where(b => b.BTNameOriginal == businessTransactionEntryRule.RuleName).ToList());
                businessTransactionsForThisRule.AddRange(businessTransactionsList.Where(b => b.BTNameOriginal.StartsWith(String.Format("{0}.", businessTransactionEntryRule.RuleName))).ToList());
                businessTransactionsForThisRule = businessTransactionsForThisRule.Distinct().ToList();
                businessTransactionEntryRule.NumDetectedBTs = businessTransactionsForThisRule.Count;
                if (businessTransactionsForThisRule.Count > 0)
                {
                    StringBuilder sb = new StringBuilder(32 * businessTransactionsForThisRule.Count);
                    foreach (EntityBusinessTransaction bt in businessTransactionsForThisRule)
                    {
                        sb.AppendFormat("{0}/{1};\n", bt.TierName, bt.BTName);
                    }
                    sb.Remove(sb.Length - 1, 1);

                    businessTransactionEntryRule.DetectedBTs = sb.ToString();
                }
            }

            return businessTransactionEntryRule;
        }

        private static BusinessTransactionEntryScope fillBusinessTransactionEntryScope(XmlNode scopeConfigurationNode, XmlNode scopeToRuleMappingConfigurationNode, EntityApplicationConfiguration applicationConfiguration)
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
                businessTransactionEntryScope.IncludedTiers = sb.ToString();
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

        private static BusinessTransactionEntryRule20 fillBusinessTransactionEntryRule20(XmlNode ruleConfigurationNode, XmlNode scopeToRuleMappingConfigurationNode, EntityApplicationConfiguration applicationConfiguration, List<EntityBusinessTransaction> businessTransactionsList)
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
                List<EntityBusinessTransaction> businessTransactionsForThisRule = new List<EntityBusinessTransaction>();
                businessTransactionsForThisRule.AddRange(businessTransactionsList.Where(b => b.BTName == businessTransactionEntryRule.RuleName).ToList());
                businessTransactionsForThisRule.AddRange(businessTransactionsList.Where(b => b.BTName.StartsWith(String.Format("{0}.", businessTransactionEntryRule.RuleName))).ToList());
                businessTransactionsForThisRule.AddRange(businessTransactionsList.Where(b => b.BTNameOriginal == businessTransactionEntryRule.RuleName).ToList());
                businessTransactionsForThisRule.AddRange(businessTransactionsList.Where(b => b.BTNameOriginal.StartsWith(String.Format("{0}.", businessTransactionEntryRule.RuleName))).ToList());
                businessTransactionsForThisRule = businessTransactionsForThisRule.Distinct().ToList();
                businessTransactionEntryRule.NumDetectedBTs = businessTransactionsForThisRule.Count;
                if (businessTransactionsForThisRule.Count > 0)
                {
                    StringBuilder sb = new StringBuilder(32 * businessTransactionsForThisRule.Count);
                    foreach (EntityBusinessTransaction bt in businessTransactionsForThisRule)
                    {
                        sb.AppendFormat("{0}/{1};\n", bt.TierName, bt.BTName);
                    }
                    sb.Remove(sb.Length - 1, 1);

                    businessTransactionEntryRule.DetectedBTs = sb.ToString();
                }
            }

            businessTransactionEntryRule.RuleRawValue = makeXMLFormattedAndIndented(ruleConfigurationNode);

            return businessTransactionEntryRule;
        }

        private static List<BusinessTransactionDiscoveryRule20> fillBusinessTransactionDiscoveryRule20(XmlNode ruleConfigurationNode, XmlNode scopeToRuleMappingConfigurationNode, EntityApplicationConfiguration applicationConfiguration, List<EntityBusinessTransaction> businessTransactionsList)
        {
            List<BusinessTransactionDiscoveryRule20> businessTransactionDiscoveryRule20List = new List<BusinessTransactionDiscoveryRule20>();

            JObject txRuleSettings = JObject.Parse(getStringValueFromXmlNode(ruleConfigurationNode.SelectSingleNode("tx-match-rule")));
            if (txRuleSettings != null)
            {
                if (txRuleSettings["type"].ToString() != "AUTOMATIC_DISCOVERY")
                {
                    // This is not an autodiscovery rule, do not fill it out and bail
                    return null;
                }

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

                        businessTransactionDiscoveryRule20.EntryPointType = txDiscoveryConfig["txentrypointtype"].ToString();
                        businessTransactionDiscoveryRule20.IsMonitoringEnabled = (bool)txDiscoveryConfig["monitoringenabled"];
                        businessTransactionDiscoveryRule20.IsDiscoveryEnabled = (bool)txDiscoveryConfig["discoveryenabled"];
                        businessTransactionDiscoveryRule20.NamingConfigType = txDiscoveryConfig["namingschemetype"].ToString();

                        if (txDiscoveryConfig["httpautodiscovery"] != null)
                        {
                            businessTransactionDiscoveryRule20.HTTPAutoDiscovery = txDiscoveryConfig["httpautodiscovery"].ToString();
                        }

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

            return businessTransactionDiscoveryRule20List;
        }

        private static BackendDiscoveryRule fillBackendDiscoveryRule(XmlNode backendDiscoveryMatchPointConfigurationNode, XmlNode backendDiscoveryConfigurationNode, EntityApplicationConfiguration applicationConfiguration, XmlNode applicationComponentNode, List<EntityBackend> backendsList)
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
                List<EntityBackend> backendsForThisRule = new List<EntityBackend>();

                // Try to find them by match first
                backendsForThisRule.AddRange(backendsList.Where(b => b.BackendName == backendDiscoveryRule.RuleName).ToList());
                backendsForThisRule.AddRange(backendsList.Where(b => b.BackendName.StartsWith(String.Format("{0}", backendDiscoveryRule.RuleName))).ToList());
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
                    foreach (EntityBackend backend in backendsForThisRule)
                    {
                        sb.AppendFormat("{0} ({1});\n", backend.BackendName, backend.BackendID);
                    }
                    sb.Remove(sb.Length - 1, 1);

                    backendDiscoveryRule.DetectedBackends = sb.ToString();
                }
            }

            return backendDiscoveryRule;
        }

        private static CustomExitRule fillCustomExitRule(XmlNode backendDiscoveryMatchPointConfigurationNode, XmlNode customExitConfigurationNode, EntityApplicationConfiguration applicationConfiguration, XmlNode applicationComponentNode, List<EntityBackend> backendsList)
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

            customExitRule.DataCollectorsConfig = makeXMLFormattedAndIndented(customExitConfigurationNode.SelectNodes("instrumentation-point/method-invocation-data-gatherer-config"));
            customExitRule.InfoPointsConfig = makeXMLFormattedAndIndented(customExitConfigurationNode.SelectNodes("instrumentation-point/info-point-metric-definition"));

            customExitRule.RuleRawValue = makeXMLFormattedAndIndented(customExitConfigurationNode);

            if (applicationComponentNode != null)
            {
                customExitRule.TierName = getStringValueFromXmlNode(applicationComponentNode.SelectSingleNode("name"));
            }

            if (backendsList != null)
            {
                List<EntityBackend> backendsForThisRule = new List<EntityBackend>();

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
                    foreach (EntityBackend backend in backendsForThisRule)
                    {
                        sb.AppendFormat("{0} ({1});\n", backend.BackendName, backend.BackendID);
                    }
                    sb.Remove(sb.Length - 1, 1);

                    customExitRule.DetectedBackends = sb.ToString();
                }
            }

            return customExitRule;
        }

        private static InformationPointRule fillInformationPointRule(XmlNode informationPointConfigurationNode, EntityApplicationConfiguration applicationConfiguration)
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

            informationPointRule.InfoPointsConfig = makeXMLFormattedAndIndented(informationPointConfigurationNode.SelectNodes("info-point-metric-definition"));

            informationPointRule.RuleRawValue = makeXMLFormattedAndIndented(informationPointConfigurationNode);

            return informationPointRule;
        }

        private static AgentConfigurationProperty fillAgentConfigurationProperty(XmlNode agentConfigurationNode, XmlNode agentPropertyDefinitionConfigurationNode, XmlNode agentPropertyValueConfigurationNode, EntityApplicationConfiguration applicationConfiguration, XmlNode applicationComponentNode)
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

            return agentConfigurationProperty;
        }

        private static MethodInvocationDataCollector fillMethodInvocationDataCollector(XmlNode methodInvocationDataCollectorConfigurationNode, XmlNode dataGathererConfigurationNode, EntityApplicationConfiguration applicationConfiguration, List<EntityBusinessTransactionConfiguration> entityBusinessTransactionConfigurationsList)
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
                List<EntityBusinessTransactionConfiguration> entityBusinessTransactionConfigurationsForThisDCList = entityBusinessTransactionConfigurationsList.Where(b => b.AssignedMIDCs.Contains(String.Format("{0};", methodInvocationDataCollector.CollectorName)) == true).ToList();

                if (entityBusinessTransactionConfigurationsForThisDCList.Count > 0)
                {
                    methodInvocationDataCollector.IsAssignedToBTs = true;
                    methodInvocationDataCollector.NumAssignedBTs = entityBusinessTransactionConfigurationsForThisDCList.Count;

                    StringBuilder sb = new StringBuilder(32 * entityBusinessTransactionConfigurationsForThisDCList.Count);
                    foreach (EntityBusinessTransactionConfiguration bt in entityBusinessTransactionConfigurationsForThisDCList)
                    {
                        sb.AppendFormat("{0}/{1};\n", bt.TierName, bt.BTName);
                    }
                    sb.Remove(sb.Length - 1, 1);
                    methodInvocationDataCollector.AssignedBTs = sb.ToString();
                }
            }

            return methodInvocationDataCollector;
        }

        private static HTTPDataCollector fillHTTPDataCollector(XmlNode httpDataCollectorConfigurationNode, XmlNode dataGathererConfigurationNode, EntityApplicationConfiguration applicationConfiguration, List<EntityBusinessTransactionConfiguration> entityBusinessTransactionConfigurationsList)
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
                List<EntityBusinessTransactionConfiguration> entityBusinessTransactionConfigurationsForThisDCList = entityBusinessTransactionConfigurationsList.Where(b => b.AssignedMIDCs.Contains(String.Format("{0};", httpDataCollector.CollectorName)) == true).ToList();

                if (entityBusinessTransactionConfigurationsForThisDCList.Count > 0)
                {
                    httpDataCollector.IsAssignedToBTs = true;
                    httpDataCollector.NumAssignedBTs = entityBusinessTransactionConfigurationsForThisDCList.Count;

                    sb = new StringBuilder(32 * entityBusinessTransactionConfigurationsForThisDCList.Count);
                    foreach (EntityBusinessTransactionConfiguration bt in entityBusinessTransactionConfigurationsForThisDCList)
                    {
                        sb.AppendFormat("{0}/{1};\n", bt.TierName, bt.BTName);
                    }
                    sb.Remove(sb.Length - 1, 1);
                    httpDataCollector.AssignedBTs = sb.ToString();
                }
            }

            return httpDataCollector;
        }

        private static EntityTierConfiguration fillEntityTierConfiguration(XmlNode applicationComponentNode, EntityApplicationConfiguration applicationConfiguration, List<EntityTier> tiersList, List<EntityBusinessTransactionConfiguration> entityBusinessTransactionConfigurationsList)
        {
            EntityTierConfiguration entityTierConfiguration = new EntityTierConfiguration();

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
                EntityTier tier = tiersList.Where(t => t.TierName == entityTierConfiguration.TierName).FirstOrDefault();
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
                List<EntityBusinessTransactionConfiguration> businessTransactionsList = entityBusinessTransactionConfigurationsList.Where(b => b.TierID == entityTierConfiguration.TierID).ToList();
                entityTierConfiguration.NumBTs = businessTransactionsList.Count;

                var businessTransactionsListGroup = businessTransactionsList.GroupBy(b => b.BTType);
                entityTierConfiguration.NumBTTypes = businessTransactionsListGroup.Count();
            }

            return entityTierConfiguration;
        }

        private static EntityBusinessTransactionConfiguration fillEntityBusinessTransactionConfiguration(XmlNode applicationComponentNode, XmlNode businessTransactionConfigurationtNode, EntityApplicationConfiguration applicationConfiguration, List<EntityTier> tiersList, List<EntityBusinessTransaction> businessTransactionsList)
        {
            EntityBusinessTransactionConfiguration entityBusinessTransactionConfiguration = new EntityBusinessTransactionConfiguration();

            entityBusinessTransactionConfiguration.Controller = applicationConfiguration.Controller;
            entityBusinessTransactionConfiguration.ControllerLink = applicationConfiguration.ControllerLink;
            entityBusinessTransactionConfiguration.ApplicationName = applicationConfiguration.ApplicationName;
            entityBusinessTransactionConfiguration.ApplicationID = applicationConfiguration.ApplicationID;
            entityBusinessTransactionConfiguration.ApplicationLink = applicationConfiguration.ApplicationLink;

            entityBusinessTransactionConfiguration.TierName = getStringValueFromXmlNode(applicationComponentNode.SelectSingleNode("name"));
            if (tiersList != null)
            {
                EntityTier tier = tiersList.Where(t => t.TierName == entityBusinessTransactionConfiguration.TierName).FirstOrDefault();
                if (tier != null)
                {
                    entityBusinessTransactionConfiguration.TierID = tier.TierID;
                }
            }

            entityBusinessTransactionConfiguration.BTName = getStringValueFromXmlNode(businessTransactionConfigurationtNode.SelectSingleNode("name"));
            entityBusinessTransactionConfiguration.BTType = businessTransactionConfigurationtNode.Attributes["transaction-entry-point-type"].Value;
            if (businessTransactionsList != null)
            {
                EntityBusinessTransaction businessTransaction = businessTransactionsList.Where(b => b.BTName == entityBusinessTransactionConfiguration.BTName && b.TierName == entityBusinessTransactionConfiguration.TierName).FirstOrDefault();
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

        private static AgentCallGraphSetting fillAgentCallGraphSetting(XmlNode agentCallGraphSettingConfigurationNode, EntityApplicationConfiguration applicationConfiguration)
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

        private static HealthRule fillHealthRule(XmlNode healthRuleConfigurationNode, EntityApplicationConfiguration applicationConfiguration)
        {
            HealthRule healthRule = new HealthRule();

            healthRule.Controller = applicationConfiguration.Controller;
            healthRule.ControllerLink = applicationConfiguration.ControllerLink;
            healthRule.ApplicationName = applicationConfiguration.ApplicationName;
            healthRule.ApplicationID = applicationConfiguration.ApplicationID;
            healthRule.ApplicationLink = applicationConfiguration.ApplicationLink;

            healthRule.RuleName = getStringValueFromXmlNode(healthRuleConfigurationNode.SelectSingleNode("name"));
            healthRule.HRRuleType = getStringValueFromXmlNode(healthRuleConfigurationNode.SelectSingleNode("type"));
            healthRule.Description = getStringValueFromXmlNode(healthRuleConfigurationNode.SelectSingleNode("description"));
            healthRule.IsEnabled = getBoolValueFromXmlNode(healthRuleConfigurationNode.SelectSingleNode("enabled"));
            healthRule.IsDefault = getBoolValueFromXmlNode(healthRuleConfigurationNode.SelectSingleNode("is-default"));
            healthRule.IsAlwaysEnabled = getBoolValueFromXmlNode(healthRuleConfigurationNode.SelectSingleNode("always-enabled"));
            healthRule.Schedule = getStringValueFromXmlNode(healthRuleConfigurationNode.SelectSingleNode("schedule"));
            healthRule.DurationOfEvalPeriod = getIntegerValueFromXmlNode(healthRuleConfigurationNode.SelectSingleNode("duration-min"));
            healthRule.WaitTimeAfterViolation = getIntegerValueFromXmlNode(healthRuleConfigurationNode.SelectSingleNode("wait-time-min"));

            healthRule.AffectedEntitiesRawValue = makeXMLFormattedAndIndented(healthRuleConfigurationNode.SelectSingleNode("affected-entities-match-criteria"));
            healthRule.CriticalConditionRawValue = makeXMLFormattedAndIndented(healthRuleConfigurationNode.SelectSingleNode("critical-execution-criteria"));
            healthRule.WarningConditionRawValue = makeXMLFormattedAndIndented(healthRuleConfigurationNode.SelectSingleNode("warning-execution-criteria"));

            // Affected entity selection
            XmlNode affectedWrapperXmlNode = healthRuleConfigurationNode.SelectSingleNode("affected-entities-match-criteria").ChildNodes[0];
            if (affectedWrapperXmlNode != null)
            {
                healthRule.AffectsEntityType = getStringValueFromXmlNode(affectedWrapperXmlNode.SelectSingleNode("type"));
                healthRule.AffectsEntityMatchType = getStringValueFromXmlNode(affectedWrapperXmlNode.SelectSingleNode("match-type"));
                healthRule.AffectsEntityMatchPattern = getStringValueFromXmlNode(affectedWrapperXmlNode.SelectSingleNode("match-pattern"));
                healthRule.AffectsEntityMatchIsInverse = getBoolValueFromXmlNode(affectedWrapperXmlNode.SelectSingleNode("inverse"));
                healthRule.AffectsEntityMatchCriteria = makeXMLFormattedAndIndented(affectedWrapperXmlNode.SelectNodes("*[not(self::type) and not(self::match-type) and not(self::match-pattern) and not(self::inverse)]"));
            }

            // XML can look like that for single element
            //<critical-execution-criteria>
            //    <entity-aggregation-scope>
            //        <type>ANY</type>
            //        <value>0</value>
            //    </entity-aggregation-scope>
            //    <policy-condition>
            //        <type>leaf</type>
            //        <display-name>condition 1</display-name>
            //        <condition-value-type>ABSOLUTE</condition-value-type>
            //        <condition-value>0.0</condition-value>
            //        <operator>GREATER_THAN</operator>
            //        <condition-expression/>
            //        <use-active-baseline>false</use-active-baseline>
            //        <trigger-on-no-data>true</trigger-on-no-data>
            //        <metric-expression>
            //            <type>leaf</type>
            //            <function-type>VALUE</function-type>
            //            <value>0</value>
            //            <is-literal-expression>false</is-literal-expression>
            //            <display-name>null</display-name>
            //            <metric-definition>
            //                <type>LOGICAL_METRIC</type>
            //                <logical-metric-name>Agent|App|Availability</logical-metric-name>
            //            </metric-definition>
            //        </metric-expression>
            //    </policy-condition>
            //</critical-execution-criteria>

            // Or like that for multiple
            //<critical-execution-criteria>
            //    <entity-aggregation-scope>
            //        <type>AGGREGATE</type>
            //        <value>0</value>
            //    </entity-aggregation-scope>
            //    <policy-condition>
            //        <type>boolean</type>
            //        <operator>AND</operator>
            //        <condition1>
            //            <type>leaf</type>
            //            <display-name>Average Response Time (ms) Baseline Condition</display-name>
            //            <condition-value-type>BASELINE_STANDARD_DEVIATION</condition-value-type>
            //            <condition-value>3.0</condition-value>
            //            <operator>GREATER_THAN</operator>
            //            <condition-expression/>
            //            <use-active-baseline>true</use-active-baseline>
            //            <trigger-on-no-data>false</trigger-on-no-data>
            //            <metric-expression>
            //                <type>leaf</type>
            //                <function-type>VALUE</function-type>
            //                <value>0</value>
            //                <is-literal-expression>false</is-literal-expression>
            //                <display-name>null</display-name>
            //                <metric-definition>
            //                    <type>LOGICAL_METRIC</type>
            //                    <logical-metric-name>Average Response Time (ms)</logical-metric-name>
            //                </metric-definition>
            //            </metric-expression>
            //        </condition1>
            //        <condition2>
            //            <type>leaf</type>
            //            <display-name>Calls per Minute Condition</display-name>
            //            <condition-value-type>ABSOLUTE</condition-value-type>
            //            <condition-value>50.0</condition-value>
            //            <operator>GREATER_THAN</operator>
            //            <condition-expression/>
            //            <use-active-baseline>false</use-active-baseline>
            //            <trigger-on-no-data>false</trigger-on-no-data>
            //            <metric-expression>
            //                <type>leaf</type>
            //                <function-type>VALUE</function-type>
            //                <value>0</value>
            //                <is-literal-expression>false</is-literal-expression>
            //                <display-name>null</display-name>
            //                <metric-definition>
            //                    <type>LOGICAL_METRIC</type>
            //                    <logical-metric-name>Calls per Minute</logical-metric-name>
            //                </metric-definition>
            //            </metric-expression>
            //        </condition2>
            //    </policy-condition>
            //</critical-execution-criteria>            

            // Critical
            XmlNode criticalExecutionCriteriaXmlNode = healthRuleConfigurationNode.SelectSingleNode("critical-execution-criteria");
            if (criticalExecutionCriteriaXmlNode != null)
            {
                healthRule.CriticalAggregateType = getStringValueFromXmlNode(criticalExecutionCriteriaXmlNode.SelectSingleNode("entity-aggregation-scope/type"));

                XmlNode firstCondition = criticalExecutionCriteriaXmlNode.SelectSingleNode("policy-condition");
                XmlNodeList condition1sXmlNodeList = criticalExecutionCriteriaXmlNode.SelectNodes("policy-condition//condition1");
                XmlNodeList condition2sXmlNodeList = criticalExecutionCriteriaXmlNode.SelectNodes("policy-condition//condition2");

                List<XmlNode> conditionsList = new List<XmlNode>();
                if (condition1sXmlNodeList.Count == 0)
                {
                    healthRule.CriticalEntityConditionType = "AND";
                    conditionsList.Add(firstCondition);
                }
                else
                {
                    healthRule.CriticalEntityConditionType = getStringValueFromXmlNode(criticalExecutionCriteriaXmlNode.SelectSingleNode("policy-condition/operator"));
                    foreach (XmlNode xmlNode in condition1sXmlNodeList)
                    {
                        conditionsList.Add(xmlNode);
                    }
                    conditionsList.Add(condition2sXmlNodeList[condition2sXmlNodeList.Count - 1]);
                }

                healthRule.CriticalNumConditions = conditionsList.Count;

                int i = 1;
                foreach (XmlNode conditionXmlNode in conditionsList)
                {
                    healthRule.GetType().GetProperty(String.Format("Crit{0}Name", i)).SetValue(healthRule, getStringValueFromXmlNode(conditionXmlNode.SelectSingleNode("display-name")), null);
                    healthRule.GetType().GetProperty(String.Format("Crit{0}Type", i)).SetValue(healthRule, getStringValueFromXmlNode(conditionXmlNode.SelectSingleNode("condition-value-type")), null);
                    healthRule.GetType().GetProperty(String.Format("Crit{0}Value", i)).SetValue(healthRule, getLongValueFromXmlNode(conditionXmlNode.SelectSingleNode("condition-value")), null);
                    healthRule.GetType().GetProperty(String.Format("Crit{0}Operator", i)).SetValue(healthRule, getStringValueFromXmlNode(conditionXmlNode.SelectSingleNode("operator")), null);
                    healthRule.GetType().GetProperty(String.Format("Crit{0}Expression", i)).SetValue(healthRule, getStringValueFromXmlNode(conditionXmlNode.SelectSingleNode("condition-expression")), null);
                    if (getBoolValueFromXmlNode(conditionXmlNode.SelectSingleNode("use-active-baseline")) == true)
                    {
                        healthRule.GetType().GetProperty(String.Format("Crit{0}BaselineUsed", i)).SetValue(healthRule, "Default Baseline", null);
                    }
                    else
                    {
                        healthRule.GetType().GetProperty(String.Format("Crit{0}BaselineUsed", i)).SetValue(healthRule, getStringValueFromXmlNode(conditionXmlNode.SelectSingleNode("metric-baseline/name")), null);
                    }
                    healthRule.GetType().GetProperty(String.Format("Crit{0}TriggerOnNoData", i)).SetValue(healthRule, getBoolValueFromXmlNode(conditionXmlNode.SelectSingleNode("trigger-on-no-data")), null);
                    healthRule.GetType().GetProperty(String.Format("Crit{0}MetricName", i)).SetValue(healthRule, getStringValueFromXmlNode(conditionXmlNode.SelectSingleNode("metric-expression/metric-definition/logical-metric-name")), null);
                    healthRule.GetType().GetProperty(String.Format("Crit{0}MetricFunction", i)).SetValue(healthRule, getStringValueFromXmlNode(conditionXmlNode.SelectSingleNode("metric-expression/function-type")), null);
                    healthRule.GetType().GetProperty(String.Format("Crit{0}MetricExpressionConfig", i)).SetValue(healthRule, makeXMLFormattedAndIndented(conditionXmlNode.SelectSingleNode("metric-expression")), null);

                    i++;
                    if (i > 5) break;
                }
            }

            // Warning
            XmlNode warningExecutionCriteriaXmlNode = healthRuleConfigurationNode.SelectSingleNode("warning-execution-criteria");
            if (warningExecutionCriteriaXmlNode != null)
            {
                healthRule.WarningAggregateType = getStringValueFromXmlNode(warningExecutionCriteriaXmlNode.SelectSingleNode("entity-aggregation-scope/type"));

                XmlNode firstCondition = warningExecutionCriteriaXmlNode.SelectSingleNode("policy-condition");
                XmlNodeList condition1sXmlNodeList = warningExecutionCriteriaXmlNode.SelectNodes("policy-condition//condition1");
                XmlNodeList condition2sXmlNodeList = warningExecutionCriteriaXmlNode.SelectNodes("policy-condition//condition2");

                List<XmlNode> conditionsList = new List<XmlNode>();
                if (condition1sXmlNodeList.Count == 0)
                {
                    healthRule.WarningEntityConditionType = "AND";
                    conditionsList.Add(firstCondition);
                }
                else
                {
                    healthRule.WarningEntityConditionType = getStringValueFromXmlNode(warningExecutionCriteriaXmlNode.SelectSingleNode("policy-condition/operator"));
                    foreach (XmlNode xmlNode in condition1sXmlNodeList)
                    {
                        conditionsList.Add(xmlNode);
                    }
                    conditionsList.Add(condition2sXmlNodeList[condition2sXmlNodeList.Count - 1]);
                }

                healthRule.WarningNumConditions = conditionsList.Count;

                int i = 1;
                foreach (XmlNode conditionXmlNode in conditionsList)
                {
                    healthRule.GetType().GetProperty(String.Format("Warn{0}Name", i)).SetValue(healthRule, getStringValueFromXmlNode(conditionXmlNode.SelectSingleNode("display-name")), null);
                    healthRule.GetType().GetProperty(String.Format("Warn{0}Type", i)).SetValue(healthRule, getStringValueFromXmlNode(conditionXmlNode.SelectSingleNode("condition-value-type")), null);
                    healthRule.GetType().GetProperty(String.Format("Warn{0}Value", i)).SetValue(healthRule, getLongValueFromXmlNode(conditionXmlNode.SelectSingleNode("condition-value")), null);
                    healthRule.GetType().GetProperty(String.Format("Warn{0}Operator", i)).SetValue(healthRule, getStringValueFromXmlNode(conditionXmlNode.SelectSingleNode("operator")), null);
                    healthRule.GetType().GetProperty(String.Format("Warn{0}Expression", i)).SetValue(healthRule, getStringValueFromXmlNode(conditionXmlNode.SelectSingleNode("condition-expression")), null);
                    if (getBoolValueFromXmlNode(conditionXmlNode.SelectSingleNode("use-active-baseline")) == true)
                    {
                        healthRule.GetType().GetProperty(String.Format("Warn{0}BaselineUsed", i)).SetValue(healthRule, "Default Baseline", null);
                    }
                    else
                    {
                        healthRule.GetType().GetProperty(String.Format("Warn{0}BaselineUsed", i)).SetValue(healthRule, getStringValueFromXmlNode(conditionXmlNode.SelectSingleNode("metric-baseline/name")), null);
                    }
                    healthRule.GetType().GetProperty(String.Format("Warn{0}TriggerOnNoData", i)).SetValue(healthRule, getBoolValueFromXmlNode(conditionXmlNode.SelectSingleNode("trigger-on-no-data")), null);
                    healthRule.GetType().GetProperty(String.Format("Warn{0}MetricName", i)).SetValue(healthRule, getStringValueFromXmlNode(conditionXmlNode.SelectSingleNode("metric-expression/metric-definition/logical-metric-name")), null);
                    healthRule.GetType().GetProperty(String.Format("Warn{0}MetricFunction", i)).SetValue(healthRule, getStringValueFromXmlNode(conditionXmlNode.SelectSingleNode("metric-expression/function-type")), null);
                    healthRule.GetType().GetProperty(String.Format("Warn{0}MetricExpressionConfig", i)).SetValue(healthRule, makeXMLFormattedAndIndented(conditionXmlNode.SelectSingleNode("metric-expression")), null);

                    i++;
                    if (i > 5) break;
                }
            }

            return healthRule;
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
