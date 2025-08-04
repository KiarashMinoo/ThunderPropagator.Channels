using RapidStreamer.Application.Collections;

namespace RapidStreamer.Channels.Games.TicTacToe.Pipelines.GetGames
{
    internal
#if !DEBUG
        sealed
#endif
        record GetGamesItemResponseDto(string SessionId, string PlayerName);

    internal
#if !DEBUG
        sealed
#endif
        class TicTacToeChannelGetGamesReceiverPipelineResponseDto : ResponseContentFormCollection
    {
        public required IEnumerable<GetGamesItemResponseDto> Items { get; init; }
    }
}