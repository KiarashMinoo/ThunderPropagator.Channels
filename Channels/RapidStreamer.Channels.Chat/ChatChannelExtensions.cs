using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RapidStreamer.Channels.Chat.Models;
using RapidStreamer.Channels.Chat.Models.Groups;
using RapidStreamer.Channels.Chat.Models.Messages;
using RapidStreamer.Channels.Chat.Models.Users;
using RapidStreamer.Channels.Chat.Pipelines.CreateGroup;
using RapidStreamer.Channels.Chat.Pipelines.GetGroups;
using RapidStreamer.Channels.Chat.Pipelines.Login;
using RapidStreamer.Channels.Chat.Pipelines.Register;
using RapidStreamer.Infrastructure.Extensions;

namespace RapidStreamer.Channels.Chat
{
    public static class ChatChannelExtensions
    {
        public static IServiceCollection AddChatChannel<TChatContext>
        (
            this IServiceCollection services,
            Action<IServiceProvider, DbContextOptionsBuilder> optionsAction
        )
            where TChatContext : BaseChatContext<TChatContext>
        {
            services.AddChannel<ChatChannel>()
                .AddReceivePipeline<ChatChannel, ChatChannelCreateGroupReceiverPipeline>()
                .AddReceivePipeline<ChatChannel, ChatChannelGetGroupsReceiverPipeline>()
                .AddReceivePipeline<ChatChannel, ChatChannelLoginReceiverPipeline>()
                .AddReceivePipeline<ChatChannel, ChatChannelRegisterReceiverPipeline>()
                .AddDbContextPool<IChatContext, TChatContext>(optionsAction)
                .AddScoped<GroupService>()
                .AddScoped<MessageService>()
                .AddScoped<UserService>();

            return services;
        }
    }
}