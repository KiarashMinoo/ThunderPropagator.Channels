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
using RapidStreamer.Channels.Chat.Exceptions;
using RapidStreamer.Channels.Chat.Models.Users;
using RapidStreamer.Channels.Chat.Pipelines.Users.Login;
using RapidStreamer.Infrastructure.Channels;

namespace RapidStreamer.Channels.Chat.Pipelines.Users.SetName
{
    [ReceivePipelineRequestSchema(typeof(ChatChannelUserSetNameReceiverPipelineRequestDto))]
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelUserSetNameReceiverPipeline(ILoggerFactory loggerFactory, UserService userService) : AbstractReceivePipeline<ChatChannel>(loggerFactory)
    {
        private Counter<long>? _counter;

        public override string RequestKey => $"{nameof(Users)}/{nameof(SetName)}";

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
                    var setNameRequest = context.Request.GetRequestContentFormData<ChatChannelUserSetNameReceiverPipelineRequestDto>()!;

                    try
                    {
                        var chatChannel = (ChatChannel)channelInfo.Channel;
                        var userId = chatChannel.LoggedInUsers[context.WebSocketConnectionInfo.ConnectionId];
                        var user = await userService.GetByIdAsync(userId, cancellationToken) ?? throw new UserNotFoundException();

                        await userService.SetNameAsync(user.Id, setNameRequest.Name, cancellationToken);

                        context.Response.ResponseCode = (int)HttpStatusCode.OK;
                        context.Response.ResponseContent = "Set";

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