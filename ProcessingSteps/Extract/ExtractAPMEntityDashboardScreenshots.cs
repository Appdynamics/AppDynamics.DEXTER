using AppDynamics.Dexter.DataObjects;
using AppDynamics.Dexter.ReportObjectMaps;
using AppDynamics.Dexter.ReportObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;
using NLog;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Text.RegularExpressions;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class ExtractAPMEntityDashboardScreenshots : JobStepBase
    {
        private static Logger loggerWebDriver = LogManager.GetLogger("AppDynamics.Dexter.WebDriver");

        // APM links
        internal const string URL_CONTROLLER_LOCAL_LOGIN = @"{0}/controller/#/localLogin=true";
        internal const string URL_APM_APPLICATION = @"{0}/controller/#/location=APP_DASHBOARD&timeRange={2}&application={1}&dashboardMode=force";
        internal const string URL_TIER = @"{0}/controller/#/location=APP_COMPONENT_MANAGER&timeRange={3}&application={1}&component={2}&dashboardMode=force";
        internal const string URL_NODE = @"{0}/controller/#/location=APP_NODE_MANAGER&timeRange={3}&application={1}&node={2}&dashboardMode=force";
        internal const string URL_BACKEND = @"{0}/controller/#/location=APP_BACKEND_DASHBOARD&timeRange={3}&application={1}&backendDashboard={2}&dashboardMode=force";
        internal const string URL_BUSINESS_TRANSACTION = @"{0}/controller/#/location=APP_BT_DETAIL&timeRange={3}&application={1}&businessTransaction={2}&dashboardMode=force";

        internal const string DEEPLINK_TIMERANGE_BETWEEN_TIMES = "Custom_Time_Range.BETWEEN_TIMES.{0}.{1}.{2}";

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Compiler", "CS0618", Justification = "Selenium driver obsolete warning is nice but I am not upgrading")]
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

                if (jobConfiguration.Target.Count(t => t.Type == APPLICATION_TYPE_APM) == 0)
                {
                    return true;
                }

                string validatedChromeDriverFolderPath = String.Empty;

                // Process each target
                for (int i = 0; i < jobConfiguration.Target.Count; i++)
                {
                    Stopwatch stopWatchTarget = new Stopwatch();
                    stopWatchTarget.Start();

                    JobTarget jobTarget = jobConfiguration.Target[i];

                    if (jobTarget.Type != null && jobTarget.Type.Length > 0 && jobTarget.Type != APPLICATION_TYPE_APM) continue;

                    if (jobTarget.UserName == "BEARER")
                    {
                        logger.Warn("{0} step for {1} does not support support BEARER token", jobConfiguration.Status, jobTarget);
                        loggerConsole.Warn("{0} step for {1} does not support support BEARER token", jobConfiguration.Status, jobTarget);
                        continue;
                    }

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

                        Version version4_5 = new Version(4, 5);
                        Version versionThisController = new Version(jobTarget.ControllerVersion);
                        int numEntitiesTotal = 0;

                        #endregion

                        #region Prepare time range

                        long fromTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobConfiguration.Input.TimeRange.From);
                        long toTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobConfiguration.Input.TimeRange.To);
                        long differenceInMinutes = (toTimeUnix - fromTimeUnix) / (60000);
                        string DEEPLINK_THIS_TIMERANGE = String.Format(DEEPLINK_TIMERANGE_BETWEEN_TIMES, toTimeUnix, fromTimeUnix, differenceInMinutes);

                        #endregion

                        #region Prepare the driver appropriate for the system

                        RemoteWebDriver chromeDriver = null;

                        ChromeOptions options = new ChromeOptions();
                        options.AcceptInsecureCertificates = true;
                        options.AddArgument("--headless");
                        options.AddArgument("--guest");
                        options.AddArgument("--disable-extensions");

                        if (validatedChromeDriverFolderPath.Length == 0)
                        {
                            // Check chrome version
                            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) == true)
                            {
                                string chromeExeFilePath = Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles(x86)"), "Google", "Chrome", "Application", "chrome.exe");
                                loggerConsole.Info("Trying to find Chrome version on Windows in {0}", chromeExeFilePath);
                                loggerWebDriver.Info("Trying to find Chrome version on Windows in {0}", chromeExeFilePath);
                                try
                                {
                                    FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(chromeExeFilePath);

                                    loggerConsole.Info("Chrome version is {0}", fvi.FileVersion);
                                    loggerWebDriver.Info("Chrome version is {0}", fvi.FileVersion);
                                }
                                catch { }
                            }

                            // Get all available versions for the driver, going from newest to oldest
                            // https://chromedriver.chromium.org/
                            string[] chromeDriverFolderPaths = Directory.GetDirectories(Path.Combine(programOptions.ProgramLocationFolderPath, "ChromeDriver"));
                            Array.Sort(chromeDriverFolderPaths);
                            Array.Reverse(chromeDriverFolderPaths);

                            foreach (string chromeDriverVersionFolderPath in chromeDriverFolderPaths)
                            {
                                string chromeDriverFolderPath = String.Empty;
                                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) == true)
                                {
                                    chromeDriverFolderPath = Path.Combine(chromeDriverVersionFolderPath, "win32");
                                    loggerWebDriver.Info("Trying Chrome Web Driver {0} on Windows", chromeDriverFolderPath);
                                }
                                // Mac/Linux: a child of %HOME% path
                                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) == true)
                                {
                                    chromeDriverFolderPath = Path.Combine(chromeDriverVersionFolderPath, "linux64");
                                    loggerWebDriver.Info("Trying Chrome Web Driver {0} on Linux", chromeDriverFolderPath);
                                }
                                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) == true)
                                {
                                    chromeDriverFolderPath = Path.Combine(chromeDriverVersionFolderPath, "mac64");
                                    loggerWebDriver.Info("Trying Chrome Web Driver {0} on OSX", chromeDriverFolderPath);
                                }

                                if (chromeDriverFolderPath.Length > 0)
                                {
                                    try
                                    {
                                        loggerConsole.Info("Trying to open Chrome Web Driver from {0}", chromeDriverVersionFolderPath);
                                        chromeDriver = new ChromeDriver(chromeDriverFolderPath, options);
                                    }
                                    catch (InvalidOperationException ex)
                                    {
                                        // This happens when the driver is for browser that is newer then what is installed
                                        //System.InvalidOperationException
                                        //  HResult = 0x80131509
                                        //  Message = session not created: This version of ChromeDriver only supports Chrome version 78(SessionNotCreated)
                                        //  Source = WebDriver
                                        //  StackTrace:
                                        //   at OpenQA.Selenium.Remote.RemoteWebDriver.UnpackAndThrowOnError(Response errorResponse)
                                        //   at OpenQA.Selenium.Remote.RemoteWebDriver.Execute(String driverCommandToExecute, Dictionary`2 parameters)
                                        //   at OpenQA.Selenium.Remote.RemoteWebDriver.StartSession(ICapabilities desiredCapabilities)
                                        //   at OpenQA.Selenium.Remote.RemoteWebDriver..ctor(ICommandExecutor commandExecutor, ICapabilities desiredCapabilities)
                                        //   at OpenQA.Selenium.Chrome.ChromeDriver..ctor(ChromeDriverService service, ChromeOptions options, TimeSpan commandTimeout)
                                        //   at AppDScreenShot.Program.Main(String[] args) in C:\appdynamics\AppDScreenShot\AppDScreenShot\Program.cs:line 22

                                        loggerConsole.Warn("Unable to create Web Driver from {0}", chromeDriverFolderPath);
                                        loggerWebDriver.Error("Unable to create Web Driver from {0}", chromeDriverFolderPath);

                                        loggerWebDriver.Error(ex);
                                    }

                                    if (chromeDriver != null)
                                    {
                                        // Success
                                        validatedChromeDriverFolderPath = chromeDriverFolderPath;
                                        loggerConsole.Info("Have Chrome Web Driver from {0}", chromeDriverFolderPath);
                                        loggerWebDriver.Info("Have Chrome Web Driver from {0}", chromeDriverFolderPath);
                                        loggerWebDriver.Trace(chromeDriver.Capabilities);
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Choose previously found path
                            try
                            {
                                loggerConsole.Info("Trying to open Chrome Web Driver from {0}", validatedChromeDriverFolderPath);
                                chromeDriver = new ChromeDriver(validatedChromeDriverFolderPath, options);
                            }
                            catch (InvalidOperationException ex)
                            {
                                // This happens when the driver is for browser that is newer then what is installed
                                //System.InvalidOperationException
                                //  HResult = 0x80131509
                                //  Message = session not created: This version of ChromeDriver only supports Chrome version 78(SessionNotCreated)
                                //  Source = WebDriver
                                //  StackTrace:
                                //   at OpenQA.Selenium.Remote.RemoteWebDriver.UnpackAndThrowOnError(Response errorResponse)
                                //   at OpenQA.Selenium.Remote.RemoteWebDriver.Execute(String driverCommandToExecute, Dictionary`2 parameters)
                                //   at OpenQA.Selenium.Remote.RemoteWebDriver.StartSession(ICapabilities desiredCapabilities)
                                //   at OpenQA.Selenium.Remote.RemoteWebDriver..ctor(ICommandExecutor commandExecutor, ICapabilities desiredCapabilities)
                                //   at OpenQA.Selenium.Chrome.ChromeDriver..ctor(ChromeDriverService service, ChromeOptions options, TimeSpan commandTimeout)
                                //   at AppDScreenShot.Program.Main(String[] args) in C:\appdynamics\AppDScreenShot\AppDScreenShot\Program.cs:line 22

                                loggerConsole.Warn("Unable to create Web Driver from {0}", validatedChromeDriverFolderPath);
                                loggerWebDriver.Error("Unable to create Web Driver from {0}", validatedChromeDriverFolderPath);

                                loggerWebDriver.Error(ex);
                            }
                        }

                        if (chromeDriver == null)
                        {
                            loggerConsole.Warn("Do not have Chrome Web Driver Initialized");

                            continue;
                        }

                        chromeDriver.Manage().Window.Size = new System.Drawing.Size(1920, 1200);
                        chromeDriver.Manage().Timeouts().ImplicitWait = new TimeSpan(0, 0, 7);

                        #endregion

                        #region Authenticate web driver just one time

                        bool isLogonSuccessful = false;

                        // Now we have a copy of Web Driver and it's running, let's authenticate it to the Controller
                        try
                        {
                            string addressURL = String.Format(URL_CONTROLLER_LOCAL_LOGIN, jobTarget.Controller);

                            loggerWebDriver.Info("Navigating to {0}", addressURL);
                            loggerConsole.Info("Logging into {0}({1}) with {2}", jobTarget.Controller, jobTarget.ControllerVersion, jobTarget.UserName);

                            // https://soandso.saas.appdynamics.com/controller/#/localLogin=true
                            chromeDriver.Url = addressURL;

                            WebDriverWait wait = new WebDriverWait(chromeDriver, new TimeSpan(0, 0, 20));

                            RemoteWebElement accountTextBox = null;
                            RemoteWebElement userNameTextBox = null;
                            RemoteWebElement userPasswordTextBox = null;
                            RemoteWebElement loginButton = null;
                            RemoteWebElement useLocalLoginHref= null;

                            try
                            {
                                accountTextBox = wait.Until<RemoteWebElement>(d => (RemoteWebElement)d.FindElement(By.Id("accountNameInput")));
                            }
                            catch { }
                            try
                            {
                                userNameTextBox = wait.Until<RemoteWebElement>(d => (RemoteWebElement)d.FindElement(By.Id("userNameInput")));
                            }
                            catch { }
                            try
                            {
                                userPasswordTextBox = wait.Until<RemoteWebElement>(d => (RemoteWebElement)d.FindElement(By.Id("passwordInput")));
                            }
                            catch { }
                            try
                            {
                                loginButton = wait.Until<RemoteWebElement>(d => (RemoteWebElement)d.FindElement(By.Id("submitInput")));
                            }
                            catch { }

                            string userName = String.Empty;
                            string accountName = String.Empty;
                            string[] userAndAccountTokens = jobTarget.UserName.Split('@');
                            if (userAndAccountTokens.Length == 2)
                            {
                                userName = userAndAccountTokens[0];
                                accountName = userAndAccountTokens[1];
                            }
                            else
                            {
                                accountName = userAndAccountTokens[userAndAccountTokens.Length - 1];
                                userName = jobTarget.UserName.Replace(String.Format("@{0}", accountName), "");
                            }
                            if (accountTextBox != null && accountTextBox.Displayed == true)
                            {
                                loggerWebDriver.Trace("Setting account={0}", accountName);
                                accountTextBox.SendKeys(accountName);
                            }

                            Thread.Sleep(1000);

                            if (userPasswordTextBox != null && userPasswordTextBox.Displayed == false)
                            {
                                loggerWebDriver.Warn("SAML must be configured, Password got hidden. Trying to click on Use Local Login");

                                // Check if Use Local Login is turned on
                                // In 4.5.13 or later the URL does not seem to take effect
                                try
                                {
                                    useLocalLoginHref = wait.Until<RemoteWebElement>(d => (RemoteWebElement)d.FindElement(By.ClassName("adsLink")));

                                    useLocalLoginHref.Click();
                                }
                                catch { }
                            }

                            if (userNameTextBox != null && userNameTextBox.Displayed == true)
                            {
                                loggerWebDriver.Trace("Setting user={0}", userName);
                                userNameTextBox.SendKeys(userName);

                                if (userPasswordTextBox != null && userPasswordTextBox.Displayed == true)
                                {
                                    loggerWebDriver.Trace("Sending password");
                                    userPasswordTextBox.SendKeys(AESEncryptionHelper.Decrypt(jobTarget.UserPassword));

                                    if (loginButton != null && loginButton.Displayed == true)
                                    {
                                        loggerWebDriver.Trace("Clicking login button");
                                        loginButton.Click();

                                        loggerWebDriver.Trace("Waiting for home page to appear");
                                        try
                                        {
                                            wait.Until(ExpectedConditions.ElementIsVisible(By.ClassName("ads-home-view-card-container")));
                                        }
                                        catch (WebDriverTimeoutException ex)
                                        { 
                                            if (ex.InnerException is OpenQA.Selenium.NoSuchElementException)
                                            {
                                                loggerWebDriver.Warn("Clicked Login button but homepage did not show up");
                                                loggerWebDriver.Warn(ex);
                                            }
                                        }

                                        string currentBrowserURL = chromeDriver.Url.ToLower();
                                        if (currentBrowserURL.Contains("locallogin"))
                                        {
                                            loggerWebDriver.Trace("Redirect after clicking Login was unsuccessful, but we are logged in");
                                            isLogonSuccessful = true;
                                        }
                                        else if (currentBrowserURL.Contains("location=ad_home_overview"))
                                        {
                                            loggerWebDriver.Trace("Redirect after clicking Login was successful");
                                            isLogonSuccessful = true;
                                        }
                                        else
                                        {
                                            isLogonSuccessful = false;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                loggerWebDriver.Error("SAML must be configured, Password got hidden. Clicking on Login must not have worked");
                            }

                        }
                        catch (Exception ex)
                        {
                            loggerWebDriver.Error(ex);
                            loggerConsole.Warn(ex);

                            chromeDriver.Quit();

                            continue;
                        }

                        if (isLogonSuccessful == false)
                        {
                            loggerWebDriver.Warn("Logging into {0} failed", jobTarget.Controller);
                            loggerConsole.Warn("Logging into {0} failed", jobTarget.Controller);

                            chromeDriver.Quit();

                            continue;
                        }

                        #endregion

                        #region Take pretty pretty screenshots

                        // Now we have a copy of Web Driver, it's running and authenticated
                        try
                        {
                            WebDriverWait wait = new WebDriverWait(chromeDriver, new TimeSpan(0, 0, 7));

                            #region Application

                            loggerConsole.Info("Taking screenshot for Application");

                            string addressURL = String.Format(URL_APM_APPLICATION, jobTarget.Controller, jobTarget.ApplicationID, DEEPLINK_THIS_TIMERANGE);

                            loggerWebDriver.Info("Navigating to {0} for Application {1}/{2}", addressURL, jobTarget.Controller, jobTarget.Application);
                            loggerConsole.Info("Application Flowmap");

                            // https://soandso.saas.appdynamics.com/controller/#/location=APP_DASHBOARD&timeRange=Custom_Time_Range.BETWEEN_TIMES.1568249100000.1568246400000.45&application=68&dashboardMode=force
                            chromeDriver.Url = addressURL;

                            // Collapse left side navigation to make more screen real estate
                            try
                            {
                                RemoteWebElement leftSideNavigationCollapseDIV = wait.Until<RemoteWebElement>(d => (RemoteWebElement)d.FindElement(By.ClassName("ads-left-nav2-hover-toggle-container")));
                                if (leftSideNavigationCollapseDIV != null && leftSideNavigationCollapseDIV.Displayed == true) leftSideNavigationCollapseDIV.Click();
                            }
                            catch
                            {
                                loggerWebDriver.Warn("Waiting for left side navigation resize timed out");
                            }

                            if (File.Exists(FilePathMap.ApplicationDashboardScreenshotDataFilePath(jobTarget)) == false)
                            {
                                // If there are any helpful flyout tips, dismiss them 
                                //try
                                //{
                                //    RemoteWebElement popoutDiv = (RemoteWebElement)chromeDriver.FindElement(By.ClassName("ads-popover-footer-container"));
                                //    if (popoutDiv != null && popoutDiv.Displayed == true)
                                //    {
                                //        RemoteWebElement okGotItA = (RemoteWebElement)popoutDiv.FindElement(By.ClassName("pull-right"));
                                //        if (okGotItA != null) okGotItA.Click();
                                //    }
                                //}
                                //catch { }

                                waitForFlowmap(chromeDriver, wait);

                                // Autofit the flowmap
                                // Commenting this out for now 
                                //RemoteWebElement autoLayoutButtonElement = null;
                                //try
                                //{
                                //    autoLayoutButtonElement = wait.Until<RemoteWebElement>(d => (RemoteWebElement)d.FindElement(By.ClassName("adsDashLayoutForceButton")));
                                //    if (autoLayoutButtonElement != null && autoLayoutButtonElement.Displayed == true) autoLayoutButtonElement.Click();
                                //}
                                //catch { }
                                //Thread.Sleep(5000);

                                loggerWebDriver.Info("Taking screenshot of {0}", addressURL);

                                Screenshot screenshot = chromeDriver.GetScreenshot();
                                FileIOHelper.CreateFolderForFile(FilePathMap.ApplicationDashboardScreenshotDataFilePath(jobTarget));
                                screenshot.SaveAsFile(FilePathMap.ApplicationDashboardScreenshotDataFilePath(jobTarget), ScreenshotImageFormat.Png);

                                numEntitiesTotal++;
                            }

                            #endregion

                            #region Tiers

                            List<AppDRESTTier> tiersList = FileIOHelper.LoadListOfObjectsFromFile<AppDRESTTier>(FilePathMap.APMTiersDataFilePath(jobTarget));
                            if (tiersList != null)
                            {
                                loggerConsole.Info("Taking Screenshots for Tiers ({0} entities)", tiersList.Count);

                                int j = 0;

                                foreach (AppDRESTTier tier in tiersList)
                                {
                                    // Filter Tier type
                                    if (jobConfiguration.Input.EntityDashboardSelectionCriteria.TierType.All != true)
                                    {
                                        PropertyInfo pi = jobConfiguration.Input.EntityDashboardSelectionCriteria.TierType.GetType().GetProperty(tier.agentType);
                                        if (pi != null)
                                        {
                                            if ((bool)pi.GetValue(jobConfiguration.Input.EntityDashboardSelectionCriteria.TierType) == false) continue;
                                        }
                                    }
                                    // Filter Tier name
                                    bool tierNameMatch = false;
                                    if (jobConfiguration.Input.EntityDashboardSelectionCriteria.Tiers.Length == 0) tierNameMatch = true;
                                    foreach (string matchCriteria in jobConfiguration.Input.EntityDashboardSelectionCriteria.Tiers)
                                    {
                                        if (matchCriteria.Length > 0)
                                        {
                                            // Try straight up string compare first
                                            if (String.Compare(tier.name, matchCriteria, true) == 0)
                                            {
                                                tierNameMatch = true;
                                                break;
                                            }

                                            // Try regex compare second
                                            Regex regexQuery = new Regex(matchCriteria, RegexOptions.IgnoreCase);
                                            Match regexMatch = regexQuery.Match(tier.name.ToString());
                                            if (regexMatch.Success == true && regexMatch.Index == 0)
                                            {
                                                tierNameMatch = true;
                                                break;
                                            }
                                        }
                                    }
                                    if (tierNameMatch == false) continue;

                                    if (File.Exists(FilePathMap.TierDashboardScreenshotDataFilePath(jobTarget, tier)) == false)
                                    {
                                        addressURL = String.Format(URL_TIER, jobTarget.Controller, jobTarget.ApplicationID, tier.id, DEEPLINK_THIS_TIMERANGE);

                                        loggerWebDriver.Info("Navigating to {0} for Tier {1}/{2}/{3}", addressURL, jobTarget.Controller, jobTarget.Application, tier.name);

                                        chromeDriver.Url = addressURL;

                                        waitForFlowmap(chromeDriver, wait);

                                        loggerWebDriver.Info("Taking screenshot of {0}", addressURL);

                                        Screenshot screenshot = chromeDriver.GetScreenshot();
                                        FileIOHelper.CreateFolderForFile(FilePathMap.TierDashboardScreenshotDataFilePath(jobTarget, tier));
                                        screenshot.SaveAsFile(FilePathMap.TierDashboardScreenshotDataFilePath(jobTarget, tier), ScreenshotImageFormat.Png);
                                    }

                                    if (j % 10 == 0)
                                    {
                                        Console.Write("[{0}].", j);
                                    }
                                    else
                                    {
                                        Console.Write(".");
                                    }
                                    j++;
                                    numEntitiesTotal++;
                                }
                                loggerConsole.Info("Completed {0} Tiers", tiersList.Count);
                            }

                            #endregion

                            #region Nodes

                            List<AppDRESTNode> nodesList = FileIOHelper.LoadListOfObjectsFromFile<AppDRESTNode>(FilePathMap.APMNodesDataFilePath(jobTarget));
                            if (nodesList != null)
                            {
                                loggerConsole.Info("Taking Screenshots for Nodes ({0} entities)", nodesList.Count);

                                int j = 0;

                                foreach (AppDRESTNode node in nodesList)
                                {
                                    // Filter Node type
                                    if (jobConfiguration.Input.EntityDashboardSelectionCriteria.NodeType.All != true)
                                    {
                                        PropertyInfo pi = jobConfiguration.Input.EntityDashboardSelectionCriteria.NodeType.GetType().GetProperty(node.agentType);
                                        if (pi != null)
                                        {
                                            if ((bool)pi.GetValue(jobConfiguration.Input.EntityDashboardSelectionCriteria.NodeType) == false) continue;
                                        }
                                    }
                                    // Filter Node name
                                    bool nodeNameMatch = false;
                                    if (jobConfiguration.Input.EntityDashboardSelectionCriteria.Nodes.Length == 0) nodeNameMatch = true;
                                    foreach (string matchCriteria in jobConfiguration.Input.EntityDashboardSelectionCriteria.Nodes)
                                    {
                                        if (matchCriteria.Length > 0)
                                        {
                                            // Try straight up string compare first
                                            if (String.Compare(node.name, matchCriteria, true) == 0)
                                            {
                                                nodeNameMatch = true;
                                                break;
                                            }

                                            // Try regex compare second
                                            Regex regexQuery = new Regex(matchCriteria, RegexOptions.IgnoreCase);
                                            Match regexMatch = regexQuery.Match(node.name.ToString());
                                            if (regexMatch.Success == true && regexMatch.Index == 0)
                                            {
                                                nodeNameMatch = true;
                                                break;
                                            }
                                        }
                                    }
                                    if (nodeNameMatch == false) continue;

                                    if (File.Exists(FilePathMap.NodeDashboardScreenshotDataFilePath(jobTarget, node)) == false)
                                    {
                                        addressURL = String.Format(URL_NODE, jobTarget.Controller, jobTarget.ApplicationID, node.id, DEEPLINK_THIS_TIMERANGE);

                                        loggerWebDriver.Info("Navigating to {0} for Node {1}/{2}/{3}/{4}", addressURL, jobTarget.Controller, jobTarget.Application, node.tierName, node.name);

                                        chromeDriver.Url = addressURL;

                                        waitForFlowmap(chromeDriver, wait);

                                        loggerWebDriver.Info("Taking screenshot of {0}", addressURL);

                                        Screenshot screenshot = chromeDriver.GetScreenshot();
                                        FileIOHelper.CreateFolderForFile(FilePathMap.NodeDashboardScreenshotDataFilePath(jobTarget, node));
                                        screenshot.SaveAsFile(FilePathMap.NodeDashboardScreenshotDataFilePath(jobTarget, node), ScreenshotImageFormat.Png);
                                    }

                                    if (j % 10 == 0)
                                    {
                                        Console.Write("[{0}].", j);
                                    }
                                    else
                                    {
                                        Console.Write(".");
                                    }
                                    j++;
                                    numEntitiesTotal++;
                                }
                                loggerConsole.Info("Completed {0} Nodes", nodesList.Count);
                            }

                            #endregion

                            #region Business Transactions

                            List<AppDRESTBusinessTransaction> businessTransactionsList = FileIOHelper.LoadListOfObjectsFromFile<AppDRESTBusinessTransaction>(FilePathMap.APMBusinessTransactionsDataFilePath(jobTarget));
                            if (businessTransactionsList != null)
                            {
                                loggerConsole.Info("Taking Screenshots for Business Transactions ({0} entities)", businessTransactionsList.Count);

                                int j = 0;

                                foreach (AppDRESTBusinessTransaction businessTransaction in businessTransactionsList)
                                {
                                    // Filter Business Transaction type
                                    if (jobConfiguration.Input.EntityDashboardSelectionCriteria.BusinessTransactionType.All != true)
                                    {
                                        PropertyInfo pi = jobConfiguration.Input.EntityDashboardSelectionCriteria.BusinessTransactionType.GetType().GetProperty(businessTransaction.entryPointType);
                                        if (pi != null)
                                        {
                                            if ((bool)pi.GetValue(jobConfiguration.Input.EntityDashboardSelectionCriteria.BusinessTransactionType) == false) continue;
                                        }
                                    }
                                    // Filter Business Transaction name
                                    bool businessTransactionNameMatch = false;
                                    if (jobConfiguration.Input.EntityDashboardSelectionCriteria.BusinessTransactions.Length == 0) businessTransactionNameMatch = true;
                                    foreach (string matchCriteria in jobConfiguration.Input.EntityDashboardSelectionCriteria.BusinessTransactions)
                                    {
                                        if (matchCriteria.Length > 0)
                                        {
                                            // Try straight up string compare first
                                            if (String.Compare(businessTransaction.name, matchCriteria, true) == 0)
                                            {
                                                businessTransactionNameMatch = true;
                                                break;
                                            }

                                            // Try regex compare second
                                            Regex regexQuery = new Regex(matchCriteria, RegexOptions.IgnoreCase);
                                            Match regexMatch = regexQuery.Match(businessTransaction.name.ToString());
                                            if (regexMatch.Success == true && regexMatch.Index == 0)
                                            {
                                                businessTransactionNameMatch = true;
                                                break;
                                            }
                                        }
                                    }
                                    if (businessTransactionNameMatch == false) continue;

                                    if (File.Exists(FilePathMap.BusinessTransactionDashboardScreenshotDataFilePath(jobTarget, businessTransaction)) == false)
                                    {
                                        addressURL = String.Format(URL_BUSINESS_TRANSACTION, jobTarget.Controller, jobTarget.ApplicationID, businessTransaction.id, DEEPLINK_THIS_TIMERANGE);

                                        loggerWebDriver.Info("Navigating to {0} for Business Transaction {1}/{2}/{3}/{4}", addressURL, jobTarget.Controller, jobTarget.Application, businessTransaction.tierName, businessTransaction.name);

                                        chromeDriver.Url = addressURL;

                                        waitForFlowmap(chromeDriver, wait);

                                        loggerWebDriver.Info("Taking screenshot of {0}", addressURL);

                                        Screenshot screenshot = chromeDriver.GetScreenshot();
                                        FileIOHelper.CreateFolderForFile(FilePathMap.BusinessTransactionDashboardScreenshotDataFilePath(jobTarget, businessTransaction));
                                        screenshot.SaveAsFile(FilePathMap.BusinessTransactionDashboardScreenshotDataFilePath(jobTarget, businessTransaction), ScreenshotImageFormat.Png);
                                    }

                                    if (j % 10 == 0)
                                    {
                                        Console.Write("[{0}].", j);
                                    }
                                    else
                                    {
                                        Console.Write(".");
                                    }
                                    j++;
                                    numEntitiesTotal++;
                                }
                                loggerConsole.Info("Completed {0} Business Transactions", businessTransactionsList.Count);
                            }

                            #endregion

                            #region Backends

                            List<AppDRESTBackend> backendsList = FileIOHelper.LoadListOfObjectsFromFile<AppDRESTBackend>(FilePathMap.APMBackendsDataFilePath(jobTarget));
                            if (backendsList != null)
                            {
                                loggerConsole.Info("Taking Screenshots for Backends ({0} entities)", backendsList.Count);

                                int j = 0;

                                foreach (AppDRESTBackend backend in backendsList)
                                {
                                    // Filter Backend type
                                    if (jobConfiguration.Input.EntityDashboardSelectionCriteria.BackendType.All != true)
                                    {
                                        PropertyInfo pi = jobConfiguration.Input.EntityDashboardSelectionCriteria.BackendType.GetType().GetProperty(backend.exitPointType);
                                        if (pi != null)
                                        {
                                            if ((bool)pi.GetValue(jobConfiguration.Input.EntityDashboardSelectionCriteria.BackendType) == false) continue;
                                        }
                                    }
                                    // Filter Backend name
                                    bool backendNameMatch = false;
                                    if (jobConfiguration.Input.EntityDashboardSelectionCriteria.Backends.Length == 0) backendNameMatch = true;
                                    foreach (string matchCriteria in jobConfiguration.Input.EntityDashboardSelectionCriteria.Backends)
                                    {
                                        if (matchCriteria.Length > 0)
                                        {
                                            // Try straight up string compare first
                                            if (String.Compare(backend.name, matchCriteria, true) == 0)
                                            {
                                                backendNameMatch = true;
                                                break;
                                            }

                                            // Try regex compare second
                                            Regex regexQuery = new Regex(matchCriteria, RegexOptions.IgnoreCase);
                                            Match regexMatch = regexQuery.Match(backend.name.ToString());
                                            if (regexMatch.Success == true && regexMatch.Index == 0)
                                            {
                                                backendNameMatch = true;
                                                break;
                                            }
                                        }
                                    }
                                    if (backendNameMatch == false) continue;

                                    if (File.Exists(FilePathMap.BackendDashboardScreenshotDataFilePath(jobTarget, backend)) == false)
                                    {
                                        addressURL = String.Format(URL_BACKEND, jobTarget.Controller, jobTarget.ApplicationID, backend.id, DEEPLINK_THIS_TIMERANGE);

                                        loggerWebDriver.Info("Navigating to {0} for Backends {1}/{2}/{3}", addressURL, jobTarget.Controller, jobTarget.Application, backend.name);

                                        chromeDriver.Url = addressURL;

                                        waitForFlowmap(chromeDriver, wait);

                                        loggerWebDriver.Info("Taking screenshot of {0}", addressURL);

                                        Screenshot screenshot = chromeDriver.GetScreenshot();
                                        FileIOHelper.CreateFolderForFile(FilePathMap.BackendDashboardScreenshotDataFilePath(jobTarget, backend));
                                        screenshot.SaveAsFile(FilePathMap.BackendDashboardScreenshotDataFilePath(jobTarget, backend), ScreenshotImageFormat.Png);
                                    }

                                    if (j % 10 == 0)
                                    {
                                        Console.Write("[{0}].", j);
                                    }
                                    else
                                    {
                                        Console.Write(".");
                                    }
                                    j++;
                                    numEntitiesTotal++;
                                }
                                loggerConsole.Info("Completed {0} Backends", backendsList.Count);
                            }

                            #endregion
                        }
                        catch (Exception ex)
                        {
                            loggerWebDriver.Error(ex);
                            loggerConsole.Warn(ex);

                            chromeDriver.Quit();

                            continue;
                        }

                        loggerWebDriver.Trace("Quitting Chrome Web Driver");
                        loggerConsole.Info("Quitting Chrome Web Driver");
                        chromeDriver.Quit();

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
            logger.Trace("Input.EntityDashboards={0}", jobConfiguration.Input.EntityDashboards);
            loggerConsole.Trace("Input.EntityDashboards={0}", jobConfiguration.Input.EntityDashboards);
            if (jobConfiguration.Input.EntityDashboards == false)
            {
                loggerConsole.Trace("Skipping export of entity dashboard/flowmap screenshots");
            }
            return (jobConfiguration.Input.EntityDashboards == true);
        }

        private bool waitForFlowmap(RemoteWebDriver chromeDriver, WebDriverWait wait)
        {
            try
            {
                loggerWebDriver.Trace("Waiting for 500ms");
                Thread.Sleep(500);

                loggerWebDriver.Trace("Looking for adsFlowMap");
                RemoteWebElement flowmapDIV = wait.Until<RemoteWebElement>(d => (RemoteWebElement)d.FindElement(By.ClassName("adsFlowMap")));
                if (flowmapDIV == null)
                {
                    loggerWebDriver.Trace("No flowmapDIV found");
                }
                else
                {
                    if (flowmapDIV.Text.Length == 0)
                    {
                        loggerWebDriver.Trace("flowmapDIV text is empty");
                    }
                    else
                    {
                        loggerWebDriver.Trace("flowmapDIV.Text={0}", flowmapDIV.Text);
                        if (flowmapDIV.Text.Contains("Loading") || flowmapDIV.Text.Contains("Creating"))
                        {
                            for (int k = 0; k < 3; k++)
                            {
                                loggerWebDriver.Trace("Waiting for loading sign to go away #{0}", k);
                                Thread.Sleep(1000 * (k + 1));
                                if (flowmapDIV.Text.Contains("Loading") == false && flowmapDIV.Text.Contains("Creating") == false)
                                {
                                    loggerWebDriver.Trace("Loading sign went away on #{0}", k);
                                    break;
                                }
                            }
                        }
                        if (flowmapDIV.Text.Length > 0)
                        {
                            loggerWebDriver.Trace("looking for svg inside flowmapDIV");
                            RemoteWebElement flowmapSVG = wait.Until<RemoteWebElement>(d => (RemoteWebElement)flowmapDIV.FindElement(By.TagName("svg")));
                            if (flowmapSVG.Displayed == false)
                            {
                                loggerWebDriver.Trace("Waiting for svg to render");
                                for (int k = 0; k < 3; k++)
                                {
                                    loggerWebDriver.Trace("Waiting for Flowmap to be displayed #{0}", k);
                                    Thread.Sleep(1000 * (k + 1));
                                    if (flowmapSVG.Displayed == true)
                                    {
                                        loggerWebDriver.Trace("Displayed flowmap on #{0}", k);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                loggerWebDriver.Trace("Couldn't find flowmap or svg");
                loggerWebDriver.Warn(ex.Message);

                try
                {
                    // Let's see if the application flowmap is too complex and if yes, click it
                    loggerWebDriver.Trace("Looking for ads-large-flowmap-info-container");
                    RemoteWebElement applicationFlowmapTooLargeDiv = (RemoteWebElement)chromeDriver.FindElement(By.ClassName("ads-large-flowmap-info-container"));
                    if (applicationFlowmapTooLargeDiv != null && applicationFlowmapTooLargeDiv.Displayed == true)
                    {
                        loggerWebDriver.Info("Flowmap too large warning displayed, clicking the button to make it expand");

                        loggerWebDriver.Trace("Looking for show big flowmap button");
                        RemoteWebElement showFlowmapButton = wait.Until<RemoteWebElement>(d => (RemoteWebElement)applicationFlowmapTooLargeDiv.FindElement(By.TagName("button")));
                        if (showFlowmapButton != null)
                        {
                            loggerWebDriver.Trace("Clicking show big flowmap button");
                            showFlowmapButton.Click();
                            loggerWebDriver.Trace("Waiting for 5000ms");
                            Thread.Sleep(5000);
                        }
                    }
                }
                catch (Exception ex1)
                {
                    loggerWebDriver.Warn("Could not get Flowmap SVG");
                    loggerWebDriver.Warn(ex1);

                    return false;
                }
            }

            return true;
        }
    }
}
