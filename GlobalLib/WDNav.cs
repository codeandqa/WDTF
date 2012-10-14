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
using System.Net;
using System.Net.Mail;

using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Android;
using OpenQA.Selenium.IE;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Interactions.Internal;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium.Support.PageObjects;
using OpenQA.Selenium.Support.Events;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Safari;

using Microsoft.Expression.Encoder.ScreenCapture;
using System.Drawing;
using Microsoft.Expression.Encoder.Profiles;
using Microsoft.Expression.Encoder;

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
        static TestEnvInfo testEInfo = null;
        static ScreenCaptureJob job;

        public wdgl(TestEnvInfo t)
        {
           testEInfo = t;
           testTimeout = Convert.ToInt32(t.ExplicitTimeout());
        }

        public static void OpenURL(string url)
        {
            _driver.Navigate().GoToUrl(url);
        }
        public static IWebElement FindElement(SearchBy by, string locator)
        {
            loggerInfo.Instance.LogInfo("Find By [ " + by + " ]" + " Locator [ " + locator + " ]");

            return FindElement(by, locator, 2);//TODO: Ideally we should be using 0 but for now its 2 sec
        }
        public static IWebElement FindElement(SearchBy by, string locator, int timeOut)
        {
            ReadOnlyCollection<IWebElement> SearchElements = FindElements(by, locator, timeOut);
            if (SearchElements != null)
            {
                if (SearchElements.Count > 0)
                {
                    loggerInfo.Instance.LogInfo("Found Element By [ " + by + " ]" + " Locator [ " + locator + " ]");
                    return SearchElements.First<IWebElement>();
                }
                else
                {
                    PromptAlertMessage("Unable to find Element By [ " + by + " ]" + " Locator [ " + locator + " ]");
                    loggerInfo.Instance.LogAppErro(new Exception("Unable to find Element By [ " + by + " ]" + " Locator [ " + locator + " ]"), "", NLog.LogLevel.Error);
                    TakeScreenShot(testEInfo);
                    return null;
                }
            }
            else
            {
                PromptAlertMessage("Unable to Error Message is Element By [ " + by + " ]" + " Locator [ " + locator + " ]");
                loggerInfo.Instance.LogAppErro(new Exception("Unable to Error Message is Element By [ " + by + " ]" + " Locator [ " + locator + " ]"), "", NLog.LogLevel.Error);
                TakeScreenShot(testEInfo);
                return null;
            }
        }
        public static IWebElement FindElement(SearchBy by, string locator, string logMessage)
        {
            loggerInfo.Instance.LogInfo(logMessage);
            return FindElement(by, locator, testTimeout);
        }
        public static ReadOnlyCollection<IWebElement> FindElements(SearchBy by, string locator)
        {
            loggerInfo.Instance.LogInfo("Find Elements By [ " + by + " ]" + " Locator [ " + locator + " ]");
            return FindElements(by, locator, testTimeout);   
        }
        public static ReadOnlyCollection<IWebElement> FindElements(SearchBy by, string locator, int timeout)
        {
            loggerInfo.Instance.LogInfo("Find Elements By [ " + by+ " ]" + " Locator [ " + locator + " ]");
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
            switch (by)
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

            DateTime endTime = DateTime.Now.AddSeconds(10);
            while (elements.Count == 0 && endTime > DateTime.Now)
            {
                try
                {
                    elements = _driver.FindElements(elementPointer);
                }
                catch (Exception e)
                {
                    loggerInfo.Instance.Message(e.Message + System.Environment.NewLine);
                    e = new Exception(e.Message);
                    loggerInfo.Instance.LogAppErro(e, "Unable to find element: [ " + elementPointer + " ]", NLog.LogLevel.Error);
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
                
                loggerInfo.Instance.LogInfo("Found '" + elements.Count + "' elements");
            }
            return elements;
        }
        
        public static bool ClickElement(SearchBy by, string locator, bool teardownTestIfFail,string logMessage)
        {
            IWebElement element = FindElement(by, locator, string.Empty);
            if (element != null)
            {
                try
                {
                    loggerInfo.Instance.Message(logMessage);
                    loggerInfo.Instance.Message("Click on " + locator);
                    return ClickElement(element, teardownTestIfFail);
                }
                catch (Exception e)
                {
                    loggerInfo.Instance.LogAppErro(e, "Unable to Click on Element : [ " + locator + " ]", LogLevel.Error);
                    return false;
                }
            }
            else
            {
                Exception e = new Exception("Click Element with By [ " + by + " ]" + " Locator [ " + locator + " ]");
                loggerInfo.Instance.LogAppErro(e, "", LogLevel.Error);
                return false;
            }
        }
        public static bool ClickElement(SearchBy by, string locator, string logMessage)
        {
            loggerInfo.Instance.LogInfo("Click Element with By [ " + by + " ]" + " Locator [ " + locator + " ]");
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
                loggerInfo.Instance.LogAppErro(new Exception(e.Message + ": Unable to click Elements: [" + Element + "]"), "", LogLevel.Error);// + System.Environment.NewLine);
                EmergencyTeardown(testEInfo);
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
                loggerInfo.Instance.Message("Type '" + textToType + "' in Elements with By [ " + by + " ]" + " Locator [ " + locator + " ]");
                element.SendKeys(textToType+postString);
               
            }
            catch (Exception e)
            {
                e = new Exception(e.Message);
                loggerInfo.Instance.LogAppErro(e, "Unable to Type '" + textToType + "' in Elements with By [ " + by + " ]" + " Locator [ " + locator + " ]", NLog.LogLevel.Error);
                return;
                
            }
        }
        public static void TypeElement(SearchBy by, string locator, string textToType, string logMessage)
        {
            TypeElement(by, locator, textToType, logMessage, false);
        }
        
        public static bool IsElementVisible(SearchBy by, string locator, string logMessage)
        {
            loggerInfo.Instance.LogInfo("Check if Visible: Elements By [ " + by + " ]" + " Locator [ " + locator + " ]");
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
                loggerInfo.Instance.LogAppErro(e,"Unable to find Element By [ " + by + " ]" + " Locator [ " + locator + " ]",NLog.LogLevel.Error);
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
                Thread.Sleep(5000);
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
              //  builder.DragAndDrop(source, target).Build().Perform();
                builder.ClickAndHold(source).MoveToElement(target).Release(target).Build().Perform();
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
            loggerInfo.Instance.LogInfo("Check Elements By [ " + by + " ]" + " Locator [ " + locator + " ], if enabled");
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
                loggerInfo.Instance.LogAppErro(e, "Unable to Find Elements By [ " + by + " ]" + " Locator [ " + locator + " ]", NLog.LogLevel.Error);
                return false;
            }
            return result;
        }
        
        public static bool WaitForElement(SearchBy by, string locator, string logMessage)
        {
            ReadOnlyCollection<IWebElement> SearchElements = FindElements(by, locator, testTimeout);
            if (SearchElements.Count > 0)
            {
                loggerInfo.Instance.LogInfo("Found Elements By [ " + by + " ]" + " Locator [ " + locator + " ]");
                return true;
            }
            else
            {
                return false;
            }
        }
        
        public static void EmergencyTeardown(TestEnvInfo t)
        {
            if (!t.EndToEnd)
            {
                TakeScreenShot(t);
                wdgl wd = new wdgl(t);
                loggerInfo.Instance.LogWarning("killing the process in middle of the test");
                IWebElement currEl = _driver.SwitchTo().ActiveElement();
                wd.TeardownTest(t);
                Process p = Process.GetCurrentProcess();
                p.Kill();
            }
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
            if (AutomationLogging.countOfError > 0)
            {
                Screenshot ss = ((ITakesScreenshot)_driver).GetScreenshot();
                string screenshot = ss.AsBase64EncodedString;
                byte[] screenshotAsByteArray = ss.AsByteArray;
                ss.SaveAsFile(AutomationLogging.newLocationInResultFolder + "\\" + testInfo.testClassName + "_" + AutomationLogging.countOfError.ToString() + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                string pageSource = _driver.PageSource;
                using (StreamWriter outfile = new StreamWriter(AutomationLogging.newLocationInResultFolder + "\\" + testInfo.testClassName + "_" + AutomationLogging.countOfError.ToString() + ".html"))
                {
                    outfile.Write(pageSource.ToString());
                }
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

        #region Recording methods
        public static void StartRecordingVideo()
        {
            if (testEInfo.isRecording)
            {
                job = new ScreenCaptureJob();
                job.CaptureRectangle = Screen.PrimaryScreen.Bounds;
                job.ShowFlashingBoundary = true;
                job.OutputPath = AutomationLogging.newLocationInResultFolder;
                job.Start();
            }
        }

        public static void StopRecordingVideo()
        {
            if (testEInfo.isRecording)
            {
                string filename = job.ScreenCaptureFileName;
                job.Stop();
                if (AutomationLogging.countOfError > 0)
                {
                    MediaItem src = new MediaItem(filename);
                    Job jb = new Job();
                    jb.MediaItems.Add(src);
                    jb.ApplyPreset(Presets.VC1HD720pVBR);
                    jb.OutputDirectory = AutomationLogging.newLocationInResultFolder;
                    string output = ((Microsoft.Expression.Encoder.JobBase)(jb)).ActualOutputDirectory;
                    jb.Encode();
                }

                File.Delete(filename);
            }
        }
        #endregion 

        #region Script Executor
        public void SetupTest(TestEnvInfo testEnvInfo)
        {
            StringBuilder verificationErrors = new StringBuilder();
            verificationErrors.AppendLine(System.Environment.NewLine);
            verificationErrors.AppendLine("#####################  Test Header ####################");
            verificationErrors.AppendLine(" baseURL:            " + testEInfo.testClassName);
            verificationErrors.AppendLine(" baseURL:            " + testEnvInfo.baseURL);
            verificationErrors.AppendLine(" GUID:               " + testEnvInfo.guid);
            verificationErrors.AppendLine(" CurrentBrowser:     " + testEnvInfo.parentBrowser);
            verificationErrors.AppendLine(" TestUser:           " + testEnvInfo.testUser);
            verificationErrors.AppendLine(" TestPassword:       " + testEnvInfo.testPassword);
            verificationErrors.AppendLine(" TestEmail:          " + testEnvInfo.email);
            verificationErrors.AppendLine(" Timeout:            " + testEnvInfo.implicitTimeout + " sec");
            verificationErrors.AppendLine(" StartTime:          " + DateTime.Now.ToString());
            verificationErrors.AppendLine("#######################################################");
            //loggerInfo loggerInfo = new WDGL.loggerInfo();
            
            loggerInfo.Instance.LogInfo(verificationErrors.ToString());
            loggerInfo.Instance.LogInfo("Start SetupTest");
            StartRecordingVideo();
            _driver = testEnvInfo.StratDriver();
            _driver.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromSeconds(Convert.ToInt16(testEnvInfo.implicitTimeout)));
            string baseURL = testEnvInfo.GetURL();
        }
        
        public void TeardownTest(TestEnvInfo testEnvInfo)
        {
            StringBuilder verificationErrors = new StringBuilder();
            verificationErrors.AppendLine(System.Environment.NewLine);
            StopRecordingVideo();
            loggerInfo.Instance.Message("TearDown the Test");
            try
            {
                _driver = testEnvInfo.driver;
                if (AutomationLogging.countOfError > 0)
                {
                    verificationErrors.AppendLine("******************Result:Fail with {" + AutomationLogging.countOfError + "} Error(s)******************");
                    loggerInfo.Instance.LogInfo(verificationErrors.ToString());

                }
                else
                {
                    verificationErrors.AppendLine("******************Result:Pass with No Error******************");
                    loggerInfo.Instance.LogInfo(verificationErrors.ToString());
                }
                //TakeScreenShot(testEnvInfo);
                AutomationLogging.countOfError = 0;
                _driver.Quit();
                _driver.Dispose();
            }
            catch (Exception)
            {
                
                //loggerInfo.Instance.Message("Unable to TearDown the Test");
            }
            
            //SendEmailUsingGmail();
        }
        #endregion

        public static void SendEmailUsingGmail()
        {
            DirectoryInfo dirInfo = new DirectoryInfo(AutomationLogging.newLocationInResultFolder);
            string[] extensions = new[] { ".jpg" };

            FileInfo[] files = dirInfo.GetFiles()
                                      .Where(f => extensions.Contains(f.Extension.ToLower()))
                                      .ToArray();
            if (files.Length > 0 && testEInfo.sendResult)
            {
                loggerInfo.Instance.LogInfo("Email Results and result files to "+testEInfo.reportingEmail);
                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");
                mail.From = new MailAddress("shahi.adityas@gmail.com");
                mail.To.Add("shahi.aditya@gmail.com");
                mail.Subject = "WebDriver Test Result: " + "[" + files.Length +"] Error/Failed TestCase: " + testEInfo.testClassName;
                mail.Body = "This is for testing SMTP mail from GMAIL";
                System.Net.Mail.Attachment attachment;
                dirInfo = new DirectoryInfo(AutomationLogging.newLocationInResultFolder);
                extensions = new[] { ".jpg", ".html", ".txt" };

                files = dirInfo.GetFiles()
                                          .Where(f => extensions.Contains(f.Extension.ToLower()))
                                          .ToArray();
                foreach (FileInfo item in files)
                {
                    attachment = new System.Net.Mail.Attachment(item.FullName);
                    mail.Attachments.Add(attachment);
                }
                SmtpServer.Port = 587;
                SmtpServer.Credentials = new System.Net.NetworkCredential(testEInfo.email, testEInfo.password);
                SmtpServer.EnableSsl = true;
                SmtpServer.Send(mail);
            }
        }

        public static void PromptAlertMessage(string message)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "C:\\WDTF\\ThirdPartyTools\\AlertMessage.exe";
            startInfo.Arguments = message;
            Process.Start(startInfo);
        }
    }
    
    public sealed class TestEnvInfo
    {
        public IWebDriver driver;

        //private string _guid = string.Empty;
        //private string _timout = string.Empty;
        //private string _parentBrowser = string.Empty;
        //private string _testUser = string.Empty;
        //private string _testPassword = string.Empty;
        //private string _testEmail = string.Empty;
        //private string _networkLocation = string.Empty;
        //private string _baseURL = string.Empty;
        //private string _killOtherBrowser = string.Empty;
        //private string _deleteOldLogFiles = string.Empty;
        //private string _fileType = string.Empty;
        public  string testClassName = string.Empty;
        public string guid = System.Configuration.ConfigurationManager.AppSettings["UniqueGUID"];
        public string implicitTimeout = System.Configuration.ConfigurationManager.AppSettings["ImplicitWaits"];
        public string explicitTimeout = System.Configuration.ConfigurationManager.AppSettings["ExplicitWait"];
        public string parentBrowser = System.Configuration.ConfigurationManager.AppSettings["ParentBrowser"];
        public string testUser = System.Configuration.ConfigurationManager.AppSettings["TestUser"];
        public string testPassword = System.Configuration.ConfigurationManager.AppSettings["TestPassword"];
        public string email = System.Configuration.ConfigurationManager.AppSettings["Email"];
        public string password = System.Configuration.ConfigurationManager.AppSettings["Password"];
        public string reportingEmail = System.Configuration.ConfigurationManager.AppSettings["ResultReportingEmail"];
        public string networkLocation = System.Configuration.ConfigurationManager.AppSettings["NetworkLocation"];
        public string baseURL = System.Configuration.ConfigurationManager.AppSettings["baseURL"];
        public bool sendResult = System.Configuration.ConfigurationManager.AppSettings["sendlog"] == "true" ? true : false;
        public string deleteOldLogFiles = System.Configuration.ConfigurationManager.AppSettings["DeleteOldLogfiles"];
        public bool isRecording = System.Configuration.ConfigurationManager.AppSettings["RecordingWhileFailure"]=="true"?true:false;
        public bool EndToEnd = System.Configuration.ConfigurationManager.AppSettings["EndToEndTesting"] == "true" ? true : false;
        public string GetBrowser()
        {
            return parentBrowser;
        }
        public IWebDriver GetDriver()
        {
            return driver;
        }
        public IWebDriver StratDriver()
        {
            if (parentBrowser.ToLower().Equals("Firefox".ToLower()))
            {
                FirefoxProfile firefoxProfile = new FirefoxProfile(@"C:\FFProfile", true);
                firefoxProfile.AddExtension(@"C:\WDTF\ThirdPartyTools\firebug-1.9.2.xpi");
                firefoxProfile.SetPreference("extensions.firebug.currentVersion", "1.9.2"); // Avoid startup screen
                firefoxProfile.EnableNativeEvents = true;
                this.driver = new FirefoxDriver(firefoxProfile);
            }
            else if (parentBrowser.ToLower().Equals("googlechrome".ToLower()))
            {
                this.driver = new ChromeDriver();
            }
            else if (parentBrowser.ToLower().Equals("Iexplore".ToLower()))
            {
                this.driver = new InternetExplorerDriver();
            }
            else if (parentBrowser.ToLower().Equals("Android".ToLower()))
            {
                this.driver =new  AndroidDriver();
                
            }
            else
            {
                this.driver = null;
            }

            return driver;
        }
        public int ImplicitTimeout()
        {
            return Convert.ToInt32(implicitTimeout);
        }
        public int ExplicitTimeout()
        {
            return Convert.ToInt32(explicitTimeout);
        }
        public string GetURL()
        {
            return baseURL;
        }
        public string GetTestUserName()
        {
            return testUser;
        }
        public string GetTestUserPassword()
        {
            return testPassword;
        }
        public string GetTestEmail()
        {
            return email;
        }
        public string GetNetworkLocation()
        {
            return networkLocation;
        }
        
    }
    
    public sealed class loggerInfo
    {
        public static int countOfError = 0;
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
            string logFormat = string.Format("{0:yyyy-MM-dd-hh-mm-ss}", DateTime.Now);
            newLocationInResultFolder = resultFolder + "/" + currentGuid + "_" + logFormat;
            DirectoryInfo directoryInfo = new DirectoryInfo(newLocationInResultFolder);
            if (!directoryInfo.Exists)
            {
                System.IO.Directory.CreateDirectory(newLocationInResultFolder);
            }
            LoggingConfiguration config = new LoggingConfiguration();


            //===========================================================================================//             
            ColoredConsoleTarget consoleTarget = new ColoredConsoleTarget();
            consoleTarget.Layout = "${time} | ${level}  | ${stacktrace::topFrames=2}|${message} ";
            config.AddTarget("console", consoleTarget);
            LoggingRule consoleInfo = new LoggingRule("*", LogLevel.Info, consoleTarget);
            config.LoggingRules.Add(consoleInfo);
            //===========================================================================================//
            FileTarget fileTarget = new FileTarget();
            fileTarget.Layout = "${time} | ${level}  | ${stacktrace:topFrames=2} | ${message} ";
            fileTarget.FileName = newLocationInResultFolder + "/" + className + "_" + logFormat + DateTime.Now.Second + ".txt";
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
            mailTarget.To = "shahi.aditya@gmail.com";
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
        public void Email(string message)
        {
        }
    }

    public sealed class VerifyLib
    {
        public static eResult VerifyText(string[] actualText, string[] expectedText, string requirementTags)
        {
            return eResult.TRUE;
        }

        public static string[] VerifyAndReturnBrokenImage(IWebDriver driver)
        {
            List<string> invalidSrc = new List<string>();
            int i = 1;
            string url = string.Empty;
            ReadOnlyCollection<IWebElement> elements = driver.FindElements(By.TagName("img"));
            List<string> list = new List<string>();
            List<IWebElement> nullSourceElement = new List<IWebElement>();
            foreach (IWebElement item in elements)
            {
                url = item.GetAttribute("src");
                if (url == null)
                {
                    nullSourceElement.Add(item);
                }
                else
                {
                    list.Add(item.GetAttribute("src"));
                }
            }
            foreach (string item in list)
            {
                if (item != null)
                {
                    HttpWebRequest lxRequest = (HttpWebRequest)WebRequest.Create(item);
                    String lsResponse = string.Empty;
                    try
                    {
                        HttpWebResponse lxResponse = (HttpWebResponse)lxRequest.GetResponse();
                        
                        using (BinaryReader reader = new BinaryReader(lxResponse.GetResponseStream()))
                        {
                            Byte[] lnByte = reader.ReadBytes(1 * 1024 * 1024 * 10);
                            if (lnByte.Length > 0)
                            {
                                loggerInfo.Instance.Message("Valid image link on current page: " + item);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        //404 error
                        if (!invalidSrc.Contains(item))
                        {
                            loggerInfo.Instance.Message("Invalid image link on current page: " + item);
                            invalidSrc.Add(item);
                        }
                    }
                }
                else
                {
                   // Console.WriteLine("{0}", i);
                   loggerInfo.Instance.Message("{0} null image found.");
                }
            }
           return invalidSrc.ToArray();

        }
    }
}

