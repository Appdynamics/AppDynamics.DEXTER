using AppDynamics.Dexter.ReportObjectMaps;
using AppDynamics.Dexter.ReportObjects;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class IndexControllerVersionAndApplications : JobStepIndexBase
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

                bool reportFolderCleaned = false;

                // Process each Controller once
                int i = 0;
                var controllers = jobConfiguration.Target.GroupBy(t => t.Controller);
                foreach (var controllerGroup in controllers)
                {
                    Stopwatch stopWatchTarget = new Stopwatch();
                    stopWatchTarget.Start();

                    JobTarget jobTarget = controllerGroup.ToList()[0];

                    StepTiming stepTimingTarget = new StepTiming();
                    stepTimingTarget.Controller = jobTarget.Controller;
                    stepTimingTarget.ApplicationName = jobTarget.Application;
                    stepTimingTarget.ApplicationID = jobTarget.ApplicationID;
                    stepTimingTarget.JobFileName = programOptions.OutputJobFilePath;
                    stepTimingTarget.StepName = jobConfiguration.Status.ToString();
                    stepTimingTarget.StepID = (int)jobConfiguration.Status;
                    stepTimingTarget.StartTime = DateTime.Now;

                    stepTimingTarget.NumEntities = 0;

                    try
                    {
                        this.DisplayJobTargetStartingStatus(jobConfiguration, jobTarget, i + 1);

                        #region Controller Version

                        loggerConsole.Info("Controller Version");

                        // Create this row 
                        ControllerSummary controllerSummary = new ControllerSummary();
                        controllerSummary.Controller = jobTarget.Controller;
                        controllerSummary.ControllerLink = String.Format(DEEPLINK_CONTROLLER, controllerSummary.Controller, DEEPLINK_TIMERANGE_LAST_15_MINUTES);

                        // Lookup version
                        // Load the configuration.xml from the child to parse the version
                        XmlDocument configXml = FileIOHelper.LoadXmlDocumentFromFile(FilePathMap.ControllerVersionDataFilePath(jobTarget));
                        if (configXml != null)
                        {
                            //<serverstatus version="1" vendorid="">
                            //    <available>true</available>
                            //    <serverid/>
                            //    <serverinfo>
                            //        <vendorname>AppDynamics</vendorname>
                            //        <productname>AppDynamics Application Performance Management</productname>
                            //        <serverversion>004-004-001-000</serverversion>
                            //        <implementationVersion>Controller v4.4.1.0 Build 164 Commit 6e1fd94d18dc87c1ecab2da573f98cea49d31c3a</implementationVersion>
                            //    </serverinfo>
                            //    <startupTimeInSeconds>19</startupTimeInSeconds>
                            //</serverstatus>
                            string controllerVersion = configXml.SelectSingleNode("serverstatus/serverinfo/serverversion").InnerText;
                            string[] controllerVersionArray = controllerVersion.Split('-');
                            int[] controllerVersionArrayNum = new int[controllerVersionArray.Length];
                            for (int j = 0; j < controllerVersionArray.Length; j++)
                            {
                                controllerVersionArrayNum[j] = Convert.ToInt32(controllerVersionArray[j]);
                            }
                            controllerVersion = String.Join(".", controllerVersionArrayNum);
                            controllerSummary.Version = controllerVersion;
                            controllerSummary.VersionDetail = configXml.SelectSingleNode("serverstatus/serverinfo/implementationVersion").InnerText;
                            controllerSummary.StartupTime = Convert.ToInt32(configXml.SelectSingleNode("serverstatus/startupTimeInSeconds").InnerText);
                        }
                        else
                        {
                            controllerSummary.Version = "No config data";
                        }

                        #endregion

                        #region All Applications

                        JObject allApplicationsContainerObject = FileIOHelper.LoadJObjectFromFile(FilePathMap.AllApplicationsDataFilePath(jobTarget));
                        JArray mobileApplicationsArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.MOBILEApplicationsDataFilePath(jobTarget));

                        List<ControllerApplication> controllerApplicationsList = new List<ControllerApplication>(100);

                        if (isTokenPropertyNull(allApplicationsContainerObject, "apmApplications") == false)
                        {
                            loggerConsole.Info("Index List of APM Applications");

                            foreach (JObject applicationObject in allApplicationsContainerObject["apmApplications"])
                            {
                                ControllerApplication controllerApplication = new ControllerApplication();
                                controllerApplication.Controller = jobTarget.Controller;

                                populateApplicationInfo(applicationObject, controllerApplication);

                                controllerApplication.Type = APPLICATION_TYPE_APM;

                                controllerApplicationsList.Add(controllerApplication);
                            }
                        }

                        if (isTokenPropertyNull(allApplicationsContainerObject, "eumWebApplications") == false)
                        {
                            loggerConsole.Info("Index List of WEB Applications");

                            foreach (JObject applicationObject in allApplicationsContainerObject["eumWebApplications"])
                            {
                                ControllerApplication controllerApplication = new ControllerApplication();
                                controllerApplication.Controller = jobTarget.Controller;

                                populateApplicationInfo(applicationObject, controllerApplication);

                                controllerApplication.Type = APPLICATION_TYPE_WEB;

                                controllerApplicationsList.Add(controllerApplication);
                            }
                        }

                        if (isTokenPropertyNull(allApplicationsContainerObject, "iotApplications") == false)
                        {
                            loggerConsole.Info("Index List of IOT Applications");

                            foreach (JObject applicationObject in allApplicationsContainerObject["iotApplications"])
                            {
                                ControllerApplication controllerApplication = new ControllerApplication();
                                controllerApplication.Controller = jobTarget.Controller;

                                populateApplicationInfo(applicationObject, controllerApplication);

                                controllerApplication.Type = APPLICATION_TYPE_IOT;

                                controllerApplicationsList.Add(controllerApplication);
                            }
                        }

                        if (isTokenPropertyNull(allApplicationsContainerObject, "mobileAppContainers") == false)
                        {
                            loggerConsole.Info("Index List of MOBILE Applications");

                            foreach (JObject applicationObject in allApplicationsContainerObject["mobileAppContainers"])
                            {
                                ControllerApplication controllerApplication = new ControllerApplication();
                                controllerApplication.Controller = jobTarget.Controller;

                                populateApplicationInfo(applicationObject, controllerApplication);

                                controllerApplication.Type = APPLICATION_TYPE_MOBILE;

                                if (controllerApplicationsList.Where(a => a.ApplicationID == controllerApplication.ApplicationID).Count() == 0)
                                {
                                    controllerApplicationsList.Add(controllerApplication);
                                }

                                // Now go through children
                                if (mobileApplicationsArray != null)
                                {
                                    JToken mobileApplicationContainerObject = mobileApplicationsArray.Where(a => getLongValueFromJToken(a, "applicationId") == controllerApplication.ApplicationID).FirstOrDefault();
                                    if (mobileApplicationContainerObject != null)
                                    {
                                        foreach (JObject mobileApplicationChildJSON in mobileApplicationContainerObject["children"])
                                        {
                                            ControllerApplication controllerApplicationChild = controllerApplication.Clone();
                                            controllerApplicationChild.ParentApplicationID = controllerApplicationChild.ApplicationID;

                                            controllerApplicationChild.ApplicationName = getStringValueFromJToken(mobileApplicationChildJSON, "name");
                                            controllerApplicationChild.ApplicationID = getLongValueFromJToken(mobileApplicationChildJSON, "mobileAppId");

                                            controllerApplicationsList.Add(controllerApplicationChild);
                                        }
                                    }
                                }
                            }
                        }

                        if (isTokenPropertyNull(allApplicationsContainerObject, "dbMonApplication") == false)
                        {
                            loggerConsole.Info("Index DB Application");

                            JObject applicationObject = (JObject)allApplicationsContainerObject["dbMonApplication"];

                            ControllerApplication controllerApplication = new ControllerApplication();
                            controllerApplication.Controller = jobTarget.Controller;

                            populateApplicationInfo(applicationObject, controllerApplication);

                            controllerApplication.Type = APPLICATION_TYPE_DB;

                            controllerApplicationsList.Add(controllerApplication);
                        }

                        if (isTokenPropertyNull(allApplicationsContainerObject, "simApplication") == false)
                        {
                            loggerConsole.Info("Index SIM Application");

                            JObject applicationObject = (JObject)allApplicationsContainerObject["simApplication"];

                            ControllerApplication controllerApplication = new ControllerApplication();
                            controllerApplication.Controller = jobTarget.Controller;

                            populateApplicationInfo(applicationObject, controllerApplication);

                            controllerApplication.Type = APPLICATION_TYPE_SIM;

                            controllerApplicationsList.Add(controllerApplication);
                        }

                        if (isTokenPropertyNull(allApplicationsContainerObject, "analyticsApplication") == false)
                        {
                            loggerConsole.Info("Index BIQ Application");

                            JObject applicationObject = (JObject)allApplicationsContainerObject["analyticsApplication"];

                            ControllerApplication controllerApplication = new ControllerApplication();
                            controllerApplication.Controller = jobTarget.Controller;

                            populateApplicationInfo(applicationObject, controllerApplication);

                            controllerApplication.Type = APPLICATION_TYPE_BIQ;

                            controllerApplicationsList.Add(controllerApplication);
                        }

                        // Sort them
                        controllerApplicationsList = controllerApplicationsList.OrderBy(o => o.Type).ThenBy(o => o.ApplicationName).ToList();
                        FileIOHelper.WriteListToCSVFile(controllerApplicationsList, new ControllerApplicationReportMap(), FilePathMap.ControllerApplicationsIndexFilePath(jobTarget));

                        controllerSummary.NumApps = controllerApplicationsList.Count;
                        controllerSummary.NumAPMApps = controllerApplicationsList.Where(a => a.Type == APPLICATION_TYPE_APM).Count();
                        controllerSummary.NumWEBApps = controllerApplicationsList.Where(a => a.Type == APPLICATION_TYPE_WEB).Count();
                        controllerSummary.NumMOBILEApps = controllerApplicationsList.Where(a => a.Type == APPLICATION_TYPE_MOBILE).Count();
                        controllerSummary.NumSIMApps = controllerApplicationsList.Where(a => a.Type == APPLICATION_TYPE_SIM).Count();
                        controllerSummary.NumDBApps = controllerApplicationsList.Where(a => a.Type == APPLICATION_TYPE_DB).Count();
                        controllerSummary.NumBIQApps = controllerApplicationsList.Where(a => a.Type == APPLICATION_TYPE_BIQ).Count();
                        controllerSummary.NumIOTApps = controllerApplicationsList.Where(a => a.Type == APPLICATION_TYPE_IOT).Count();

                        List<ControllerSummary> controllerList = new List<ControllerSummary>(1);
                        controllerList.Add(controllerSummary);
                        FileIOHelper.WriteListToCSVFile(controllerList, new ControllerSummaryReportMap(), FilePathMap.ControllerSummaryIndexFilePath(jobTarget));

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + controllerApplicationsList.Count;

                        #endregion

                        #region Combine All for Report CSV

                        // If it is the first one, clear out the combined folder
                        if (reportFolderCleaned == false)
                        {
                            FileIOHelper.DeleteFolder(FilePathMap.ControllerEntitiesReportFolderPath());
                            Thread.Sleep(1000);
                            FileIOHelper.CreateFolder(FilePathMap.ControllerEntitiesReportFolderPath());
                            reportFolderCleaned = true;
                        }

                        // Append all the individual report files into one
                        if (File.Exists(FilePathMap.ControllerSummaryIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.ControllerSummaryIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.ControllerSummaryReportFilePath(), FilePathMap.ControllerSummaryIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.ControllerApplicationsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.ControllerApplicationsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.ControllerApplicationsReportFilePath(), FilePathMap.ControllerApplicationsIndexFilePath(jobTarget));
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

                    i++;
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

        private void populateApplicationInfo(JObject applicationJSON, ControllerApplication controllerApplication)
        {
            controllerApplication.ApplicationName = getStringValueFromJToken(applicationJSON, "name");
            controllerApplication.Description = getStringValueFromJToken(applicationJSON, "description");
            controllerApplication.ApplicationID = getLongValueFromJToken(applicationJSON, "id");
            if (isTokenPropertyNull(applicationJSON, "applicationTypeInfo") == false &&
                isTokenPropertyNull(applicationJSON["applicationTypeInfo"], "applicationTypes") == false)
            {
                controllerApplication.Types = applicationJSON["applicationTypeInfo"]["applicationTypes"].ToString(Newtonsoft.Json.Formatting.None);
            }

            controllerApplication.CreatedBy = getStringValueFromJToken(applicationJSON, "createdBy");
            controllerApplication.CreatedOnUtc = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(applicationJSON, "createdOn"));
            try { controllerApplication.CreatedOn = controllerApplication.CreatedOnUtc.ToLocalTime(); } catch { }
            controllerApplication.UpdatedBy = getStringValueFromJToken(applicationJSON, "modifiedBy");
            controllerApplication.UpdatedOnUtc = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(applicationJSON, "modifiedOn"));
            try { controllerApplication.UpdatedOn = controllerApplication.UpdatedOnUtc.ToLocalTime(); } catch { }
        }

        public override bool ShouldExecute(JobConfiguration jobConfiguration)
        {
            logger.Trace("{0} is always executed", jobConfiguration.Status);
            loggerConsole.Trace("{0} is always executed", jobConfiguration.Status);
            return true;
        }
    }
}
