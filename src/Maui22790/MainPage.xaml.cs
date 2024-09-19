namespace Maui22790;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
		InitializeComponent();
	}

    /// <inheritdoc />
    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        await Task.Delay(TimeSpan.FromMilliseconds(100));
        await this.Navigation.PushAsync(new SecondPage(), true);
    }

    private async void Navigate(object sender, EventArgs e)
    {
        await this.Navigation.PushAsync(new SecondPage(), true);
    }
}

