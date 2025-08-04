using RapidStreamer.Application.Collections;
using RapidStreamer.BuildingBlocks.Application.Collections;

namespace RapidStreamer.Channels.Demo.Portfolio.Pipelines.Dtos
{
    internal
#if !DEBUG
        sealed
#endif
        class PortfolioDemoChannelReceiverPipelineRequestDto : BindingDictionary<string, object>, IRequestContentFormCollection
    {
        public required string Key
        {
            get => (string)this[nameof(Key)];
            set => this[nameof(Key)] = value;
        }

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