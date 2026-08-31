using ThunderPropagator.Application.Collections;
using ThunderPropagator.BuildingBlocks.Application.Collections;

namespace ThunderPropagator.Channels.Demo.Portfolio.Pipelines.Dtos
{
    internal
#if !DEBUG
        sealed
#endif
        class PortfolioDemoChannelReceiverPipelineRequestDto : BindingDictionary<string, object>, IRequestContentFormCollection
    {
        // Issue #36: Key used to be supplied here and trusted directly to search/mutate a snapshot
        // entry, with no check tying it to the calling connection — any caller could buy/sell against
        // any other subscriber's position just by knowing (or guessing) their Key. Buy/Sell now
        // resolve the caller's own Key from PortfolioDemoChannel.FindSubscribedKey instead, so it's no
        // longer part of the request at all.
        public required string Stock
        {
            get => (string)this[nameof(Stock)];
            set => this[nameof(Stock)] = value;
        }

        public bool IsBuy
        {
            get => (bool)this[nameof(IsBuy)];
            set => this[nameof(IsBuy)] = value;
        }

        public int Quantity
        {
            get => (int)this[nameof(Quantity)];
            set => this[nameof(Quantity)] = value;
        }
    }
}