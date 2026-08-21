using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using ThunderPropagator.Channels.Chat.Models.Users;
using ThunderPropagator.Channels.Chat.Pipelines.Users.Get;

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
    }
}
