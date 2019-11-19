using AppDynamics.Dexter.ReportObjectMaps;
using AppDynamics.Dexter.ReportObjects;
using Aspose.Words;
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

            if (this.ShouldExecute(jobConfiguration) == false)
            {
                return true;
            }

            if (jobConfiguration.Target.Count(t => t.Type == APPLICATION_TYPE_APM) == 0)
            {
                return true;
            }

            logger.Info("Setting Aspose License");
            Aspose.Words.License license = new Aspose.Words.License();
            license.SetLicense("Aspose.Words.lic");

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
                        List<APMApplication> applicationsMetricsList = FileIOHelper.ReadListFromCSVFile<APMApplication>(FilePathMap.EntitiesFullIndexFilePath(jobTarget, APMApplication.ENTITY_FOLDER), new ApplicationMetricReportMap());

                        List<APMTier> tiersList = FileIOHelper.ReadListFromCSVFile<APMTier>(FilePathMap.APMTiersIndexFilePath(jobTarget), new APMTierReportMap());
                        List<APMTier> tiersMetricsList = FileIOHelper.ReadListFromCSVFile<APMTier>(FilePathMap.EntitiesFullIndexFilePath(jobTarget, APMTier.ENTITY_FOLDER), new TierMetricReportMap());

                        List<APMNode> nodesList = FileIOHelper.ReadListFromCSVFile<APMNode>(FilePathMap.APMNodesIndexFilePath(jobTarget), new APMNodeReportMap());
                        List<APMNode> nodesMetricsList = FileIOHelper.ReadListFromCSVFile<APMNode>(FilePathMap.EntitiesFullIndexFilePath(jobTarget, APMNode.ENTITY_FOLDER), new NodeMetricReportMap());

                        List<APMBusinessTransaction> businessTransactionsList = FileIOHelper.ReadListFromCSVFile<APMBusinessTransaction>(FilePathMap.APMBusinessTransactionsIndexFilePath(jobTarget), new APMBusinessTransactionReportMap());
                        List<APMBusinessTransaction> businessTransactionsMetricsList = FileIOHelper.ReadListFromCSVFile<APMBusinessTransaction>(FilePathMap.EntitiesFullIndexFilePath(jobTarget, APMBusinessTransaction.ENTITY_FOLDER), new BusinessTransactionMetricReportMap());

                        List<APMBackend> backendsList = FileIOHelper.ReadListFromCSVFile<APMBackend>(FilePathMap.APMBackendsIndexFilePath(jobTarget), new APMBackendReportMap());
                        List<APMBackend> backendsMetricsList = FileIOHelper.ReadListFromCSVFile<APMBackend>(FilePathMap.EntitiesFullIndexFilePath(jobTarget, APMBackend.ENTITY_FOLDER), new BackendMetricReportMap());

                        List<APMServiceEndpoint> serviceEndpointsList = FileIOHelper.ReadListFromCSVFile<APMServiceEndpoint>(FilePathMap.APMServiceEndpointsIndexFilePath(jobTarget), new APMServiceEndpointReportMap());
                        List<APMServiceEndpoint> serviceEndpointsMetricsList = FileIOHelper.ReadListFromCSVFile<APMServiceEndpoint>(FilePathMap.EntitiesFullIndexFilePath(jobTarget, APMServiceEndpoint.ENTITY_FOLDER), new ServiceEndpointMetricReportMap());

                        List<APMError> errorsList = FileIOHelper.ReadListFromCSVFile<APMError>(FilePathMap.APMErrorsIndexFilePath(jobTarget), new APMErrorReportMap());
                        List<APMError> errorsMetricsList = FileIOHelper.ReadListFromCSVFile<APMError>(FilePathMap.EntitiesFullIndexFilePath(jobTarget, APMError.ENTITY_FOLDER), new ErrorMetricReportMap());

                        List<APMInformationPoint> informationPointsList = FileIOHelper.ReadListFromCSVFile<APMInformationPoint>(FilePathMap.APMInformationPointsIndexFilePath(jobTarget), new APMInformationPointReportMap());
                        List<APMInformationPoint> informationPointsMetricsList = FileIOHelper.ReadListFromCSVFile<APMInformationPoint>(FilePathMap.EntitiesFullIndexFilePath(jobTarget, APMInformationPoint.ENTITY_FOLDER), new InformationPointMetricReportMap());

                        List<APMResolvedBackend> resolvedBackendsList = FileIOHelper.ReadListFromCSVFile<APMResolvedBackend>(FilePathMap.APMMappedBackendsIndexFilePath(jobTarget), new APMResolvedBackendReportMap());

                        //List<Event> eventsAllList = FileIOHelper.ReadListFromCSVFile<Event>(FilePathMap.ApplicationEventsIndexFilePath(jobTarget), new EventReportMap());
                        //List<HealthRuleViolationEvent> healthRuleViolationEventsAllList = FileIOHelper.ReadListFromCSVFile<HealthRuleViolationEvent>(FilePathMap.ApplicationHealthRuleViolationsIndexFilePath(jobTarget), new HealthRuleViolationEventReportMap());
                        //List<Snapshot> snapshotsAllList = FileIOHelper.ReadListFromCSVFile<Snapshot>(FilePathMap.SnapshotsIndexFilePath(jobTarget), new SnapshotReportMap());
                        //List<Segment> segmentsAllList = FileIOHelper.ReadListFromCSVFile<Segment>(FilePathMap.SnapshotsSegmentsIndexFilePath(jobTarget), new SegmentReportMap());
                        //List<ExitCall> exitCallsAllList = FileIOHelper.ReadListFromCSVFile<ExitCall>(FilePathMap.SnapshotsExitCallsIndexFilePath(jobTarget), new ExitCallReportMap());
                        //List<ServiceEndpointCall> serviceEndpointCallsAllList = FileIOHelper.ReadListFromCSVFile<ServiceEndpointCall>(FilePathMap.SnapshotsServiceEndpointCallsIndexFilePath(jobTarget), new ServiceEndpointCallReportMap());
                        //List<DetectedError> detectedErrorsAllList = FileIOHelper.ReadListFromCSVFile<DetectedError>(FilePathMap.SnapshotsDetectedErrorsIndexFilePath(jobTarget), new DetectedErrorReportMap());
                        //List<BusinessData> businessDataAllList = FileIOHelper.ReadListFromCSVFile<BusinessData>(FilePathMap.SnapshotsBusinessDataIndexFilePath(jobTarget), new BusinessDataReportMap());


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
                            Table table = insertNameValueTable(builder);
                            
                            insertNameValueRow(builder, table, "Controller", String.Format("{0}, Version {1} ({2})", application.Controller, controllerSummary.Version, controllerSummary.VersionDetail));
                            insertNameValueRow(builder, table, "Saas or OnPrem", application.Controller.ToLower().Contains("saas.appdynamics") ? "SaaS" : "OnPrem");
                            insertNameValueRow(builder, table, "Application", String.Format("{0} ({1}) {2}", application.ApplicationName, application.ApplicationID, application.Description));
                            
                            StringBuilder sb = new StringBuilder(200);
                            sb.AppendFormat("{0} total ", application.NumTiers);
                            if (tiersList != null && tiersList.Count > 0)
                            {
                                var groupTypes = tiersList.GroupBy(t => t.AgentType);
                                sb.Append("(");
                                foreach (var groupType in groupTypes)
                                {
                                    sb.AppendFormat("{0} {1}", groupType.Count(), groupType.Key);
                                    sb.Append(", ");
                                }
                                if (sb.Length > 2) sb.Remove(sb.Length - 2, 2);
                                sb.Append("), ");
                            }
                            if (tiersMetricsList != null)
                            {
                                sb.AppendFormat("{0} active/{1} inactive", tiersMetricsList.Count(t => t.HasActivity == true), tiersMetricsList.Count(t => t.HasActivity == false));
                            }
                            insertNameValueRow(builder, table, "Tiers", sb.ToString());

                            sb = new StringBuilder(200);
                            sb.AppendFormat("{0} total ", application.NumNodes);
                            if (nodesList != null && nodesList.Count > 0)
                            {
                                var groupTypes = nodesList.GroupBy(n => n.AgentType);
                                sb.Append("(");
                                foreach (var groupType in groupTypes)
                                {
                                    sb.AppendFormat("{0} {1}", groupType.Count(), groupType.Key);
                                    sb.Append(", ");
                                }
                                if (sb.Length > 2) sb.Remove(sb.Length - 2, 2);
                                sb.Append("), ");
                            }
                            if (nodesMetricsList != null)
                            {
                                sb.AppendFormat("{0} active/{1} inactive", nodesMetricsList.Count(t => t.HasActivity == true), nodesMetricsList.Count(t => t.HasActivity == false));
                            }
                            insertNameValueRow(builder, table, "Nodes", sb.ToString());

                            sb = new StringBuilder(200);
                            sb.AppendFormat("{0} total ", application.NumBTs);
                            if (businessTransactionsList != null && businessTransactionsList.Count > 0)
                            {
                                var groupTypes = businessTransactionsList.GroupBy(b => b.BTType);
                                sb.Append("(");
                                foreach (var groupType in groupTypes)
                                {
                                    sb.AppendFormat("{0} {1}", groupType.Count(), groupType.Key);
                                    sb.Append(", ");
                                }
                                if (sb.Length > 2) sb.Remove(sb.Length - 2, 2);
                                sb.Append("), ");
                            }
                            if (businessTransactionsMetricsList != null)
                            {
                                sb.AppendFormat("{0} active/{1} inactive", businessTransactionsMetricsList.Count(t => t.HasActivity == true), businessTransactionsMetricsList.Count(t => t.HasActivity == false));
                            }
                            insertNameValueRow(builder, table, "Business Transactions", sb.ToString());

                            sb = new StringBuilder(200);
                            if (businessTransactionsMetricsList != null && businessTransactionsMetricsList.Count > 0)
                            {
                                sb.AppendFormat("{0} active/{1} inactive", businessTransactionsMetricsList.Count(t => t.BTType == "OVERFLOW" && t.HasActivity == true), businessTransactionsMetricsList.Count(t => t.BTType == "OVERFLOW" && t.HasActivity == false));
                            }
                            insertNameValueRow(builder, table, "Overflow BTs", sb.ToString());

                            sb = new StringBuilder(200);
                            sb.AppendFormat("{0} total ", application.NumBackends);
                            if (backendsList != null && backendsList.Count > 0)
                            {
                                var groupTypes = backendsList.GroupBy(b => b.BackendType);
                                sb.Append("(");
                                foreach (var groupType in groupTypes)
                                {
                                    sb.AppendFormat("{0} {1}", groupType.Count(), groupType.Key);
                                    sb.Append(", ");
                                }
                                if (sb.Length > 2) sb.Remove(sb.Length - 2, 2);
                                sb.Append("), ");
                            }
                            if (backendsMetricsList != null)
                            {
                                sb.AppendFormat("{0} active/{1} inactive", backendsMetricsList.Count(t => t.HasActivity == true), backendsMetricsList.Count(t => t.HasActivity == false));
                            }
                            insertNameValueRow(builder, table, "Backends", sb.ToString());

                            sb = new StringBuilder(200);
                            sb.AppendFormat("{0} total ", application.NumSEPs);
                            if (serviceEndpointsList != null && serviceEndpointsList.Count > 0)
                            {
                                var groupTypes = serviceEndpointsList.GroupBy(b => b.SEPType);
                                sb.Append("(");
                                foreach (var groupType in groupTypes)
                                {
                                    sb.AppendFormat("{0} {1}", groupType.Count(), groupType.Key);
                                    sb.Append(", ");
                                }
                                if (sb.Length > 2) sb.Remove(sb.Length - 2, 2);
                                sb.Append("), ");
                            }
                            if (serviceEndpointsMetricsList != null)
                            {
                                sb.AppendFormat("{0} active/{1} inactive", serviceEndpointsMetricsList.Count(t => t.HasActivity == true), serviceEndpointsMetricsList.Count(t => t.HasActivity == false));
                            }
                            insertNameValueRow(builder, table, "Service Endpoints", sb.ToString());

                            sb = new StringBuilder(200);
                            sb.AppendFormat("{0} total ", application.NumErrors);
                            if (errorsList != null && errorsList.Count > 0)
                            {
                                var groupTypes = errorsList.GroupBy(e => e.ErrorType);
                                sb.Append("(");
                                foreach (var groupType in groupTypes)
                                {
                                    sb.AppendFormat("{0} {1}", groupType.Count(), groupType.Key);
                                    sb.Append(", ");
                                }
                                if (sb.Length > 2) sb.Remove(sb.Length - 2, 2);
                                sb.Append("), ");
                            }
                            if (errorsMetricsList != null)
                            {
                                sb.AppendFormat("{0} active/{1} inactive", errorsMetricsList.Count(t => t.HasActivity == true), errorsMetricsList.Count(t => t.HasActivity == false));
                            }
                            insertNameValueRow(builder, table, "Errors", sb.ToString());

                            sb = new StringBuilder(200);
                            sb.AppendFormat("{0} total ", application.NumIPs);
                            if (informationPointsList != null && informationPointsList.Count > 0)
                            {
                                var groupTypes = informationPointsList.GroupBy(p => p.IPType);
                                sb.Append("(");
                                foreach (var groupType in groupTypes)
                                {
                                    sb.AppendFormat("{0} {1}", groupType.Count(), groupType.Key);
                                    sb.Append(", ");
                                }
                                if (sb.Length > 2) sb.Remove(sb.Length - 2, 2);
                                sb.Append("), ");
                            }
                            if (informationPointsMetricsList != null)
                            {
                                sb.AppendFormat("{0} active/{1} inactive", informationPointsMetricsList.Count(t => t.HasActivity == true), informationPointsMetricsList.Count(t => t.HasActivity == false));
                            }
                            insertNameValueRow(builder, table, "Information Points", sb.ToString());

                            sb = new StringBuilder(200);
                            if (resolvedBackendsList != null && resolvedBackendsList.Count > 0)
                            {
                                sb.AppendFormat("{0} total ", resolvedBackendsList.Count);

                                var groupTypes = resolvedBackendsList.GroupBy(b => b.BackendType);
                                sb.Append("(");
                                foreach (var groupType in groupTypes)
                                {
                                    sb.AppendFormat("{0} {1}", groupType.Count(), groupType.Key);
                                    sb.Append(", ");
                                }
                                if (sb.Length > 2) sb.Remove(sb.Length - 2, 2);
                                sb.Append(")");

                            }
                            insertNameValueRow(builder, table, "Mapped Backends", sb.ToString());

                            insertCellWithContent(builder, table, "Links");
                            Cell cell = builder.InsertCell();
                            insertLinkToURL(builder, hyperLinkStyle, "Controller", application.ControllerLink);
                            builder.Write(", ");
                            insertLinkToURL(builder, hyperLinkStyle, "Controller", application.ControllerLink);
                            builder.EndRow();

                            finalizeNameValueTable(builder, table);

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

                            insertHeading(builder, StyleIdentifier.Heading2, "Flowmap", String.Empty, listHeadingsOutline, 1);


                            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;
                            builder.Writeln("TODO");

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

                            insertHeading(
                                builder,
                                StyleIdentifier.Heading2,
                                "Summary",
                                String.Empty,
                                listHeadingsOutline,
                                1);

                            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;

                            Table table = insertNameValueTable(builder);
                            finalizeNameValueTable(builder, table);

                            #endregion

                            int j = 0;

                            foreach (APMTier tier in tiersList)
                            {
                                insertHeading(builder, StyleIdentifier.Heading2, String.Format("{0} [{1}] ({2})", tier.TierName, tier.AgentType, tier.TierID), getShortenedEntityNameForWordBookmark("tier", tier.TierName, tier.TierID), listHeadingsOutline, 1);

                                insertLinksToEntityTypeSections(builder);

                                #region Summary Table

                                insertHeading(builder, StyleIdentifier.Heading3, "Summary", String.Empty, listHeadingsOutline, 2);

                                builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;

                                table = insertNameValueTable(builder);
                                finalizeNameValueTable(builder, table);

                                #endregion

                                builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;
                                builder.Writeln(tier.ToString());

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

                                builder.InsertBreak(BreakType.PageBreak);

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

                            Table table = insertNameValueTable(builder);
                            finalizeNameValueTable(builder, table);

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

                                            table = insertNameValueTable(builder);
                                            finalizeNameValueTable(builder, table);

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

                                            builder.InsertBreak(BreakType.PageBreak);

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

                        insertHeading(builder, StyleIdentifier.Heading1, "Business Transactions", "BTs", listHeadingsOutline, 0);

                        insertLinksToEntityTypeSections(builder);

                        if (businessTransactionsList != null)
                        {
                            loggerConsole.Info("Processing Business Transactions ({0} entities)", businessTransactionsList.Count);

                            #region Summary Table

                            insertHeading(builder, StyleIdentifier.Heading2, "Summary", String.Empty, listHeadingsOutline, 1);

                            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;

                            Table table = insertNameValueTable(builder);
                            finalizeNameValueTable(builder, table);

                            #endregion

                            int j = 0;

                            if (tiersList != null)
                            {
                                insertLinksToTiersInBusinessTransactions(tiersList, builder, hyperLinkStyle);

                                foreach (APMTier tier in tiersList)
                                {
                                    insertHeading( builder, StyleIdentifier.Heading2, String.Format("{0} [{1}] ({2})", tier.TierName, tier.AgentType, tier.TierID), getShortenedEntityNameForWordBookmark("tierbt", tier.TierName, tier.TierID), listHeadingsOutline, 1);

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

                                            table = insertNameValueTable(builder);
                                            finalizeNameValueTable(builder, table);

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

                                            builder.InsertBreak(BreakType.PageBreak);

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

                            insertHeading( builder, StyleIdentifier.Heading2, "Summary", String.Empty, listHeadingsOutline, 1);

                            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;

                            Table table = insertNameValueTable(builder);
                            finalizeNameValueTable(builder, table);

                            #endregion

                            int j = 0;

                            foreach (APMBackend backend in backendsList)
                            {
                                insertHeading(builder, StyleIdentifier.Heading2, String.Format("{0} [{1}] ({2})", backend.BackendName, backend.BackendType, backend.BackendID), getShortenedEntityNameForWordBookmark("back", backend.BackendName, backend.BackendID), listHeadingsOutline, 1);

                                insertLinksToEntityTypeSections(builder);

                                #region Summary Table

                                insertHeading(builder, StyleIdentifier.Heading3, "Summary", String.Empty, listHeadingsOutline, 2);

                                builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;

                                table = insertNameValueTable(builder);
                                finalizeNameValueTable(builder, table);

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

                                builder.InsertBreak(BreakType.PageBreak);

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

                        insertHeading(builder, StyleIdentifier.Heading1, "Service Endpoints", "SEPs", listHeadingsOutline, 0);

                        insertLinksToEntityTypeSections(builder);

                        if (serviceEndpointsList != null)
                        {
                            loggerConsole.Info("Processing Service Endpoints ({0} entities)", serviceEndpointsList.Count);

                            #region Summary Table

                            insertHeading(builder, StyleIdentifier.Heading2, "Summary", String.Empty, listHeadingsOutline, 1);

                            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;

                            Table table = insertNameValueTable(builder);
                            finalizeNameValueTable(builder, table);

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

                                            table = insertNameValueTable(builder);
                                            finalizeNameValueTable(builder, table);

                                            #endregion

                                            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;
                                            builder.Writeln(serviceEndpoint.ToString());

                                            builder.InsertBreak(BreakType.PageBreak);

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

                            Table table = insertNameValueTable(builder);
                            finalizeNameValueTable(builder, table);

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

                                            table = insertNameValueTable(builder);
                                            finalizeNameValueTable(builder, table);

                                            #endregion

                                            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;
                                            builder.Writeln(error.ToString());

                                            builder.InsertBreak(BreakType.PageBreak);

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

                        insertHeading(builder, StyleIdentifier.Heading1, "Information Points", "IPs", listHeadingsOutline, 0);

                        insertLinksToEntityTypeSections(builder);

                        if (informationPointsList != null)
                        {
                            loggerConsole.Info("Processing Information Points ({0} entities)", informationPointsList.Count);

                            insertLinksToInformationPoints(informationPointsList, builder, hyperLinkStyle);

                            #region Summary Table

                            insertHeading(builder, StyleIdentifier.Heading2, "Summary", String.Empty, listHeadingsOutline, 1);

                            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;

                            Table table = insertNameValueTable(builder);
                            finalizeNameValueTable(builder, table);

                            #endregion

                            int j = 0;

                            foreach (APMInformationPoint informationPoint in informationPointsList)
                            {
                                insertHeading(builder, StyleIdentifier.Heading2, String.Format("{0} [{1}] ({2})", informationPoint.IPName, informationPoint.IPType, informationPoint.IPID), getShortenedEntityNameForWordBookmark("ip", informationPoint.IPName, informationPoint.IPID), listHeadingsOutline, 1);

                                insertLinksToEntityTypeSections(builder);

                                #region Summary Table

                                insertHeading(builder, StyleIdentifier.Heading3, "Summary", String.Empty, listHeadingsOutline, 2);

                                builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;

                                table = insertNameValueTable(builder);
                                finalizeNameValueTable(builder, table);

                                #endregion

                                builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;
                                builder.Writeln(informationPoint.ToString());

                                builder.InsertBreak(BreakType.PageBreak);

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

        public override bool ShouldExecute(JobConfiguration jobConfiguration)
        {
            logger.Trace("Output.ApplicationSummary={0}", jobConfiguration.Output.ApplicationSummary);
            loggerConsole.Trace("Output.ApplicationSummary={0}", jobConfiguration.Output.ApplicationSummary);
            if (jobConfiguration.Output.ApplicationSummary == false)
            {
                loggerConsole.Trace("Skipping report of Application summary");
            }
            return (jobConfiguration.Output.ApplicationSummary == true);
        }

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

            //#region Top

            //builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Heading1;
            //builder.StartBookmark("Top");
            //builder.Writeln("Top");
            //builder.EndBookmark("Top");

            //builder.Writeln(String.Format("TODO fill out some summary about {0}/{1} ({2})", jobTarget.Controller, jobTarget.Application, jobTarget.ApplicationID));

            // builder.InsertBreak(BreakType.PageBreak);

            //#endregion

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

        private static void insertLinksToEntityTypeSections(DocumentBuilder builder)
        {
            Style hyperLinkStyle = builder.Document.Styles["HyperLinkStyle"];

            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;

            builder.Write("Jump to: ");

            //builder.Font.Style = hyperLinkStyle;
            //builder.InsertHyperlink("Top", "Top", true);
            //builder.Font.ClearFormatting();

            //builder.Write(", ");

            builder.Font.Style = hyperLinkStyle;
            builder.InsertHyperlink("Application", "Application", true);
            builder.Font.ClearFormatting();

            builder.Write(", ");

            builder.Font.Style = hyperLinkStyle;
            builder.InsertHyperlink("Tiers", "Tiers", true);
            builder.Font.ClearFormatting();

            builder.Write(", ");

            builder.Font.Style = hyperLinkStyle;
            builder.InsertHyperlink("Nodes", "Nodes", true);
            builder.Font.ClearFormatting();

            builder.Write(", ");

            builder.Font.Style = hyperLinkStyle;
            builder.InsertHyperlink("BTs", "BTs", true);
            builder.Font.ClearFormatting();

            builder.Write(", ");

            builder.Font.Style = hyperLinkStyle;
            builder.InsertHyperlink("Backends", "Backends", true);
            builder.Font.ClearFormatting();

            builder.Write(", ");

            builder.Font.Style = hyperLinkStyle;
            builder.InsertHyperlink("Service Endpoints", "SEPs", true);
            builder.Font.ClearFormatting();

            builder.Write(", ");

            builder.Font.Style = hyperLinkStyle;
            builder.InsertHyperlink("Errors", "Errors", true);
            builder.Font.ClearFormatting();

            builder.Write(", ");

            builder.Font.Style = hyperLinkStyle;
            builder.InsertHyperlink("Information Points", "IPs", true);
            builder.Font.ClearFormatting();

            builder.Writeln("");
        }

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
            builder.InsertBreak(BreakType.PageBreak);
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

                builder.Font.Style = hyperLinkStyle;
                builder.InsertHyperlink(node.NodeName, getShortenedEntityNameForWordBookmark("node", node.NodeName, node.NodeID), true);
                builder.Font.ClearFormatting();

                if (i < nodesList.Count - 1)
                {
                    builder.Write(", ");
                }
            }
            builder.Writeln("");
            builder.InsertBreak(BreakType.PageBreak);
        }

        private static void insertLinksToBusinessTransactions(List<APMBusinessTransaction> businessTransactionsList, DocumentBuilder builder, Style hyperLinkStyle)
        {
            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;
            builder.Write("Jump to: ");
            for (int i = 0; i < businessTransactionsList.Count; i++)
            {
                APMBusinessTransaction businessTransaction = businessTransactionsList[i];

                builder.Font.Style = hyperLinkStyle;
                builder.InsertHyperlink(businessTransaction.BTName, getShortenedEntityNameForWordBookmark("bt", businessTransaction.BTName, businessTransaction.BTID), true);
                builder.Font.ClearFormatting();

                if (i < businessTransactionsList.Count - 1)
                {
                    builder.Write(", ");
                }
            }
            builder.Writeln("");
            builder.InsertBreak(BreakType.PageBreak);
        }

        private static void insertLinksToBackends(List<APMBackend> backendsList, DocumentBuilder builder, Style hyperLinkStyle)
        {
            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;
            builder.Write("Jump to: ");
            for (int i = 0; i < backendsList.Count; i++)
            {
                APMBackend backend = backendsList[i];

                builder.Font.Style = hyperLinkStyle;
                builder.InsertHyperlink(backend.BackendName, getShortenedEntityNameForWordBookmark("back", backend.BackendName, backend.BackendID), true);
                builder.Font.ClearFormatting();

                if (i < backendsList.Count - 1)
                {
                    builder.Write(", ");
                }
            }
            builder.Writeln("");
            builder.InsertBreak(BreakType.PageBreak);
        }

        private static void insertLinksToServiceEndpoints(List<APMServiceEndpoint> serviceEndpointsList, DocumentBuilder builder, Style hyperLinkStyle)
        {
            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;
            builder.Write("Jump to: ");
            for (int i = 0; i < serviceEndpointsList.Count; i++)
            {
                APMServiceEndpoint serviceEndpoint = serviceEndpointsList[i];

                builder.Font.Style = hyperLinkStyle;
                builder.InsertHyperlink(serviceEndpoint.SEPName, getShortenedEntityNameForWordBookmark("sep", serviceEndpoint.SEPName, serviceEndpoint.SEPID), true);
                builder.Font.ClearFormatting();

                if (i < serviceEndpointsList.Count - 1)
                {
                    builder.Write(", ");
                }
            }
            builder.Writeln("");
            builder.InsertBreak(BreakType.PageBreak);
        }

        private static void insertLinksToErrors(List<APMError> errorsList, DocumentBuilder builder, Style hyperLinkStyle)
        {
            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;
            builder.Write("Jump to: ");
            for (int i = 0; i < errorsList.Count; i++)
            {
                APMError error = errorsList[i];

                builder.Font.Style = hyperLinkStyle;
                builder.InsertHyperlink(error.ErrorName, getShortenedEntityNameForWordBookmark("error", error.ErrorName, error.ErrorID), true);
                builder.Font.ClearFormatting();

                if (i < errorsList.Count - 1)
                {
                    builder.Write(", ");
                }
            }
            builder.Writeln("");
            builder.InsertBreak(BreakType.PageBreak);
        }

        private static void insertLinksToInformationPoints(List<APMInformationPoint> informationPointsList, DocumentBuilder builder, Style hyperLinkStyle)
        {
            builder.ParagraphFormat.StyleIdentifier = StyleIdentifier.Normal;
            builder.Write("Jump to: ");
            for (int i = 0; i < informationPointsList.Count; i++)
            {
                APMInformationPoint informationPoint = informationPointsList[i];

                builder.Font.Style = hyperLinkStyle;
                builder.InsertHyperlink(informationPoint.IPName, getShortenedEntityNameForWordBookmark("ip", informationPoint.IPName, informationPoint.IPID), true);
                builder.Font.ClearFormatting();

                if (i < informationPointsList.Count - 1)
                {
                    builder.Write(", ");
                }
            }
            builder.Writeln("");
            builder.InsertBreak(BreakType.PageBreak);
        }

        internal static Table insertNameValueTable(DocumentBuilder builder)
        { 
            Table table = builder.StartTable();
            Cell cell = insertCellWithContent(builder, table, "Property");
            cell.CellFormat.PreferredWidth = PreferredWidth.FromPoints(100);
            cell = insertCellWithContent(builder, table, "Value");
            cell.CellFormat.PreferredWidth = PreferredWidth.FromPoints(600);

            builder.EndRow();

            return table;
        }

        internal static void finalizeNameValueTable(DocumentBuilder builder, Table table)
        {
            table.StyleIdentifier = StyleIdentifier.ListTable1Light;
            table.AutoFit(AutoFitBehavior.AutoFitToContents);
            table.StyleOptions = TableStyleOptions.RowBands | TableStyleOptions.FirstRow;
            builder.EndTable();
        }

        internal static void insertNameValueRow(DocumentBuilder builder, Table table, string propertyName, object propertyValue)
        {
            insertNameValueRow(builder, table, propertyName, propertyValue.ToString());
        }

        internal static void insertNameValueRow(DocumentBuilder builder, Table table, string propertyName, string propertyValue)
        {
            insertCellWithContent(builder, table, propertyName);
            insertCellWithContent(builder, table, propertyValue);
            builder.EndRow();
        }

        internal static Cell insertCellWithContent(DocumentBuilder builder, Table table, string cellContent)
        {
            Cell cell = builder.InsertCell();
            builder.Write(cellContent);
            return cell;
        }

    }
}
