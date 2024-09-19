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
    public void LetItCrash()
    {
        while (true)
        { }
    }
}
