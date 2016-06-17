﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using System.Threading;

using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;

using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.IE;
using OpenQA.Selenium.PhantomJS;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;

using FluentAssertions;
using Protractor.Extensions;

namespace Protractor.Test
{

    [TestFixture]
    public class LocalFileTests
    {
        private StringBuilder _verificationErrors = new StringBuilder();
        private IWebDriver _driver;
        private NgWebDriver _ngDriver;
        private WebDriverWait _wait;
        private const int _wait_seconds = 3;
        private const long _wait_poll_milliseconds = 300;

        // private String testpage;

        [TestFixtureSetUp]
        public void SetUp()
        {
            // _driver = new FirefoxDriver();
            // System.InvalidOperationException : Access to 'file:///...' from script denied (UnexpectedJavaScriptError)
            // _driver = new ChromeDriver();
            _driver = new PhantomJSDriver();
            _driver.Manage().Timeouts().SetScriptTimeout(TimeSpan.FromSeconds(60));
            _ngDriver = new NgWebDriver(_driver);
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(_wait_seconds));
            _wait.PollingInterval = TimeSpan.FromMilliseconds(_wait_poll_milliseconds);
        }

        [TestFixtureTearDown]
        public void TearDown()
        {
            try
            {
                _driver.Quit();
            }
            catch (Exception) { } /* Ignore cleanup errors */
            Assert.IsEmpty(_verificationErrors.ToString());
        }
        private void GetPageContent(string testpage)
        {
            String base_url = new System.Uri(Path.Combine(Directory.GetCurrentDirectory(), testpage)).AbsoluteUri;
            _ngDriver.Navigate().GoToUrl(base_url);
        }

        [Test]
        public void ShouldDropDown()
        {
            GetPageContent("ng_dropdown.htm");
            string optionsCountry = "country for (country, states) in countries";
            ReadOnlyCollection<NgWebElement> ng_countries = _ngDriver.FindElements(NgBy.Options(optionsCountry));

            Assert.IsTrue(4 == ng_countries.Count);
            Assert.IsTrue(ng_countries[0].Enabled);
            string optionsState = "state for (state,city) in states";
            NgWebElement ng_state = _ngDriver.FindElement(NgBy.Options(optionsState));
            Assert.IsFalse(ng_state.Enabled);
            SelectElement countries = new SelectElement(_ngDriver.FindElement(NgBy.Model("states")).WrappedElement);
            countries.SelectByText("Australia");
            Thread.Sleep(1000);
            Assert.IsTrue(ng_state.Enabled);
            NgWebElement ng_selected_country = _ngDriver.FindElement(NgBy.SelectedOption(optionsCountry));
            // TODO:debug (works in Java client)
            // Assert.IsNotNull(ng_selected_country.WrappedElement);
            // ng_countries = _ngDriver.FindElements(NgBy.Options(optionsCountry));
            NgWebElement ng_country = ng_countries.First(o => o.Selected);
            StringAssert.IsMatch("Australia", ng_country.Text);
        }

        [Test]
        public void ShouldDropDownWatch()
        {
            GetPageContent("ng_dropdown_watch.htm");
            string optionsCountry = "country for country in countries";
            ReadOnlyCollection<NgWebElement> ng_countries = _ngDriver.FindElements(NgBy.Options(optionsCountry));

            Assert.IsTrue(3 == ng_countries.Count);
            Assert.IsTrue(ng_countries[0].Enabled);
            string optionsState = "state for state in states";
            NgWebElement ng_state = _ngDriver.FindElement(NgBy.Options(optionsState));
            Assert.IsFalse(ng_state.Enabled);
            SelectElement countries = new SelectElement(_ngDriver.FindElement(NgBy.Model("country")).WrappedElement);
            countries.SelectByText("china");
            Thread.Sleep(1000);
            Assert.IsTrue(ng_state.Enabled);
            NgWebElement ng_selected_country = _ngDriver.FindElement(NgBy.SelectedOption("country"));
            Assert.IsNotNull(ng_selected_country.WrappedElement);
            NgWebElement ng_country = ng_countries.First(o => o.Selected);
            StringAssert.IsMatch("china", ng_country.Text);

        }

        [Test]
        public void ShouldEvaluateIf()
        {
            GetPageContent("ng_watch_ng_if.htm");
            IWebElement button = _ngDriver.FindElement(By.CssSelector("button.btn"));
            NgWebElement ng_button = new NgWebElement(_ngDriver, button);
            Object state = ng_button.Evaluate("!house.frontDoor.isOpen");
            Assert.IsTrue(Convert.ToBoolean(state));
            StringAssert.IsMatch("house.frontDoor.open()", button.GetAttribute("ng-click"));
            StringAssert.IsMatch("Open Door", button.Text);
            button.Click();
        }

        [Test]
        public void ShouldFindRepeaterSelectedtOption()
        {
            GetPageContent("ng_repeat_selected.htm");
            NgWebElement ng_element = _ngDriver.FindElement(NgBy.SelectedRepeaterOption("fruit in Fruits"));
            StringAssert.IsMatch("Mango", ng_element.Text);
        }


        [Test]
        public void ShouldChangeRepeaterSelectedtOption()
        {
            GetPageContent("ng_repeat_selected.htm");
            NgWebElement ng_element = _ngDriver.FindElement(NgBy.SelectedRepeaterOption("fruit in Fruits"));
            StringAssert.IsMatch("Mango", ng_element.Text);
            ReadOnlyCollection<NgWebElement> ng_elements = _ngDriver.FindElements(NgBy.Repeater("fruit in Fruits"));
            ng_element = ng_elements.First(o => String.Compare("Orange", o.Text,
                                                               StringComparison.InvariantCulture) == 0);
            ng_element.Click();
            string text = ng_element.Text;
            // to trigger WaitForAngular
            Assert.IsTrue(ng_element.Displayed);
            // reload
            ng_element = _ngDriver.FindElement(NgBy.SelectedRepeaterOption("fruit in Fruits"));
            StringAssert.IsMatch("Orange", ng_element.Text);

        }


        [Test]
        public void ShouldHandleMultiSelect()
        // appears to be broken in PahtomJS / working in desktop browsers
        {
            Actions actions = new Actions(_ngDriver.WrappedDriver);
            GetPageContent("ng_multi_select.htm");
            IWebElement element = _ngDriver.FindElement(NgBy.Model("selectedValues"));
            // use core Selenium
            IList<IWebElement> options = new SelectElement(element).Options;
            IEnumerator<IWebElement> etr = options.Where(o => Convert.ToBoolean(o.GetAttribute("selected"))).GetEnumerator();
            while (etr.MoveNext())
            {
                Console.Error.WriteLine(etr.Current.Text);
            }
            foreach (IWebElement option in options)
            {
                // http://selenium.googlecode.com/svn/trunk/docs/api/dotnet/html/AllMembers_T_OpenQA_Selenium_Keys.htm
                actions.KeyDown(Keys.Control).Click(option).KeyUp(Keys.Control).Build().Perform();
                // triggers _ngDriver.WaitForAngular()
                Assert.IsNotEmpty(_ngDriver.Url);
            }
            // re-read select options
            element = _ngDriver.FindElement(NgBy.Model("selectedValues"));
            options = new SelectElement(element).Options;
            etr = options.Where(o => Convert.ToBoolean(o.GetAttribute("selected"))).GetEnumerator();
            while (etr.MoveNext())
            {
                Console.Error.WriteLine(etr.Current.Text);
            }
        }

        [Test]
        public void ShouldPrintOrderByFieldColumn()
        {
            GetPageContent("ng_headers_sort_example2.htm");
            String[] headers = new String[] { "First Name", "Last Name", "Age" };
            foreach (String header in headers)
            {
                for (int cnt = 0; cnt != 2; cnt++)
                {
                    IWebElement headerElement = _ngDriver.FindElement(By.XPath("//th/a[contains(text(),'" + header + "')]"));
                    Console.Error.WriteLine("Clicking on header: " + header);
                    headerElement.Click();
                    // triggers _ngDriver.WaitForAngular()
                    Assert.IsNotEmpty(_ngDriver.Url);
                    ReadOnlyCollection<NgWebElement> ng_emps = _ngDriver.FindElements(NgBy.Repeater("emp in data.employees"));
                    NgWebElement ng_emp = ng_emps[0];
                    String field = ng_emp.GetAttribute("ng-order-by");
                    Console.Error.WriteLine(field + ": " + ng_emp.Evaluate(field).ToString());
                    String empField = "emp." + ng_emp.Evaluate(field);
                    Console.Error.WriteLine(empField + ":");
                    var ng_emp_enumerator = ng_emps.GetEnumerator();
                    ng_emp_enumerator.Reset();
                    while (ng_emp_enumerator.MoveNext())
                    {
                        ng_emp = (NgWebElement)ng_emp_enumerator.Current;
                        if (ng_emp.Text == null)
                        {
                            break;
                        }
                        Assert.IsNotNull(ng_emp.WrappedElement);

                        try
                        {
                            NgWebElement ng_column = ng_emp.FindElement(NgBy.Binding(empField));
                            Assert.IsNotNull(ng_column);
                            Console.Error.WriteLine(ng_column.Text);
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine(ex.ToString());
                        }
                    }
                }
            }
        }

        [Test]
        public void ShouldFindOrderByField()
        {
            GetPageContent("ng_headers_sort_example1.htm");

            String[] headers = new String[] { "First Name", "Last Name", "Age" };
            foreach (String header in headers)
            {
                IWebElement headerelement = _ngDriver.FindElement(By.XPath(String.Format("//th/a[contains(text(),'{0}')]", header)));
                Console.Error.WriteLine(header);
                headerelement.Click();
                // to trigger WaitForAngular
                Assert.IsNotEmpty(_ngDriver.Url);
                IWebElement emp = _ngDriver.FindElement(NgBy.Repeater("emp in data.employees"));
                NgWebElement ngRow = new NgWebElement(_ngDriver, emp);
                String orderByField = emp.GetAttribute("ng-order-by");
                Console.Error.WriteLine(orderByField + ": " + ngRow.Evaluate(orderByField).ToString());
            }


            ReadOnlyCollection<NgWebElement> ng_people = _ngDriver.FindElements(NgBy.Repeater("person in people"));
            var ng_people_enumerator = ng_people.GetEnumerator();
            ng_people_enumerator.Reset();
            while (ng_people_enumerator.MoveNext())
            {
                NgWebElement ng_person = (NgWebElement)ng_people_enumerator.Current;
                if (ng_person.Text == null)
                {
                    break;
                }
                NgWebElement ng_name = ng_person.FindElement(NgBy.Binding("person.Name"));
                Assert.IsNotNull(ng_name.WrappedElement);
                Object obj_country = ng_person.Evaluate("person.Country");
                Assert.IsNotNull(obj_country);
                if (String.Compare("Around the Horn", ng_name.Text) == 0)
                {
                    StringAssert.IsMatch("UK", obj_country.ToString());
                }
            }
        }

        [Test]
        public void ShouldFindElementByModel()
        {
            //  NOTE: works with Angular 1.2.13, fails with Angular 1.4.9
            GetPageContent("ng_pattern_validate.htm");
            NgWebElement ng_input = _ngDriver.FindElement(NgBy.Model("myVal"));
            ng_input.Clear();
            NgWebElement ng_valid = _ngDriver.FindElement(NgBy.Binding("form.value.$valid"));
            StringAssert.IsMatch("false", ng_valid.Text);

            NgWebElement ng_pattern = _ngDriver.FindElement(NgBy.Binding("form.value.$error.pattern"));
            StringAssert.IsMatch("false", ng_pattern.Text);

            NgWebElement ng_required = _ngDriver.FindElement(NgBy.Binding("!!form.value.$error.required"));
            StringAssert.IsMatch("true", ng_required.Text);

            ng_input.SendKeys("42");
            Assert.IsTrue(ng_input.Displayed);
            ng_valid = _ngDriver.FindElement(NgBy.Binding("form.value.$valid"));
            StringAssert.IsMatch("true", ng_valid.Text);

            ng_pattern = _ngDriver.FindElement(NgBy.Binding("form.value.$error.pattern"));
            StringAssert.IsMatch("false", ng_pattern.Text);

            ng_required = _ngDriver.FindElement(NgBy.Binding("!!form.value.$error.required"));
            StringAssert.IsMatch("false", ng_required.Text);
        }

        [Test]
        public void ShouldHandleSearchAngularUISelect()
        {
            GetPageContent("ng_ui_select_example1.htm");
            String searchText = "Ma";
            IWebElement search = _ngDriver.FindElement(By.CssSelector("input[type='search']"));
            search.SendKeys(searchText);
            NgWebElement ng_search = new NgWebElement(_ngDriver, search);

            StringAssert.IsMatch(@"input", ng_search.TagName); // triggers  _ngDriver.waitForAngular();
            ReadOnlyCollection<IWebElement> available_colors = _ngDriver.WrappedDriver.FindElements(By.CssSelector("div[role='option']"));

            var matching_colors = available_colors.Where(color => color.Text.Contains(searchText));
            foreach (IWebElement matching_color in matching_colors)
            {
                _ngDriver.Highlight(matching_color);
                Console.Error.WriteLine(String.Format("Matched color: {0}", matching_color.Text));
            }


        }

        [Test]
        public void ShouldHandleDeselectAngularUISelect()
        {
            GetPageContent("ng_ui_select_example1.htm");
            ReadOnlyCollection<NgWebElement> ng_selected_colors = _ngDriver.FindElements(NgBy.Repeater("$item in $select.selected"));
            while (true)
            {
                ng_selected_colors = _ngDriver.FindElements(NgBy.Repeater("$item in $select.selected"));
                if (ng_selected_colors.Count == 0)
                {
                    break;
                }
                NgWebElement ng_deselect_color = ng_selected_colors.Last();
                Object itemColor = ng_deselect_color.Evaluate("$item");
                Console.Error.WriteLine(String.Format("Deselecting color: {0}", itemColor.ToString()));
                IWebElement ng_close = ng_deselect_color.FindElement(By.CssSelector("span[class *='close']"));
                Assert.IsNotNull(ng_close);
                Assert.IsNotNull(ng_close.GetAttribute("ng-click"));
                StringAssert.IsMatch(@"removeChoice", ng_close.GetAttribute("ng-click"));

                _ngDriver.Highlight(ng_close);
                ng_close.Click();
                // _ngDriver.waitForAngular();

            }
            Console.Error.WriteLine("Nothing is selected");

        }

        [Test]
        public void ShouldHandleAngularUISelect()
        {
            GetPageContent("ng_ui_select_example1.htm");
            ReadOnlyCollection<NgWebElement> ng_selected_colors = _ngDriver.FindElements(NgBy.Repeater("$item in $select.selected"));
            Assert.IsTrue(2 == ng_selected_colors.Count);
            foreach (NgWebElement ng_selected_color in ng_selected_colors)
            {
                _ngDriver.Highlight(ng_selected_color);
                Object selected_color_item = ng_selected_color.Evaluate("$item");
                Console.Error.WriteLine(String.Format("selected color: {0}", selected_color_item.ToString()));
            }
            // IWebElement search = _ngDriver.FindElement(By.CssSelector("input[type='search']"));
            // same element
            NgWebElement ng_search = _ngDriver.FindElement(NgBy.Model("$select.search"));
            ng_search.Click();
            int wait_seconds = 3;
            WebDriverWait wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(wait_seconds));
            wait.Until(d => (d.FindElements(By.CssSelector("div[role='option']"))).Count > 0);
            ReadOnlyCollection<NgWebElement> ng_available_colors = _ngDriver.FindElements(By.CssSelector("div[role='option']"));
            Assert.IsTrue(6 == ng_available_colors.Count);
            foreach (NgWebElement ng_available_color in ng_available_colors)
            {
                _ngDriver.Highlight(ng_available_color);
                int available_color_index = -1;
                try
                {
                    available_color_index = Int32.Parse(ng_available_color.Evaluate("$index").ToString());
                }
                catch (Exception)
                {
                    // ignore
                }
                Console.Error.WriteLine(String.Format("available color [{1}]:{0}", ng_available_color.Text, available_color_index));
            }
        }


        [Test]
        public void ShouldFindElementByRepeaterColumn()
        {
            GetPageContent("ng_service.htm");
            // TODO: properly wait for Angular service to complete
            Thread.Sleep(3000);
            // _wait.Until(ExpectedConditions.ElementIsVisible(NgBy.Repeater("person in people")));
            ReadOnlyCollection<NgWebElement> ng_people = _ngDriver.FindElements(NgBy.Repeater("person in people"));
            if (ng_people.Count > 0)
            {
                ng_people = _ngDriver.FindElements(NgBy.Repeater("person in people"));
                var check = ng_people.Select(o => o.FindElement(NgBy.Binding("person.Country")));
                Assert.AreEqual(ng_people.Count, check.Count());
                ReadOnlyCollection<NgWebElement> ng_countries = _ngDriver.FindElements(NgBy.RepeaterColumn("person in people", "person.Country"));

                Assert.AreEqual(3, ng_countries.Count(o => String.Compare("Mexico", o.Text,
                                                                          StringComparison.InvariantCulture) == 0));
            }
        }

        [Test]
        public void ShouldFindSelectedtOption()
        {
            GetPageContent("ng_select_array.htm");
            NgWebElement ng_element = _ngDriver.FindElement(NgBy.SelectedOption("myChoice"));
            StringAssert.IsMatch("three", ng_element.Text);
            Assert.IsTrue(ng_element.Displayed);
        }

        [Test]
        public void ShouldChangeSelectedtOption()
        {
            GetPageContent("ng_select_array.htm");
            ReadOnlyCollection<NgWebElement> ng_elements = _ngDriver.FindElements(NgBy.Repeater("option in options"));
            NgWebElement ng_element = ng_elements.First(o => String.Compare("two", o.Text,
                                                                            StringComparison.InvariantCulture) == 0);
            ng_element.Click();
            string text = ng_element.Text;
            // to trigger WaitForAngular
            Assert.IsTrue(ng_element.Displayed);

            ng_element = _ngDriver.FindElement(NgBy.SelectedOption("myChoice"));
            StringAssert.IsMatch(text, ng_element.Text);
            Assert.IsTrue(ng_element.Displayed);
        }

        [Test]
        public void ShouldFindCells()
        {
            //  NOTE: works with Angular 1.2.13, fails with Angular 1.4.9
            GetPageContent("ng_repeat_start_end.htm");
            ReadOnlyCollection<NgWebElement> elements = _ngDriver.FindElements(NgBy.RepeaterColumn("definition in definitions", "definition.text"));
            Assert.AreEqual(2, elements.Count);
            StringAssert.IsMatch("Lorem ipsum", elements[0].Text);
        }

        [Test]
        public void ShouldNavigateDatesInDatePicker()
        {
            GetPageContent("ng_datepicker.htm");
            NgWebElement ng_result = _ngDriver.FindElement(NgBy.Model("data.inputOnTimeSet"));
            ng_result.Clear();
            _ngDriver.Highlight(ng_result);
            IWebElement calendar = _ngDriver.FindElement(By.CssSelector(".input-group-addon"));
            _ngDriver.Highlight(calendar);
            Actions actions = new Actions(_ngDriver.WrappedDriver);
            actions.MoveToElement(calendar).Click().Build().Perform();

            IWebElement dropdown = _driver.FindElement(By.CssSelector("div.dropdown.open ul.dropdown-menu"));
            NgWebElement ng_dropdown = new NgWebElement(_ngDriver, dropdown);
            Assert.IsNotNull(ng_dropdown);
            NgWebElement ng_display = _ngDriver.FindElement(NgBy.Binding("data.previousViewDate.display"));
            Assert.IsNotNull(ng_display);
            String dateDattern = @"\d{4}\-(?<month>\w{3})";

            Regex dateDatternReg = new Regex(dateDattern);

            Assert.IsTrue(dateDatternReg.IsMatch(ng_display.Text));
            _ngDriver.Highlight(ng_display);
            String display_month = ng_display.Text.FindMatch(dateDattern);
            // Console.Error.WriteLine("Current month: " + ng_display.Text);
            String[] months = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Dec", "Jan" };

            String next_month = months[Array.IndexOf(months, display_month) + 1];

            Console.Error.WriteLine("Current month: " + display_month);
            Console.Error.WriteLine("Next month: " + next_month);
            IWebElement ng_right = ng_display.FindElement(By.XPath("..")).FindElement(By.ClassName("right"));
            Assert.IsNotNull(ng_right);
            _ngDriver.Highlight(ng_right, 100);
            ng_right.Click();
            Assert.IsTrue(ng_display.Text.Contains(next_month));
            _ngDriver.Highlight(ng_display);
            Console.Error.WriteLine("Next month: " + ng_display.Text);
        }

        [Test]
        public void ShouldProperlyHandeMixedPages()
        {
            NgWebElement element;
            _ngDriver.Navigate().GoToUrl("http://dalelotts.github.io/angular-bootstrap-datetimepicker/");
            Action a = () =>
            {
                element = _ngDriver.FindElements(NgBy.Model("data.dateDropDownInput")).First();
                Console.Error.WriteLine("Type: {0}", element.GetAttribute("type"));
            };
            a.ShouldThrow<InvalidOperationException>();
            // it is somewhat hard to build expectation on exact exception message
            // Can't find variable: angular
            // [ng:test] no injector found for element argument to getTestability

            // '[ng-app]', '[data-ng-app]'
            element = _ngDriver.FindElements(NgBy.Model("data.dateDropDownInput", "[data-ng-app]")).First();
            Assert.IsNotNull(element);
            Console.Error.WriteLine("Type: {0}", element.GetAttribute("type"));

        }


        [Test]
        public void ShouldDirectSelectFromDatePicker()
        {
            GetPageContent("ng_datepicker.htm");
            // http://dalelotts.github.io/angular-bootstrap-datetimepicker/
            NgWebElement ng_result = _ngDriver.FindElement(NgBy.Model("data.inputOnTimeSet"));
            ng_result.Clear();
            _ngDriver.Highlight(ng_result);
            IWebElement calendar = _ngDriver.FindElement(By.CssSelector(".input-group-addon"));
            _ngDriver.Highlight(calendar);
            Actions actions = new Actions(_ngDriver.WrappedDriver);
            actions.MoveToElement(calendar).Click().Build().Perform();

            int datepicker_width = 900;
            int datepicker_heght = 800;
            _driver.Manage().Window.Size = new System.Drawing.Size(datepicker_width, datepicker_heght);
            IWebElement dropdown = _driver.FindElement(By.CssSelector("div.dropdown.open ul.dropdown-menu"));
            NgWebElement ng_dropdown = new NgWebElement(_ngDriver, dropdown);
            Assert.IsNotNull(ng_dropdown);
            ReadOnlyCollection<NgWebElement> elements = ng_dropdown.FindElements(NgBy.Repeater("dateObject in week.dates"));
            Assert.IsTrue(28 <= elements.Count);

            String monthDate = "12";
            IWebElement dateElement = ng_dropdown.FindElements(NgBy.CssContainingText("td.ng-binding", monthDate)).First();
            Console.Error.WriteLine("Mondh Date: " + dateElement.Text);
            dateElement.Click();
            NgWebElement ng_element = ng_dropdown.FindElement(NgBy.Model("data.inputOnTimeSet"));
            Assert.IsNotNull(ng_element);
            _ngDriver.Highlight(ng_element);
            ReadOnlyCollection<NgWebElement> ng_dataDates = ng_element.FindElements(NgBy.Repeater("dateObject in data.dates"));
            Assert.AreEqual(24, ng_dataDates.Count);

            String timeOfDay = "6:00 PM";
            NgWebElement ng_hour = ng_element.FindElements(NgBy.CssContainingText("span.hour", timeOfDay)).First();
            Assert.IsNotNull(ng_hour);
            _ngDriver.Highlight(ng_hour);
            Console.Error.WriteLine("Hour of the day: " + ng_hour.Text);
            ng_hour.Click();
            String specificMinute = "6:35 PM";

            // reload
            // dropdown = _driver.FindElement(By.CssSelector("div.dropdown.open ul.dropdown-menu"));
            // ng_dropdown = new NgWebElement(_ngDriver, dropdown);
            ng_element = ng_dropdown.FindElement(NgBy.Model("data.inputOnTimeSet"));
            Assert.IsNotNull(ng_element);
            _ngDriver.Highlight(ng_element);
            NgWebElement ng_minute = ng_element.FindElements(NgBy.CssContainingText("span.minute", specificMinute)).First();
            Assert.IsNotNull(ng_minute);
            _ngDriver.Highlight(ng_minute);
            Console.Error.WriteLine("Time of the day: " + ng_minute.Text);
            ng_minute.Click();
            ng_result = _ngDriver.FindElement(NgBy.Model("data.inputOnTimeSet"));
            _ngDriver.Highlight(ng_result, 100);
            Console.Error.WriteLine("Selected Date/time: " + ng_result.GetAttribute("value"));
        }

        [Test]
        public void ShouldFindOptions()
        {
            // base_url = "http://www.java2s.com/Tutorials/AngularJSDemo/n/ng_options_with_object_example.htm";
            GetPageContent("ng_options_with_object.htm");
            ReadOnlyCollection<NgWebElement> elements = _ngDriver.FindElements(NgBy.Options("c.name for c in colors"));
            Assert.AreEqual(5, elements.Count);
            try
            {
                List<Dictionary<String, String>> result = elements[0].ScopeOf();
            }
            catch (InvalidOperationException)
            {
                // Maximum call stack size exceeded.
                // TODO
            }
            StringAssert.IsMatch("black", elements[0].Text);
            StringAssert.IsMatch("white", elements[1].Text);
        }

        [Test]
        public void ShouldFindRows()
        {
            GetPageContent("ng_repeat_start_end.htm");
            ReadOnlyCollection<NgWebElement> elements = _ngDriver.FindElements(NgBy.Repeater("definition in definitions"));
            Assert.IsTrue(elements[0].Displayed);

            StringAssert.AreEqualIgnoringCase(elements[0].Text, "Foo");
        }

        [Test]
        public void ShouldHandleFluentExceptions()
        {
            GetPageContent("ng_repeat_start_end.htm");
            Action a = () =>
            {
                var displayed = _ngDriver.FindElement(NgBy.Repeater("this is not going to be found")).Displayed;
            };
            // NoSuchElement Exception is not thrown by Protractor
            // a.ShouldThrow<NoSuchElementException>().WithMessage("Could not find element by: NgBy.Repeater:");
            a.ShouldThrow<NullReferenceException>();
        }

        [Test]
        public void ShouldUpload()
        {
            // GetPageContent("ng_upload1.htm");
            //  need to run
            _ngDriver.Navigate().GoToUrl("http://localhost:8080/ng_upload1.htm");

            IWebElement file = _driver.FindElement(By.CssSelector("div[ng-controller = 'myCtrl'] > input[type='file']"));
            Assert.IsNotNull(file);
            StringAssert.AreEqualIgnoringCase(file.GetAttribute("file-model"), "myFile");
            String localPath = CreateTempFile("lorem ipsum dolor sit amet");


            IAllowsFileDetection fileDetectionDriver = _driver as IAllowsFileDetection;
            if (fileDetectionDriver == null)
            {
                Assert.Fail("driver does not support file detection. This should not be");
            }

            fileDetectionDriver.FileDetector = new LocalFileDetector();

            try
            {
                file.SendKeys(localPath);
            }
            catch (WebDriverException e)
            {
                // the operation has timed out
                Console.Error.WriteLine(e.Message);
            }
            NgWebElement button = _ngDriver.FindElement(NgBy.ButtonText("Upload"));
            button.Click();
            NgWebElement ng_file = new NgWebElement(_ngDriver, file);
            Object myFile = ng_file.Evaluate("myFile");
            if (myFile != null)
            {
                Dictionary<String, Object> result = (Dictionary<String, Object>)myFile;
                Assert.IsTrue(result.Keys.Contains("name"));
                Assert.IsTrue(result.Keys.Contains("type"));
                Assert.IsTrue(result.Keys.Contains("size"));
            }
            else
            {
                Console.Error.WriteLine("myFile is null");
            }
            String script = "var e = angular.element(arguments[0]); var f = e.scope().myFile; if (f){return f.name} else {return null;}";
            try
            {
                Object result = ((IJavaScriptExecutor)_driver).ExecuteScript(script, ng_file);
                if (result != null)
                {
                    Console.Error.WriteLine(result.ToString());
                }
                else
                {
                    Console.Error.WriteLine("result is null");
                }
            }
            catch (InvalidOperationException e)
            {
                Console.Error.WriteLine(e.Message);
            }
        }

        [Test]
        public void ShouldFindAllBindings()
        {
            GetPageContent("ng_directive_binding.htm");
            IWebElement container = _ngDriver.FindElement(By.CssSelector("body div"));
            Console.Error.WriteLine(container.GetAttribute("innerHTML"));
            ReadOnlyCollection<NgWebElement> elements = _ngDriver.FindElements(NgBy.Binding("name"));
            Assert.AreEqual(5, elements.Count);
            foreach (NgWebElement element in elements)
            {
                Console.Error.WriteLine(element.GetAttribute("outerHTML"));
                Console.Error.WriteLine(String.Format("Identity: {0}", element.IdentityOf()));
                Console.Error.WriteLine(String.Format("Text: {0}", element.Text));
            }
        }

        [Test]
        public void ShouldAngularTodoApp()
        {
            GetPageContent("ng_todo.htm");
            ReadOnlyCollection<NgWebElement> ng_todo_elements = _ngDriver.FindElements(NgBy.Repeater("todo in todoList.todos"));
            String ng_identity = ng_todo_elements[0].IdentityOf();
            // <input type="checkbox" ng-model="todo.done" class="ng-pristine ng-untouched ng-valid">
            // <span class="done-true">learn angular</span>
            List<Dictionary<String, String>> todo_scope_data = ng_todo_elements[0].ScopeDataOf("todoList.todos");
            int todo_index = todo_scope_data.FindIndex(o => String.Equals(o["text"], "build an angular app"));
            Assert.AreEqual(1, todo_index);
            //foreach (var row in todo_scope_data)
            //{
            //    foreach (string key in row.Keys)
            //    {
            //        Console.Error.WriteLine(key + " " + row[key]);
            //    }
            //}
        }

        private string CreateTempFile(string content)
        {
            FileInfo testFile = new FileInfo("web_driver.tmp");
            if (testFile.Exists)
            {
                testFile.Delete();
            }
            StreamWriter testFileWriter = testFile.CreateText();
            testFileWriter.WriteLine(content);
            testFileWriter.Close();
            return testFile.FullName;
        }
    }
}