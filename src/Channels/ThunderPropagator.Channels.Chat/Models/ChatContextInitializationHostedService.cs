using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ThunderPropagator.BuildingBlocks.Application;
using ThunderPropagator.Channels.Chat.Extensions;

namespace ThunderPropagator.Channels.Chat.Models
{
    /// <summary>
    /// Runs <see cref="BaseChatContext.InitializeAsync"/> during host startup, registered by
    /// <see cref="ChatChannelExtensions.AddChatChannel{TChatContext}"/> (#114). Hosted services'
    /// StartAsync all run — and are awaited — before IHost.StartAsync/RunAsync returns, so an
    /// exception here aborts application startup itself: nothing resolves a Chat pipeline against an
    /// unmigrated/unseeded <typeparamref name="TChatContext"/>. <typeparamref name="TChatContext"/> is
    /// Scoped (see AddChatChannel), so a fresh scope is created here to resolve it rather than
    /// injecting it directly into this Singleton hosted service.
    /// </summary>
    internal sealed partial class ChatContextInitializationHostedService<TChatContext>(
        IServiceScopeFactory scopeFactory,
        ILogger<ChatContextInitializationHostedService<TChatContext>> logger)
        : IHostedService
        where TChatContext : BaseChatContext
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var providerName = typeof(TChatContext).Name;
            using var activity = Telemetry.StartActivity($"{providerName}_{nameof(StartAsync)}", ActivityKind.Internal)?
                .SetTag("ChatContext.ProviderType", providerName);

            Log.InitializingChatContext(logger, providerName);

            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TChatContext>();

            try
            {
                await context.InitializeAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Cancelled");
                throw;
            }
            catch (Exception exception)
            {
                activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
                Log.ChatContextInitializationFailed(logger, exception, providerName);
                throw new ChatContextInitializationException(typeof(TChatContext), exception);
            }

            activity?.SetStatus(ActivityStatusCode.Ok);
            Log.ChatContextInitialized(logger, providerName);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        // Issue #39: LoggerMessage-generated methods for this hosted service's log call sites.
        // EventIds 1101-1103 are this file's own block; no cross-file EventId registry exists yet in
        // this repo.
        private static partial class Log
        {
            /// <summary>Logs that chat context initialization for a provider is starting.</summary>
            [LoggerMessage(EventId = 1101, Level = LogLevel.Information, Message = "Initializing chat context for provider {ProviderType}.")]
            public static partial void InitializingChatContext(ILogger logger, string providerType);

            /// <summary>Logs that chat context initialization for a provider failed.</summary>
            [LoggerMessage(EventId = 1102, Level = LogLevel.Critical, Message = "Chat context initialization failed for provider {ProviderType}.")]
            public static partial void ChatContextInitializationFailed(ILogger logger, Exception exception, string providerType);

            /// <summary>Logs that chat context initialization for a provider succeeded.</summary>
            [LoggerMessage(EventId = 1103, Level = LogLevel.Information, Message = "Chat context for provider {ProviderType} initialized successfully.")]
            public static partial void ChatContextInitialized(ILogger logger, string providerType);
        }
    }
}
