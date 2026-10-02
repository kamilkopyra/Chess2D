namespace ChessEngine
{
    // A bot that reports details of its last search (for UCI "info" lines and game analysis)
    public interface ISearchInfo
    {
        // Score of the move played, from the side to move's point of view, of the deepest completed iteration
        // (centipawns; mates are BotBase.MateScore minus the distance in plies). 0 for a book move.
        int LastScore { get; }

        // Nodes searched for the last move
        long LastNodes { get; }
    }
}
