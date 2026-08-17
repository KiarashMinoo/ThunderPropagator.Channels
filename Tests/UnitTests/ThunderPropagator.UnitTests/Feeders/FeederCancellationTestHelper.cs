using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ThunderPropagator.Application.Channels;
using ThunderPropagator.Application.Feeders;
using ThunderPropagator.BuildingBlocks.Application;

namespace ThunderPropagator.UnitTests.Feeders
{
    /// <summary>
    /// Castle/NSubstitute can't proxy <see cref="IFeederHandler{TChannel,TFeederMessage}"/> when
    /// <c>TFeederMessage</c> is an internal type (every real feeder message is internal by convention) —
    /// the generated proxy assembly isn't granted access. A hand-written no-op stands in instead.
    /// </summary>
    internal sealed class NoOpFeederHandler<TChannel, TFeederMessage> : IFeederHandler<TChannel, TFeederMessage>
        where TChannel : class, IChannel
        where TFeederMessage : FeederMessage
    {
        public Task HandleAsync(TFeederMessage feederMessage, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    /// <summary>
    /// Same Castle/internal-type limitation as <see cref="NoOpFeederHandler{TChannel,TFeederMessage}"/>,
    /// for the deserializer resolved via <see cref="FeederMessageDeserializerResolver{TFeederMessage,TFeederConfiguration}"/>.
    /// </summary>
    internal sealed class NoOpFeederMessageDeserializer<TFeederMessage, TFeederConfiguration> : IFeederMessageDeserializer<TFeederMessage, TFeederConfiguration>
        where TFeederMessage : FeederMessage
        where TFeederConfiguration : class, IAbstractFeederConfiguration
    {
        public TFeederMessage? Deserialize(string data, CancellationToken cancellationToken = default) => null;
        public TFeederMessage? Deserialize(byte[] bytes, CancellationToken cancellationToken = default) => null;
        public void DeserializeInto(string data, FeederMessage target, CancellationToken cancellationToken = default) { }
        public void DeserializeInto(byte[] bytes, FeederMessage target, CancellationToken cancellationToken = default) { }
    }


    /// <summary>
    /// Feeders resolve <see cref="IHostApplicationLifetime"/>, <see cref="ILoggerFactory"/> and a
    /// <see cref="FeederMessageDeserializerResolver{TFeederMessage,TFeederConfiguration}"/> from the DI
    /// container deep in base-class constructors (AbstractChannel/AbstractFeeder/DelegativeFeeder). These
    /// helpers assemble the minimal service provider needed to construct a feeder for unit testing, and
    /// invoke its protected <c>ReceiveAsync(CancellationToken)</c> via reflection since it isn't otherwise
    /// reachable from outside the class hierarchy.
    /// </summary>
    internal static class FeederCancellationTestHelper
    {
        public static IServiceProvider BuildServiceProvider<TFeederMessage, TFeederConfiguration>()
            where TFeederMessage : FeederMessage
            where TFeederConfiguration : class, IAbstractFeederConfiguration
        {
            var serviceProvider = Substitute.For<IServiceProvider>();

            serviceProvider.GetService(typeof(IHostApplicationLifetime)).Returns(Substitute.For<IHostApplicationLifetime>());
            serviceProvider.GetService(typeof(ILoggerFactory)).Returns(NullLoggerFactory.Instance);

            FeederMessageDeserializerResolver<TFeederMessage, TFeederConfiguration> resolver =
                _ => new NoOpFeederMessageDeserializer<TFeederMessage, TFeederConfiguration>();
            serviceProvider.GetService(typeof(FeederMessageDeserializerResolver<TFeederMessage, TFeederConfiguration>)).Returns(resolver);

            return serviceProvider;
        }

        public static void RegisterChannelConfiguration<TChannelConfiguration>(this IServiceProvider serviceProvider, TChannelConfiguration configuration)
            where TChannelConfiguration : class
            => serviceProvider.GetService(typeof(TChannelConfiguration)).Returns(configuration);

        public static void RegisterService<TService>(this IServiceProvider serviceProvider, TService service)
            where TService : class
            => serviceProvider.GetService(typeof(TService)).Returns(service);

        public static IAsyncEnumerable<FeederReceivedMessage<TFeederMessage>> InvokeReceiveAsync<TFeederMessage>(object feeder, CancellationToken cancellationToken)
            where TFeederMessage : FeederMessage
        {
            var receiveAsyncMethod = feeder.GetType().GetMethod("ReceiveAsync", BindingFlags.NonPublic | BindingFlags.Instance, [typeof(CancellationToken)])
                ?? throw new MissingMethodException(feeder.GetType().FullName, "ReceiveAsync");

            return (IAsyncEnumerable<FeederReceivedMessage<TFeederMessage>>)receiveAsyncMethod.Invoke(feeder, [cancellationToken])!;
        }

        /// <summary>
        /// Starts enumerating <paramref name="feeder"/>'s <c>ReceiveAsync</c> sequence, cancels
        /// <paramref name="cancellationTokenSource"/> shortly after (while the feeder's iteration delay is
        /// still pending) and asserts the enumeration is cancelled promptly rather than waiting out the delay.
        /// </summary>
        public static async Task AssertCancelledDuringDelayAsync<TFeederMessage>(object feeder, CancellationTokenSource cancellationTokenSource, TimeSpan cancelAfter, TimeSpan promptTimeout)
            where TFeederMessage : FeederMessage
        {
            var enumerator = InvokeReceiveAsync<TFeederMessage>(feeder, cancellationTokenSource.Token).GetAsyncEnumerator(cancellationTokenSource.Token);

            var moveNextTask = enumerator.MoveNextAsync().AsTask();

            await Task.Delay(cancelAfter);
            await cancellationTokenSource.CancelAsync();

            var firstToComplete = await Task.WhenAny(moveNextTask, Task.Delay(promptTimeout));

            Assert.Same(moveNextTask, firstToComplete);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => moveNextTask);
        }
    }
}
