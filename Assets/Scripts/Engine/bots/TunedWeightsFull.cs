namespace ChessEngine
{
    // Evaluation weights found by Texel tuning (Tools/Chess2D.Tune), in the layout of TunableEvaluation:
    // Weights[2 * term] for the middlegame, Weights[2 * term + 1] for the endgame.
    // Feature set: Full (TunableEvaluation.Evaluate(position, weights, FeatureSet.Full)).
    // 2501077 positions, K = 0.8665, error 0.066422 -> 0.059665
    public static class TunedWeightsFull
    {
        public static readonly int[] Weights =
        {
               95,   109,   // Material Pawn
              298,   433,   // Material Knight
              337,   473,   // Material Bishop
              405,   756,   // Material Rook
              912,  1274,   // Material Queen
                0,     0,   // PieceSquare Pawn 0
                0,     0,   // PieceSquare Pawn 1
                0,     0,   // PieceSquare Pawn 2
                0,     0,   // PieceSquare Pawn 3
                0,     0,   // PieceSquare Pawn 4
                0,     0,   // PieceSquare Pawn 5
                0,     0,   // PieceSquare Pawn 6
                0,     0,   // PieceSquare Pawn 7
              104,    44,   // PieceSquare Pawn 8
               45,    92,   // PieceSquare Pawn 9
               92,    85,   // PieceSquare Pawn 10
               74,    92,   // PieceSquare Pawn 11
              118,    72,   // PieceSquare Pawn 12
               90,    75,   // PieceSquare Pawn 13
               40,   111,   // PieceSquare Pawn 14
               29,    79,   // PieceSquare Pawn 15
               53,    42,   // PieceSquare Pawn 16
               28,    58,   // PieceSquare Pawn 17
               51,    42,   // PieceSquare Pawn 18
               52,    35,   // PieceSquare Pawn 19
               71,    51,   // PieceSquare Pawn 20
               84,    50,   // PieceSquare Pawn 21
               64,    54,   // PieceSquare Pawn 22
               32,    60,   // PieceSquare Pawn 23
               31,    42,   // PieceSquare Pawn 24
               21,    26,   // PieceSquare Pawn 25
               14,    13,   // PieceSquare Pawn 26
               12,     0,   // PieceSquare Pawn 27
               34,    -3,   // PieceSquare Pawn 28
               34,    10,   // PieceSquare Pawn 29
               22,    27,   // PieceSquare Pawn 30
               17,    35,   // PieceSquare Pawn 31
               20,    30,   // PieceSquare Pawn 32
                4,    29,   // PieceSquare Pawn 33
                2,    16,   // PieceSquare Pawn 34
               -3,     9,   // PieceSquare Pawn 35
                3,    12,   // PieceSquare Pawn 36
               -3,    18,   // PieceSquare Pawn 37
                6,    17,   // PieceSquare Pawn 38
                1,    16,   // PieceSquare Pawn 39
                6,    16,   // PieceSquare Pawn 40
              -14,    22,   // PieceSquare Pawn 41
              -15,    20,   // PieceSquare Pawn 42
              -20,    23,   // PieceSquare Pawn 43
              -21,    25,   // PieceSquare Pawn 44
              -22,    23,   // PieceSquare Pawn 45
               -6,     7,   // PieceSquare Pawn 46
               -6,     5,   // PieceSquare Pawn 47
               20,    19,   // PieceSquare Pawn 48
                3,    22,   // PieceSquare Pawn 49
                1,    26,   // PieceSquare Pawn 50
               -5,    13,   // PieceSquare Pawn 51
              -13,    44,   // PieceSquare Pawn 52
                2,    35,   // PieceSquare Pawn 53
               14,    13,   // PieceSquare Pawn 54
                3,     1,   // PieceSquare Pawn 55
                0,     0,   // PieceSquare Pawn 56
                0,     0,   // PieceSquare Pawn 57
                0,     0,   // PieceSquare Pawn 58
                0,     0,   // PieceSquare Pawn 59
                0,     0,   // PieceSquare Pawn 60
                0,     0,   // PieceSquare Pawn 61
                0,     0,   // PieceSquare Pawn 62
                0,     0,   // PieceSquare Pawn 63
             -169,   -58,   // PieceSquare Knight 0
              -53,    34,   // PieceSquare Knight 1
              -67,    35,   // PieceSquare Knight 2
             -235,    67,   // PieceSquare Knight 3
               -7,    24,   // PieceSquare Knight 4
             -138,    69,   // PieceSquare Knight 5
              130,   -17,   // PieceSquare Knight 6
             -181,   -78,   // PieceSquare Knight 7
                3,     3,   // PieceSquare Knight 8
                9,    15,   // PieceSquare Knight 9
               -4,    17,   // PieceSquare Knight 10
               69,    -6,   // PieceSquare Knight 11
               56,     2,   // PieceSquare Knight 12
               55,   -17,   // PieceSquare Knight 13
                4,    25,   // PieceSquare Knight 14
               28,    -7,   // PieceSquare Knight 15
              -25,     3,   // PieceSquare Knight 16
               -4,    15,   // PieceSquare Knight 17
               12,    32,   // PieceSquare Knight 18
               27,    11,   // PieceSquare Knight 19
               59,     6,   // PieceSquare Knight 20
               74,    28,   // PieceSquare Knight 21
               23,    20,   // PieceSquare Knight 22
               15,    11,   // PieceSquare Knight 23
                7,    33,   // PieceSquare Knight 24
                0,    12,   // PieceSquare Knight 25
                4,    39,   // PieceSquare Knight 26
               33,    36,   // PieceSquare Knight 27
                6,    36,   // PieceSquare Knight 28
               16,    38,   // PieceSquare Knight 29
               14,     6,   // PieceSquare Knight 30
               34,    18,   // PieceSquare Knight 31
              -13,     9,   // PieceSquare Knight 32
              -14,    18,   // PieceSquare Knight 33
                4,    47,   // PieceSquare Knight 34
                3,    45,   // PieceSquare Knight 35
               16,    44,   // PieceSquare Knight 36
               27,    31,   // PieceSquare Knight 37
               29,    17,   // PieceSquare Knight 38
                4,    28,   // PieceSquare Knight 39
              -40,    10,   // PieceSquare Knight 40
              -18,    13,   // PieceSquare Knight 41
              -15,    14,   // PieceSquare Knight 42
               -5,    40,   // PieceSquare Knight 43
               13,    36,   // PieceSquare Knight 44
               -7,     1,   // PieceSquare Knight 45
                8,     3,   // PieceSquare Knight 46
              -11,    16,   // PieceSquare Knight 47
              -48,    11,   // PieceSquare Knight 48
              -50,    34,   // PieceSquare Knight 49
              -16,     4,   // PieceSquare Knight 50
               -6,     9,   // PieceSquare Knight 51
               -5,    18,   // PieceSquare Knight 52
               -1,     9,   // PieceSquare Knight 53
               -3,    29,   // PieceSquare Knight 54
              -20,     3,   // PieceSquare Knight 55
              -83,   -10,   // PieceSquare Knight 56
              -40,     4,   // PieceSquare Knight 57
              -53,    30,   // PieceSquare Knight 58
              -22,    27,   // PieceSquare Knight 59
              -17,    43,   // PieceSquare Knight 60
              -23,    14,   // PieceSquare Knight 61
              -40,     1,   // PieceSquare Knight 62
              -68,    29,   // PieceSquare Knight 63
              -39,    36,   // PieceSquare Bishop 0
              -21,    48,   // PieceSquare Bishop 1
              -61,    33,   // PieceSquare Bishop 2
              -31,    38,   // PieceSquare Bishop 3
              -64,    36,   // PieceSquare Bishop 4
              -83,    39,   // PieceSquare Bishop 5
              -28,    28,   // PieceSquare Bishop 6
              -29,    39,   // PieceSquare Bishop 7
              -20,    53,   // PieceSquare Bishop 8
              -28,    42,   // PieceSquare Bishop 9
               12,    23,   // PieceSquare Bishop 10
                9,    21,   // PieceSquare Bishop 11
               10,    26,   // PieceSquare Bishop 12
               -5,    34,   // PieceSquare Bishop 13
              -14,    45,   // PieceSquare Bishop 14
              -37,    35,   // PieceSquare Bishop 15
                7,    36,   // PieceSquare Bishop 16
               10,    50,   // PieceSquare Bishop 17
                4,    42,   // PieceSquare Bishop 18
               37,    26,   // PieceSquare Bishop 19
               37,    27,   // PieceSquare Bishop 20
               84,    41,   // PieceSquare Bishop 21
               13,    64,   // PieceSquare Bishop 22
               20,    42,   // PieceSquare Bishop 23
              -11,    29,   // PieceSquare Bishop 24
               -2,    51,   // PieceSquare Bishop 25
                7,    45,   // PieceSquare Bishop 26
               36,    37,   // PieceSquare Bishop 27
               21,    44,   // PieceSquare Bishop 28
               19,    43,   // PieceSquare Bishop 29
               16,    30,   // PieceSquare Bishop 30
               -3,    34,   // PieceSquare Bishop 31
               -5,    25,   // PieceSquare Bishop 32
               -6,    31,   // PieceSquare Bishop 33
               -6,    47,   // PieceSquare Bishop 34
               20,    46,   // PieceSquare Bishop 35
               16,    34,   // PieceSquare Bishop 36
                1,    28,   // PieceSquare Bishop 37
               -4,    14,   // PieceSquare Bishop 38
               25,   -12,   // PieceSquare Bishop 39
               -7,     7,   // PieceSquare Bishop 40
               10,    33,   // PieceSquare Bishop 41
               -1,    45,   // PieceSquare Bishop 42
               -4,    42,   // PieceSquare Bishop 43
               -2,    59,   // PieceSquare Bishop 44
              -13,    31,   // PieceSquare Bishop 45
               10,    10,   // PieceSquare Bishop 46
                7,     4,   // PieceSquare Bishop 47
                1,    21,   // PieceSquare Bishop 48
                4,     2,   // PieceSquare Bishop 49
               12,     6,   // PieceSquare Bishop 50
              -13,    34,   // PieceSquare Bishop 51
               -4,    24,   // PieceSquare Bishop 52
                7,     8,   // PieceSquare Bishop 53
               10,     8,   // PieceSquare Bishop 54
               16,   -38,   // PieceSquare Bishop 55
                6,    -8,   // PieceSquare Bishop 56
                9,    -8,   // PieceSquare Bishop 57
              -13,    23,   // PieceSquare Bishop 58
              -30,    23,   // PieceSquare Bishop 59
                3,    29,   // PieceSquare Bishop 60
              -24,    30,   // PieceSquare Bishop 61
               -2,    12,   // PieceSquare Bishop 62
              -19,    13,   // PieceSquare Bishop 63
               -5,    86,   // PieceSquare Rook 0
               24,    80,   // PieceSquare Rook 1
               14,    92,   // PieceSquare Rook 2
               13,   100,   // PieceSquare Rook 3
               40,    85,   // PieceSquare Rook 4
               55,    76,   // PieceSquare Rook 5
               27,    99,   // PieceSquare Rook 6
               75,    65,   // PieceSquare Rook 7
              -36,    36,   // PieceSquare Rook 8
              -27,    35,   // PieceSquare Rook 9
               -6,    29,   // PieceSquare Rook 10
                6,    29,   // PieceSquare Rook 11
               18,    12,   // PieceSquare Rook 12
               88,    -3,   // PieceSquare Rook 13
               27,    11,   // PieceSquare Rook 14
               33,     7,   // PieceSquare Rook 15
              -36,   102,   // PieceSquare Rook 16
                8,    67,   // PieceSquare Rook 17
                2,    78,   // PieceSquare Rook 18
               19,    63,   // PieceSquare Rook 19
               53,    45,   // PieceSquare Rook 20
               89,    43,   // PieceSquare Rook 21
               81,    38,   // PieceSquare Rook 22
               29,    58,   // PieceSquare Rook 23
              -43,    87,   // PieceSquare Rook 24
              -11,    73,   // PieceSquare Rook 25
              -13,    71,   // PieceSquare Rook 26
                5,    61,   // PieceSquare Rook 27
              -10,    68,   // PieceSquare Rook 28
               30,    50,   // PieceSquare Rook 29
               30,    47,   // PieceSquare Rook 30
               10,    60,   // PieceSquare Rook 31
              -50,    71,   // PieceSquare Rook 32
              -41,    76,   // PieceSquare Rook 33
              -36,    75,   // PieceSquare Rook 34
              -12,    51,   // PieceSquare Rook 35
              -15,    50,   // PieceSquare Rook 36
              -24,    63,   // PieceSquare Rook 37
               34,    29,   // PieceSquare Rook 38
              -20,    47,   // PieceSquare Rook 39
              -47,    64,   // PieceSquare Rook 40
              -33,    54,   // PieceSquare Rook 41
              -38,    56,   // PieceSquare Rook 42
              -27,    43,   // PieceSquare Rook 43
              -13,    37,   // PieceSquare Rook 44
               -9,    35,   // PieceSquare Rook 45
               19,     9,   // PieceSquare Rook 46
                4,    11,   // PieceSquare Rook 47
              -50,    46,   // PieceSquare Rook 48
              -48,    48,   // PieceSquare Rook 49
              -33,    44,   // PieceSquare Rook 50
              -23,    39,   // PieceSquare Rook 51
              -15,    24,   // PieceSquare Rook 52
               -2,    27,   // PieceSquare Rook 53
               10,     9,   // PieceSquare Rook 54
              -25,    20,   // PieceSquare Rook 55
              -38,    63,   // PieceSquare Rook 56
              -30,    42,   // PieceSquare Rook 57
              -27,    59,   // PieceSquare Rook 58
              -16,    36,   // PieceSquare Rook 59
               -6,    29,   // PieceSquare Rook 60
              -10,    43,   // PieceSquare Rook 61
                9,    17,   // PieceSquare Rook 62
              -14,    32,   // PieceSquare Rook 63
              -74,   226,   // PieceSquare Queen 0
              -28,   155,   // PieceSquare Queen 1
                7,   157,   // PieceSquare Queen 2
                5,   187,   // PieceSquare Queen 3
                7,   201,   // PieceSquare Queen 4
               55,   187,   // PieceSquare Queen 5
              -17,   211,   // PieceSquare Queen 6
              -20,   212,   // PieceSquare Queen 7
              -23,   164,   // PieceSquare Queen 8
              -36,   184,   // PieceSquare Queen 9
              -23,   206,   // PieceSquare Queen 10
                2,   183,   // PieceSquare Queen 11
              -25,   228,   // PieceSquare Queen 12
               57,   173,   // PieceSquare Queen 13
              -32,   228,   // PieceSquare Queen 14
               31,   185,   // PieceSquare Queen 15
              -24,   172,   // PieceSquare Queen 16
               12,   162,   // PieceSquare Queen 17
              -14,   193,   // PieceSquare Queen 18
                4,   181,   // PieceSquare Queen 19
               17,   229,   // PieceSquare Queen 20
               43,   243,   // PieceSquare Queen 21
               46,   194,   // PieceSquare Queen 22
               21,   196,   // PieceSquare Queen 23
               -9,   139,   // PieceSquare Queen 24
              -14,   186,   // PieceSquare Queen 25
               -1,   167,   // PieceSquare Queen 26
              -19,   226,   // PieceSquare Queen 27
               -6,   217,   // PieceSquare Queen 28
               16,   199,   // PieceSquare Queen 29
               11,   189,   // PieceSquare Queen 30
               11,   177,   // PieceSquare Queen 31
               -8,   138,   // PieceSquare Queen 32
              -19,   154,   // PieceSquare Queen 33
              -15,   183,   // PieceSquare Queen 34
              -11,   193,   // PieceSquare Queen 35
               -5,   182,   // PieceSquare Queen 36
                2,   161,   // PieceSquare Queen 37
               17,   161,   // PieceSquare Queen 38
                6,   156,   // PieceSquare Queen 39
               -6,   103,   // PieceSquare Queen 40
               -6,   119,   // PieceSquare Queen 41
               -3,   132,   // PieceSquare Queen 42
              -15,   157,   // PieceSquare Queen 43
               -5,   154,   // PieceSquare Queen 44
                0,   152,   // PieceSquare Queen 45
               23,   108,   // PieceSquare Queen 46
               13,   130,   // PieceSquare Queen 47
              -22,   110,   // PieceSquare Queen 48
              -10,   103,   // PieceSquare Queen 49
               -5,   117,   // PieceSquare Queen 50
                6,   110,   // PieceSquare Queen 51
                5,   117,   // PieceSquare Queen 52
               10,    98,   // PieceSquare Queen 53
               25,    12,   // PieceSquare Queen 54
                9,    87,   // PieceSquare Queen 55
               -4,    85,   // PieceSquare Queen 56
               -7,   106,   // PieceSquare Queen 57
               -7,   102,   // PieceSquare Queen 58
               -2,   124,   // PieceSquare Queen 59
               17,    49,   // PieceSquare Queen 60
              -22,    68,   // PieceSquare Queen 61
              -11,    50,   // PieceSquare Queen 62
               41,   -10,   // PieceSquare Queen 63
               90,  -120,   // PieceSquare King 0
               -2,    33,   // PieceSquare King 1
              -56,    92,   // PieceSquare King 2
              -30,    65,   // PieceSquare King 3
              -49,    24,   // PieceSquare King 4
               24,    44,   // PieceSquare King 5
               53,     5,   // PieceSquare King 6
              -15,  -102,   // PieceSquare King 7
               63,   -38,   // PieceSquare King 8
               61,    34,   // PieceSquare King 9
              -21,    52,   // PieceSquare King 10
               63,     7,   // PieceSquare King 11
              -10,    11,   // PieceSquare King 12
              -88,    53,   // PieceSquare King 13
              101,    27,   // PieceSquare King 14
              -58,    -9,   // PieceSquare King 15
               42,    23,   // PieceSquare King 16
              -37,    47,   // PieceSquare King 17
                9,    54,   // PieceSquare King 18
               -4,    44,   // PieceSquare King 19
              -59,    53,   // PieceSquare King 20
              -44,    43,   // PieceSquare King 21
               -5,    41,   // PieceSquare King 22
               77,   -20,   // PieceSquare King 23
               40,    -8,   // PieceSquare King 24
               -7,    25,   // PieceSquare King 25
              -37,    34,   // PieceSquare King 26
              -64,    37,   // PieceSquare King 27
             -151,    44,   // PieceSquare King 28
             -113,    30,   // PieceSquare King 29
              -92,    28,   // PieceSquare King 30
             -165,    12,   // PieceSquare King 31
              -37,     5,   // PieceSquare King 32
              -10,     9,   // PieceSquare King 33
               -4,    -4,   // PieceSquare King 34
              -95,    10,   // PieceSquare King 35
              -83,     2,   // PieceSquare King 36
              -61,    -4,   // PieceSquare King 37
              -70,    -1,   // PieceSquare King 38
             -128,     1,   // PieceSquare King 39
               -9,   -18,   // PieceSquare King 40
                0,   -16,   // PieceSquare King 41
              -51,    -9,   // PieceSquare King 42
              -83,   -12,   // PieceSquare King 43
              -79,   -10,   // PieceSquare King 44
              -67,   -17,   // PieceSquare King 45
              -27,   -23,   // PieceSquare King 46
              -29,   -20,   // PieceSquare King 47
               96,   -36,   // PieceSquare King 48
               46,   -18,   // PieceSquare King 49
               14,   -21,   // PieceSquare King 50
              -36,   -19,   // PieceSquare King 51
              -32,   -13,   // PieceSquare King 52
              -24,   -14,   // PieceSquare King 53
               49,   -23,   // PieceSquare King 54
               74,   -41,   // PieceSquare King 55
               50,   -44,   // PieceSquare King 56
               59,   -38,   // PieceSquare King 57
               18,   -17,   // PieceSquare King 58
              -76,    -6,   // PieceSquare King 59
              -13,   -41,   // PieceSquare King 60
              -19,   -16,   // PieceSquare King 61
               57,   -31,   // PieceSquare King 62
               79,   -83,   // PieceSquare King 63
                0,     0,   // Passed rank 0
              -46,    53,   // Passed rank 1
              -35,    41,   // Passed rank 2
              -48,    69,   // Passed rank 3
              -21,   106,   // Passed rank 4
              -56,    97,   // Passed rank 5
              159,    36,   // Passed rank 6
                0,     0,   // Passed rank 7
               -4,   -15,   // Doubled
               -9,   -14,   // Isolated
               26,   108,   // BishopPair
               35,    13,   // RookOpenFile
               14,    15,   // RookSemiOpenFile
                5,    59,   // RookSeventh
               -3,   -11,   // ShieldNear
                1,    -3,   // ShieldFar
                1,    -8,   // ShieldAdvanced
              -18,    12,   // ShieldNoOwnPawn
              -14,    -5,   // ShieldOpenFile
                4,     4,   // Mobility Knight
                5,     5,   // Mobility Bishop
                2,     4,   // Mobility Rook
                1,     2,   // Mobility Queen
               51,    62,   // PawnThreat
               39,    43,   // MinorThreat
                8,   -22,   // Hanging
               27,    19,   // Outpost
               27,    20,   // Tempo
              -51,   -66,   // MobilityTable Knight 0
              -32,    21,   // MobilityTable Knight 1
              -20,    53,   // MobilityTable Knight 2
              -12,    75,   // MobilityTable Knight 3
               -6,    90,   // MobilityTable Knight 4
               -1,   101,   // MobilityTable Knight 5
                4,   104,   // MobilityTable Knight 6
               15,    99,   // MobilityTable Knight 7
               19,    92,   // MobilityTable Knight 8
              -43,   -31,   // MobilityTable Bishop 0
              -31,     7,   // MobilityTable Bishop 1
              -22,    25,   // MobilityTable Bishop 2
              -16,    34,   // MobilityTable Bishop 3
               -7,    43,   // MobilityTable Bishop 4
               -3,    51,   // MobilityTable Bishop 5
               -1,    60,   // MobilityTable Bishop 6
               -1,    64,   // MobilityTable Bishop 7
                5,    64,   // MobilityTable Bishop 8
                9,    61,   // MobilityTable Bishop 9
                8,    62,   // MobilityTable Bishop 10
                6,    54,   // MobilityTable Bishop 11
                4,    76,   // MobilityTable Bishop 12
               42,    40,   // MobilityTable Bishop 13
              -46,    31,   // MobilityTable Rook 0
              -34,    87,   // MobilityTable Rook 1
              -28,    81,   // MobilityTable Rook 2
              -22,    89,   // MobilityTable Rook 3
              -24,    98,   // MobilityTable Rook 4
              -18,   102,   // MobilityTable Rook 5
              -17,   107,   // MobilityTable Rook 6
              -15,   115,   // MobilityTable Rook 7
              -12,   117,   // MobilityTable Rook 8
               -9,   123,   // MobilityTable Rook 9
               -5,   125,   // MobilityTable Rook 10
               -4,   126,   // MobilityTable Rook 11
               -8,   135,   // MobilityTable Rook 12
              -11,   133,   // MobilityTable Rook 13
               25,   110,   // MobilityTable Rook 14
              -43,    78,   // MobilityTable Queen 0
              -21,   264,   // MobilityTable Queen 1
              -17,   132,   // MobilityTable Queen 2
              -22,   194,   // MobilityTable Queen 3
              -18,   175,   // MobilityTable Queen 4
              -17,   217,   // MobilityTable Queen 5
              -16,   223,   // MobilityTable Queen 6
              -13,   226,   // MobilityTable Queen 7
              -11,   245,   // MobilityTable Queen 8
              -10,   248,   // MobilityTable Queen 9
               -6,   249,   // MobilityTable Queen 10
               -4,   260,   // MobilityTable Queen 11
               -4,   258,   // MobilityTable Queen 12
                3,   254,   // MobilityTable Queen 13
                5,   249,   // MobilityTable Queen 14
                5,   253,   // MobilityTable Queen 15
               14,   248,   // MobilityTable Queen 16
               17,   248,   // MobilityTable Queen 17
               33,   226,   // MobilityTable Queen 18
               36,   218,   // MobilityTable Queen 19
               54,   218,   // MobilityTable Queen 20
               83,   177,   // MobilityTable Queen 21
              137,   111,   // MobilityTable Queen 22
               49,   205,   // MobilityTable Queen 23
              171,    82,   // MobilityTable Queen 24
               98,   143,   // MobilityTable Queen 25
                7,    44,   // MobilityTable Queen 26
               14,    45,   // MobilityTable Queen 27
                0,     0,   // KingDanger 0
                0,     0,   // KingDanger 1
                1,     0,   // KingDanger 2
                2,     0,   // KingDanger 3
                4,     0,   // KingDanger 4
                6,     0,   // KingDanger 5
                9,     0,   // KingDanger 6
               12,     0,   // KingDanger 7
               16,     0,   // KingDanger 8
               20,     0,   // KingDanger 9
               25,     0,   // KingDanger 10
               30,     0,   // KingDanger 11
               36,     0,   // KingDanger 12
               42,     0,   // KingDanger 13
               49,     0,   // KingDanger 14
               56,     0,   // KingDanger 15
               64,     0,   // KingDanger 16
               72,     0,   // KingDanger 17
               81,     0,   // KingDanger 18
               90,     0,   // KingDanger 19
              100,     0,   // KingDanger 20
              110,     0,   // KingDanger 21
              121,     0,   // KingDanger 22
              132,     0,   // KingDanger 23
              144,     0,   // KingDanger 24
              156,     0,   // KingDanger 25
              169,     0,   // KingDanger 26
              182,     0,   // KingDanger 27
              196,     0,   // KingDanger 28
              210,     0,   // KingDanger 29
              225,     0,   // KingDanger 30
              240,     0,   // KingDanger 31
              256,     0,   // KingDanger 32
              272,     0,   // KingDanger 33
              289,     0,   // KingDanger 34
              306,     0,   // KingDanger 35
              324,     0,   // KingDanger 36
              342,     0,   // KingDanger 37
              361,     0,   // KingDanger 38
              380,     0,   // KingDanger 39
              400,     0,   // KingDanger 40
              420,     0,   // KingDanger 41
              441,     0,   // KingDanger 42
              462,     0,   // KingDanger 43
              484,     0,   // KingDanger 44
              500,     0,   // KingDanger 45
              500,     0,   // KingDanger 46
              500,     0,   // KingDanger 47
              500,     0,   // KingDanger 48
              500,     0,   // KingDanger 49
              500,     0,   // KingDanger 50
              500,     0,   // KingDanger 51
              500,     0,   // KingDanger 52
              500,     0,   // KingDanger 53
              500,     0,   // KingDanger 54
              500,     0,   // KingDanger 55
              500,     0,   // KingDanger 56
              500,     0,   // KingDanger 57
              500,     0,   // KingDanger 58
              500,     0,   // KingDanger 59
              500,     0,   // KingDanger 60
              500,     0,   // KingDanger 61
              500,     0,   // KingDanger 62
              500,     0,   // KingDanger 63
              500,     0,   // KingDanger 64
              500,     0,   // KingDanger 65
              500,     0,   // KingDanger 66
              500,     0,   // KingDanger 67
              500,     0,   // KingDanger 68
              500,     0,   // KingDanger 69
              500,     0,   // KingDanger 70
              500,     0,   // KingDanger 71
              500,     0,   // KingDanger 72
              500,     0,   // KingDanger 73
              500,     0,   // KingDanger 74
              500,     0,   // KingDanger 75
              500,     0,   // KingDanger 76
              500,     0,   // KingDanger 77
              500,     0,   // KingDanger 78
              500,     0,   // KingDanger 79
              500,     0,   // KingDanger 80
              500,     0,   // KingDanger 81
              500,     0,   // KingDanger 82
              500,     0,   // KingDanger 83
              500,     0,   // KingDanger 84
              500,     0,   // KingDanger 85
              500,     0,   // KingDanger 86
              500,     0,   // KingDanger 87
              500,     0,   // KingDanger 88
              500,     0,   // KingDanger 89
              500,     0,   // KingDanger 90
              500,     0,   // KingDanger 91
              500,     0,   // KingDanger 92
              500,     0,   // KingDanger 93
              500,     0,   // KingDanger 94
              500,     0,   // KingDanger 95
              500,     0,   // KingDanger 96
              500,     0,   // KingDanger 97
              500,     0,   // KingDanger 98
              500,     0,   // KingDanger 99
               83,     6,   // SafeCheck Knight
               15,    42,   // SafeCheck Bishop
               76,     0,   // SafeCheck Rook
               33,    31,   // SafeCheck Queen
                0,     0,   // PawnStorm rank 0
                0,     0,   // PawnStorm rank 1
                0,     0,   // PawnStorm rank 2
                5,     5,   // PawnStorm rank 3
               11,     2,   // PawnStorm rank 4
               42,   -13,   // PawnStorm rank 5
              -63,   -81,   // PawnStorm rank 6
                0,     0,   // PawnStorm rank 7
               20,    -6,   // WeakKingSquare
              -10,   -12,   // Backward
                0,     0,   // Phalanx rank 0
                3,     2,   // Phalanx rank 1
                9,     6,   // Phalanx rank 2
               10,    13,   // Phalanx rank 3
               23,    45,   // Phalanx rank 4
               74,   114,   // Phalanx rank 5
              140,   256,   // Phalanx rank 6
                0,     0,   // Phalanx rank 7
                0,     0,   // Supported rank 0
                0,     0,   // Supported rank 1
               17,    19,   // Supported rank 2
               13,    13,   // Supported rank 3
                9,    25,   // Supported rank 4
               24,    64,   // Supported rank 5
              127,   132,   // Supported rank 6
                0,     0,   // Supported rank 7
                0,     0,   // PassedBlocked rank 0
              -23,     0,   // PassedBlocked rank 1
              -25,    -6,   // PassedBlocked rank 2
              -36,   -10,   // PassedBlocked rank 3
              -38,   -12,   // PassedBlocked rank 4
              -26,    -6,   // PassedBlocked rank 5
              -90,   101,   // PassedBlocked rank 6
                0,     0,   // PassedBlocked rank 7
                0,     0,   // PassedFree rank 0
                0,     4,   // PassedFree rank 1
              -13,    12,   // PassedFree rank 2
              -18,    22,   // PassedFree rank 3
              -10,    30,   // PassedFree rank 4
              -10,    64,   // PassedFree rank 5
              -67,   212,   // PassedFree rank 6
                0,     0,   // PassedFree rank 7
                0,     0,   // PassedOwnKingDistance rank 0
                3,    -4,   // PassedOwnKingDistance rank 1
                9,   -10,   // PassedOwnKingDistance rank 2
                9,   -19,   // PassedOwnKingDistance rank 3
                7,   -23,   // PassedOwnKingDistance rank 4
               11,   -23,   // PassedOwnKingDistance rank 5
               -2,   -20,   // PassedOwnKingDistance rank 6
                0,     0,   // PassedOwnKingDistance rank 7
                0,     0,   // PassedEnemyKingDistance rank 0
                7,    -5,   // PassedEnemyKingDistance rank 1
                2,     3,   // PassedEnemyKingDistance rank 2
                9,    10,   // PassedEnemyKingDistance rank 3
                8,    20,   // PassedEnemyKingDistance rank 4
               15,    34,   // PassedEnemyKingDistance rank 5
               10,    32,   // PassedEnemyKingDistance rank 6
                0,     0,   // PassedEnemyKingDistance rank 7
               -1,    39,   // RookBehindPassed
               -6,    -7,   // BadBishopPawns
              -86,  -152,   // TrappedBishop
              -40,   -33,   // TrappedRook
               10,     7,   // MinorBehindPawn
                3,     2,   // Space
               -4,    20,   // ThreatByMinor on Pawn
               11,    51,   // ThreatByMinor on Knight
               29,    47,   // ThreatByMinor on Bishop
               13,    19,   // ThreatByMinor on Rook
               16,    15,   // ThreatByMinor on Queen
               12,    47,   // ThreatByRook on Pawn
               24,    75,   // ThreatByRook on Knight
               24,    77,   // ThreatByRook on Bishop
              -22,    41,   // ThreatByRook on Rook
               82,    19,   // ThreatByRook on Queen
               19,   -17,   // ThreatByQueen on Pawn
               21,    26,   // ThreatByQueen on Knight
               39,    27,   // ThreatByQueen on Bishop
               35,    27,   // ThreatByQueen on Rook
               10,    25,   // ThreatByQueen on Queen
               17,    57,   // ThreatByKing
                3,     9,   // KnightPawns
                9,    -6,   // RookPawns
               31,   -52,   // RookPair
              -12,    -2,   // KingOwnPawnDistance
                4,    -5,   // KingEnemyPawnDistance
               35,     0,   // DangerZoneAttack Knight
               35,     0,   // DangerZoneAttack Bishop
               29,     0,   // DangerZoneAttack Rook
               71,     0,   // DangerZoneAttack Queen
               78,     0,   // DangerSafeCheck Knight
              124,     0,   // DangerSafeCheck Bishop
               73,     0,   // DangerSafeCheck Rook
               79,     0,   // DangerSafeCheck Queen
               18,     0,   // DangerWeakSquare
               73,     0,   // DangerAttackers
             -120,     0,   // DangerBias
        };
    }
}
