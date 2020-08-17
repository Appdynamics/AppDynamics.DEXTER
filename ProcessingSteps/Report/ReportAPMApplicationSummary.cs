using AppDynamics.Dexter.ReportObjectMaps;
using AppDynamics.Dexter.ReportObjects;
using Aspose.Words;
using Aspose.Words.Drawing;
using Aspose.Words.Drawing.Charts;
using Aspose.Words.Lists;
using Aspose.Words.Saving;
using Aspose.Words.Tables;
using Aspose.Words.Themes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class ReportAPMApplicationSummary : JobStepReportBase
    {
        #region Constants for report contents


        #endregion

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

            try
            {
                logger.Info("Setting Aspose License");
                Aspose.Words.License license = new Aspose.Words.License();
                license.SetLicense("Aspose.Words.lic");
            }
            catch (Exception ex)
            {
                logger.Error("No Aspose license");
                logger.Error(ex);
                loggerConsole.Warn("No Aspose license, will not generate document");

                return true;
            }

            try
            {
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

                        #region Target step variables

                        int numEntitiesTotal = 0;

                        #endregion

                        #region Preload all the reports that will be filtered by the subsequent entities

                        loggerConsole.Info("Entity Details Data Preloading");

                        List<ControllerSummary> controllerSummariesList = FileIOHelper.ReadListFromCSVFile<ControllerSummary>(FilePathMap.ControllerSummaryIndexFilePath(jobTarget), new ControllerSummaryReportMap());
                        List<APMApplication> applicationsList = FileIOHelper.ReadListFromCSVFile<APMApplication>(FilePathMap.APMApplicationsIndexFilePath(jobTarget), new APMApplicationReportMap());
                        List<APMApplication> applicationsMetricsList = FileIOHelper.ReadListFromCSVFile<APMApplication>(FilePathMap.APMEntitiesFullIndexFilePath(jobTarget, APMApplication.ENTITY_FOLDER), new ApplicationMetricReportMap());

                        List<APMTier> tiersList = FileIOHelper.ReadListFromCSVFile<APMTier>(FilePathMap.APMTiersIndexFilePath(jobTarget), new APMTierReportMap());
                        List<APMTier> tiersMetricsList = FileIOHelper.ReadListFromCSVFile<APMTier>(FilePathMap.APMEntitiesFullIndexFilePath(jobTarget, APMTier.ENTITY_FOLDER), new TierMetricReportMap());

                        List<APMNode> nodesList = FileIOHelper.ReadListFromCSVFile<APMNode>(FilePathMap.APMNodesIndexFilePath(jobTarget), new APMNodeReportMap());
                        List<APMNode> nodesMetricsList = FileIOHelper.ReadListFromCSVFile<APMNode>(FilePathMap.APMEntitiesFullIndexFilePath(jobTarget, APMNode.ENTITY_FOLDER), new NodeMetricReportMap());

                        List<APMBusinessTransaction> businessTransactionsList = FileIOHelper.ReadListFromCSVFile<APMBusinessTransaction>(FilePathMap.APMBusinessTransactionsIndexFilePath(jobTarget), new APMBusinessTransactionReportMap());
                        List<APMBusinessTransaction> businessTransactionsMetricsList = FileIOHelper.ReadListFromCSVFile<APMBusinessTransaction>(FilePathMap.APMEntitiesFullIndexFilePath(jobTarget, APMBusinessTransaction.ENTITY_FOLDER), new BusinessTransactionMetricReportMap());

                        List<APMBackend> backendsList = FileIOHelper.ReadListFromCSVFile<APMBackend>(FilePathMap.APMBackendsIndexFilePath(jobTarget), new APMBackendReportMap());
                        List<APMBackend> backendsMetricsList = FileIOHelper.ReadListFromCSVFile<APMBackend>(FilePathMap.APMEntitiesFullIndexFilePath(jobTarget, APMBackend.ENTITY_FOLDER), new BackendMetricReportMap());

                        List<APMServiceEndpoint> serviceEndpointsList = FileIOHelper.ReadListFromCSVFile<APMServiceEndpoint>(FilePathMap.APMServiceEndpointsIndexFilePath(jobTarget), new APMServiceEndpointReportMap());
                        List<APMServiceEndpoint> serviceEndpointsMetricsList = FileIOHelper.ReadListFromCSVFile<APMServiceEndpoint>(FilePathMap.APMEntitiesFullIndexFilePath(jobTarget, APMServiceEndpoint.ENTITY_FOLDER), new ServiceEndpointMetricReportMap());

                        List<APMError> errorsList = FileIOHelper.ReadListFromCSVFile<APMError>(FilePathMap.APMErrorsIndexFilePath(jobTarget), new APMErrorReportMap());
                        List<APMError> errorsMetricsList = FileIOHelper.ReadListFromCSVFile<APMError>(FilePathMap.APMEntitiesFullIndexFilePath(jobTarget, APMError.ENTITY_FOLDER), new ErrorMetricReportMap());

                        List<APMInformationPoint> informationPointsList = FileIOHelper.ReadListFromCSVFile<APMInformationPoint>(FilePathMap.APMInformationPointsIndexFilePath(jobTarget), new APMInformationPointReportMap());
                        List<APMInformationPoint> informationPointsMetricsList = FileIOHelper.ReadListFromCSVFile<APMInformationPoint>(FilePathMap.APMEntitiesFullIndexFilePath(jobTarget, APMInformationPoint.ENTITY_FOLDER), new InformationPointMetricReportMap());

                        List<APMResolvedBackend> resolvedBackendsList = FileIOHelper.ReadListFromCSVFile<APMResolvedBackend>(FilePathMap.APMMappedBackendsIndexFilePath(jobTarget), new APMResolvedBackendReportMap());

                        List<APMBusinessTransaction> businessTransactionsOverflowList = FileIOHelper.ReadListFromCSVFile<APMBusinessTransaction>(FilePathMap.APMOverflowBusinessTransactionsIndexFilePath(jobTarget), new APMOverflowBusinessTransactionReportMap());

                        //List<Event> eventsList = FileIOHelper.ReadListFromCSVFile<Event>(FilePathMap.ApplicationEventsIndexFilePath(jobTarget), new EventReportMap());
                        //List<HealthRuleViolationEvent> healthRuleViolationEventsList = FileIOHelper.ReadListFromCSVFile<HealthRuleViolationEvent>(FilePathMap.ApplicationHealthRuleViolationsIndexFilePath(jobTarget), new HealthRuleViolationEventReportMap());

                        //List<Snapshot> snapshotsAllList = FileIOHelper.ReadListFromCSVFile<Snapshot>(FilePathMap.SnapshotsIndexFilePath(jobTarget), new SnapshotReportMap());
                        //List<Segment> segmentsAllList = FileIOHelper.ReadListFromCSVFile<Segment>(FilePathMap.SnapshotsSegmentsIndexFilePath(jobTarget), new SegmentReportMap());
                        //List<ExitCall> exitCallsAllList = FileIOHelper.ReadListFromCSVFile<ExitCall>(FilePathMap.SnapshotsExitCallsIndexFilePath(jobTarget), new ExitCallReportMap());
                        //List<ServiceEndpointCall> serviceEndpointCallsAllList = FileIOHelper.ReadListFromCSVFile<ServiceEndpointCall>(FilePathMap.SnapshotsServiceEndpointCallsIndexFilePath(jobTarget), new ServiceEndpointCallReportMap());
                        //List<DetectedError> detectedErrorsAllList = FileIOHelper.ReadListFromCSVFile<DetectedError>(FilePathMap.SnapshotsDetectedErrorsIndexFilePath(jobTarget), new DetectedErrorReportMap());
                        //List<BusinessData> businessDataAllList = FileIOHelper.ReadListFromCSVFile<BusinessData>(FilePathMap.SnapshotsBusinessDataIndexFilePath(jobTarget), new BusinessDataReportMap());

                        List<ActivityFlow> applicationActivityFlowsList = FileIOHelper.ReadListFromCSVFile<ActivityFlow>(FilePathMap.ApplicationFlowmapIndexFilePath(jobTarget), new ApplicationActivityFlowReportMap());
                        List<ActivityFlow> tiersActivityFlowsList = FileIOHelper.ReadListFromCSVFile<ActivityFlow>(FilePathMap.TiersFlowmapIndexFilePath(jobTarget), new TierActivityFlowReportMap());
                        List<ActivityFlow> nodesActivityFlowsList = FileIOHelper.ReadListFromCSVFile<ActivityFlow>(FilePathMap.NodesFlowmapIndexFilePath(jobTarget), new NodeActivityFlowReportMap());
                        List<ActivityFlow> businessTransactionsActivityFlowsList = FileIOHelper.ReadListFromCSVFile<ActivityFlow>(FilePathMap.BusinessTransactionsFlowmapIndexFilePath(jobTarget), new BusinessTransactionActivityFlowReportMap());
                        List<ActivityFlow> backendsActivityFlowsList = FileIOHelper.ReadListFromCSVFile<ActivityFlow>(FilePathMap.BackendsFlowmapIndexFilePath(jobTarget), new BackendActivityFlowReportMap());

                        List<HealthRule> healthRulesList = FileIOHelper.ReadListFromCSVFile<HealthRule>(FilePathMap.ApplicationHealthRulesIndexFilePath(jobTarget), new HealthRuleReportMap());
                        List<Policy> policiesList = FileIOHelper.ReadListFromCSVFile<Policy>(FilePathMap.ApplicationPoliciesIndexFilePath(jobTarget), new PolicyReportMap());
                        List<ReportObjects.Action> actionsList = FileIOHelper.ReadListFromCSVFile<ReportObjects.Action>(FilePathMap.ApplicationActionsIndexFilePath(jobTarget), new ActionReportMap());
                        List<PolicyActionMapping> policyActionMappingList = FileIOHelper.ReadListFromCSVFile<PolicyActionMapping>(FilePathMap.ApplicationPolicyActionMappingsIndexFilePath(jobTarget), new PolicyActionMappingReportMap());

                        List<AgentConfigurationProperty> agentPropertiesList = FileIOHelper.ReadListFromCSVFile<AgentConfigurationProperty>(FilePathMap.APMAgentConfigurationPropertiesIndexFilePath(jobTarget), new AgentConfigurationPropertyReportMap());

                        #endregion

                        #region Render the report

                        Document applicationSummaryDocument = createApplicationSummaryDocument(programOptions, jobConfiguration, jobTarget);

                        List listHeadingsOutline = applicationSummaryDocument.Lists.Add(ListTemplate.OutlineLegal);

                        DocumentBuilder builder = new DocumentBuilder(applicationSummaryDocument);
                        builder.MoveToDocumentEnd();

                        Style hyperLinkStyle = builder.Document.Styles["HyperLinkStyle"];

                        #region Application

                        insertHeading(builder, StyleIdentifier.Heading1, "Application", "Application", listHeadingsOutline, 0);

                        insertLinksToEntityTypeSections(builder);

                        builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;
                        builder.Writeln("");

                        if (applicationsList != null && applicationsList.Count > 0)
                        {
                            #region Summary Table

                            insertHeading(builder, StyleIdentifier.Heading2, "Summary", String.Empty, listHeadingsOutline, 1);

                            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;

                            APMApplication application = applicationsList[0];
                            ControllerSummary controllerSummary = controllerSummariesList[0];

                            Table tableApplicationSummary = insertTableApplicationSummary(builder);

                            insertCellStringValue(builder, "Controller");
                            insertCellStringValue(builder, application.Controller);
                            insertCellStringValue(builder, application.Controller.ToLower().Contains("saas.appdynamics") ? "SaaS" : "On Premises");
                            builder.EndRow();

                            insertCellStringValue(builder, "Version");
                            insertCellStringValue(builder, controllerSummary.Version);
                            insertCellStringValue(builder, controllerSummary.VersionDetail);
                            builder.EndRow();

                            insertCellStringValue(builder, "Application");
                            insertCellStringValue(builder, String.Format("{0} ({1}) {2} [{3}]", application.ApplicationName, application.ApplicationID, application.Description, jobTarget.Type));
                            insertCellNoValue(builder);
                            builder.Write("Navigate to: ");
                            if (applicationsMetricsList != null && applicationsMetricsList.Count > 0)
                            {
                                APMApplication applicationWithMetrics = applicationsMetricsList[0];
                                insertLinkToURL(builder, hyperLinkStyle, "Controller", applicationWithMetrics.ControllerLink);
                                builder.Write(", ");
                                insertLinkToURL(builder, hyperLinkStyle, "Application", applicationWithMetrics.ApplicationLink);
                            }
                            else
                            {
                                insertLinkToURL(builder, hyperLinkStyle, "Controller", application.ControllerLink);
                                builder.Write(", ");
                                insertLinkToURL(builder, hyperLinkStyle, "Application", application.ApplicationLink);
                            }
                            builder.EndRow();

                            finalizeTableApplicationSummary(builder, tableApplicationSummary);

                            #endregion

                            #region Entity Types Table

                            insertHeading(builder, StyleIdentifier.Heading2, "Entity Types and Activity", String.Empty, listHeadingsOutline, 1);

                            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;

                            Table tableApplicationEntitySummary = insertTableApplicationEntityTypes(builder);

                            #region Tiers

                            insertCellNoValue(builder); insertLinkToBookmark(builder, hyperLinkStyle, "Tiers", "Tiers");
                            if (tiersList != null && tiersList.Count > 0)
                            {
                                List<string> tierTypesList = new List<string>(100);
                                List<double> tierTypesCountsList = new List<double>(100);
                                List<string> tierTypeAndCountsList = new List<string>(100);

                                var groupTypes = tiersList.GroupBy(t => t.AgentType);
                                measureTypesOfItemsInGroupBy(groupTypes, tierTypeAndCountsList, tierTypesList, tierTypesCountsList);

                                insertCellStringValue(builder, String.Format("{0} total\n{1}", tiersList.Count, String.Join("\n", tierTypeAndCountsList.ToArray())));
                                insertCellNoValue(builder);
                                insertPieChart(builder, "Tier Types", tierTypesList.ToArray(), tierTypesCountsList.ToArray());
                            }
                            else
                            {
                                insertCellStringValue(builder, "No tier list available");
                                insertCellStringValue(builder, String.Empty);
                            }

                            if (tiersMetricsList != null && tiersMetricsList.Count > 0)
                            {
                                string[] tierActivityArray = new string[2];
                                tierActivityArray[0] = "Has Activity";
                                tierActivityArray[1] = "No Activity";
                                double[] tierActivityCountsArray = new double[2];

                                tierActivityCountsArray[0] = tiersMetricsList.Count(t => t.HasActivity == true);
                                tierActivityCountsArray[1] = tiersMetricsList.Count(t => t.HasActivity == false);

                                insertCellStringValue(builder, String.Format("{0} active\n{1} inactive", tierActivityCountsArray[0], tierActivityCountsArray[1]));
                                insertCellNoValue(builder);
                                insertPieChart(builder, "Tier Activity", tierActivityArray, tierActivityCountsArray);
                            }
                            else
                            {
                                insertCellStringValue(builder, "No metrics available");
                                insertCellStringValue(builder, String.Empty);
                            }
                            builder.EndRow();

                            #endregion

                            #region Nodes

                            insertCellNoValue(builder); insertLinkToBookmark(builder, hyperLinkStyle, "Nodes", "Nodes");
                            if (nodesList != null && nodesList.Count > 0)
                            {
                                List<string> nodeTypesList = new List<string>(100);
                                List<double> nodeTypesCountsList = new List<double>(100);
                                List<string> nodeTypeAndCountsList = new List<string>(100);

                                var groupTypes = nodesList.GroupBy(t => t.AgentType);
                                measureTypesOfItemsInGroupBy(groupTypes, nodeTypeAndCountsList, nodeTypesList, nodeTypesCountsList);

                                insertCellStringValue(builder, String.Format("{0} total\n{1}", nodesList.Count, String.Join("\n", nodeTypeAndCountsList.ToArray())));
                                insertCellNoValue(builder);
                                insertPieChart(builder, "Node Types", nodeTypesList.ToArray(), nodeTypesCountsList.ToArray());
                            }
                            else
                            {
                                insertCellStringValue(builder, "No node list available");
                                insertCellStringValue(builder, String.Empty);
                            }

                            if (nodesMetricsList != null && nodesMetricsList.Count > 0)
                            {
                                string[] nodeActivityArray = new string[2];
                                nodeActivityArray[0] = "Has Activity";
                                nodeActivityArray[1] = "No Activity";
                                double[] nodeActivityCountsArray = new double[2];

                                nodeActivityCountsArray[0] = nodesMetricsList.Count(t => t.HasActivity == true);
                                nodeActivityCountsArray[1] = nodesMetricsList.Count(t => t.HasActivity == false);

                                insertCellStringValue(builder, String.Format("{0} active\n{1} inactive", nodeActivityCountsArray[0], nodeActivityCountsArray[1]));
                                insertCellNoValue(builder);
                                insertPieChart(builder, "Node Activity", nodeActivityArray, nodeActivityCountsArray);
                            }
                            else
                            {
                                insertCellStringValue(builder, "No metrics available");
                                insertCellStringValue(builder, String.Empty);
                            }
                            builder.EndRow();

                            #endregion

                            #region Business Transactions

                            insertCellNoValue(builder); insertLinkToBookmark(builder, hyperLinkStyle, "Business Transactions", "Business_Transactions");
                            if (businessTransactionsList != null && businessTransactionsList.Count > 0)
                            {
                                List<string> businessTransactionTypesList = new List<string>(100);
                                List<double> businessTransactionTypesCountsList = new List<double>(100);
                                List<string> businessTransactionTypeAndCountsList = new List<string>(100);

                                var groupTypes = businessTransactionsList.GroupBy(t => t.BTType);
                                measureTypesOfItemsInGroupBy(groupTypes, businessTransactionTypeAndCountsList, businessTransactionTypesList, businessTransactionTypesCountsList);

                                insertCellStringValue(builder, String.Format("{0} total\n{1}", businessTransactionsList.Count, String.Join("\n", businessTransactionTypeAndCountsList.ToArray())));
                                insertCellNoValue(builder);
                                insertPieChart(builder, "Business Transaction Types", businessTransactionTypesList.ToArray(), businessTransactionTypesCountsList.ToArray());
                            }
                            else
                            {
                                insertCellStringValue(builder, "No business transaction list available");
                                insertCellStringValue(builder, String.Empty);
                            }

                            if (businessTransactionsMetricsList != null && businessTransactionsMetricsList.Count > 0)
                            {
                                string[] businessTransactionActivityArray = new string[2];
                                businessTransactionActivityArray[0] = "Has Activity";
                                businessTransactionActivityArray[1] = "No Activity";
                                double[] businessTransactionActivityCountsArray = new double[2];

                                businessTransactionActivityCountsArray[0] = businessTransactionsMetricsList.Count(t => t.HasActivity == true);
                                businessTransactionActivityCountsArray[1] = businessTransactionsMetricsList.Count(t => t.HasActivity == false);

                                insertCellStringValue(builder, String.Format("{0} active\n{1} inactive", businessTransactionActivityCountsArray[0], businessTransactionActivityCountsArray[1]));
                                insertCellNoValue(builder);
                                insertPieChart(builder, "Business Transaction Activity", businessTransactionActivityArray, businessTransactionActivityCountsArray);
                            }
                            else
                            {
                                insertCellStringValue(builder, "No metrics available");
                                insertCellStringValue(builder, String.Empty);
                            }
                            builder.EndRow();

                            #endregion

                            #region Business Transactions - Overflow

                            insertCellStringValue(builder, "Overflow Business Transactions");
                            if (businessTransactionsList != null && businessTransactionsList.Count > 0 &&
                                businessTransactionsOverflowList != null && businessTransactionsOverflowList.Count > 0)
                            {
                                List<string> businessTransactionTypesList = new List<string>(100);
                                List<double> businessTransactionTypesCountsList = new List<double>(100);
                                List<string> businessTransactionTypeAndCountsList = new List<string>(100);

                                var groupTypes = businessTransactionsOverflowList.GroupBy(t => t.BTType);
                                measureTypesOfItemsInGroupBy(groupTypes, businessTransactionTypeAndCountsList, businessTransactionTypesList, businessTransactionTypesCountsList);

                                insertCellStringValue(builder, String.Format("{0} total\n{1} unregistered\n{2}", businessTransactionsList.Where(b => b.BTType == "OVERFLOW").Count(), businessTransactionsOverflowList.Count, String.Join("\n", businessTransactionTypeAndCountsList.ToArray())));
                                insertCellNoValue(builder);
                                insertPieChart(builder, "Overflow Business Transaction Types", businessTransactionTypesList.ToArray(), businessTransactionTypesCountsList.ToArray());
                            }
                            else
                            {
                                insertCellStringValue(builder, "No business transaction list available");
                                insertCellStringValue(builder, String.Empty);
                            }

                            if (businessTransactionsMetricsList != null && businessTransactionsMetricsList.Count > 0)
                            {
                                string[] businessTransactionActivityArray = new string[2];
                                businessTransactionActivityArray[0] = "Has Activity";
                                businessTransactionActivityArray[1] = "No Activity";
                                double[] businessTransactionActivityCountsArray = new double[2];

                                businessTransactionActivityCountsArray[0] = businessTransactionsMetricsList.Where(b => b.BTType == "OVERFLOW").Count(t => t.HasActivity == true);
                                businessTransactionActivityCountsArray[1] = businessTransactionsMetricsList.Where(b => b.BTType == "OVERFLOW").Count(t => t.HasActivity == false);

                                insertCellStringValue(builder, String.Format("{0} active\n{1} inactive", businessTransactionActivityCountsArray[0], businessTransactionActivityCountsArray[1]));
                                insertCellNoValue(builder);
                                insertPieChart(builder, "Business Transaction Activity", businessTransactionActivityArray, businessTransactionActivityCountsArray);
                            }
                            else
                            {
                                insertCellStringValue(builder, "No metrics available");
                                insertCellStringValue(builder, String.Empty);
                            }
                            builder.EndRow();
                            #endregion

                            #region Backends

                            insertCellNoValue(builder); insertLinkToBookmark(builder, hyperLinkStyle, "Backends", "Backends");
                            if (backendsList != null && backendsList.Count > 0)
                            {
                                List<string> backendTypesList = new List<string>(100);
                                List<double> backendTypesCountsList = new List<double>(100);
                                List<string> backendTypeAndCountsList = new List<string>(100);

                                var groupTypes = backendsList.GroupBy(t => t.BackendType);
                                measureTypesOfItemsInGroupBy(groupTypes, backendTypeAndCountsList, backendTypesList, backendTypesCountsList);

                                insertCellStringValue(builder, String.Format("{0} total\n{1}", backendsList.Count, String.Join("\n", backendTypeAndCountsList.ToArray())));
                                insertCellNoValue(builder);
                                insertPieChart(builder, "Backend Types", backendTypesList.ToArray(), backendTypesCountsList.ToArray());
                            }
                            else
                            {
                                insertCellStringValue(builder, "No backend list available");
                                insertCellStringValue(builder, String.Empty);
                            }

                            if (backendsMetricsList != null && backendsMetricsList.Count > 0)
                            {
                                string[] backendActivityArray = new string[2];
                                backendActivityArray[0] = "Has Activity";
                                backendActivityArray[1] = "No Activity";
                                double[] backendActivityCountsArray = new double[2];

                                backendActivityCountsArray[0] = backendsMetricsList.Count(t => t.HasActivity == true);
                                backendActivityCountsArray[1] = backendsMetricsList.Count(t => t.HasActivity == false);

                                insertCellStringValue(builder, String.Format("{0} active\n{1} inactive", backendActivityCountsArray[0], backendActivityCountsArray[1]));
                                insertCellNoValue(builder);
                                insertPieChart(builder, "Backend Activity", backendActivityArray, backendActivityCountsArray);
                            }
                            else
                            {
                                insertCellStringValue(builder, "No metrics available");
                                insertCellStringValue(builder, String.Empty);
                            }
                            builder.EndRow();

                            #endregion

                            #region Service Endpoints

                            insertCellNoValue(builder); insertLinkToBookmark(builder, hyperLinkStyle, "Service Endpoints", "Service_Endpoints");
                            if (serviceEndpointsList != null && serviceEndpointsList.Count > 0)
                            {
                                List<string> serviceEndpointTypesList = new List<string>(100);
                                List<double> serviceEndpointTypesCountsList = new List<double>(100);
                                List<string> serviceEndpointTypeAndCountsList = new List<string>(100);

                                var groupTypes = serviceEndpointsList.GroupBy(t => t.SEPType);
                                measureTypesOfItemsInGroupBy(groupTypes, serviceEndpointTypeAndCountsList, serviceEndpointTypesList, serviceEndpointTypesCountsList);

                                insertCellStringValue(builder, String.Format("{0} total\n{1}", serviceEndpointsList.Count, String.Join("\n", serviceEndpointTypeAndCountsList.ToArray())));
                                insertCellNoValue(builder);
                                insertPieChart(builder, "Service Endpoint Types", serviceEndpointTypesList.ToArray(), serviceEndpointTypesCountsList.ToArray());
                            }
                            else
                            {
                                insertCellStringValue(builder, "No service endpoint list available");
                                insertCellStringValue(builder, String.Empty);
                            }

                            if (serviceEndpointsMetricsList != null && serviceEndpointsMetricsList.Count > 0)
                            {
                                string[] serviceEndpointActivityArray = new string[2];
                                serviceEndpointActivityArray[0] = "Has Activity";
                                serviceEndpointActivityArray[1] = "No Activity";
                                double[] serviceEndpointActivityCountsArray = new double[2];

                                serviceEndpointActivityCountsArray[0] = serviceEndpointsMetricsList.Count(t => t.HasActivity == true);
                                serviceEndpointActivityCountsArray[1] = serviceEndpointsMetricsList.Count(t => t.HasActivity == false);

                                insertCellStringValue(builder, String.Format("{0} active\n{1} inactive", serviceEndpointActivityCountsArray[0], serviceEndpointActivityCountsArray[1]));
                                insertCellNoValue(builder);
                                insertPieChart(builder, "Service Endpoint Activity", serviceEndpointActivityArray, serviceEndpointActivityCountsArray);
                            }
                            else
                            {
                                insertCellStringValue(builder, "No metrics available");
                                insertCellStringValue(builder, String.Empty);
                            }
                            builder.EndRow();

                            #endregion

                            #region Errors

                            insertCellNoValue(builder); insertLinkToBookmark(builder, hyperLinkStyle, "Errors", "Errors");
                            if (errorsList != null && errorsList.Count > 0)
                            {
                                List<string> errorTypesList = new List<string>(100);
                                List<double> errorTypesCountsList = new List<double>(100);
                                List<string> errorTypeAndCountsList = new List<string>(100);

                                var groupTypes = errorsList.GroupBy(t => t.ErrorType);
                                measureTypesOfItemsInGroupBy(groupTypes, errorTypeAndCountsList, errorTypesList, errorTypesCountsList);

                                insertCellStringValue(builder, String.Format("{0} total\n{1}", errorsList.Count, String.Join("\n", errorTypeAndCountsList.ToArray())));
                                insertCellNoValue(builder);
                                insertPieChart(builder, "Error Types", errorTypesList.ToArray(), errorTypesCountsList.ToArray());
                            }
                            else
                            {
                                insertCellStringValue(builder, "No error list available");
                                insertCellStringValue(builder, String.Empty);
                            }

                            if (errorsMetricsList != null && errorsMetricsList.Count > 0)
                            {
                                string[] errorActivityArray = new string[2];
                                errorActivityArray[0] = "Has Activity";
                                errorActivityArray[1] = "No Activity";
                                double[] errorActivityCountsArray = new double[2];

                                errorActivityCountsArray[0] = errorsMetricsList.Count(t => t.HasActivity == true);
                                errorActivityCountsArray[1] = errorsMetricsList.Count(t => t.HasActivity == false);

                                insertCellStringValue(builder, String.Format("{0} active\n{1} inactive", errorActivityCountsArray[0], errorActivityCountsArray[1]));
                                insertCellNoValue(builder);
                                insertPieChart(builder, "Error Activity", errorActivityArray, errorActivityCountsArray);
                            }
                            else
                            {
                                insertCellStringValue(builder, "No metrics available");
                                insertCellStringValue(builder, String.Empty);
                            }
                            builder.EndRow();

                            #endregion

                            #region Information Points

                            insertCellNoValue(builder); insertLinkToBookmark(builder, hyperLinkStyle, "Information Points", "Information_Points");
                            if (informationPointsList != null && informationPointsList.Count > 0)
                            {
                                List<string> informationPointTypesList = new List<string>(100);
                                List<double> informationPointTypesCountsList = new List<double>(100);
                                List<string> informationPointTypeAndCountsList = new List<string>(100);

                                var groupTypes = informationPointsList.GroupBy(t => t.IPType);
                                measureTypesOfItemsInGroupBy(groupTypes, informationPointTypeAndCountsList, informationPointTypesList, informationPointTypesCountsList);

                                insertCellStringValue(builder, String.Format("{0} total\n{1}", informationPointsList.Count, String.Join("\n", informationPointTypeAndCountsList.ToArray())));
                                insertCellNoValue(builder);
                                insertPieChart(builder, "Information Point Types", informationPointTypesList.ToArray(), informationPointTypesCountsList.ToArray());
                            }
                            else
                            {
                                insertCellStringValue(builder, "No information point list available");
                                insertCellStringValue(builder, String.Empty);
                            }

                            if (informationPointsMetricsList != null && informationPointsMetricsList.Count > 0)
                            {
                                string[] informationPointActivityArray = new string[2];
                                informationPointActivityArray[0] = "Has Activity";
                                informationPointActivityArray[1] = "No Activity";
                                double[] informationPointActivityCountsArray = new double[2];

                                informationPointActivityCountsArray[0] = informationPointsMetricsList.Count(t => t.HasActivity == true);
                                informationPointActivityCountsArray[1] = informationPointsMetricsList.Count(t => t.HasActivity == false);

                                insertCellStringValue(builder, String.Format("{0} active\n{1} inactive", informationPointActivityCountsArray[0], informationPointActivityCountsArray[1]));
                                insertCellNoValue(builder);
                                insertPieChart(builder, "Information Point Activity", informationPointActivityArray, informationPointActivityCountsArray);
                            }
                            else
                            {
                                insertCellStringValue(builder, "No metrics available");
                                insertCellStringValue(builder, String.Empty);
                            }
                            builder.EndRow();

                            #endregion

                            finalizeTableApplicationEntityTypes(builder, tableApplicationEntitySummary);

                            #endregion

                            #region Detected Entity Dependencies

                            insertHeading(builder, StyleIdentifier.Heading2, "Detected Entity Dependencies", String.Empty, listHeadingsOutline, 1);

                            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;

                            Table tableApplicationEntityMapped = insertTableApplicationEntityMapped(builder);

                            #region Explicitly Registered Business Transactions

                            insertCellStringValue(builder, "Business Transaction Source");
                            if (businessTransactionsList != null && businessTransactionsList.Count > 0)
                            {
                                List<string> businessTransactionTypesList = new List<string>(100);
                                List<double> businessTransactionTypesCountsList = new List<double>(100);
                                List<string> businessTransactionTypeAndCountsList = new List<string>(100);

                                var groupTypes = businessTransactionsList.Where(t => t.IsExplicitRule == true).GroupBy(t => t.BTType);
                                measureTypesOfItemsInGroupBy(groupTypes, businessTransactionTypeAndCountsList, businessTransactionTypesList, businessTransactionTypesCountsList);

                                insertCellStringValue(builder, String.Format("Explicitly registered\n{0} total\n{1}", businessTransactionsList.Where(t => t.IsExplicitRule == true).Count(), String.Join("\n", businessTransactionTypeAndCountsList.ToArray())));
                                insertCellNoValue(builder);
                                insertPieChart(builder, "Explicit Business Transaction", businessTransactionTypesList.ToArray(), businessTransactionTypesCountsList.ToArray());
                            }
                            else
                            {
                                insertCellStringValue(builder, "No business transaction list available");
                                insertCellStringValue(builder, String.Empty);
                            }
                            if (businessTransactionsList != null && businessTransactionsList.Count > 0)
                            {
                                List<string> businessTransactionTypesList = new List<string>(100);
                                List<double> businessTransactionTypesCountsList = new List<double>(100);
                                List<string> businessTransactionTypeAndCountsList = new List<string>(100);

                                var groupTypes = businessTransactionsList.Where(t => t.IsExplicitRule == false).GroupBy(t => t.BTType);
                                measureTypesOfItemsInGroupBy(groupTypes, businessTransactionTypeAndCountsList, businessTransactionTypesList, businessTransactionTypesCountsList);

                                insertCellStringValue(builder, String.Format("Automatically registered\n{0} total\n{1}", businessTransactionsList.Where(t => t.IsExplicitRule == false).Count(), String.Join("\n", businessTransactionTypeAndCountsList.ToArray())));
                                insertCellNoValue(builder);
                                insertPieChart(builder, "Automatic Business Transaction", businessTransactionTypesList.ToArray(), businessTransactionTypesCountsList.ToArray());
                            }
                            else
                            {
                                insertCellStringValue(builder, "No business transaction list available");
                                insertCellStringValue(builder, String.Empty);
                            }

                            builder.EndRow();

                            #endregion

                            #region Mapped Backends

                            insertCellStringValue(builder, "Mapped Backends");
                            if (resolvedBackendsList != null && resolvedBackendsList.Count > 0)
                            {
                                List<string> backendTypesList = new List<string>(100);
                                List<double> backendTypesCountsList = new List<double>(100);
                                List<string> backendTypeAndCountsList = new List<string>(100);

                                var groupTypes = resolvedBackendsList.GroupBy(t => t.BackendType);
                                measureTypesOfItemsInGroupBy(groupTypes, backendTypeAndCountsList, backendTypesList, backendTypesCountsList);

                                insertCellStringValue(builder, String.Format("{0} total\n{1}", resolvedBackendsList.Count, String.Join("\n", backendTypeAndCountsList.ToArray())));
                                insertCellNoValue(builder);
                                insertPieChart(builder, "Mapped Backend Types", backendTypesList.ToArray(), backendTypesCountsList.ToArray());
                            }
                            else
                            {
                                insertCellStringValue(builder, "No mapped backend list available");
                                insertCellStringValue(builder, String.Empty);
                            }
                            insertCellNoValue(builder);
                            insertCellNoValue(builder);

                            builder.EndRow();

                            #endregion

                            finalizeTableApplicationEntityMapped(builder, tableApplicationEntityMapped);

                            #endregion

                            #region Dashboard

                            insertHeading(builder, StyleIdentifier.Heading2, "Dashboard", String.Empty, listHeadingsOutline, 1);

                            if (File.Exists(FilePathMap.EntityDashboardScreenshotReportFilePath(application, true, false)) == true)
                            {
                                builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;
                                builder.InsertImage(FilePathMap.EntityDashboardScreenshotReportFilePath(application, true, false));
                                builder.Writeln("");
                            }
                            else
                            {
                                builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;
                                builder.Writeln("No dashboard captured");
                            }

                            #endregion

                            #region Flowmap

                            insertHeading(builder, StyleIdentifier.Heading2, "Flowmap in Grid Form", String.Empty, listHeadingsOutline, 1);

                            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;

                            Table tableApplicationActivityGrid = insertTableActivityGrid(builder);

                            if (applicationActivityFlowsList != null && applicationActivityFlowsList.Count > 0)
                            {
                                foreach (ActivityFlow activityFlow in applicationActivityFlowsList)
                                {
                                    insertRowActivityGrid(builder, activityFlow);
                                }
                            }

                            finalizeTableActivityGrid(builder, tableApplicationActivityGrid);

                            #endregion

                            #region Agent Properties

                            if (agentPropertiesList != null && agentPropertiesList.Count > 0 && agentPropertiesList.Where(p => p.IsDefault == false).Count() > 0)
                            {
                                insertHeading(builder, StyleIdentifier.Heading2, "Agent Properties", "Application_Properties", listHeadingsOutline, 1);

                                builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;

                                Table tableApplicationAgentProperties = insertTableApplicationAgentProperties(builder);

                                foreach (AgentConfigurationProperty agentProperty in agentPropertiesList.Where(p => p.IsDefault == false))
                                {
                                    insertCellStringValue(builder, agentProperty.TierName.Length == 0 ? "Application" : agentProperty.TierName);
                                    insertCellStringValue(builder, agentProperty.AgentType);
                                    insertCellStringValue(builder, agentProperty.PropertyName);
                                    insertCellStringValue(builder, agentProperty.PropertyType);
                                    switch (agentProperty.PropertyType)
                                    {
                                        case "BOOLEAN":
                                            insertCellStringValue(builder, agentProperty.BooleanValue);
                                            insertCellNoValue(builder); insertStrikethroughText(builder, agentProperty.BooleanDefaultValue.ToString());
                                            break;

                                        case "INTEGER":
                                            insertCellStringValue(builder, agentProperty.IntegerValue);
                                            insertCellNoValue(builder); insertStrikethroughText(builder, agentProperty.IntegerDefaultValue.ToString());
                                            break;

                                        case "STRING":
                                            insertCellStringValue(builder, agentProperty.StringValue);
                                            insertCellNoValue(builder); insertStrikethroughText(builder, agentProperty.StringDefaultValue);
                                            break;

                                        default:
                                            break;
                                    }
                                    insertCellNoValue(builder); insertTextWithSize(builder, agentProperty.Description, 8);
                                    builder.EndRow();
                                }

                                finalizeApplicationAgentProperties(builder, tableApplicationAgentProperties);
                            }

                            #endregion
                        }

                        builder.InsertBreak(BreakType.PageBreak);

                        #endregion

                        #region Tiers

                        insertHeading(builder, StyleIdentifier.Heading1, "Tiers", "Tiers", listHeadingsOutline, 0);

                        insertLinksToEntityTypeSections(builder);

                        if (tiersList != null)
                        {
                            loggerConsole.Info("Processing Tiers ({0} entities)", tiersList.Count);

                            insertLinksToTiers(tiersList, builder, hyperLinkStyle);

                            #region Summary Table

                            insertHeading(builder, StyleIdentifier.Heading2, "Summary", String.Empty, listHeadingsOutline, 1);

                            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;

                            Table table = insertTableTiersSummary(builder);

                            foreach (APMTier tier in tiersList)
                            {
                                APMTier tierWithMetric = tier;
                                if (tiersMetricsList != null) tierWithMetric = tiersMetricsList.Where(t => t.TierID == tier.TierID).FirstOrDefault();

                                insertCellNoValue(builder); insertLinkToBookmark(builder, hyperLinkStyle, tier.TierName, getShortenedEntityNameForWordBookmark("tier", tier.TierName, tier.TierID));
                                insertCellStringValue(builder, tier.AgentType);
                                insertCellNoValue(builder); insertLinkToBookmark(builder, hyperLinkStyle, tier.NumNodes.ToString(), getShortenedEntityNameForWordBookmark("tiernode", tier.TierName, tier.TierID));
                                if (nodesList != null)
                                {
                                    insertCellStringValue(builder, nodesList.Where(n => n.TierID == tier.TierID && n.MachineAgentPresent == true).Count());
                                }
                                else
                                {
                                    insertCellStringValue(builder, 0);
                                }
                                insertCellNoValue(builder); insertLinkToBookmark(builder, hyperLinkStyle, tier.NumBTs.ToString(), getShortenedEntityNameForWordBookmark("tierbt", tier.TierName, tier.TierID));
                                insertCellNoValue(builder); insertLinkToBookmark(builder, hyperLinkStyle, tier.NumSEPs.ToString(), getShortenedEntityNameForWordBookmark("tiersep", tier.TierName, tier.TierID));
                                insertCellNoValue(builder); insertLinkToBookmark(builder, hyperLinkStyle, tier.NumErrors.ToString(), getShortenedEntityNameForWordBookmark("tiererr", tier.TierName, tier.TierID));
                                insertCellStringValue(builder, tierWithMetric.AvailAgent);
                                insertCellStringValue(builder, tierWithMetric.AvailMachine);
                                insertCellStringValue(builder, tierWithMetric.HasActivity);
                                insertCellStringValue(builder, -123);
                                insertCellStringValue(builder, -321);
                                builder.EndRow();
                            }

                            finalizeTableTiersSummary(builder, table);

                            #endregion

                            int j = 0;

                            foreach (APMTier tier in tiersList)
                            {
                                APMTier tierWithMetric = tier;
                                if (tiersMetricsList != null) tierWithMetric = tiersMetricsList.Where(t => t.TierID == tier.TierID).FirstOrDefault();

                                insertHeading(builder, StyleIdentifier.Heading2, String.Format("{0} [{1}] ({2})", tier.TierName, tier.AgentType, tier.TierID), getShortenedEntityNameForWordBookmark("tier", tier.TierName, tier.TierID), listHeadingsOutline, 1);

                                insertLinksToEntityTypeSections(builder);

                                #region Summary Table

                                insertHeading(builder, StyleIdentifier.Heading3, "Summary", String.Empty, listHeadingsOutline, 2);

                                builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;

                                table = insertTableTierEntityTypes(builder);

                                // APM Node versions
                                insertCellStringValue(builder, "Node APM");
                                if (nodesList != null && nodesList.Count > 0)
                                {
                                    List<string> nodeTypesList = new List<string>(100);
                                    List<double> nodeTypesCountsList = new List<double>(100);
                                    List<string> nodeTypeAndCountsList = new List<string>(100);

                                    var groupTypes = nodesList.Where(n => n.TierID == tier.TierID && n.AgentPresent == true).GroupBy(t => t.AgentVersion);
                                    measureTypesOfItemsInGroupBy(groupTypes, nodeTypeAndCountsList, nodeTypesList, nodeTypesCountsList);

                                    insertCellStringValue(builder, String.Format("{0} total\n{1}", nodesList.Where(n => n.TierID == tier.TierID && n.AgentPresent == true).Count(), String.Join("\n", nodeTypeAndCountsList.ToArray())));
                                    insertCellNoValue(builder);
                                    insertPieChart(builder, "Node Versions", nodeTypesList.ToArray(), nodeTypesCountsList.ToArray());
                                }
                                else
                                {
                                    insertCellStringValue(builder, "No node list available");
                                    insertCellStringValue(builder, String.Empty);
                                }

                                if (nodesMetricsList != null && nodesMetricsList.Count > 0)
                                {
                                    string[] nodeActivityArray = new string[2];
                                    nodeActivityArray[0] = "Has Activity";
                                    nodeActivityArray[1] = "No Activity";
                                    double[] nodeActivityCountsArray = new double[2];

                                    nodeActivityCountsArray[0] = nodesMetricsList.Where(n => n.TierID == tier.TierID && n.IsAPMAgentUsed == true).Count(t => t.HasActivity == true);
                                    nodeActivityCountsArray[1] = nodesMetricsList.Where(n => n.TierID == tier.TierID && n.IsAPMAgentUsed == true).Count(t => t.HasActivity == false);

                                    insertCellStringValue(builder, String.Format("{0} active\n{1} inactive", nodeActivityCountsArray[0], nodeActivityCountsArray[1]));
                                    insertCellNoValue(builder);
                                    insertPieChart(builder, "Node Activity", nodeActivityArray, nodeActivityCountsArray);
                                }
                                else
                                {
                                    insertCellStringValue(builder, "No metrics available");
                                    insertCellStringValue(builder, String.Empty);
                                }
                                builder.EndRow();

                                // Machine Agent
                                insertCellStringValue(builder, "Node MA");
                                if (nodesList != null && nodesList.Count > 0)
                                {
                                    List<string> nodeTypesList = new List<string>(100);
                                    List<double> nodeTypesCountsList = new List<double>(100);
                                    List<string> nodeTypeAndCountsList = new List<string>(100);

                                    var groupTypes = nodesList.Where(n => n.TierID == tier.TierID && n.MachineAgentPresent == true).GroupBy(t => t.MachineAgentVersion);
                                    measureTypesOfItemsInGroupBy(groupTypes, nodeTypeAndCountsList, nodeTypesList, nodeTypesCountsList);

                                    insertCellStringValue(builder, String.Format("{0} total\n{1}", nodesList.Where(n => n.TierID == tier.TierID && n.MachineAgentPresent == true).Count(), String.Join("\n", nodeTypeAndCountsList.ToArray())));
                                    insertCellNoValue(builder);
                                    insertPieChart(builder, "Node Versions", nodeTypesList.ToArray(), nodeTypesCountsList.ToArray());
                                }
                                else
                                {
                                    insertCellStringValue(builder, "No node list available");
                                    insertCellStringValue(builder, String.Empty);
                                }

                                if (nodesMetricsList != null && nodesMetricsList.Count > 0)
                                {
                                    string[] nodeActivityArray = new string[2];
                                    nodeActivityArray[0] = "Has Activity";
                                    nodeActivityArray[1] = "No Activity";
                                    double[] nodeActivityCountsArray = new double[2];

                                    nodeActivityCountsArray[0] = nodesMetricsList.Where(n => n.TierID == tier.TierID && n.IsMachineAgentUsed == true).Count(t => t.HasActivity == true);
                                    nodeActivityCountsArray[1] = nodesMetricsList.Where(n => n.TierID == tier.TierID && n.IsMachineAgentUsed == true).Count(t => t.HasActivity == false);

                                    insertCellStringValue(builder, String.Format("{0} active\n{1} inactive", nodeActivityCountsArray[0], nodeActivityCountsArray[1]));
                                    insertCellNoValue(builder);
                                    insertPieChart(builder, "Node Activity", nodeActivityArray, nodeActivityCountsArray);
                                }
                                else
                                {
                                    insertCellStringValue(builder, "No metrics available");
                                    insertCellStringValue(builder, String.Empty);
                                }
                                builder.EndRow();
                                finalizeTableTierEntityTypes(builder, table);

                                #endregion

                                #region Dashboard

                                insertHeading(builder, StyleIdentifier.Heading3, "Dashboard", String.Empty, listHeadingsOutline, 2);

                                if (File.Exists(FilePathMap.EntityDashboardScreenshotReportFilePath(tier, true, false)) == true)
                                {
                                    builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;
                                    builder.InsertImage(FilePathMap.EntityDashboardScreenshotReportFilePath(tier, true, false));
                                    builder.Writeln("");
                                }
                                else
                                {
                                    builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;
                                    builder.Writeln("No dashboard captured");
                                }

                                #endregion

                                #region Flowmap

                                insertHeading(builder, StyleIdentifier.Heading3, "Flowmap in Grid Form", String.Empty, listHeadingsOutline, 2);

                                builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;

                                Table tableApplicationActivityGrid = insertTableActivityGrid(builder);

                                if (tiersActivityFlowsList != null && tiersActivityFlowsList.Count > 0)
                                {
                                    List<ActivityFlow> thisTierAcvitityFlowsList = tiersActivityFlowsList.Where(a => a.TierID == tier.TierID).ToList();
                                    foreach (ActivityFlow activityFlow in thisTierAcvitityFlowsList)
                                    {
                                        insertRowActivityGrid(builder, activityFlow);
                                    }
                                }

                                finalizeTableActivityGrid(builder, tableApplicationActivityGrid);

                                #endregion

                                #region Agent Properties

                                if (agentPropertiesList != null && agentPropertiesList.Count > 0 && agentPropertiesList.Where(p => p.TierName == tier.TierName && p.IsDefault == false).Count() > 0)
                                {
                                    insertHeading(builder, StyleIdentifier.Heading3, "Agent Properties", getShortenedEntityNameForWordBookmark("tierprop", tier.TierName, tier.TierID), listHeadingsOutline, 2);

                                    builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;

                                    Table tableTierAgentProperties = insertTableTierAgentProperties(builder);

                                    foreach (AgentConfigurationProperty agentProperty in agentPropertiesList.Where(p => p.TierName == tier.TierName && p.IsDefault == false))
                                    {
                                        insertCellStringValue(builder, agentProperty.PropertyName);
                                        insertCellStringValue(builder, agentProperty.PropertyType);
                                        switch (agentProperty.PropertyType)
                                        {
                                            case "BOOLEAN":
                                                insertCellStringValue(builder, agentProperty.BooleanValue);
                                                insertCellNoValue(builder); insertStrikethroughText(builder, agentProperty.BooleanDefaultValue.ToString());
                                                break;

                                            case "INTEGER":
                                                insertCellStringValue(builder, agentProperty.IntegerValue);
                                                insertCellNoValue(builder); insertStrikethroughText(builder, agentProperty.IntegerDefaultValue.ToString());
                                                break;

                                            case "STRING":
                                                insertCellStringValue(builder, agentProperty.StringValue);
                                                insertCellNoValue(builder); insertStrikethroughText(builder, agentProperty.StringDefaultValue);
                                                break;

                                            default:
                                                break;
                                        }
                                        insertCellNoValue(builder); insertTextWithSize(builder, agentProperty.Description, 8);
                                        builder.EndRow();
                                    }

                                    finalizeTierAgentProperties(builder, tableTierAgentProperties);
                                }

                                #endregion

                                if (j % 10 == 0)
                                {
                                    Console.Write("[{0}].", j);
                                }
                                j++;
                            }

                            loggerConsole.Info("Completed {0} Tiers", tiersList.Count);
                            numEntitiesTotal = numEntitiesTotal + tiersList.Count;
                        }

                        builder.InsertBreak(BreakType.PageBreak);

                        #endregion

                        #region Nodes

                        insertHeading(builder, StyleIdentifier.Heading1, "Nodes", "Nodes", listHeadingsOutline, 0);

                        insertLinksToEntityTypeSections(builder);

                        if (nodesList != null)
                        {
                            loggerConsole.Info("Processing Nodes ({0} entities)", nodesList.Count);

                            #region Summary Table

                            insertHeading(builder, StyleIdentifier.Heading2, "Summary", String.Empty, listHeadingsOutline, 1);

                            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;

                            Table table = insertTableApplicationSummary(builder);
                            finalizeTableApplicationSummary(builder, table);

                            #endregion

                            int j = 0;

                            if (tiersList != null)
                            {
                                insertLinksToTiersInNodes(tiersList, builder, hyperLinkStyle);

                                foreach (APMTier tier in tiersList)
                                {
                                    insertHeading(builder, StyleIdentifier.Heading2, String.Format("{0} [{1}] ({2})", tier.TierName, tier.AgentType, tier.TierID), getShortenedEntityNameForWordBookmark("tiernode", tier.TierName, tier.TierID), listHeadingsOutline, 1);

                                    insertLinksToEntityTypeSections(builder);

                                    // Select Nodes for this Tier
                                    List<APMNode> nodesInTierList = nodesList.Where(n => n.TierID == tier.TierID).ToList();
                                    if (nodesInTierList != null)
                                    {
                                        insertLinksToNodes(nodesInTierList, builder, hyperLinkStyle);

                                        foreach (APMNode node in nodesInTierList)
                                        {
                                            insertHeading(builder, StyleIdentifier.Heading3, String.Format("{0} [{1}] ({2})", node.NodeName, node.AgentType, node.NodeID), getShortenedEntityNameForWordBookmark("node", node.NodeName, node.NodeID), listHeadingsOutline, 2);

                                            insertLinksToEntityTypeSections(builder);

                                            #region Summary Table

                                            insertHeading(builder, StyleIdentifier.Heading4, "Summary", String.Empty, listHeadingsOutline, 3);

                                            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;

                                            table = insertTableApplicationSummary(builder);
                                            finalizeTableApplicationSummary(builder, table);

                                            #endregion

                                            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;
                                            builder.Writeln(node.ToString());

                                            #region Dashboard

                                            insertHeading(builder, StyleIdentifier.Heading4, "Dashboard", String.Empty, listHeadingsOutline, 3);

                                            if (File.Exists(FilePathMap.EntityDashboardScreenshotReportFilePath(node, true, false)) == true)
                                            {
                                                builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;
                                                builder.InsertImage(FilePathMap.EntityDashboardScreenshotReportFilePath(node, true, false));
                                                builder.Writeln("");
                                            }
                                            else
                                            {
                                                builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;
                                                builder.Writeln("No dashboard captured");
                                            }

                                            #endregion

                                            #region Flowmap

                                            insertHeading(builder, StyleIdentifier.Heading4, "Flowmap in Grid Form", String.Empty, listHeadingsOutline, 3);

                                            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;

                                            Table tableNodeActivityGrid = insertTableActivityGrid(builder);

                                            if (nodesActivityFlowsList != null && nodesActivityFlowsList.Count > 0)
                                            {
                                                List<ActivityFlow> thisNodeAcvitityFlowsList = nodesActivityFlowsList.Where(a => a.NodeID == node.NodeID).ToList();
                                                foreach (ActivityFlow activityFlow in thisNodeAcvitityFlowsList)
                                                {
                                                    insertRowActivityGrid(builder, activityFlow);
                                                }
                                            }

                                            finalizeTableActivityGrid(builder, tableNodeActivityGrid);

                                            #endregion

                                            if (j % 10 == 0)
                                            {
                                                Console.Write("[{0}].", j);
                                            }
                                            j++;
                                        }
                                    }
                                }
                            }

                            loggerConsole.Info("Completed {0} Nodes", nodesList.Count);
                            numEntitiesTotal = numEntitiesTotal + nodesList.Count;
                        }

                        builder.InsertBreak(BreakType.PageBreak);

                        #endregion

                        #region Business Transactions

                        insertHeading(builder, StyleIdentifier.Heading1, "Business Transactions", "Business_Transactions", listHeadingsOutline, 0);

                        insertLinksToEntityTypeSections(builder);

                        if (businessTransactionsList != null)
                        {
                            loggerConsole.Info("Processing Business Transactions ({0} entities)", businessTransactionsList.Count);

                            #region Summary Table

                            insertHeading(builder, StyleIdentifier.Heading2, "Summary", String.Empty, listHeadingsOutline, 1);

                            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;

                            Table table = insertTableApplicationSummary(builder);
                            finalizeTableApplicationSummary(builder, table);

                            #endregion

                            int j = 0;

                            if (tiersList != null)
                            {
                                insertLinksToTiersInBusinessTransactions(tiersList, builder, hyperLinkStyle);

                                foreach (APMTier tier in tiersList)
                                {
                                    insertHeading(builder, StyleIdentifier.Heading2, String.Format("{0} [{1}] ({2})", tier.TierName, tier.AgentType, tier.TierID), getShortenedEntityNameForWordBookmark("tierbt", tier.TierName, tier.TierID), listHeadingsOutline, 1);

                                    insertLinksToEntityTypeSections(builder);

                                    // Select Business Transactions for this Tier
                                    List<APMBusinessTransaction> businessTransactionsInTierList = businessTransactionsList.Where(n => n.TierID == tier.TierID).ToList();
                                    if (businessTransactionsInTierList != null)
                                    {
                                        insertLinksToBusinessTransactions(businessTransactionsInTierList, builder, hyperLinkStyle);

                                        foreach (APMBusinessTransaction businessTransaction in businessTransactionsInTierList)
                                        {
                                            insertHeading(builder, StyleIdentifier.Heading3, String.Format("{0} [{1}] ({2})", businessTransaction.BTName, businessTransaction.BTType, businessTransaction.BTID), getShortenedEntityNameForWordBookmark("bt", businessTransaction.BTName, businessTransaction.BTID), listHeadingsOutline, 2);

                                            insertLinksToEntityTypeSections(builder);

                                            #region Summary Table

                                            insertHeading(builder, StyleIdentifier.Heading4, "Summary", String.Empty, listHeadingsOutline, 3);

                                            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;

                                            table = insertTableApplicationSummary(builder);
                                            finalizeTableApplicationSummary(builder, table);

                                            #endregion

                                            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;
                                            builder.Writeln(businessTransaction.ToString());

                                            #region Dashboard

                                            insertHeading(builder, StyleIdentifier.Heading4, "Dashboard", String.Empty, listHeadingsOutline, 3);

                                            if (File.Exists(FilePathMap.EntityDashboardScreenshotReportFilePath(businessTransaction, true, false)) == true)
                                            {
                                                builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;
                                                builder.InsertImage(FilePathMap.EntityDashboardScreenshotReportFilePath(businessTransaction, true, false));
                                                builder.Writeln("");
                                            }
                                            else
                                            {
                                                builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;
                                                builder.Writeln("No dashboard captured");
                                            }

                                            #endregion

                                            #region Flowmap

                                            insertHeading(builder, StyleIdentifier.Heading4, "Flowmap in Grid Form", String.Empty, listHeadingsOutline, 3);

                                            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;

                                            Table tableBusinessTransactionActivityGrid = insertTableActivityGrid(builder);

                                            if (businessTransactionsActivityFlowsList != null && businessTransactionsActivityFlowsList.Count > 0)
                                            {
                                                List<ActivityFlow> thisBusinessTransactionAcvitityFlowsList = businessTransactionsActivityFlowsList.Where(a => a.BTID == businessTransaction.BTID).ToList();
                                                foreach (ActivityFlow activityFlow in thisBusinessTransactionAcvitityFlowsList)
                                                {
                                                    insertRowActivityGrid(builder, activityFlow);
                                                }
                                            }

                                            finalizeTableActivityGrid(builder, tableBusinessTransactionActivityGrid);

                                            #endregion

                                            if (j % 10 == 0)
                                            {
                                                Console.Write("[{0}].", j);
                                            }
                                            j++;
                                        }
                                    }
                                }
                            }

                            loggerConsole.Info("Completed {0} Business Transactions", businessTransactionsList.Count);
                            numEntitiesTotal = numEntitiesTotal + businessTransactionsList.Count;
                        }

                        builder.InsertBreak(BreakType.PageBreak);

                        #endregion

                        #region Backends

                        insertHeading(builder, StyleIdentifier.Heading1, "Backends", "Backends", listHeadingsOutline, 0);

                        insertLinksToEntityTypeSections(builder);

                        if (backendsList != null)
                        {
                            loggerConsole.Info("Processing Backends ({0} entities)", backendsList.Count);

                            insertLinksToBackends(backendsList, builder, hyperLinkStyle);

                            #region Summary Table

                            insertHeading(builder, StyleIdentifier.Heading2, "Summary", String.Empty, listHeadingsOutline, 1);

                            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;

                            Table table = insertTableApplicationSummary(builder);
                            finalizeTableApplicationSummary(builder, table);

                            #endregion

                            int j = 0;

                            foreach (APMBackend backend in backendsList)
                            {
                                insertHeading(builder, StyleIdentifier.Heading2, String.Format("{0} [{1}] ({2})", backend.BackendName, backend.BackendType, backend.BackendID), getShortenedEntityNameForWordBookmark("back", backend.BackendName, backend.BackendID), listHeadingsOutline, 1);

                                insertLinksToEntityTypeSections(builder);

                                #region Summary Table

                                insertHeading(builder, StyleIdentifier.Heading3, "Summary", String.Empty, listHeadingsOutline, 2);

                                builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;

                                table = insertTableApplicationSummary(builder);
                                finalizeTableApplicationSummary(builder, table);

                                #endregion

                                builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;
                                builder.Writeln(backend.ToString());

                                #region Dashboard

                                insertHeading(builder, StyleIdentifier.Heading3, "Dashboard", String.Empty, listHeadingsOutline, 2);

                                if (File.Exists(FilePathMap.EntityDashboardScreenshotReportFilePath(backend, true, false)) == true)
                                {
                                    builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;
                                    builder.InsertImage(FilePathMap.EntityDashboardScreenshotReportFilePath(backend, true, false));
                                    builder.Writeln("");
                                }
                                else
                                {
                                    builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;
                                    builder.Writeln("No dashboard captured");
                                }

                                #endregion

                                #region Flowmap

                                insertHeading(builder, StyleIdentifier.Heading3, "Flowmap in Grid Form", String.Empty, listHeadingsOutline, 2);

                                builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;

                                Table tableBackendActivityGrid = insertTableActivityGrid(builder);

                                if (backendsActivityFlowsList != null && backendsActivityFlowsList.Count > 0)
                                {
                                    List<ActivityFlow> thisBackendAcvitityFlowsList = backendsActivityFlowsList.Where(a => a.BackendID == backend.BackendID).ToList();
                                    foreach (ActivityFlow activityFlow in thisBackendAcvitityFlowsList)
                                    {
                                        insertRowActivityGrid(builder, activityFlow);
                                    }
                                }

                                finalizeTableActivityGrid(builder, tableBackendActivityGrid);

                                #endregion

                                if (j % 10 == 0)
                                {
                                    Console.Write("[{0}].", j);
                                }
                                j++;
                            }

                            loggerConsole.Info("Completed {0} Backends", backendsList.Count);
                            numEntitiesTotal = numEntitiesTotal + backendsList.Count;
                        }

                        builder.InsertBreak(BreakType.PageBreak);

                        #endregion

                        #region Service Endpoints

                        insertHeading(builder, StyleIdentifier.Heading1, "Service Endpoints", "Service_Endpoints", listHeadingsOutline, 0);

                        insertLinksToEntityTypeSections(builder);

                        if (serviceEndpointsList != null)
                        {
                            loggerConsole.Info("Processing Service Endpoints ({0} entities)", serviceEndpointsList.Count);

                            #region Summary Table

                            insertHeading(builder, StyleIdentifier.Heading2, "Summary", String.Empty, listHeadingsOutline, 1);

                            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;

                            Table table = insertTableApplicationSummary(builder);
                            finalizeTableApplicationSummary(builder, table);

                            #endregion

                            int j = 0;

                            if (tiersList != null)
                            {
                                insertLinksToTiersInServiceEndpoints(tiersList, builder, hyperLinkStyle);

                                foreach (APMTier tier in tiersList)
                                {
                                    insertHeading(builder, StyleIdentifier.Heading2, String.Format("{0} [{1}] ({2})", tier.TierName, tier.AgentType, tier.TierID), getShortenedEntityNameForWordBookmark("tiersep", tier.TierName, tier.TierID), listHeadingsOutline, 1);

                                    insertLinksToEntityTypeSections(builder);

                                    // Select Service Endpoints for this Tier
                                    List<APMServiceEndpoint> serviceEndpointsInTierList = serviceEndpointsList.Where(n => n.TierID == tier.TierID).ToList();
                                    if (serviceEndpointsInTierList != null)
                                    {
                                        insertLinksToServiceEndpoints(serviceEndpointsInTierList, builder, hyperLinkStyle);

                                        foreach (APMServiceEndpoint serviceEndpoint in serviceEndpointsInTierList)
                                        {
                                            insertHeading(builder, StyleIdentifier.Heading3, String.Format("{0} [{1}] ({2})", serviceEndpoint.SEPName, serviceEndpoint.SEPType, serviceEndpoint.SEPID), getShortenedEntityNameForWordBookmark("sep", serviceEndpoint.SEPName, serviceEndpoint.SEPID), listHeadingsOutline, 2);

                                            insertLinksToEntityTypeSections(builder);

                                            #region Summary Table

                                            insertHeading(builder, StyleIdentifier.Heading4, "Summary", String.Empty, listHeadingsOutline, 3);

                                            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;

                                            table = insertTableApplicationSummary(builder);
                                            finalizeTableApplicationSummary(builder, table);

                                            #endregion

                                            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;
                                            builder.Writeln(serviceEndpoint.ToString());

                                            if (j % 10 == 0)
                                            {
                                                Console.Write("[{0}].", j);
                                            }
                                            j++;
                                        }
                                    }
                                }
                            }

                            loggerConsole.Info("Completed {0} Service Endpoints", serviceEndpointsList.Count);
                            numEntitiesTotal = numEntitiesTotal + serviceEndpointsList.Count;
                        }

                        builder.InsertBreak(BreakType.PageBreak);

                        #endregion

                        #region Errors

                        insertHeading(builder, StyleIdentifier.Heading1, "Errors", "Errors", listHeadingsOutline, 0);

                        insertLinksToEntityTypeSections(builder);

                        if (errorsList != null)
                        {
                            loggerConsole.Info("Processing Errors ({0} entities)", errorsList.Count);

                            #region Summary Table

                            insertHeading(builder, StyleIdentifier.Heading2, "Summary", String.Empty, listHeadingsOutline, 1);

                            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;

                            Table table = insertTableApplicationSummary(builder);
                            finalizeTableApplicationSummary(builder, table);

                            #endregion

                            int j = 0;

                            if (tiersList != null)
                            {
                                insertLinksToTiersInErrors(tiersList, builder, hyperLinkStyle);

                                foreach (APMTier tier in tiersList)
                                {
                                    insertHeading(builder, StyleIdentifier.Heading2, String.Format("{0} [{1}] ({2})", tier.TierName, tier.AgentType, tier.TierID), getShortenedEntityNameForWordBookmark("tiererr", tier.TierName, tier.TierID), listHeadingsOutline, 1);

                                    insertLinksToEntityTypeSections(builder);

                                    // Select Errors for this Tier
                                    List<APMError> errorsInTierList = errorsList.Where(n => n.TierID == tier.TierID).ToList();
                                    if (errorsInTierList != null)
                                    {
                                        insertLinksToErrors(errorsInTierList, builder, hyperLinkStyle);

                                        foreach (APMError error in errorsInTierList)
                                        {
                                            insertHeading(builder, StyleIdentifier.Heading3, String.Format("{0} [{1}] {2}", error.ErrorName, error.ErrorType, error.ErrorID), getShortenedEntityNameForWordBookmark("error", error.ErrorName, error.ErrorID), listHeadingsOutline, 2);

                                            insertLinksToEntityTypeSections(builder);

                                            #region Summary Table

                                            insertHeading(builder, StyleIdentifier.Heading4, "Summary", String.Empty, listHeadingsOutline, 3);

                                            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;

                                            table = insertTableApplicationSummary(builder);
                                            finalizeTableApplicationSummary(builder, table);

                                            #endregion

                                            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;
                                            builder.Writeln(error.ToString());

                                            if (j % 10 == 0)
                                            {
                                                Console.Write("[{0}].", j);
                                            }
                                            j++;
                                        }
                                    }
                                }
                            }

                            loggerConsole.Info("Completed {0} Errors", errorsList.Count);
                            numEntitiesTotal = numEntitiesTotal + errorsList.Count;
                        }

                        builder.InsertBreak(BreakType.PageBreak);

                        #endregion

                        #region Information Points

                        insertHeading(builder, StyleIdentifier.Heading1, "Information Points", "Information_Points", listHeadingsOutline, 0);

                        insertLinksToEntityTypeSections(builder);

                        if (informationPointsList != null)
                        {
                            loggerConsole.Info("Processing Information Points ({0} entities)", informationPointsList.Count);

                            insertLinksToInformationPoints(informationPointsList, builder, hyperLinkStyle);

                            #region Summary Table

                            insertHeading(builder, StyleIdentifier.Heading2, "Summary", String.Empty, listHeadingsOutline, 1);

                            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;

                            Table table = insertTableApplicationSummary(builder);
                            finalizeTableApplicationSummary(builder, table);

                            #endregion

                            int j = 0;

                            foreach (APMInformationPoint informationPoint in informationPointsList)
                            {
                                insertHeading(builder, StyleIdentifier.Heading2, String.Format("{0} [{1}] ({2})", informationPoint.IPName, informationPoint.IPType, informationPoint.IPID), getShortenedEntityNameForWordBookmark("ip", informationPoint.IPName, informationPoint.IPID), listHeadingsOutline, 1);

                                insertLinksToEntityTypeSections(builder);

                                #region Summary Table

                                insertHeading(builder, StyleIdentifier.Heading3, "Summary", String.Empty, listHeadingsOutline, 2);

                                builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;

                                table = insertTableApplicationSummary(builder);
                                finalizeTableApplicationSummary(builder, table);

                                #endregion

                                builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;
                                builder.Writeln(informationPoint.ToString());

                                if (j % 10 == 0)
                                {
                                    Console.Write("[{0}].", j);
                                }
                                j++;
                            }

                            loggerConsole.Info("Completed {0} Information Points", informationPointsList.Count);
                            numEntitiesTotal = numEntitiesTotal + informationPointsList.Count;
                        }

                        builder.InsertBreak(BreakType.PageBreak);

                        #endregion

                        // Save the final document
                        finalizeAndSaveApplicationSummaryReport(applicationSummaryDocument, FilePathMap.ApplicationSummaryWordReportFilePath(jobTarget, jobConfiguration.Input.TimeRange, true));

                        #endregion

                        stepTimingTarget.NumEntities = numEntitiesTotal;
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
            logger.Trace("LicensedReports.ApplicationSummary={0}", programOptions.LicensedReports.ApplicationSummary);
            loggerConsole.Trace("LicensedReports.ApplicationSummary={0}", programOptions.LicensedReports.ApplicationSummary);
            if (programOptions.LicensedReports.ApplicationSummary == false)
            {
                loggerConsole.Warn("Not licensed for application summary");
                return false;
            }

            logger.Trace("Output.ApplicationSummary={0}", jobConfiguration.Output.ApplicationSummary);
            loggerConsole.Trace("Output.ApplicationSummary={0}", jobConfiguration.Output.ApplicationSummary);
            if (jobConfiguration.Output.ApplicationSummary == false)
            {
                loggerConsole.Trace("Skipping report of application summary");
            }
            return (jobConfiguration.Output.ApplicationSummary == true);
        }

        #region Document prep and saving

        private Document createApplicationSummaryDocument(ProgramOptions programOptions, JobConfiguration jobConfiguration, JobTarget jobTarget)
        {
            Document doc = new Document();
            DocumentBuilder builder = new DocumentBuilder(doc);

            #region Document properties

            //Author : Test Author
            //Category : Test Category
            //Characters : 586
            //CharactersWithSpaces : 687
            //Comments : Test Comments
            //Company : Test Company
            //CreateTime : 4/25/2006 11:10:00 AM
            //HeadingPairs : System.Object[]
            //HyperlinkBase : Test Hyperlink
            //Keywords : Test Keywords
            //LastSavedBy : RK
            //LastSavedTime : 4/25/2006 11:11:00 AM
            //Lines : 4
            //LinksUpToDate : False
            //Manager : Test Manager
            //NameOfApplication : Microsoft Office Word
            //Pages : 1
            //Paragraphs : 1
            //RevisionNumber : 3
            //Security : 0
            //Subject : Test Subject
            //Template : Normal.dot
            //Title : Test Title
            //TitlesOfParts : System.String[]
            //TotalEditingTime : 1
            //Version : 727464
            //Words : 102

            doc.BuiltInDocumentProperties["Author"].Value = String.Format("AppDynamics DEXTER {0}", Assembly.GetEntryAssembly().GetName().Version);
            doc.BuiltInDocumentProperties["Title"].Value = "AppDynamics DEXTER Application Summary Report";
            doc.BuiltInDocumentProperties["Subject"].Value = programOptions.JobName;

            #endregion

            #region Theme

            Theme theme = doc.Theme;
            //theme.MinorFonts.Latin = "Calibri";

            #endregion

            #region Styles

            // Default document font size
            doc.Styles[StyleIdentifier.Normal].Font.Name = "Calibri";
            doc.Styles[StyleIdentifier.Normal].Font.Size = 11;

            // Hyperlink style
            Style linkStyle = doc.Styles.Add(StyleType.Character, "HyperLinkStyle");
            linkStyle.Font.Color = Color.Blue;
            linkStyle.Font.Underline = Underline.Single;

            #endregion

            #region Headers and Footers

            Section currentSection = builder.CurrentSection;
            PageSetup pageSetup = currentSection.PageSetup;

            pageSetup.PaperSize = PaperSize.A3;
            pageSetup.Orientation = Orientation.Landscape;

            // Specify if we want headers/footers of the first page to be different from other pages.
            // You can also use PageSetup.OddAndEvenPagesHeaderFooter property to specify
            // Different headers/footers for odd and even pages.
            pageSetup.DifferentFirstPageHeaderFooter = true;

            // --- Create header for the first page. ---
            pageSetup.HeaderDistance = 20;
            builder.MoveToHeaderFooter(HeaderFooterType.HeaderFirst);
            builder.ParagraphFormat.Alignment = ParagraphAlignment.Center;

            // Set font properties for header text.
            // builder.Font.Name = "Arial";
            // builder.Font.Bold = true;
            builder.Font.Size = 12;
            // Specify header title for the first page.
            builder.Write("AppDynamics DEXTER Application Summary Report");

            // --- Create header for pages other than first. ---
            pageSetup.HeaderDistance = 20;
            builder.MoveToHeaderFooter(HeaderFooterType.HeaderPrimary);

            builder.Write("AppDynamics DEXTER Application Summary Report");

            // --- Create footer for pages other than first. ---
            builder.MoveToHeaderFooter(HeaderFooterType.FooterPrimary);

            // We use table with two cells to make one part of the text on the line (with page numbering)
            // To be aligned left, and the other part of the text (with copyright) to be aligned right.
            builder.StartTable();

            // Clear table borders.
            builder.CellFormat.ClearFormatting();

            builder.InsertCell();

            // Set first cell to 1/3 of the page width.
            builder.CellFormat.PreferredWidth = PreferredWidth.FromPercent(100 / 3);

            // Insert page numbering text here.
            // It uses PAGE and NUMPAGES fields to auto calculate current page number and total number of pages.
            builder.Write("Page ");
            builder.InsertField("PAGE", "");
            builder.Write(" of ");
            builder.InsertField("NUMPAGES", "");

            // Align this text to the left.
            builder.CurrentParagraph.ParagraphFormat.Alignment = ParagraphAlignment.Left;

            builder.InsertCell();
            // Set the second cell to 2/3 of the page width.
            builder.CellFormat.PreferredWidth = PreferredWidth.FromPercent(100 * 2 / 3);

            builder.Write(String.Format("{0}/{1} ({2})", jobTarget.Controller, jobTarget.Application, jobTarget.ApplicationID));

            // Align this text to the right.
            builder.CurrentParagraph.ParagraphFormat.Alignment = ParagraphAlignment.Right;

            builder.EndRow();
            builder.EndTable();

            #endregion

            #region Title page

            builder.MoveToDocumentStart();

            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Title;
            builder.Writeln("AppDynamics DEXTER Application Summary Report");

            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;
            builder.Writeln("");
            builder.Writeln("");
            builder.Writeln("");
            builder.Writeln("");
            builder.Writeln("");

            builder.Writeln(String.Format("{0} ({1})", jobTarget.Controller, jobTarget.ControllerVersion));
            builder.Writeln(String.Format("{0} ({1})", jobTarget.Application, jobTarget.ApplicationID));
            builder.Writeln(String.Format("{0:G}-{1:G} ({2})", jobConfiguration.Input.TimeRange.From.ToLocalTime(), jobConfiguration.Input.TimeRange.To.ToLocalTime(), TimeZoneInfo.Local.DisplayName));
            builder.Writeln(String.Format("{0:G}-{1:G} (UTC)", jobConfiguration.Input.TimeRange.From.ToLocalTime(), jobConfiguration.Input.TimeRange.To.ToLocalTime()));

            builder.Writeln("");

            builder.Writeln(String.Format("Version {0}", Assembly.GetEntryAssembly().GetName().Version));

            builder.InsertBreak(BreakType.PageBreak);

            #endregion

            #region Table of Contents

            // Insert a table of contents at the beginning of the document.

            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Heading1;
            builder.Writeln("Table of Contents");

            builder.ListFormat.List = null;
            builder.InsertTableOfContents("\\o \"1-3\" \\h \\z \\u");

            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;
            builder.Writeln("");

            builder.InsertBreak(BreakType.PageBreak);

            #endregion

            return doc;
        }

        private static bool finalizeAndSaveApplicationSummaryReport(Document doc, string reportFilePath)
        {
            logger.Info("Finalize Application Summary Report File {0}", reportFilePath);

            #region Update fields

            doc.UpdateFields();

            #endregion

            #region Save file 

            // Report files
            logger.Info("Saving report {0}", reportFilePath);
            loggerConsole.Info("Saving report {0}", reportFilePath);

            FileIOHelper.CreateFolderForFile(reportFilePath);

            try
            {
                // Save full report Word file
                SaveOutputParameters saveResults = doc.Save(reportFilePath, SaveFormat.Docx);
            }
            catch (Exception ex)
            {
                logger.Warn("Unable to save Word file {0}", reportFilePath);
                logger.Warn(ex);
                loggerConsole.Warn("Unable to save Word file {0}", reportFilePath);

                return false;
            }

            //string reportFilePDFPath = reportFilePath.Replace(".docx", ".pdf");
            //logger.Info("Saving report {0}", reportFilePDFPath);
            //loggerConsole.Info("Saving report {0}", reportFilePDFPath);
            //try
            //{
            //    // Save full report Word file
            //    SaveOutputParameters saveResults = doc.Save(reportFilePDFPath, SaveFormat.Pdf);
            //}
            //catch (Exception ex)
            //{
            //    logger.Warn("Unable to save PDF file {0}", reportFilePDFPath);
            //    logger.Warn(ex);
            //    loggerConsole.Warn("Unable to save PDF file {0}", reportFilePDFPath);

            //    return false;
            //}

            //string reportFileHTMLPath = reportFilePath.Replace(".docx", ".html");
            //logger.Info("Saving report {0}", reportFileHTMLPath);
            //loggerConsole.Info("Saving report {0}", reportFileHTMLPath);
            //try
            //{
            //    // Save full report Word file
            //    SaveOutputParameters saveResults = doc.Save(reportFileHTMLPath, SaveFormat.Html);
            //}
            //catch (Exception ex)
            //{
            //    logger.Warn("Unable to save PDF file {0}", reportFileHTMLPath);
            //    logger.Warn(ex);
            //    loggerConsole.Warn("Unable to save PDF file {0}", reportFileHTMLPath);

            //    return false;
            //}

            #endregion

            return true;
        }

        #endregion

        private static void insertHeading(DocumentBuilder builder, StyleIdentifier headingStyle, string headingName, string headingBookmarkName, List listHeadingsOutline, int listNumberingOutlineLevel)
        {
            builder.ParagraphFormat.StyleIdentifier = headingStyle;
            if (listHeadingsOutline != null)
            {
                builder.ListFormat.List = listHeadingsOutline;
                builder.ListFormat.ListLevelNumber = listNumberingOutlineLevel;
            }
            if (headingBookmarkName.Length > 0)
            {
                builder.StartBookmark(headingBookmarkName);
            }
            builder.Writeln(headingName);
            if (headingBookmarkName.Length > 0)
            {
                builder.EndBookmark(headingBookmarkName);
            }
            if (listHeadingsOutline != null)
            {
                builder.ListFormat.List = null;
            }
        }

        private static void insertLinkToURL(DocumentBuilder builder, Style hyperLinkStyle, string displayName, string linkURL)
        {
            builder.Font.Style = hyperLinkStyle;
            builder.InsertHyperlink(displayName, linkURL, false);
            builder.Font.ClearFormatting();
        }

        private static void insertLinkToBookmark(DocumentBuilder builder, Style hyperLinkStyle, string displayName, string linkBookmark)
        {
            builder.Font.Style = hyperLinkStyle;
            builder.InsertHyperlink(displayName, linkBookmark, true);
            builder.Font.ClearFormatting();
        }

        #region Insertion of links to various entities

        private static void insertLinksToEntityTypeSections(DocumentBuilder builder)
        {
            Style hyperLinkStyle = builder.Document.Styles["HyperLinkStyle"];

            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;

            builder.Write("Jump to: ");

            insertLinkToBookmark(builder, hyperLinkStyle, "Application", "Application"); builder.Write(", ");
            insertLinkToBookmark(builder, hyperLinkStyle, "Tiers", "Tiers"); builder.Write(", ");
            insertLinkToBookmark(builder, hyperLinkStyle, "Nodes", "Nodes"); builder.Write(", ");
            insertLinkToBookmark(builder, hyperLinkStyle, "Business Transactions", "Business_Transactions"); builder.Write(", ");
            insertLinkToBookmark(builder, hyperLinkStyle, "Backends", "Backends"); builder.Write(", ");
            insertLinkToBookmark(builder, hyperLinkStyle, "Service Endpoints", "Service_Endpoints"); builder.Write(", ");
            insertLinkToBookmark(builder, hyperLinkStyle, "Errors", "Errors"); builder.Write(", ");
            insertLinkToBookmark(builder, hyperLinkStyle, "Information Points", "Information_Points");

            builder.Writeln("");
        }

        private static void insertLinksToTiers(List<APMTier> tiersList, DocumentBuilder builder, Style hyperLinkStyle, string bookmarkPrefix)
        {
            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;
            builder.Write("Jump to: ");
            for (int i = 0; i < tiersList.Count; i++)
            {
                APMTier tier = tiersList[i];

                insertLinkToBookmark(builder, hyperLinkStyle, tier.TierName, getShortenedEntityNameForWordBookmark(bookmarkPrefix, tier.TierName, tier.TierID));

                if (i < tiersList.Count - 1)
                {
                    builder.Write(", ");
                }
            }
            builder.Writeln("");
        }

        private static void insertLinksToTiers(List<APMTier> tiersList, DocumentBuilder builder, Style hyperLinkStyle)
        {
            insertLinksToTiers(tiersList, builder, hyperLinkStyle, "tier");
        }

        private static void insertLinksToTiersInNodes(List<APMTier> tiersList, DocumentBuilder builder, Style hyperLinkStyle)
        {
            insertLinksToTiers(tiersList, builder, hyperLinkStyle, "tiernode");
        }

        private static void insertLinksToTiersInBusinessTransactions(List<APMTier> tiersList, DocumentBuilder builder, Style hyperLinkStyle)
        {
            insertLinksToTiers(tiersList, builder, hyperLinkStyle, "tierbt");
        }

        private static void insertLinksToTiersInServiceEndpoints(List<APMTier> tiersList, DocumentBuilder builder, Style hyperLinkStyle)
        {
            insertLinksToTiers(tiersList, builder, hyperLinkStyle, "tiersep");
        }

        private static void insertLinksToTiersInErrors(List<APMTier> tiersList, DocumentBuilder builder, Style hyperLinkStyle)
        {
            insertLinksToTiers(tiersList, builder, hyperLinkStyle, "tiererr");
        }

        private static void insertLinksToNodes(List<APMNode> nodesList, DocumentBuilder builder, Style hyperLinkStyle)
        {
            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;
            builder.Write("Jump to: ");
            for (int i = 0; i < nodesList.Count; i++)
            {
                APMNode node = nodesList[i];

                insertLinkToBookmark(builder, hyperLinkStyle, node.NodeName, getShortenedEntityNameForWordBookmark("node", node.NodeName, node.NodeID));

                if (i < nodesList.Count - 1)
                {
                    builder.Write(", ");
                }
            }
            builder.Writeln("");
        }

        private static void insertLinksToBusinessTransactions(List<APMBusinessTransaction> businessTransactionsList, DocumentBuilder builder, Style hyperLinkStyle)
        {
            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;
            builder.Write("Jump to: ");
            for (int i = 0; i < businessTransactionsList.Count; i++)
            {
                APMBusinessTransaction businessTransaction = businessTransactionsList[i];

                insertLinkToBookmark(builder, hyperLinkStyle, businessTransaction.BTName, getShortenedEntityNameForWordBookmark("bt", businessTransaction.BTName, businessTransaction.BTID));

                if (i < businessTransactionsList.Count - 1)
                {
                    builder.Write(", ");
                }
            }
            builder.Writeln("");
        }

        private static void insertLinksToBackends(List<APMBackend> backendsList, DocumentBuilder builder, Style hyperLinkStyle)
        {
            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;
            builder.Write("Jump to: ");
            for (int i = 0; i < backendsList.Count; i++)
            {
                APMBackend backend = backendsList[i];

                insertLinkToBookmark(builder, hyperLinkStyle, backend.BackendName, getShortenedEntityNameForWordBookmark("back", backend.BackendName, backend.BackendID));

                if (i < backendsList.Count - 1)
                {
                    builder.Write(", ");
                }
            }
            builder.Writeln("");
        }

        private static void insertLinksToServiceEndpoints(List<APMServiceEndpoint> serviceEndpointsList, DocumentBuilder builder, Style hyperLinkStyle)
        {
            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;
            builder.Write("Jump to: ");
            for (int i = 0; i < serviceEndpointsList.Count; i++)
            {
                APMServiceEndpoint serviceEndpoint = serviceEndpointsList[i];

                insertLinkToBookmark(builder, hyperLinkStyle, serviceEndpoint.SEPName, getShortenedEntityNameForWordBookmark("sep", serviceEndpoint.SEPName, serviceEndpoint.SEPID));

                if (i < serviceEndpointsList.Count - 1)
                {
                    builder.Write(", ");
                }
            }
            builder.Writeln("");
        }

        private static void insertLinksToErrors(List<APMError> errorsList, DocumentBuilder builder, Style hyperLinkStyle)
        {
            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;
            builder.Write("Jump to: ");
            for (int i = 0; i < errorsList.Count; i++)
            {
                APMError error = errorsList[i];

                insertLinkToBookmark(builder, hyperLinkStyle, error.ErrorName, getShortenedEntityNameForWordBookmark("error", error.ErrorName, error.ErrorID));

                if (i < errorsList.Count - 1)
                {
                    builder.Write(", ");
                }
            }
            builder.Writeln("");
        }

        private static void insertLinksToInformationPoints(List<APMInformationPoint> informationPointsList, DocumentBuilder builder, Style hyperLinkStyle)
        {
            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;
            builder.Write("Jump to: ");
            for (int i = 0; i < informationPointsList.Count; i++)
            {
                APMInformationPoint informationPoint = informationPointsList[i];

                insertLinkToBookmark(builder, hyperLinkStyle, informationPoint.IPName, getShortenedEntityNameForWordBookmark("ip", informationPoint.IPName, informationPoint.IPID));
                if (i < informationPointsList.Count - 1)
                {
                    builder.Write(", ");
                }
            }
            builder.Writeln("");
        }

        #endregion

        #region Helper functions for creation of various tables

        internal static Table insertTableApplicationSummary(DocumentBuilder builder)
        {
            Table table = builder.StartTable();

            Cell cell = insertCellStringValue(builder, "Property");
            cell.CellFormat.PreferredWidth = PreferredWidth.FromPoints(100);
            cell = insertCellStringValue(builder, "Value");
            cell.CellFormat.PreferredWidth = PreferredWidth.FromPoints(400);
            cell = insertCellStringValue(builder, "Detail");
            cell.CellFormat.PreferredWidth = PreferredWidth.FromPoints(300);

            builder.EndRow();

            return table;
        }

        internal static void finalizeTableApplicationSummary(DocumentBuilder builder, Table table)
        {
            finalizeTableStyleAndResize(builder, table);
        }

        internal static Table insertTableApplicationEntityTypes(DocumentBuilder builder)
        {
            Table table = builder.StartTable();

            Cell cell = insertCellStringValue(builder, "Property");
            cell.CellFormat.PreferredWidth = PreferredWidth.FromPoints(100);
            cell = insertCellStringValue(builder, "Types");
            cell.CellFormat.PreferredWidth = PreferredWidth.FromPoints(400);
            cell = insertCellStringValue(builder, "Types Chart");
            cell.CellFormat.PreferredWidth = PreferredWidth.FromPoints(300);
            cell = insertCellStringValue(builder, "Activity");
            cell.CellFormat.PreferredWidth = PreferredWidth.FromPoints(400);
            cell = insertCellStringValue(builder, "Activity Chart");
            cell.CellFormat.PreferredWidth = PreferredWidth.FromPoints(300);

            builder.EndRow();

            return table;
        }

        internal static void finalizeTableApplicationEntityTypes(DocumentBuilder builder, Table table)
        {
            finalizeTableStyleAndResize(builder, table);
        }

        internal static Table insertTableApplicationEntityMapped(DocumentBuilder builder)
        {
            Table table = builder.StartTable();

            Cell cell = insertCellStringValue(builder, "Property");
            cell.CellFormat.PreferredWidth = PreferredWidth.FromPoints(100);
            cell = insertCellStringValue(builder, "Value");
            cell.CellFormat.PreferredWidth = PreferredWidth.FromPoints(400);
            cell = insertCellStringValue(builder, "Chart");
            cell.CellFormat.PreferredWidth = PreferredWidth.FromPoints(300);
            cell = insertCellStringValue(builder, "Value");
            cell.CellFormat.PreferredWidth = PreferredWidth.FromPoints(400);
            cell = insertCellStringValue(builder, "Chart");
            cell.CellFormat.PreferredWidth = PreferredWidth.FromPoints(300);

            builder.EndRow();

            return table;
        }

        internal static void finalizeTableApplicationEntityMapped(DocumentBuilder builder, Table table)
        {
            finalizeTableStyleAndResize(builder, table);
        }

        internal static Table insertTableActivityGrid(DocumentBuilder builder)
        {
            Table table = builder.StartTable();

            Cell cell = insertCellStringValue(builder, "Type");
            cell = insertCellStringValue(builder, "Direction");
            cell = insertCellStringValue(builder, "From");
            cell = insertCellStringValue(builder, "From Type");
            cell = insertCellStringValue(builder, "To");
            cell = insertCellStringValue(builder, "To Type");

            cell = insertCellStringValue(builder, "ART");
            cell = insertCellStringValue(builder, "Calls");
            cell = insertCellStringValue(builder, "CPM");
            cell = insertCellStringValue(builder, "Errors");
            cell = insertCellStringValue(builder, "EPM");
            cell = insertCellStringValue(builder, "Errors %");

            builder.EndRow();

            return table;
        }

        internal static Row insertRowActivityGrid(DocumentBuilder builder, ActivityFlow activityFlow)
        {
            insertCellStringValue(builder, activityFlow.CallType);
            insertCellStringValue(builder, activityFlow.CallDirection);
            insertCellStringValue(builder, activityFlow.FromName);
            insertCellStringValue(builder, activityFlow.FromType);
            insertCellStringValue(builder, activityFlow.ToName);
            insertCellStringValue(builder, activityFlow.ToType);
            insertCellStringValue(builder, activityFlow.ART);
            insertCellStringValue(builder, activityFlow.Calls);
            insertCellStringValue(builder, activityFlow.CPM);
            insertCellStringValue(builder, activityFlow.Errors);
            insertCellStringValue(builder, activityFlow.EPM);
            insertCellStringValue(builder, activityFlow.ErrorsPercentage);
            Row row = builder.EndRow();
            return row;
        }

        internal static void finalizeTableActivityGrid(DocumentBuilder builder, Table table)
        {
            finalizeTableStyleAndResize(builder, table);
        }

        internal static Table insertTableTiersSummary(DocumentBuilder builder)
        {
            Table table = builder.StartTable();

            Cell cell = insertCellStringValue(builder, "Tier");
            cell = insertCellStringValue(builder, "Type");
            cell = insertCellStringValue(builder, "# APM Nodes");
            cell = insertCellStringValue(builder, "# Machine Nodes");
            cell = insertCellStringValue(builder, "# BTs");
            cell = insertCellStringValue(builder, "# SEPs");
            cell = insertCellStringValue(builder, "# Errors");
            cell = insertCellStringValue(builder, "Agent Avail");
            cell = insertCellStringValue(builder, "Machine Avail");
            cell = insertCellStringValue(builder, "TX Activity");
            cell = insertCellStringValue(builder, "# Custom Props");
            cell = insertCellStringValue(builder, "# Events");

            builder.EndRow();

            return table;
        }

        internal static void finalizeTableTiersSummary(DocumentBuilder builder, Table table)
        {
            finalizeTableStyleAndResize(builder, table);
        }

        internal static Table insertTableTierEntityTypes(DocumentBuilder builder)
        {
            Table table = builder.StartTable();

            Cell cell = insertCellStringValue(builder, "Property");
            cell.CellFormat.PreferredWidth = PreferredWidth.FromPoints(100);
            cell = insertCellStringValue(builder, "Types");
            cell.CellFormat.PreferredWidth = PreferredWidth.FromPoints(400);
            cell = insertCellStringValue(builder, "Types Chart");
            cell.CellFormat.PreferredWidth = PreferredWidth.FromPoints(300);
            cell = insertCellStringValue(builder, "Activity");
            cell.CellFormat.PreferredWidth = PreferredWidth.FromPoints(400);
            cell = insertCellStringValue(builder, "Activity Chart");
            cell.CellFormat.PreferredWidth = PreferredWidth.FromPoints(300);

            builder.EndRow();

            return table;
        }

        internal static void finalizeTableTierEntityTypes(DocumentBuilder builder, Table table)
        {
            finalizeTableStyleAndResize(builder, table);
        }

        internal static Table insertTableApplicationAgentProperties(DocumentBuilder builder)
        {
            Table table = builder.StartTable();

            Cell cell = insertCellStringValue(builder, "Scope");
            cell = insertCellStringValue(builder, "Agent Type");
            cell = insertCellStringValue(builder, "Name");
            cell = insertCellStringValue(builder, "Type");
            cell = insertCellStringValue(builder, "Value");
            cell = insertCellStringValue(builder, "Old Value");
            cell = insertCellStringValue(builder, "Description");

            builder.EndRow();

            return table;
        }

        internal static void finalizeApplicationAgentProperties(DocumentBuilder builder, Table table)
        {
            finalizeTableStyleAndResize(builder, table);
        }

        internal static Table insertTableTierAgentProperties(DocumentBuilder builder)
        {
            Table table = builder.StartTable();

            Cell cell = insertCellStringValue(builder, "Name");
            cell = insertCellStringValue(builder, "Type");
            cell = insertCellStringValue(builder, "Value");
            cell = insertCellStringValue(builder, "Old Value");
            cell = insertCellStringValue(builder, "Description");

            builder.EndRow();

            return table;
        }

        internal static void finalizeTierAgentProperties(DocumentBuilder builder, Table table)
        {
            finalizeTableStyleAndResize(builder, table);
        }

        internal static void finalizeTableStyleAndResize(DocumentBuilder builder, Table table)
        {
            table.StyleIdentifier = StyleIdentifier.ListTable1Light;
            table.AutoFit(AutoFitBehavior.AutoFitToContents);
            table.StyleOptions = TableStyleOptions.RowBands | TableStyleOptions.FirstRow;
            builder.EndTable();
        }

        #endregion

        internal static Cell insertCellNoValue(DocumentBuilder builder)
        {
            Cell cell = builder.InsertCell();
            return cell;
        }

        internal static Cell insertCellStringValue(DocumentBuilder builder, string cellContent)
        {
            Cell cell = insertCellNoValue(builder);
            builder.Write(cellContent);
            return cell;
        }

        internal static Cell insertCellStringValue(DocumentBuilder builder, object cellContent)
        {
            Cell cell = insertCellNoValue(builder);
            builder.Write(cellContent.ToString());
            return cell;
        }

        private static void insertStrikethroughText(DocumentBuilder builder, String textContent)
        {
            builder.Font.StrikeThrough = true;
            builder.Write(textContent);
            builder.Font.ClearFormatting();
        }

        private static void insertTextWithSize(DocumentBuilder builder, String textContent, int fontSize)
        {
            builder.Font.Size = fontSize;
            builder.Write(textContent);
            builder.Font.ClearFormatting();
        }

        internal static Shape insertPieChart(DocumentBuilder builder, string chartTitle, string[] categories, double[] values)
        {
            // If no data is passed, Aspose acts very cute and adds a mock Sales pie chart out of thin air. I'd rather have a blank one
            if (categories.Length == 0)
            {
                categories = new string[1];
                categories[0] = "";
                values = new double[1];
                values[0] = 0;
            }
            if (categories.Length > 0 && values.Length > 0)
            {
                Shape shape = builder.InsertChart(ChartType.Pie, 300, 130);
                Chart chart = shape.Chart;
                chart.Series.Clear();
                ChartSeries series = chart.Series.Add(chartTitle, categories, values);
                chart.Legend.Position = LegendPosition.Right;
                chart.Title.Overlay = true;
                chart.Title.Show = false;

                ChartDataLabelCollection labels = series.DataLabels;
                labels.ShowPercentage = true;
                labels.ShowValue = true;
                labels.ShowLeaderLines = false;
                labels.Separator = "-";

                return shape;
            }
            else
            {
                return null;
            }
        }

        internal void measureTypesOfItemsInGroupBy(IEnumerable<IGrouping<string, APMEntityBase>> groupTypesToMeasure, List<string> prettyFormatList, List<string> typesList, List<double> countsList)
        {
            foreach (var groupType in groupTypesToMeasure)
            {
                prettyFormatList.Add(String.Format("{0} {1}", groupType.Count(), groupType.Key));
                typesList.Add(groupType.Key);
                countsList.Add(groupType.Count());
            }
        }

        internal void measureTypesOfItemsInGroupBy(IEnumerable<IGrouping<string, object>> groupTypesToMeasure, List<string> prettyFormatList, List<string> typesList, List<double> countsList)
        {
            foreach (var groupType in groupTypesToMeasure)
            {
                prettyFormatList.Add(String.Format("{0} {1}", groupType.Count(), groupType.Key));
                typesList.Add(groupType.Key);
                countsList.Add(groupType.Count());
            }
        }
    }
}
