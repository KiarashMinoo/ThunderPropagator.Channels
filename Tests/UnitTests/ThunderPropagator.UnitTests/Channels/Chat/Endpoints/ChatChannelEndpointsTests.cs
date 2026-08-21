using System.Linq.Expressions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using ThunderPropagator.Channels.Chat.Endpoints;
using ThunderPropagator.Channels.Chat.Models;
using ThunderPropagator.Channels.Chat.Models.Messages;
using ThunderPropagator.Channels.Chat.Models.Users;
using ThunderPropagator.Channels.Chat.Pipelines.Users.Get;

namespace ThunderPropagator.UnitTests.Channels.Chat.Endpoints
{
    /// <summary>
    /// Issue #127: covers GetUserByIdAsync's OpenAPI/HTTP contract directly against the handler
    /// delegate (the same approach minimal-API route handlers are unit tested with generally) rather
    /// than through a hosted TestServer, since this repo has no ASP.NET Core host project at all —
    /// MapChatEndpoints is a library extension a downstream consumer's own host calls.
    /// </summary>
    public sealed class ChatChannelEndpointsTests
    {
        private sealed class FakeChatContext : IChatContext
        {
            private readonly List<User> _users = [];

            public void Seed(User user) => _users.Add(user);

            public Task<TEntity?> GetAsync<TEntity>(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default) where TEntity : class
                => throw new NotSupportedException();

            public Task<TEntity?> GetAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class
                => Task.FromResult(_users.OfType<TEntity>().FirstOrDefault(user => ((User)(object)user).Id.Equals(id)));

            public Task<IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default) where TEntity : class
                => throw new NotSupportedException();

            public Task<IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class
                => throw new NotSupportedException();

            public Task<TEntity> CreateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
                => throw new NotSupportedException();

            public Task<TEntity> UpdateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
                => throw new NotSupportedException();

            public Task<bool> DeleteAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class
                => throw new NotSupportedException();

            public Task<IReadOnlyCollection<User>> GetContactsAsync(Guid userId, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public Task<MessageHistoryPage> GetDirectMessageHistoryAsync(Guid userId, Guid otherUserId, int page, int pageSize, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public Task<MessageHistoryPage> GetGroupMessageHistoryAsync(Guid groupId, int page, int pageSize, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public Task<UserSearchPage> SearchUsersAsync(string normalizedTerm, int page, int pageSize, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();
        }

        private static (UserService Service, FakeChatContext Context) CreateService()
        {
            var context = new FakeChatContext();
            return (new UserService(context, new PasswordHasher<User>()), context);
        }

        [Fact]
        public async Task GetUserByIdAsync_ForAnExistingUser_ReturnsOkWithThePublicProjection()
        {
            var (service, context) = CreateService();
            var user = User.Create("alice", "Alice").SetAvatar("avatar.png").SetBio("Hi there.");
            context.Seed(user);

            var result = await ChatChannelEndpoints.GetUserByIdAsync(user.Id.ToString(), service, CancellationToken.None);

            var ok = Assert.IsType<Ok<ChatChannelGetUserReceiverPipelineResponseDto>>(result.Result);
            Assert.Equal(user.Id, ok.Value!.Id);
            Assert.Equal(user.UserName, ok.Value.UserName);
            Assert.Equal(user.Name, ok.Value.Name);
        }

        [Fact]
        public async Task GetUserByIdAsync_ForAnUnknownUser_ReturnsNotFound()
        {
            var (service, _) = CreateService();

            var result = await ChatChannelEndpoints.GetUserByIdAsync(Guid.NewGuid().ToString(), service, CancellationToken.None);

            Assert.IsType<NotFound>(result.Result);
        }

        [Theory]
        [InlineData("not-a-guid")]
        [InlineData("")]
        public async Task GetUserByIdAsync_ForAMalformedId_ReturnsValidationProblem(string userId)
        {
            var (service, _) = CreateService();

            var result = await ChatChannelEndpoints.GetUserByIdAsync(userId, service, CancellationToken.None);

            Assert.IsType<ValidationProblem>(result.Result);
        }

        [Fact]
        public async Task GetUserByIdAsync_ForTheEmptyGuid_ReturnsValidationProblem()
        {
            var (service, _) = CreateService();

            var result = await ChatChannelEndpoints.GetUserByIdAsync(Guid.Empty.ToString(), service, CancellationToken.None);

            Assert.IsType<ValidationProblem>(result.Result);
        }

        [Fact]
        public void MapChatEndpoints_IsPublic()
        {
            var method = typeof(ChatChannelEndpoints).GetMethod(nameof(ChatChannelEndpoints.MapChatEndpoints));

            Assert.NotNull(method);
            Assert.True(method.IsPublic);
        }
    }
}
