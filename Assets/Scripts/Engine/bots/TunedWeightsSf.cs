namespace ChessEngine
{
    // Evaluation weights found by Texel tuning (Tools/Chess2D.Tune), in the layout of TunableEvaluation:
    // Weights[2 * term] for the middlegame, Weights[2 * term + 1] for the endgame.
    // Feature set: basic.
    // 2501077 positions, K = 0.8466, error 0.066541 -> 0.062405
    public static class TunedWeightsSf
    {
        public static readonly int[] Weights =
        {
               67,   124,   // Material Pawn
              302,   444,   // Material Knight
              336,   453,   // Material Bishop
              419,   770,   // Material Rook
              979,  1296,   // Material Queen
                0,     0,   // PieceSquare Pawn 0
                0,     0,   // PieceSquare Pawn 1
                0,     0,   // PieceSquare Pawn 2
                0,     0,   // PieceSquare Pawn 3
                0,     0,   // PieceSquare Pawn 4
                0,     0,   // PieceSquare Pawn 5
                0,     0,   // PieceSquare Pawn 6
                0,     0,   // PieceSquare Pawn 7
              136,   109,   // PieceSquare Pawn 8
               50,   139,   // PieceSquare Pawn 9
              127,   104,   // PieceSquare Pawn 10
              106,    78,   // PieceSquare Pawn 11
              124,    56,   // PieceSquare Pawn 12
               53,    87,   // PieceSquare Pawn 13
              -52,   135,   // PieceSquare Pawn 14
              -46,   136,   // PieceSquare Pawn 15
               24,    66,   // PieceSquare Pawn 16
               15,    68,   // PieceSquare Pawn 17
               46,    36,   // PieceSquare Pawn 18
               51,    -4,   // PieceSquare Pawn 19
               64,     8,   // PieceSquare Pawn 20
               97,    30,   // PieceSquare Pawn 21
               64,    41,   // PieceSquare Pawn 22
               36,    46,   // PieceSquare Pawn 23
               -5,    52,   // PieceSquare Pawn 24
               -5,    41,   // PieceSquare Pawn 25
                8,    23,   // PieceSquare Pawn 26
                8,     4,   // PieceSquare Pawn 27
               31,     2,   // PieceSquare Pawn 28
               39,    19,   // PieceSquare Pawn 29
               20,    35,   // PieceSquare Pawn 30
                5,    37,   // PieceSquare Pawn 31
              -15,    30,   // PieceSquare Pawn 32
              -23,    29,   // PieceSquare Pawn 33
               -4,    15,   // PieceSquare Pawn 34
                2,     7,   // PieceSquare Pawn 35
                6,    13,   // PieceSquare Pawn 36
                8,    22,   // PieceSquare Pawn 37
                1,    17,   // PieceSquare Pawn 38
               -5,    13,   // PieceSquare Pawn 39
              -24,    20,   // PieceSquare Pawn 40
              -26,    22,   // PieceSquare Pawn 41
              -20,    20,   // PieceSquare Pawn 42
              -17,    24,   // PieceSquare Pawn 43
               -4,    28,   // PieceSquare Pawn 44
               -7,    27,   // PieceSquare Pawn 45
                7,    11,   // PieceSquare Pawn 46
               -8,     6,   // PieceSquare Pawn 47
              -14,    21,   // PieceSquare Pawn 48
              -25,    23,   // PieceSquare Pawn 49
              -13,    23,   // PieceSquare Pawn 50
              -19,     8,   // PieceSquare Pawn 51
              -12,    44,   // PieceSquare Pawn 52
                0,    39,   // PieceSquare Pawn 53
               13,    16,   // PieceSquare Pawn 54
              -10,     4,   // PieceSquare Pawn 55
                0,     0,   // PieceSquare Pawn 56
                0,     0,   // PieceSquare Pawn 57
                0,     0,   // PieceSquare Pawn 58
                0,     0,   // PieceSquare Pawn 59
                0,     0,   // PieceSquare Pawn 60
                0,     0,   // PieceSquare Pawn 61
                0,     0,   // PieceSquare Pawn 62
                0,     0,   // PieceSquare Pawn 63
             -162,   -65,   // PieceSquare Knight 0
              -43,    22,   // PieceSquare Knight 1
              -57,    43,   // PieceSquare Knight 2
             -201,    70,   // PieceSquare Knight 3
               20,    29,   // PieceSquare Knight 4
             -153,    81,   // PieceSquare Knight 5
              174,   -31,   // PieceSquare Knight 6
             -172,   -92,   // PieceSquare Knight 7
                5,    -1,   // PieceSquare Knight 8
                6,    23,   // PieceSquare Knight 9
               -1,    34,   // PieceSquare Knight 10
               83,     7,   // PieceSquare Knight 11
               59,    15,   // PieceSquare Knight 12
               81,    -6,   // PieceSquare Knight 13
                5,    36,   // PieceSquare Knight 14
               50,    -3,   // PieceSquare Knight 15
              -15,     5,   // PieceSquare Knight 16
               -6,    30,   // PieceSquare Knight 17
               13,    41,   // PieceSquare Knight 18
               36,    25,   // PieceSquare Knight 19
               83,    12,   // PieceSquare Knight 20
               95,    35,   // PieceSquare Knight 21
               39,    32,   // PieceSquare Knight 22
               34,    14,   // PieceSquare Knight 23
                6,    40,   // PieceSquare Knight 24
               -7,    34,   // PieceSquare Knight 25
               -4,    55,   // PieceSquare Knight 26
               29,    56,   // PieceSquare Knight 27
                7,    54,   // PieceSquare Knight 28
               26,    56,   // PieceSquare Knight 29
               18,    37,   // PieceSquare Knight 30
               43,    22,   // PieceSquare Knight 31
              -19,    10,   // PieceSquare Knight 32
              -21,    30,   // PieceSquare Knight 33
               -2,    61,   // PieceSquare Knight 34
               -2,    55,   // PieceSquare Knight 35
                9,    60,   // PieceSquare Knight 36
               22,    45,   // PieceSquare Knight 37
               25,    33,   // PieceSquare Knight 38
               -1,    17,   // PieceSquare Knight 39
              -42,     8,   // PieceSquare Knight 40
              -23,    25,   // PieceSquare Knight 41
              -16,    22,   // PieceSquare Knight 42
               -7,    48,   // PieceSquare Knight 43
               13,    43,   // PieceSquare Knight 44
               -7,    11,   // PieceSquare Knight 45
                1,    20,   // PieceSquare Knight 46
              -15,    11,   // PieceSquare Knight 47
              -54,     2,   // PieceSquare Knight 48
              -58,    34,   // PieceSquare Knight 49
              -19,     8,   // PieceSquare Knight 50
               -7,    12,   // PieceSquare Knight 51
               -6,    17,   // PieceSquare Knight 52
               -4,    16,   // PieceSquare Knight 53
               -8,    22,   // PieceSquare Knight 54
              -22,   -12,   // PieceSquare Knight 55
              -92,   -30,   // PieceSquare Knight 56
              -39,   -19,   // PieceSquare Knight 57
              -56,    21,   // PieceSquare Knight 58
              -25,    10,   // PieceSquare Knight 59
              -27,    30,   // PieceSquare Knight 60
              -22,     9,   // PieceSquare Knight 61
              -38,   -26,   // PieceSquare Knight 62
              -79,     0,   // PieceSquare Knight 63
              -35,    50,   // PieceSquare Bishop 0
              -38,    52,   // PieceSquare Bishop 1
              -89,    52,   // PieceSquare Bishop 2
              -61,    56,   // PieceSquare Bishop 3
              -55,    50,   // PieceSquare Bishop 4
             -110,    50,   // PieceSquare Bishop 5
              -24,    38,   // PieceSquare Bishop 6
              -39,    54,   // PieceSquare Bishop 7
              -26,    43,   // PieceSquare Bishop 8
              -39,    42,   // PieceSquare Bishop 9
                0,    25,   // PieceSquare Bishop 10
                2,    32,   // PieceSquare Bishop 11
               -4,    32,   // PieceSquare Bishop 12
               -3,    31,   // PieceSquare Bishop 13
              -22,    39,   // PieceSquare Bishop 14
              -20,    19,   // PieceSquare Bishop 15
                5,    47,   // PieceSquare Bishop 16
                4,    48,   // PieceSquare Bishop 17
               -2,    37,   // PieceSquare Bishop 18
               24,    20,   // PieceSquare Bishop 19
               40,    19,   // PieceSquare Bishop 20
              101,    26,   // PieceSquare Bishop 21
               27,    52,   // PieceSquare Bishop 22
               22,    48,   // PieceSquare Bishop 23
              -21,    34,   // PieceSquare Bishop 24
               -3,    53,   // PieceSquare Bishop 25
               -3,    38,   // PieceSquare Bishop 26
               35,    28,   // PieceSquare Bishop 27
               19,    32,   // PieceSquare Bishop 28
               19,    37,   // PieceSquare Bishop 29
                6,    33,   // PieceSquare Bishop 30
               -6,    49,   // PieceSquare Bishop 31
               -8,    37,   // PieceSquare Bishop 32
              -16,    29,   // PieceSquare Bishop 33
               -9,    46,   // PieceSquare Bishop 34
               21,    37,   // PieceSquare Bishop 35
               20,    22,   // PieceSquare Bishop 36
              -13,    29,   // PieceSquare Bishop 37
              -15,    24,   // PieceSquare Bishop 38
               15,    -2,   // PieceSquare Bishop 39
              -11,    18,   // PieceSquare Bishop 40
                9,    38,   // PieceSquare Bishop 41
               -3,    43,   // PieceSquare Bishop 42
                0,    42,   // PieceSquare Bishop 43
               -3,    60,   // PieceSquare Bishop 44
              -13,    35,   // PieceSquare Bishop 45
                3,    13,   // PieceSquare Bishop 46
                8,    17,   // PieceSquare Bishop 47
                3,    40,   // PieceSquare Bishop 48
                3,     6,   // PieceSquare Bishop 49
               19,    11,   // PieceSquare Bishop 50
              -17,    31,   // PieceSquare Bishop 51
               -2,    28,   // PieceSquare Bishop 52
                4,    15,   // PieceSquare Bishop 53
               16,    16,   // PieceSquare Bishop 54
               16,   -31,   // PieceSquare Bishop 55
               -3,     5,   // PieceSquare Bishop 56
               15,     5,   // PieceSquare Bishop 57
              -12,    14,   // PieceSquare Bishop 58
              -37,    29,   // PieceSquare Bishop 59
              -10,    29,   // PieceSquare Bishop 60
              -16,    31,   // PieceSquare Bishop 61
                6,    13,   // PieceSquare Bishop 62
              -19,    18,   // PieceSquare Bishop 63
                4,    45,   // PieceSquare Rook 0
               25,    53,   // PieceSquare Rook 1
               16,    59,   // PieceSquare Rook 2
               14,    71,   // PieceSquare Rook 3
               57,    48,   // PieceSquare Rook 4
               52,    52,   // PieceSquare Rook 5
               29,    69,   // PieceSquare Rook 6
               95,    38,   // PieceSquare Rook 7
              -41,    27,   // PieceSquare Rook 8
              -32,    29,   // PieceSquare Rook 9
               -6,    23,   // PieceSquare Rook 10
                4,    26,   // PieceSquare Rook 11
               15,    10,   // PieceSquare Rook 12
               93,    -9,   // PieceSquare Rook 13
               33,     2,   // PieceSquare Rook 14
               46,    -3,   // PieceSquare Rook 15
              -43,    84,   // PieceSquare Rook 16
                2,    53,   // PieceSquare Rook 17
               -2,    63,   // PieceSquare Rook 18
               17,    50,   // PieceSquare Rook 19
               52,    26,   // PieceSquare Rook 20
               91,    27,   // PieceSquare Rook 21
               91,    18,   // PieceSquare Rook 22
               36,    35,   // PieceSquare Rook 23
              -49,    69,   // PieceSquare Rook 24
              -14,    56,   // PieceSquare Rook 25
              -19,    57,   // PieceSquare Rook 26
                1,    46,   // PieceSquare Rook 27
              -20,    57,   // PieceSquare Rook 28
               27,    37,   // PieceSquare Rook 29
               32,    30,   // PieceSquare Rook 30
               13,    44,   // PieceSquare Rook 31
              -53,    55,   // PieceSquare Rook 32
              -47,    58,   // PieceSquare Rook 33
              -43,    57,   // PieceSquare Rook 34
              -19,    35,   // PieceSquare Rook 35
              -23,    34,   // PieceSquare Rook 36
              -28,    47,   // PieceSquare Rook 37
               33,    14,   // PieceSquare Rook 38
              -22,    37,   // PieceSquare Rook 39
              -50,    43,   // PieceSquare Rook 40
              -36,    33,   // PieceSquare Rook 41
              -41,    35,   // PieceSquare Rook 42
              -34,    25,   // PieceSquare Rook 43
              -21,    19,   // PieceSquare Rook 44
              -14,    17,   // PieceSquare Rook 45
               19,    -7,   // PieceSquare Rook 46
               -1,    -2,   // PieceSquare Rook 47
              -54,    25,   // PieceSquare Rook 48
              -52,    26,   // PieceSquare Rook 49
              -38,    25,   // PieceSquare Rook 50
              -26,    18,   // PieceSquare Rook 51
              -19,     6,   // PieceSquare Rook 52
               -7,     7,   // PieceSquare Rook 53
               15,   -13,   // PieceSquare Rook 54
              -21,     3,   // PieceSquare Rook 55
              -37,    42,   // PieceSquare Rook 56
              -30,    21,   // PieceSquare Rook 57
              -27,    39,   // PieceSquare Rook 58
              -16,    16,   // PieceSquare Rook 59
               -8,     8,   // PieceSquare Rook 60
               -5,    21,   // PieceSquare Rook 61
               13,    -4,   // PieceSquare Rook 62
              -15,    10,   // PieceSquare Rook 63
              -37,   208,   // PieceSquare Queen 0
              -20,   176,   // PieceSquare Queen 1
                9,   186,   // PieceSquare Queen 2
               19,   197,   // PieceSquare Queen 3
               40,   201,   // PieceSquare Queen 4
               84,   203,   // PieceSquare Queen 5
               23,   222,   // PieceSquare Queen 6
               13,   216,   // PieceSquare Queen 7
               -6,   173,   // PieceSquare Queen 8
              -32,   200,   // PieceSquare Queen 9
              -21,   220,   // PieceSquare Queen 10
                6,   202,   // PieceSquare Queen 11
              -27,   258,   // PieceSquare Queen 12
               72,   187,   // PieceSquare Queen 13
               -7,   230,   // PieceSquare Queen 14
               65,   203,   // PieceSquare Queen 15
              -20,   190,   // PieceSquare Queen 16
               15,   175,   // PieceSquare Queen 17
              -12,   216,   // PieceSquare Queen 18
                9,   197,   // PieceSquare Queen 19
               23,   250,   // PieceSquare Queen 20
               72,   266,   // PieceSquare Queen 21
               75,   213,   // PieceSquare Queen 22
               49,   223,   // PieceSquare Queen 23
               -3,   147,   // PieceSquare Queen 24
              -11,   195,   // PieceSquare Queen 25
                2,   177,   // PieceSquare Queen 26
               -8,   229,   // PieceSquare Queen 27
                6,   233,   // PieceSquare Queen 28
               22,   243,   // PieceSquare Queen 29
               15,   230,   // PieceSquare Queen 30
               30,   204,   // PieceSquare Queen 31
               -4,   159,   // PieceSquare Queen 32
              -14,   165,   // PieceSquare Queen 33
              -11,   191,   // PieceSquare Queen 34
                3,   200,   // PieceSquare Queen 35
                5,   197,   // PieceSquare Queen 36
                7,   188,   // PieceSquare Queen 37
               26,   186,   // PieceSquare Queen 38
               14,   192,   // PieceSquare Queen 39
                0,   114,   // PieceSquare Queen 40
                0,   136,   // PieceSquare Queen 41
                5,   145,   // PieceSquare Queen 42
              -11,   180,   // PieceSquare Queen 43
               -1,   174,   // PieceSquare Queen 44
                9,   174,   // PieceSquare Queen 45
               31,   139,   // PieceSquare Queen 46
               26,   157,   // PieceSquare Queen 47
              -19,   135,   // PieceSquare Queen 48
               -7,   133,   // PieceSquare Queen 49
                3,   141,   // PieceSquare Queen 50
               15,   125,   // PieceSquare Queen 51
               11,   136,   // PieceSquare Queen 52
               16,   121,   // PieceSquare Queen 53
               35,    47,   // PieceSquare Queen 54
               22,   100,   // PieceSquare Queen 55
                2,   113,   // PieceSquare Queen 56
               -2,   127,   // PieceSquare Queen 57
                3,   114,   // PieceSquare Queen 58
               12,   133,   // PieceSquare Queen 59
               23,    77,   // PieceSquare Queen 60
              -16,    98,   // PieceSquare Queen 61
                5,    64,   // PieceSquare Queen 62
               48,    25,   // PieceSquare Queen 63
              242,  -167,   // PieceSquare King 0
               34,    -5,   // PieceSquare King 1
              -10,    54,   // PieceSquare King 2
               -7,    28,   // PieceSquare King 3
              -21,    10,   // PieceSquare King 4
               32,    11,   // PieceSquare King 5
               92,   -11,   // PieceSquare King 6
              -35,   -57,   // PieceSquare King 7
              138,   -79,   // PieceSquare King 8
               74,    33,   // PieceSquare King 9
               62,    31,   // PieceSquare King 10
               87,     4,   // PieceSquare King 11
              -56,    20,   // PieceSquare King 12
             -147,    63,   // PieceSquare King 13
               61,    48,   // PieceSquare King 14
               -9,   -29,   // PieceSquare King 15
               87,    -8,   // PieceSquare King 16
                6,    35,   // PieceSquare King 17
               43,    49,   // PieceSquare King 18
                7,    48,   // PieceSquare King 19
               -1,    42,   // PieceSquare King 20
              -51,    43,   // PieceSquare King 21
              -70,    65,   // PieceSquare King 22
               49,   -30,   // PieceSquare King 23
               26,   -31,   // PieceSquare King 24
              -51,    25,   // PieceSquare King 25
              -17,    37,   // PieceSquare King 26
              -42,    44,   // PieceSquare King 27
             -194,    62,   // PieceSquare King 28
             -160,    46,   // PieceSquare King 29
             -159,    42,   // PieceSquare King 30
             -252,    10,   // PieceSquare King 31
              -87,   -14,   // PieceSquare King 32
               -7,     1,   // PieceSquare King 33
              -38,    12,   // PieceSquare King 34
             -101,    33,   // PieceSquare King 35
             -119,    31,   // PieceSquare King 36
              -95,    15,   // PieceSquare King 37
             -121,     6,   // PieceSquare King 38
             -168,   -13,   // PieceSquare King 39
              -30,   -42,   // PieceSquare King 40
               -4,   -25,   // PieceSquare King 41
              -57,    -4,   // PieceSquare King 42
              -82,     5,   // PieceSquare King 43
              -69,     6,   // PieceSquare King 44
              -70,    -8,   // PieceSquare King 45
              -48,   -25,   // PieceSquare King 46
              -70,   -30,   // PieceSquare King 47
               56,   -29,   // PieceSquare King 48
               16,     0,   // PieceSquare King 49
               25,    -7,   // PieceSquare King 50
              -23,     1,   // PieceSquare King 51
              -24,     8,   // PieceSquare King 52
              -27,     3,   // PieceSquare King 53
               23,    -8,   // PieceSquare King 54
               21,   -40,   // PieceSquare King 55
                5,   -43,   // PieceSquare King 56
               43,   -42,   // PieceSquare King 57
               48,   -34,   // PieceSquare King 58
              -44,   -23,   // PieceSquare King 59
               33,   -58,   // PieceSquare King 60
              -34,   -24,   // PieceSquare King 61
               39,   -46,   // PieceSquare King 62
               37,  -104,   // PieceSquare King 63
                0,     0,   // Passed rank 0
               -7,    17,   // Passed rank 1
               -9,    18,   // Passed rank 2
               -1,    44,   // Passed rank 3
               13,    76,   // Passed rank 4
               25,   126,   // Passed rank 5
               25,   122,   // Passed rank 6
                0,     0,   // Passed rank 7
              -11,   -23,   // Doubled
              -12,   -20,   // Isolated
               19,   117,   // BishopPair
               34,    11,   // RookOpenFile
               13,    17,   // RookSemiOpenFile
                4,    46,   // RookSeventh
               22,   -23,   // ShieldNear
               14,   -15,   // ShieldFar
               10,   -25,   // ShieldAdvanced
              -12,     4,   // ShieldNoOwnPawn
              -24,    -3,   // ShieldOpenFile
                8,     7,   // Mobility Knight
                7,     7,   // Mobility Bishop
                4,     5,   // Mobility Rook
                3,     5,   // Mobility Queen
               25,    33,   // PawnThreat
               32,    32,   // MinorThreat
                9,    14,   // Hanging
               29,    22,   // Outpost
                0,     0,   // Tempo
                0,     0,   // MobilityTable Knight 0
                0,     0,   // MobilityTable Knight 1
                0,     0,   // MobilityTable Knight 2
                0,     0,   // MobilityTable Knight 3
                0,     0,   // MobilityTable Knight 4
                0,     0,   // MobilityTable Knight 5
                0,     0,   // MobilityTable Knight 6
                0,     0,   // MobilityTable Knight 7
                0,     0,   // MobilityTable Knight 8
                0,     0,   // MobilityTable Bishop 0
                0,     0,   // MobilityTable Bishop 1
                0,     0,   // MobilityTable Bishop 2
                0,     0,   // MobilityTable Bishop 3
                0,     0,   // MobilityTable Bishop 4
                0,     0,   // MobilityTable Bishop 5
                0,     0,   // MobilityTable Bishop 6
                0,     0,   // MobilityTable Bishop 7
                0,     0,   // MobilityTable Bishop 8
                0,     0,   // MobilityTable Bishop 9
                0,     0,   // MobilityTable Bishop 10
                0,     0,   // MobilityTable Bishop 11
                0,     0,   // MobilityTable Bishop 12
                0,     0,   // MobilityTable Bishop 13
                0,     0,   // MobilityTable Rook 0
                0,     0,   // MobilityTable Rook 1
                0,     0,   // MobilityTable Rook 2
                0,     0,   // MobilityTable Rook 3
                0,     0,   // MobilityTable Rook 4
                0,     0,   // MobilityTable Rook 5
                0,     0,   // MobilityTable Rook 6
                0,     0,   // MobilityTable Rook 7
                0,     0,   // MobilityTable Rook 8
                0,     0,   // MobilityTable Rook 9
                0,     0,   // MobilityTable Rook 10
                0,     0,   // MobilityTable Rook 11
                0,     0,   // MobilityTable Rook 12
                0,     0,   // MobilityTable Rook 13
                0,     0,   // MobilityTable Rook 14
                0,     0,   // MobilityTable Queen 0
                0,     0,   // MobilityTable Queen 1
                0,     0,   // MobilityTable Queen 2
                0,     0,   // MobilityTable Queen 3
                0,     0,   // MobilityTable Queen 4
                0,     0,   // MobilityTable Queen 5
                0,     0,   // MobilityTable Queen 6
                0,     0,   // MobilityTable Queen 7
                0,     0,   // MobilityTable Queen 8
                0,     0,   // MobilityTable Queen 9
                0,     0,   // MobilityTable Queen 10
                0,     0,   // MobilityTable Queen 11
                0,     0,   // MobilityTable Queen 12
                0,     0,   // MobilityTable Queen 13
                0,     0,   // MobilityTable Queen 14
                0,     0,   // MobilityTable Queen 15
                0,     0,   // MobilityTable Queen 16
                0,     0,   // MobilityTable Queen 17
                0,     0,   // MobilityTable Queen 18
                0,     0,   // MobilityTable Queen 19
                0,     0,   // MobilityTable Queen 20
                0,     0,   // MobilityTable Queen 21
                0,     0,   // MobilityTable Queen 22
                0,     0,   // MobilityTable Queen 23
                0,     0,   // MobilityTable Queen 24
                0,     0,   // MobilityTable Queen 25
                0,     0,   // MobilityTable Queen 26
                0,     0,   // MobilityTable Queen 27
                0,     0,   // KingDanger 0
                0,     0,   // KingDanger 1
                0,     0,   // KingDanger 2
                0,     0,   // KingDanger 3
                0,     0,   // KingDanger 4
                0,     0,   // KingDanger 5
                0,     0,   // KingDanger 6
                0,     0,   // KingDanger 7
                0,     0,   // KingDanger 8
                0,     0,   // KingDanger 9
                0,     0,   // KingDanger 10
                0,     0,   // KingDanger 11
                0,     0,   // KingDanger 12
                0,     0,   // KingDanger 13
                0,     0,   // KingDanger 14
                0,     0,   // KingDanger 15
                0,     0,   // KingDanger 16
                0,     0,   // KingDanger 17
                0,     0,   // KingDanger 18
                0,     0,   // KingDanger 19
                0,     0,   // KingDanger 20
                0,     0,   // KingDanger 21
                0,     0,   // KingDanger 22
                0,     0,   // KingDanger 23
                0,     0,   // KingDanger 24
                0,     0,   // KingDanger 25
                0,     0,   // KingDanger 26
                0,     0,   // KingDanger 27
                0,     0,   // KingDanger 28
                0,     0,   // KingDanger 29
                0,     0,   // KingDanger 30
                0,     0,   // KingDanger 31
                0,     0,   // KingDanger 32
                0,     0,   // KingDanger 33
                0,     0,   // KingDanger 34
                0,     0,   // KingDanger 35
                0,     0,   // KingDanger 36
                0,     0,   // KingDanger 37
                0,     0,   // KingDanger 38
                0,     0,   // KingDanger 39
                0,     0,   // KingDanger 40
                0,     0,   // KingDanger 41
                0,     0,   // KingDanger 42
                0,     0,   // KingDanger 43
                0,     0,   // KingDanger 44
                0,     0,   // KingDanger 45
                0,     0,   // KingDanger 46
                0,     0,   // KingDanger 47
                0,     0,   // KingDanger 48
                0,     0,   // KingDanger 49
                0,     0,   // KingDanger 50
                0,     0,   // KingDanger 51
                0,     0,   // KingDanger 52
                0,     0,   // KingDanger 53
                0,     0,   // KingDanger 54
                0,     0,   // KingDanger 55
                0,     0,   // KingDanger 56
                0,     0,   // KingDanger 57
                0,     0,   // KingDanger 58
                0,     0,   // KingDanger 59
                0,     0,   // KingDanger 60
                0,     0,   // KingDanger 61
                0,     0,   // KingDanger 62
                0,     0,   // KingDanger 63
                0,     0,   // KingDanger 64
                0,     0,   // KingDanger 65
                0,     0,   // KingDanger 66
                0,     0,   // KingDanger 67
                0,     0,   // KingDanger 68
                0,     0,   // KingDanger 69
                0,     0,   // KingDanger 70
                0,     0,   // KingDanger 71
                0,     0,   // KingDanger 72
                0,     0,   // KingDanger 73
                0,     0,   // KingDanger 74
                0,     0,   // KingDanger 75
                0,     0,   // KingDanger 76
                0,     0,   // KingDanger 77
                0,     0,   // KingDanger 78
                0,     0,   // KingDanger 79
                0,     0,   // KingDanger 80
                0,     0,   // KingDanger 81
                0,     0,   // KingDanger 82
                0,     0,   // KingDanger 83
                0,     0,   // KingDanger 84
                0,     0,   // KingDanger 85
                0,     0,   // KingDanger 86
                0,     0,   // KingDanger 87
                0,     0,   // KingDanger 88
                0,     0,   // KingDanger 89
                0,     0,   // KingDanger 90
                0,     0,   // KingDanger 91
                0,     0,   // KingDanger 92
                0,     0,   // KingDanger 93
                0,     0,   // KingDanger 94
                0,     0,   // KingDanger 95
                0,     0,   // KingDanger 96
                0,     0,   // KingDanger 97
                0,     0,   // KingDanger 98
                0,     0,   // KingDanger 99
                0,     0,   // SafeCheck Knight
                0,     0,   // SafeCheck Bishop
                0,     0,   // SafeCheck Rook
                0,     0,   // SafeCheck Queen
                0,     0,   // PawnStorm rank 0
                0,     0,   // PawnStorm rank 1
                0,     0,   // PawnStorm rank 2
                0,     0,   // PawnStorm rank 3
                0,     0,   // PawnStorm rank 4
                0,     0,   // PawnStorm rank 5
                0,     0,   // PawnStorm rank 6
                0,     0,   // PawnStorm rank 7
                0,     0,   // WeakKingSquare
                0,     0,   // Backward
                0,     0,   // Phalanx rank 0
                0,     0,   // Phalanx rank 1
                0,     0,   // Phalanx rank 2
                0,     0,   // Phalanx rank 3
                0,     0,   // Phalanx rank 4
                0,     0,   // Phalanx rank 5
                0,     0,   // Phalanx rank 6
                0,     0,   // Phalanx rank 7
                0,     0,   // Supported rank 0
                0,     0,   // Supported rank 1
                0,     0,   // Supported rank 2
                0,     0,   // Supported rank 3
                0,     0,   // Supported rank 4
                0,     0,   // Supported rank 5
                0,     0,   // Supported rank 6
                0,     0,   // Supported rank 7
                0,     0,   // PassedBlocked rank 0
                0,     0,   // PassedBlocked rank 1
                0,     0,   // PassedBlocked rank 2
                0,     0,   // PassedBlocked rank 3
                0,     0,   // PassedBlocked rank 4
                0,     0,   // PassedBlocked rank 5
                0,     0,   // PassedBlocked rank 6
                0,     0,   // PassedBlocked rank 7
                0,     0,   // PassedFree rank 0
                0,     0,   // PassedFree rank 1
                0,     0,   // PassedFree rank 2
                0,     0,   // PassedFree rank 3
                0,     0,   // PassedFree rank 4
                0,     0,   // PassedFree rank 5
                0,     0,   // PassedFree rank 6
                0,     0,   // PassedFree rank 7
                0,     0,   // PassedOwnKingDistance rank 0
                0,     0,   // PassedOwnKingDistance rank 1
                0,     0,   // PassedOwnKingDistance rank 2
                0,     0,   // PassedOwnKingDistance rank 3
                0,     0,   // PassedOwnKingDistance rank 4
                0,     0,   // PassedOwnKingDistance rank 5
                0,     0,   // PassedOwnKingDistance rank 6
                0,     0,   // PassedOwnKingDistance rank 7
                0,     0,   // PassedEnemyKingDistance rank 0
                0,     0,   // PassedEnemyKingDistance rank 1
                0,     0,   // PassedEnemyKingDistance rank 2
                0,     0,   // PassedEnemyKingDistance rank 3
                0,     0,   // PassedEnemyKingDistance rank 4
                0,     0,   // PassedEnemyKingDistance rank 5
                0,     0,   // PassedEnemyKingDistance rank 6
                0,     0,   // PassedEnemyKingDistance rank 7
                0,     0,   // RookBehindPassed
                0,     0,   // BadBishopPawns
                0,     0,   // TrappedBishop
                0,     0,   // TrappedRook
                0,     0,   // MinorBehindPawn
                0,     0,   // Space
        };
    }
}
