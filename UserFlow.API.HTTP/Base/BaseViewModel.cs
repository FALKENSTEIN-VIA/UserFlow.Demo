using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using System.Net;

namespace UserFlow.API.HTTP.Base;

public abstract partial class BaseViewModel : ObservableObject
{
    protected readonly ILogger _logger;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    protected BaseViewModel(ILogger logger)
    {
        _logger = logger;
    }

    public virtual Task OnViewAppearingAsync() => Task.CompletedTask;
    public virtual Task OnViewDisappearingAsync() => Task.CompletedTask;

    protected async Task RunApiAsync<T>(
        Func<Task<T>> call,
        Func<T, Task>? onSuccess = null,
        Action? onFailure = null,
        string loadingMessage = "Loading...")
    {
        IsBusy = true;
        StatusMessage = loadingMessage;

        try
        {
            var result = await call();
            if (result == null)
            {
                StatusMessage = "No data received";
                onFailure?.Invoke();
            }
            else
            {
                if (onSuccess != null)
                    await onSuccess(result);

                await OnSuccessAsync();
                StatusMessage = "Operation succeeded";
            }
        }
        catch (Exception ex) when (IsUnauthorizedException(ex))
        {
            StatusMessage = "Session expired. Please login again";
            onFailure?.Invoke();
            await OnErrorAsync(ex);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            _logger.LogError(ex, "API operation failed");
            onFailure?.Invoke();
            await OnErrorAsync(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected virtual Task OnErrorAsync(Exception ex) => Task.CompletedTask;
    protected virtual Task OnSuccessAsync() => Task.CompletedTask;

    private static bool IsUnauthorizedException(Exception ex) =>
        ex is HttpRequestException httpEx && httpEx.StatusCode == HttpStatusCode.Unauthorized;
}













/////// *****************************************************************************************
/////// @file BaseViewModel.cs
/////// @author Claus Falkenstein
/////// @company VIA Software GmbH
/////// @date 2025-05-09
/////// @brief Abstract base class for all MVVM ViewModels.
/////// @details
/////// Provides shared functionality such as loading state handling (IsBusy),
/////// status messaging, and a safe async wrapper for API calls with logging.
/////// *****************************************************************************************

//using CommunityToolkit.Mvvm.ComponentModel;
//using Microsoft.Extensions.Logging;
//using System.Net;

//namespace UserFlow.API.HTTP.Base;

///// <summary>
///// Abstract base class for all MVVM ViewModels providing common functionality like IsBusy, StatusMessage,
///// safe API call execution, and lifecycle hooks (OnViewAppearingAsync, OnViewDisappearingAsync).
///// </summary>
//public abstract partial class BaseViewModel : ObservableObject
//{
//    #region 👉 Observable Properties

//    [ObservableProperty]
//    public partial bool IsBusy { get; set; }

//    [ObservableProperty]
//    public partial string StatusMessage { get; set; } = string.Empty;

//    #endregion

//    #region 👉 OnViewAppearing / OnViewDisappearing 

//    /// <summary>
//    /// 🔄 Called when the view appears (optional override in concrete VMs)
//    /// </summary>
//    public virtual Task OnViewAppearingAsync() => Task.CompletedTask;

//    /// <summary>
//    /// 🚪 Called when the view disappears (optional override in concrete VMs)
//    /// </summary>
//    public virtual Task OnViewDisappearingAsync() => Task.CompletedTask;

//    #endregion

//    #region 🛡 RunAPI Async Method

//    /// <summary>
//    /// 🛡 Executes an API call with safe handling, busy indicator, logging, and error fallback.
//    /// </summary>
//    protected async Task RunApiAsync<T>(
//        Func<Task<T?>> call,
//        Func<T, Task> onSuccess,
//        Action? onFailure = null,
//        string loadingMessage = "Loading...",
//        ILogger? logger = null)
//    {
//        IsBusy = true;
//        StatusMessage = loadingMessage;

//        try
//        {
//            var result = await call();

//            if (result == null)
//            {
//                StatusMessage = "No data received";
//                onFailure?.Invoke();
//                return;
//            }

//            await onSuccess(result);
//            StatusMessage = "Operation succeeded";
//        }
//        catch (Exception ex) when (IsUnauthorizedException(ex))
//        {
//            StatusMessage = "Session expired. Please login again";
//            onFailure?.Invoke();
//        }
//        catch (Exception ex)
//        {
//            StatusMessage = $"Error: {ex.Message}";
//            logger?.LogError(ex, "API operation failed");
//            onFailure?.Invoke();
//        }
//        finally
//        {
//            IsBusy = false;
//        }
//    }

//    private static bool IsUnauthorizedException(Exception ex)
//    {
//        return ex is HttpRequestException httpEx &&
//               httpEx.StatusCode == HttpStatusCode.Unauthorized;
//    }

//    #endregion
//}

/////// *****************************************************************************************
/////// @remarks 📄 Developer Notes:
/////// - ✅ Complies with current C# preview syntax for [ObservableProperty].
/////// - 🔁 Centralizes IsBusy and StatusMessage across all ViewModels.
/////// - 🧱 Portable: designed for reuse in MAUI, WPF, Blazor and more.
/////// - 📦 Use RunApiAsync to standardize safe API call patterns.
/////// *****************************************************************************************
