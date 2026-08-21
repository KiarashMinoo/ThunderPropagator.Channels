using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using ThunderPropagator.Channels.Chat.Models.Users;
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

            return endpoints;
        }

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
    }
}
