﻿using AppDynamics.Dexter.ReportObjectMaps;
using AppDynamics.Dexter.ReportObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class ExtractControllerVersionAndApplications : JobStepBase
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
                if (this.ShouldExecute(programOptions, jobConfiguration) == false)
                {
                    return true;
                }

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

                    stepTimingTarget.NumEntities = 1;

                    try
                    {
                        this.DisplayJobTargetStartingStatus(jobConfiguration, jobTarget, i + 1);

                        // Set up controller access
                        using (ControllerApi controllerApi = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword)))
                        {
                            #region Controller

                            loggerConsole.Info("Controller Version");

                            string controllerVersionXML = controllerApi.GetControllerVersion();
                            if (controllerVersionXML != String.Empty) FileIOHelper.SaveFileToPath(controllerVersionXML, FilePathMap.ControllerVersionDataFilePath(jobTarget));

                            if (controllerVersionXML != String.Empty)
                            {
                                XmlDocument configXml = new XmlDocument();
                                configXml.LoadXml(controllerVersionXML);

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

                                    jobTarget.ControllerVersion = controllerVersion;

                                    loggerConsole.Info("Controller {0} version is {1}", jobTarget.Controller, jobTarget.ControllerVersion);

                                    foreach (JobTarget jobTargetOthers in controllerGroup)
                                    {
                                        jobTargetOthers.ControllerVersion = controllerVersion;
                                    }
                                }
                                else
                                {
                                    jobTarget.ControllerVersion = "0.0.0.0";
                                }
                            }

                            #endregion

                            #region APM Applications

                            loggerConsole.Info("APM Applications");

                            string applicationsJSON = controllerApi.GetAPMApplications();
                            if (applicationsJSON != String.Empty) FileIOHelper.SaveFileToPath(applicationsJSON, FilePathMap.APMApplicationsDataFilePath(jobTarget));

                            #endregion

                            #region Applications

                            loggerConsole.Info("All Applications");

                            controllerApi.PrivateApiLogin();

                            string applicationsAllJSON = controllerApi.GetAllApplicationsAllTypes();
                            if (applicationsAllJSON != String.Empty) FileIOHelper.SaveFileToPath(applicationsAllJSON, FilePathMap.AllApplicationsDataFilePath(jobTarget));

                            #endregion

                            #region Mobile Applications

                            loggerConsole.Info("Mobile Applications");

                            string applicationsMobileJSON = controllerApi.GetMOBILEApplications();
                            if (applicationsMobileJSON != String.Empty) FileIOHelper.SaveFileToPath(applicationsMobileJSON, FilePathMap.MOBILEApplicationsDataFilePath(jobTarget));

                            #endregion
                        }

                        // Also pre-copy the template controller configuration to the Data folder
                        loggerConsole.Info("Template Configuration");
                        FileIOHelper.CopyFolder(FilePathMap.TemplateControllerConfigurationSourceFolderPath(), FilePathMap.TemplateControllerConfigurationTargetFolderPath());
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

        public override bool ShouldExecute(ProgramOptions programOptions, JobConfiguration jobConfiguration)
        {
            logger.Trace("{0} is always executed", jobConfiguration.Status);
            loggerConsole.Trace("{0} is always executed", jobConfiguration.Status);
            return true;
        }
    }
}
