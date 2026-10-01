namespace ChessEngine
{
    // A bot that searches for a given time instead of a fixed depth
    public interface ITimedBot
    {
        // Time budget for one move, in milliseconds
        int MoveTimeMs { get; set; }

        // Deepest fully completed search depth of the last move (for display and UCI "info")
        int LastDepth { get; }
    }
}
