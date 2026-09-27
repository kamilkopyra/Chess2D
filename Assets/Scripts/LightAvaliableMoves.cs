//using System.Collections.Generic;
//using UnityEngine;

//public class LightAvailableMoves : MonoBehaviour
//{
//    public GameObject dotPrefab;  // Przypisz w Inspectorze prefab kropki
//    private List<GameObject> dots = new List<GameObject>();

//    public void LightSquares(ChessPiece piece)
//    {
//        ClearHighlights();

//        bool[,] moves = piece.GetLegalMoves();

//        for (int x = 0; x < 8; x++)
//        {
//            for (int y = 0; y < 8; y++)
//            {
//                if (moves[x, y])
//                {
//                    Vector3 pos = new Vector3(x - 3.5f, y - 3.5f, 0);
//                    GameObject dot = Instantiate(dotPrefab, pos, Quaternion.identity);
//                    dots.Add(dot);
//                }
//            }
//        }
//    }

//    public void ClearHighlights()
//    {
//        foreach (GameObject dot in dots)
//        {
//            Destroy(dot);
//        }
//        dots.Clear();
//    }
//}



using System.Collections.Generic;
using UnityEngine;

public class LightAvailableMoves : MonoBehaviour
{
    public GameObject dotPrefab;  // Przypisz w Inspectorze prefab kropki
    public GameObject redSquare;  // Prefab czerwonego podświetlenia

    private List<GameObject> dots = new List<GameObject>();
    private List<GameObject> redSquares = new List<GameObject>();
    private BoardCreator board;

    public void LightSquares(ChessPiece piece)
    {
        ClearHighlights();

        if (dotPrefab == null || redSquare == null)
        {
            Debug.LogError("Nie przypisano prefabów w Inspectorze!");
            return;
        }

        board = FindFirstObjectByType<BoardCreator>();
        var mover = FindFirstObjectByType<PieceMover>();
        bool[,] allMoves = piece.GetLegalMoves();

        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                if (allMoves[x, y] && mover.WillKingBeSafe(piece, x, y))
                {
                    Vector3 pos = new Vector3(x - 3.5f, y - 3.5f, 0);

                    if (board.board[x, y] != null)
                    {
                        GameObject red = Instantiate(redSquare, pos, Quaternion.identity);
                        redSquares.Add(red);
                    }
                    else
                    {
                        GameObject dot = Instantiate(dotPrefab, pos, Quaternion.identity);
                        dots.Add(dot);
                    }
                }
            }
        }
    }

    public void ClearHighlights()
    {
        foreach (GameObject dot in dots)
            if (dot != null) Destroy(dot);

        foreach (GameObject red in redSquares)
            if (red != null) Destroy(red);

        dots.Clear();
        redSquares.Clear();
    }
}