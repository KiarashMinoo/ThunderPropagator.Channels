using System.Linq.Expressions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using ThunderPropagator.Channels.Chat.Endpoints;
using ThunderPropagator.Channels.Chat.Models;
using ThunderPropagator.Channels.Chat.Models.Messages;
using ThunderPropagator.Channels.Chat.Models.Users;
using ThunderPropagator.Channels.Chat.Pipelines.Users.Get;
using ThunderPropagator.Channels.Chat.Pipelines.Users.Search;

namespace ThunderPropagator.UnitTests.Channels.Chat.Endpoints
{
    /// <summary>
    /// Issue #127/#128: covers each MapChatEndpoints handler's OpenAPI/HTTP contract directly against
    /// the handler delegate (the same approach minimal-API route handlers are unit tested with
    /// generally) rather than through a hosted TestServer, since this repo has no ASP.NET Core host
    /// project at all — MapChatEndpoints is a library extension a downstream consumer's own host
    /// calls.
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
            {
                var matches = _users
                    .Where(user => user.UserName.Contains(normalizedTerm, StringComparison.OrdinalIgnoreCase)
                        || user.Name.Contains(normalizedTerm, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(user => user.UserName, StringComparer.Ordinal)
                    .ThenBy(user => user.Id)
                    .ToList();

                return Task.FromResult(new UserSearchPage
                {
                    Users = matches.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                    TotalCount = matches.Count,
                    Page = page,
                    PageSize = pageSize
                });
            }
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

        [Fact]
        public async Task SearchUsersAsync_ForAMatchingTerm_ReturnsOkWithDeterministicPagedResults()
        {
            var (service, context) = CreateService();
            var alice = User.Create("alice", "Alice Anderson");
            var alicia = User.Create("aliciab", "Alicia Brown");
            var bob = User.Create("bob", "Bob Bishop");
            context.Seed(alice);
            context.Seed(alicia);
            context.Seed(bob);

            var result = await ChatChannelEndpoints.SearchUsersAsync(service, "ali", page: 1, pageSize: 10);

            var ok = Assert.IsType<Ok<ChatChannelSearchUsersReceiverPipelineResponseDto>>(result.Result);
            Assert.Equal(2, ok.Value!.TotalCount);
            Assert.Equal(new[] { "alice", "aliciab" }, ok.Value.Users.Select(user => user.UserName).ToArray());
        }

        [Fact]
        public async Task SearchUsersAsync_ExcludesSensitiveFields()
        {
            var (service, context) = CreateService();
            var user = User.Create("alice", "Alice").SetPasswordHash("hashed").SetBirthDate(new DateOnly(1990, 1, 1));
            context.Seed(user);

            var result = await ChatChannelEndpoints.SearchUsersAsync(service, "alice", page: 1, pageSize: 10);

            var ok = Assert.IsType<Ok<ChatChannelSearchUsersReceiverPipelineResponseDto>>(result.Result);
            Assert.Null(typeof(ChatChannelGetUserReceiverPipelineResponseDto).GetProperty(nameof(User.PasswordHash)));
            Assert.Null(typeof(ChatChannelGetUserReceiverPipelineResponseDto).GetProperty(nameof(User.BirthDate)));
            Assert.Single(ok.Value!.Users);
        }

        [Fact]
        public async Task SearchUsersAsync_WhenPageAndPageSizeAreOmitted_UsesTheDefaults()
        {
            var (service, context) = CreateService();
            context.Seed(User.Create("alice", "Alice"));

            var result = await ChatChannelEndpoints.SearchUsersAsync(service, "alice");

            var ok = Assert.IsType<Ok<ChatChannelSearchUsersReceiverPipelineResponseDto>>(result.Result);
            Assert.Equal(1, ok.Value!.Page);
            Assert.Equal(UserService.DefaultPageSize, ok.Value.PageSize);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("a")]
        public async Task SearchUsersAsync_ForATermThatIsTooShort_ReturnsValidationProblem(string? term)
        {
            var (service, _) = CreateService();

            var result = await ChatChannelEndpoints.SearchUsersAsync(service, term, page: 1, pageSize: 10);

            Assert.IsType<ValidationProblem>(result.Result);
        }

        [Fact]
        public async Task SearchUsersAsync_ForAnOutOfRangePage_ReturnsValidationProblem()
        {
            var (service, _) = CreateService();

            var result = await ChatChannelEndpoints.SearchUsersAsync(service, "alice", page: 0, pageSize: 10);

            Assert.IsType<ValidationProblem>(result.Result);
        }

        [Fact]
        public async Task SearchUsersAsync_ForAnOutOfRangePageSize_ReturnsValidationProblem()
        {
            var (service, _) = CreateService();

            var result = await ChatChannelEndpoints.SearchUsersAsync(service, "alice", page: 1, pageSize: UserService.MaxPageSize + 1);

            Assert.IsType<ValidationProblem>(result.Result);
        }
    }
}
