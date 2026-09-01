using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ThunderPropagator.BuildingBlocks.Application;

namespace ThunderPropagator.Channels.Games.RockPaperScissors.Models
{
    // Issue #288: mirrors ThunderPropagator.Channels.Chat.Models.ChatContextInitializationHostedService
    // (#114) — see its own comment for the full reasoning (Scoped context resolved via a fresh scope,
    // awaited during host startup so nothing resolves a game pipeline against an unmigrated/unseeded
    // store).
    internal sealed partial class RockPaperScissorsContextInitializationHostedService<TContext>(
        IServiceScopeFactory scopeFactory,
        ILogger<RockPaperScissorsContextInitializationHostedService<TContext>> logger)
        : IHostedService
        where TContext : BaseRockPaperScissorsContext
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var providerName = typeof(TContext).Name;
            using var activity = Telemetry.StartActivity($"{providerName}_{nameof(StartAsync)}", ActivityKind.Internal)?
                .SetTag("RockPaperScissorsContext.ProviderType", providerName);

            Log.InitializingContext(logger, providerName);

            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TContext>();

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
                Log.ContextInitializationFailed(logger, exception, providerName);
                throw new RockPaperScissorsContextInitializationException(typeof(TContext), exception);
            }

            activity?.SetStatus(ActivityStatusCode.Ok);
            Log.ContextInitialized(logger, providerName);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        // Issue #39: LoggerMessage-generated methods for this hosted service's log call sites. EventIds
        // 4101-4103 are this file's own block; no cross-file EventId registry exists yet in this repo.
        private static partial class Log
        {
            /// <summary>Logs that RockPaperScissors context initialization for a provider is starting.</summary>
            [LoggerMessage(EventId = 4101, Level = LogLevel.Information, Message = "Initializing RockPaperScissors context for provider {ProviderType}.")]
            public static partial void InitializingContext(ILogger logger, string providerType);

            /// <summary>Logs that RockPaperScissors context initialization for a provider failed.</summary>
            [LoggerMessage(EventId = 4102, Level = LogLevel.Critical, Message = "RockPaperScissors context initialization failed for provider {ProviderType}.")]
            public static partial void ContextInitializationFailed(ILogger logger, Exception exception, string providerType);

            /// <summary>Logs that RockPaperScissors context initialization for a provider succeeded.</summary>
            [LoggerMessage(EventId = 4103, Level = LogLevel.Information, Message = "RockPaperScissors context for provider {ProviderType} initialized successfully.")]
            public static partial void ContextInitialized(ILogger logger, string providerType);
        }
    }
}
