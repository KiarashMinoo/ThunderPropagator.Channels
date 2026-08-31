using System.Security.Authentication;
using Microsoft.AspNetCore.Identity;
using ThunderPropagator.Channels.Chat.Models.Groups;
using ThunderPropagator.Channels.Chat.Channel;
using ThunderPropagator.Channels.Chat.Configuration;

namespace ThunderPropagator.Channels.Chat.Models.Users
{
    internal
#if !DEBUG
        sealed
#endif
        class UserService(IChatContext chatContext, IPasswordHasher<User> passwordHasher, ChatChannelConfiguration configuration)
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
            // Issue #141: checked before the username-uniqueness lookup, since a disabled-registration
            // host shouldn't even reveal whether a given username already exists.
            if (!configuration.AllowGuestRegister)
                throw new GuestRegistrationDisabledException();

            // Issue #38: previously unvalidated here — an empty/over-length username or display name
            // reached User's constructor (which only rejects null/whitespace, via a raw
            // ArgumentException rather than this domain's usual BadRequest-mapped exception shape),
            // and an empty or arbitrarily long password reached IPasswordHasher directly, hashing
            // either a blank credential or a pathologically long one with no rejection. Checked before
            // the uniqueness lookup, same reasoning as the AllowGuestRegister check above.
            if (string.IsNullOrWhiteSpace(username))
                throw new InvalidUserRegistrationRequestException("Username cannot be empty.");

            if (username.Length > configuration.MaxUserNameLength)
                throw new InvalidUserRegistrationRequestException($"Username must not exceed {configuration.MaxUserNameLength} characters (was {username.Length}).");

            if (string.IsNullOrWhiteSpace(password))
                throw new InvalidUserRegistrationRequestException("Password cannot be empty.");

            if (password.Length > configuration.MaxPasswordLength)
                throw new InvalidUserRegistrationRequestException($"Password must not exceed {configuration.MaxPasswordLength} characters.");

            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidUserRegistrationRequestException("Name cannot be empty.");

            if (name.Length > configuration.MaxDisplayNameLength)
                throw new InvalidUserRegistrationRequestException($"Name must not exceed {configuration.MaxDisplayNameLength} characters (was {name.Length}).");

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

        // Issue #38: bio was previously unbounded — any length string was accepted and persisted
        // as-is. Unlike SetNameAsync's name, an empty bio is a legitimate "clear my bio" request (the
        // domain's Bio is nullable), so this only caps length, it doesn't require non-empty.
        public async Task<User> UpdateAsync(Guid userId, string bio, DateOnly? birthDate, CancellationToken cancellationToken = default)
        {
            if (bio.Length > configuration.MaxBioLength)
                throw new InvalidUserProfileRequestException($"Bio must not exceed {configuration.MaxBioLength} characters (was {bio.Length}).");

            var user = await chatContext.GetAsync<User, Guid>(userId, cancellationToken: cancellationToken) ?? throw new UserNotFoundException();

            user.SetBio(bio);

            user.SetBirthDate(birthDate);

            await chatContext.UpdateAsync(user, cancellationToken);

            return user;
        }

        // Issue #38: previously delegated straight to User.SetName, which only rejects
        // null/whitespace (via a raw ArgumentException) and enforces no length bound at all — the same
        // gap CreateAsync/RenameGroupAsync had for group names before this issue.
        public async Task SetNameAsync(Guid userId, string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidUserProfileRequestException("Name cannot be empty.");

            if (name.Length > configuration.MaxDisplayNameLength)
                throw new InvalidUserProfileRequestException($"Name must not exceed {configuration.MaxDisplayNameLength} characters (was {name.Length}).");

            var user = await chatContext.GetAsync<User, Guid>(userId, cancellationToken: cancellationToken) ?? throw new UserNotFoundException();

            user.SetName(name);

            await chatContext.UpdateAsync(user, cancellationToken);
        }

        // Issue #38: avatar was previously unbounded and unvalidated, the same gap as GroupService's
        // SetGroupIconAsync — an empty avatar remains a legitimate "clear my avatar" request (Avatar is
        // nullable), so this only caps length.
        public async Task SetAvatarAsync(Guid userId, string avatar, CancellationToken cancellationToken = default)
        {
            if (avatar.Length > configuration.MaxAvatarLength)
                throw new InvalidUserProfileRequestException($"Avatar must not exceed {configuration.MaxAvatarLength} characters (was {avatar.Length}).");

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