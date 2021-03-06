﻿using System;
using System.Linq.Expressions;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.IE;
using OpenQA.Selenium.Chrome;
using Protractor;
// https://github.com/anthonychu/Protractor-Net-Demo/tree/master/Protractor-Net-Demo
// https://github.com/bbaia/protractor-net/tree/master/src/Protractor

namespace ProtractorTests
{
    [TestFixture]
    public class ProtractorNUnitTest
    {
        const string URL = "http://juliemr.github.io/protractor-demo/";

        private StringBuilder verificationErrors = new StringBuilder();
        private IWebDriver driver;
        private NgWebDriver ngDriver;

        [TearDown]
        public void MyTestCleanup()
        {
            try
            {
                driver.Quit();
            }
            catch (Exception)
            {
                // Ignore errors if unable to close the browser
            }
            Assert.AreEqual("", verificationErrors.ToString());
        }

        [SetUp]
        public void Setup()
        {
            driver  = new ChromeDriver();
            driver.Manage().Timeouts().SetScriptTimeout(TimeSpan.FromSeconds(10));
            ngDriver = new NgWebDriver(driver);
        }

        [Test]
        public void Basic_Homepage_ShouldHaveATitle()
        {
            ngDriver.Url = URL;
            var title = ngDriver.Title;
            Assert.AreEqual(title, "Super Calculator");
        }

        [Test]
        public void Basic_AddOneAndTwo_ShouldBeThree()
        {
            ngDriver.Url = URL;
            var first = ngDriver.FindElement(NgBy.Input("first"));
            var second = ngDriver.FindElement(NgBy.Input("second"));
            var goButton = ngDriver.FindElement(By.Id("gobutton"));

            first.SendKeys("1");
            second.SendKeys("2");
            goButton.Click();
            var latestResult = ngDriver.FindElement(NgBy.Binding("latest")).Text;

            Assert.AreEqual(latestResult, "3");
        }

    }
}
