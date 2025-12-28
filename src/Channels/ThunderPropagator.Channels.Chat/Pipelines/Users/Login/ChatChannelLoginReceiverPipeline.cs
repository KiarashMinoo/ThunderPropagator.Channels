using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Reflection;
using System.Security.Authentication;
using CaseConverter;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels.Contexts;
using ThunderPropagator.Application.Pipelines.Receivers;
using ThunderPropagator.Application.Pipelines.Receivers.Attributes;
using ThunderPropagator.BuildingBlocks.Application;
using ThunderPropagator.Channels.Chat.Models.Users;
using ThunderPropagator.Infrastructure.Channels;

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
        private Counter<long>? _counter;

        public override string RequestKey => $"{nameof(Users)}/{nameof(Login)}";

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