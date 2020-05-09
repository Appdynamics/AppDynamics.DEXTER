using AppDynamics.Dexter.ReportObjectMaps;
using AppDynamics.Dexter.ReportObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class ReportAPMEntityDashboardScreenshots : JobStepReportBase
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

                        XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
                        xmlWriterSettings.Indent = true;

                        XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();
                        xmlReaderSettings.DtdProcessing = DtdProcessing.Parse;
                        xmlReaderSettings.IgnoreComments = false;

                        #endregion

                        #region Copy dashboard images and generate thumbnails

                        #region Application

                        List<APMApplication> applicationList = FileIOHelper.ReadListFromCSVFile<APMApplication>(FilePathMap.APMApplicationsIndexFilePath(jobTarget), new APMApplicationReportMap());
                        if (applicationList != null)
                        {
                            loggerConsole.Info("Application Dashboards");

                            foreach (APMApplication application in applicationList)
                            {
                                if (File.Exists(FilePathMap.EntityDashboardScreenshotReportFilePath(application, true, true)) == false)
                                {
                                    if (File.Exists(FilePathMap.ApplicationDashboardScreenshotDataFilePath(jobTarget)) == true)
                                    {
                                        // Copy original
                                        FileIOHelper.CopyFile(
                                            FilePathMap.ApplicationDashboardScreenshotDataFilePath(jobTarget),
                                            FilePathMap.EntityDashboardScreenshotReportFilePath(application, true, false));

                                        // Make thumbnail
                                        resizeDashboardScreenshot(
                                            FilePathMap.ApplicationDashboardScreenshotDataFilePath(jobTarget),
                                            FilePathMap.EntityDashboardScreenshotReportFilePath(application, true, true));
                                    }
                                }
                            }

                            numEntitiesTotal = applicationList.Count;
                        }

                        #endregion

                        #region Tiers

                        List<APMTier> tiersList = FileIOHelper.ReadListFromCSVFile<APMTier>(FilePathMap.APMTiersIndexFilePath(jobTarget), new APMTierReportMap());
                        if (tiersList != null)
                        {
                            loggerConsole.Info("Screenshots for Tiers ({0} entities)", tiersList.Count);

                            int j = 0;

                            foreach (APMTier tier in tiersList)
                            {
                                if (File.Exists(FilePathMap.EntityDashboardScreenshotReportFilePath(tier, true, true)) == false)
                                {
                                    if (File.Exists(FilePathMap.TierDashboardScreenshotDataFilePath(jobTarget, tier.TierName, tier.TierID)) == true)
                                    {
                                        // Copy original
                                        FileIOHelper.CopyFile(
                                            FilePathMap.TierDashboardScreenshotDataFilePath(jobTarget, tier.TierName, tier.TierID),
                                            FilePathMap.EntityDashboardScreenshotReportFilePath(tier, true, false));

                                        // Make thumbnail
                                        resizeDashboardScreenshot(
                                            FilePathMap.TierDashboardScreenshotDataFilePath(jobTarget, tier.TierName, tier.TierID),
                                            FilePathMap.EntityDashboardScreenshotReportFilePath(tier, true, true));
                                    }
                                }
                                if (j % 10 == 0)
                                {
                                    Console.Write("[{0}].", j);
                                }
                                j++;
                            }

                            loggerConsole.Info("Completed {0} Tiers", tiersList.Count);
                            numEntitiesTotal = numEntitiesTotal + tiersList.Count;
                        }

                        #endregion

                        #region Nodes

                        List<APMNode> nodesList = FileIOHelper.ReadListFromCSVFile<APMNode>(FilePathMap.APMNodesIndexFilePath(jobTarget), new APMNodeReportMap());
                        if (nodesList != null)
                        {
                            loggerConsole.Info("Screenshots for Nodes ({0} entities)", nodesList.Count);

                            int j = 0;

                            foreach (APMNode node in nodesList)
                            {
                                if (File.Exists(FilePathMap.EntityDashboardScreenshotReportFilePath(node, true, true)) == false)
                                {
                                    if (File.Exists(FilePathMap.NodeDashboardScreenshotDataFilePath(jobTarget, node.TierName, node.TierID, node.NodeName, node.NodeID)) == true)
                                    {
                                        // Copy original
                                        FileIOHelper.CopyFile(
                                            FilePathMap.NodeDashboardScreenshotDataFilePath(jobTarget, node.TierName, node.TierID, node.NodeName, node.NodeID),
                                            FilePathMap.EntityDashboardScreenshotReportFilePath(node, true, false));

                                        // Make thumbnail
                                        resizeDashboardScreenshot(
                                            FilePathMap.NodeDashboardScreenshotDataFilePath(jobTarget, node.TierName, node.TierID, node.NodeName, node.NodeID),
                                            FilePathMap.EntityDashboardScreenshotReportFilePath(node, true, true));
                                    }
                                }
                                if (j % 10 == 0)
                                {
                                    Console.Write("[{0}].", j);
                                }
                                j++;
                            }

                            loggerConsole.Info("Completed {0} Nodes", nodesList.Count);
                            numEntitiesTotal = numEntitiesTotal + nodesList.Count;
                        }

                        #endregion

                        #region Business Transactions

                        List<APMBusinessTransaction> businessTransactionsList = FileIOHelper.ReadListFromCSVFile<APMBusinessTransaction>(FilePathMap.APMBusinessTransactionsIndexFilePath(jobTarget), new APMBusinessTransactionReportMap());
                        if (businessTransactionsList != null)
                        {
                            loggerConsole.Info("Screenshots for Business Transactions ({0} entities)", businessTransactionsList.Count);

                            int j = 0;

                            foreach (APMBusinessTransaction businessTransaction in businessTransactionsList)
                            {
                                if (File.Exists(FilePathMap.EntityDashboardScreenshotReportFilePath(businessTransaction, true, true)) == false)
                                {
                                    if (File.Exists(FilePathMap.BusinessTransactionDashboardScreenshotDataFilePath(jobTarget, businessTransaction.TierName, businessTransaction.TierID, businessTransaction.BTName, businessTransaction.BTID)) == true)
                                    {
                                        // Copy original
                                        FileIOHelper.CopyFile(
                                            FilePathMap.BusinessTransactionDashboardScreenshotDataFilePath(jobTarget, businessTransaction.TierName, businessTransaction.TierID, businessTransaction.BTName, businessTransaction.BTID),
                                            FilePathMap.EntityDashboardScreenshotReportFilePath(businessTransaction, true, false));

                                        // Make thumbnail
                                        resizeDashboardScreenshot(
                                            FilePathMap.BusinessTransactionDashboardScreenshotDataFilePath(jobTarget, businessTransaction.TierName, businessTransaction.TierID, businessTransaction.BTName, businessTransaction.BTID),
                                            FilePathMap.EntityDashboardScreenshotReportFilePath(businessTransaction, true, true));
                                    }
                                }
                                if (j % 10 == 0)
                                {
                                    Console.Write("[{0}].", j);
                                }
                                j++;
                            }

                            loggerConsole.Info("Completed {0} Business Transactions", businessTransactionsList.Count);
                            numEntitiesTotal = numEntitiesTotal + businessTransactionsList.Count;
                        }

                        #endregion

                        #region Backends

                        List<APMBackend> backendsList = FileIOHelper.ReadListFromCSVFile<APMBackend>(FilePathMap.APMBackendsIndexFilePath(jobTarget), new APMBackendReportMap());
                        if (businessTransactionsList != null)
                        {
                            loggerConsole.Info("Screenshots for Backends ({0} entities)", backendsList.Count);

                            int j = 0;

                            foreach (APMBackend backend in backendsList)
                            {
                                if (File.Exists(FilePathMap.EntityDashboardScreenshotReportFilePath(backend, true, true)) == false)
                                {
                                    if (File.Exists(FilePathMap.BackendDashboardScreenshotDataFilePath(jobTarget, backend.BackendName, backend.BackendID)) == true)
                                    {
                                        // Copy original
                                        FileIOHelper.CopyFile(
                                            FilePathMap.BackendDashboardScreenshotDataFilePath(jobTarget, backend.BackendName, backend.BackendID),
                                            FilePathMap.EntityDashboardScreenshotReportFilePath(backend, true, false));

                                        // Make thumbnail
                                        resizeDashboardScreenshot(
                                            FilePathMap.BackendDashboardScreenshotDataFilePath(jobTarget, backend.BackendName, backend.BackendID),
                                            FilePathMap.EntityDashboardScreenshotReportFilePath(backend, true, true));
                                    }
                                }
                                if (j % 10 == 0)
                                {
                                    Console.Write("[{0}].", j);
                                }
                                j++;
                            }

                            loggerConsole.Info("Completed {0} Backends", backendsList.Count);
                            numEntitiesTotal = numEntitiesTotal + backendsList.Count;
                        }

                        #endregion

                        #endregion

                        #region Generate links

                        loggerConsole.Info("Generating Links to All Dashboards");

                        if (applicationList != null && applicationList.Count > 0)
                        {
                            APMApplication application = applicationList[0];

                            string dashboardFilePath = FilePathMap.ApplicationDashboardLinksReportFilePath(application, true);

                            FileIOHelper.CreateFolderForFile(dashboardFilePath);

                            using (StringReader stringReader = new StringReader(FileIOHelper.ReadFileFromPath(FilePathMap.ApplicationDashboardsLinksTemplateFilePath())))
                            {
                                using (XmlReader xmlReader = XmlReader.Create(stringReader, xmlReaderSettings))
                                {
                                    using (XmlWriter xmlWriter = XmlWriter.Create(dashboardFilePath, xmlWriterSettings))
                                    {
                                        while (xmlReader.Read())
                                        {
                                            // Adjust version
                                            if (xmlReader.IsStartElement("td") == true && xmlReader.GetAttribute("id") == "tdVersion")
                                            {
                                                xmlWriter.WriteStartElement(xmlReader.LocalName);
                                                xmlReader.Read();
                                                xmlWriter.WriteString(String.Format(xmlReader.Value, Assembly.GetEntryAssembly().GetName().Version));
                                                xmlWriter.WriteEndElement();

                                                // Read the template string and closing /text tag to move the reader forward
                                                xmlReader.Read();
                                            }
                                            // Adjust date from
                                            else if (xmlReader.IsStartElement("td") == true && xmlReader.GetAttribute("id") == "tdFromDateTime")
                                            {
                                                xmlWriter.WriteStartElement(xmlReader.LocalName);
                                                xmlReader.Read();
                                                xmlWriter.WriteString(jobConfiguration.Input.TimeRange.From.ToLocalTime().ToString("G"));
                                                xmlWriter.WriteEndElement();

                                                // Read the template string and closing /text tag to move the reader forward
                                                xmlReader.Read();
                                            }
                                            // Adjust date to
                                            else if (xmlReader.IsStartElement("td") == true && xmlReader.GetAttribute("id") == "tdToDateTime")
                                            {
                                                xmlWriter.WriteStartElement(xmlReader.LocalName);
                                                xmlReader.Read();
                                                xmlWriter.WriteString(jobConfiguration.Input.TimeRange.To.ToLocalTime().ToString("G"));
                                                xmlWriter.WriteEndElement();

                                                // Read the template string and closing /text tag to move the reader forward
                                                xmlReader.Read();
                                            }
                                            // Adjust date timezone
                                            else if (xmlReader.IsStartElement("td") == true && xmlReader.GetAttribute("id") == "tdTimezone")
                                            {
                                                xmlWriter.WriteStartElement(xmlReader.LocalName);
                                                xmlReader.Read();
                                                xmlWriter.WriteString(TimeZoneInfo.Local.DisplayName);
                                                xmlWriter.WriteEndElement();

                                                // Read the template string and closing /text tag to move the reader forward
                                                xmlReader.Read();
                                            }
                                            // Adjust Controller
                                            else if (xmlReader.IsStartElement("td") == true && xmlReader.GetAttribute("id") == "tdController")
                                            {
                                                xmlWriter.WriteStartElement(xmlReader.LocalName);
                                                xmlReader.Read();
                                                xmlWriter.WriteString(application.Controller);
                                                xmlWriter.WriteEndElement();

                                                // Read the template string and closing /text tag to move the reader forward
                                                xmlReader.Read();
                                            }
                                            // Adjust Application
                                            else if (xmlReader.IsStartElement("td") == true && xmlReader.GetAttribute("id") == "tdApplication")
                                            {
                                                xmlWriter.WriteStartElement(xmlReader.LocalName);
                                                xmlReader.Read();
                                                xmlWriter.WriteString(application.ApplicationName);
                                                xmlWriter.WriteEndElement();

                                                // Read the template string and closing /text tag to move the reader forward
                                                xmlReader.Read();
                                            }
                                            // Adjust Application ID
                                            else if (xmlReader.IsStartElement("td") == true && xmlReader.GetAttribute("id") == "tdApplicationID")
                                            {
                                                xmlWriter.WriteStartElement(xmlReader.LocalName);
                                                xmlReader.Read();
                                                xmlWriter.WriteString(application.ApplicationID.ToString());
                                                xmlWriter.WriteEndElement();

                                                // Read the template string and closing /text tag to move the reader forward
                                                xmlReader.Read();
                                            }
                                            // Title
                                            else if (xmlReader.IsStartElement("title") == true)
                                            {
                                                xmlWriter.WriteStartElement(xmlReader.LocalName);
                                                xmlReader.Read();
                                                xmlWriter.WriteString(String.Format("{0}[{1}] - {2}", jobTarget.Application, jobTarget.ApplicationID, jobTarget.Controller));
                                                xmlWriter.WriteEndElement();

                                                // Read the template string and closing /text tag to move the reader forward
                                                xmlReader.Read();
                                            }
                                            // List of Applications
                                            else if (xmlReader.IsStartElement("tr") == true && xmlReader.GetAttribute("id") == "trApplicationPlaceholder")
                                            {
                                                xmlWriter.WriteStartElement("tr");

                                                xmlWriter.WriteStartElement("td");
                                                xmlWriter.WriteAttributeString("class", "Controller");
                                                xmlWriter.WriteString(application.Controller);
                                                xmlWriter.WriteEndElement();

                                                xmlWriter.WriteStartElement("td");
                                                xmlWriter.WriteAttributeString("class", "Application");
                                                xmlWriter.WriteString(application.ApplicationName);
                                                xmlWriter.WriteEndElement();

                                                xmlWriter.WriteStartElement("td");
                                                xmlWriter.WriteString(application.ApplicationID.ToString());
                                                xmlWriter.WriteEndElement();

                                                xmlWriter.WriteStartElement("td");
                                                xmlWriter.WriteStartElement("img");
                                                xmlWriter.WriteAttributeString("src", String.Format(@"..\..\{0}", FilePathMap.EntityDashboardScreenshotReportFilePath(application, false, true)));
                                                xmlWriter.WriteEndElement();
                                                xmlWriter.WriteEndElement();

                                                xmlWriter.WriteStartElement("td");
                                                xmlWriter.WriteStartElement("a");
                                                xmlWriter.WriteAttributeString("href", String.Format(@"..\..\{0}", FilePathMap.EntityDashboardScreenshotReportFilePath(application, false, false)));
                                                xmlWriter.WriteAttributeString("target", "_blank");
                                                xmlWriter.WriteString("Dashboard");
                                                xmlWriter.WriteEndElement();
                                                xmlWriter.WriteEndElement();

                                                xmlWriter.WriteStartElement("td");
                                                xmlWriter.WriteStartElement("a");
                                                xmlWriter.WriteAttributeString("href", application.ApplicationLink);
                                                xmlWriter.WriteString("Application");
                                                xmlWriter.WriteEndElement();
                                                xmlWriter.WriteEndElement();

                                                xmlWriter.WriteEndElement();

                                                // Move off the content placeholder
                                                xmlReader.Read();
                                                xmlReader.Read();
                                            }
                                            // List of Tiers
                                            else if (xmlReader.IsStartElement("tr") == true && xmlReader.GetAttribute("id") == "trTiersPlaceholder")
                                            {
                                                if (tiersList != null)
                                                {
                                                    foreach (APMTier tier in tiersList)
                                                    {
                                                        xmlWriter.WriteStartElement("tr");

                                                        xmlWriter.WriteStartElement("td");
                                                        xmlWriter.WriteAttributeString("class", "Controller");
                                                        xmlWriter.WriteString(tier.Controller);
                                                        xmlWriter.WriteEndElement();

                                                        xmlWriter.WriteStartElement("td");
                                                        xmlWriter.WriteAttributeString("class", "Application");
                                                        xmlWriter.WriteString(tier.ApplicationName);
                                                        xmlWriter.WriteEndElement();

                                                        xmlWriter.WriteStartElement("td");
                                                        xmlWriter.WriteAttributeString("class", "Tier");
                                                        xmlWriter.WriteString(tier.TierName);
                                                        xmlWriter.WriteEndElement();

                                                        xmlWriter.WriteStartElement("td");
                                                        xmlWriter.WriteAttributeString("class", "Type");
                                                        xmlWriter.WriteString(tier.TierType);
                                                        xmlWriter.WriteEndElement();

                                                        xmlWriter.WriteStartElement("td");
                                                        xmlWriter.WriteString(tier.TierID.ToString());
                                                        xmlWriter.WriteEndElement();

                                                        xmlWriter.WriteStartElement("td");
                                                        xmlWriter.WriteStartElement("img");
                                                        xmlWriter.WriteAttributeString("src", String.Format(@"..\..\{0}", FilePathMap.EntityDashboardScreenshotReportFilePath(tier, false, true)));
                                                        xmlWriter.WriteEndElement();
                                                        xmlWriter.WriteEndElement();

                                                        xmlWriter.WriteStartElement("td");
                                                        xmlWriter.WriteStartElement("a");
                                                        xmlWriter.WriteAttributeString("href", String.Format(@"..\..\{0}", FilePathMap.EntityDashboardScreenshotReportFilePath(tier, false, false)));
                                                        xmlWriter.WriteAttributeString("target", "_blank");
                                                        xmlWriter.WriteString("Dashboard");
                                                        xmlWriter.WriteEndElement();
                                                        xmlWriter.WriteEndElement();

                                                        xmlWriter.WriteStartElement("td");
                                                        xmlWriter.WriteStartElement("a");
                                                        xmlWriter.WriteAttributeString("href", tier.TierLink);
                                                        xmlWriter.WriteString("Tier");
                                                        xmlWriter.WriteEndElement();
                                                        xmlWriter.WriteEndElement();

                                                        xmlWriter.WriteEndElement();
                                                    }
                                                }

                                                // Move off the content placeholder
                                                xmlReader.Read();
                                                xmlReader.Read();
                                            }
                                            // List of Nodes
                                            else if (xmlReader.IsStartElement("tr") == true && xmlReader.GetAttribute("id") == "trNodesPlaceholder")
                                            {
                                                if (nodesList != null)
                                                {
                                                    foreach (APMNode node in nodesList)
                                                    {
                                                        xmlWriter.WriteStartElement("tr");

                                                        xmlWriter.WriteStartElement("td");
                                                        xmlWriter.WriteAttributeString("class", "Controller");
                                                        xmlWriter.WriteString(node.Controller);
                                                        xmlWriter.WriteEndElement();

                                                        xmlWriter.WriteStartElement("td");
                                                        xmlWriter.WriteAttributeString("class", "Application");
                                                        xmlWriter.WriteString(node.ApplicationName);
                                                        xmlWriter.WriteEndElement();

                                                        xmlWriter.WriteStartElement("td");
                                                        xmlWriter.WriteAttributeString("class", "Tier");
                                                        xmlWriter.WriteString(node.TierName);
                                                        xmlWriter.WriteEndElement();

                                                        xmlWriter.WriteStartElement("td");
                                                        xmlWriter.WriteAttributeString("class", "Node");
                                                        xmlWriter.WriteString(node.NodeName);
                                                        xmlWriter.WriteEndElement();

                                                        xmlWriter.WriteStartElement("td");
                                                        xmlWriter.WriteAttributeString("class", "Type");
                                                        xmlWriter.WriteString(node.AgentType);
                                                        xmlWriter.WriteEndElement();

                                                        xmlWriter.WriteStartElement("td");
                                                        xmlWriter.WriteString(node.NodeID.ToString());
                                                        xmlWriter.WriteEndElement();

                                                        xmlWriter.WriteStartElement("td");
                                                        xmlWriter.WriteStartElement("img");
                                                        xmlWriter.WriteAttributeString("src", String.Format(@"..\..\{0}", FilePathMap.EntityDashboardScreenshotReportFilePath(node, false, true)));
                                                        xmlWriter.WriteEndElement();
                                                        xmlWriter.WriteEndElement();

                                                        xmlWriter.WriteStartElement("td");
                                                        xmlWriter.WriteStartElement("a");
                                                        xmlWriter.WriteAttributeString("href", String.Format(@"..\..\{0}", FilePathMap.EntityDashboardScreenshotReportFilePath(node, false, false)));
                                                        xmlWriter.WriteAttributeString("target", "_blank");
                                                        xmlWriter.WriteString("Dashboard");
                                                        xmlWriter.WriteEndElement();
                                                        xmlWriter.WriteEndElement();

                                                        xmlWriter.WriteStartElement("td");
                                                        xmlWriter.WriteStartElement("a");
                                                        xmlWriter.WriteAttributeString("href", node.NodeLink);
                                                        xmlWriter.WriteString("Node");
                                                        xmlWriter.WriteEndElement();
                                                        xmlWriter.WriteEndElement();

                                                        xmlWriter.WriteEndElement();
                                                    }
                                                }

                                                // Move off the content placeholder
                                                xmlReader.Read();
                                                xmlReader.Read();
                                            }
                                            // List of Business Transactions
                                            else if (xmlReader.IsStartElement("tr") == true && xmlReader.GetAttribute("id") == "trBusinessTransactionsPlaceholder")
                                            {
                                                if (businessTransactionsList != null)
                                                {
                                                    foreach (APMBusinessTransaction businessTransaction in businessTransactionsList)
                                                    {
                                                        xmlWriter.WriteStartElement("tr");

                                                        xmlWriter.WriteStartElement("td");
                                                        xmlWriter.WriteAttributeString("class", "Controller");
                                                        xmlWriter.WriteString(businessTransaction.Controller);
                                                        xmlWriter.WriteEndElement();

                                                        xmlWriter.WriteStartElement("td");
                                                        xmlWriter.WriteAttributeString("class", "Application");
                                                        xmlWriter.WriteString(businessTransaction.ApplicationName);
                                                        xmlWriter.WriteEndElement();

                                                        xmlWriter.WriteStartElement("td");
                                                        xmlWriter.WriteAttributeString("class", "Tier");
                                                        xmlWriter.WriteString(businessTransaction.TierName);
                                                        xmlWriter.WriteEndElement();

                                                        xmlWriter.WriteStartElement("td");
                                                        xmlWriter.WriteAttributeString("class", "BusinessTransaction");
                                                        xmlWriter.WriteString(businessTransaction.BTName);
                                                        xmlWriter.WriteEndElement();

                                                        xmlWriter.WriteStartElement("td");
                                                        xmlWriter.WriteAttributeString("class", "Type");
                                                        xmlWriter.WriteString(businessTransaction.BTType);
                                                        xmlWriter.WriteEndElement();

                                                        xmlWriter.WriteStartElement("td");
                                                        xmlWriter.WriteString(businessTransaction.BTID.ToString());
                                                        xmlWriter.WriteEndElement();

                                                        xmlWriter.WriteStartElement("td");
                                                        xmlWriter.WriteStartElement("img");
                                                        xmlWriter.WriteAttributeString("src", String.Format(@"..\..\{0}", FilePathMap.EntityDashboardScreenshotReportFilePath(businessTransaction, false, true)));
                                                        xmlWriter.WriteEndElement();
                                                        xmlWriter.WriteEndElement();

                                                        xmlWriter.WriteStartElement("td");
                                                        xmlWriter.WriteStartElement("a");
                                                        xmlWriter.WriteAttributeString("href", String.Format(@"..\..\{0}", FilePathMap.EntityDashboardScreenshotReportFilePath(businessTransaction, false, false)));
                                                        xmlWriter.WriteAttributeString("target", "_blank");
                                                        xmlWriter.WriteString("Dashboard");
                                                        xmlWriter.WriteEndElement();
                                                        xmlWriter.WriteEndElement();

                                                        xmlWriter.WriteStartElement("td");
                                                        xmlWriter.WriteStartElement("a");
                                                        xmlWriter.WriteAttributeString("href", businessTransaction.BTLink);
                                                        xmlWriter.WriteString("BT");
                                                        xmlWriter.WriteEndElement();
                                                        xmlWriter.WriteEndElement();

                                                        xmlWriter.WriteEndElement();
                                                    }
                                                }

                                                // Move off the content placeholder
                                                xmlReader.Read();
                                                xmlReader.Read();
                                            }
                                            // List of Backends
                                            else if (xmlReader.IsStartElement("tr") == true && xmlReader.GetAttribute("id") == "trBackendsPlaceholder")
                                            {
                                                if (backendsList != null)
                                                {
                                                    foreach (APMBackend backend in backendsList)
                                                    {
                                                        xmlWriter.WriteStartElement("tr");

                                                        xmlWriter.WriteStartElement("td");
                                                        xmlWriter.WriteAttributeString("class", "Controller");
                                                        xmlWriter.WriteString(backend.Controller);
                                                        xmlWriter.WriteEndElement();

                                                        xmlWriter.WriteStartElement("td");
                                                        xmlWriter.WriteAttributeString("class", "Application");
                                                        xmlWriter.WriteString(backend.ApplicationName);
                                                        xmlWriter.WriteEndElement();

                                                        xmlWriter.WriteStartElement("td");
                                                        xmlWriter.WriteAttributeString("class", "Backend");
                                                        xmlWriter.WriteString(backend.BackendName);
                                                        xmlWriter.WriteEndElement();

                                                        xmlWriter.WriteStartElement("td");
                                                        xmlWriter.WriteAttributeString("class", "Type");
                                                        xmlWriter.WriteString(backend.BackendType);
                                                        xmlWriter.WriteEndElement();

                                                        xmlWriter.WriteStartElement("td");
                                                        xmlWriter.WriteString(backend.BackendID.ToString());
                                                        xmlWriter.WriteEndElement();

                                                        xmlWriter.WriteStartElement("td");
                                                        xmlWriter.WriteStartElement("img");
                                                        xmlWriter.WriteAttributeString("src", String.Format(@"..\..\{0}", FilePathMap.EntityDashboardScreenshotReportFilePath(backend, false, true)));
                                                        xmlWriter.WriteEndElement();
                                                        xmlWriter.WriteEndElement();

                                                        xmlWriter.WriteStartElement("td");
                                                        xmlWriter.WriteStartElement("a");
                                                        xmlWriter.WriteAttributeString("href", String.Format(@"..\..\{0}", FilePathMap.EntityDashboardScreenshotReportFilePath(backend, false, false)));
                                                        xmlWriter.WriteAttributeString("target", "_blank");
                                                        xmlWriter.WriteString("Dashboard");
                                                        xmlWriter.WriteEndElement();
                                                        xmlWriter.WriteEndElement();

                                                        xmlWriter.WriteStartElement("td");
                                                        xmlWriter.WriteStartElement("a");
                                                        xmlWriter.WriteAttributeString("href", backend.BackendLink);
                                                        xmlWriter.WriteString("Backend");
                                                        xmlWriter.WriteEndElement();
                                                        xmlWriter.WriteEndElement();

                                                        xmlWriter.WriteEndElement();
                                                    }
                                                }

                                                // Move off the content placeholder
                                                xmlReader.Read();
                                                xmlReader.Read();
                                            }
                                            // All other nodes
                                            else
                                            {
                                                WriteShallowNode(xmlReader, xmlWriter);
                                            }
                                        }
                                    }
                                }
                            }

                        }

                        #endregion

                        stepTimingTarget.NumEntities = numEntitiesTotal;
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

                #region Applications list

                List<APMApplication> applicationsList = FileIOHelper.ReadListFromCSVFile<APMApplication>(FilePathMap.APMApplicationsReportFilePath(), new APMApplicationReportMap());
                if (applicationsList != null)
                {
                    loggerConsole.Info("List of Applications");

                    XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
                    xmlWriterSettings.Indent = true;

                    XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();
                    xmlReaderSettings.DtdProcessing = DtdProcessing.Parse;
                    xmlReaderSettings.IgnoreComments = false;

                    using (StringReader stringReader = new StringReader(FileIOHelper.ReadFileFromPath(FilePathMap.ApplicationsLinksTemplateFilePath())))
                    {
                        using (XmlReader xmlReader = XmlReader.Create(stringReader, xmlReaderSettings))
                        {
                            using (XmlWriter xmlWriter = XmlWriter.Create(FilePathMap.ApplicationsLinksReportFilePath(jobConfiguration.Input.TimeRange), xmlWriterSettings))
                            {
                                while (xmlReader.Read())
                                {
                                    // Adjust version
                                    if (xmlReader.IsStartElement("td") == true && xmlReader.GetAttribute("id") == "tdVersion")
                                    {
                                        xmlWriter.WriteStartElement(xmlReader.LocalName);
                                        xmlReader.Read();
                                        xmlWriter.WriteString(String.Format(xmlReader.Value, Assembly.GetEntryAssembly().GetName().Version));
                                        xmlWriter.WriteEndElement();

                                        // Read the template string and closing /text tag to move the reader forward
                                        xmlReader.Read();
                                    }
                                    // Adjust date from
                                    else if (xmlReader.IsStartElement("td") == true && xmlReader.GetAttribute("id") == "tdFromDateTime")
                                    {
                                        xmlWriter.WriteStartElement(xmlReader.LocalName);
                                        xmlReader.Read();
                                        xmlWriter.WriteString(jobConfiguration.Input.TimeRange.From.ToLocalTime().ToString("G"));
                                        xmlWriter.WriteEndElement();

                                        // Read the template string and closing /text tag to move the reader forward
                                        xmlReader.Read();
                                    }
                                    // Adjust date to
                                    else if (xmlReader.IsStartElement("td") == true && xmlReader.GetAttribute("id") == "tdToDateTime")
                                    {
                                        xmlWriter.WriteStartElement(xmlReader.LocalName);
                                        xmlReader.Read();
                                        xmlWriter.WriteString(jobConfiguration.Input.TimeRange.To.ToLocalTime().ToString("G"));
                                        xmlWriter.WriteEndElement();

                                        // Read the template string and closing /text tag to move the reader forward
                                        xmlReader.Read();
                                    }
                                    // Adjust date timezone
                                    else if (xmlReader.IsStartElement("td") == true && xmlReader.GetAttribute("id") == "tdTimezone")
                                    {
                                        xmlWriter.WriteStartElement(xmlReader.LocalName);
                                        xmlReader.Read();
                                        xmlWriter.WriteString(TimeZoneInfo.Local.DisplayName);
                                        xmlWriter.WriteEndElement();

                                        // Read the template string and closing /text tag to move the reader forward
                                        xmlReader.Read();
                                    }
                                    // Output row for each application
                                    else if (xmlReader.IsStartElement("tr") == true && xmlReader.GetAttribute("id") == "trApplicationsPlaceholder")
                                    {
                                        foreach (APMApplication application in applicationsList)
                                        {
                                            xmlWriter.WriteStartElement("tr");

                                            xmlWriter.WriteStartElement("td");
                                            xmlWriter.WriteString(application.Controller);
                                            xmlWriter.WriteEndElement();

                                            xmlWriter.WriteStartElement("td");
                                            xmlWriter.WriteString(application.ApplicationName);
                                            xmlWriter.WriteEndElement();

                                            xmlWriter.WriteStartElement("td");
                                            xmlWriter.WriteString(application.ApplicationID.ToString());
                                            xmlWriter.WriteEndElement();

                                            xmlWriter.WriteStartElement("td");
                                            xmlWriter.WriteStartElement("img");
                                            xmlWriter.WriteAttributeString("src", FilePathMap.EntityDashboardScreenshotReportFilePath(application, false, true));
                                            xmlWriter.WriteEndElement();
                                            xmlWriter.WriteEndElement();

                                            xmlWriter.WriteStartElement("td");
                                            xmlWriter.WriteStartElement("a");
                                            xmlWriter.WriteAttributeString("href", FilePathMap.ApplicationDashboardLinksReportFilePath(application, false));
                                            xmlWriter.WriteString("Dashboards");
                                            xmlWriter.WriteEndElement();
                                            xmlWriter.WriteEndElement();

                                            xmlWriter.WriteStartElement("td");
                                            xmlWriter.WriteStartElement("a");
                                            xmlWriter.WriteAttributeString("href", application.ApplicationLink);
                                            xmlWriter.WriteString("Application");
                                            xmlWriter.WriteEndElement();
                                            xmlWriter.WriteEndElement();

                                            xmlWriter.WriteEndElement();
                                        }

                                        // Move off the content placeholder
                                        xmlReader.Read();
                                        xmlReader.Read();
                                    }
                                    // All other nodes
                                    else
                                    {
                                        WriteShallowNode(xmlReader, xmlWriter);
                                    }
                                }
                            }
                        }
                    }
                }

                #endregion

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
            logger.Trace("LicensedReports.EntityDashboards={0}", programOptions.LicensedReports.EntityDashboards);
            loggerConsole.Trace("LicensedReports.EntityDashboards={0}", programOptions.LicensedReports.EntityDashboards);
            if (programOptions.LicensedReports.EntityDashboards == false)
            {
                loggerConsole.Warn("Not licensed for entity dashboard/flowmap screenshots");
                return false;
            }

            logger.Trace("Input.EntityDashboards={0}", jobConfiguration.Input.EntityDashboards);
            loggerConsole.Trace("Input.EntityDashboards={0}", jobConfiguration.Input.EntityDashboards);
            logger.Trace("Output.EntityDashboards={0}", jobConfiguration.Output.EntityDashboards);
            loggerConsole.Trace("Output.EntityDashboards={0}", jobConfiguration.Output.EntityDashboards);
            if (jobConfiguration.Input.EntityDashboards == false || jobConfiguration.Output.EntityDashboards == false)
            {
                loggerConsole.Trace("Skipping report of entity dashboard/flowmap screenshots");
            }
            return (jobConfiguration.Input.EntityDashboards == true && jobConfiguration.Output.EntityDashboards == true);
        }

        /// <summary>
        /// https://blogs.msdn.microsoft.com/mfussell/2005/02/12/combining-the-xmlreader-and-xmlwriter-classes-for-simple-streaming-transformations/
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="writer"></param>
        private static void WriteShallowNode(XmlReader reader, XmlWriter writer)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    writer.WriteStartElement(reader.Prefix, reader.LocalName, reader.NamespaceURI);
                    writer.WriteAttributes(reader, true);
                    if (reader.IsEmptyElement)
                    {
                        writer.WriteEndElement();
                    }
                    break;
                case XmlNodeType.Text:
                    writer.WriteString(reader.Value);
                    break;
                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                    writer.WriteWhitespace(reader.Value);
                    break;
                case XmlNodeType.CDATA:
                    writer.WriteCData(reader.Value);
                    break;
                case XmlNodeType.EntityReference:
                    writer.WriteEntityRef(reader.Name);
                    break;
                case XmlNodeType.XmlDeclaration:
                case XmlNodeType.ProcessingInstruction:
                    writer.WriteProcessingInstruction(reader.Name, reader.Value);
                    break;
                case XmlNodeType.DocumentType:
                    writer.WriteDocType(reader.Name, reader.GetAttribute("PUBLIC"), reader.GetAttribute("SYSTEM"), reader.Value);
                    break;
                case XmlNodeType.Comment:
                    writer.WriteComment(reader.Value);
                    break;
                case XmlNodeType.EndElement:
                    writer.WriteFullEndElement();
                    break;
                case XmlNodeType.Attribute:
                    writer.WriteAttributeString(reader.LocalName, reader.Value);
                    break;
            }
        }

        private static bool resizeDashboardScreenshot(string originalImageFilePath, string thumbnailImageFilePath)
        {
            try
            {
                logger.Trace("Loading image from {0}", originalImageFilePath);
                Image fullSizeImage = Image.FromFile(originalImageFilePath);

                Image thumbNailImage = resizeImage(fullSizeImage, 200, 200);

                logger.Trace("Saving resized image to {0}", thumbnailImageFilePath);
                thumbNailImage.Save(thumbnailImageFilePath);
            }
            catch (Exception ex)
            {
                loggerConsole.Warn("Unable to resize {0}", originalImageFilePath);

                logger.Warn(ex);
                loggerConsole.Warn(ex);

                return false;
            }

            return true;
        }

        /// <summary>
        /// https://stackoverflow.com/questions/2808887/create-thumbnail-image
        /// </summary>
        /// <param name="image"></param>
        /// <param name="maxWidth"></param>
        /// <param name="maxHeight"></param>
        /// <returns></returns>
        private static Image resizeImage(Image image, int maxWidth, int maxHeight)
        {
            int newWidth;
            int newHeight;

            //check if the with or height of the image exceeds the maximum specified, if so calculate the new dimensions
            if (image.Width > maxWidth || image.Height > maxHeight)
            {
                double ratioX = (double)maxWidth / image.Width;
                double ratioY = (double)maxHeight / image.Height;
                double ratio = Math.Min(ratioX, ratioY);

                newWidth = (int)(image.Width * ratio);
                newHeight = (int)(image.Height * ratio);
            }
            else
            {
                newWidth = image.Width;
                newHeight = image.Height;
            }

            //start the resize with a new image
            Bitmap newImage = new Bitmap(newWidth, newHeight);

            //set the new resolution
            newImage.SetResolution(96, 96);

            //start the resizing
            using (var graphics = Graphics.FromImage(newImage))
            {
                //set some encoding specs
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                graphics.DrawImage(image, 0, 0, newWidth, newHeight);
            }

            //save the image to a memorystream to apply the compression level
            using (MemoryStream ms = new MemoryStream())
            {
                newImage.Save(ms, ImageFormat.Png);

                //save the image as byte array here if you want the return type to be a Byte Array instead of Image
                //byte[] imageAsByteArray = ms.ToArray();
            }

            //return the image
            return newImage;
        }
    }
}
