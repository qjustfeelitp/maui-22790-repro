namespace Maui22790;

public partial class SecondPage : ContentPage
{
	public SecondPage()
	{
		InitializeComponent();
	}

    private async void Navigate(object sender, EventArgs e)
    {
        await this.Navigation.PopAsync();
    }
}