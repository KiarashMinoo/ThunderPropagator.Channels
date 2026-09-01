using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels.Contexts;
using ThunderPropagator.Application.Events.Receivers;
using ThunderPropagator.Channels.Games.RockPaperScissors.Channel;

namespace ThunderPropagator.Channels.Games.RockPaperScissors
{
    internal
#if !DEBUG
        sealed
#endif
        partial class RockPaperScissorsChannelReceiveEvent : AbstractReceiveEvent<RockPaperScissorsChannel>
    {
        private readonly RockPaperScissorsComputer _rockPaperScissorsComputer;

        public RockPaperScissorsChannelReceiveEvent(RockPaperScissorsComputer rockPaperScissorsComputer,
            ILoggerFactory loggerFactory)
            : base(loggerFactory)
        {
            _rockPaperScissorsComputer = rockPaperScissorsComputer;
        }

        /// <summary>
        /// Issue #12: this was previously <c>async void InternalInvoke</c>, so any exception thrown while
        /// processing a subscription was posted to the synchronization context and unobservable by any
        /// caller — crashing the process in ASP.NET Core, or silently vanishing in other hosts. It was
        /// also unreachable dead code regardless: its own call site
        /// (<c>context.Response.Subscriptions.ForEach(...)</c>) was written against a <c>Subscriptions</c>
        /// collection <see cref="ResponseContext"/> no longer exposes in this package version —
        /// <see cref="ResponseContext"/> now identifies exactly one connection per receive event
        /// (<see cref="ResponseContext.ConnectionId"/>), which
        /// <see cref="RockPaperScissorsComputer.HandleSubscription"/> looks up from the channel's own
        /// live subscription registry. Every fault is now caught and logged here instead — matching
        /// #12's own suggested fix, "wrap in a try/catch that logs the error" — rather than either
        /// crashing the host or disappearing.
        /// </summary>
        public async Task Invoke(ReceiveContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                await _rockPaperScissorsComputer.HandleSubscriptionAsync(context.Response.ConnectionId, cancellationToken);
            }
            catch (Exception ex)
            {
                Log.SubscriptionHandlingFailed(Logger, ex, context.Response.ConnectionId);
            }
        }

        // Issue #39: LoggerMessage-generated method for this event's log call site. EventId 3001 is
        // this file's own block; no cross-file EventId registry exists yet in this repo.
        private static partial class Log
        {
            /// <summary>Logs that handling a connection's subscription failed.</summary>
            [LoggerMessage(EventId = 3001, Level = LogLevel.Error, Message = "RockPaperScissorsChannelReceiveEvent failed while processing connection {ConnectionId}.")]
            public static partial void SubscriptionHandlingFailed(ILogger logger, Exception exception, string connectionId);
        }
    }
}
