using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Diagnostics;
using UserFlow.API.ChangeStreams.Helper;
using UserFlow.API.Http.Base;
using UserFlow.API.Http.HubServices;
using UserFlow.API.HTTP;
using UserFlow.API.Shared.DTO;
using UserFlow.API.Shared.Notifications;

namespace UserFlow.Maui.Client.ViewModels;

public partial class ProjectsViewModel : BaseChangeStreamsViewModel
{
    private readonly IUnitOfWork _unitOfWork;

    public ProjectsViewModel(IUnitOfWork unitOfWork, IHubService hubService, ILogger<ProjectsViewModel> logger)
        : base(hubService, logger)
    {
        _unitOfWork = unitOfWork;
    }

    protected override string ChangeStreamEntityName => "Projects";

    [ObservableProperty]
    public partial ObservableCollection<ProjectDTO> Projects { get; set; } = [];

    [ObservableProperty]
    public partial ProjectDTO? CurrentProject { get; set; }

    [ObservableProperty]
    public partial ProjectDTO? ScrollToProject { get; set; }

    [RelayCommand]
    public async Task LoadAsync()
    {
        await RunApiAsync(
            () => _unitOfWork.Projects.GetAllAsync(),
            result =>
            {
                Projects = new ObservableCollection<ProjectDTO>((result ?? []).OrderBy(x => x.Id));
                CurrentProject = Projects.FirstOrDefault(x => x.Id == CurrentProject?.Id) ?? Projects.FirstOrDefault();
                ScrollToProject = CurrentProject;
                _logger.LogInformation("✅ Loaded {Count} projects.", Projects.Count);
                return Task.CompletedTask;
            },
            onFailure: () => _logger.LogWarning("⚠️ No projects returned from API."),
            loadingMessage: "📦 Loading projects...");
    }

    [RelayCommand]
    public void ScrollToCurrentProject()
    {
        if (CurrentProject != null)
        {
            ScrollToProject = CurrentProject;
            _logger.LogInformation("📍 ScrollTo triggered for Project Id {Id}", CurrentProject.Id);
        }
    }

    protected override void OnChangeNotificationReceived(ChangeNotification notification)
    {
        // INFO: Enable this to reload the entire collection on change notification.
        //var sw = Stopwatch.StartNew();
        //await LoadAsync();
        //_logger.LogInformation("⏱ Refreshed Projects after ChangeNotification in {Elapsed} ms", sw.ElapsedMilliseconds);

        // INFO: Here the collection is updated based on the change notification.
        CollectionChangeHandler.ApplyChange(
            Projects,
            notification,
            id => _unitOfWork.Projects.GetByIdAsync(id),
            project => project.Id,
            action => App.Current?.Dispatcher.Dispatch(action)
        );
    }
}
