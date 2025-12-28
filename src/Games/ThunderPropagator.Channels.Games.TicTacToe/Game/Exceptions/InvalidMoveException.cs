namespace ThunderPropagator.Channels.Games.TicTacToe.Game.Exceptions
{
    public
#if !DEBUG
        sealed
#endif
        class InvalidMoveException : Exception
    {
    }
}