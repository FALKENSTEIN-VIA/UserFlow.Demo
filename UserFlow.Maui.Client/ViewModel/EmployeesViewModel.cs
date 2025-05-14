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

public partial class EmployeesViewModel : BaseChangeStreamsViewModel
{
    private readonly IUnitOfWork _unitOfWork;

    public EmployeesViewModel(IUnitOfWork unitOfWork, IHubService hubService, ILogger<EmployeesViewModel> logger)
        : base(hubService, logger)
    {
        _unitOfWork = unitOfWork;
    }

    protected override string ChangeStreamEntityName => "Employees";

    [ObservableProperty]
    public partial ObservableCollection<EmployeeDTO> Employees { get; set; } = [];

    [ObservableProperty]
    public partial EmployeeDTO? CurrentEmployee { get; set; }

    [ObservableProperty]
    public partial EmployeeDTO? ScrollToEmployee { get; set; }

    [RelayCommand]
    public async Task LoadAsync()
    {
        await RunApiAsync(
            () => _unitOfWork.Employees.GetAllAsync(),
            result =>
            {
                Employees = new ObservableCollection<EmployeeDTO>((result ?? []).OrderBy(x => x.Id));
                CurrentEmployee = Employees.FirstOrDefault(x => x.Id == CurrentEmployee?.Id) ?? Employees.FirstOrDefault();
                ScrollToEmployee = CurrentEmployee;
                _logger.LogInformation("✅ Loaded {Count} employees.", Employees.Count);
                return Task.CompletedTask;
            },
            onFailure: () => _logger.LogWarning("⚠️ Failed to load employees."),
            loadingMessage: "👥 Loading employees...");
    }

    [RelayCommand]
    public async Task UpdateCurrentEmployeeAsync()
    {
        if (CurrentEmployee == null) return;

        await RunApiAsync(
            async () =>
            {
                var updateDto = new EmployeeUpdateDTO
                {
                    Id = CurrentEmployee.Id,
                    Name = $"{CurrentEmployee.Name} (Updated {DateTime.Now:T})",
                    Email = CurrentEmployee.Email,
                };
                return await _unitOfWork.Employees.UpdateAsync(updateDto) ? updateDto : null;
            },
            async updateDto =>
            {
                if (updateDto == null)
                {
                    StatusMessage = "❌ Failed to update employee (null response).";
                    return;
                }

                StatusMessage = $"✅ Employee '{updateDto.Name}' updated successfully.";
                await LoadAsync();
            },
            onFailure: () => StatusMessage = "❌ Failed to update employee.",
            loadingMessage: "🔄 Updating employee...");
    }

    [RelayCommand]
    public void ScrollToCurrentEmployee()
    {
        if (CurrentEmployee != null)
        {
            ScrollToEmployee = CurrentEmployee;
            _logger.LogInformation("📍 ScrollTo triggered for Employee Id {Id}", CurrentEmployee.Id);
        }
    }

    protected override void OnChangeNotificationReceived(ChangeNotification notification)
    {
        // INFO: Enable this to reload the entire collection on change notification.
        //await LoadAsync();

        // INFO: Here the collection is updated based on the change notification.
        CollectionChangeHandler.ApplyChange(
            Employees,
            notification,
            id => _unitOfWork.Employees.GetByIdAsync(id),
            user => user.Id,
            action => App.Current?.Dispatcher.Dispatch(action)
        );
        ScrollToCurrentEmployee();
    }
}
