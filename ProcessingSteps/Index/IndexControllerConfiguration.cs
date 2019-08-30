using AppDynamics.Dexter.ReportObjectMaps;
using AppDynamics.Dexter.ReportObjects;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class IndexControllerConfiguration : JobStepIndexBase
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

                        #region Controller Settings

                        loggerConsole.Info("Controller Settings");

                        List<ControllerSetting> controllerSettingsList = new List<ControllerSetting>();
                        JArray controllerSettingsArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.ControllerSettingsDataFilePath(jobTarget));
                        if (controllerSettingsArray != null)
                        {
                            foreach (JObject controllerSettingObject in controllerSettingsArray)
                            {
                                ControllerSetting controllerSetting = new ControllerSetting();

                                controllerSetting.Controller = jobTarget.Controller;
                                controllerSetting.ControllerLink = String.Format(DEEPLINK_CONTROLLER, controllerSetting.Controller, DEEPLINK_TIMERANGE_LAST_15_MINUTES);
                                controllerSetting.Name = getStringValueFromJToken(controllerSettingObject, "name");
                                controllerSetting.Description = getStringValueFromJToken(controllerSettingObject, "description");
                                controllerSetting.Value = getStringValueFromJToken(controllerSettingObject, "value");
                                controllerSetting.Updateable = getBoolValueFromJToken(controllerSettingObject, "updateable");
                                controllerSetting.Scope = getStringValueFromJToken(controllerSettingObject, "scope");

                                controllerSettingsList.Add(controllerSetting);
                            }
                        }

                        controllerSettingsList = controllerSettingsList.OrderBy(c => c.Name).ToList();
                        FileIOHelper.WriteListToCSVFile(controllerSettingsList, new ControllerSettingReportMap(), FilePathMap.ControllerSettingsIndexFilePath(jobTarget));

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + controllerSettingsList.Count;

                        #endregion

                        #region HTTP Templates

                        loggerConsole.Info("HTTP Templates");

                        List<HTTPAlertTemplate> httpTemplatesList = new List<HTTPAlertTemplate>();
                        JArray httpTemplatesArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.HTTPTemplatesDataFilePath(jobTarget));
                        JArray httpTemplatesDetailArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.HTTPTemplatesDetailDataFilePath(jobTarget));
                        if (httpTemplatesArray != null)
                        {
                            foreach (JObject httpTemplateObject in httpTemplatesArray)
                            {
                                HTTPAlertTemplate httpAlertTemplate = new HTTPAlertTemplate();

                                httpAlertTemplate.Controller = jobTarget.Controller;

                                httpAlertTemplate.Name = getStringValueFromJToken(httpTemplateObject, "name");

                                httpAlertTemplate.Method = getStringValueFromJToken(httpTemplateObject, "method");
                                httpAlertTemplate.Scheme = getStringValueFromJToken(httpTemplateObject, "scheme");
                                httpAlertTemplate.Host = getStringValueFromJToken(httpTemplateObject, "host");
                                httpAlertTemplate.Port = getIntValueFromJToken(httpTemplateObject, "port");
                                httpAlertTemplate.Path = getStringValueFromJToken(httpTemplateObject, "path");
                                httpAlertTemplate.Query = getStringValueFromJToken(httpTemplateObject, "query");

                                httpAlertTemplate.AuthType = getStringValueFromJToken(httpTemplateObject, "authType");
                                httpAlertTemplate.AuthUsername = getStringValueFromJToken(httpTemplateObject, "authUsername");
                                httpAlertTemplate.AuthPassword = getStringValueFromJToken(httpTemplateObject, "authPassword");

                                httpAlertTemplate.Headers = getStringValueOfObjectFromJToken(httpTemplateObject, "headers", true);
                                if (isTokenPropertyNull(httpTemplateObject, "payloadTemplate") == false)
                                {
                                    httpAlertTemplate.ContentType = getStringValueFromJToken(httpTemplateObject["payloadTemplate"], "httpRequestActionMediaType");
                                    httpAlertTemplate.FormData = getStringValueOfObjectFromJToken(httpTemplateObject["payloadTemplate"], "formDataPairs", true);
                                    httpAlertTemplate.Payload = getStringValueFromJToken(httpTemplateObject["payloadTemplate"], "payload");
                                }

                                httpAlertTemplate.ConnectTimeout = getLongValueFromJToken(httpTemplateObject, "connectTimeoutInMillis");
                                httpAlertTemplate.SocketTimeout = getLongValueFromJToken(httpTemplateObject, "socketTimeoutInMillis");

                                httpAlertTemplate.ResponseAny = getStringValueOfObjectFromJToken(httpTemplateObject, "responseMatchCriteriaAnyTemplate");
                                httpAlertTemplate.ResponseNone = getStringValueOfObjectFromJToken(httpTemplateObject, "responseMatchCriteriaNoneTemplate");

                                if (httpTemplatesDetailArray != null)
                                {
                                    JToken httpAlertTemplateToken = httpTemplatesDetailArray.Where(t => t["name"].ToString() == httpAlertTemplate.Name).FirstOrDefault();
                                    if (httpAlertTemplateToken != null)
                                    {
                                        httpAlertTemplate.TemplateID = getLongValueFromJToken(httpAlertTemplateToken, "id");
                                    }
                                }

                                httpTemplatesList.Add(httpAlertTemplate);
                            }
                        }

                        httpTemplatesList = httpTemplatesList.OrderBy(c => c.Name).ToList();
                        FileIOHelper.WriteListToCSVFile(httpTemplatesList, new HTTPAlertTemplateReportMap(), FilePathMap.HTTPTemplatesIndexFilePath(jobTarget));

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + httpTemplatesList.Count;

                        #endregion

                        #region Email Templates

                        loggerConsole.Info("Email Templates");

                        List<EmailAlertTemplate> emailTemplatesList = new List<EmailAlertTemplate>();
                        JArray emailTemplatesArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.EmailTemplatesDataFilePath(jobTarget));
                        JArray emailTemplatesDetailArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.EmailTemplatesDetailDataFilePath(jobTarget));
                        if (emailTemplatesArray != null)
                        {
                            foreach (JObject emailTemplateObject in emailTemplatesArray)
                            {
                                EmailAlertTemplate emailAlertTemplate = new EmailAlertTemplate();

                                emailAlertTemplate.Controller = jobTarget.Controller;

                                emailAlertTemplate.Name = getStringValueFromJToken(emailTemplateObject, "name");

                                emailAlertTemplate.OneEmailPerEvent = getBoolValueFromJToken(emailTemplateObject, "oneEmailPerEvent");
                                emailAlertTemplate.EventLimit = getLongValueFromJToken(emailTemplateObject, "eventClampLimit");

                                try
                                {
                                    string[] emails = emailTemplateObject["toRecipients"].Select(s => getStringValueFromJToken(s, "value")).ToArray();
                                    emailAlertTemplate.To = String.Join(";", emails);
                                }
                                catch { }
                                try
                                {
                                    string[] emails = emailTemplateObject["ccRecipients"].Select(s => getStringValueFromJToken(s, "value")).ToArray();
                                    emailAlertTemplate.CC = String.Join(";", emails);
                                }
                                catch { }
                                try
                                {
                                    string[] emails = emailTemplateObject["bccRecipients"].Select(s => getStringValueFromJToken(s, "value")).ToArray();
                                    emailAlertTemplate.BCC = String.Join(";", emails);
                                }
                                catch { }

                                try
                                {
                                    string[] emails = emailTemplateObject["testToRecipients"].Select(s => getStringValueFromJToken(s, "value")).ToArray();
                                    emailAlertTemplate.TestTo = String.Join(";", emails);
                                }
                                catch { }
                                try
                                {
                                    string[] emails = emailTemplateObject["testCcRecipients"].Select(s => getStringValueFromJToken(s, "value")).ToArray();
                                    emailAlertTemplate.TestCC = String.Join(";", emails);
                                }
                                catch { }
                                try
                                {
                                    string[] emails = emailTemplateObject["testBccRecipients"].Select(s => getStringValueFromJToken(s, "value")).ToArray();
                                    emailAlertTemplate.TestBCC = String.Join(";", emails);
                                }
                                catch { }
                                emailAlertTemplate.TestLogLevel = getStringValueFromJToken(emailTemplateObject, "testLogLevel");

                                emailAlertTemplate.Headers = getStringValueOfObjectFromJToken(emailTemplateObject, "headers", true);
                                emailAlertTemplate.Subject = getStringValueFromJToken(emailTemplateObject, "subject");
                                emailAlertTemplate.TextBody = getStringValueFromJToken(emailTemplateObject, "textBody");
                                emailAlertTemplate.HTMLBody = getStringValueFromJToken(emailTemplateObject, "htmlBody");
                                emailAlertTemplate.IncludeHTMLBody = getBoolValueFromJToken(emailTemplateObject, "includeHtmlBody");

                                emailAlertTemplate.Properties = getStringValueOfObjectFromJToken(emailTemplateObject, "defaultCustomProperties");
                                emailAlertTemplate.TestProperties = getStringValueOfObjectFromJToken(emailTemplateObject, "testPropertiesPairs");

                                emailAlertTemplate.EventTypes = getStringValueOfObjectFromJToken(emailTemplateObject, "eventTypeCountPairs");

                                if (emailTemplatesDetailArray != null)
                                {
                                    JToken emailTemplateDetailToken = emailTemplatesDetailArray.Where(t => t["name"].ToString() == emailAlertTemplate.Name).FirstOrDefault();
                                    if (emailTemplateDetailToken != null)
                                    {
                                        emailAlertTemplate.TemplateID = getLongValueFromJToken(emailTemplateDetailToken, "id");

                                    }
                                }

                                emailTemplatesList.Add(emailAlertTemplate);
                            }
                        }

                        emailTemplatesList = emailTemplatesList.OrderBy(c => c.Name).ToList();
                        FileIOHelper.WriteListToCSVFile(emailTemplatesList, new EmailAlertTemplateReportMap(), FilePathMap.EmailTemplatesIndexFilePath(jobTarget));

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + emailTemplatesList.Count;

                        #endregion

                        #region Combine All for Report CSV

                        // If it is the first one, clear out the combined folder
                        if (reportFolderCleaned == false)
                        {
                            FileIOHelper.DeleteFolder(FilePathMap.ControllerSettingsReportFolderPath());
                            Thread.Sleep(1000);
                            FileIOHelper.CreateFolder(FilePathMap.ControllerSettingsReportFolderPath());
                            reportFolderCleaned = true;
                        }

                        // Append all the individual report files into one
                        if (File.Exists(FilePathMap.ControllerSettingsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.ControllerSettingsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.ControllerSettingsReportFilePath(), FilePathMap.ControllerSettingsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.HTTPTemplatesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.HTTPTemplatesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.HTTPTemplatesReportFilePath(), FilePathMap.HTTPTemplatesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.EmailTemplatesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.EmailTemplatesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.EmailTemplatesReportFilePath(), FilePathMap.EmailTemplatesIndexFilePath(jobTarget));
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
    }
}
