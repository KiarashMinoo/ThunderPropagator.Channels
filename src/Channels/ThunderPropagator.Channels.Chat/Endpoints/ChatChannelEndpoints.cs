using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using ThunderPropagator.Channels.Chat.Models.Groups;
using ThunderPropagator.Channels.Chat.Models.Messages;
using ThunderPropagator.Channels.Chat.Models.Users;
using ThunderPropagator.Channels.Chat.Pipelines.Messages.History;
using ThunderPropagator.Channels.Chat.Pipelines.Users.Get;
using ThunderPropagator.Channels.Chat.Pipelines.Users.Search;

namespace ThunderPropagator.Channels.Chat.Endpoints
{
    /// <summary>
    /// Issue #4/#127: the REST surface for the Chat channel. Every route is mapped through this one
    /// entry point (as the parent issue's AC requires) and reuses the same application services as
    /// the corresponding WebSocket pipeline rather than duplicating domain logic.
    /// </summary>
    public static class ChatChannelEndpoints
    {
        public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var chat = endpoints.MapGroup("/api/chat")
                .RequireAuthorization()
                .WithTags("Chat");

            chat.MapGet("/users/{userId}", GetUserByIdAsync)
                .WithName("Chat_GetUserById")
                .WithSummary("Retrieves the public profile of a chat user by id.")
                .Produces<ChatChannelGetUserReceiverPipelineResponseDto>(StatusCodes.Status200OK)
                .ProducesValidationProblem()
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status404NotFound);

            chat.MapGet("/users/search", SearchUsersAsync)
                .WithName("Chat_SearchUsers")
                .WithSummary("Searches for chat users by username or display name.")
                .Produces<ChatChannelSearchUsersReceiverPipelineResponseDto>(StatusCodes.Status200OK)
                .ProducesValidationProblem()
                .Produces(StatusCodes.Status401Unauthorized);

            chat.MapGet("/messages", GetDirectMessageHistoryAsync)
                .WithName("Chat_GetDirectMessageHistory")
                .WithSummary("Retrieves paginated direct-message history with another user.")
                .Produces<ChatChannelGetMessageHistoryReceiverPipelineResponseDto>(StatusCodes.Status200OK)
                .ProducesValidationProblem()
                .Produces(StatusCodes.Status401Unauthorized);

            chat.MapGet("/groups/{groupId}/messages", GetGroupMessageHistoryAsync)
                .WithName("Chat_GetGroupMessageHistory")
                .WithSummary("Retrieves paginated message history for a group.")
                .Produces<ChatChannelGetMessageHistoryReceiverPipelineResponseDto>(StatusCodes.Status200OK)
                .ProducesValidationProblem()
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces(StatusCodes.Status404NotFound);

            chat.MapGet("/groups", GetGroupsAsync)
                .WithName("Chat_GetGroups")
                .WithSummary("Lists the authenticated caller's groups.")
                .Produces<ChatChannelGetGroupsResponseDto>(StatusCodes.Status200OK)
                .ProducesValidationProblem()
                .Produces(StatusCodes.Status401Unauthorized);

            chat.MapGet("/groups/{groupId}", GetGroupDetailsAsync)
                .WithName("Chat_GetGroupDetails")
                .WithSummary("Retrieves a group's details and a page of its members.")
                .Produces<ChatChannelGroupDetailsResponseDto>(StatusCodes.Status200OK)
                .ProducesValidationProblem()
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces(StatusCodes.Status404NotFound);

            chat.MapPost("/messages", SendMessageAsync)
                .WithName("Chat_SendMessage")
                .WithSummary("Sends a direct or group message.")
                .Produces<ChatChannelSentMessageResponseDto>(StatusCodes.Status201Created)
                .ProducesValidationProblem()
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status404NotFound);

            return endpoints;
        }

        // Issue #129: the caller's own id comes from the authenticated principal, never from a
        // client-supplied parameter — every other REST issue that needs "who am I" (#130-#137) will
        // share this same resolution rather than each reimplementing it. RequireAuthorization() on
        // the group already guarantees an authenticated principal; this still fails closed (as
        // Unauthorized, the same outcome as not being authenticated at all) if that principal somehow
        // lacks a NameIdentifier claim shaped as a non-empty GUID, since every domain call downstream
        // requires one.
        private static bool TryGetCurrentUserId(ClaimsPrincipal principal, out Guid currentUserId)
            => Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out currentUserId) && currentUserId != Guid.Empty;

        // Issue #127: userId is bound as a plain string (rather than a {userId:guid} route
        // constraint) so a malformed value reaches this handler as the documented ValidationProblem
        // response instead of ASP.NET Core's routing layer silently 404-ing it as "no route matched"
        // indistinguishably from a well-formed but unknown id.
        internal static async Task<Results<Ok<ChatChannelGetUserReceiverPipelineResponseDto>, ValidationProblem, NotFound>> GetUserByIdAsync(
            string userId,
            [FromServices] UserService userService,
            CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(userId, out var parsedUserId) || parsedUserId == Guid.Empty)
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(userId)] = ["userId must be a valid, non-empty GUID."]
                });

            // Issue #122's own pipeline notes that every existing user is currently visible to every
            // authenticated caller — this codebase has no blocking/hidden-profile concept yet — so
            // "not found" is the only rejection this endpoint can produce beyond authentication today.
            var user = await userService.GetByIdAsync(parsedUserId, cancellationToken);

            return user is null
                ? TypedResults.NotFound()
                : TypedResults.Ok(ChatChannelGetUserReceiverPipelineResponseDto.FromUser(user));
        }

        // Issue #128: term/paging bounds (minimum/maximum term length, page and page-size limits) are
        // validated exactly once, inside UserService.SearchUsersAsync — the same call the WebSocket
        // Users/Search pipeline (#123) makes — rather than re-implemented here. page/pageSize default
        // to the same values the WebSocket request DTO defaults to (see
        // ChatChannelSearchUsersReceiverPipelineRequestDto) when the caller omits them; this handler
        // only translates the one exception that validation can throw — for whichever of term, page,
        // or pageSize is out of bounds — into the documented ValidationProblem response.
        internal static async Task<Results<Ok<ChatChannelSearchUsersReceiverPipelineResponseDto>, ValidationProblem>> SearchUsersAsync(
            [FromServices] UserService userService,
            string? q,
            int page = 1,
            int pageSize = UserService.DefaultPageSize,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var results = await userService.SearchUsersAsync(q ?? string.Empty, page, pageSize, cancellationToken);

                return TypedResults.Ok(new ChatChannelSearchUsersReceiverPipelineResponseDto
                {
                    Users = results.Users.Select(ChatChannelGetUserReceiverPipelineResponseDto.FromUser).ToList(),
                    TotalCount = results.TotalCount,
                    Page = results.Page,
                    PageSize = results.PageSize
                });
            }
            catch (InvalidUserSearchRequestException exception)
            {
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["q"] = [exception.Message]
                });
            }
        }

        // Issue #129: "with" identifies the other participant; MessageService.GetDirectMessageHistoryAsync
        // (the same call the WebSocket Messages/History pipeline (#118) makes for the direct case) is
        // always scoped to a conversation between the caller and "with" — there is no way to pass any
        // other user's id as "mine", so every caller can only ever read their own conversations. That
        // makes "only conversation participants can retrieve history" true by construction rather than
        // something this handler needs to check separately, and leaves no genuine forbidden case beyond
        // TryGetCurrentUserId's own Unauthorized rejection.
        internal static async Task<Results<Ok<ChatChannelGetMessageHistoryReceiverPipelineResponseDto>, ValidationProblem, UnauthorizedHttpResult>> GetDirectMessageHistoryAsync(
            [FromServices] MessageService messageService,
            ClaimsPrincipal principal,
            string? with,
            int page = 1,
            int size = MessageService.DefaultPageSize,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(principal, out var currentUserId))
                return TypedResults.Unauthorized();

            if (!Guid.TryParse(with, out var otherUserId) || otherUserId == Guid.Empty)
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(with)] = ["with must be a valid, non-empty GUID identifying the other participant."]
                });

            try
            {
                var history = await messageService.GetDirectMessageHistoryAsync(currentUserId, otherUserId, page, size, cancellationToken);

                return TypedResults.Ok(new ChatChannelGetMessageHistoryReceiverPipelineResponseDto
                {
                    Messages = history.Messages,
                    TotalCount = history.TotalCount,
                    Page = history.Page,
                    PageSize = history.PageSize
                });
            }
            catch (InvalidMessageHistoryPageRequestException exception)
            {
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["page"] = [exception.Message]
                });
            }
        }

        // Issue #130: unlike the direct case, group membership isn't implied by asking — reused
        // MessageService.GetGroupMessageHistoryAsync (the same call the WebSocket Messages/History
        // pipeline makes for the group case) checks the group exists and that the caller is a
        // *current* member of its GroupUsers, which is also this codebase's documented former-member
        // policy: leaving (or being removed from) a group's GroupUsers revokes access to its history
        // the same way never having joined does — there is no grandfather clause for messages seen
        // before leaving.
        internal static async Task<Results<Ok<ChatChannelGetMessageHistoryReceiverPipelineResponseDto>, ValidationProblem, UnauthorizedHttpResult, ForbidHttpResult, NotFound>> GetGroupMessageHistoryAsync(
            [FromServices] MessageService messageService,
            ClaimsPrincipal principal,
            string groupId,
            int page = 1,
            int size = MessageService.DefaultPageSize,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(principal, out var currentUserId))
                return TypedResults.Unauthorized();

            if (!Guid.TryParse(groupId, out var parsedGroupId) || parsedGroupId == Guid.Empty)
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(groupId)] = ["groupId must be a valid, non-empty GUID."]
                });

            try
            {
                var history = await messageService.GetGroupMessageHistoryAsync(currentUserId, parsedGroupId, page, size, cancellationToken);

                return TypedResults.Ok(new ChatChannelGetMessageHistoryReceiverPipelineResponseDto
                {
                    Messages = history.Messages,
                    TotalCount = history.TotalCount,
                    Page = history.Page,
                    PageSize = history.PageSize
                });
            }
            catch (InvalidMessageHistoryPageRequestException exception)
            {
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["page"] = [exception.Message]
                });
            }
            catch (GroupNotFoundException)
            {
                return TypedResults.NotFound();
            }
            catch (GroupAccessDeniedException)
            {
                return TypedResults.Forbid();
            }
        }

        // Issue #131: deliberately calls UserService.GetUserGroupsAsync — the membership-scoped query
        // (#115) — rather than GroupService.GetAllAsync, which the WebSocket Groups/GetAll pipeline
        // uses and which returns every non-deleted group in the system with no membership filter at
        // all. That pipeline answers a different question ("what groups exist") than this endpoint's
        // own AC ("groups visible to the current user"), so reusing it here would violate this
        // issue's own "do not accept a user id that could bypass the authenticated identity" and
        // "returns only groups visible to the current user" requirements. GetUserGroupsAsync's
        // membership predicate (GroupUsers.Any(gu => gu.UserId == id)) returns each matching Group
        // entity once regardless of how many GroupUser rows reference it, so a duplicate membership
        // row can never duplicate a group in the result. Paging happens here rather than in the
        // service/provider: unlike message history, a single user's own group count is bounded, and
        // no persistence method for it exists yet to push this into; the envelope shape still matches
        // every other paginated response in this surface.
        internal static async Task<Results<Ok<ChatChannelGetGroupsResponseDto>, ValidationProblem, UnauthorizedHttpResult>> GetGroupsAsync(
            [FromServices] UserService userService,
            ClaimsPrincipal principal,
            int page = 1,
            int pageSize = UserService.DefaultPageSize,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(principal, out var currentUserId))
                return TypedResults.Unauthorized();

            if (page < 1)
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(page)] = ["Page must be 1 or greater."]
                });

            if (pageSize is < 1 or > UserService.MaxPageSize)
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(pageSize)] = [$"PageSize must be between 1 and {UserService.MaxPageSize}."]
                });

            var groups = await userService.GetUserGroupsAsync(currentUserId, cancellationToken);
            var ordered = groups.OrderBy(group => group.Name, StringComparer.Ordinal).ThenBy(group => group.Id).ToList();

            return TypedResults.Ok(new ChatChannelGetGroupsResponseDto
            {
                Groups = ordered.Skip((page - 1) * pageSize).Take(pageSize).Select(ChatChannelGroupSummaryDto.FromGroup).ToList(),
                TotalCount = ordered.Count,
                Page = page,
                PageSize = pageSize
            });
        }

        // Issue #132: no WebSocket pipeline retrieves a single group's details, so this calls the new
        // GroupService.GetGroupDetailsAsync (added for this issue — see its own comment for why),
        // which owns the same membership check #130 already established for group message history.
        // Member ids are paginated before any User is looked up, so a large group's cost stays bounded
        // by pageSize member lookups rather than scaling with total membership; ordering by UserId
        // keeps that paging deterministic across calls without needing every member's profile loaded
        // first just to sort by name.
        internal static async Task<Results<Ok<ChatChannelGroupDetailsResponseDto>, ValidationProblem, UnauthorizedHttpResult, ForbidHttpResult, NotFound>> GetGroupDetailsAsync(
            [FromServices] GroupService groupService,
            [FromServices] UserService userService,
            ClaimsPrincipal principal,
            string groupId,
            int page = 1,
            int pageSize = UserService.DefaultPageSize,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(principal, out var currentUserId))
                return TypedResults.Unauthorized();

            if (!Guid.TryParse(groupId, out var parsedGroupId) || parsedGroupId == Guid.Empty)
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(groupId)] = ["groupId must be a valid, non-empty GUID."]
                });

            if (page < 1)
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(page)] = ["Page must be 1 or greater."]
                });

            if (pageSize is < 1 or > UserService.MaxPageSize)
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(pageSize)] = [$"PageSize must be between 1 and {UserService.MaxPageSize}."]
                });

            try
            {
                var group = await groupService.GetGroupDetailsAsync(currentUserId, parsedGroupId, cancellationToken);

                var orderedMemberIds = group.GroupUsers.Select(groupUser => groupUser.UserId).OrderBy(id => id).ToList();
                var pagedMemberIds = orderedMemberIds.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                var members = new List<ChatChannelGetUserReceiverPipelineResponseDto>();
                foreach (var memberId in pagedMemberIds)
                {
                    var member = await userService.GetByIdAsync(memberId, cancellationToken);
                    if (member is not null)
                        members.Add(ChatChannelGetUserReceiverPipelineResponseDto.FromUser(member));
                }

                return TypedResults.Ok(new ChatChannelGroupDetailsResponseDto
                {
                    Id = group.Id,
                    Name = group.Name,
                    GroupIcon = group.GroupIcon,
                    CreatedByUserId = group.CreatedByUserId,
                    MemberCount = orderedMemberIds.Count,
                    Members = members,
                    MembersPage = page,
                    MembersPageSize = pageSize
                });
            }
            catch (GroupNotFoundException)
            {
                return TypedResults.NotFound();
            }
            catch (GroupAccessDeniedException)
            {
                return TypedResults.Forbid();
            }
        }

        // Issue #133: reuses MessageService.SendMessageAsync/SendMessageToGroupAsync — the same calls
        // the WebSocket Messages/Send pipeline makes — including that pipeline's own lack of a
        // group-membership check on send (SendMessageToGroupAsync fans out to every current member
        // without verifying the sender is one); adding that check here only for REST would give the
        // two transports different rules for the exact case this issue's AC says must match. Emits
        // through ChatChannel.EmitMessage for every persisted row, same as the pipeline, so a message
        // sent over REST still reaches WebSocket-connected recipients in real time — the parent
        // issue's "WebSocket and REST transports ... produce equivalent state" requirement. The
        // sender is only ever TryGetCurrentUserId's resolved identity; there is no field on the
        // request body a client could set instead.
        internal static async Task<Results<Created<ChatChannelSentMessageResponseDto>, ValidationProblem, UnauthorizedHttpResult, NotFound>> SendMessageAsync(
            [FromServices] MessageService messageService,
            [FromServices] ChatChannel chatChannel,
            ClaimsPrincipal principal,
            [FromBody] ChatChannelSendMessageRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(principal, out var currentUserId))
                return TypedResults.Unauthorized();

            var hasReceiver = request.ReceiverId is not null && request.ReceiverId != Guid.Empty;
            var hasGroup = request.GroupId is not null && request.GroupId != Guid.Empty;

            if (hasReceiver == hasGroup)
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["target"] = ["Exactly one of receiverId or groupId must be specified."]
                });

            if (string.IsNullOrWhiteSpace(request.Body))
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(request.Body)] = ["Body must not be empty."]
                });

            if (hasReceiver)
            {
                var message = await messageService.SendMessageAsync(currentUserId, request.ReceiverId!.Value, request.Body, cancellationToken);
                chatChannel.EmitMessage(new ChatChannelFeederMessage(message));

                return TypedResults.Created((string?)null, new ChatChannelSentMessageResponseDto
                {
                    MessageIds = [message.Id],
                    SenderId = currentUserId,
                    ReceiverId = request.ReceiverId,
                    GroupId = null,
                    Body = request.Body,
                    Created = message.Created
                });
            }

            try
            {
                var messages = await messageService.SendMessageToGroupAsync(currentUserId, request.GroupId!.Value, request.Body, cancellationToken);
                foreach (var message in messages)
                    chatChannel.EmitMessage(new ChatChannelFeederMessage(message));

                return TypedResults.Created((string?)null, new ChatChannelSentMessageResponseDto
                {
                    MessageIds = messages.Select(message => message.Id).ToList(),
                    SenderId = currentUserId,
                    ReceiverId = null,
                    GroupId = request.GroupId,
                    Body = request.Body,
                    Created = messages.FirstOrDefault()?.Created ?? DateTimeOffset.UtcNow
                });
            }
            catch (GroupNotFoundException)
            {
                return TypedResults.NotFound();
            }
        }
    }
}
