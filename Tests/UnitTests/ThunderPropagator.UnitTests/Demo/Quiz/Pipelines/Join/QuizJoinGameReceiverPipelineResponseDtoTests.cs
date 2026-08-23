using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ThunderPropagator.Application.Connections;
using ThunderPropagator.Channels.Demo.Quiz;
using ThunderPropagator.Channels.Demo.Quiz.Game;
using ThunderPropagator.Channels.Demo.Quiz.Pipelines.Join;

namespace ThunderPropagator.UnitTests.Demo.Quiz.Pipelines.Join
{
    public sealed class QuizJoinGameReceiverPipelineResponseDtoTests
    {
        private static QuizJoinResult CreateAJoinResult()
        {
            var sessionStore = new QuizGameSessionStore();
            sessionStore.GetOrCreateSession("game-1");

            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IHostApplicationLifetime)).Returns(Substitute.For<IHostApplicationLifetime>());
            serviceProvider.GetService(typeof(ILoggerFactory)).Returns(NullLoggerFactory.Instance);
            serviceProvider.GetService(typeof(QuizChannelConfiguration)).Returns(new QuizChannelConfiguration());
            serviceProvider.GetService(typeof(QuizGameSessionStore)).Returns(sessionStore);

            var channel = new QuizChannel(serviceProvider);
            channel.Initialize(CancellationToken.None);

            var connectionInfo = Substitute.For<IConnectionInfo>();
            connectionInfo.ConnectionId.Returns("conn-1");

            return channel.Join(connectionInfo, "request-1", "game-1", "Alice");
        }

        [Fact]
        public void Properties_RoundTripTheAssignedValues()
        {
            var joinResult = CreateAJoinResult();

            var dto = new QuizJoinGameReceiverPipelineResponseDto
            {
                Subscription = joinResult.Subscription,
                IsReconnect = joinResult.IsReconnect,
                IsHost = joinResult.IsHost,
                PlayerName = joinResult.PlayerName
            };

            Assert.Same(joinResult.Subscription, dto.Subscription);
            Assert.Equal(joinResult.IsReconnect, dto.IsReconnect);
            Assert.Equal(joinResult.IsHost, dto.IsHost);
            Assert.Equal(joinResult.PlayerName, dto.PlayerName);
        }
    }
}
