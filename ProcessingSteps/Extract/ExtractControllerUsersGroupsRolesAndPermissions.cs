using AppDynamics.Dexter.ReportObjectMaps;
using AppDynamics.Dexter.ReportObjects;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class ExtractControllerUsersGroupsRolesAndPermissions : JobStepBase
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

                    stepTimingTarget.NumEntities = 5;

                    try
                    {
                        this.DisplayJobTargetStartingStatus(jobConfiguration, jobTarget, i + 1);

                        // Set up controller access
                        using (ControllerApi controllerApi = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword)))
                        {
                            controllerApi.PrivateApiLogin();

                            #region Security Provider Type

                            loggerConsole.Info("Security Configuration");

                            string securityProviderTypeJSON = controllerApi.GetSecurityProviderType();
                            if (securityProviderTypeJSON != String.Empty) FileIOHelper.SaveFileToPath(securityProviderTypeJSON, FilePathMap.SecurityProviderTypeDataFilePath(jobTarget));

                            string requireStrongPasswordsJSON = controllerApi.GetRequireStrongPasswords();
                            if (requireStrongPasswordsJSON != String.Empty) FileIOHelper.SaveFileToPath(requireStrongPasswordsJSON, FilePathMap.StrongPasswordsDataFilePath(jobTarget));

                            #endregion

                            #region Users

                            loggerConsole.Info("Users");

                            // Users
                            string usersJSON = controllerApi.GetUsersExtended();
                            if (usersJSON != String.Empty) FileIOHelper.SaveFileToPath(usersJSON, FilePathMap.UsersDataFilePath(jobTarget));

                            #endregion

                            #region Groups

                            loggerConsole.Info("Groups");

                            // Groups
                            string groupsJSON = controllerApi.GetGroupsExtended();
                            if (groupsJSON != String.Empty) FileIOHelper.SaveFileToPath(groupsJSON, FilePathMap.GroupsDataFilePath(jobTarget));

                            #endregion

                            #region Roles

                            loggerConsole.Info("Roles");

                            // Roles
                            string rolesJSON = controllerApi.GetRolesExtended();
                            if (rolesJSON != String.Empty) FileIOHelper.SaveFileToPath(rolesJSON, FilePathMap.RolesDataFilePath(jobTarget));

                            #endregion

                            #region User Details

                            JArray usersArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.UsersDataFilePath(jobTarget));
                            if (usersArray != null)
                            {
                                loggerConsole.Info("User Details ({0} entities)", usersArray.Count);

                                int j = 0;

                                foreach (JObject userObject in usersArray)
                                {
                                    string userJSON = controllerApi.GetUserExtended((long)userObject["id"]);
                                    if (userJSON != String.Empty) FileIOHelper.SaveFileToPath(userJSON, FilePathMap.UserDataFilePath(jobTarget, userObject["name"].ToString(), (long)userObject["id"]));

                                    if (j % 10 == 0)
                                    {
                                        Console.Write("[{0}].", j);
                                    }
                                    j++;
                                }

                                loggerConsole.Info("Completed {0} Users", usersArray.Count);

                                stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + usersArray.Count;
                            }

                            #endregion

                            #region Group Details

                            JArray groupsArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.GroupsDataFilePath(jobTarget));
                            if (groupsArray != null)
                            {
                                loggerConsole.Info("Group Details and Members ({0} entities)", groupsArray.Count);

                                int j = 0;

                                foreach (JObject groupObject in groupsArray)
                                {
                                    string groupJSON = controllerApi.GetGroupExtended((long)groupObject["id"]);
                                    if (groupJSON != String.Empty) FileIOHelper.SaveFileToPath(groupJSON, FilePathMap.GroupDataFilePath(jobTarget, groupObject["name"].ToString(), (long)groupObject["id"]));

                                    string usersInGroupJSON = controllerApi.GetUsersInGroup((long)groupObject["id"]);
                                    if (usersInGroupJSON != String.Empty) FileIOHelper.SaveFileToPath(usersInGroupJSON, FilePathMap.GroupUsersDataFilePath(jobTarget, groupObject["name"].ToString(), (long)groupObject["id"]));

                                    if (j % 10 == 0)
                                    {
                                        Console.Write("[{0}].", j);
                                    }
                                    j++;
                                }

                                loggerConsole.Info("Completed {0} Groups", groupsArray.Count);

                                stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + groupsArray.Count;
                            }

                            #endregion

                            #region Role Details

                            JArray rolesArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.RolesDataFilePath(jobTarget));
                            if (rolesArray != null)
                            {
                                loggerConsole.Info("Role Details ({0} entities)", rolesArray.Count);

                                int j = 0;

                                foreach (JObject roleObject in rolesArray)
                                {
                                    string roleJSON = controllerApi.GetRoleExtended((long)roleObject["id"]);
                                    if (roleJSON != String.Empty) FileIOHelper.SaveFileToPath(roleJSON, FilePathMap.RoleDataFilePath(jobTarget, roleObject["name"].ToString(), (long)roleObject["id"]));

                                    if (j % 10 == 0)
                                    {
                                        Console.Write("[{0}].", j);
                                    }
                                    j++;
                                }

                                loggerConsole.Info("Completed {0} Roles", rolesArray.Count);

                                stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + rolesArray.Count;
                            }

                            #endregion
                        }
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
            logger.Trace("Input.UsersGroupsRolesPermissions={0}", jobConfiguration.Input.UsersGroupsRolesPermissions);
            loggerConsole.Trace("Input.UsersGroupsRolesPermissions={0}", jobConfiguration.Input.UsersGroupsRolesPermissions);
            if (jobConfiguration.Input.UsersGroupsRolesPermissions == false)
            {
                loggerConsole.Trace("Skipping export of users, groups, roles and permissions");
            }
            return (jobConfiguration.Input.UsersGroupsRolesPermissions == true);
        }
    }
}
