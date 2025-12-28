using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Channels.Chat.Models;
using ThunderPropagator.Channels.Chat.Models.Groups;
using ThunderPropagator.Channels.Chat.Models.Messages;
using ThunderPropagator.Channels.Chat.Models.Users;
using ThunderPropagator.Channels.Chat.Pipelines.Groups.AddUser;
using ThunderPropagator.Channels.Chat.Pipelines.Groups.Create;
using ThunderPropagator.Channels.Chat.Pipelines.Groups.GetAll;
using ThunderPropagator.Channels.Chat.Pipelines.Groups.Join;
using ThunderPropagator.Channels.Chat.Pipelines.Groups.RemoveUser;
using ThunderPropagator.Channels.Chat.Pipelines.Groups.Rename;
using ThunderPropagator.Channels.Chat.Pipelines.Groups.SetIcon;
using ThunderPropagator.Channels.Chat.Pipelines.Groups.UserLeave;
using ThunderPropagator.Channels.Chat.Pipelines.Messages.Send;
using ThunderPropagator.Channels.Chat.Pipelines.Users.Login;
using ThunderPropagator.Channels.Chat.Pipelines.Users.Register;
using ThunderPropagator.Channels.Chat.Pipelines.Users.SetAvatar;
using ThunderPropagator.Channels.Chat.Pipelines.Users.SetName;
using ThunderPropagator.Channels.Chat.Pipelines.Users.Update;
using ThunderPropagator.Infrastructure.Extensions;

namespace ThunderPropagator.Channels.Chat
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