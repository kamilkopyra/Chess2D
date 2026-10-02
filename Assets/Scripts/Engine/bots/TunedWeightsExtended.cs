namespace ChessEngine
{
    // Evaluation weights found by Texel tuning (Tools/Chess2D.Tune), in the layout of TunableEvaluation:
    // Weights[2 * term] for the middlegame, Weights[2 * term + 1] for the endgame.
    // Feature set: extended (TunableEvaluation.Evaluate(..., extended: true)).
    // 2501077 positions, K = 0.8659, error 0.066437 -> 0.060126
    public static class TunedWeightsExtended
    {
        public static readonly int[] Weights =
        {
               60,   116,   // Material Pawn
              298,   429,   // Material Knight
              333,   471,   // Material Bishop
              420,   732,   // Material Rook
              911,  1263,   // Material Queen
                0,     0,   // PieceSquare Pawn 0
                0,     0,   // PieceSquare Pawn 1
                0,     0,   // PieceSquare Pawn 2
                0,     0,   // PieceSquare Pawn 3
                0,     0,   // PieceSquare Pawn 4
                0,     0,   // PieceSquare Pawn 5
                0,     0,   // PieceSquare Pawn 6
                0,     0,   // PieceSquare Pawn 7
              129,    32,   // PieceSquare Pawn 8
               61,    81,   // PieceSquare Pawn 9
              105,    73,   // PieceSquare Pawn 10
               85,    81,   // PieceSquare Pawn 11
              123,    59,   // PieceSquare Pawn 12
               88,    66,   // PieceSquare Pawn 13
               33,   100,   // PieceSquare Pawn 14
               43,    62,   // PieceSquare Pawn 15
               21,    46,   // PieceSquare Pawn 16
                5,    57,   // PieceSquare Pawn 17
               31,    40,   // PieceSquare Pawn 18
               33,    30,   // PieceSquare Pawn 19
               52,    44,   // PieceSquare Pawn 20
               65,    44,   // PieceSquare Pawn 21
               51,    48,   // PieceSquare Pawn 22
               15,    58,   // PieceSquare Pawn 23
               -1,    47,   // PieceSquare Pawn 24
                0,    27,   // PieceSquare Pawn 25
                4,    11,   // PieceSquare Pawn 26
                7,    -6,   // PieceSquare Pawn 27
               29,    -9,   // PieceSquare Pawn 28
               28,     6,   // PieceSquare Pawn 29
               17,    24,   // PieceSquare Pawn 30
               10,    32,   // PieceSquare Pawn 31
              -11,    36,   // PieceSquare Pawn 32
              -14,    30,   // PieceSquare Pawn 33
               -5,    15,   // PieceSquare Pawn 34
                0,     6,   // PieceSquare Pawn 35
                6,    10,   // PieceSquare Pawn 36
                2,    17,   // PieceSquare Pawn 37
                8,    16,   // PieceSquare Pawn 38
                1,    15,   // PieceSquare Pawn 39
              -26,    24,   // PieceSquare Pawn 40
              -33,    25,   // PieceSquare Pawn 41
              -23,    22,   // PieceSquare Pawn 42
              -21,    25,   // PieceSquare Pawn 43
              -14,    27,   // PieceSquare Pawn 44
              -16,    23,   // PieceSquare Pawn 45
               -2,     7,   // PieceSquare Pawn 46
               -7,     5,   // PieceSquare Pawn 47
              -13,    28,   // PieceSquare Pawn 48
              -21,    31,   // PieceSquare Pawn 49
              -11,    35,   // PieceSquare Pawn 50
              -11,    23,   // PieceSquare Pawn 51
               -8,    53,   // PieceSquare Pawn 52
                2,    42,   // PieceSquare Pawn 53
               13,    20,   // PieceSquare Pawn 54
               -3,     8,   // PieceSquare Pawn 55
                0,     0,   // PieceSquare Pawn 56
                0,     0,   // PieceSquare Pawn 57
                0,     0,   // PieceSquare Pawn 58
                0,     0,   // PieceSquare Pawn 59
                0,     0,   // PieceSquare Pawn 60
                0,     0,   // PieceSquare Pawn 61
                0,     0,   // PieceSquare Pawn 62
                0,     0,   // PieceSquare Pawn 63
             -152,   -86,   // PieceSquare Knight 0
              -51,    28,   // PieceSquare Knight 1
              -73,    33,   // PieceSquare Knight 2
             -232,    67,   // PieceSquare Knight 3
                3,    17,   // PieceSquare Knight 4
             -161,    71,   // PieceSquare Knight 5
              129,   -19,   // PieceSquare Knight 6
             -177,   -84,   // PieceSquare Knight 7
                8,    -6,   // PieceSquare Knight 8
                8,     9,   // PieceSquare Knight 9
               -5,    16,   // PieceSquare Knight 10
               76,   -11,   // PieceSquare Knight 11
               57,    -1,   // PieceSquare Knight 12
               54,   -20,   // PieceSquare Knight 13
                3,    19,   // PieceSquare Knight 14
               36,   -13,   // PieceSquare Knight 15
              -18,    -5,   // PieceSquare Knight 16
                2,     9,   // PieceSquare Knight 17
               13,    37,   // PieceSquare Knight 18
               33,    20,   // PieceSquare Knight 19
               68,    12,   // PieceSquare Knight 20
               74,    31,   // PieceSquare Knight 21
               26,    15,   // PieceSquare Knight 22
               14,    10,   // PieceSquare Knight 23
                9,    32,   // PieceSquare Knight 24
               -2,    15,   // PieceSquare Knight 25
                3,    45,   // PieceSquare Knight 26
               34,    37,   // PieceSquare Knight 27
                3,    50,   // PieceSquare Knight 28
               17,    47,   // PieceSquare Knight 29
                9,    17,   // PieceSquare Knight 30
               33,    18,   // PieceSquare Knight 31
              -13,     5,   // PieceSquare Knight 32
              -13,    16,   // PieceSquare Knight 33
                3,    51,   // PieceSquare Knight 34
                0,    44,   // PieceSquare Knight 35
               13,    42,   // PieceSquare Knight 36
               25,    35,   // PieceSquare Knight 37
               26,    17,   // PieceSquare Knight 38
                4,    22,   // PieceSquare Knight 39
              -41,     4,   // PieceSquare Knight 40
              -18,    12,   // PieceSquare Knight 41
              -16,    13,   // PieceSquare Knight 42
               -6,    39,   // PieceSquare Knight 43
               12,    33,   // PieceSquare Knight 44
               -8,     1,   // PieceSquare Knight 45
                7,     1,   // PieceSquare Knight 46
              -13,    11,   // PieceSquare Knight 47
              -48,    -1,   // PieceSquare Knight 48
              -50,    24,   // PieceSquare Knight 49
              -16,    -2,   // PieceSquare Knight 50
               -7,     4,   // PieceSquare Knight 51
               -5,    10,   // PieceSquare Knight 52
               -2,     1,   // PieceSquare Knight 53
               -5,    19,   // PieceSquare Knight 54
              -21,    -9,   // PieceSquare Knight 55
              -80,   -28,   // PieceSquare Knight 56
              -39,    -4,   // PieceSquare Knight 57
              -52,    20,   // PieceSquare Knight 58
              -21,    17,   // PieceSquare Knight 59
              -16,    34,   // PieceSquare Knight 60
              -22,     5,   // PieceSquare Knight 61
              -39,    -8,   // PieceSquare Knight 62
              -65,    21,   // PieceSquare Knight 63
              -34,    34,   // PieceSquare Bishop 0
              -25,    52,   // PieceSquare Bishop 1
              -64,    40,   // PieceSquare Bishop 2
              -48,    47,   // PieceSquare Bishop 3
              -62,    42,   // PieceSquare Bishop 4
              -97,    45,   // PieceSquare Bishop 5
              -27,    32,   // PieceSquare Bishop 6
              -16,    37,   // PieceSquare Bishop 7
              -17,    52,   // PieceSquare Bishop 8
              -32,    43,   // PieceSquare Bishop 9
               12,    25,   // PieceSquare Bishop 10
                4,    27,   // PieceSquare Bishop 11
               10,    28,   // PieceSquare Bishop 12
               -8,    38,   // PieceSquare Bishop 13
              -21,    50,   // PieceSquare Bishop 14
              -36,    34,   // PieceSquare Bishop 15
                7,    35,   // PieceSquare Bishop 16
               10,    51,   // PieceSquare Bishop 17
                6,    43,   // PieceSquare Bishop 18
               38,    28,   // PieceSquare Bishop 19
               38,    31,   // PieceSquare Bishop 20
               87,    44,   // PieceSquare Bishop 21
               12,    67,   // PieceSquare Bishop 22
               19,    43,   // PieceSquare Bishop 23
              -13,    29,   // PieceSquare Bishop 24
               -1,    51,   // PieceSquare Bishop 25
                6,    48,   // PieceSquare Bishop 26
               33,    46,   // PieceSquare Bishop 27
               20,    53,   // PieceSquare Bishop 28
               17,    46,   // PieceSquare Bishop 29
               16,    31,   // PieceSquare Bishop 30
               -7,    40,   // PieceSquare Bishop 31
               -5,    25,   // PieceSquare Bishop 32
               -7,    33,   // PieceSquare Bishop 33
              -10,    50,   // PieceSquare Bishop 34
               19,    52,   // PieceSquare Bishop 35
               12,    38,   // PieceSquare Bishop 36
                0,    30,   // PieceSquare Bishop 37
               -5,    14,   // PieceSquare Bishop 38
               25,   -12,   // PieceSquare Bishop 39
               -7,     9,   // PieceSquare Bishop 40
                7,    37,   // PieceSquare Bishop 41
               -2,    46,   // PieceSquare Bishop 42
               -8,    42,   // PieceSquare Bishop 43
               -3,    60,   // PieceSquare Bishop 44
              -15,    31,   // PieceSquare Bishop 45
               10,     9,   // PieceSquare Bishop 46
                8,     2,   // PieceSquare Bishop 47
               -4,    25,   // PieceSquare Bishop 48
                3,     1,   // PieceSquare Bishop 49
               10,     4,   // PieceSquare Bishop 50
              -15,    30,   // PieceSquare Bishop 51
               -5,    18,   // PieceSquare Bishop 52
                5,     5,   // PieceSquare Bishop 53
                8,     4,   // PieceSquare Bishop 54
               14,   -37,   // PieceSquare Bishop 55
                4,    -4,   // PieceSquare Bishop 56
                8,    -8,   // PieceSquare Bishop 57
              -13,    21,   // PieceSquare Bishop 58
              -32,    20,   // PieceSquare Bishop 59
                4,    24,   // PieceSquare Bishop 60
              -25,    27,   // PieceSquare Bishop 61
               -1,    10,   // PieceSquare Bishop 62
              -21,    12,   // PieceSquare Bishop 63
               -9,    88,   // PieceSquare Rook 0
               15,    82,   // PieceSquare Rook 1
                3,    92,   // PieceSquare Rook 2
                2,   100,   // PieceSquare Rook 3
               33,    82,   // PieceSquare Rook 4
               41,    79,   // PieceSquare Rook 5
                5,   105,   // PieceSquare Rook 6
               70,    69,   // PieceSquare Rook 7
              -35,    35,   // PieceSquare Rook 8
              -26,    34,   // PieceSquare Rook 9
               -3,    28,   // PieceSquare Rook 10
               11,    30,   // PieceSquare Rook 11
               16,    17,   // PieceSquare Rook 12
               83,     1,   // PieceSquare Rook 13
               19,    17,   // PieceSquare Rook 14
               26,    11,   // PieceSquare Rook 15
              -37,   104,   // PieceSquare Rook 16
                6,    71,   // PieceSquare Rook 17
                2,    80,   // PieceSquare Rook 18
               20,    64,   // PieceSquare Rook 19
               55,    45,   // PieceSquare Rook 20
               81,    48,   // PieceSquare Rook 21
               73,    46,   // PieceSquare Rook 22
               20,    63,   // PieceSquare Rook 23
              -44,    91,   // PieceSquare Rook 24
               -9,    75,   // PieceSquare Rook 25
              -14,    74,   // PieceSquare Rook 26
                5,    61,   // PieceSquare Rook 27
              -11,    69,   // PieceSquare Rook 28
               26,    53,   // PieceSquare Rook 29
               28,    51,   // PieceSquare Rook 30
                7,    63,   // PieceSquare Rook 31
              -49,    73,   // PieceSquare Rook 32
              -41,    77,   // PieceSquare Rook 33
              -37,    74,   // PieceSquare Rook 34
              -13,    48,   // PieceSquare Rook 35
              -17,    47,   // PieceSquare Rook 36
              -28,    64,   // PieceSquare Rook 37
               30,    31,   // PieceSquare Rook 38
              -22,    50,   // PieceSquare Rook 39
              -47,    62,   // PieceSquare Rook 40
              -32,    52,   // PieceSquare Rook 41
              -39,    52,   // PieceSquare Rook 42
              -29,    39,   // PieceSquare Rook 43
              -16,    32,   // PieceSquare Rook 44
              -12,    32,   // PieceSquare Rook 45
               16,     8,   // PieceSquare Rook 46
               -1,    10,   // PieceSquare Rook 47
              -49,    42,   // PieceSquare Rook 48
              -49,    45,   // PieceSquare Rook 49
              -34,    38,   // PieceSquare Rook 50
              -25,    32,   // PieceSquare Rook 51
              -16,    14,   // PieceSquare Rook 52
               -6,    17,   // PieceSquare Rook 53
                8,     6,   // PieceSquare Rook 54
              -25,    19,   // PieceSquare Rook 55
              -39,    59,   // PieceSquare Rook 56
              -30,    37,   // PieceSquare Rook 57
              -28,    53,   // PieceSquare Rook 58
              -17,    29,   // PieceSquare Rook 59
               -8,    21,   // PieceSquare Rook 60
              -11,    36,   // PieceSquare Rook 61
                8,    16,   // PieceSquare Rook 62
              -16,    29,   // PieceSquare Rook 63
              -48,   196,   // PieceSquare Queen 0
              -37,   151,   // PieceSquare Queen 1
               -3,   157,   // PieceSquare Queen 2
              -17,   194,   // PieceSquare Queen 3
               -5,   201,   // PieceSquare Queen 4
               37,   188,   // PieceSquare Queen 5
              -28,   206,   // PieceSquare Queen 6
              -21,   200,   // PieceSquare Queen 7
              -14,   146,   // PieceSquare Queen 8
              -32,   169,   // PieceSquare Queen 9
              -22,   195,   // PieceSquare Queen 10
                6,   169,   // PieceSquare Queen 11
              -29,   221,   // PieceSquare Queen 12
               54,   159,   // PieceSquare Queen 13
              -23,   201,   // PieceSquare Queen 14
               32,   175,   // PieceSquare Queen 15
              -20,   159,   // PieceSquare Queen 16
               13,   152,   // PieceSquare Queen 17
              -13,   181,   // PieceSquare Queen 18
                2,   173,   // PieceSquare Queen 19
               13,   221,   // PieceSquare Queen 20
               40,   234,   // PieceSquare Queen 21
               41,   187,   // PieceSquare Queen 22
               17,   190,   // PieceSquare Queen 23
               -6,   129,   // PieceSquare Queen 24
              -12,   175,   // PieceSquare Queen 25
                0,   157,   // PieceSquare Queen 26
              -17,   212,   // PieceSquare Queen 27
               -3,   203,   // PieceSquare Queen 28
               15,   189,   // PieceSquare Queen 29
               11,   180,   // PieceSquare Queen 30
               13,   163,   // PieceSquare Queen 31
               -7,   136,   // PieceSquare Queen 32
              -18,   149,   // PieceSquare Queen 33
              -16,   176,   // PieceSquare Queen 34
              -11,   185,   // PieceSquare Queen 35
               -6,   172,   // PieceSquare Queen 36
                3,   151,   // PieceSquare Queen 37
               19,   149,   // PieceSquare Queen 38
                5,   148,   // PieceSquare Queen 39
               -6,   101,   // PieceSquare Queen 40
               -6,   116,   // PieceSquare Queen 41
               -3,   126,   // PieceSquare Queen 42
              -16,   151,   // PieceSquare Queen 43
               -5,   146,   // PieceSquare Queen 44
                1,   141,   // PieceSquare Queen 45
               23,   100,   // PieceSquare Queen 46
               13,   120,   // PieceSquare Queen 47
              -23,   107,   // PieceSquare Queen 48
              -10,    97,   // PieceSquare Queen 49
               -5,   113,   // PieceSquare Queen 50
                6,   102,   // PieceSquare Queen 51
                5,   109,   // PieceSquare Queen 52
               10,    89,   // PieceSquare Queen 53
               27,    -1,   // PieceSquare Queen 54
               15,    75,   // PieceSquare Queen 55
               -6,    84,   // PieceSquare Queen 56
               -7,   101,   // PieceSquare Queen 57
               -6,    94,   // PieceSquare Queen 58
               -2,   118,   // PieceSquare Queen 59
               15,    44,   // PieceSquare Queen 60
              -22,    60,   // PieceSquare Queen 61
               -5,    39,   // PieceSquare Queen 62
               37,    -2,   // PieceSquare Queen 63
               83,  -148,   // PieceSquare King 0
              -19,    14,   // PieceSquare King 1
              -94,    85,   // PieceSquare King 2
              -56,    58,   // PieceSquare King 3
              -60,    18,   // PieceSquare King 4
               -4,    37,   // PieceSquare King 5
               47,    -2,   // PieceSquare King 6
               -2,  -116,   // PieceSquare King 7
               37,   -54,   // PieceSquare King 8
               18,    40,   // PieceSquare King 9
              -41,    55,   // PieceSquare King 10
               45,    12,   // PieceSquare King 11
              -58,    23,   // PieceSquare King 12
             -127,    63,   // PieceSquare King 13
               54,    40,   // PieceSquare King 14
              -78,   -17,   // PieceSquare King 15
               28,     9,   // PieceSquare King 16
              -63,    51,   // PieceSquare King 17
              -16,    64,   // PieceSquare King 18
              -20,    59,   // PieceSquare King 19
              -85,    68,   // PieceSquare King 20
              -75,    60,   // PieceSquare King 21
              -50,    53,   // PieceSquare King 22
               36,   -21,   // PieceSquare King 23
               26,   -30,   // PieceSquare King 24
              -28,    25,   // PieceSquare King 25
              -27,    41,   // PieceSquare King 26
              -61,    51,   // PieceSquare King 27
             -152,    61,   // PieceSquare King 28
             -109,    41,   // PieceSquare King 29
             -107,    36,   // PieceSquare King 30
             -211,     8,   // PieceSquare King 31
              -56,   -16,   // PieceSquare King 32
                2,    -1,   // PieceSquare King 33
               14,     3,   // PieceSquare King 34
              -68,    26,   // PieceSquare King 35
              -59,    19,   // PieceSquare King 36
              -42,     8,   // PieceSquare King 37
              -67,     1,   // PieceSquare King 38
             -157,    -6,   // PieceSquare King 39
              -19,   -43,   // PieceSquare King 40
               20,   -30,   // PieceSquare King 41
              -11,   -12,   // PieceSquare King 42
              -34,    -6,   // PieceSquare King 43
              -31,    -2,   // PieceSquare King 44
              -29,   -14,   // PieceSquare King 45
              -20,   -25,   // PieceSquare King 46
              -55,   -32,   // PieceSquare King 47
               58,   -44,   // PieceSquare King 48
               36,   -12,   // PieceSquare King 49
               36,   -12,   // PieceSquare King 50
               -8,    -4,   // PieceSquare King 51
               -4,     2,   // PieceSquare King 52
              -14,     0,   // PieceSquare King 53
               33,   -14,   // PieceSquare King 54
               18,   -43,   // PieceSquare King 55
                0,   -61,   // PieceSquare King 56
               40,   -46,   // PieceSquare King 57
               36,   -26,   // PieceSquare King 58
              -41,   -16,   // PieceSquare King 59
               20,   -54,   // PieceSquare King 60
              -17,   -22,   // PieceSquare King 61
               31,   -43,   // PieceSquare King 62
               23,  -104,   // PieceSquare King 63
                0,     0,   // Passed rank 0
               39,    46,   // Passed rank 1
               12,    32,   // Passed rank 2
              -37,    68,   // Passed rank 3
                6,    96,   // Passed rank 4
                3,    79,   // Passed rank 5
              193,    27,   // Passed rank 6
                0,     0,   // Passed rank 7
               -4,   -12,   // Doubled
               -8,   -14,   // Isolated
               25,   112,   // BishopPair
               35,    10,   // RookOpenFile
               12,    17,   // RookSemiOpenFile
                2,    60,   // RookSeventh
               18,   -18,   // ShieldNear
                9,   -11,   // ShieldFar
                4,   -15,   // ShieldAdvanced
              -12,    -3,   // ShieldNoOwnPawn
              -19,     2,   // ShieldOpenFile
                4,     4,   // Mobility Knight
                5,     5,   // Mobility Bishop
                2,     4,   // Mobility Rook
                1,     2,   // Mobility Queen
               49,    38,   // PawnThreat
               50,    37,   // MinorThreat
               21,    20,   // Hanging
               28,    20,   // Outpost
               26,    13,   // Tempo
              -51,   -62,   // MobilityTable Knight 0
              -32,    20,   // MobilityTable Knight 1
              -20,    52,   // MobilityTable Knight 2
              -12,    71,   // MobilityTable Knight 3
               -6,    86,   // MobilityTable Knight 4
                0,    95,   // MobilityTable Knight 5
                5,    95,   // MobilityTable Knight 6
               17,    86,   // MobilityTable Knight 7
               23,    73,   // MobilityTable Knight 8
              -45,   -33,   // MobilityTable Bishop 0
              -33,     3,   // MobilityTable Bishop 1
              -23,    21,   // MobilityTable Bishop 2
              -17,    31,   // MobilityTable Bishop 3
               -8,    42,   // MobilityTable Bishop 4
               -3,    52,   // MobilityTable Bishop 5
               -1,    61,   // MobilityTable Bishop 6
               -1,    64,   // MobilityTable Bishop 7
                4,    63,   // MobilityTable Bishop 8
                9,    59,   // MobilityTable Bishop 9
                8,    58,   // MobilityTable Bishop 10
                4,    49,   // MobilityTable Bishop 11
               10,    64,   // MobilityTable Bishop 12
               43,    28,   // MobilityTable Bishop 13
              -48,    28,   // MobilityTable Rook 0
              -35,    79,   // MobilityTable Rook 1
              -29,    75,   // MobilityTable Rook 2
              -22,    82,   // MobilityTable Rook 3
              -24,    91,   // MobilityTable Rook 4
              -18,    96,   // MobilityTable Rook 5
              -17,   101,   // MobilityTable Rook 6
              -15,   111,   // MobilityTable Rook 7
              -12,   115,   // MobilityTable Rook 8
              -10,   121,   // MobilityTable Rook 9
               -6,   123,   // MobilityTable Rook 10
               -5,   125,   // MobilityTable Rook 11
              -11,   135,   // MobilityTable Rook 12
              -18,   135,   // MobilityTable Rook 13
               13,   112,   // MobilityTable Rook 14
              -37,    70,   // MobilityTable Queen 0
              -19,   252,   // MobilityTable Queen 1
              -17,   128,   // MobilityTable Queen 2
              -22,   190,   // MobilityTable Queen 3
              -17,   170,   // MobilityTable Queen 4
              -16,   209,   // MobilityTable Queen 5
              -15,   214,   // MobilityTable Queen 6
              -12,   218,   // MobilityTable Queen 7
              -10,   235,   // MobilityTable Queen 8
               -9,   240,   // MobilityTable Queen 9
               -5,   240,   // MobilityTable Queen 10
               -3,   250,   // MobilityTable Queen 11
               -3,   247,   // MobilityTable Queen 12
                4,   241,   // MobilityTable Queen 13
                5,   239,   // MobilityTable Queen 14
                6,   241,   // MobilityTable Queen 15
               14,   236,   // MobilityTable Queen 16
               15,   238,   // MobilityTable Queen 17
               32,   216,   // MobilityTable Queen 18
               35,   207,   // MobilityTable Queen 19
               54,   205,   // MobilityTable Queen 20
               79,   168,   // MobilityTable Queen 21
              136,    97,   // MobilityTable Queen 22
               42,   195,   // MobilityTable Queen 23
              159,    75,   // MobilityTable Queen 24
              109,   122,   // MobilityTable Queen 25
                6,    37,   // MobilityTable Queen 26
                9,    28,   // MobilityTable Queen 27
                0,     0,   // KingDanger 0
                0,     0,   // KingDanger 1
                1,     0,   // KingDanger 2
                2,     0,   // KingDanger 3
               21,     6,   // KingDanger 4
               25,    10,   // KingDanger 5
               36,   -15,   // KingDanger 6
               16,    10,   // KingDanger 7
               36,   -17,   // KingDanger 8
               44,     4,   // KingDanger 9
               70,    -9,   // KingDanger 10
               58,   -21,   // KingDanger 11
               55,   -11,   // KingDanger 12
               77,   -27,   // KingDanger 13
               82,    20,   // KingDanger 14
              151,   -66,   // KingDanger 15
              100,   -21,   // KingDanger 16
               99,     6,   // KingDanger 17
              141,   -30,   // KingDanger 18
              138,     9,   // KingDanger 19
              200,   -50,   // KingDanger 20
              170,   -39,   // KingDanger 21
              194,   -37,   // KingDanger 22
              181,   -37,   // KingDanger 23
              215,    21,   // KingDanger 24
              220,    58,   // KingDanger 25
              317,  -135,   // KingDanger 26
              284,   -20,   // KingDanger 27
              359,  -156,   // KingDanger 28
              332,   -28,   // KingDanger 29
              387,    14,   // KingDanger 30
              424,    86,   // KingDanger 31
              443,   106,   // KingDanger 32
              479,    73,   // KingDanger 33
              387,   133,   // KingDanger 34
              338,    11,   // KingDanger 35
              492,    87,   // KingDanger 36
              532,    96,   // KingDanger 37
              473,    76,   // KingDanger 38
              416,    10,   // KingDanger 39
              378,   -10,   // KingDanger 40
              455,    -7,   // KingDanger 41
              466,    10,   // KingDanger 42
              542,    38,   // KingDanger 43
              485,     0,   // KingDanger 44
              500,     0,   // KingDanger 45
              464,   -10,   // KingDanger 46
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
               94,     3,   // SafeCheck Knight
               17,    46,   // SafeCheck Bishop
               77,     9,   // SafeCheck Rook
               37,    37,   // SafeCheck Queen
                0,     0,   // PawnStorm rank 0
                0,     0,   // PawnStorm rank 1
                0,     0,   // PawnStorm rank 2
                4,    -1,   // PawnStorm rank 3
               13,    -7,   // PawnStorm rank 4
               44,   -24,   // PawnStorm rank 5
              -82,   -86,   // PawnStorm rank 6
                0,     0,   // PawnStorm rank 7
               23,    -6,   // WeakKingSquare
              -10,   -12,   // Backward
                0,     0,   // Phalanx rank 0
                3,     2,   // Phalanx rank 1
                9,     6,   // Phalanx rank 2
               10,    14,   // Phalanx rank 3
               23,    45,   // Phalanx rank 4
               78,   111,   // Phalanx rank 5
              145,   261,   // Phalanx rank 6
                0,     0,   // Phalanx rank 7
                0,     0,   // Supported rank 0
                0,     0,   // Supported rank 1
               17,    22,   // Supported rank 2
               11,    16,   // Supported rank 3
                9,    30,   // Supported rank 4
               25,    69,   // Supported rank 5
              137,   131,   // Supported rank 6
                0,     0,   // Supported rank 7
                0,     0,   // PassedBlocked rank 0
              -30,     2,   // PassedBlocked rank 1
              -26,    -7,   // PassedBlocked rank 2
              -35,   -13,   // PassedBlocked rank 3
              -37,   -11,   // PassedBlocked rank 4
              -26,    -7,   // PassedBlocked rank 5
              -69,    84,   // PassedBlocked rank 6
                0,     0,   // PassedBlocked rank 7
                0,     0,   // PassedFree rank 0
               -5,     7,   // PassedFree rank 1
              -16,    14,   // PassedFree rank 2
              -20,    22,   // PassedFree rank 3
              -10,    30,   // PassedFree rank 4
              -10,    65,   // PassedFree rank 5
              -43,   196,   // PassedFree rank 6
                0,     0,   // PassedFree rank 7
                0,     0,   // PassedOwnKingDistance rank 0
                4,    -4,   // PassedOwnKingDistance rank 1
               12,    -9,   // PassedOwnKingDistance rank 2
               14,   -18,   // PassedOwnKingDistance rank 3
                5,   -21,   // PassedOwnKingDistance rank 4
                4,   -21,   // PassedOwnKingDistance rank 5
              -12,   -19,   // PassedOwnKingDistance rank 6
                0,     0,   // PassedOwnKingDistance rank 7
                0,     0,   // PassedEnemyKingDistance rank 0
              -11,    -4,   // PassedEnemyKingDistance rank 1
              -12,     3,   // PassedEnemyKingDistance rank 2
               -2,    10,   // PassedEnemyKingDistance rank 3
                1,    20,   // PassedEnemyKingDistance rank 4
                7,    36,   // PassedEnemyKingDistance rank 5
               -2,    36,   // PassedEnemyKingDistance rank 6
                0,     0,   // PassedEnemyKingDistance rank 7
                3,    39,   // RookBehindPassed
               -6,    -8,   // BadBishopPawns
              -80,  -140,   // TrappedBishop
              -31,   -38,   // TrappedRook
               11,     6,   // MinorBehindPawn
                3,     2,   // Space
        };
    }
}
