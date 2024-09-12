namespace Maui22790;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
		InitializeComponent();
	}

    private async void Navigate(object sender, EventArgs e)
    {
        await this.Navigation.PushAsync(new SecondPage(), true);
    }
}

