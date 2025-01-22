using RapidStreamer.Application.Collections;

namespace RapidStreamer.Channels.Demo.Portfolio.Dtos
{
    internal
#if !DEBUG
        sealed
#endif
        class PortfolioResponseDto : ResponseContentFormCollection
    {
        public required string Echo { get; init; }
    }
}