param(
  [string]$browser
)


function launch_selenium {
  param(
    $browser = ''
  )
  $driver_folder_path = 'C:\java\selenium' 
  $shared_assemblies_path = 'c:\java\selenium\csharp\sharedassemblies'
  $shared_assemblies = @(
    'WebDriver.dll',
    'WebDriver.Support.dll',
    'nunit.framework.dll'
  )

  pushd $shared_assemblies_path

  $shared_assemblies | ForEach-Object {
    if ($host.Version.Major -gt 2) {
      Unblock-File -Path $_
    }
    Write-Debug $_
    Add-Type -Path $_
  }
  popd
  # adding driver folder to the path environment
  if (-not (Test-Path $driver_folder_path))
  {
    throw "Folder ${driver_folder_path} does not Exist, cannot be added to $env:PATH"
  }
  # See if the new folder is already in the path.
  if ($env:PATH | Select-String -SimpleMatch $driver_folder_path)
  { Write-Debug "Folder ${driver_folder_path} already within `$env:PATH"

  }

  # Set the new PATH environment
  $env:PATH = $env:PATH + ';' + $driver_folder_path


  # launch browser
  switch ($browser)
  {
    'firefox' {


      $selenium = New-Object OpenQA.Selenium.Firefox.FirefoxDriver
    }
    'chrome' {
      $selenium = New-Object OpenQA.Selenium.Chrome.ChromeDriver

      $selenium = New-Object OpenQA.Selenium.Firefox.FirefoxDriver
    }
    'ie' {


      $selenium = New-Object OpenQA.Selenium.IE.InternetExplorerDriver ($driver_folder_path)
    }
    default {
      Write-Host 'Running on phantomjs'
      $headless = $true
      $phantomjs_executable_folder = 'C:\tools\phantomjs-2.0.0\bin'
      $selenium = New-Object OpenQA.Selenium.PhantomJS.PhantomJSDriver ($phantomjs_executable_folder)
      $selenium.Capabilities.setCapability('ssl-protocol','any')
      $selenium.Capabilities.setCapability('ignore-ssl-errors',$true)
      $selenium.Capabilities.setCapability('takesScreenshot',$true)
      $selenium.Capabilities.setCapability('userAgent',$phantomjs_useragent)
      $options = New-Object OpenQA.Selenium.PhantomJS.PhantomJSOptions
      $options.AddAdditionalCapability('phantomjs.executable.path',$phantomjs_executable_folder)
    }

  }

  return $selenium
}


function find_element {
  param(
    [Parameter(ParameterSetName = 'set_xpath')] $xpath,
    [Parameter(ParameterSetName = 'set_css_selector')] $css,
    [Parameter(ParameterSetName = 'set_id')] $id,
    [Parameter(ParameterSetName = 'set_linktext')] $linktext,
    [Parameter(ParameterSetName = 'set_partial_link_text')] $partial_link_text,
    [Parameter(ParameterSetName = 'set_css_tagname')] $tagname
  )


  # guard
  $implemented_options = @{
    'xpath' = $true;
    'css' = $true;
    'id' = $false;
    'linktext' = $false;
    'partial_link_text' = $false;
    'tagname' = $false;
  }

  $implemented.Keys | ForEach-Object { $option = $_
    if ($psBoundParameters.ContainsKey($option)) {

      if (-not $implemented_options[$option]) {

        Write-Output ('Option {0} i not implemented' -f $option)



      } else {

      }
    }
  }
  if ($false) {
    Write-Output @psBoundParameters | Format-Table -AutoSize
  }
  $element = $null
  $wait_seconds = 5
  $wait_polling_interval = 50

  if ($css -ne $null) {

    [OpenQA.Selenium.Support.UI.WebDriverWait]$wait = New-Object OpenQA.Selenium.Support.UI.WebDriverWait ($selenium,[System.TimeSpan]::FromSeconds($wait_seconds))
    $wait.PollingInterval = $wait_polling_interval

    try {
      [void]$wait.Until([OpenQA.Selenium.Support.UI.ExpectedConditions]::ElementExists([OpenQA.Selenium.By]::CssSelector($css)))
    } catch [exception]{
      Write-Debug ("Exception : {0} ...`ncss = '{1}'" -f (($_.Exception.Message) -split "`n")[0],$css)
    }
    $element = $selenium.FindElement([OpenQA.Selenium.By]::CssSelector($css))


  }


  if ($xpath -ne $null) {

    [OpenQA.Selenium.Support.UI.WebDriverWait]$wait = New-Object OpenQA.Selenium.Support.UI.WebDriverWait ($selenum,[System.TimeSpan]::FromSeconds($wait_seconds))
    $wait.PollingInterval = $wait_polling_interval

    try {
      [void]$wait.Until([OpenQA.Selenium.Support.UI.ExpectedConditions]::ElementExists([OpenQA.Selenium.By]::XPath($xpath)))
    } catch [exception]{
      Write-Debug ("Exception : {0} ...`nxpath={1}" -f (($_.Exception.Message) -split "`n")[0],$xpath)
    }

    $element = $local:selenum_driver.FindElement([OpenQA.Selenium.By]::XPath($xpath))


  }

  return $element
}

function highlight {
  param(
    [object]$element,
    [int]$delay = 300
  )
  # https://selenium.googlecode.com/git/docs/api/java/org/openqa/selenium/JavascriptExecutor.html
  [OpenQA.Selenium.IJavaScriptExecutor]$selenium.ExecuteScript("arguments[0].setAttribute('style', arguments[1]);",$element,'color: yellow; border: 4px solid yellow;')
  Start-Sleep -Millisecond $delay
  [OpenQA.Selenium.IJavaScriptExecutor]$selenium.ExecuteScript("arguments[0].setAttribute('style', arguments[1]);",$element,'')
}



function cleanup
{
  param(
    [object]$selenium
  )
  try {
    $selenium.Quit()
  } catch [exception]{
    Write-Output (($_.Exception.Message) -split "`n")[0]
    # Ignore errors if unable to close the browser
  }
}


$selenium = launch_selenium -browser 'firefox'

[void]$selenium.Manage().Timeouts().ImplicitlyWait([System.TimeSpan]::FromSeconds(10))



$base_url = 'https://github.com/dzharii/swd-recorder'
$selenium.Navigate().GoToUrl($base_url)
# [OpenQA.Selenium.Remote.RemoteWebElement]$queryBox = $selenium.FindElement([OpenQA.Selenium.By]::Id('searchInput'))

$element = find_element -css 'div[id = "js-repo-pjax-container"] > div:nth-of-type(6) > table > tbody > tr:nth-of-type(9) > td:nth-of-type(3) > span > a'

highlight $element -Delay 10000

$element.Click()

# Cleanup
cleanup $selenium

