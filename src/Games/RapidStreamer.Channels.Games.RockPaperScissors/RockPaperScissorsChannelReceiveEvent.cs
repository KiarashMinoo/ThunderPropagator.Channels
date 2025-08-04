using Microsoft.Extensions.Logging;
using RapidStreamer.Application.Channels.Contexts;
using RapidStreamer.Application.Channels.Subscribers;
using RapidStreamer.Application.Events.Receivers;
using RapidStreamer.BuildingBlocks.Application.Helpers;

namespace RapidStreamer.Channels.Games.RockPaperScissors
{
    internal
#if !DEBUG
        sealed
#endif
        class RockPaperScissorsChannelReceiveEvent : AbstractReceiveEvent<RockPaperScissorsChannel>
    {
        private readonly RockPaperScissorsComputer _rockPaperScissorsComputer;

        public RockPaperScissorsChannelReceiveEvent(RockPaperScissorsComputer rockPaperScissorsComputer,
            ILoggerFactory loggerFactory)
            : base(loggerFactory)
        {
            _rockPaperScissorsComputer = rockPaperScissorsComputer;
        }

        public Task Invoke(ReceiveContext context, CancellationToken cancellationToken = default)
        {
            context.Response.Subscriptions.ForEach(InternalInvoke);

            return Task.CompletedTask;

            async void InternalInvoke(Subscription subscription)
            {
                var player = new Player(subscription);

                if (player.PlayerType == PlayerType.Computer)
                {
                    await _rockPaperScissorsComputer.PlayWithComputer(new Player(subscription), cancellationToken);
                }
                else
                {
                    await _rockPaperScissorsComputer.PlayWithHuman(new Player(subscription), cancellationToken);
                }
            }
        }
    }
}