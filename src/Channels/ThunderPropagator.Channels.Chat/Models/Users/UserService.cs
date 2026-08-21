using System.Security.Authentication;
using Microsoft.AspNetCore.Identity;
using ThunderPropagator.Channels.Chat.Models.Groups;

namespace ThunderPropagator.Channels.Chat.Models.Users
{
    internal
#if !DEBUG
        sealed
#endif
        class UserService(IChatContext chatContext, IPasswordHasher<User> passwordHasher)
    {
        // Issue #123: shared bounds SearchUsersAsync validates against — mirrors MessageService's
        // own DefaultPageSize/MaxPageSize (#117) for consistency, kept as this service's own
        // constants rather than a cross-service reference since the two concerns (message history
        // vs. user search) have no reason to be tied to the same numbers going forward.
        // MinSearchTermLength/MaxSearchTermLength bound the search term itself: too short (0-1 chars)
        // matches too broadly to be a useful "search" and is needlessly expensive at scale; too long
        // is just wasted/abusive input no legitimate username or display name would need.
        public const int DefaultPageSize = 50;
        public const int MaxPageSize = 100;
        public const int MinSearchTermLength = 2;
        public const int MaxSearchTermLength = 100;

        public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
            => chatContext.GetAsync<User, Guid>(userId, cancellationToken);

        public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
            => chatContext.GetAsync<User>(x => x.UserName == username, cancellationToken);

        public async Task<User> RegisterAsync(string username, string password, string name, CancellationToken cancellationToken = default)
        {
            var dbUser = await GetByUsernameAsync(username, cancellationToken);
            if (dbUser is not null)
                throw new InvalidOperationException("Username already exists");

            var user = User.Create(username, name);
            user.SetPasswordHash(passwordHasher.HashPassword(user, password));

            return await chatContext.CreateAsync(user, cancellationToken);
        }

        public async Task<User> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
        {
            var user = await GetByUsernameAsync(username, cancellationToken);

            if (user is null)
                throw new InvalidCredentialException();

            PasswordVerificationResult result;
            try
            {
                result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            }
            catch (FormatException)
            {
                throw new InvalidCredentialException();
            }

            if (result == PasswordVerificationResult.Failed)
                throw new InvalidCredentialException();

            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.SetPasswordHash(passwordHasher.HashPassword(user, password));
                await chatContext.UpdateAsync(user, cancellationToken);
            }

            return user;
        }

        public async Task<IReadOnlyCollection<Group>> GetUserGroupsAsync(Guid id, CancellationToken cancellationToken = default)
            => await chatContext.GetAllAsync<Group>(x => x.GroupUsers.Any(y => y.UserId == id), cancellationToken);

        public Task<IReadOnlyCollection<User>> GetUserContactsAsync(Guid id, CancellationToken cancellationToken = default)
            => chatContext.GetContactsAsync(id, cancellationToken);

        public async Task<User> UpdateAsync(Guid userId, string bio, DateOnly? birthDate, CancellationToken cancellationToken = default)
        {
            var user = await chatContext.GetAsync<User, Guid>(userId, cancellationToken: cancellationToken) ?? throw new UserNotFoundException();

            user.SetBio(bio);

            user.SetBirthDate(birthDate);

            await chatContext.UpdateAsync(user, cancellationToken);

            return user;
        }

        public async Task SetNameAsync(Guid userId, string name, CancellationToken cancellationToken = default)
        {
            var user = await chatContext.GetAsync<User, Guid>(userId, cancellationToken: cancellationToken) ?? throw new UserNotFoundException();

            user.SetName(name);

            await chatContext.UpdateAsync(user, cancellationToken);
        }

        public async Task SetAvatarAsync(Guid userId, string avatar, CancellationToken cancellationToken = default)
        {
            var user = await chatContext.GetAsync<User, Guid>(userId, cancellationToken: cancellationToken) ?? throw new UserNotFoundException();

            user.SetAvatar(avatar);

            await chatContext.UpdateAsync(user, cancellationToken);
        }

        // Issue #123: term is trimmed and lowercased here — once, in the one place every provider's
        // own case-insensitive match relies on — rather than each provider re-normalizing it its own
        // way. Validated before the provider is ever called, so providers can assume the term is
        // already non-empty and within bounds (the same division of responsibility #117's paging
        // validation uses).
        public Task<UserSearchPage> SearchUsersAsync(string term, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var normalizedTerm = (term ?? string.Empty).Trim().ToLowerInvariant();

            if (normalizedTerm.Length < MinSearchTermLength)
                throw new InvalidUserSearchRequestException($"Search term must be at least {MinSearchTermLength} characters.");

            if (normalizedTerm.Length > MaxSearchTermLength)
                throw new InvalidUserSearchRequestException($"Search term must be at most {MaxSearchTermLength} characters.");

            if (page < 1)
                throw new InvalidUserSearchRequestException("Page must be 1 or greater.");

            if (pageSize is < 1 or > MaxPageSize)
                throw new InvalidUserSearchRequestException($"PageSize must be between 1 and {MaxPageSize}.");

            return chatContext.SearchUsersAsync(normalizedTerm, page, pageSize, cancellationToken);
        }

        // Issue #126: "online" is presence state ChatChannel tracks in memory (see its own
        // LoggedInUsers), not something any provider's persistence can query — onlineUserIds is
        // that snapshot's distinct set of currently logged-in user ids, already deduplicated by the
        // caller regardless of how many connections a user has open (see
        // ChatChannelGetOnlineUsersReceiverPipeline). Visibility is restricted to the caller's own
        // contacts (#115's GetUserContactsAsync) rather than every online user — this codebase has
        // no broader friends/blocking model to scope it any other way, and an unscoped list would
        // leak who else is using the system to any authenticated caller. Both the contact list and
        // the online set are already fully materialized (a DB round trip and an in-memory
        // dictionary snapshot respectively) rather than two ends of one queryable source, so their
        // intersection and paging both happen here rather than being pushed down to a provider.
        public async Task<OnlineUsersPage> GetOnlineContactsAsync(Guid currentUserId, IReadOnlyCollection<Guid> onlineUserIds, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            if (page < 1)
                throw new InvalidOnlineUsersRequestException("Page must be 1 or greater.");

            if (pageSize is < 1 or > MaxPageSize)
                throw new InvalidOnlineUsersRequestException($"PageSize must be between 1 and {MaxPageSize}.");

            var onlineUserIdSet = onlineUserIds.ToHashSet();
            var contacts = await GetUserContactsAsync(currentUserId, cancellationToken);
            var onlineContacts = contacts.Where(contact => onlineUserIdSet.Contains(contact.Id)).ToList();

            return new OnlineUsersPage
            {
                Users = onlineContacts.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                TotalCount = onlineContacts.Count,
                Page = page,
                PageSize = pageSize
            };
        }
    }
}