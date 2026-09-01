using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Security.Authentication;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels.Contexts;
using ThunderPropagator.Application.Pipelines.Receivers;
using ThunderPropagator.Application.Pipelines.Receivers.Attributes;
using ThunderPropagator.BuildingBlocks.Application;
using ThunderPropagator.Channels.Chat.Models.Users;
using ThunderPropagator.Channels.Chat.Pipelines;
using ThunderPropagator.Infrastructure.Channels;
using ThunderPropagator.Channels.Chat.Channel;

namespace ThunderPropagator.Channels.Chat.Pipelines.Users.Login
{
    [ReceivePipelineRequestSchema(typeof(ChatChannelLoginReceiverPipelineRequestDto))]
    [ReceivePipelineResponseSchema(typeof(ChatChannelLoginReceiverPipelineResponseDto))]
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelLoginReceiverPipeline(ILoggerFactory loggerFactory, UserService userService) : AbstractReceivePipeline<ChatChannel>(loggerFactory)
    {
        private const string TelemetryActivityName = "thunderpropagator.channels.chat.users.login";
        private static readonly Counter<long>? TelemetryRequestCounter =
            Telemetry.CreateCounter<long>(TelemetryActivityName, "{request}", "Total login requests received.");

        public override string RequestKey => $"{nameof(Users)}/{nameof(Login)}";

        public async Task Invoke(ChannelInfo channelInfo,
            ReceiveContext context,
            ReceivePipelineDelegate next,
            CancellationToken cancellationToken = default)
        {
            using var activity = Telemetry.StartActivity(TelemetryActivityName, ActivityKind.Consumer)?
                .SetTag(ChatChannelTelemetryTags.ChannelType, channelInfo.ChannelType)
                .SetTag(ChatChannelTelemetryTags.ChannelKey, channelInfo.ChannelKey)
                .SetTag(ChatChannelTelemetryTags.ChannelName, channelInfo.ChannelName);

            try
            {
                if (context.Request.RouteTable["RequestType"].Equals(RequestKey))
                {
                    var loginRequest = context.Request.GetRequestContentFormData<ChatChannelLoginReceiverPipelineRequestDto>()!;

                    try
                    {
                        var user = await userService.LoginAsync(loginRequest.UserName, loginRequest.Password, cancellationToken);

                        ((ChatChannel)channelInfo.Channel)
                            .LoggedInUsers
                            .AddOrUpdate(context.WebSocketConnectionInfo.ConnectionId, user.Id, (_, _) => user.Id);

                        context.Response.ResponseCode = (int)HttpStatusCode.OK;
                        context.Response.ResponseContent = new ChatChannelLoginReceiverPipelineResponseDto
                        {
                            User = user,
                            Groups = await userService.GetUserGroupsAsync(user.Id, cancellationToken),
                            Contacts = await userService.GetUserContactsAsync(user.Id, cancellationToken),
                        };

                        TelemetryRequestCounter?.Add(1, new KeyValuePair<string, object?>(ChatChannelTelemetryTags.ChannelName, channelInfo.ChannelName));
                    }
                    catch (InvalidCredentialException exception)
                    {
                        throw new ChatChannelLoginReceiverPipelineInvalidCredentialException(exception);
                    }
                }
                else
                {
                    await next(context, cancellationToken);
                }
            }
            finally
            {
                activity?.SetStatus(ActivityStatusCode.Ok);
            }
        }
    }
}