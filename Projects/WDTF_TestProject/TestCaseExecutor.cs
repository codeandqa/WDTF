#region Please do dont delete or modify this file. If you modify then script may stop
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;
using WDGL;
using System.Configuration;
using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using OpenQA.Selenium.Support.PageObjects;
namespace WDTF
{
    class Program
    {
        static void Main(string[] args)
        {
            TestEnvInfo t = new TestEnvInfo();
            AutomationLogging.className = t.testClassName;
            AutomationLogging.currentGuid = t.guid;
            wdgl nav = new wdgl(t);
            string browser = string.Empty;
            Program p = new Program();
            List<TestCaseDefinition> input = new List<TestCaseDefinition>();

            argData argData = new argData();
            List<argData> listOfArg = new List<argData>();
            
            
            TestCaseDefinition[] testcaseList = WDTF.TestCaseList.TEST_CASES;
            if (args.Length == 0)
            {
                foreach (TestCaseDefinition item in testcaseList)
                {
                    input.Add(item);

                }
                browser = t.parentBrowser;
            }
            else
            {
                foreach (string arg in args)
                {

                   // var out = from testcaseList in 



                    foreach (TestCaseDefinition item in testcaseList)
                    {
                        string namespace_tc = item.ToString();
                        string tc = namespace_tc.Substring(namespace_tc.LastIndexOf('.') + 1);
                        if (tc == arg)
                        {
                            if (!input.Contains(item))
                            {
                                input.Add(item);
                            }
                        }
                        else
                        {
                            if (arg.ToLower() =="firefox")
                            {
                                browser = arg.ToLower();
                            }
                            else if (arg.ToLower() == "googlechrome")
                            {
                                browser = arg.ToLower();
                            }
                            else if (arg.ToLower() == "iexplore")
                            {
                                browser = arg.ToLower();
                            }
                            else
                            {
                                browser = t.parentBrowser;
                            }
                            // loggerInfo.Instance.Message("Mismatched argument:"+arg); //TODO:handle non matching argument.
                        }
                    }
                }
            }
            if (input.Count == 0)
            {
                foreach (TestCaseDefinition item in testcaseList)
                {
                    input.Add(item);
                }
            }

            foreach (TestCaseDefinition item in input)
            {
                t.parentBrowser = browser;
                AutomationLogging.className = item.ToString();
                t.testClassName = AutomationLogging.className;
                nav.SetupTest(t);
                item.ExecuteTest(t);
                nav.TeardownTest(t);
            }

            

        }
        
    }
    public interface TestCaseDefinition
    {
        void ExecuteTest(TestEnvInfo driver);
    }
    struct argData
    {
        private TestCaseDefinition t_testcaseDef;
        public TestCaseDefinition testCases
        {
            get
            {
                return t_testcaseDef;
            }
            set
            {
                t_testcaseDef = value;
            }
        }
        private string p_browser;
        public string browser
        {
            get
            {
                return p_browser;
            }
            set
            {
                p_browser = value;
            }
        }
    }
}
#endregion