using AppDynamics.Dexter.Extensions;
using AppDynamics.Dexter.ReportObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class ReportFlameGraphs : JobStepReportBase
    {
        private const int SNAPSHOTS_FLAMEGRAPH_NUMBER_OF_ENTITIES_TO_PROCESS_PER_THREAD = 1000;

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

                        string flameGraphTemplateString = FileIOHelper.ReadFileFromPath(FilePathMap.FlameGraphTemplateFilePath());

                        int numEntitiesTotal = 0;

                        // Load and bucketize the framework mappings
                        Dictionary<string, List<MethodCallLineClassTypeMapping>> methodCallLineClassToFrameworkTypeMappingDictionary = populateMethodCallMappingDictionary(FilePathMap.MethodCallLinesToFrameworkTypetMappingFilePath());

                        #endregion

                        Parallel.Invoke(
                            () =>
                            {
                                #region Application

                                List<APMApplication> applicationList = FileIOHelper.ReadListFromCSVFile<APMApplication>(FilePathMap.ApplicationIndexFilePath(jobTarget), new APMApplicationReportMap());
                                if (applicationList != null && applicationList.Count > 0)
                                {
                                    loggerConsole.Info("Flame Graphs for Application");

                                    stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + 1;

                                    APMApplication application = applicationList[0];

                                    if (File.Exists(FilePathMap.SnapshotsFoldedCallStacksIndexApplicationFilePath(jobTarget)) == true)
                                    {
                                        createFlameGraph(
                                            FilePathMap.SnapshotsFoldedCallStacksIndexApplicationFilePath(jobTarget),
                                            FilePathMap.FlameGraphReportFilePath(applicationList[0], jobTarget, jobConfiguration.Input.TimeRange, true),
                                            String.Format("{0}/{1} ({2:G}-{3:G})", application.Controller, application.ApplicationName, jobConfiguration.Input.TimeRange.From.ToLocalTime(), jobConfiguration.Input.TimeRange.To.ToLocalTime()),
                                            flameGraphTemplateString,
                                            methodCallLineClassToFrameworkTypeMappingDictionary,
                                            true);
                                    }

                                    if (File.Exists(FilePathMap.SnapshotsFoldedCallStacksWithTimeIndexApplicationFilePath(jobTarget)) == true)
                                    {
                                        createFlameGraph(
                                            FilePathMap.SnapshotsFoldedCallStacksWithTimeIndexApplicationFilePath(jobTarget),
                                            FilePathMap.FlameChartReportFilePath(applicationList[0], jobTarget, jobConfiguration.Input.TimeRange, true),
                                            String.Format("{0}/{1} ({2:G}-{3:G})", application.Controller, application.ApplicationName, jobConfiguration.Input.TimeRange.From.ToLocalTime(), jobConfiguration.Input.TimeRange.To.ToLocalTime()),
                                            flameGraphTemplateString,
                                            methodCallLineClassToFrameworkTypeMappingDictionary,
                                            true);
                                    }

                                    Interlocked.Add(ref numEntitiesTotal, applicationList.Count);
                                }

                                #endregion
                            },
                            () =>
                            {
                                #region Tier

                                List<APMTier> tiersList = FileIOHelper.ReadListFromCSVFile<APMTier>(FilePathMap.TiersIndexFilePath(jobTarget), new APMTierReportMap());
                                if (tiersList != null)
                                {
                                    loggerConsole.Info("Flame Graphs for Tiers ({0} entities)", tiersList.Count);

                                    foreach (APMTier tier in tiersList)
                                    {
                                        if (File.Exists(FilePathMap.SnapshotsFoldedCallStacksIndexEntityFilePath(jobTarget, tier)) == true)
                                        {
                                            createFlameGraph(
                                                FilePathMap.SnapshotsFoldedCallStacksIndexEntityFilePath(jobTarget, tier),
                                                FilePathMap.FlameGraphReportFilePath(tier, jobTarget, jobConfiguration.Input.TimeRange, true),
                                                String.Format("{0}/{1}/{2} ({3:G}-{4:G})", tier.Controller, tier.ApplicationName, tier.TierName, jobConfiguration.Input.TimeRange.From.ToLocalTime(), jobConfiguration.Input.TimeRange.To.ToLocalTime()),
                                                flameGraphTemplateString,
                                                methodCallLineClassToFrameworkTypeMappingDictionary,
                                                true);
                                        }

                                        if (File.Exists(FilePathMap.SnapshotsFoldedCallStacksWithTimeIndexEntityFilePath(jobTarget, tier)) == true)
                                        {
                                            createFlameGraph(
                                                FilePathMap.SnapshotsFoldedCallStacksWithTimeIndexEntityFilePath(jobTarget, tier),
                                                FilePathMap.FlameChartReportFilePath(tier, jobTarget, jobConfiguration.Input.TimeRange, true),
                                                String.Format("{0}/{1}/{2} ({3:G}-{4:G})", tier.Controller, tier.ApplicationName, tier.TierName, jobConfiguration.Input.TimeRange.From.ToLocalTime(), jobConfiguration.Input.TimeRange.To.ToLocalTime()),
                                                flameGraphTemplateString,
                                                methodCallLineClassToFrameworkTypeMappingDictionary,
                                                false);
                                        }

                                    }

                                    Interlocked.Add(ref numEntitiesTotal, tiersList.Count);

                                    loggerConsole.Info("Flame Graphs for Tiers Done ({0} entities)", tiersList.Count);
                                }

                                #endregion
                            },
                            () =>
                            {
                                #region Nodes

                                List<APMNode> nodesList = FileIOHelper.ReadListFromCSVFile<APMNode>(FilePathMap.NodesIndexFilePath(jobTarget), new APMNodeReportMap());
                                if (nodesList != null)
                                {
                                    loggerConsole.Info("Flame Graphs for Nodes ({0} entities)", nodesList.Count);

                                    foreach (APMNode node in nodesList)
                                    {
                                        if (File.Exists(FilePathMap.SnapshotsFoldedCallStacksIndexEntityFilePath(jobTarget, node)) == true)
                                        {
                                            createFlameGraph(
                                                FilePathMap.SnapshotsFoldedCallStacksIndexEntityFilePath(jobTarget, node),
                                                FilePathMap.FlameGraphReportFilePath(node, jobTarget, jobConfiguration.Input.TimeRange, true),
                                                String.Format("{0}/{1}/{2}, Node {3} ({4:G}-{5:G})", node.Controller, node.ApplicationName, node.TierName, node.NodeName, jobConfiguration.Input.TimeRange.From.ToLocalTime(), jobConfiguration.Input.TimeRange.To.ToLocalTime()),
                                                flameGraphTemplateString,
                                                methodCallLineClassToFrameworkTypeMappingDictionary,
                                                true);
                                        }

                                        if (File.Exists(FilePathMap.SnapshotsFoldedCallStacksWithTimeIndexEntityFilePath(jobTarget, node)) == true)
                                        {
                                            createFlameGraph(
                                                FilePathMap.SnapshotsFoldedCallStacksWithTimeIndexEntityFilePath(jobTarget, node),
                                                FilePathMap.FlameChartReportFilePath(node, jobTarget, jobConfiguration.Input.TimeRange, true),
                                                String.Format("{0}/{1}/{2}, Node {3} ({4:G}-{5:G})", node.Controller, node.ApplicationName, node.TierName, node.NodeName, jobConfiguration.Input.TimeRange.From.ToLocalTime(), jobConfiguration.Input.TimeRange.To.ToLocalTime()),
                                                flameGraphTemplateString,
                                                methodCallLineClassToFrameworkTypeMappingDictionary,
                                                true);
                                        }
                                    }

                                    Interlocked.Add(ref numEntitiesTotal, nodesList.Count);

                                    loggerConsole.Info("Flame Graphs for Nodes Done ({0} entities)", nodesList.Count);
                                }

                                #endregion
                            },
                            () =>
                            {
                                #region Business Transactions

                                List<BusinessTransaction> businessTransactionsList = FileIOHelper.ReadListFromCSVFile<BusinessTransaction>(FilePathMap.BusinessTransactionsIndexFilePath(jobTarget), new BusinessTransactionReportMap());
                                if (businessTransactionsList != null)
                                {
                                    loggerConsole.Info("Flame Graphs for Business Transactions ({0} entities)", businessTransactionsList.Count);

                                    foreach (BusinessTransaction businessTransaction in businessTransactionsList)
                                    {
                                        if (File.Exists(FilePathMap.SnapshotsFoldedCallStacksIndexEntityFilePath(jobTarget, businessTransaction)) == true)
                                        {
                                            createFlameGraph(
                                                FilePathMap.SnapshotsFoldedCallStacksIndexEntityFilePath(jobTarget, businessTransaction),
                                                FilePathMap.FlameGraphReportFilePath(businessTransaction, jobTarget, jobConfiguration.Input.TimeRange, true),
                                                String.Format("{0}/{1}/{2}, BT {3} ({4:G}-{5:G})", businessTransaction.Controller, businessTransaction.ApplicationName, businessTransaction.TierName, businessTransaction.BTName, jobConfiguration.Input.TimeRange.From.ToLocalTime(), jobConfiguration.Input.TimeRange.To.ToLocalTime()),
                                                flameGraphTemplateString,
                                                methodCallLineClassToFrameworkTypeMappingDictionary,
                                                true);
                                        }

                                        if (File.Exists(FilePathMap.SnapshotsFoldedCallStacksWithTimeIndexEntityFilePath(jobTarget, businessTransaction)) == true)
                                        {
                                            createFlameGraph(
                                                FilePathMap.SnapshotsFoldedCallStacksWithTimeIndexEntityFilePath(jobTarget, businessTransaction),
                                                FilePathMap.FlameChartReportFilePath(businessTransaction, jobTarget, jobConfiguration.Input.TimeRange, true),
                                                String.Format("{0}/{1}/{2}, BT {3} ({4:G}-{5:G})", businessTransaction.Controller, businessTransaction.ApplicationName, businessTransaction.TierName, businessTransaction.BTName, jobConfiguration.Input.TimeRange.From.ToLocalTime(), jobConfiguration.Input.TimeRange.To.ToLocalTime()),
                                                flameGraphTemplateString,
                                                methodCallLineClassToFrameworkTypeMappingDictionary,
                                                true);
                                        }
                                    }

                                    Interlocked.Add(ref numEntitiesTotal, businessTransactionsList.Count);

                                    loggerConsole.Info("Flame Graphs for Business Transactions Done ({0} entities)", businessTransactionsList.Count);
                                }

                                #endregion
                            }
                        );

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
            logger.Trace("Input.Snapshots={0}", jobConfiguration.Input.Snapshots);
            loggerConsole.Trace("Input.Snapshots={0}", jobConfiguration.Input.Snapshots);
            logger.Trace("Output.FlameGraphs={0}", jobConfiguration.Output.FlameGraphs);
            loggerConsole.Trace("Output.FlameGraphs={0}", jobConfiguration.Output.FlameGraphs);
            if (jobConfiguration.Input.Snapshots == false || jobConfiguration.Output.FlameGraphs == false)
            {
                loggerConsole.Trace("Skipping report of flame graphs");
            }
            return (jobConfiguration.Input.Snapshots == true && jobConfiguration.Output.FlameGraphs == true);
        }

        /// <summary>
        /// Review http://www.brendangregg.com/FlameGraphs/cpuflamegraphs.html
        /// Review http://techblog.netflix.com/2015/07/java-in-flames.html
        /// Review http://techblog.netflix.com/2014/11/nodejs-in-flames.html
        /// Implementation courtesy of learning how https://github.com/davepacheco/node-stackvis does it, which in itself is reimplementing Brendan's work
        /// </summary>
        /// <param name="foldedStackFilePath"></param>
        /// <param name="flameGraphFilePath"></param>
        /// <param name="descriptionText"></param>
        /// <param name="flameGraphTemplate"></param>
        /// <returns></returns>
        private bool createFlameGraph(
            string foldedStackFilePath,
            string flameGraphFilePath,
            string descriptionText,
            string flameGraphTemplate,
            Dictionary<string, List<MethodCallLineClassTypeMapping>> methodCallLineClassToFrameworkTypeMappingDictionary,
            bool progressToConsole)
        {
            // If it already been rendered, bail
            if (File.Exists(flameGraphFilePath) == true)
            {
                return false;
            }

            // Check to see if the input data exists
            if (File.Exists(foldedStackFilePath) == false)
            {
                logger.Warn("Flame graph file {0} does not exist", foldedStackFilePath);
                return false;
            }

            logger.Trace("Rendering flame graph {0} from {1}", flameGraphFilePath, foldedStackFilePath);

            #region Brendan Gregg's Perl version

            //if (false)
            //{
            //    string foldedStackPerlFilePath = String.Format("{0}.pl.svg", flameGraphFilePath);

            //    // Use Brendan Gregg's script but only in debug mode
            //    // http://www.brendangregg.com/FlameGraphs/cpuflamegraphs.html
            //    if (File.Exists(foldedStackPerlFilePath) == false)
            //    {
            //        Process p = new Process();
            //        // Redirect the output stream of the child process.
            //        p.StartInfo.UseShellExecute = false;
            //        p.StartInfo.RedirectStandardOutput = true;
            //        p.StartInfo.RedirectStandardError = true;
            //        p.StartInfo.FileName = @"C:\Perl64\bin\perl.exe";
            //        p.StartInfo.Arguments = String.Format(@"""C:\appdynamics\FlameGraph\FlameGraph-master\flamegraph.pl"" -width 1600 --title ""{1}"" ""{0}""", foldedStackFilePath, descriptionText);
            //        p.Start();
            //        string output = p.StandardOutput.ReadToEnd();
            //        p.WaitForExit();

            //        if (output.Contains("ERROR:") == true)
            //        {
            //            // Do nothing
            //            return false;
            //        }
            //        else
            //        {
            //            FileIOHelper.SaveFileToPath(output, foldedStackPerlFilePath);
            //        }
            //    }
            //}

            #endregion

            #region Create object representation of Flame Graph

            // Read folded call stacks with counts of occurrences
            List<FoldedStackLine> foldedCallStacksList = FileIOHelper.ReadListFromCSVFile<FoldedStackLine>(foldedStackFilePath, new FoldedStackLineReportMap());
            if (foldedCallStacksList.Count == 0)
            {
                logger.Warn("Flame graph file {0} contains no folded frames", foldedStackFilePath);
                return false;
            }

            // Sort the call stacks by the string portion alphabetically
            List<FoldedStackLine> sortedFoldedCallStacksList = foldedCallStacksList.OrderBy(s => s.FoldedStack).ToList();

            // Depth of the tallest call stack, used to measure the height of final canvas
            int maxFrameDepth = 0;
            // Width of the samples, used to measure the width of the frames relative to width of the final canvas
            int maxSampleWidth = 0;
            // List of boxes that last call stack consists of, to compare the current call stack with
            List<FlameGraphBox> lastCallStack = new List<FlameGraphBox>();
            // All boxes that should be displayed on the Flame Graph. Assume 15 frames per call stack on average
            List<FlameGraphBox> flameGraphBoxes = new List<FlameGraphBox>(sortedFoldedCallStacksList.Count * 15);

            // Process call stacks one by one, stacking frames from left to right in alphabetical order, and stacking them from bottom up
            foreach (FoldedStackLine foldedStackLine in sortedFoldedCallStacksList)
            {
                // Add an empty frame for frame representing "all samples" frame. 
                // Parse the folded stack into array of frames, prepended with empty frame to account for all items
                string[] stackFrames = String.Format("all;{0}", foldedStackLine.FoldedStack).Split(';');

                // Calculate maximum depth for later measurement of the overall canvas
                if (stackFrames.Length > maxFrameDepth)
                {
                    maxFrameDepth = stackFrames.Length;
                }

                // This becomes the current call stack
                List<FlameGraphBox> thisCallStack = new List<FlameGraphBox>(stackFrames.Length);

                // If not the first stack processed, compare this new stack frames to the previous stack frames, going up 
                // Keep comparing until you see no differences or hit the top
                // measure that height 
                int depthOfSameFrames = 0;
                for (int i = 0; i < lastCallStack.Count && i < stackFrames.Length; i++)
                {
                    // Compare frames at the same level
                    if (String.Compare(lastCallStack[i].FullName, stackFrames[i], true) == 0)
                    {
                        // Same frame. Make it wider by adding the number of samples to the already existing flame graph box
                        FlameGraphBox flameGraphBox = lastCallStack[i];
                        flameGraphBox.Samples = flameGraphBox.Samples + foldedStackLine.NumSamples;
                        if (i > 0)
                        {
                            flameGraphBox.Duration = flameGraphBox.Duration + foldedStackLine.StackTimingArray[i - 1];
                        }

                        // Add this frame to the current call stack
                        thisCallStack.Add(flameGraphBox);

                        // Measure the depth of the frames that are the same 
                        depthOfSameFrames++;
                    }
                    else
                    {
                        // Not the same level, time to break out to move to next step
                        break;
                    }
                }

                // Each of these stack frames was present in previous stack but not this one
                // Using last frame output, go from height measured in step 3 down to the bottom, and mark them as ended
                for (int i = lastCallStack.Count - 1; i >= depthOfSameFrames; i--)
                {
                    FlameGraphBox flameGraphBox = lastCallStack[i];
                }

                // Each of these frames was not present in the previous stack, so start them
                // Using this frame output, go from height measured in step 3 up to the top, marking them as having started
                for (int i = depthOfSameFrames; i < stackFrames.Length; i++)
                {
                    FlameGraphBox flameGraphBox = new FlameGraphBox();
                    // We will output XML, and occasionally the bad characters will trip us up. But I don't want to encode everything
                    try
                    {
                        flameGraphBox.FullName = XmlConvert.VerifyXmlChars(stackFrames[i]);
                    }
                    catch (XmlException ex)
                    {
                        // When this is thrown, that means that there are invalid characters. Encode the string then.
                        flameGraphBox.FullName = XmlConvert.EncodeName(stackFrames[i]);
                    }
                    flameGraphBox.Start = maxSampleWidth;
                    flameGraphBox.Samples = foldedStackLine.NumSamples;
                    if (i > 0 && i <= foldedStackLine.StackTimingArray.Length)
                    {
                        flameGraphBox.Duration = flameGraphBox.Duration + foldedStackLine.StackTimingArray[i - 1];
                    }

                    flameGraphBox.Depth = i;

                    // Add this frame to the current call stack
                    thisCallStack.Add(flameGraphBox);

                    // Add it to all boxes
                    flameGraphBoxes.Add(flameGraphBox);
                }

                // If items at the same level are idenfical in MethodCall, make the leftmost item wider in adding this 
                // frames' number of samples to the leftmost. Discard the new frame, and use the left one, wider instead
                // If items at the same level are not identical in MethodCall, use this frame, adjusting its start to be
                // right after the leftmost one's end

                // Move the starting point for next call stack right by the value of the current call stack sample rate
                maxSampleWidth = maxSampleWidth + foldedStackLine.NumSamples;

                // Make this call stack last and move on to next folded call stack
                lastCallStack = thisCallStack;
            }

            #endregion

            #region Output Flame Graph SVG file

            // Height of the single frame
            int frameHeight = 16;
            // Minimum width of the frame, beyond which it isn't output
            decimal minFrameWidth = 0.1m;

            // Area to pad from the top to get the frames
            int flameGraphHeightPaddingTop = 30;
            // Area to pad from the bottom to get the frames
            int flameGraphHeightPaddingBottom = 30;

            // The height of the flame graph
            int flameGraphHeight = maxFrameDepth * frameHeight + flameGraphHeightPaddingTop + flameGraphHeightPaddingBottom;

            // Width of 1 sample relative to the total width
            decimal widthPerSample = 1600 / (decimal)maxSampleWidth;

            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
            xmlWriterSettings.Indent = true;

            XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();
            xmlReaderSettings.DtdProcessing = DtdProcessing.Parse;
            xmlReaderSettings.IgnoreComments = false;

            FileIOHelper.CreateFolder(Path.GetDirectoryName(flameGraphFilePath));

            using (StringReader stringReader = new StringReader(flameGraphTemplate))
            {
                using (XmlReader xmlReader = XmlReader.Create(stringReader, xmlReaderSettings))
                {
                    using (XmlWriter xmlWriter = XmlWriter.Create(flameGraphFilePath, xmlWriterSettings))
                    {
                        xmlWriter.WriteDocType("svg", "-//W3C//DTD SVG 1.1//EN", "http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd", null);
                        while (xmlReader.Read())
                        {
                            // Adjust SVG element
                            if (xmlReader.IsStartElement("svg") == true)
                            {
                                xmlWriter.WriteStartElement("svg", "http://www.w3.org/2000/svg");

                                while (xmlReader.MoveToNextAttribute())
                                {
                                    if (xmlReader.LocalName == "height")
                                    {
                                        xmlWriter.WriteAttributeString("height", flameGraphHeight.ToString());
                                    }
                                    else if (xmlReader.LocalName == "viewBox")
                                    {
                                        xmlWriter.WriteAttributeString("viewBox", xmlReader.Value.Replace("1200", flameGraphHeight.ToString()));
                                    }
                                    else if (xmlReader.LocalName == "xmlns")
                                    {
                                        // Skip that one
                                    }
                                    else
                                    {
                                        WriteShallowNode(xmlReader, xmlWriter);
                                    }
                                }
                            }
                            // Adjust the size of the background rectangle
                            else if (xmlReader.IsStartElement("rect") == true && xmlReader.GetAttribute("id") == "background")
                            {
                                xmlWriter.WriteStartElement("rect");

                                while (xmlReader.MoveToNextAttribute())
                                {
                                    if (xmlReader.LocalName == "height")
                                    {
                                        xmlWriter.WriteAttributeString("height", flameGraphHeight.ToString());
                                    }
                                    else
                                    {
                                        WriteShallowNode(xmlReader, xmlWriter);
                                    }
                                }

                                xmlWriter.WriteEndElement();
                            }
                            // Set Title
                            else if (xmlReader.IsStartElement("text") == true && xmlReader.GetAttribute("id") == "title")
                            {
                                xmlWriter.WriteStartElement(xmlReader.LocalName);
                                while (xmlReader.MoveToNextAttribute())
                                {
                                    WriteShallowNode(xmlReader, xmlWriter);
                                }
                                xmlWriter.WriteString(descriptionText);
                                xmlWriter.WriteEndElement();

                                // Read the template string and closing /text tag to move the reader forward
                                xmlReader.Read();
                                xmlReader.Read();
                            }
                            // Set Mouseover Function display area location
                            else if (xmlReader.IsStartElement("text") == true && xmlReader.GetAttribute("id") == "details")
                            {
                                xmlWriter.WriteStartElement(xmlReader.LocalName);
                                while (xmlReader.MoveToNextAttribute())
                                {
                                    if (xmlReader.LocalName == "y")
                                    {
                                        xmlWriter.WriteAttributeString("y", (flameGraphHeight - 12).ToString());
                                    }
                                    else
                                    {
                                        WriteShallowNode(xmlReader, xmlWriter);
                                    }
                                }
                                xmlReader.Read();

                                WriteShallowNode(xmlReader, xmlWriter);
                                xmlWriter.WriteEndElement();

                                // Read the template string and closing /text tag to move the reader forward
                                xmlReader.Read();
                            }
                            // Adjust version
                            else if (xmlReader.IsStartElement("text") == true && xmlReader.GetAttribute("id") == "version")
                            {
                                xmlWriter.WriteStartElement(xmlReader.LocalName);
                                while (xmlReader.MoveToNextAttribute())
                                {
                                    if (xmlReader.LocalName == "y")
                                    {
                                        xmlWriter.WriteAttributeString("y", (flameGraphHeight - 12).ToString());
                                    }
                                    else
                                    {
                                        WriteShallowNode(xmlReader, xmlWriter);
                                    }
                                }
                                xmlReader.Read();

                                xmlWriter.WriteString(String.Format(xmlReader.Value, Assembly.GetEntryAssembly().GetName().Version));
                                xmlWriter.WriteEndElement();

                                // Read the template string and closing /text tag to move the reader forward
                                xmlReader.Read();
                            }
                            // Found content placeholder, let's output all the flame graphs
                            else if (xmlReader.IsStartElement("g") == true && xmlReader.GetAttribute("id") == "contentPlaceholder")
                            {
                                if (progressToConsole == true && flameGraphBoxes.Count >= 10000)
                                {
                                    loggerConsole.Info("Outputing Flame Graph Boxes {0}", flameGraphBoxes.Count);
                                }

                                // Output each flame graph element one by one
                                //<g onmouseover="s(this)" onmouseout="c()" onclick="zoom(this)">
                                //  <title>AALevel4 (4 samples, 22.22%)</title>
                                //  <rect x="403.3" y="69" width="262.3" height="15.0" fill="rgb(237,67,16)" rx="2" ry="2" />
                                //  <text text-anchor="" x="406.33" y="79.5" font-size="12" font-family="Verdana" fill="rgb(0,0,0)">AALevel4</text>
                                //</g>
                                for (int i = 0; i < flameGraphBoxes.Count; i++)
                                {
                                    FlameGraphBox flameGraphBox = flameGraphBoxes[i];

                                    // Measure the flame graph frame
                                    decimal x1 = Math.Round(flameGraphBox.Start * widthPerSample, 2);
                                    decimal y1 = flameGraphHeight - flameGraphHeightPaddingBottom - (flameGraphBox.Depth + 1) * frameHeight + 1;
                                    decimal x2 = Math.Round(flameGraphBox.End * widthPerSample, 2);
                                    decimal y2 = flameGraphHeight - flameGraphHeightPaddingBottom - flameGraphBox.Depth * frameHeight;

                                    decimal boxWidth = x2 - x1;

                                    // Is the sample very narrow?
                                    if (boxWidth <= minFrameWidth)
                                    {
                                        // Skip this really narrow frame
                                        continue;
                                    }

                                    // Output container with mouseover and mouseout
                                    xmlWriter.WriteStartElement("g");
                                    xmlWriter.WriteAttributeString("class", "func_g");
                                    xmlWriter.WriteAttributeString("id", String.Format("flameGraphBox_{0}", i));
                                    xmlWriter.WriteAttributeString("onmouseover", "s(this)");
                                    xmlWriter.WriteAttributeString("onmouseout", "c()");
                                    xmlWriter.WriteAttributeString("onclick", "zoom(this)");

                                    // Output mouseover hint
                                    xmlWriter.WriteStartElement("title");
                                    long averageDuration = (long)Math.Round(flameGraphBox.Duration / (decimal)flameGraphBox.Samples, 0);
                                    xmlWriter.WriteString(String.Format("{0} (Duration Total {1}, Average {2}, {3} samples, {4:P})", flameGraphBox.FullName, flameGraphBox.Duration, averageDuration, flameGraphBox.Samples, flameGraphBox.Samples / (decimal)maxSampleWidth));
                                    xmlWriter.WriteEndElement();

                                    // Determine the color for the box, using the framework lookup
                                    MethodCallLineClassTypeMapping methodCallLineClassTypeMapping = getMethodCallLineClassTypeMappingFromClassOrFunctionName(flameGraphBox.FullName, methodCallLineClassToFrameworkTypeMappingDictionary);
                                    Color colorStart = colorFlameGraphStackStart;
                                    Color colorEnd = colorFlameGraphStackEnd;

                                    // No mapping
                                    if (methodCallLineClassTypeMapping == null)
                                    {
                                        // Could this be Node.JS?
                                        // it has :: and .js in it
                                        if (flameGraphBox.FullName.Contains("::") == true &&
                                            flameGraphBox.FullName.Contains(".js") == true)
                                        {
                                            colorStart = colorFlameGraphStackNodeJSStart;
                                            colorEnd = colorFlameGraphStackNodeJSEnd;
                                        }
                                    }
                                    else if (methodCallLineClassTypeMapping.FlameGraphColorStart.Length != 0 || methodCallLineClassTypeMapping.FlameGraphColorEnd.Length != 0)
                                    {
                                        colorStart = getColorFromHexString(methodCallLineClassTypeMapping.FlameGraphColorStart);
                                        colorEnd = getColorFromHexString(methodCallLineClassTypeMapping.FlameGraphColorEnd);
                                    }

                                    // Output the rectangle with pretty colors
                                    xmlWriter.WriteStartElement("rect");
                                    xmlWriter.WriteAttributeString("x", x1.ToString());
                                    xmlWriter.WriteAttributeString("y", y1.ToString());
                                    xmlWriter.WriteAttributeString("width", (x2 - x1).ToString());
                                    xmlWriter.WriteAttributeString("height", (y2 - y1).ToString());
                                    xmlWriter.WriteAttributeString("fill", getFlameGraphBoxColorAsRGBString(flameGraphBox.Depth, maxFrameDepth, colorStart, colorEnd));
                                    xmlWriter.WriteAttributeString("rx", "2");
                                    xmlWriter.WriteAttributeString("ry", "2");

                                    if (flameGraphBox.Duration > 0 && averageDuration > 100)
                                    {
                                        xmlWriter.WriteAttributeString("stroke", "#BF0000");
                                        if (averageDuration <= 250)
                                        {
                                            xmlWriter.WriteAttributeString("stroke-width", "1");
                                        }
                                        else if (averageDuration <= 500)
                                        {
                                            xmlWriter.WriteAttributeString("stroke-width", "2");
                                        }
                                        else if (averageDuration <= 1000)
                                        {
                                            xmlWriter.WriteAttributeString("stroke-width", "3");
                                        }
                                        else if (averageDuration <= 5000)
                                        {
                                            xmlWriter.WriteAttributeString("stroke-width", "4");
                                        }
                                        else if (averageDuration <= 10000)
                                        {
                                            xmlWriter.WriteAttributeString("stroke-width", "5");
                                        }
                                        else
                                        {
                                            xmlWriter.WriteAttributeString("stroke-width", "6");
                                        }
                                    }

                                    xmlWriter.WriteEndElement();

                                    // Output text label that is visible and positioned right over the rectanlge
                                    xmlWriter.WriteStartElement("text");
                                    xmlWriter.WriteAttributeString("text-anchor", "left");
                                    xmlWriter.WriteAttributeString("x", (x1 + 2).ToString());
                                    xmlWriter.WriteAttributeString("y", (y1 + 11).ToString());
                                    xmlWriter.WriteAttributeString("font-size", "11");
                                    xmlWriter.WriteAttributeString("font-family", "monospace");
                                    // Is the box big enough for the text?
                                    if (boxWidth > 25)
                                    {
                                        // Measure the text and put .. if it won't fit
                                        int numberOfCharactersThatWillFit = (int)Math.Round(Decimal.Divide(boxWidth, 8.4m), 0);

                                        if (flameGraphBox.FullName.Length > numberOfCharactersThatWillFit)
                                        {
                                            xmlWriter.WriteString(String.Format("{0}..", flameGraphBox.FullName.Substring(0, numberOfCharactersThatWillFit)));
                                        }
                                        else
                                        {
                                            xmlWriter.WriteString(flameGraphBox.FullName);
                                        }
                                    }
                                    else
                                    {
                                        xmlWriter.WriteString(String.Empty);
                                    }
                                    xmlWriter.WriteEndElement();

                                    xmlWriter.WriteEndElement();

                                    // Show progress
                                    if (progressToConsole == true)
                                    {
                                        if (i != 0 && i % 10000 == 0)
                                        {
                                            Console.Write("[{0}].", i);
                                        }
                                    }
                                }

                                if (progressToConsole == true && flameGraphBoxes.Count >= 10000)
                                {
                                    Console.WriteLine();
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

            #endregion

            return true;
        }

        private static string getFlameGraphBoxColorAsRGBString(int thisDepth, int maxDepth, Color startColor, Color endColor)
        {
            double rStep = (endColor.R - startColor.R) / (double)maxDepth;
            double gStep = (endColor.G - startColor.G) / (double)maxDepth;
            double bStep = (endColor.B - startColor.B) / (double)maxDepth;

            var rActual = startColor.R + (int)(rStep * thisDepth);
            var gActual = startColor.G + (int)(gStep * thisDepth);
            var bActual = startColor.B + (int)(bStep * thisDepth);

            // Make color in RGBA format, where A (Alpha) is exactly half 2/3 of the way to 255
            return String.Format("#{0:X2}{1:X2}{2:X2}B8", rActual, gActual, bActual);
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

        public static MethodCallLineClassTypeMapping getMethodCallLineClassTypeMappingFromClassOrFunctionName(string classOrFunctionName, Dictionary<string, List<MethodCallLineClassTypeMapping>> methodCallLineClassToFrameworkTypeMappingDictionary)
        {
            if (classOrFunctionName.Length > 0)
            {
                // Find mapping
                string keyLetterOfMappingList = classOrFunctionName.Substring(0, 1).ToLower();
                if (methodCallLineClassToFrameworkTypeMappingDictionary.ContainsKey(keyLetterOfMappingList) == true)
                {
                    List<MethodCallLineClassTypeMapping> methodCallLineClassToFrameworkTypeMappingList = methodCallLineClassToFrameworkTypeMappingDictionary[keyLetterOfMappingList];
                    foreach (MethodCallLineClassTypeMapping mapping in methodCallLineClassToFrameworkTypeMappingList)
                    {
                        if (classOrFunctionName.StartsWith(mapping.ClassPrefix, StringComparison.InvariantCultureIgnoreCase) == true)
                        {
                            return mapping;
                        }
                    }
                }
            }

            return null;
        }

    }
}
