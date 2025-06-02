using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Reflection;
using System.Security.Authentication;
using CaseConverter;
using Microsoft.Extensions.Logging;
using RapidStreamer.Application.Channels.Contexts;
using RapidStreamer.Application.Pipelines.Receivers;
using RapidStreamer.Application.Pipelines.Receivers.Attributes;
using RapidStreamer.BuildingBlocks.Application;
using RapidStreamer.Channels.Chat.Models.Users;
using RapidStreamer.Channels.Chat.Pipelines.Users.Login;
using RapidStreamer.Infrastructure.Channels;

namespace RapidStreamer.Channels.Chat.Pipelines.Users.Update
{
    [ReceivePipelineRequestSchema(typeof(ChatChannelUpdateUserReceiverPipelineRequestDto))]
    [ReceivePipelineResponseSchema(typeof(ChatChannelUpdateUserReceiverPipelineResponseDto))]
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelUpdateUserReceiverPipeline(ILoggerFactory loggerFactory, UserService userService) : AbstractReceivePipeline<ChatChannel>(loggerFactory)
    {
        private Counter<long>? _counter;

        public override string RequestKey => $"{nameof(Users)}/{nameof(Update)}";

        public async Task Invoke(ChannelInfo channelInfo,
            ReceiveContext context,
            ReceivePipelineDelegate next,
            CancellationToken cancellationToken = default)
        {
            var activityName = $"{channelInfo.ChannelName}_{GetType().GetTypeInfo().Name}_{nameof(Invoke)}";
            _counter ??= Telemetry.CreateCounter<long>(activityName.ToSnakeCase());

            using var activity = Telemetry.StartActivity(activityName, ActivityKind.Consumer)?
                .SetTag(nameof(ChannelInfo.ChannelType), channelInfo.ChannelType)
                .SetTag(nameof(ChannelInfo.ChannelKey), channelInfo.ChannelKey)
                .SetTag(nameof(ChannelInfo.ChannelName), channelInfo.ChannelName);

            try
            {
                if (context.Request.RouteTable["RequestType"].Equals(RequestKey))
                {
                    var updateRequest = context.Request.GetRequestContentFormData<ChatChannelUpdateUserReceiverPipelineRequestDto>()!;

                    try
                    {
                        var chatChannel = (ChatChannel)channelInfo.Channel;
                        var userId = chatChannel.LoggedInUsers[context.WebSocketConnectionInfo.ConnectionId];
                        var user = await userService.GetByIdAsync(userId, cancellationToken) ?? throw new UserNotFoundException();

                        context.Response.ResponseCode = (int)HttpStatusCode.OK;
                        context.Response.ResponseContent = new ChatChannelUpdateUserReceiverPipelineResponseDto
                        {
                            User = await userService.UpdateAsync(user.Id, updateRequest.Bio, updateRequest.BirthDate, cancellationToken)
                        };

                        _counter?.Add(1, new KeyValuePair<string, object?>(nameof(channelInfo.ChannelName), channelInfo.ChannelName));
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