namespace UserFlow.Maui.Client.Views;

public partial class LoadingPage : ContentPage
{
    public LoadingPage()
    {
        InitializeComponent();
        Content = new ActivityIndicator { IsRunning = true };
    }
}