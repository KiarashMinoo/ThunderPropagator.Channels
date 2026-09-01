using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels.Contexts;
using ThunderPropagator.Application.Pipelines.Receivers;
using ThunderPropagator.Application.Pipelines.Receivers.Attributes;
using ThunderPropagator.BuildingBlocks.Application;
using ThunderPropagator.Channels.Chat.Models.Users;
using ThunderPropagator.Channels.Chat.Pipelines;
using ThunderPropagator.Infrastructure.Channels;
using ThunderPropagator.Channels.Chat.Channel;

namespace ThunderPropagator.Channels.Chat.Pipelines.Users.Register
{
    [ReceivePipelineRequestSchema(typeof(ChatChannelRegisterReceiverPipelineRequestDto))]
    [ReceivePipelineResponseSchema(typeof(ChatChannelRegisterReceiverPipelineResponseDto))]
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelRegisterReceiverPipeline(ILoggerFactory loggerFactory, UserService userService) : AbstractReceivePipeline<ChatChannel>(loggerFactory)
    {
        private const string TelemetryActivityName = "thunderpropagator.channels.chat.users.register";
        private static readonly Counter<long>? TelemetryRequestCounter =
            Telemetry.CreateCounter<long>(TelemetryActivityName, "{request}", "Total register requests received.");

        public override string RequestKey => $"{nameof(Users)}/{nameof(Register)}";

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
                    var registerRequest = context.Request.GetRequestContentFormData<ChatChannelRegisterReceiverPipelineRequestDto>()!;

                    context.Response.ResponseCode = (int)HttpStatusCode.OK;
                    context.Response.ResponseContent = new ChatChannelRegisterReceiverPipelineResponseDto
                    {
                        User = await userService.RegisterAsync(registerRequest.UserName, registerRequest.Password, registerRequest.Name, cancellationToken)
                    };

                    TelemetryRequestCounter?.Add(1, new KeyValuePair<string, object?>(ChatChannelTelemetryTags.ChannelName, channelInfo.ChannelName));
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