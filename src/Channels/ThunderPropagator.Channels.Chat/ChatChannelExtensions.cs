using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
using ThunderPropagator.Channels.Chat.Pipelines.Messages.Delete;
using ThunderPropagator.Channels.Chat.Pipelines.Messages.History;
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
                .AddReceivePipeline<ChatChannel, ChatChannelDeleteMessageReceiverPipeline>()
                // Issue #117/#118: ChatChannelGetMessageHistoryReceiverPipeline was implemented and
                // covered by the authentication reflection sweep (which only checks the assembly, not
                // DI) but was never actually registered here — a real consumer's AddChatChannel call
                // would never have this pipeline available at all. Fixed alongside #119 since this
                // file was already being touched for the Delete pipeline's own registration.
                .AddReceivePipeline<ChatChannel, ChatChannelGetMessageHistoryReceiverPipeline>()
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
                .AddScoped<UserService>()
                .AddScoped<IPasswordHasher<User>, PasswordHasher<User>>()
                // Issue #114: awaited during host startup, before the host (and, for ASP.NET Core,
                // Kestrel) starts accepting traffic — see ChatContextInitializationHostedService.
                .AddHostedService<ChatContextInitializationHostedService<TChatContext>>();

            return services;
        }
    }
}