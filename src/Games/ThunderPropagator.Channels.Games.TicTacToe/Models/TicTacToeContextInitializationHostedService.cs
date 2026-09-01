using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ThunderPropagator.BuildingBlocks.Application;

namespace ThunderPropagator.Channels.Games.TicTacToe.Models
{
    // Mirrors ThunderPropagator.Channels.Games.RockPaperScissors's own
    // RockPaperScissorsContextInitializationHostedService — see its own comment for the full
    // reasoning (Scoped context resolved via a fresh scope, awaited during host startup so nothing
    // resolves a game pipeline against an unmigrated/unseeded store).
    internal sealed partial class TicTacToeContextInitializationHostedService<TContext>(
        IServiceScopeFactory scopeFactory,
        ILogger<TicTacToeContextInitializationHostedService<TContext>> logger)
        : IHostedService
        where TContext : BaseTicTacToeContext
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var providerName = typeof(TContext).Name;
            using var activity = Telemetry.StartActivity($"{providerName}_{nameof(StartAsync)}", ActivityKind.Internal)?
                .SetTag("TicTacToeContext.ProviderType", providerName);

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
                throw new TicTacToeContextInitializationException(typeof(TContext), exception);
            }

            activity?.SetStatus(ActivityStatusCode.Ok);
            Log.ContextInitialized(logger, providerName);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        // Issue #39: LoggerMessage-generated methods for this hosted service's log call sites. EventIds
        // 5101-5103 are this file's own block; no cross-file EventId registry exists yet in this repo.
        private static partial class Log
        {
            /// <summary>Logs that TicTacToe context initialization for a provider is starting.</summary>
            [LoggerMessage(EventId = 5101, Level = LogLevel.Information, Message = "Initializing TicTacToe context for provider {ProviderType}.")]
            public static partial void InitializingContext(ILogger logger, string providerType);

            /// <summary>Logs that TicTacToe context initialization for a provider failed.</summary>
            [LoggerMessage(EventId = 5102, Level = LogLevel.Critical, Message = "TicTacToe context initialization failed for provider {ProviderType}.")]
            public static partial void ContextInitializationFailed(ILogger logger, Exception exception, string providerType);

            /// <summary>Logs that TicTacToe context initialization for a provider succeeded.</summary>
            [LoggerMessage(EventId = 5103, Level = LogLevel.Information, Message = "TicTacToe context for provider {ProviderType} initialized successfully.")]
            public static partial void ContextInitialized(ILogger logger, string providerType);
        }
    }
}
