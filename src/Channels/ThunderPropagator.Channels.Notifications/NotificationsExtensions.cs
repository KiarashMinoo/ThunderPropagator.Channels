using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ThunderPropagator.Application.Channels;
using ThunderPropagator.Application.Feeders;
using ThunderPropagator.Infrastructure.Extensions;

namespace ThunderPropagator.Channels.Notifications
{
    public static class NotificationsExtensions
    {
        public static IServiceCollection AddNotificationsChannel<TNotificationsChannelConfiguration>(this IServiceCollection services, Action<TNotificationsChannelConfiguration>? options = null)
            where TNotificationsChannelConfiguration : AbstractChannelConfiguration, new()
        {
            var channelConfiguration = new TNotificationsChannelConfiguration();
            options?.Invoke(channelConfiguration);
            services.TryAddSingleton(channelConfiguration);

            services.AddChannel<NotificationsChannel<TNotificationsChannelConfiguration>>();

            return services;
        }

        public static IServiceCollection AddNotificationsChannel<TNotificationsChannelConfiguration>(this IServiceCollection services, IConfigurationSection configurationSection)
            where TNotificationsChannelConfiguration : AbstractChannelConfiguration, new()
        {
            AddNotificationsChannel<TNotificationsChannelConfiguration>(services, configurationSection.Bind);
            return services;
        }

        public static IServiceCollection AddNotificationsChannelFeeder<TFeeder, TNotificationsChannelConfiguration, TNotificationsFeederConfiguration>(this IServiceCollection services, Action<TNotificationsFeederConfiguration>? options = null)
            where TFeeder : AbstractFeeder<NotificationsChannel<TNotificationsChannelConfiguration>, NotificationsChannelFeederMessage, TNotificationsFeederConfiguration>
            where TNotificationsChannelConfiguration : AbstractChannelConfiguration, new()
            where TNotificationsFeederConfiguration : NotificationsFeederConfiguration, new()
        {
            services.AddChannelFeeder<NotificationsChannel<TNotificationsChannelConfiguration>, TFeeder, NotificationsChannelFeederMessage, TNotificationsFeederConfiguration>(options);
            return services;
        }

        public static IServiceCollection AddNotificationsChannelFeeder<TFeeder, TNotificationsChannelConfiguration, TNotificationsFeederConfiguration>(this IServiceCollection services, IConfigurationSection feederConfigurationSection)
            where TFeeder : AbstractFeeder<NotificationsChannel<TNotificationsChannelConfiguration>, NotificationsChannelFeederMessage, TNotificationsFeederConfiguration>
            where TNotificationsChannelConfiguration : AbstractChannelConfiguration, new()
            where TNotificationsFeederConfiguration : NotificationsFeederConfiguration, new()
        {
            AddNotificationsChannelFeeder<TFeeder, TNotificationsChannelConfiguration, TNotificationsFeederConfiguration>(services, feederConfigurationSection.Bind);
            return services;
        }
    }
}