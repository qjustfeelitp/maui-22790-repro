namespace Maui22790;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

        this.MainPage = new NavigationPage(new MainPage());
    }
}
