using System.Linq.Expressions;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ThunderPropagator.Channels.Chat;
using ThunderPropagator.Channels.Chat.Endpoints;
using ThunderPropagator.Channels.Chat.Models;
using ThunderPropagator.Channels.Chat.Models.Groups;
using ThunderPropagator.Channels.Chat.Models.Messages;
using ThunderPropagator.Channels.Chat.Models.Users;
using ThunderPropagator.Channels.Chat.Pipelines.Messages.History;
using ThunderPropagator.Channels.Chat.Pipelines.Users.Get;
using ThunderPropagator.Channels.Chat.Pipelines.Users.Search;

namespace ThunderPropagator.UnitTests.Channels.Chat.Endpoints
{
    /// <summary>
    /// Issue #127/#128/#129: covers each MapChatEndpoints handler's OpenAPI/HTTP contract directly
    /// against the handler delegate (the same approach minimal-API route handlers are unit tested
    /// with generally) rather than through a hosted TestServer, since this repo has no ASP.NET Core
    /// host project at all — MapChatEndpoints is a library extension a downstream consumer's own
    /// host calls.
    /// </summary>
    public sealed class ChatChannelEndpointsTests
    {
        private sealed class FakeChatContext : IChatContext
        {
            private readonly List<User> _users = [];
            private readonly List<Message> _messages = [];
            private readonly List<Group> _groups = [];

            public void Seed(User user) => _users.Add(user);
            public void Seed(Message message) => _messages.Add(message);
            public void Seed(Group group) => _groups.Add(group);

            public IReadOnlyCollection<Message> Messages => _messages;

            public Task<TEntity?> GetAsync<TEntity>(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default) where TEntity : class
                => throw new NotSupportedException();

            public Task<TEntity?> GetAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class
            {
                if (typeof(TEntity) == typeof(Group))
                    return Task.FromResult(_groups.OfType<TEntity>().FirstOrDefault(group => ((Group)(object)group).Id.Equals(id)));

                return Task.FromResult(_users.OfType<TEntity>().FirstOrDefault(user => ((User)(object)user).Id.Equals(id)));
            }

            public Task<IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default) where TEntity : class
            {
                if (typeof(TEntity) == typeof(Group))
                {
                    var predicate = ((Expression<Func<Group, bool>>)(object)expression).Compile();
                    IReadOnlyCollection<TEntity> matches = _groups.Where(predicate).Cast<TEntity>().ToList();
                    return Task.FromResult(matches);
                }

                throw new NotSupportedException();
            }

            public Task<IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class
                => throw new NotSupportedException();

            public Task<TEntity> CreateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
            {
                if (entity is Message message)
                {
                    _messages.Add(message);
                    return Task.FromResult(entity);
                }

                throw new NotSupportedException();
            }

            public Task<TEntity> UpdateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
                => throw new NotSupportedException();

            public Task<bool> DeleteAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class
                => throw new NotSupportedException();

            public Task<IReadOnlyCollection<User>> GetContactsAsync(Guid userId, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public Task<MessageHistoryPage> GetDirectMessageHistoryAsync(Guid userId, Guid otherUserId, int page, int pageSize, CancellationToken cancellationToken = default)
            {
                var matches = _messages
                    .Where(message => message.GroupId is null
                        && ((message.SenderId == userId && message.ReceiverId == otherUserId)
                            || (message.SenderId == otherUserId && message.ReceiverId == userId)))
                    .OrderByDescending(message => message.Created)
                    .ToList();

                return Task.FromResult(new MessageHistoryPage
                {
                    Messages = matches.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                    TotalCount = matches.Count,
                    Page = page,
                    PageSize = pageSize
                });
            }

            public Task<MessageHistoryPage> GetGroupMessageHistoryAsync(Guid groupId, int page, int pageSize, CancellationToken cancellationToken = default)
            {
                var matches = _messages
                    .Where(message => message.GroupId == groupId)
                    .OrderByDescending(message => message.Created)
                    .ToList();

                return Task.FromResult(new MessageHistoryPage
                {
                    Messages = matches.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                    TotalCount = matches.Count,
                    Page = page,
                    PageSize = pageSize
                });
            }

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

        private static (MessageService Service, FakeChatContext Context) CreateMessageService()
        {
            var context = new FakeChatContext();
            return (new MessageService(context, new ChatChannelConfiguration()), context);
        }

        private static (GroupService GroupService, UserService UserService, FakeChatContext Context) CreateGroupAndUserServices()
        {
            var context = new FakeChatContext();
            return (new GroupService(context), new UserService(context, new PasswordHasher<User>()), context);
        }

        // Mirrors ChatChannelAuthenticationTests.CreateChannel — constructing the real ChatChannel
        // with a faked IServiceProvider rather than substituting ChatChannel itself, since it's
        // sealed in Release (the configuration these tests run under) and NSubstitute can't
        // substitute a sealed class.
        private static ChatChannel CreateChatChannel()
        {
            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IHostApplicationLifetime)).Returns(Substitute.For<IHostApplicationLifetime>());
            serviceProvider.GetService(typeof(ILoggerFactory)).Returns(NullLoggerFactory.Instance);
            serviceProvider.GetService(typeof(ChatChannelConfiguration)).Returns(new ChatChannelConfiguration());

            var channel = new ChatChannel(serviceProvider);
            channel.Initialize(CancellationToken.None);

            return channel;
        }

        private static (MessageService MessageService, ChatChannel Channel, FakeChatContext Context) CreateMessageServiceAndChannel()
        {
            var context = new FakeChatContext();
            return (new MessageService(context, new ChatChannelConfiguration()), CreateChatChannel(), context);
        }

        private static ClaimsPrincipal CreatePrincipal(Guid? userId = null)
        {
            var claims = userId is null
                ? []
                : new[] { new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()) };
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
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

        [Fact]
        public async Task GetDirectMessageHistoryAsync_ReturnsOnlyMessagesBetweenTheCallerAndTheOtherParticipant()
        {
            var (service, context) = CreateMessageService();
            var caller = Guid.NewGuid();
            var other = Guid.NewGuid();
            var stranger = Guid.NewGuid();
            context.Seed(Message.Create(caller, other, "hi"));
            context.Seed(Message.Create(other, caller, "hello back"));
            context.Seed(Message.Create(caller, stranger, "unrelated"));

            var result = await ChatChannelEndpoints.GetDirectMessageHistoryAsync(service, CreatePrincipal(caller), other.ToString());

            var ok = Assert.IsType<Ok<ChatChannelGetMessageHistoryReceiverPipelineResponseDto>>(result.Result);
            Assert.Equal(2, ok.Value!.TotalCount);
            Assert.All(ok.Value.Messages, message => Assert.True(
                (message.SenderId == caller && message.ReceiverId == other)
                || (message.SenderId == other && message.ReceiverId == caller)));
        }

        [Fact]
        public async Task GetDirectMessageHistoryAsync_ForAConversationWithNoMessages_ReturnsOkWithZeroTotalCount()
        {
            var (service, _) = CreateMessageService();

            var result = await ChatChannelEndpoints.GetDirectMessageHistoryAsync(service, CreatePrincipal(Guid.NewGuid()), Guid.NewGuid().ToString());

            var ok = Assert.IsType<Ok<ChatChannelGetMessageHistoryReceiverPipelineResponseDto>>(result.Result);
            Assert.Equal(0, ok.Value!.TotalCount);
            Assert.Empty(ok.Value.Messages);
        }

        [Fact]
        public async Task GetDirectMessageHistoryAsync_WithoutAResolvableCallerIdentity_ReturnsUnauthorized()
        {
            var (service, _) = CreateMessageService();

            var result = await ChatChannelEndpoints.GetDirectMessageHistoryAsync(service, CreatePrincipal(), Guid.NewGuid().ToString());

            Assert.IsType<UnauthorizedHttpResult>(result.Result);
        }

        [Theory]
        [InlineData("not-a-guid")]
        [InlineData("")]
        [InlineData(null)]
        public async Task GetDirectMessageHistoryAsync_ForAMalformedWithParameter_ReturnsValidationProblem(string? with)
        {
            var (service, _) = CreateMessageService();

            var result = await ChatChannelEndpoints.GetDirectMessageHistoryAsync(service, CreatePrincipal(Guid.NewGuid()), with);

            Assert.IsType<ValidationProblem>(result.Result);
        }

        [Fact]
        public async Task GetDirectMessageHistoryAsync_ForTheEmptyGuidWithParameter_ReturnsValidationProblem()
        {
            var (service, _) = CreateMessageService();

            var result = await ChatChannelEndpoints.GetDirectMessageHistoryAsync(service, CreatePrincipal(Guid.NewGuid()), Guid.Empty.ToString());

            Assert.IsType<ValidationProblem>(result.Result);
        }

        [Fact]
        public async Task GetDirectMessageHistoryAsync_ForAnOutOfRangePage_ReturnsValidationProblem()
        {
            var (service, _) = CreateMessageService();

            var result = await ChatChannelEndpoints.GetDirectMessageHistoryAsync(service, CreatePrincipal(Guid.NewGuid()), Guid.NewGuid().ToString(), page: 0);

            Assert.IsType<ValidationProblem>(result.Result);
        }

        [Fact]
        public async Task GetDirectMessageHistoryAsync_ForAnOutOfRangeSize_ReturnsValidationProblem()
        {
            var (service, _) = CreateMessageService();

            var result = await ChatChannelEndpoints.GetDirectMessageHistoryAsync(service, CreatePrincipal(Guid.NewGuid()), Guid.NewGuid().ToString(), size: MessageService.MaxPageSize + 1);

            Assert.IsType<ValidationProblem>(result.Result);
        }

        [Fact]
        public async Task GetGroupMessageHistoryAsync_ForACurrentMember_ReturnsOkWithTheGroupsMessages()
        {
            var (service, context) = CreateMessageService();
            var member = Guid.NewGuid();
            var group = Group.Create("Team", member).AddUser(member);
            context.Seed(group);
            context.Seed(Message.Create(member, Guid.NewGuid(), group.Id, "hello team"));

            var result = await ChatChannelEndpoints.GetGroupMessageHistoryAsync(service, CreatePrincipal(member), group.Id.ToString());

            var ok = Assert.IsType<Ok<ChatChannelGetMessageHistoryReceiverPipelineResponseDto>>(result.Result);
            Assert.Equal(1, ok.Value!.TotalCount);
        }

        [Fact]
        public async Task GetGroupMessageHistoryAsync_ForANonMember_ReturnsForbidden()
        {
            var (service, context) = CreateMessageService();
            var member = Guid.NewGuid();
            var nonMember = Guid.NewGuid();
            var group = Group.Create("Team", member).AddUser(member);
            context.Seed(group);

            var result = await ChatChannelEndpoints.GetGroupMessageHistoryAsync(service, CreatePrincipal(nonMember), group.Id.ToString());

            Assert.IsType<ForbidHttpResult>(result.Result);
        }

        [Fact]
        public async Task GetGroupMessageHistoryAsync_ForAFormerMember_ReturnsForbidden()
        {
            var (service, context) = CreateMessageService();
            var formerMember = Guid.NewGuid();
            var group = Group.Create("Team", formerMember).AddUser(formerMember).RemoveUser(formerMember);
            context.Seed(group);

            var result = await ChatChannelEndpoints.GetGroupMessageHistoryAsync(service, CreatePrincipal(formerMember), group.Id.ToString());

            Assert.IsType<ForbidHttpResult>(result.Result);
        }

        [Fact]
        public async Task GetGroupMessageHistoryAsync_ForAMissingGroup_ReturnsNotFound()
        {
            var (service, _) = CreateMessageService();

            var result = await ChatChannelEndpoints.GetGroupMessageHistoryAsync(service, CreatePrincipal(Guid.NewGuid()), Guid.NewGuid().ToString());

            Assert.IsType<NotFound>(result.Result);
        }

        [Fact]
        public async Task GetGroupMessageHistoryAsync_WithoutAResolvableCallerIdentity_ReturnsUnauthorized()
        {
            var (service, _) = CreateMessageService();

            var result = await ChatChannelEndpoints.GetGroupMessageHistoryAsync(service, CreatePrincipal(), Guid.NewGuid().ToString());

            Assert.IsType<UnauthorizedHttpResult>(result.Result);
        }

        [Theory]
        [InlineData("not-a-guid")]
        [InlineData("")]
        public async Task GetGroupMessageHistoryAsync_ForAMalformedGroupId_ReturnsValidationProblem(string groupId)
        {
            var (service, _) = CreateMessageService();

            var result = await ChatChannelEndpoints.GetGroupMessageHistoryAsync(service, CreatePrincipal(Guid.NewGuid()), groupId);

            Assert.IsType<ValidationProblem>(result.Result);
        }

        [Fact]
        public async Task GetGroupMessageHistoryAsync_ForAnOutOfRangePage_ReturnsValidationProblem()
        {
            var (service, context) = CreateMessageService();
            var member = Guid.NewGuid();
            var group = Group.Create("Team", member).AddUser(member);
            context.Seed(group);

            var result = await ChatChannelEndpoints.GetGroupMessageHistoryAsync(service, CreatePrincipal(member), group.Id.ToString(), page: 0);

            Assert.IsType<ValidationProblem>(result.Result);
        }

        [Fact]
        public async Task GetGroupMessageHistoryAsync_ForAnOutOfRangeSize_ReturnsValidationProblem()
        {
            var (service, context) = CreateMessageService();
            var member = Guid.NewGuid();
            var group = Group.Create("Team", member).AddUser(member);
            context.Seed(group);

            var result = await ChatChannelEndpoints.GetGroupMessageHistoryAsync(service, CreatePrincipal(member), group.Id.ToString(), size: MessageService.MaxPageSize + 1);

            Assert.IsType<ValidationProblem>(result.Result);
        }

        [Fact]
        public async Task GetGroupsAsync_ReturnsOnlyGroupsTheCallerIsAMemberOf()
        {
            var (service, context) = CreateService();
            var caller = Guid.NewGuid();
            var stranger = Guid.NewGuid();
            var myGroup = Group.Create("Mine", caller).AddUser(caller);
            var otherGroup = Group.Create("Not Mine", stranger).AddUser(stranger);
            context.Seed(myGroup);
            context.Seed(otherGroup);

            var result = await ChatChannelEndpoints.GetGroupsAsync(service, CreatePrincipal(caller));

            var ok = Assert.IsType<Ok<ChatChannelGetGroupsResponseDto>>(result.Result);
            Assert.Equal(1, ok.Value!.TotalCount);
            Assert.Equal(myGroup.Id, ok.Value.Groups.Single().Id);
        }

        [Fact]
        public async Task GetGroupsAsync_ForDuplicateMembershipRows_DoesNotDuplicateTheGroup()
        {
            var (service, context) = CreateService();
            var caller = Guid.NewGuid();
            var group = Group.Create("Team", caller).AddUser(caller).AddUser(caller);
            context.Seed(group);

            var result = await ChatChannelEndpoints.GetGroupsAsync(service, CreatePrincipal(caller));

            var ok = Assert.IsType<Ok<ChatChannelGetGroupsResponseDto>>(result.Result);
            Assert.Equal(1, ok.Value!.TotalCount);
            Assert.Single(ok.Value.Groups);
        }

        [Fact]
        public void GetGroupsAsync_ExcludesSensitiveMemberData()
        {
            var type = typeof(ChatChannelGroupSummaryDto);

            Assert.Null(type.GetProperty("GroupUsers"));
            Assert.Null(type.GetProperty(nameof(User.PasswordHash)));
        }

        [Fact]
        public async Task GetGroupsAsync_WhenTheCallerHasNoGroups_ReturnsOkWithZeroTotalCount()
        {
            var (service, _) = CreateService();

            var result = await ChatChannelEndpoints.GetGroupsAsync(service, CreatePrincipal(Guid.NewGuid()));

            var ok = Assert.IsType<Ok<ChatChannelGetGroupsResponseDto>>(result.Result);
            Assert.Equal(0, ok.Value!.TotalCount);
            Assert.Empty(ok.Value.Groups);
        }

        [Fact]
        public async Task GetGroupsAsync_PaginatesDeterministically()
        {
            var (service, context) = CreateService();
            var caller = Guid.NewGuid();
            var alpha = Group.Create("Alpha", caller).AddUser(caller);
            var bravo = Group.Create("Bravo", caller).AddUser(caller);
            var charlie = Group.Create("Charlie", caller).AddUser(caller);
            context.Seed(alpha);
            context.Seed(bravo);
            context.Seed(charlie);

            var result = await ChatChannelEndpoints.GetGroupsAsync(service, CreatePrincipal(caller), page: 2, pageSize: 1);

            var ok = Assert.IsType<Ok<ChatChannelGetGroupsResponseDto>>(result.Result);
            Assert.Equal(3, ok.Value!.TotalCount);
            Assert.Equal("Bravo", ok.Value.Groups.Single().Name);
        }

        [Fact]
        public async Task GetGroupsAsync_WithoutAResolvableCallerIdentity_ReturnsUnauthorized()
        {
            var (service, _) = CreateService();

            var result = await ChatChannelEndpoints.GetGroupsAsync(service, CreatePrincipal());

            Assert.IsType<UnauthorizedHttpResult>(result.Result);
        }

        [Fact]
        public async Task GetGroupsAsync_ForAnOutOfRangePage_ReturnsValidationProblem()
        {
            var (service, _) = CreateService();

            var result = await ChatChannelEndpoints.GetGroupsAsync(service, CreatePrincipal(Guid.NewGuid()), page: 0);

            Assert.IsType<ValidationProblem>(result.Result);
        }

        [Fact]
        public async Task GetGroupsAsync_ForAnOutOfRangePageSize_ReturnsValidationProblem()
        {
            var (service, _) = CreateService();

            var result = await ChatChannelEndpoints.GetGroupsAsync(service, CreatePrincipal(Guid.NewGuid()), pageSize: UserService.MaxPageSize + 1);

            Assert.IsType<ValidationProblem>(result.Result);
        }

        [Fact]
        public async Task GetGroupDetailsAsync_ForAMember_ReturnsOkWithDetailsAndMembers()
        {
            var (groupService, userService, context) = CreateGroupAndUserServices();
            var member = User.Create("alice", "Alice");
            context.Seed(member);
            var group = Group.Create("Team", member.Id).AddUser(member.Id).SetGroupIcon("icon.png");
            context.Seed(group);

            var result = await ChatChannelEndpoints.GetGroupDetailsAsync(groupService, userService, CreatePrincipal(member.Id), group.Id.ToString());

            var ok = Assert.IsType<Ok<ChatChannelGroupDetailsResponseDto>>(result.Result);
            Assert.Equal(group.Id, ok.Value!.Id);
            Assert.Equal("Team", ok.Value.Name);
            Assert.Equal("icon.png", ok.Value.GroupIcon);
            Assert.Equal(1, ok.Value.MemberCount);
            Assert.Equal(member.Id, ok.Value.Members.Single().Id);
        }

        [Fact]
        public async Task GetGroupDetailsAsync_ForTheAdministratorWhoIsAlsoAMember_ReturnsOkWithDetails()
        {
            var (groupService, userService, context) = CreateGroupAndUserServices();
            var admin = User.Create("admin", "Admin");
            context.Seed(admin);
            var group = Group.Create("Team", admin.Id).AddUser(admin.Id);
            context.Seed(group);

            var result = await ChatChannelEndpoints.GetGroupDetailsAsync(groupService, userService, CreatePrincipal(admin.Id), group.Id.ToString());

            var ok = Assert.IsType<Ok<ChatChannelGroupDetailsResponseDto>>(result.Result);
            Assert.Equal(admin.Id, ok.Value!.CreatedByUserId);
        }

        [Fact]
        public async Task GetGroupDetailsAsync_ForAnOutsider_ReturnsForbidden()
        {
            var (groupService, userService, context) = CreateGroupAndUserServices();
            var member = Guid.NewGuid();
            var outsider = Guid.NewGuid();
            var group = Group.Create("Team", member).AddUser(member);
            context.Seed(group);

            var result = await ChatChannelEndpoints.GetGroupDetailsAsync(groupService, userService, CreatePrincipal(outsider), group.Id.ToString());

            Assert.IsType<ForbidHttpResult>(result.Result);
        }

        [Fact]
        public async Task GetGroupDetailsAsync_ForAMissingGroup_ReturnsNotFound()
        {
            var (groupService, userService, _) = CreateGroupAndUserServices();

            var result = await ChatChannelEndpoints.GetGroupDetailsAsync(groupService, userService, CreatePrincipal(Guid.NewGuid()), Guid.NewGuid().ToString());

            Assert.IsType<NotFound>(result.Result);
        }

        [Fact]
        public async Task GetGroupDetailsAsync_ForALargeMembership_BoundsTheReturnedMembersByPageSize()
        {
            var (groupService, userService, context) = CreateGroupAndUserServices();
            var caller = User.Create("caller", "Caller");
            context.Seed(caller);
            var group = Group.Create("Big Team", caller.Id).AddUser(caller.Id);
            for (var i = 0; i < 149; i++)
            {
                var member = User.Create($"member{i}", $"Member {i}");
                context.Seed(member);
                group.AddUser(member.Id);
            }
            context.Seed(group);

            var result = await ChatChannelEndpoints.GetGroupDetailsAsync(groupService, userService, CreatePrincipal(caller.Id), group.Id.ToString(), page: 1, pageSize: 50);

            var ok = Assert.IsType<Ok<ChatChannelGroupDetailsResponseDto>>(result.Result);
            Assert.Equal(150, ok.Value!.MemberCount);
            Assert.Equal(50, ok.Value.Members.Count);
            Assert.Equal(1, ok.Value.MembersPage);
            Assert.Equal(50, ok.Value.MembersPageSize);
        }

        [Fact]
        public async Task GetGroupDetailsAsync_WithoutAResolvableCallerIdentity_ReturnsUnauthorized()
        {
            var (groupService, userService, _) = CreateGroupAndUserServices();

            var result = await ChatChannelEndpoints.GetGroupDetailsAsync(groupService, userService, CreatePrincipal(), Guid.NewGuid().ToString());

            Assert.IsType<UnauthorizedHttpResult>(result.Result);
        }

        [Theory]
        [InlineData("not-a-guid")]
        [InlineData("")]
        public async Task GetGroupDetailsAsync_ForAMalformedGroupId_ReturnsValidationProblem(string groupId)
        {
            var (groupService, userService, _) = CreateGroupAndUserServices();

            var result = await ChatChannelEndpoints.GetGroupDetailsAsync(groupService, userService, CreatePrincipal(Guid.NewGuid()), groupId);

            Assert.IsType<ValidationProblem>(result.Result);
        }

        [Fact]
        public async Task GetGroupDetailsAsync_ForAnOutOfRangeMembersPage_ReturnsValidationProblem()
        {
            var (groupService, userService, context) = CreateGroupAndUserServices();
            var member = Guid.NewGuid();
            var group = Group.Create("Team", member).AddUser(member);
            context.Seed(group);

            var result = await ChatChannelEndpoints.GetGroupDetailsAsync(groupService, userService, CreatePrincipal(member), group.Id.ToString(), page: 0);

            Assert.IsType<ValidationProblem>(result.Result);
        }

        [Fact]
        public async Task GetGroupDetailsAsync_ForAnOutOfRangeMembersPageSize_ReturnsValidationProblem()
        {
            var (groupService, userService, context) = CreateGroupAndUserServices();
            var member = Guid.NewGuid();
            var group = Group.Create("Team", member).AddUser(member);
            context.Seed(group);

            var result = await ChatChannelEndpoints.GetGroupDetailsAsync(groupService, userService, CreatePrincipal(member), group.Id.ToString(), pageSize: UserService.MaxPageSize + 1);

            Assert.IsType<ValidationProblem>(result.Result);
        }

        [Fact]
        public async Task SendMessageAsync_ForADirectTarget_PersistsAndReturnsCreated()
        {
            var (messageService, channel, context) = CreateMessageServiceAndChannel();
            var sender = Guid.NewGuid();
            var receiver = Guid.NewGuid();
            var request = new ChatChannelSendMessageRequestDto { ReceiverId = receiver, Body = "hi there" };

            var result = await ChatChannelEndpoints.SendMessageAsync(messageService, channel, CreatePrincipal(sender), request);

            var created = Assert.IsType<Created<ChatChannelSentMessageResponseDto>>(result.Result);
            Assert.Equal(sender, created.Value!.SenderId);
            Assert.Equal(receiver, created.Value.ReceiverId);
            Assert.Null(created.Value.GroupId);
            Assert.Equal("hi there", created.Value.Body);
            Assert.Single(created.Value.MessageIds);
            Assert.Single(context.Messages);
        }

        [Fact]
        public async Task SendMessageAsync_ForAGroupTarget_FansOutToEveryMemberAndReturnsCreated()
        {
            var (messageService, channel, context) = CreateMessageServiceAndChannel();
            var sender = Guid.NewGuid();
            var memberA = Guid.NewGuid();
            var memberB = Guid.NewGuid();
            var group = Group.Create("Team", sender).AddUser(sender).AddUser(memberA).AddUser(memberB);
            context.Seed(group);
            var request = new ChatChannelSendMessageRequestDto { GroupId = group.Id, Body = "hi team" };

            var result = await ChatChannelEndpoints.SendMessageAsync(messageService, channel, CreatePrincipal(sender), request);

            var created = Assert.IsType<Created<ChatChannelSentMessageResponseDto>>(result.Result);
            Assert.Equal(group.Id, created.Value!.GroupId);
            Assert.Null(created.Value.ReceiverId);
            Assert.Equal(3, created.Value.MessageIds.Count);
            Assert.Equal(3, context.Messages.Count(message => message.GroupId == group.Id));
        }

        [Fact]
        public async Task SendMessageAsync_SendingTheSameContentTwice_CreatesTwoSeparateMessages()
        {
            // The AC's idempotency policy: sending is not deduplicated — two identical requests
            // produce two distinct messages, not one.
            var (messageService, channel, context) = CreateMessageServiceAndChannel();
            var sender = Guid.NewGuid();
            var receiver = Guid.NewGuid();
            var request = new ChatChannelSendMessageRequestDto { ReceiverId = receiver, Body = "hi there" };

            var firstResult = await ChatChannelEndpoints.SendMessageAsync(messageService, channel, CreatePrincipal(sender), request);
            var secondResult = await ChatChannelEndpoints.SendMessageAsync(messageService, channel, CreatePrincipal(sender), request);

            var firstId = Assert.IsType<Created<ChatChannelSentMessageResponseDto>>(firstResult.Result).Value!.MessageIds.Single();
            var secondId = Assert.IsType<Created<ChatChannelSentMessageResponseDto>>(secondResult.Result).Value!.MessageIds.Single();
            Assert.NotEqual(firstId, secondId);
            Assert.Equal(2, context.Messages.Count);
        }

        [Fact]
        public async Task SendMessageAsync_ForAMissingGroup_ReturnsNotFound()
        {
            var (messageService, channel, _) = CreateMessageServiceAndChannel();
            var request = new ChatChannelSendMessageRequestDto { GroupId = Guid.NewGuid(), Body = "hi team" };

            var result = await ChatChannelEndpoints.SendMessageAsync(messageService, channel, CreatePrincipal(Guid.NewGuid()), request);

            Assert.IsType<NotFound>(result.Result);
        }

        [Fact]
        public async Task SendMessageAsync_WithoutAResolvableCallerIdentity_ReturnsUnauthorized()
        {
            var (messageService, channel, _) = CreateMessageServiceAndChannel();
            var request = new ChatChannelSendMessageRequestDto { ReceiverId = Guid.NewGuid(), Body = "hi there" };

            var result = await ChatChannelEndpoints.SendMessageAsync(messageService, channel, CreatePrincipal(), request);

            Assert.IsType<UnauthorizedHttpResult>(result.Result);
        }

        [Fact]
        public async Task SendMessageAsync_WithBothTargetsSet_ReturnsValidationProblem()
        {
            var (messageService, channel, _) = CreateMessageServiceAndChannel();
            var request = new ChatChannelSendMessageRequestDto { ReceiverId = Guid.NewGuid(), GroupId = Guid.NewGuid(), Body = "hi" };

            var result = await ChatChannelEndpoints.SendMessageAsync(messageService, channel, CreatePrincipal(Guid.NewGuid()), request);

            Assert.IsType<ValidationProblem>(result.Result);
        }

        [Fact]
        public async Task SendMessageAsync_WithNeitherTargetSet_ReturnsValidationProblem()
        {
            var (messageService, channel, _) = CreateMessageServiceAndChannel();
            var request = new ChatChannelSendMessageRequestDto { Body = "hi" };

            var result = await ChatChannelEndpoints.SendMessageAsync(messageService, channel, CreatePrincipal(Guid.NewGuid()), request);

            Assert.IsType<ValidationProblem>(result.Result);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SendMessageAsync_WithAnEmptyBody_ReturnsValidationProblem(string body)
        {
            var (messageService, channel, _) = CreateMessageServiceAndChannel();
            var request = new ChatChannelSendMessageRequestDto { ReceiverId = Guid.NewGuid(), Body = body };

            var result = await ChatChannelEndpoints.SendMessageAsync(messageService, channel, CreatePrincipal(Guid.NewGuid()), request);

            Assert.IsType<ValidationProblem>(result.Result);
        }

        [Fact]
        public void SendMessageAsync_TheRequestDtoHasNoSenderField()
        {
            // The AC's "clients cannot spoof the sender" — there is no field a client could set to
            // claim a different sender at all; SendMessageAsync only ever uses the authenticated
            // principal's resolved id.
            var type = typeof(ChatChannelSendMessageRequestDto);

            Assert.Null(type.GetProperty("SenderId"));
            Assert.Null(type.GetProperty("Sender"));
        }
    }
}
