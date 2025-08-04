using Microsoft.Extensions.DependencyInjection;
using RapidStreamer.Channels.Chat.Models;
using RapidStreamer.Channels.Chat.Models.Groups;
using RapidStreamer.Channels.Chat.Models.Messages;
using RapidStreamer.Channels.Chat.Models.Users;
using RapidStreamer.Channels.Chat.Pipelines.Groups.AddUser;
using RapidStreamer.Channels.Chat.Pipelines.Groups.Create;
using RapidStreamer.Channels.Chat.Pipelines.Groups.GetAll;
using RapidStreamer.Channels.Chat.Pipelines.Groups.Join;
using RapidStreamer.Channels.Chat.Pipelines.Groups.RemoveUser;
using RapidStreamer.Channels.Chat.Pipelines.Groups.Rename;
using RapidStreamer.Channels.Chat.Pipelines.Groups.SetIcon;
using RapidStreamer.Channels.Chat.Pipelines.Groups.UserLeave;
using RapidStreamer.Channels.Chat.Pipelines.Messages.Send;
using RapidStreamer.Channels.Chat.Pipelines.Users.Login;
using RapidStreamer.Channels.Chat.Pipelines.Users.Register;
using RapidStreamer.Channels.Chat.Pipelines.Users.SetAvatar;
using RapidStreamer.Channels.Chat.Pipelines.Users.SetName;
using RapidStreamer.Channels.Chat.Pipelines.Users.Update;
using RapidStreamer.Infrastructure.Extensions;

namespace RapidStreamer.Channels.Chat
{
    public static class ChatChannelExtensions
    {
        public static IServiceCollection AddChatChannel<TChatContext>
        (
            this IServiceCollection services,
            Action<ChatChannelConfiguration>? channelConfigurator = null)
            where TChatContext : BaseChatContext
        {
            ChatChannelConfiguration chatChannelConfiguration = new();
            channelConfigurator?.Invoke(chatChannelConfiguration);

            services
                .AddSingleton(chatChannelConfiguration)
                .AddChannel<ChatChannel>()
                //Groups
                .AddReceivePipeline<ChatChannel, ChatChannelAddUserToGroupReceiverPipeline>()
                .AddReceivePipeline<ChatChannel, ChatChannelCreateGroupReceiverPipeline>()
                .AddReceivePipeline<ChatChannel, ChatChannelGetGroupsReceiverPipeline>()
                .AddReceivePipeline<ChatChannel, ChatChannelJoinUserToGroupReceiverPipeline>()
                .AddReceivePipeline<ChatChannel, ChatChannelRemoveUserToGroupReceiverPipeline>()
                .AddReceivePipeline<ChatChannel, ChatChannelRenameGroupReceiverPipeline>()
                .AddReceivePipeline<ChatChannel, ChatChannelSetGroupIconReceiverPipeline>()
                .AddReceivePipeline<ChatChannel, ChatChannelUserLeaveFromGroupReceiverPipeline>()
                //Messages
                .AddReceivePipeline<ChatChannel, ChatChannelSendMessageReceiverPipeline>()
                //Users
                .AddReceivePipeline<ChatChannel, ChatChannelLoginReceiverPipeline>()
                .AddReceivePipeline<ChatChannel, ChatChannelRegisterReceiverPipeline>()
                .AddReceivePipeline<ChatChannel, ChatChannelUserSetAvatarReceiverPipeline>()
                .AddReceivePipeline<ChatChannel, ChatChannelUserSetNameReceiverPipeline>()
                .AddReceivePipeline<ChatChannel, ChatChannelUpdateUserReceiverPipeline>()
                //Db Services
                .AddScoped<TChatContext>()
                .AddScoped<IChatContext>(serviceProvider => serviceProvider.GetRequiredService<TChatContext>())
                .AddScoped<GroupService>()
                .AddScoped<MessageService>()
                .AddScoped<UserService>();

            return services;
        }
    }
}