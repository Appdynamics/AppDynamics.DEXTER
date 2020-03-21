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
    public class IndexControllerUsersGroupsRolesAndPermissions : JobStepIndexBase
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

                        #region Users

                        JArray usersArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.UsersDataFilePath(jobTarget));

                        List<RBACUser> usersList = null;

                        if (usersArray != null && usersArray.Count > 0)
                        {
                            loggerConsole.Info("Index List of Users ({0} entities)", usersArray.Count);

                            usersList = new List<RBACUser>(usersArray.Count);

                            foreach (JToken userToken in usersArray)
                            {
                                RBACUser user = new RBACUser();
                                user.Controller = jobTarget.Controller;

                                user.UserName = getStringValueFromJToken(userToken, "name");
                                user.DisplayName = getStringValueFromJToken(userToken, "displayName");
                                user.Email = getStringValueFromJToken(userToken, "email");
                                user.SecurityProvider = getStringValueFromJToken(userToken, "securityProviderType");

                                user.UserID = getLongValueFromJToken(userToken, "id");

                                user.CreatedBy = getStringValueFromJToken(userToken, "createdBy");
                                user.CreatedOnUtc = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(userToken, "createdOn"));
                                try { user.CreatedOn = user.CreatedOnUtc.ToLocalTime(); } catch { }
                                user.UpdatedBy = getStringValueFromJToken(userToken, "modifiedBy");
                                user.UpdatedOnUtc = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(userToken, "modifiedOn"));
                                try { user.UpdatedOn = user.UpdatedOnUtc.ToLocalTime(); } catch { }

                                usersList.Add(user);
                            }

                            // Sort them
                            usersList = usersList.OrderBy(o => o.SecurityProvider).ThenBy(o => o.UserName).ToList();
                            FileIOHelper.WriteListToCSVFile(usersList, new RBACUserReportMap(), FilePathMap.UsersIndexFilePath(jobTarget));

                            stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + usersList.Count;
                        }

                        #endregion

                        #region Groups

                        JArray groupsArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.GroupsDataFilePath(jobTarget));

                        List<RBACGroup> groupsList = null;

                        if (groupsArray != null && groupsArray.Count > 0)
                        {
                            loggerConsole.Info("Index List of Groups ({0} entities)", groupsArray.Count);

                            groupsList = new List<RBACGroup>(groupsArray.Count);

                            foreach (JToken groupToken in groupsArray)
                            {
                                RBACGroup group = new RBACGroup();
                                group.Controller = jobTarget.Controller;

                                group.GroupName = getStringValueFromJToken(groupToken, "name");
                                group.Description = getStringValueFromJToken(groupToken, "description");
                                group.SecurityProvider = getStringValueFromJToken(groupToken, "securityProviderType");

                                group.GroupID = getLongValueFromJToken(groupToken, "id");

                                group.CreatedBy = getStringValueFromJToken(groupToken, "createdBy");
                                group.CreatedOnUtc = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(groupToken, "createdOn"));
                                try { group.CreatedOn = group.CreatedOnUtc.ToLocalTime(); } catch { }
                                group.UpdatedBy = getStringValueFromJToken(groupToken, "modifiedBy");
                                group.UpdatedOnUtc = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(groupToken, "modifiedOn"));
                                try { group.UpdatedOn = group.UpdatedOnUtc.ToLocalTime(); } catch { }

                                groupsList.Add(group);
                            }

                            // Sort them
                            groupsList = groupsList.OrderBy(o => o.SecurityProvider).ThenBy(o => o.GroupName).ToList();
                            FileIOHelper.WriteListToCSVFile(groupsList, new RBACGroupReportMap(), FilePathMap.GroupsIndexFilePath(jobTarget));

                            stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + groupsList.Count;
                        }

                        #endregion

                        #region Roles

                        JArray rolesArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.RolesDataFilePath(jobTarget));

                        List<RBACRole> rolesList = null;
                        List<RBACPermission> permissionsList = null;

                        List<ControllerApplication> applicationsList = FileIOHelper.ReadListFromCSVFile<ControllerApplication>(FilePathMap.ControllerApplicationsIndexFilePath(jobTarget), new ControllerApplicationReportMap());

                        if (rolesArray != null && rolesArray.Count > 0)
                        {
                            loggerConsole.Info("Index List of Roles ({0} entities)", rolesArray.Count);

                            rolesList = new List<RBACRole>(rolesArray.Count);
                            permissionsList = new List<RBACPermission>(rolesArray.Count * 32);

                            foreach (JToken roleToken in rolesArray)
                            {
                                RBACRole role = new RBACRole();
                                role.Controller = jobTarget.Controller;

                                role.RoleName = getStringValueFromJToken(roleToken, "name");
                                role.Description = getStringValueFromJToken(roleToken, "description");
                                role.ReadOnly = getBoolValueFromJToken(roleToken, "readonly");

                                role.RoleID = getLongValueFromJToken(roleToken, "id");

                                role.CreatedBy = getStringValueFromJToken(roleToken, "createdBy");
                                role.CreatedOnUtc = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(roleToken, "createdOn"));
                                try { role.CreatedOn = role.CreatedOnUtc.ToLocalTime(); } catch { }
                                role.UpdatedBy = getStringValueFromJToken(roleToken, "modifiedBy");
                                role.UpdatedOnUtc = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(roleToken, "modifiedOn"));
                                try { role.UpdatedOn = role.UpdatedOnUtc.ToLocalTime(); } catch { }

                                // Permissions from role detail
                                JObject roleDetailObject = FileIOHelper.LoadJObjectFromFile(FilePathMap.RoleDataFilePath(jobTarget, role.RoleName, role.RoleID));
                                if (roleDetailObject != null)
                                {
                                    foreach (JToken permissionToken in roleDetailObject["permissions"])
                                    {
                                        RBACPermission permission = new RBACPermission();
                                        permission.Controller = role.Controller;

                                        permission.RoleName = role.RoleName;
                                        permission.RoleID = role.RoleID;

                                        permission.PermissionName = getStringValueFromJToken(permissionToken, "action");
                                        permission.Allowed = getBoolValueFromJToken(permissionToken, "allowed");

                                        permission.PermissionID = getLongValueFromJToken(permissionToken, "id");

                                        if (isTokenPropertyNull(permissionToken, "affectedEntity") == false)
                                        {
                                            permission.EntityType = getStringValueFromJToken(permissionToken["affectedEntity"], "entityType");
                                            permission.EntityID = getLongValueFromJToken(permissionToken["affectedEntity"], "entityId");
                                        }

                                        // Lookup the application
                                        if (permission.EntityType == "APPLICATION" && permission.EntityID != 0)
                                        {
                                            if (applicationsList != null)
                                            {
                                                ControllerApplication application = applicationsList.Where(e => e.ApplicationID == permission.EntityID).FirstOrDefault();
                                                if (application != null)
                                                {
                                                    permission.EntityName = application.ApplicationName;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            permission.EntityType = "";
                                        }

                                        role.NumPermissions++;

                                        permissionsList.Add(permission);
                                    }
                                }

                                rolesList.Add(role);
                            }

                            // Sort them
                            rolesList = rolesList.OrderBy(o => o.RoleName).ToList();
                            FileIOHelper.WriteListToCSVFile(rolesList, new RBACRoleReportMap(), FilePathMap.RolesIndexFilePath(jobTarget));

                            permissionsList = permissionsList.OrderBy(o => o.RoleName).ThenBy(o => o.EntityName).ThenBy(o => o.PermissionName).ToList();
                            FileIOHelper.WriteListToCSVFile(permissionsList, new RBACPermissionReportMap(), FilePathMap.PermissionsIndexFilePath(jobTarget));

                            stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + rolesList.Count;
                        }

                        #endregion

                        #region Groups and Users in Roles

                        List<RBACRoleMembership> roleMembershipsList = new List<RBACRoleMembership>();

                        // Users in Roles
                        if (usersList != null && rolesList != null)
                        {
                            loggerConsole.Info("Index Users in Roles ({0} entities)", usersList.Count);

                            foreach (RBACUser user in usersList)
                            {
                                JObject userDetailObject = FileIOHelper.LoadJObjectFromFile(FilePathMap.UserDataFilePath(jobTarget, user.UserName, user.UserID));
                                if (userDetailObject != null)
                                {
                                    foreach (JToken roleIDToken in userDetailObject["accountRoleIds"])
                                    {
                                        long roleID = (long)roleIDToken;

                                        RBACRole role = rolesList.Where(r => r.RoleID == roleID).FirstOrDefault();
                                        if (role != null)
                                        {
                                            RBACRoleMembership roleMembership = new RBACRoleMembership();
                                            roleMembership.Controller = user.Controller;

                                            roleMembership.RoleName = role.RoleName;
                                            roleMembership.RoleID = role.RoleID;

                                            roleMembership.EntityName = user.UserName;
                                            roleMembership.EntityID = user.UserID;
                                            roleMembership.EntityType = "User";

                                            roleMembershipsList.Add(roleMembership);
                                        }
                                    }
                                }
                            }
                        }

                        // Groups in Roles
                        if (groupsList != null && rolesList != null)
                        {
                            loggerConsole.Info("Index Groups in Roles ({0} entities)", groupsList.Count);

                            foreach (RBACGroup group in groupsList)
                            {
                                JObject groupDetailJSON = FileIOHelper.LoadJObjectFromFile(FilePathMap.GroupDataFilePath(jobTarget, group.GroupName, group.GroupID));
                                if (groupDetailJSON != null)
                                {
                                    foreach (JToken roleIDToken in groupDetailJSON["accountRoleIds"])
                                    {
                                        long roleID = (long)roleIDToken;

                                        RBACRole role = rolesList.Where(r => r.RoleID == roleID).FirstOrDefault();
                                        if (role != null)
                                        {
                                            RBACRoleMembership roleMembership = new RBACRoleMembership();
                                            roleMembership.Controller = group.Controller;

                                            roleMembership.RoleName = role.RoleName;
                                            roleMembership.RoleID = role.RoleID;

                                            roleMembership.EntityName = group.GroupName;
                                            roleMembership.EntityID = group.GroupID;
                                            roleMembership.EntityType = "Group";

                                            roleMembershipsList.Add(roleMembership);
                                        }
                                    }
                                }
                            }
                        }

                        roleMembershipsList = roleMembershipsList.OrderBy(o => o.RoleName).ThenBy(o => o.EntityType).ThenBy(o => o.EntityName).ToList();
                        FileIOHelper.WriteListToCSVFile(roleMembershipsList, new RBACRoleMembershipReportMap(), FilePathMap.RoleMembershipsIndexFilePath(jobTarget));

                        #endregion

                        #region Users in Groups

                        List<RBACGroupMembership> groupMembershipsList = new List<RBACGroupMembership>();

                        if (groupsList != null && usersList != null)
                        {
                            loggerConsole.Info("Index Users in Groups ({0} entities)", groupsList.Count);

                            foreach (RBACGroup group in groupsList)
                            {
                                JArray usersInGroupArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.GroupUsersDataFilePath(jobTarget, group.GroupName, group.GroupID));
                                if (usersInGroupArray != null)
                                {
                                    foreach (JToken userIDToken in usersInGroupArray)
                                    {
                                        long userID = (long)userIDToken;

                                        RBACUser user = usersList.Where(r => r.UserID == userID).FirstOrDefault();
                                        if (user != null)
                                        {
                                            RBACGroupMembership groupMembership = new RBACGroupMembership();
                                            groupMembership.Controller = group.Controller;

                                            groupMembership.GroupName = group.GroupName;
                                            groupMembership.GroupID = group.GroupID;

                                            groupMembership.UserName = user.UserName;
                                            groupMembership.UserID = user.UserID;

                                            groupMembershipsList.Add(groupMembership);
                                        }
                                    }
                                }
                            }
                        }

                        groupMembershipsList = groupMembershipsList.OrderBy(o => o.GroupName).ThenBy(o => o.UserName).ToList();
                        FileIOHelper.WriteListToCSVFile(groupMembershipsList, new RBACGroupMembershipReportMap(), FilePathMap.GroupMembershipsIndexFilePath(jobTarget));

                        #endregion

                        #region User Permissions

                        List<RBACUserPermission> userPermissionsList = new List<RBACUserPermission>();

                        if (roleMembershipsList != null && permissionsList != null)
                        {
                            loggerConsole.Info("Index Users Permissions ({0} entities)", roleMembershipsList.Count);

                            // Scroll through the list of Role memberships
                            foreach (RBACRoleMembership roleMembership in roleMembershipsList)
                            {
                                if (roleMembership.EntityType == "User")
                                {
                                    if (usersList != null)
                                    {
                                        // For User, enumerate permissions associated with this role
                                        RBACUser user = usersList.Where(u => u.UserID == roleMembership.EntityID).FirstOrDefault();
                                        if (user != null)
                                        {
                                            List<RBACPermission> permissionsForRoleList = permissionsList.Where(p => p.RoleID == roleMembership.RoleID).ToList();
                                            if (permissionsForRoleList != null)
                                            {
                                                foreach (RBACPermission permission in permissionsForRoleList)
                                                {
                                                    RBACUserPermission userPermission = new RBACUserPermission();
                                                    userPermission.Controller = user.Controller;

                                                    userPermission.UserName = user.UserName;
                                                    userPermission.UserSecurityProvider = user.SecurityProvider;
                                                    userPermission.UserID = user.UserID;

                                                    userPermission.RoleName = permission.RoleName;
                                                    userPermission.RoleID = permission.RoleID;

                                                    userPermission.PermissionName = permission.PermissionName;
                                                    userPermission.PermissionID = permission.PermissionID;

                                                    userPermission.Allowed = permission.Allowed;

                                                    userPermission.EntityName = permission.EntityName;
                                                    userPermission.EntityType = permission.EntityType;
                                                    userPermission.EntityID = permission.EntityID;

                                                    userPermissionsList.Add(userPermission);
                                                }
                                            }
                                        }
                                    }
                                }
                                else if (roleMembership.EntityType == "Group")
                                {
                                    RBACGroup groupDetail = null;
                                    if (groupsList != null)
                                    {
                                        groupDetail = groupsList.Where(g => g.GroupID == roleMembership.EntityID).FirstOrDefault();
                                    }

                                    if (groupMembershipsList != null)
                                    {
                                        // For Group, find all users in the group and repeat the permission output
                                        List<RBACGroupMembership> usersInGroups = groupMembershipsList.Where(g => g.GroupID == roleMembership.EntityID).ToList();
                                        if (usersInGroups != null)
                                        {
                                            foreach (RBACGroupMembership user in usersInGroups)
                                            {
                                                RBACUser userDetail = null;
                                                if (usersList != null)
                                                {
                                                    userDetail = usersList.Where(u => u.UserID == user.UserID).FirstOrDefault();
                                                }

                                                List<RBACPermission> permissionsForRoleList = permissionsList.Where(p => p.RoleID == roleMembership.RoleID).ToList();
                                                if (permissionsForRoleList != null)
                                                {
                                                    foreach (RBACPermission permission in permissionsForRoleList)
                                                    {
                                                        RBACUserPermission userPermission = new RBACUserPermission();
                                                        userPermission.Controller = user.Controller;

                                                        userPermission.UserName = user.UserName;
                                                        userPermission.UserID = user.UserID;
                                                        if (userDetail != null)
                                                        {
                                                            userPermission.UserSecurityProvider = userDetail.SecurityProvider;
                                                        }

                                                        if (groupDetail != null)
                                                        {
                                                            userPermission.GroupName = groupDetail.GroupName;
                                                            userPermission.GroupSecurityProvider = groupDetail.SecurityProvider;
                                                            userPermission.GroupID = groupDetail.GroupID;
                                                        }

                                                        userPermission.RoleName = permission.RoleName;
                                                        userPermission.RoleID = permission.RoleID;

                                                        userPermission.PermissionName = permission.PermissionName;
                                                        userPermission.PermissionID = permission.PermissionID;

                                                        userPermission.Allowed = permission.Allowed;

                                                        userPermission.EntityName = permission.EntityName;
                                                        userPermission.EntityType = permission.EntityType;
                                                        userPermission.EntityID = permission.EntityID;

                                                        userPermissionsList.Add(userPermission);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        userPermissionsList = userPermissionsList.OrderBy(o => o.UserName).ThenBy(o => o.GroupName).ThenBy(o => o.PermissionName).ToList();
                        FileIOHelper.WriteListToCSVFile(userPermissionsList, new RBACUserPermissionReportMap(), FilePathMap.UserPermissionsIndexFilePath(jobTarget));

                        #endregion

                        #region Controller Summary

                        loggerConsole.Info("Index Controller Summary");

                        RBACControllerSummary controller = new RBACControllerSummary();
                        controller.Controller = jobTarget.Controller;

                        string securityProviderType = FileIOHelper.ReadFileFromPath(FilePathMap.SecurityProviderTypeDataFilePath(jobTarget));
                        if (securityProviderType != String.Empty)
                        {
                            controller.SecurityProvider = securityProviderType.Replace("\"", "");
                        }

                        string requireStrongPasswords = FileIOHelper.ReadFileFromPath(FilePathMap.StrongPasswordsDataFilePath(jobTarget));
                        if (requireStrongPasswords != String.Empty)
                        {
                            bool parsedBool = false;
                            Boolean.TryParse(requireStrongPasswords, out parsedBool);
                            controller.IsStrongPasswords = parsedBool;
                        }

                        if (usersList != null) controller.NumUsers = usersList.Count;
                        if (groupsList != null) controller.NumGroups = groupsList.Count;
                        if (rolesList != null) controller.NumRoles = rolesList.Count;

                        List<RBACControllerSummary> controllerList = new List<RBACControllerSummary>(1);
                        controllerList.Add(controller);

                        FileIOHelper.WriteListToCSVFile(controllerList, new RBACControllerSummaryReportMap(), FilePathMap.RBACControllerSummaryIndexFilePath(jobTarget));

                        #endregion

                        #region Combine All for Report CSV

                        // If it is the first one, clear out the combined folder
                        if (reportFolderCleaned == false)
                        {
                            FileIOHelper.DeleteFolder(FilePathMap.UsersGroupsRolesPermissionsReportFolderPath());
                            Thread.Sleep(1000);
                            FileIOHelper.CreateFolder(FilePathMap.UsersGroupsRolesPermissionsReportFolderPath());
                            reportFolderCleaned = true;
                        }

                        // Append all the individual report files into one
                        if (File.Exists(FilePathMap.UsersIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.UsersIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.UsersReportFilePath(), FilePathMap.UsersIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.GroupsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.GroupsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.GroupsReportFilePath(), FilePathMap.GroupsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.RolesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.RolesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.RolesReportFilePath(), FilePathMap.RolesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.PermissionsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.PermissionsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.PermissionsReportFilePath(), FilePathMap.PermissionsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.GroupMembershipsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.GroupMembershipsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.GroupMembershipsReportFilePath(), FilePathMap.GroupMembershipsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.RoleMembershipsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.RoleMembershipsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.RoleMembershipsReportFilePath(), FilePathMap.RoleMembershipsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.UserPermissionsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.UserPermissionsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.UserPermissionsReportFilePath(), FilePathMap.UserPermissionsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.RBACControllerSummaryIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.RBACControllerSummaryIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.RBACControllerSummaryReportFilePath(), FilePathMap.RBACControllerSummaryIndexFilePath(jobTarget));
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

        public override bool ShouldExecute(ProgramOptions programOptions, JobConfiguration jobConfiguration)
        {
            logger.Trace("LicensedReports.UsersGroupsRolesPermissions={0}", programOptions.LicensedReports.UsersGroupsRolesPermissions);
            loggerConsole.Trace("LicensedReports.UsersGroupsRolesPermissions={0}", programOptions.LicensedReports.UsersGroupsRolesPermissions);
            if (programOptions.LicensedReports.UsersGroupsRolesPermissions == false)
            {
                loggerConsole.Warn("Not licensed for users, groups, roles and permissions");
                return false;
            }

            logger.Trace("Input.UsersGroupsRolesPermissions={0}", jobConfiguration.Input.UsersGroupsRolesPermissions);
            loggerConsole.Trace("Input.UsersGroupsRolesPermissions={0}", jobConfiguration.Input.UsersGroupsRolesPermissions);
            if (jobConfiguration.Input.UsersGroupsRolesPermissions == false)
            {
                loggerConsole.Trace("Skipping index of users, groups, roles and permissions");
            }
            return (jobConfiguration.Input.UsersGroupsRolesPermissions == true);
        }
    }
}
