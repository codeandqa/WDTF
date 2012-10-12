using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using NUnit.Framework;
using System.Diagnostics;
using System.IO;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;
using System.Reflection;
using System.Windows.Forms;
using System.Drawing;

using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.IE;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Interactions.Internal;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium.Support.PageObjects;
using OpenQA.Selenium.Support.Events;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Safari;

namespace WDGL
{
    public enum SearchBy
    {
        ClassName,
        CssSelector,
        Id,
        LinkText,
        Name,
        PartialLinkText,
        TagName,
        XPath
    };
    enum outputLogType
    {
        Email,
        ColoredConsole,
        Console,
        Database,
        HTMLfile,
        Log
    }
    public enum MouseEvent
    {
        click,
        contextmenu,
        dblclick,
        mousedown,
        mousemove,
        mouseout,
        mouseover,
        mouseup,
        mousewheel
    };
    public enum eResult
    {
        TRUE,
        FALSE
    };
    
    public sealed class wdgl//:IDisposable
    {
        static int testTimeout;
        static IWebDriver _driver;
        static TestEnvInfo teInfo = null;
        public wdgl(TestEnvInfo t)
        {
           teInfo = t;
           testTimeout = Convert.ToInt32(t.timeout);
        }
  
        public static void OpenURL(string url)
        {
            _driver.Navigate().GoToUrl(url);
        }
       
        public static IWebElement FindElement(SearchBy by, string locator)
        {
            return FindElement(by, locator, 2);//TODO: Ideally we should be using 0 but for now its 2 sec
        }
        public static IWebElement FindElement(SearchBy by, string locator, int timeOut)
        {
            ReadOnlyCollection<IWebElement> SearchElements = FindElements(by, locator, timeOut);
            if (SearchElements != null)
            {
                if (SearchElements.Count > 0)
                {
                    loggerInfo.Instance.Message("Found Element with locator: " + locator);
                    return SearchElements.First<IWebElement>();
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
        public static IWebElement FindElement(SearchBy by, string locator, string logMessage)
        {
            loggerInfo.Instance.LogInfo(logMessage);
            return FindElement(by, locator, testTimeout);
        }
        public static ReadOnlyCollection<IWebElement> FindElements(SearchBy By, string locator)
        {
            return FindElements(By, locator, testTimeout);   
        }
        public static ReadOnlyCollection<IWebElement> FindElements(SearchBy By, string locator, int timeout)
        {
            loggerInfo.Instance.LogInfo("Looking for Locator: { " + locator+" }");
            List<IWebElement> list = new List<IWebElement>();
            ReadOnlyCollection<IWebElement> elements = new ReadOnlyCollection<IWebElement>(list);
            OpenQA.Selenium.By elementPointer = null;
            string baseIdentifier = string.Empty;
            string innerText = string.Empty;
            string[] splittedId = { };
            List<string> containsItemList = new List<string>();
            string[] containtTextArray = { };
            //Split the locator
            if (locator.Contains(":contains"))
            {
                splittedId = locator.Split(new char[] { ':' });
                baseIdentifier = splittedId[0];
                foreach (string item in splittedId)
                {
                    if (item.Contains("contains("))
                    {
                        containsItemList.Add(item.Substring(item.IndexOf('(') + 1, (item.IndexOf(')') - item.IndexOf('(') - 1)));
                    }
                }
                containtTextArray = containsItemList.ToArray();
            }
            else
            {
                baseIdentifier = locator;
            }
            switch (By)
            {
                case SearchBy.ClassName:
                    elementPointer = OpenQA.Selenium.By.ClassName(baseIdentifier);
                    // elements = _driver.FindElements(OpenQA.Selenium.By.ClassName(baseIdentifier));
                    break;

                case SearchBy.CssSelector:
                    elementPointer = OpenQA.Selenium.By.CssSelector(baseIdentifier);
                    break;

                case SearchBy.Id:
                    elementPointer = OpenQA.Selenium.By.Id(baseIdentifier);
                    break;

                case SearchBy.LinkText:
                    elementPointer = OpenQA.Selenium.By.LinkText(baseIdentifier);
                    break;

                case SearchBy.Name:
                    elementPointer = OpenQA.Selenium.By.Name(baseIdentifier);
                    break;

                case SearchBy.PartialLinkText:
                    elementPointer = OpenQA.Selenium.By.PartialLinkText(baseIdentifier);
                    break;

                case SearchBy.TagName:
                    elementPointer = OpenQA.Selenium.By.TagName(baseIdentifier);
                    break;

                case SearchBy.XPath:
                    elementPointer = OpenQA.Selenium.By.XPath(baseIdentifier);
                    break;
            };

            DateTime endTime = DateTime.Now.AddSeconds(120);
            while (elements.Count == 0 && endTime > DateTime.Now)
            {
                try
                {
                    elements = _driver.FindElements(elementPointer);
                }
                catch (Exception e)
                {
                    loggerInfo.Instance.Message(e.Message + System.Environment.NewLine);
                    loggerInfo.Instance.Message("Unable to find element: < "+elementPointer+" >");
                    return null;
                }
                
            }

            if (containtTextArray.Length != 0)
            {
                foreach (IWebElement element in elements)
                {
                    innerText = element.Text;
                    foreach (string contText in containtTextArray)
                    {
                        if (innerText.Contains(contText))
                        {
                            IWebElement elem = element;
                            list.Add(element);
                        }
                    }
                }

                elements = new ReadOnlyCollection<IWebElement>(list);
                loggerInfo.Instance.LogInfo("Found " + elements.Count + " elements");
            }
            return elements;
        }
        
        public static bool ClickElement(SearchBy by, string locator, bool teardownTestIfFail,string logMessage)
        {
            IWebElement element = FindElement(by, locator, string.Empty);
            try
            {
                loggerInfo.Instance.Message(logMessage);
                loggerInfo.Instance.Message("Click on " + locator);
                return ClickElement(element, teardownTestIfFail);
            }
            catch (Exception e)
            {
                loggerInfo.Instance.LogAppErro(e, "Unable to Click on Locator : " + locator, LogLevel.Error);
                return false;
            }
        }
        public static bool ClickElement(SearchBy by, string locator, string logMessage)
        {
            return ClickElement(by, locator, false, logMessage);
        }
        public static bool ClickElement(IWebElement Element, bool teardownTestIfFail)
        {
            try
            {
                Element.Click();
            }
            catch (Exception e)
            {
                loggerInfo.Instance.LogAppErro(new Exception(e.Message),"somelocation",LogLevel.Error);// + System.Environment.NewLine);
                EmergencyTeardown(teInfo);
                
            }
            return false;
        }
        
        public static void TypeElement(SearchBy by, string locator, string textToType, string logMessage, bool hitEnterAfterType)
        {
            string postString = string.Empty;
            if (logMessage != "")
            {
                loggerInfo.Instance.Message(logMessage);
            }
            if (hitEnterAfterType)
                postString = System.Environment.NewLine;
           
            IWebElement element = FindElement(by, locator, string.Empty);
            try
            {
                ClickElement(element,false);
                loggerInfo.Instance.Message("Type '" + textToType + "' in locator: { " + locator+" }");
                element.SendKeys(textToType+postString);
               
            }
            catch (Exception e)
            {
                e = new Exception(e.Message);
                loggerInfo.Instance.LogAppErro(e, "Unable to Type '" + textToType + "' in " + locator, NLog.LogLevel.Error);
                return;
                
            }
        }
        public static void TypeElement(SearchBy by, string locator, string textToType, string logMessage)
        {
            TypeElement(by, locator, textToType, logMessage, false);
        }
        
        public static bool IsElementVisible(SearchBy by, string locator, string logMessage)
        {
            if (logMessage != "")
            {
                loggerInfo.Instance.Message(logMessage);
            }
            IWebElement element = FindElement(by, locator,5);
            bool result = false;
            try
            {
                result = element.Displayed;
            }
            catch (Exception e)
            {
                
                e = new Exception(e.Message);
                loggerInfo.Instance.LogAppErro(e,"",NLog.LogLevel.Error);
                return false;
            }
            return result;
        }
        public static bool IsElementPresent(SearchBy by, string locator, string logMessage)
        {
            return IsElementVisible(by, locator, logMessage);
        }
        
        public static bool ElementMouseOver(SearchBy by, string locator, string logMessage)
        {
            return ElementMouseOver(by, locator);
        }
        public static bool ElementMouseOver(SearchBy by, string locator)
        {
            IWebElement targetElement = FindElement(by, locator,testTimeout);
            return ElementMouseOver(targetElement);
        }
        public static bool ElementMouseOver(IWebElement targetElement)
        {
            Size currentWinSize = _driver.Manage().Window.Size;
            _driver.Manage().Window.Maximize();
            OpenQA.Selenium.Interactions.Actions builder = new OpenQA.Selenium.Interactions.Actions(_driver);
            try
            {
                builder.MoveToElement(targetElement).Build().Perform();
                Thread.Sleep(2000);
                _driver.Manage().Window.Size = currentWinSize;
            }
            catch (Exception e)
            {
                loggerInfo.Instance.Message(e.Message);
                return false;
            }
            return true;
        }


        public static bool PerformDragAndDrop(SearchBy sourceBy, string sourceLocator, SearchBy targetBy, string targetLocator, string logMessage)
        {
            loggerInfo.Instance.Message(logMessage);
            return PerformDragAndDrop(sourceBy, sourceLocator, targetBy, targetLocator);
        }
        public static bool PerformDragAndDrop(SearchBy sourceBy, string sourceLocator, SearchBy targetBy, string targetLocator)
        {
            
            IWebElement source = FindElement(sourceBy, sourceLocator,10);
            IWebElement target = FindElement(targetBy, targetLocator,10);
            OpenQA.Selenium.Interactions.Actions builder = new OpenQA.Selenium.Interactions.Actions(_driver);
            try
            {
                loggerInfo.Instance.Message("Perform Drag and Drop for :  "+"Source:{" + sourceLocator + "}   Destination:{" + targetLocator + "}");
                builder.DragAndDrop(source, target).Build().Perform();
            }
            catch (Exception e)
            {
                Exception exception = new Exception(e.Message);
                loggerInfo.Instance.LogAppErro(exception, "Source:{" + sourceLocator + "}   Destination:{" + targetLocator + "}", LogLevel.Error);
                return false;
            }
            return true;
        }
        
        public static bool IsElementEnabled(SearchBy by, string locator, string logMessage)
        {
            if (logMessage != "")
            {
                loggerInfo.Instance.Message(logMessage);
            }
            IWebElement element = FindElement(by, locator, 5);
            bool result = false;
            try
            {
                result = element.Enabled;
            }
            catch (Exception e)
            {

                e = new Exception(e.Message);
                loggerInfo.Instance.LogAppErro(e, "", NLog.LogLevel.Error);
                return false;
            }
            return result;
        }
        
        public static bool WaitForElement(SearchBy by, string locator, string logMessage)
        {
            ReadOnlyCollection<IWebElement> SearchElements = FindElements(by, locator, testTimeout);
            if (SearchElements.Count > 0)
            {
                loggerInfo.Instance.Message("Found Element with locator: " + locator);
                return true;
            }
            else
            {
                return false;
            }
        }
        
        public static void EmergencyTeardown(TestEnvInfo t)
        {
            wdgl wd = new wdgl(t);
            loggerInfo.Instance.LogWarning("killing the process in middle of the test");
            IWebElement currEl = _driver.SwitchTo().ActiveElement();
            wd.TeardownTest(t);
            Process p = Process.GetCurrentProcess();
            p.Kill();
        }
        
        public static void ExecuteJavaScript(IWebElement targetElement)
        {
            loggerInfo.Instance.Message("Executing JavaScript");
            string javaScript = "var evObj = document.createEvent('MouseEvents');" +
                                "evObj.initMouseEvent(\"mouseover\",true, false, window, 0, 0, 0, 0, 0, false, false, false, false, 0, null);" +
                                "arguments[0].dispatchEvent(evObj);";
            IJavaScriptExecutor js = _driver as IJavaScriptExecutor;
            js.ExecuteScript(javaScript, targetElement);
            

        }
        
        public static void TakeScreenShot(TestEnvInfo testInfo)
        {
            Screenshot ss = ((ITakesScreenshot)_driver).GetScreenshot();
            string screenshot = ss.AsBase64EncodedString;
            byte[] screenshotAsByteArray = ss.AsByteArray;
            ss.SaveAsFile(AutomationLogging.newLocationInResultFolder+"\\"+testInfo.testClassName+"_"+AutomationLogging.countOfError.ToString()+".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
            string pageSource = _driver.PageSource;
            using (StreamWriter outfile = new StreamWriter(AutomationLogging.newLocationInResultFolder+"\\"+testInfo.testClassName+"_"+AutomationLogging.countOfError.ToString()+".html"))
            {
                outfile.Write(pageSource.ToString());
            }
        }

        #region Frames and Windows
        public static void SwitchToTop()
        {
            try
            {
                _driver.SwitchTo().DefaultContent();
            }
            catch (Exception e)
            {
                loggerInfo.Instance.LogAppErro(new Exception(e.Message), "Unable to to to Top frame", LogLevel.Error);
                throw;
            }
        }
        public static void SwitchFrame(int frameIndex)
        {
            try
            {
                _driver.SwitchTo().Frame(frameIndex);
            }
            catch (Exception e)
            {
                loggerInfo.Instance.LogAppErro(new Exception(e.Message), "Unable to switch frame", LogLevel.Error);
                throw;
            } 
        }
        public static void SwitchFrame(string frameName)
        {
            try
            {
                _driver.SwitchTo().Frame(frameName);
            }
            catch (Exception e)
            {
                loggerInfo.Instance.LogAppErro(new Exception(e.Message), "Unable to switch to  frame: "+frameName, LogLevel.Error);
                throw;
            }
        }
        public static void SwitchFrame(IWebElement webElement)
        {
            try
            {
                _driver.SwitchTo().Frame(webElement);
            }
            catch (Exception e)
            {
                loggerInfo.Instance.LogAppErro(new Exception(e.Message), "Unable to switch to  frame: " + webElement.ToString(), LogLevel.Error);
                throw;
            }
        }
        public static void SwitchWindow(string windowName)
        {
            try
            {
                _driver.SwitchTo().Window(windowName);
            }
            catch (Exception e)
            {
                loggerInfo.Instance.LogAppErro(new Exception(e.Message), "Unable to switch to  Window: "+windowName, LogLevel.Error);
                throw;
            }
        }
        public static void AlertAccept()
        {
            try
            {
                _driver.SwitchTo().Alert().Accept();
            }
            catch (Exception e)
            {
                loggerInfo.Instance.LogAppErro(new Exception(e.Message), "Unable to accept in alert", LogLevel.Error);
                throw;
            }
        }
        public static void AlertDismiss()
        {
            try
            {
                _driver.SwitchTo().Alert().Dismiss();
            }
            catch (Exception e)
            {
                loggerInfo.Instance.LogAppErro(new Exception(e.Message), "Unable to accept in alert", LogLevel.Error);
                throw;
            }
        }
        #endregion





        #region Script Executor
        public void SetupTest(TestEnvInfo testEnvInfo)
        {
            _driver = testEnvInfo.StratDriver();
            _driver.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromSeconds(Convert.ToInt16(testEnvInfo.timeout)));
            string baseURL = testEnvInfo.GetURL();
            StringBuilder verificationErrors = new StringBuilder();
            AutomationLogging.className = testEnvInfo.testClassName;
            AutomationLogging.currentGuid = testEnvInfo.guid;
            verificationErrors.AppendLine(System.Environment.NewLine+
                                          ":::::    GUID: "+ testEnvInfo.guid);
            verificationErrors.AppendLine(":::::    Timeout: "+testEnvInfo.timeout+" sec");
            verificationErrors.AppendLine(":::::    ParentBrowser: " + testEnvInfo.parentBrowser);
            verificationErrors.AppendLine(":::::    TestUser: " + testEnvInfo.testUser);
            verificationErrors.AppendLine(":::::    TestPassword: " + testEnvInfo.testPassword);
            verificationErrors.AppendLine(":::::    TestEmail: " + testEnvInfo.testEmail);
            verificationErrors.AppendLine(":::::    baseURL: " + testEnvInfo.baseURL);
            verificationErrors.AppendLine(":::::    DeleteOldLogfiles: " + testEnvInfo.deleteOldLogFiles);
            loggerInfo.Instance.LogInfo(verificationErrors.ToString());
            
        }
        
        public void TeardownTest(TestEnvInfo testEnvInfo)
        {
            loggerInfo.Instance.Message("TearDown the Test");
            try
            {
                _driver = testEnvInfo.driver;
                TakeScreenShot(testEnvInfo);
                _driver.Quit();
                _driver.Dispose();
            }
            catch (Exception)
            {
                //loggerInfo.Instance.Message("Unable to TearDown the Test");
            }
        }
        #endregion

        //public void Dispose()
        //{
        //    GC.SuppressFinalize(this);
        //}
        
    }
    
    public sealed class TestEnvInfo
    {
        public IWebDriver driver;
        private string _guid = string.Empty;
        private string _timout = string.Empty;
        private string _parentBrowser = string.Empty;
        private string _testUser = string.Empty;
        private string _testPassword = string.Empty;
        private string _testEmail = string.Empty;
        private string _networkLocation = string.Empty;
        private string _baseURL = string.Empty;
        private string _killOtherBrowser = string.Empty;
        private string _deleteOldLogFiles = string.Empty;
        private string _fileType = string.Empty;
        public  string testClassName = string.Empty;
        public string guid = System.Configuration.ConfigurationManager.AppSettings["UniqueGUID"];
        public string timeout = System.Configuration.ConfigurationManager.AppSettings["ImplicitWaits"];
        public string parentBrowser = System.Configuration.ConfigurationManager.AppSettings["ParentBrowser"];
        public string testUser = System.Configuration.ConfigurationManager.AppSettings["TestUser"];
        public string testPassword = System.Configuration.ConfigurationManager.AppSettings["TestPassword"];
        public string testEmail = System.Configuration.ConfigurationManager.AppSettings["TestEmail"];
        public string networkLocation = System.Configuration.ConfigurationManager.AppSettings["NetworkLocation"];
        public string baseURL = System.Configuration.ConfigurationManager.AppSettings["baseURL"];
        public string killOtherBrowser = System.Configuration.ConfigurationManager.AppSettings["KillOtherBrowser"];
        public string deleteOldLogFiles = System.Configuration.ConfigurationManager.AppSettings["DeleteOldLogfiles"];
        public string fileType = System.Configuration.ConfigurationManager.AppSettings["filetype"];
        public string GetBrowser()
        {
            _parentBrowser = parentBrowser;
           return _parentBrowser;
        }
        public IWebDriver GetDriver()
        {
            return driver;
        }
        public IWebDriver StratDriver()
        {
            if (parentBrowser.ToLower().Equals("Firefox".ToLower()))
            {
                FirefoxProfile firefoxProfile = new FirefoxProfile();
                firefoxProfile.AddExtension(@"C:\WDTF\ThirdPartyTools\firebug-1.9.2.xpi");
                firefoxProfile.SetPreference("extensions.firebug.currentVersion", "1.9.2"); // Avoid startup screen
                this.driver = new FirefoxDriver(firefoxProfile);
            }
            else if (parentBrowser.ToLower().Equals("chrome".ToLower()))
            {
                this.driver = new ChromeDriver();
            }
            else if (parentBrowser.ToLower().Equals("Iexplore".ToLower()))
            {
                this.driver = new InternetExplorerDriver();
            }
            else
            {
                this.driver = null;
            }

            return driver;
        }
        public int Timeout()
        {
            return Convert.ToInt32(timeout);
        }
        public string GetURL()
        {
            _baseURL = baseURL;
            return _baseURL;
        }
        public string GetTestUserName()
        {
            _testUser = testUser;
            return _testUser;
        }
        public string GetTestUserPassword()
        {
            _testPassword = testPassword;
            return _testPassword;
        }
        public string GetTestEmail()
        {
            _testEmail = testEmail;
            return _testUser;
        }
        public string GetNetworkLocation()
        {
            _networkLocation = networkLocation;
            return _networkLocation;
        }

    }
    
    public sealed class loggerInfo
    {
        private static AutomationLogging _logger;
        public static AutomationLogging Instance
        {
            get
            {
                if (_logger == null)
                {
                    _logger = new AutomationLogging();
                }
                return _logger;
            }
        }
    }
    public class AutomationLogging
    {
        public static string currentGuid = string.Empty;
        public static string logfileName = string.Empty;
        public static string resultdirectory = string.Empty;
        public static Logger logger = LogManager.GetCurrentClassLogger();
        public static string _logFolder;
        public static string className = string.Empty;
        public static StringBuilder stringbuilder = null;
        public static List<string> list = new List<string>();
        public static int countOfError = 0;
        public static string newLocationInResultFolder = string.Empty;
        public AutomationLogging()
        {

            string resultFolder = @"C:/WDTF/TestResult";
            string asm = Assembly.GetCallingAssembly().FullName;
            string logFormat = string.Format("{0:yyMMddhhmmss}", DateTime.Now);
            newLocationInResultFolder = resultFolder + "/" + currentGuid + "_" + logFormat;
            DirectoryInfo directoryInfo = new DirectoryInfo(newLocationInResultFolder);
            if (!directoryInfo.Exists)
            {
                System.IO.Directory.CreateDirectory(newLocationInResultFolder);
            }
            //FileInfo[] fileInformation = directoryInfo.GetFiles();
            //foreach (FileInfo item in fileInformation)
            //{
            //    item.Delete();                 
            //}
            LoggingConfiguration config = new LoggingConfiguration();
            //{TestName}_TIME.htm
            // string logFormat = string.Format("{0:yyMMddhhmmss}", DateTime.Now);


            //===========================================================================================//             
            ColoredConsoleTarget consoleTarget = new ColoredConsoleTarget();
            consoleTarget.Layout = "${time} | ${level}  | ${stacktrace::topFrames=2}|${message} ";
            config.AddTarget("console", consoleTarget);
            LoggingRule consoleInfo = new LoggingRule("*", LogLevel.Info, consoleTarget);
            config.LoggingRules.Add(consoleInfo);
            //===========================================================================================//
            FileTarget fileTarget = new FileTarget();
            fileTarget.Layout = "${time} | ${level}  | ${stacktrace:topFrames=2} | ${message} ";
            fileTarget.FileName = newLocationInResultFolder + "/" + className + "_" + logFormat + ".txt";
            config.AddTarget("file", fileTarget);
            LoggingRule fileInfo = new LoggingRule("*", LogLevel.Info, fileTarget);
            config.LoggingRules.Add(fileInfo);
            //===========================================================================================//
            TraceTarget traceTarget = new TraceTarget();
            traceTarget.Layout = "${time} | ${level}  | ${stacktrace:topFrames=2} | ${message} ";

            //===========================================================================================//
            MailTarget mailTarget = new MailTarget();
            mailTarget.Name = "gmail";
            mailTarget.SmtpServer = "smtp.gmail.com";
            mailTarget.SmtpPort = 465;
            mailTarget.SmtpAuthentication = SmtpAuthenticationMode.Basic;
            mailTarget.SmtpUserName = "donethedeal@gmail.com";
            mailTarget.SmtpPassword = "passwd@123";
            mailTarget.EnableSsl = true;
            mailTarget.From = "donethedeal@gmail.com";
            mailTarget.To = "shahi.aditya@example.com";
            mailTarget.CC = "";
            LoggingRule mailInfo = new LoggingRule("*", LogLevel.Info, mailTarget);
            config.LoggingRules.Add(mailInfo);

            //===========================================================================================//
            DatabaseTarget dbTarget = new DatabaseTarget();
            //===========================================================================================//

            // Step 4. Define rules
            LogManager.Configuration = config;

        }
        public void LogDebug(string Message)
        {
            logger.Debug(Message);
        }
        public void LogInfo(string Message)
        {
            logger.Info(Message);
        }
        public void LogWarning(string Message)
        {
            logger.Warn(Message);
        }
        public void LogAppErro(Exception e, string appLocation, LogLevel logLevel)
        {
            logger.Error("Error Message is: {0}", e.Message);
            countOfError += 1;
            
        }
        public void Message(string message)
        {
            logger.Info(message);
        }
    }

    public sealed class VerifyLib
    {
        public static eResult VerifyText(string[] actualText, string[] expectedText, string requirementTags)
        {
            return eResult.TRUE;
        }
    }
    public sealed class SealedClass
    {
        public void PressXY()
        {
            Console.WriteLine("13");
        }
    }
    interface somthing
    {
        void Press();
    }
}
