using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using UserFlow.API.ChangeStreams.Helper;
using UserFlow.API.Http.Base;
using UserFlow.API.Http.HubServices;
using UserFlow.API.HTTP;
using UserFlow.API.Shared.DTO;
using UserFlow.API.Shared.Notifications;

namespace UserFlow.Maui.Client.ViewModels;

public partial class CompaniesViewModel : BaseChangeStreamsViewModel
{
    private readonly IUnitOfWork _unitOfWork;

    public CompaniesViewModel(IUnitOfWork unitOfWork, IHubService hubService, ILogger<CompaniesViewModel> logger)
        : base(hubService, logger)
    {
        _unitOfWork = unitOfWork;
    }

    protected override string ChangeStreamEntityName => "Companies";

    [ObservableProperty]
    public partial ObservableCollection<CompanyDTO> Companies { get; set; } = [];

    [ObservableProperty]
    public partial CompanyDTO? CurrentCompany { get; set; }
    partial void OnCurrentCompanyChanged(CompanyDTO? value)
    {
        Employees.Clear();
        if (value != null)
            Task.Run(() => LoadEmployeesAsync(value.Id));
    }

    [ObservableProperty]
    public partial CompanyDTO? ScrollToCompany { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<EmployeeDTO> Employees { get; set; } = [];

    [ObservableProperty]
    public partial EmployeeDTO? CurrentEmployee { get; set; }

    [RelayCommand]
    public async Task LoadCompaniesAsync()
    {
        await RunApiAsync(
            call: () => _unitOfWork.Companies.GetAllAsync(),
            onSuccess: result =>
            {
                Companies = new ObservableCollection<CompanyDTO>((result ?? []).OrderBy(x => x.Id));
                CurrentCompany = Companies.FirstOrDefault(x => x.Id == CurrentCompany?.Id) ?? Companies.FirstOrDefault();
                ScrollToCompany = CurrentCompany;
                _logger.LogInformation("✅ Loaded {Count} companies.", Companies.Count);
                return Task.CompletedTask;
            },
            onFailure: () => _logger.LogWarning("⚠️ No companies returned from API."),
            loadingMessage: "🏢 Loading companies...");
    }

    private async Task LoadEmployeesAsync(long companyId)
    {
        if (CurrentCompany == null)
        {
            StatusMessage = "❗ Please select a company first.";
            return;
        }

        await RunApiAsync(
            () => _unitOfWork.Employees.GetByCompanyIdAsync(companyId),
            result =>
            {
                Employees = new ObservableCollection<EmployeeDTO>(result ?? []);
                CurrentEmployee = Employees.FirstOrDefault();
                _logger.LogInformation("✅ Loaded {Count} employees for company {CompanyId}.", Employees.Count, companyId);
                return Task.CompletedTask;
            },
            onFailure: () => _logger.LogWarning("⚠️ No employees returned for company {CompanyId}.", companyId),
            loadingMessage: "👔 Loading employees...");
    }

    [RelayCommand]
    public void ScrollToCurrentCompany()
    {
        if (CurrentCompany != null)
        {
            ScrollToCompany = CurrentCompany;
            _logger.LogInformation("📍 ScrollTo triggered for Company Id {Id}", CurrentCompany.Id);
        }
    }

    protected override void OnChangeNotificationReceived(ChangeNotification notification)
    {
        // INFO: Enable this to reload the entire collection on change notification.
        //await LoadAsync();

        // INFO: Here the collection is updated based on the change notification.
        CollectionChangeHandler.ApplyChange(
            Companies,
            notification,
            id => _unitOfWork.Companies.GetByIdAsync(id),
            company => company.Id,
            action => App.Current?.Dispatcher.Dispatch(action)
        );
    }
}
