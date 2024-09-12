using NUnit.Framework;
using OpenQA.Selenium.Appium;

namespace Maui22790.Tests;

public sealed class CrashIt
{
    private AppiumDriver App
    {
        get { return AppiumSetup.App; }
    }

    private AppiumElement FindUIElement(string id)
    {
        return this.App.FindElement(MobileBy.AccessibilityId(id));
    }

    /// <summary>
    /// This will fail because the windows get closed!
    /// </summary>
    [Test]
    public async Task LetItCrash()
    {
        while (true)
        {
            var toElement = FindUIElement("NavigateTo");

            toElement.Click();
            await Task.Delay(500);

            var fromElement = FindUIElement("NavigateFrom");
            fromElement.Click();
            await Task.Delay(500);
        }
    }
}
