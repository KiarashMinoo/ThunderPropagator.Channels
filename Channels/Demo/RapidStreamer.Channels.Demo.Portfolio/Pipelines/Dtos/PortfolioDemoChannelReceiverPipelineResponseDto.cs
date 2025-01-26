using RapidStreamer.Application.Collections;

namespace RapidStreamer.Channels.Demo.Portfolio.Dtos
{
    internal
#if !DEBUG
        sealed
#endif
        class PortfolioDemoChannelReceiverPipelineResponseDto : ResponseContentFormCollection
    {
        public required string Echo { get; init; }
    }
}