using AppDynamics.Dexter.ReportObjects;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class IndexApplicationConfigurationComparison : JobStepIndexBase
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

                if (jobConfiguration.Target.Count(t => t.Type == APPLICATION_TYPE_APM) == 0)
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
                        logger.Warn("Unable to find reference target {0}, will compare against default template", jobConfiguration.Input.ConfigurationComparisonReferenceCriteria);
                        loggerConsole.Warn("Unable to find reference target {0}, will compare against default template", jobConfiguration.Input.ConfigurationComparisonReferenceCriteria);

                        compareAgainstTemplateConfiguration = true;
                    }
                    else
                    {
                        jobConfiguration.Input.ConfigurationComparisonReferenceCriteria.ApplicationID = jobTargetReferenceApp.ApplicationID;
                    }
                }
                if (compareAgainstTemplateConfiguration == true)
                {
                    // Add this target to the list of targets to unwrap
                    jobConfiguration.Target.Add(jobConfiguration.Input.ConfigurationComparisonReferenceCriteria);
                }

                logger.Info("Comparing with target {0}", jobConfiguration.Input.ConfigurationComparisonReferenceCriteria);
                loggerConsole.Info("Comparing with target {0}", jobConfiguration.Input.ConfigurationComparisonReferenceCriteria);

                List<ConfigurationDifference> configurationDifferencesListAll = new List<ConfigurationDifference>(10240 * jobConfiguration.Target.Count);

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

                    stepTimingTarget.NumEntities = 1;

                    try
                    {
                        this.DisplayJobTargetStartingStatus(jobConfiguration, jobTarget, i + 1);

                        #region Skip the reference Application

                        // Skip itself
                        if (jobConfiguration.Input.ConfigurationComparisonReferenceCriteria.Controller == jobTarget.Controller &&
                            jobConfiguration.Input.ConfigurationComparisonReferenceCriteria.Application == jobTarget.Application)
                        {
                            continue;
                        }

                        #endregion

                        List<ConfigurationDifference> configurationDifferencesList = new List<ConfigurationDifference>(10240);

                        #region Controller Settings

                        loggerConsole.Info("Controller Settings");

                        List<ControllerSetting> controllerSettingsListReference = FileIOHelper.ReadListFromCSVFile<ControllerSetting>(FilePathMap.ControllerSettingsIndexFilePath(jobConfiguration.Input.ConfigurationComparisonReferenceCriteria), new ControllerSettingReportMap());
                        List<ControllerSetting> controllerSettingsListDifference = FileIOHelper.ReadListFromCSVFile<ControllerSetting>(FilePathMap.ControllerSettingsIndexFilePath(jobTarget), new ControllerSettingReportMap());

                        configurationDifferencesList.AddRange(compareListOfEntities(jobConfiguration.Input.ConfigurationComparisonReferenceCriteria, jobTarget, controllerSettingsListReference, controllerSettingsListDifference));

                        #endregion

                        #region Application Summary

                        loggerConsole.Info("Application Summary");

                        List<EntityApplicationConfiguration> applicationConfigurationsListReference = FileIOHelper.ReadListFromCSVFile<EntityApplicationConfiguration>(FilePathMap.ApplicationConfigurationIndexFilePath(jobConfiguration.Input.ConfigurationComparisonReferenceCriteria), new EntityApplicationConfigurationReportMap());
                        List<EntityApplicationConfiguration> applicationConfigurationsListDifference = FileIOHelper.ReadListFromCSVFile<EntityApplicationConfiguration>(FilePathMap.ApplicationConfigurationIndexFilePath(jobTarget), new EntityApplicationConfigurationReportMap());

                        if (applicationConfigurationsListReference != null && applicationConfigurationsListReference.Count > 0 &&
                            applicationConfigurationsListDifference != null && applicationConfigurationsListDifference.Count > 0)
                        {
                            configurationDifferencesList.AddRange(compareTwoEntities(jobConfiguration.Input.ConfigurationComparisonReferenceCriteria, jobTarget, applicationConfigurationsListReference[0], applicationConfigurationsListDifference[0]));
                        }

                        #endregion

                        #region Business Transaction Detection Rules

                        loggerConsole.Info("Business Transaction Detection Rules");

                        List<BusinessTransactionDiscoveryRule> businessTransactionDiscoveryRulesListReference = FileIOHelper.ReadListFromCSVFile<BusinessTransactionDiscoveryRule>(FilePathMap.BusinessTransactionDiscoveryRulesIndexFilePath(jobConfiguration.Input.ConfigurationComparisonReferenceCriteria), new BusinessTransactionDiscoveryRuleReportMap());
                        List<BusinessTransactionDiscoveryRule> businessTransactionDiscoveryRulesListDifference = FileIOHelper.ReadListFromCSVFile<BusinessTransactionDiscoveryRule>(FilePathMap.BusinessTransactionDiscoveryRulesIndexFilePath(jobTarget), new BusinessTransactionDiscoveryRuleReportMap());

                        configurationDifferencesList.AddRange(compareListOfEntities(jobConfiguration.Input.ConfigurationComparisonReferenceCriteria, jobTarget, businessTransactionDiscoveryRulesListReference, businessTransactionDiscoveryRulesListDifference));

                        #endregion

                        #region Business Transaction Rules

                        loggerConsole.Info("Business Transaction Include and Exclude Rules");

                        List<BusinessTransactionEntryRule> businessTransactionEntryRulesListReference = FileIOHelper.ReadListFromCSVFile<BusinessTransactionEntryRule>(FilePathMap.BusinessTransactionEntryRulesIndexFilePath(jobConfiguration.Input.ConfigurationComparisonReferenceCriteria), new BusinessTransactionEntryRuleReportMap());
                        List<BusinessTransactionEntryRule> businessTransactionEntryRulesListDifference = FileIOHelper.ReadListFromCSVFile<BusinessTransactionEntryRule>(FilePathMap.BusinessTransactionEntryRulesIndexFilePath(jobTarget), new BusinessTransactionEntryRuleReportMap());

                        configurationDifferencesList.AddRange(compareListOfEntities(jobConfiguration.Input.ConfigurationComparisonReferenceCriteria, jobTarget, businessTransactionEntryRulesListReference, businessTransactionEntryRulesListDifference));

                        #endregion

                        #region MDS/Config 2.0 Scopes, BT Detection and BT Rules

                        loggerConsole.Info("Business Transaction Include and Exclude Rules - MDS 2.0");

                        List<BusinessTransactionEntryScope> businessTransactionEntryScopeListReference = FileIOHelper.ReadListFromCSVFile<BusinessTransactionEntryScope>(FilePathMap.BusinessTransactionEntryScopesIndexFilePath(jobConfiguration.Input.ConfigurationComparisonReferenceCriteria), new BusinessTransactionEntryRuleScopeReportMap());
                        List<BusinessTransactionEntryScope> businessTransactionEntryScopeListDifference = FileIOHelper.ReadListFromCSVFile<BusinessTransactionEntryScope>(FilePathMap.BusinessTransactionEntryScopesIndexFilePath(jobTarget), new BusinessTransactionEntryRuleScopeReportMap());

                        configurationDifferencesList.AddRange(compareListOfEntities(jobConfiguration.Input.ConfigurationComparisonReferenceCriteria, jobTarget, businessTransactionEntryScopeListReference, businessTransactionEntryScopeListDifference));

                        List<BusinessTransactionEntryRule20> businessTransactionEntryRules20ListReference = FileIOHelper.ReadListFromCSVFile<BusinessTransactionEntryRule20>(FilePathMap.BusinessTransactionEntryRules20IndexFilePath(jobConfiguration.Input.ConfigurationComparisonReferenceCriteria), new BusinessTransactionEntryRule20ReportMap());
                        List<BusinessTransactionEntryRule20> businessTransactionEntryRules20ListDifference = FileIOHelper.ReadListFromCSVFile<BusinessTransactionEntryRule20>(FilePathMap.BusinessTransactionEntryRules20IndexFilePath(jobTarget), new BusinessTransactionEntryRule20ReportMap());

                        configurationDifferencesList.AddRange(compareListOfEntities(jobConfiguration.Input.ConfigurationComparisonReferenceCriteria, jobTarget, businessTransactionEntryRules20ListReference, businessTransactionEntryRules20ListDifference));

                        List<BusinessTransactionDiscoveryRule20> businessTransactionDiscoveryRule20ListReference = FileIOHelper.ReadListFromCSVFile<BusinessTransactionDiscoveryRule20>(FilePathMap.BusinessTransactionDiscoveryRules20IndexFilePath(jobConfiguration.Input.ConfigurationComparisonReferenceCriteria), new BusinessTransactionDiscoveryRule20ReportMap());
                        List<BusinessTransactionDiscoveryRule20> businessTransactionDiscoveryRule20ListDifference = FileIOHelper.ReadListFromCSVFile<BusinessTransactionDiscoveryRule20>(FilePathMap.BusinessTransactionDiscoveryRules20IndexFilePath(jobTarget), new BusinessTransactionDiscoveryRule20ReportMap());

                        configurationDifferencesList.AddRange(compareListOfEntities(jobConfiguration.Input.ConfigurationComparisonReferenceCriteria, jobTarget, businessTransactionDiscoveryRule20ListReference, businessTransactionDiscoveryRule20ListDifference));

                        #endregion

                        #region Backend Rules 

                        loggerConsole.Info("Backend Detection Rules");

                        List<BackendDiscoveryRule> backendDiscoveryRulesListReference = FileIOHelper.ReadListFromCSVFile<BackendDiscoveryRule>(FilePathMap.BackendDiscoveryRulesIndexFilePath(jobConfiguration.Input.ConfigurationComparisonReferenceCriteria), new BackendDiscoveryRuleReportMap());
                        List<BackendDiscoveryRule> backendDiscoveryRulesListDifference = FileIOHelper.ReadListFromCSVFile<BackendDiscoveryRule>(FilePathMap.BackendDiscoveryRulesIndexFilePath(jobTarget), new BackendDiscoveryRuleReportMap());

                        configurationDifferencesList.AddRange(compareListOfEntities(jobConfiguration.Input.ConfigurationComparisonReferenceCriteria, jobTarget, backendDiscoveryRulesListReference, backendDiscoveryRulesListDifference));

                        #endregion

                        #region Custom Exit Rules 

                        loggerConsole.Info("Custom Exit Rules");

                        List<CustomExitRule> customExitRulesListReference = FileIOHelper.ReadListFromCSVFile<CustomExitRule>(FilePathMap.CustomExitRulesIndexFilePath(jobConfiguration.Input.ConfigurationComparisonReferenceCriteria), new CustomExitRuleReportMap());
                        List<CustomExitRule> customExitRulesListDifference = FileIOHelper.ReadListFromCSVFile<CustomExitRule>(FilePathMap.CustomExitRulesIndexFilePath(jobTarget), new CustomExitRuleReportMap());

                        configurationDifferencesList.AddRange(compareListOfEntities(jobConfiguration.Input.ConfigurationComparisonReferenceCriteria, jobTarget, customExitRulesListReference, customExitRulesListDifference));

                        #endregion

                        #region Agent Configuration Properties 

                        loggerConsole.Info("Agent Configuration Properties");

                        List<AgentConfigurationProperty> agentConfigurationPropertiesListReference = FileIOHelper.ReadListFromCSVFile<AgentConfigurationProperty>(FilePathMap.AgentConfigurationPropertiesIndexFilePath(jobConfiguration.Input.ConfigurationComparisonReferenceCriteria), new AgentConfigurationPropertyReportMap());
                        List<AgentConfigurationProperty> agentConfigurationPropertiesListDifference = FileIOHelper.ReadListFromCSVFile<AgentConfigurationProperty>(FilePathMap.AgentConfigurationPropertiesIndexFilePath(jobTarget), new AgentConfigurationPropertyReportMap());

                        configurationDifferencesList.AddRange(compareListOfEntities(jobConfiguration.Input.ConfigurationComparisonReferenceCriteria, jobTarget, agentConfigurationPropertiesListReference, agentConfigurationPropertiesListDifference));

                        #endregion

                        #region Information Point Rules

                        loggerConsole.Info("Information Point Rules");

                        List<InformationPointRule> informationPointRulesListReference = FileIOHelper.ReadListFromCSVFile<InformationPointRule>(FilePathMap.InformationPointRulesIndexFilePath(jobConfiguration.Input.ConfigurationComparisonReferenceCriteria), new InformationPointRuleReportMap());
                        List<InformationPointRule> informationPointRulesListDifference = FileIOHelper.ReadListFromCSVFile<InformationPointRule>(FilePathMap.InformationPointRulesIndexFilePath(jobTarget), new InformationPointRuleReportMap());

                        configurationDifferencesList.AddRange(compareListOfEntities(jobConfiguration.Input.ConfigurationComparisonReferenceCriteria, jobTarget, informationPointRulesListReference, informationPointRulesListDifference));

                        #endregion

                        #region Detected Business Transaction and Assigned Data Collectors

                        loggerConsole.Info("Detected Business Transaction and Assigned Data Collectors");

                        List<EntityBusinessTransactionConfiguration> entityBusinessTransactionConfigurationsListReference = FileIOHelper.ReadListFromCSVFile<EntityBusinessTransactionConfiguration>(FilePathMap.EntityBusinessTransactionConfigurationsIndexFilePath(jobConfiguration.Input.ConfigurationComparisonReferenceCriteria), new EntityBusinessTransactionConfigurationReportMap());
                        List<EntityBusinessTransactionConfiguration> entityBusinessTransactionConfigurationsListDifference = FileIOHelper.ReadListFromCSVFile<EntityBusinessTransactionConfiguration>(FilePathMap.EntityBusinessTransactionConfigurationsIndexFilePath(jobTarget), new EntityBusinessTransactionConfigurationReportMap());

                        configurationDifferencesList.AddRange(compareListOfEntities(jobConfiguration.Input.ConfigurationComparisonReferenceCriteria, jobTarget, entityBusinessTransactionConfigurationsListReference, entityBusinessTransactionConfigurationsListDifference));

                        #endregion

                        #region Tier Settings

                        loggerConsole.Info("Tier Settings");

                        List<EntityTierConfiguration> entityTierConfigurationsListReference = FileIOHelper.ReadListFromCSVFile<EntityTierConfiguration>(FilePathMap.EntityTierConfigurationsIndexFilePath(jobConfiguration.Input.ConfigurationComparisonReferenceCriteria), new EntityTierConfigurationReportMap());
                        List<EntityTierConfiguration> entityTierConfigurationsListDifference = FileIOHelper.ReadListFromCSVFile<EntityTierConfiguration>(FilePathMap.EntityTierConfigurationsIndexFilePath(jobTarget), new EntityTierConfigurationReportMap());

                        configurationDifferencesList.AddRange(compareListOfEntities(jobConfiguration.Input.ConfigurationComparisonReferenceCriteria, jobTarget, entityTierConfigurationsListReference, entityTierConfigurationsListDifference));

                        #endregion

                        #region Data Collectors

                        loggerConsole.Info("Data Collectors");

                        List<MethodInvocationDataCollector> methodInvocationDataCollectorsListReference = FileIOHelper.ReadListFromCSVFile<MethodInvocationDataCollector>(FilePathMap.MethodInvocationDataCollectorsIndexFilePath(jobConfiguration.Input.ConfigurationComparisonReferenceCriteria), new MethodInvocationDataCollectorReportMap());
                        List<MethodInvocationDataCollector> methodInvocationDataCollectorsListDifference = FileIOHelper.ReadListFromCSVFile<MethodInvocationDataCollector>(FilePathMap.MethodInvocationDataCollectorsIndexFilePath(jobTarget), new MethodInvocationDataCollectorReportMap());

                        configurationDifferencesList.AddRange(compareListOfEntities(jobConfiguration.Input.ConfigurationComparisonReferenceCriteria, jobTarget, methodInvocationDataCollectorsListReference, methodInvocationDataCollectorsListDifference));

                        List<HTTPDataCollector> httpDataCollectorsListReference = FileIOHelper.ReadListFromCSVFile<HTTPDataCollector>(FilePathMap.HttpDataCollectorsIndexFilePath(jobConfiguration.Input.ConfigurationComparisonReferenceCriteria), new HTTPDataCollectorReportMap());
                        List<HTTPDataCollector> httpDataCollectorsListDifference = FileIOHelper.ReadListFromCSVFile<HTTPDataCollector>(FilePathMap.HttpDataCollectorsIndexFilePath(jobTarget), new HTTPDataCollectorReportMap());

                        configurationDifferencesList.AddRange(compareListOfEntities(jobConfiguration.Input.ConfigurationComparisonReferenceCriteria, jobTarget, httpDataCollectorsListReference, httpDataCollectorsListDifference));

                        #endregion

                        #region Call Graph Settings

                        loggerConsole.Info("Call Graph Settings");

                        List<AgentCallGraphSetting> agentCallGraphSettingCollectorsListReference = FileIOHelper.ReadListFromCSVFile<AgentCallGraphSetting>(FilePathMap.AgentCallGraphSettingsIndexFilePath(jobConfiguration.Input.ConfigurationComparisonReferenceCriteria), new AgentCallGraphSettingReportMap());
                        List<AgentCallGraphSetting> agentCallGraphSettingCollectorsListDifference = FileIOHelper.ReadListFromCSVFile<AgentCallGraphSetting>(FilePathMap.AgentCallGraphSettingsIndexFilePath(jobTarget), new AgentCallGraphSettingReportMap());

                        configurationDifferencesList.AddRange(compareListOfEntities(jobConfiguration.Input.ConfigurationComparisonReferenceCriteria, jobTarget, agentCallGraphSettingCollectorsListReference, agentCallGraphSettingCollectorsListDifference));

                        #endregion

                        #region Health Rules

                        loggerConsole.Info("Health Rules");

                        List<HealthRule> healthRulesListReference = FileIOHelper.ReadListFromCSVFile<HealthRule>(FilePathMap.HealthRulesIndexFilePath(jobConfiguration.Input.ConfigurationComparisonReferenceCriteria), new HealthRuleReportMap());
                        List<HealthRule> healthRulesListDifference = FileIOHelper.ReadListFromCSVFile<HealthRule>(FilePathMap.HealthRulesIndexFilePath(jobTarget), new HealthRuleReportMap());

                        configurationDifferencesList.AddRange(compareListOfEntities(jobConfiguration.Input.ConfigurationComparisonReferenceCriteria, jobTarget, healthRulesListReference, healthRulesListDifference));

                        #endregion

                        #region Save data for this comparison

                        FileIOHelper.WriteListToCSVFile(configurationDifferencesList, new ConfigurationDifferenceReportMap(), FilePathMap.ConfigurationComparisonIndexFilePath(jobTarget));

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + configurationDifferencesList.Count;

                        #endregion

                        #region Combine All for Report CSV

                        // If it is the first one, clear out the combined folder
                        if (reportFolderCleaned == false)
                        {
                            FileIOHelper.DeleteFolder(FilePathMap.ConfigurationComparisonReportFolderPath());
                            Thread.Sleep(1000);
                            FileIOHelper.CreateFolder(FilePathMap.ConfigurationComparisonReportFolderPath());
                            reportFolderCleaned = true;
                        }

                        // Append all the individual application files into one
                        if (File.Exists(FilePathMap.ConfigurationComparisonIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.ConfigurationComparisonIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.ConfigurationComparisonReportFilePath(), FilePathMap.ConfigurationComparisonIndexFilePath(jobTarget));
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
                loggerConsole.Trace("Skipping comparison of configuration");
            }
            return (jobConfiguration.Input.Configuration == true);
        }

        private List<ConfigurationDifference> compareListOfEntities<T>(
            JobTarget jobTargetReference,
            JobTarget jobTargetDifference,
            List<T> configEntitiesListReference, 
            List<T> configEntitiesListDifference) where T : ConfigurationEntityBase
        {
            List<ConfigurationDifference> configurationDifferencesList = new List<ConfigurationDifference>(256);

            if (configEntitiesListReference == null) configEntitiesListReference = new List<T>();
            if (configEntitiesListDifference == null) configEntitiesListDifference = new List<T>();
            List<T> configEntitiesListExtra = new List<T>(256);

            // Here is what we get in the reference and difference lists
            // List             List
            // Reference        Difference      Action
            // AAA              AAA             Compare items
            // BBB                              item in Difference is MISSING
            //                  CCC             item in Difference is EXTRA

            // First loop through Reference list looking for matches
            // Remove each item from the reference list until there are no more left
            while (configEntitiesListReference.Count > 0)
            {
                ConfigurationEntityBase configEntityReference = configEntitiesListReference[0];
                configEntitiesListReference.RemoveAt(0);

                // Find match for this configuration entity
                int configEntityDifferenceIndex = configEntitiesListDifference.FindIndex(c => String.Compare(c.EntityIdentifier, configEntityReference.EntityIdentifier, true) == 0);
                if (configEntityDifferenceIndex < 0)
                {
                    // No match. This must be entity BBB, where item in Difference is MISSING
                    ConfigurationDifference configDifference = fillConfigurationDifference(
                        jobTargetReference,
                        jobTargetDifference,
                        configEntityReference,
                        DIFFERENCE_MISSING,
                        PROPERTY_ENTIRE_OBJECT,
                        configEntityReference.ToString(),
                        String.Empty);

                    configurationDifferencesList.Add(configDifference);
                }
                else
                {
                    // Found matching entity AAA. Let's compare them against each other
                    ConfigurationEntityBase configEntityDifference = configEntitiesListDifference[configEntityDifferenceIndex];
                    configEntitiesListDifference.RemoveAt(configEntityDifferenceIndex);

                    configurationDifferencesList.AddRange(compareTwoEntities(jobTargetReference, jobTargetDifference, configEntityReference, configEntityDifference));
                }
            }

            // Whatever is left in Difference list must be like entity CCC, therefore they are all EXTRA
            if (configEntitiesListDifference.Count > 0)
            {
                for (int i = 0; i < configEntitiesListDifference.Count; i++)
                {
                    ConfigurationEntityBase configEntityDifference = configEntitiesListDifference[i];

                    ConfigurationDifference configDifference = fillConfigurationDifference(
                        jobTargetReference,
                        jobTargetDifference,
                        configEntityDifference,
                        DIFFERENCE_EXTRA,
                        PROPERTY_ENTIRE_OBJECT,
                        String.Empty,
                        configEntityDifference.ToString());

                    configurationDifferencesList.Add(configDifference);
                }
            }

            return configurationDifferencesList;
        }

        private List<ConfigurationDifference> compareTwoEntities(
            JobTarget jobTargetReference,
            JobTarget jobTargetDifference,
            ConfigurationEntityBase configEntityReference,
            ConfigurationEntityBase configEntityDifference)
        {
            List<ConfigurationDifference> configurationDifferencesList = new List<ConfigurationDifference>(16);

            // Look through all the properties
            Type configEntityType = configEntityReference.GetType();
            foreach (PropertyInfo prop in configEntityType.GetProperties())
            {
                // Only care about the ones marked for comparison with the superawesome custom attribute
                object[] attributes = prop.GetCustomAttributes(typeof(FieldComparisonAttribute), true);
                if (attributes.Length > 0)
                {
                    FieldComparisonAttribute fieldComparisonAttribute = ((FieldComparisonAttribute)attributes[0]);

                    object propValueReferenceObject = prop.GetValue(configEntityReference);
                    object propValueDifferenceObject = prop.GetValue(configEntityDifference);

                    switch (fieldComparisonAttribute.FieldComparison)
                    {
                        case FieldComparisonType.ValueComparison:
                            // Compare values using the right datatype
                            if (propValueReferenceObject is System.String)
                            {
                                string propValueReferenceString = (string)propValueReferenceObject;
                                string propValueDifferenceString = (string)propValueDifferenceObject;

                                if (String.Compare(propValueReferenceString, propValueDifferenceString, false) != 0)
                                {
                                    ConfigurationDifference configDifference = fillConfigurationDifference(
                                        jobTargetReference,
                                        jobTargetDifference,
                                        configEntityReference,
                                        DIFFERENCE_DIFFERENT,
                                        prop.Name,
                                        propValueReferenceString,
                                        propValueDifferenceString);

                                    configurationDifferencesList.Add(configDifference);
                                }
                            }
                            else if (propValueReferenceObject is System.Int32)
                            {
                                int propValueReferenceInt = (int)propValueReferenceObject;
                                int propValueDifferenceInt = (int)propValueDifferenceObject;

                                if (propValueReferenceInt != propValueDifferenceInt)
                                {
                                    ConfigurationDifference configDifference = fillConfigurationDifference(
                                        jobTargetReference,
                                        jobTargetDifference,
                                        configEntityReference,
                                        DIFFERENCE_DIFFERENT,
                                        prop.Name,
                                        propValueReferenceInt.ToString(),
                                        propValueDifferenceInt.ToString());

                                    configurationDifferencesList.Add(configDifference);
                                }
                            }
                            else if (propValueReferenceObject is System.Int64)
                            {
                                long propValueReferenceLong = (long)propValueReferenceObject;
                                long propValueDifferenceLong = (long)propValueDifferenceObject;

                                if (propValueReferenceLong != propValueDifferenceLong)
                                {
                                    ConfigurationDifference configDifference = fillConfigurationDifference(
                                        jobTargetReference,
                                        jobTargetDifference,
                                        configEntityReference,
                                        DIFFERENCE_DIFFERENT,
                                        prop.Name,
                                        propValueReferenceLong.ToString(),
                                        propValueDifferenceLong.ToString());

                                    configurationDifferencesList.Add(configDifference);
                                }
                            }
                            else if (propValueReferenceObject is System.Boolean)
                            {
                                bool propValueReferenceBoolean = (bool)propValueReferenceObject;
                                bool propValueDifferenceBoolean = (bool)propValueDifferenceObject;

                                if (propValueReferenceBoolean != propValueDifferenceBoolean)
                                {
                                    ConfigurationDifference configDifference = fillConfigurationDifference(
                                        jobTargetReference,
                                        jobTargetDifference,
                                        configEntityReference,
                                        DIFFERENCE_DIFFERENT,
                                        prop.Name,
                                        propValueReferenceBoolean.ToString(),
                                        propValueDifferenceBoolean.ToString());

                                    configurationDifferencesList.Add(configDifference);
                                }
                            }

                            break;

                        case FieldComparisonType.SemicolonMultiLineValueComparison:
                            // These are strings separated by ;\n
                            string propValueReferenceMultiLineString = (string)propValueReferenceObject;
                            string propValueDifferenceMultiLineString = (string)propValueDifferenceObject;

                            configurationDifferencesList.AddRange(compareTwoEntitiesMultiValueStringProperty(jobTargetReference, jobTargetDifference, configEntityReference, configEntityReference, prop.Name, propValueReferenceMultiLineString, propValueDifferenceMultiLineString));

                            break;

                        case FieldComparisonType.XmlValueComparison:
                            // Compare XML tree
                            string propValueReferenceXML = (string)propValueReferenceObject;
                            string propValueDifferenceXML = (string)propValueDifferenceObject;

                            configurationDifferencesList.AddRange(compareTwoEntitiesXMLProperty(jobTargetReference, jobTargetDifference, configEntityReference, configEntityReference, prop.Name, propValueReferenceXML, propValueDifferenceXML));

                            break;

                        case FieldComparisonType.JSONValueComparison:
                            // Compare JSON tree
                            string propValueReferenceJSON = (string)propValueReferenceObject;
                            string propValueDifferenceJSON = (string)propValueDifferenceObject;

                            configurationDifferencesList.AddRange(compareTwoEntitiesJSONProperty(jobTargetReference, jobTargetDifference, configEntityReference, configEntityReference, prop.Name, propValueReferenceJSON, propValueDifferenceJSON));

                            break;

                        default:
                            // Not sure what this value is, skip
                            logger.Warn("Unknown FieldComparisonAttribute.FieldComparison={0}", fieldComparisonAttribute.FieldComparison);

                            break;
                    }
                }
            }

            return configurationDifferencesList;
        }

        private List<ConfigurationDifference> compareTwoEntitiesMultiValueStringProperty(
            JobTarget jobTargetReference,
            JobTarget jobTargetDifference,
            ConfigurationEntityBase configEntityReference,
            ConfigurationEntityBase configEntityDifference,
            string propertyName,
            string propValueReferenceMultiLineString,
            string propValueDifferenceMultiLineString)
        {
            List<ConfigurationDifference> configurationDifferencesList = new List<ConfigurationDifference>(16);

            List<System.String> propValueMultiLineListReference = propValueReferenceMultiLineString.Split(new string[] { ";\n" }, StringSplitOptions.RemoveEmptyEntries).ToList<System.String>();
            propValueMultiLineListReference.Sort();

            List<System.String> propValueMultiLineListDifference = propValueDifferenceMultiLineString.Split(new string[] { ";\n" }, StringSplitOptions.RemoveEmptyEntries).ToList<System.String>();
            propValueMultiLineListDifference.Sort();

            while (propValueMultiLineListReference.Count > 0)
            {
                string propValueReferenceString = propValueMultiLineListReference[0];
                propValueMultiLineListReference.RemoveAt(0);

                // Find match for this configuration entity
                int configEntityDifferenceIndex = propValueMultiLineListDifference.FindIndex(c => String.Compare(c, propValueReferenceString, true) == 0);
                if (configEntityDifferenceIndex < 0)
                {
                    // No match. This must be entity BBB, where item in Difference is MISSING
                    ConfigurationDifference configDifference = fillConfigurationDifference(
                        jobTargetReference,
                        jobTargetDifference,
                        configEntityReference,
                        DIFFERENCE_MISSING,
                        propertyName,
                        propValueReferenceString,
                        String.Empty);

                    configurationDifferencesList.Add(configDifference);
                }
                else
                {
                    // Found matching entity AAA. No need to compare them, by definition they are matching 
                    string propValueDifferenceString = propValueMultiLineListDifference[configEntityDifferenceIndex];
                    propValueMultiLineListDifference.RemoveAt(configEntityDifferenceIndex);
                }
            }

            // Whatever is left in Difference list must be like entity CCC, therefore they are all EXTRA
            if (propValueMultiLineListDifference.Count > 0)
            {
                for (int i = 0; i < propValueMultiLineListDifference.Count; i++)
                {
                    string propValueDifferenceString = propValueMultiLineListDifference[i];

                    ConfigurationDifference configDifference = fillConfigurationDifference(
                        jobTargetReference,
                        jobTargetDifference,
                        configEntityDifference,
                        DIFFERENCE_EXTRA,
                        propertyName,
                        String.Empty,
                        propValueDifferenceString);

                    configurationDifferencesList.Add(configDifference);
                }
            }
            return configurationDifferencesList;
        }


        private List<ConfigurationDifference> compareTwoEntitiesJSONProperty(
            JobTarget jobTargetReference,
            JobTarget jobTargetDifference,
            ConfigurationEntityBase configEntityReference,
            ConfigurationEntityBase configEntityDifference,
            string propertyName,
            string propValueReferenceJSON,
            string propValueDifferenceJSON)
        {
            List<ConfigurationDifference> configurationDifferencesList = new List<ConfigurationDifference>(16);

            // Do a spot check on the strings to see if they are different at all
            if (String.Compare(propValueReferenceJSON, propValueDifferenceJSON, false) == 0)
            {
                // The strings are identical, no need to bother with anything
                return configurationDifferencesList;
            }

            // If hit here, the strings are different. Let's parse them into JSON and compare element by element
            JToken propValueReference = null;
            JToken propValueDifference = null;
            try
            {
                if (propValueReferenceJSON.Length > 0)
                {
                    propValueReference = JToken.Parse(propValueReferenceJSON);
                }
                if (propValueDifferenceJSON.Length > 0)
                {
                    propValueDifference = JToken.Parse(propValueDifferenceJSON);
                }
            }
            catch (JsonReaderException ex)
            {
                logger.Error(ex);
                loggerConsole.Error(ex);
            }

            // Check for success in parsing object from string
            if (propValueReference == null && propValueDifference == null)
                {
                    return configurationDifferencesList;
                }
            else if (propValueReference != null && propValueDifference == null)
            {
                ConfigurationDifference configDifference = fillConfigurationDifference(
                    jobTargetReference,
                    jobTargetDifference,
                    configEntityReference,
                    DIFFERENCE_MISSING,
                    propertyName,
                    propValueReferenceJSON,
                    String.Empty);

                configurationDifferencesList.Add(configDifference);

                return configurationDifferencesList;
            }
            else if (propValueReference == null && propValueDifference != null)
            {
                ConfigurationDifference configDifference = fillConfigurationDifference(
                    jobTargetReference,
                    jobTargetDifference,
                    configEntityDifference,
                    DIFFERENCE_EXTRA,
                    propertyName,
                    String.Empty,
                    propValueDifferenceJSON);

                configurationDifferencesList.Add(configDifference);

                return configurationDifferencesList;
            }

            // If hit this line, we have two perfectly good JSON objects that contain slightly different JSON content
            // Let's get all their properties and look through them each
            Dictionary<string, string> tokenListReference = new Dictionary<string, string>(1024);
            flattenJSONObjectIntoDictionary(propValueReference, tokenListReference);

            Dictionary<string, string> tokenListDifference = new Dictionary<string, string>(1024);
            flattenJSONObjectIntoDictionary(propValueDifference, tokenListDifference);

            while (tokenListReference.Count > 0)
            {
                KeyValuePair<string, string> referenceKVP = tokenListReference.First();
                tokenListReference.Remove(referenceKVP.Key);

                if (tokenListDifference.ContainsKey(referenceKVP.Key) == false)
                {
                    // No match. This must be entity BBB, where item in Difference is MISSING
                    ConfigurationDifference configDifference = fillConfigurationDifference(
                        jobTargetReference,
                        jobTargetDifference,
                        configEntityReference,
                        DIFFERENCE_MISSING,
                        String.Format("{0}/{1}", propertyName, referenceKVP.Key),
                        referenceKVP.Value,
                        String.Empty);

                    configurationDifferencesList.Add(configDifference);
                }
                else
                {
                    // Found matching entity AAA. Let's compare them against each other
                    string propValueReferenceString = referenceKVP.Value;
                    string propValueDifferenceString = tokenListDifference[referenceKVP.Key];
                    tokenListDifference.Remove(referenceKVP.Key);

                    if (String.Compare(propValueReferenceString, propValueDifferenceString, false) != 0)
                    {
                        ConfigurationDifference configDifference = fillConfigurationDifference(
                            jobTargetReference,
                            jobTargetDifference,
                            configEntityReference,
                            DIFFERENCE_DIFFERENT,
                            String.Format("{0}/{1}", propertyName, referenceKVP.Key),
                            propValueReferenceString,
                            propValueDifferenceString);

                        configurationDifferencesList.Add(configDifference);
                    }
                }
            }

            // Whatever is left in Difference list must be like entity CCC, therefore they are all EXTRA
            if (tokenListDifference.Count > 0)
            {
                foreach (KeyValuePair<string, string> differenceKVP in tokenListDifference)
                {
                    ConfigurationDifference configDifference = fillConfigurationDifference(
                        jobTargetReference,
                        jobTargetDifference,
                        configEntityDifference,
                        DIFFERENCE_EXTRA,
                        String.Format("{0}/{1}", propertyName, differenceKVP.Key),
                        String.Empty,
                        differenceKVP.Value);

                    configurationDifferencesList.Add(configDifference);
                }
            }

            return configurationDifferencesList;
        }

        private void flattenJSONObjectIntoDictionary(JToken jTokenRoot, Dictionary<string, string> dictionary)
        {
            // Recursively go through each of the tokens
            foreach (JToken jToken in jTokenRoot)
            {
                if (jToken.Type == JTokenType.Object)
                {
                    // Object
                    // "{ "test1": 123, "test2: 321 }"
                    flattenJSONObjectIntoDictionary(jToken, dictionary);
                }
                else if (jToken.Type == JTokenType.Array)
                {
                    // Array of something
                    // [{ "test1": 123, "test2: 321 }, { "test1": 123, "test2: 321 }]
                    foreach (JToken jTokenArrayMember in jToken)
                    {
                        flattenJSONObjectIntoDictionary(jTokenArrayMember, dictionary);
                    }
                }
                else if (jToken.Type == JTokenType.Property)
                {
                    JToken jTokenProperty = ((JProperty)jToken).Value;

                    if (jTokenProperty is JObject)
                    {
                        if (jTokenProperty.HasValues == false)
                        {
                            // Empty object
                            // "test1" : {} 
                            dictionary.Add(jTokenProperty.Path, jTokenProperty.ToString());
                        }
                        else
                        {
                            // Object
                            // "test1" : { "test1": 123, "test2: 321 }
                            flattenJSONObjectIntoDictionary(jTokenProperty, dictionary);
                        }
                    }
                    else if (jTokenProperty is JArray)
                    {
                        if (jTokenProperty.HasValues == false)
                        {
                            // Empty array
                            // "test1" : [] 
                            dictionary.Add(jTokenProperty.Path, jTokenProperty.ToString());
                        }
                        else
                        {
                            int i = 0;
                            foreach (JToken jTokenArrayMember in jTokenProperty)
                            {
                                if (jTokenArrayMember.Type == JTokenType.Object || jTokenArrayMember.Type == JTokenType.Array)
                                {
                                    // Array of objects
                                    // "test1" : [{ "test1": 123, "test2: 321 }, { "test1": 123, "test2: 321 }]
                                    flattenJSONObjectIntoDictionary(jTokenArrayMember, dictionary);
                                }
                                else
                                {
                                    // Array of values
                                    // "test1" : ["test1", "test2", "test3"]
                                    dictionary.Add(String.Format("{0}/{1}", jTokenProperty.Path, i), jTokenArrayMember.ToString());
                                }
                                i++;
                            }
                        }
                    }
                    else
                    {
                        // Value
                        // "test1" : "value1"
                        dictionary.Add(jToken.Path, ((JProperty)jToken).Value.ToString());
                    }
                }
                else
                {
                    // Value
                    // "test1" : "value1"
                    dictionary.Add(jToken.Path, ((JProperty)jToken).Value.ToString());
                }
            }
        }

        private List<ConfigurationDifference> compareTwoEntitiesXMLProperty(
            JobTarget jobTargetReference,
            JobTarget jobTargetDifference,
            ConfigurationEntityBase configEntityReference,
            ConfigurationEntityBase configEntityDifference,
            string propertyName,
            string propValueReferenceXML,
            string propValueDifferenceXML)
        {
            List<ConfigurationDifference> configurationDifferencesList = new List<ConfigurationDifference>(16);

            // Do a spot check on the strings to see if they are different at all
            if (String.Compare(propValueReferenceXML, propValueDifferenceXML, false) == 0)
            {
                // The strings are identical, no need to bother with anything
                return configurationDifferencesList;
            }

            // If hit here, the strings are different. Let's parse them into XML and compare element by element
            XmlDocument propValueReference = null;
            XmlDocument propValueDifference = null;
            try
            {
                if (propValueReferenceXML.Length > 0)
                {
                    propValueReference = new XmlDocument();
                    propValueReference.LoadXml(propValueReferenceXML);
                }

                if (propValueDifferenceXML.Length > 0)
                {
                    propValueDifference = new XmlDocument();
                    propValueDifference.LoadXml(propValueDifferenceXML);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                loggerConsole.Error(ex);
            }

            // Check for success in parsing object from string
            if (propValueReference == null && propValueDifference == null)
            {
                return configurationDifferencesList;
            }
            else if (propValueReference != null && propValueDifference == null)
            {
                ConfigurationDifference configDifference = fillConfigurationDifference(
                    jobTargetReference,
                    jobTargetDifference,
                    configEntityReference,
                    DIFFERENCE_MISSING,
                    propertyName,
                    propValueReferenceXML,
                    String.Empty);

                configurationDifferencesList.Add(configDifference);

                return configurationDifferencesList;
            }
            else if (propValueReference == null && propValueDifference != null)
            {
                ConfigurationDifference configDifference = fillConfigurationDifference(
                    jobTargetReference,
                    jobTargetDifference,
                    configEntityDifference,
                    DIFFERENCE_EXTRA,
                    propertyName,
                    String.Empty,
                    propValueDifferenceXML);

                configurationDifferencesList.Add(configDifference);

                return configurationDifferencesList;
            }

            // If hit this line, we have two perfectly good XML objects that contain slightly different XML content
            // Let's get all their properties and look through them each
            Dictionary<string, string> tokenListReference = new Dictionary<string, string>(1024);
            flattenXMLObjectIntoDictionary(propValueReference.DocumentElement, "", tokenListReference);

            Dictionary<string, string> tokenListDifference = new Dictionary<string, string>(1024);
            flattenXMLObjectIntoDictionary(propValueDifference.DocumentElement, "", tokenListDifference);

            while (tokenListReference.Count > 0)
            {
                KeyValuePair<string, string> referenceKVP = tokenListReference.First();
                tokenListReference.Remove(referenceKVP.Key);

                if (tokenListDifference.ContainsKey(referenceKVP.Key) == false)
                {
                    // No match. This must be entity BBB, where item in Difference is MISSING
                    ConfigurationDifference configDifference = fillConfigurationDifference(
                        jobTargetReference,
                        jobTargetDifference,
                        configEntityReference,
                        DIFFERENCE_MISSING,
                        String.Format("{0}/{1}", propertyName, referenceKVP.Key),
                        referenceKVP.Value,
                        String.Empty);

                    configurationDifferencesList.Add(configDifference);
                }
                else
                {
                    // Found matching entity AAA. Let's compare them against each other
                    string propValueReferenceString = referenceKVP.Value;
                    string propValueDifferenceString = tokenListDifference[referenceKVP.Key];
                    tokenListDifference.Remove(referenceKVP.Key);

                    if (String.Compare(propValueReferenceString, propValueDifferenceString, false) != 0)
                    {
                        ConfigurationDifference configDifference = fillConfigurationDifference(
                            jobTargetReference,
                            jobTargetDifference,
                            configEntityReference,
                            DIFFERENCE_DIFFERENT,
                            String.Format("{0}/{1}", propertyName, referenceKVP.Key),
                            propValueReferenceString,
                            propValueDifferenceString);

                        configurationDifferencesList.Add(configDifference);
                    }
                }
            }

            // Whatever is left in Difference list must be like entity CCC, therefore they are all EXTRA
            if (tokenListDifference.Count > 0)
            {
                foreach (KeyValuePair<string, string> differenceKVP in tokenListDifference)
                {
                    ConfigurationDifference configDifference = fillConfigurationDifference(
                        jobTargetReference,
                        jobTargetDifference,
                        configEntityDifference,
                        DIFFERENCE_EXTRA,
                        String.Format("{0}/{1}", propertyName, differenceKVP.Key),
                        String.Empty,
                        differenceKVP.Value);

                    configurationDifferencesList.Add(configDifference);
                }
            }

            return configurationDifferencesList;
        }

        private void flattenXMLObjectIntoDictionary(XmlNode xmlNodeRoot, string relativePath, Dictionary<string, string> dictionary)
        {
            if (xmlNodeRoot.ChildNodes != null && xmlNodeRoot.ChildNodes.Count > 0)
            {
                for (int i = 0; i < xmlNodeRoot.ChildNodes.Count; i++)
                {
                    XmlNode xmlNode = xmlNodeRoot.ChildNodes[i];

                    if (xmlNode.Attributes != null)
                    {
                        foreach (XmlAttribute xmlAttribute in xmlNode.Attributes)
                        {
                            dictionary.Add(String.Format("{0}{1}:{2}[{3}]/", relativePath, xmlNode.Name, i, xmlAttribute.Name), xmlAttribute.InnerText);
                        }
                    }

                    if (xmlNode.Value != null)
                    {
                        if (xmlNode.Name == "#text")
                        {
                            dictionary.Add(relativePath, xmlNode.Value);
                        }
                        else
                        {
                            dictionary.Add(String.Format("{0}{1}:{2}/", relativePath, xmlNode.Name, i), xmlNode.Value);
                        }
                    }
                    if (xmlNode.ChildNodes.Count > 0)
                    {
                        flattenXMLObjectIntoDictionary(xmlNode, String.Format("{0}{1}:{2}/", relativePath, xmlNode.Name, i), dictionary);
                    }
                }
            }
            else
            {
                XmlNode xmlNode = xmlNodeRoot;

                if (xmlNode.Attributes != null)
                {
                    foreach (XmlAttribute xmlAttribute in xmlNode.Attributes)
                    {
                        dictionary.Add(String.Format("{0}{1}:{2}[{3}]/", relativePath, xmlNode.Name, 0, xmlAttribute.Name), xmlAttribute.InnerText);
                    }
                }

                if (xmlNode.Value != null)
                {
                    if (xmlNode.Name == "#text")
                    {
                        dictionary.Add(relativePath, xmlNode.Value);
                    }
                    else
                    {
                        dictionary.Add(String.Format("{0}{1}:{2}/", relativePath, xmlNode.Name, 0), xmlNode.Value);
                    }
                }
            }
        }

        private ConfigurationDifference fillConfigurationDifference(
            JobTarget jobTargetReference,
            JobTarget jobTargetDifference,
            ConfigurationEntityBase configEntityBeingCompared,
            string differenceType,
            string propertyName,
            string referenceValue,
            string differenceValue
            )
        {
            ConfigurationDifference configDifference = new ConfigurationDifference();

            configDifference.ReferenceConroller = jobTargetReference.Controller;
            configDifference.ReferenceApp = jobTargetReference.Application;
            configDifference.DifferenceController = jobTargetDifference.Controller;
            configDifference.DifferenceApp = jobTargetDifference.Application;
            configDifference.Difference = differenceType;

            configDifference.EntityName = configEntityBeingCompared.EntityName;
            configDifference.RuleType = configEntityBeingCompared.RuleType;
            configDifference.RuleSubType = configEntityBeingCompared.RuleSubType;
            configDifference.TierName = configEntityBeingCompared.TierName;

            configDifference.Property = propertyName;
            configDifference.ReferenceValue = referenceValue;
            configDifference.DifferenceValue = differenceValue;

            return configDifference;
        }

    }
}
