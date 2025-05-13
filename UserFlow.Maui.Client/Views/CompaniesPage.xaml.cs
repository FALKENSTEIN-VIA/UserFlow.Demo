using UserFlow.Maui.Client.UserControls;
using UserFlow.Maui.Client.ViewModels;

namespace UserFlow.Maui.Client.Views;

/// <summary>
/// 🏢 Companies and Employees Page.
/// </summary>
public partial class CompaniesPage : CustomPage
{
    private readonly CompaniesViewModel _viewModel;

    public CompaniesPage(CompaniesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        Loaded += async (_, _) => await _viewModel.LoadCompaniesAsync();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnViewAppearingAsync();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        await _viewModel.OnViewDisappearingAsync();
    }

    private void CompaniesCollectionView_Scrolled(object sender, ItemsViewScrolledEventArgs e)
    {
        // Optional Debugging
    }

    /// <summary>
    /// ScrollToItem auslösen, sobald es gesetzt wird.
    /// </summary>
    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_viewModel.ScrollToCompany) && _viewModel.ScrollToCompany != null)
                {
                    CompaniesCollectionView.ScrollTo(_viewModel.ScrollToCompany, position: ScrollToPosition.Center, animate: true);
                }
            };
        }
    }
}
