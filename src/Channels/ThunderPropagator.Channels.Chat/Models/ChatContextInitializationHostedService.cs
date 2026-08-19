using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ThunderPropagator.BuildingBlocks.Application;

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
    internal sealed class ChatContextInitializationHostedService<TChatContext>(
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

            logger.LogInformation("Initializing chat context for provider {ProviderType}.", providerName);

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
                logger.LogCritical(exception, "Chat context initialization failed for provider {ProviderType}.", providerName);
                throw new ChatContextInitializationException(typeof(TChatContext), exception);
            }

            activity?.SetStatus(ActivityStatusCode.Ok);
            logger.LogInformation("Chat context for provider {ProviderType} initialized successfully.", providerName);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
