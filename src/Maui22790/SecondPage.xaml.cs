namespace Maui22790;

public partial class SecondPage : ContentPage
{
	public SecondPage()
	{
		InitializeComponent();
	}

    /// <inheritdoc />
    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        await Task.Delay(TimeSpan.FromMilliseconds(100));
        await this.Navigation.PopAsync();
    }

    private async void Navigate(object sender, EventArgs e)
    {
        await this.Navigation.PopAsync();
    }
}