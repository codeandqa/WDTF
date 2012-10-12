using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Chrome;
using WDGL;
using System.Configuration;
using System.Diagnostics;
using System.Reflection;
using OpenQA.Selenium.Support.PageObjects;
using System.IO;
using System.Net;
using Microsoft.Expression.Encoder.ScreenCapture;
using Microsoft.Expression.Encoder;
using Microsoft.Expression.Encoder.Live;
using System.Drawing;
using System.Windows.Forms;

namespace WDTF
{
    public class WDTF_TestCase11 : TestCaseDefinition
    {
        private IWebDriver driver;
        [TestCase]
        public void ExecuteTest(TestEnvInfo testEnv)
        {
            driver = testEnv.driver;//This is going to be there in code.
            loggerInfo.Instance.Message("Description of Test............");
            /*
            Start Coding here.
            Example:
            wdgl.ClickElement(SearchBy.CssSelector, "div[class='dCalssName']", "Comment");
            */
            loggerInfo.Instance.Message("End statement of Test..........");
        }

    }
}
