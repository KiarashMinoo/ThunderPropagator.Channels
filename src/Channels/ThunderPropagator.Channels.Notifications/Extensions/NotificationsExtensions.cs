using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ThunderPropagator.Application.Channels;
using ThunderPropagator.Application.Feeders;
using ThunderPropagator.Channels.Notifications.Pipelines.Acknowledge;
using ThunderPropagator.Infrastructure.Extensions;
using ThunderPropagator.Channels.Notifications.Channel;
using ThunderPropagator.Channels.Notifications.Feeders;
using ThunderPropagator.Channels.Notifications.Messages;

namespace ThunderPropagator.Channels.Notifications.Extensions
{
    /// <summary>
    /// Dependency-injection registration for the Notifications channel and for feeder
    /// implementations that push notifications into it.
    /// </summary>
    public static class NotificationsExtensions
    {
        /// <summary>
        /// Registers a <see cref="NotificationsChannel{TNotificationsChannelConfiguration}"/> with a
        /// programmatically configured channel configuration.
        /// </summary>
        /// <typeparam name="TNotificationsChannelConfiguration">Concrete channel configuration type.</typeparam>
        /// <param name="services">The service collection to register into.</param>
        /// <param name="options">
        /// Optional callback to set channel configuration values. If omitted, the configuration is
        /// left at its defaults.
        /// </param>
        /// <returns><paramref name="services"/>, for chaining.</returns>
        /// <remarks>
        /// Also registers <c>NotificationsAcknowledgeReceiverPipeline</c> (see #77) — acknowledging
        /// delivery/read state is a core capability of this channel, not an opt-in extra, so every
        /// consumer registering the channel gets it automatically, the same way Chat bakes in all of
        /// its own receive pipelines unconditionally.
        /// </remarks>
        public static IServiceCollection AddNotificationsChannel<TNotificationsChannelConfiguration>(this IServiceCollection services, Action<TNotificationsChannelConfiguration>? options = null)
            where TNotificationsChannelConfiguration : AbstractChannelConfiguration, new()
        {
            var channelConfiguration = new TNotificationsChannelConfiguration();
            options?.Invoke(channelConfiguration);
            services.TryAddSingleton(channelConfiguration);

            services.AddChannel<NotificationsChannel<TNotificationsChannelConfiguration>>()
                .AddReceivePipeline<NotificationsChannel<TNotificationsChannelConfiguration>, NotificationsAcknowledgeReceiverPipeline<TNotificationsChannelConfiguration>>();

            return services;
        }

        /// <summary>
        /// Registers a <see cref="NotificationsChannel{TNotificationsChannelConfiguration}"/> with
        /// its channel configuration bound from <paramref name="configurationSection"/>.
        /// </summary>
        /// <typeparam name="TNotificationsChannelConfiguration">Concrete channel configuration type.</typeparam>
        /// <param name="services">The service collection to register into.</param>
        /// <param name="configurationSection">Configuration section to bind the channel configuration from.</param>
        /// <returns><paramref name="services"/>, for chaining.</returns>
        public static IServiceCollection AddNotificationsChannel<TNotificationsChannelConfiguration>(this IServiceCollection services, IConfigurationSection configurationSection)
            where TNotificationsChannelConfiguration : AbstractChannelConfiguration, new()
        {
            AddNotificationsChannel<TNotificationsChannelConfiguration>(services, configurationSection.Bind);
            return services;
        }

        /// <summary>
        /// Registers a consumer-authored feeder implementation for the Notifications channel, with a
        /// programmatically configured feeder configuration. Notifications is push-only and ships no
        /// feeder of its own — <typeparamref name="TFeeder"/> is supplied by the consumer and is
        /// expected to read and act on <typeparamref name="TNotificationsFeederConfiguration"/>'s
        /// settings (batching, deduplication, expiration, retry).
        /// </summary>
        /// <typeparam name="TFeeder">The consumer-authored feeder implementation.</typeparam>
        /// <typeparam name="TNotificationsChannelConfiguration">Concrete channel configuration type of the target channel.</typeparam>
        /// <typeparam name="TNotificationsFeederConfiguration">Concrete feeder configuration type.</typeparam>
        /// <param name="services">The service collection to register into.</param>
        /// <param name="options">
        /// Optional callback to set feeder configuration values. If omitted, the configuration is
        /// left at its defaults.
        /// </param>
        /// <returns><paramref name="services"/>, for chaining.</returns>
        public static IServiceCollection AddNotificationsChannelFeeder<TFeeder, TNotificationsChannelConfiguration, TNotificationsFeederConfiguration>(this IServiceCollection services, Action<TNotificationsFeederConfiguration>? options = null)
            where TFeeder : AbstractFeeder<NotificationsChannel<TNotificationsChannelConfiguration>, NotificationsChannelFeederMessage, TNotificationsFeederConfiguration>
            where TNotificationsChannelConfiguration : AbstractChannelConfiguration, new()
            where TNotificationsFeederConfiguration : NotificationsFeederConfiguration, new()
        {
            services.AddChannelFeeder<NotificationsChannel<TNotificationsChannelConfiguration>, TFeeder, NotificationsChannelFeederMessage, TNotificationsFeederConfiguration>(options);
            return services;
        }

        /// <summary>
        /// Registers a consumer-authored feeder implementation for the Notifications channel, with
        /// its feeder configuration bound from <paramref name="feederConfigurationSection"/>.
        /// </summary>
        /// <typeparam name="TFeeder">The consumer-authored feeder implementation.</typeparam>
        /// <typeparam name="TNotificationsChannelConfiguration">Concrete channel configuration type of the target channel.</typeparam>
        /// <typeparam name="TNotificationsFeederConfiguration">Concrete feeder configuration type.</typeparam>
        /// <param name="services">The service collection to register into.</param>
        /// <param name="feederConfigurationSection">Configuration section to bind the feeder configuration from.</param>
        /// <returns><paramref name="services"/>, for chaining.</returns>
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