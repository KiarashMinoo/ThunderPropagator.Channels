using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ThunderPropagator.Application.Channels.Subscribers;
using ThunderPropagator.Application.Connections;
using ThunderPropagator.Channels.Chat;
using ThunderPropagator.Channels.Chat.Models;
using ThunderPropagator.Channels.Chat.Models.Messages;
using ThunderPropagator.Channels.Chat.Models.Sessions;
using ThunderPropagator.Channels.Chat.Models.Users;
using ThunderPropagator.Channels.Chat.Pipelines;
using ThunderPropagator.Channels.Chat.Channel;
using ThunderPropagator.Channels.Chat.Configuration;

namespace ThunderPropagator.UnitTests.Channels.Chat
{
    /// <summary>
    /// Issue #106: ChatChannelSendMessageReceiverPipeline used to index LoggedInUsers directly, so
    /// an unauthenticated connection (never logged in, or logged in on a connection that has since
    /// disconnected) triggered an unhandled KeyNotFoundException instead of a controlled Unauthorized
    /// response. ChatChannel.TryGetLoggedInUserIdAsync replaces that indexing — these tests cover its
    /// three states (missing, removed, valid session) directly, since the pipeline's own Invoke
    /// method can't be exercised in isolation (ChannelInfo's constructor is internal to a
    /// closed-source assembly, the same limitation noted for Notifications' receive pipelines).
    /// Issue #109 generalized the pipeline-specific unauthorized exception this originally covered
    /// into the shared ChatChannelUnauthorizedException used by every authenticated pipeline; see
    /// ChatChannelPipelineAuthenticationTests for the guard's cross-pipeline enforcement.
    ///
    /// Issue #46: session state moved from ChatChannel's own node-local LoggedInUsers dictionary to
    /// the persisted, cluster-wide ChatUserSessionService — see that class's own doc comment. These
    /// tests exercise it against a minimal in-memory IChatContext fake, mirroring
    /// UserServiceAuthenticationTests' own FakeChatContext rather than depending on the separate
    /// InMemory provider package. ChatChannel.TryGetLoggedInUserIdAsync/disconnect cleanup create a
    /// fresh DI scope per call (see their own comments), so CreateChannel below builds a real,
    /// minimal ServiceProvider rather than an NSubstitute-faked one.
    /// </summary>
    public sealed class ChatChannelAuthenticationTests
    {
        private sealed class FakeChatContext : IChatContext
        {
            private readonly List<ChatUserSession> _sessions = [];

            public Task<TEntity?> GetAsync<TEntity>(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default) where TEntity : class
                => Task.FromResult(_sessions.OfType<TEntity>().AsQueryable().FirstOrDefault(expression));

            public Task<TEntity?> GetAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class
                => throw new NotSupportedException();

            public Task<IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default) where TEntity : class
                => throw new NotSupportedException();

            public Task<IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class
                => Task.FromResult<IReadOnlyCollection<TEntity>>(_sessions.OfType<TEntity>().ToList());

            public Task<TEntity> CreateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
            {
                _sessions.Add((ChatUserSession)(object)entity!);
                return Task.FromResult(entity);
            }

            public Task<TEntity> UpdateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
                => throw new NotSupportedException();

            public Task<bool> DeleteAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class
            {
                var removed = _sessions.RemoveAll(session => Equals(session.Id, id)) > 0;
                return Task.FromResult(removed);
            }

            public Task<IReadOnlyCollection<User>> GetContactsAsync(Guid userId, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public Task<MessageHistoryPage> GetDirectMessageHistoryAsync(Guid userId, Guid otherUserId, int page, int pageSize, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public Task<MessageHistoryPage> GetGroupMessageHistoryAsync(Guid groupId, int page, int pageSize, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public Task<UserSearchPage> SearchUsersAsync(string normalizedTerm, int page, int pageSize, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();
        }

        private static ChatChannel CreateChannel()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IChatContext>(new FakeChatContext());
            services.AddScoped<ChatUserSessionService>();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddSingleton(new ChatChannelConfiguration());
            services.AddSingleton(Substitute.For<IHostApplicationLifetime>());

            var channel = new ChatChannel(services.BuildServiceProvider());
            channel.Initialize(CancellationToken.None);

            return channel;
        }

        private static Subscription CreateSubscription(ChatChannel channel, string connectionId)
        {
            var connectionInfo = Substitute.For<IConnectionInfo>();
            connectionInfo.ConnectionId.Returns(connectionId);

            return new Subscription(connectionInfo, channel, "request-1", "subscription-1", [], new Dictionary<string, ThunderPropagator.Application.Channels.ChannelProgramsDescriptors.ChannelProgramsDescriptor>());
        }

        private static void InvokeOnSubscriptionRemoved(ChatChannel channel, Subscription subscription)
        {
            var method = typeof(ChatChannel).GetMethod("OnSubscriptionRemoved", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new MissingMethodException(typeof(ChatChannel).FullName, "OnSubscriptionRemoved");
            method.Invoke(channel, [subscription]);
        }

        /// <summary>
        /// Disconnect cleanup (<see cref="ChatChannel.OnSubscriptionRemoved"/>) is fire-and-forget —
        /// see its own comment — so tests that need to observe its effect await the private async
        /// method it kicks off directly, rather than polling for eventual consistency.
        /// </summary>
        private static async Task InvokeCleanUpSessionOnDisconnectAsync(ChatChannel channel, string connectionId)
        {
            var method = typeof(ChatChannel).GetMethod("CleanUpSessionOnDisconnectAsync", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new MissingMethodException(typeof(ChatChannel).FullName, "CleanUpSessionOnDisconnectAsync");
            await (Task)method.Invoke(channel, [connectionId, CancellationToken.None])!;
        }

        [Fact]
        public async Task TryGetLoggedInUserIdAsync_ForAConnectionThatNeverLoggedIn_ReturnsNull()
        {
            var channel = CreateChannel();

            var userId = await channel.TryGetLoggedInUserIdAsync("never-logged-in", CancellationToken.None);

            Assert.Null(userId);
        }

        [Fact]
        public async Task TryGetLoggedInUserIdAsync_ForALoggedInConnection_ReturnsTheUserId()
        {
            var channel = CreateChannel();
            var expectedUserId = Guid.NewGuid();
            await LogInAsync(channel, "connection-1", expectedUserId);

            var userId = await channel.TryGetLoggedInUserIdAsync("connection-1", CancellationToken.None);

            Assert.Equal(expectedUserId, userId);
        }

        [Fact]
        public async Task TryGetLoggedInUserIdAsync_ForARemovedSession_ReturnsNull()
        {
            var channel = CreateChannel();
            await LogInAsync(channel, "connection-1", Guid.NewGuid());

            await InvokeCleanUpSessionOnDisconnectAsync(channel, "connection-1");

            var userId = await channel.TryGetLoggedInUserIdAsync("connection-1", CancellationToken.None);
            Assert.Null(userId);
        }

        // Issue #121: contract coverage for the persisted logout state transition
        // ChatChannelLogoutReceiverPipeline relies on (ChatUserSessionService.LogOutAsync), tested
        // via the same disconnect-cleanup path for the reason above (the pipeline's own Invoke can't
        // be exercised in isolation).
        [Fact]
        public async Task DisconnectCleanup_ForAConnectionThatNeverLoggedIn_DoesNotThrow()
        {
            var channel = CreateChannel();

            var exception = await Record.ExceptionAsync(() => InvokeCleanUpSessionOnDisconnectAsync(channel, "never-logged-in"));

            Assert.Null(exception);
        }

        [Fact]
        public async Task DisconnectCleanup_ForALoggedInConnection_RemovesTheSession()
        {
            var channel = CreateChannel();
            await LogInAsync(channel, "connection-1", Guid.NewGuid());

            await InvokeCleanUpSessionOnDisconnectAsync(channel, "connection-1");

            Assert.Null(await channel.TryGetLoggedInUserIdAsync("connection-1", CancellationToken.None));
        }

        [Fact]
        public async Task DisconnectCleanup_CalledTwiceForTheSameConnection_DoesNotThrow()
        {
            var channel = CreateChannel();
            await LogInAsync(channel, "connection-1", Guid.NewGuid());
            await InvokeCleanUpSessionOnDisconnectAsync(channel, "connection-1");

            var exception = await Record.ExceptionAsync(() => InvokeCleanUpSessionOnDisconnectAsync(channel, "connection-1"));

            Assert.Null(exception);
        }

        [Fact]
        public void OnSubscriptionRemoved_DoesNotThrowSynchronously()
        {
            // OnSubscriptionRemoved is a synchronous, non-awaitable override (see its own comment) —
            // this confirms kicking off the fire-and-forget cleanup itself never throws inline,
            // regardless of whether a session exists for the connection.
            var channel = CreateChannel();
            var subscription = CreateSubscription(channel, "connection-1");

            var exception = Record.Exception(() => InvokeOnSubscriptionRemoved(channel, subscription));

            Assert.Null(exception);
        }

        [Fact]
        public void ChatChannelUnauthorizedException_IsException()
        {
            var type = typeof(ChatChannelUnauthorizedException);
            Assert.True(typeof(Exception).IsAssignableFrom(type));
        }

        [Fact]
        public void ChatChannelUnauthorizedException_IsInternal()
        {
            var type = typeof(ChatChannelUnauthorizedException);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void ChatChannelUnauthorizedException_MessageCarriesNoSessionDetails()
        {
            var exception = new ChatChannelUnauthorizedException();

            Assert.DoesNotContain("connection-1", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(Guid.Empty.ToString(), exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// No public/internal seam on ChatChannel itself creates a session (that's
        /// ChatChannelLoginReceiverPipeline's job, calling ChatUserSessionService directly) — tests
        /// reach the same scoped ChatUserSessionService ChatChannel itself uses via reflection on its
        /// private _scopeFactory field, so a session set up here is visible through
        /// TryGetLoggedInUserIdAsync exactly as it would be in production.
        /// </summary>
        private static async Task LogInAsync(ChatChannel channel, string connectionId, Guid userId)
        {
            var scopeFactoryField = typeof(ChatChannel).GetField("_scopeFactory", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new MissingFieldException(typeof(ChatChannel).FullName, "_scopeFactory");
            var scopeFactory = (IServiceScopeFactory)scopeFactoryField.GetValue(channel)!;

            using var scope = scopeFactory.CreateScope();
            var sessionService = scope.ServiceProvider.GetRequiredService<ChatUserSessionService>();
            await sessionService.LogInAsync(connectionId, userId);
        }
    }
}
