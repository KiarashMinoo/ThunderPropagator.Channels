using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ThunderPropagator.Channels.Chat.Models;

namespace ThunderPropagator.UnitTests.Channels.Chat.Models
{
    /// <summary>
    /// Issue #114: ChatContextInitializationHostedService is what AddChatChannel&lt;TChatContext&gt;
    /// registers so BaseChatContext.InitializeAsync() is awaited during host startup, before the host
    /// (and, for ASP.NET Core, Kestrel) starts accepting traffic — an exception from
    /// IHostedService.StartAsync aborts IHost.StartAsync/RunAsync itself. These tests exercise the
    /// hosted service directly against a bare DI container rather than a full generic Host, since all
    /// that matters here is: does it resolve a scoped instance, call InitializeAsync, and translate a
    /// genuine failure into a ChatContextInitializationException carrying provider context, while
    /// letting cancellation through unwrapped.
    ///
    /// Each test uses its own private nested BaseChatContext subclass — the per-type initialization
    /// state BaseChatContext keys by GetType() is static/process-wide, so sharing one type across
    /// these tests would let an earlier test's successful initialization make a later test's
    /// MigrateAsync never run at all.
    /// </summary>
    public sealed class ChatContextInitializationHostedServiceTests
    {
        private abstract class RecordingChatContext(Func<CancellationToken, Task> onMigrate) : BaseChatContext
        {
            protected override Task MigrateAsync(CancellationToken cancellationToken) => onMigrate(cancellationToken);
            protected override Task SeedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

            public override Task<TEntity?> GetAsync<TEntity>(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default) where TEntity : class
                => throw new NotSupportedException();

            public override Task<TEntity?> GetAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class
                => throw new NotSupportedException();

            public override Task<IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default) where TEntity : class
                => throw new NotSupportedException();

            public override Task<IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class
                => throw new NotSupportedException();

            public override Task<TEntity> CreateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
                => throw new NotSupportedException();

            public override Task<TEntity> UpdateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
                => throw new NotSupportedException();

            public override Task<bool> DeleteAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class
                => throw new NotSupportedException();
        }

        private sealed class SuccessContext(Func<CancellationToken, Task> onMigrate) : RecordingChatContext(onMigrate);
        private sealed class FailureContext(Func<CancellationToken, Task> onMigrate) : RecordingChatContext(onMigrate);
        private sealed class CancellationContext(Func<CancellationToken, Task> onMigrate) : RecordingChatContext(onMigrate);

        private static (ServiceProvider Provider, ChatContextInitializationHostedService<TChatContext> HostedService) Build<TChatContext>(
            Func<CancellationToken, Task> onMigrate, Func<Func<CancellationToken, Task>, TChatContext> factory)
            where TChatContext : BaseChatContext
        {
            var services = new ServiceCollection();
            services.AddScoped(_ => factory(onMigrate));
            var provider = services.BuildServiceProvider();

            var hostedService = new ChatContextInitializationHostedService<TChatContext>(
                provider.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<ChatContextInitializationHostedService<TChatContext>>.Instance);

            return (provider, hostedService);
        }

        [Fact]
        public async Task StartAsync_ResolvesAScopedContextAndInitializesIt()
        {
            var migrateCalls = 0;
            var (provider, hostedService) = Build(
                _ => { migrateCalls++; return Task.CompletedTask; },
                onMigrate => new SuccessContext(onMigrate));
            using var _ = provider;

            await hostedService.StartAsync(CancellationToken.None);

            Assert.Equal(1, migrateCalls);
        }

        [Fact]
        public async Task StartAsync_WhenMigrationFails_ThrowsChatContextInitializationExceptionWithProviderContext()
        {
            var failure = new InvalidOperationException("boom");
            var (provider, hostedService) = Build<FailureContext>(
                _ => throw failure,
                onMigrate => new FailureContext(onMigrate));
            using var _ = provider;

            var exception = await Assert.ThrowsAsync<ChatContextInitializationException>(() => hostedService.StartAsync(CancellationToken.None));

            Assert.Equal(typeof(FailureContext), exception.ProviderType);
            Assert.Same(failure, exception.InnerException);
            Assert.Contains(nameof(FailureContext), exception.Message);
        }

        [Fact]
        public async Task StartAsync_WhenCancelled_PropagatesOperationCanceledExceptionUnwrapped()
        {
            var (provider, hostedService) = Build<CancellationContext>(
                cancellationToken => throw new OperationCanceledException(cancellationToken),
                onMigrate => new CancellationContext(onMigrate));
            using var _ = provider;
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            var exception = await Record.ExceptionAsync(() => hostedService.StartAsync(cts.Token));

            Assert.IsNotType<ChatContextInitializationException>(exception);
            Assert.IsAssignableFrom<OperationCanceledException>(exception);
        }
    }
}
