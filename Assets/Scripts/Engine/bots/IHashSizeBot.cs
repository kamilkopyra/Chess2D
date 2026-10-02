namespace ChessEngine
{
    // A bot with a transposition table whose size can be changed (UCI option "Hash")
    public interface IHashSizeBot
    {
        // Size of the transposition table in megabytes
        int HashMb { get; set; }

        // Empties the table (e.g. before a new game)
        void ClearTable();
    }
}
