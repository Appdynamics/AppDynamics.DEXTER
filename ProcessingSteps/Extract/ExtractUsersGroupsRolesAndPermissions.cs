using AppDynamics.Dexter.ReportObjects;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class ExtractUsersGroupsRolesAndPermissions : JobStepBase
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

                    stepTimingTarget.NumEntities = 1;

                    try
                    {
                        this.DisplayJobTargetStartingStatus(jobConfiguration, jobTarget, i + 1);

                        #region Target step variables

                        // Set up controller access
                        ControllerApi controllerApi = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword));

                        controllerApi.PrivateApiLogin();

                        #endregion

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

                        JArray usersList = FileIOHelper.LoadJArrayFromFile(FilePathMap.UsersDataFilePath(jobTarget));
                        if (usersList != null)
                        {
                            loggerConsole.Info("User Details ({0} entities)", usersList.Count);

                            int j = 0;

                            foreach (JObject user in usersList)
                            {
                                string userJSON = controllerApi.GetUserExtended((long)user["id"]);
                                if (userJSON != String.Empty) FileIOHelper.SaveFileToPath(userJSON, FilePathMap.UserDataFilePath(jobTarget, user["name"].ToString(), (long)user["id"]));

                                if (j % 10 == 0)
                                {
                                    Console.Write("[{0}].", j);
                                }
                                j++;
                            }

                            loggerConsole.Info("Completed {0} Users", usersList.Count);
                        }

                        #endregion

                        #region Group Details

                        JArray groupsList = FileIOHelper.LoadJArrayFromFile(FilePathMap.GroupsDataFilePath(jobTarget));
                        if (groupsList != null)
                        {
                            loggerConsole.Info("Group Details and Members ({0} entities)", groupsList.Count);

                            int j = 0;

                            foreach (JObject group in groupsList)
                            {
                                string groupJSON = controllerApi.GetGroupExtended((long)group["id"]);
                                if (groupJSON != String.Empty) FileIOHelper.SaveFileToPath(groupJSON, FilePathMap.GroupDataFilePath(jobTarget, group["name"].ToString(), (long)group["id"]));

                                string usersInGroupJSON = controllerApi.GetUsersInGroup((long)group["id"]);
                                if (usersInGroupJSON != String.Empty) FileIOHelper.SaveFileToPath(usersInGroupJSON, FilePathMap.GroupUsersDataFilePath(jobTarget, group["name"].ToString(), (long)group["id"]));

                                if (j % 10 == 0)
                                {
                                    Console.Write("[{0}].", j);
                                }
                                j++;
                            }

                            loggerConsole.Info("Completed {0} Groups", groupsList.Count);
                        }

                        #endregion

                        #region Role Details

                        JArray rolesList = FileIOHelper.LoadJArrayFromFile(FilePathMap.RolesDataFilePath(jobTarget));
                        if (rolesList != null)
                        {
                            loggerConsole.Info("Role Details ({0} entities)", rolesList.Count);

                            int j = 0;

                            foreach (JObject role in rolesList)
                            {
                                string roleJSON = controllerApi.GetRoleExtended((long)role["id"]);
                                if (roleJSON != String.Empty) FileIOHelper.SaveFileToPath(roleJSON, FilePathMap.RoleDataFilePath(jobTarget, role["name"].ToString(), (long)role["id"]));

                                if (j % 10 == 0)
                                {
                                    Console.Write("[{0}].", j);
                                }
                                j++;
                            }

                            loggerConsole.Info("Completed {0} Roles", rolesList.Count);
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
            loggerConsole.Trace("Input.UsersGroupsRolesPermissions={0}", jobConfiguration.Input.UsersGroupsRolesPermissions);
            if (jobConfiguration.Input.UsersGroupsRolesPermissions == false)
            {
                loggerConsole.Trace("Skipping export of users, groups, roles and permissions");
            }
            return (jobConfiguration.Input.UsersGroupsRolesPermissions == true);
        }
    }
}
