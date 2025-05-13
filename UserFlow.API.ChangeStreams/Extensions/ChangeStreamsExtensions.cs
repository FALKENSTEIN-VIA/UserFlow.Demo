/// *****************************************************************************************
/// @file ChangeStreamsExtensions.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-13
/// @brief Extension methods for adding ChangeStreams services dependency injection container.
/// *****************************************************************************************

using Microsoft.Extensions.DependencyInjection;
using UserFlow.API.ChangeStreams.Services;

namespace UserFlow.API.ChangeStreams.Extensions;

public static class ChangeStreamsExtensions
{
    public static IServiceCollection AddChangeStreams(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddHostedService<DatabaseChangeService>();
        return services;
    }
}
