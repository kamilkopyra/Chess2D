using UnityEngine;

public class PieceMover : MonoBehaviour
{
    private ChessPiece selectedPiece;

    static int xmin = 0;
    static int xmax = 7;
    static int ymin = 0;
    static int ymax = 7;

    bool isWhiteTurn = true;
    private bool isWaitingForPromotion = false;
    private ChessPiece pieceWithFirstMove = null;
    private int halfMoveCounter = 0;
    private LightAvailableMoves lightManager;



   
   
    void Start()
    {
        lightManager = FindFirstObjectByType<LightAvailableMoves>();
    }

    
    void Update()
    {

        if (isWaitingForPromotion) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Collider2D hit = Physics2D.OverlapPoint(mouseWorldPos);

            if (hit != null)
            {
                ChessPiece clickedPiece = hit.GetComponent<ChessPiece>();



                if (clickedPiece != null)
                {
                    if (selectedPiece == null)
                    {
                        // Kiedy klikam na figurę pierwszy raz
                        
                        if (clickedPiece.isWhite != isWhiteTurn)
                        {
                            Debug.Log("Nie Twoja kolej.");
                            return;
                        }

                        selectedPiece = clickedPiece;
                        lightManager.LightSquares(selectedPiece);
                        Debug.Log("Wybrano figurę: " + clickedPiece.name);
                    }
                    else
                    {

                        if (selectedPiece.isWhite == clickedPiece.isWhite)
                        {
                            // Kiedy zmieniam wybór figury
                            lightManager.ClearHighlights();
                            selectedPiece = clickedPiece;
                            lightManager.LightSquares(selectedPiece);

                            clickedPiece = null;
                            

                        }

                        else
                        {
                            //Kiedy zbijam
                            
                            if (MovePiece(selectedPiece, clickedPiece.transform.position)) { isWhiteTurn = !isWhiteTurn; }
                            selectedPiece = null;
                            lightManager.ClearHighlights();

                        }

                    }
                }
                else if (selectedPiece != null)
                {
                    // Kiedy klikam w planszę
                    
                    if (MovePiece(selectedPiece, mouseWorldPos)) { isWhiteTurn = !isWhiteTurn; }
                    selectedPiece = null;
                    lightManager.ClearHighlights();

                }
            }
            else if (selectedPiece != null)
            {
                
                if (MovePiece(selectedPiece, mouseWorldPos)) { isWhiteTurn = !isWhiteTurn; }
                selectedPiece = null; // null dla odznaczenia obecnego wyboru
                lightManager.ClearHighlights();

            }

        }
        


    }


    bool MovePiece(ChessPiece piece, Vector2 worldPosition)
    {
        int newX = Mathf.RoundToInt(worldPosition.x + 3.5f);
        int newY = Mathf.RoundToInt(worldPosition.y + 3.5f);

        if (newX < xmin || newX > xmax || newY < ymin || newY > ymax) return false;

        // GetLegalMoves odrzuca pola z własnymi figurami, więc kliknięcie we własną figurę
        // (np. gdy trafimy w collider pola zamiast figury) nic nie zmienia.
        // Musi być sprawdzone PRZED wyczyszczeniem flagi en passant, inaczej bicie w przelocie zniknie z listy.
        bool[,] moves = piece.GetLegalMoves();
        if (!moves[newX, newY]) return false;

        if (!WillKingBeSafe(piece, newX, newY))
        {
            Debug.Log("Nie możesz wykonać tego ruchu – król nadal byłby w szachu.");
            return false;
        }

        BoardCreator boardCreator = FindFirstObjectByType<BoardCreator>();
        if (boardCreator == null) return false;

        // Od tego miejsca ruch jest legalny i na pewno zostanie wykonany

        // Bicie w przelocie jest możliwe tylko w ruchu zaraz po skoku piona o dwa pola
        if (pieceWithFirstMove != null)
        {
            pieceWithFirstMove.justMadeFirstMove = false;
            pieceWithFirstMove = null;
        }

        int oldX = piece.currentX;
        int oldY = piece.currentY;
        bool wasCapture = false;

        if (!MakeCastle(piece, newX, newY))
        {
            ChessPiece targetPiece = boardCreator.board[newX, newY];

            if (piece is Pawn && targetPiece == null && newX != oldX)
            {
                CheckIfenPassant(piece, newX, newY, boardCreator);
                wasCapture = true;
            }

            if (targetPiece != null)
            {
                Destroy(targetPiece.gameObject); // Usuwam figurę przeciwnika
                Debug.Log($"{piece.name} bije {targetPiece.name} na [{newX}, {newY}]");
                wasCapture = true;
            }

            // aktualizacja matematycznej reprezentacji planszy
            boardCreator.board[oldX, oldY] = null;
            boardCreator.board[newX, newY] = piece;
            piece.SetPosition(newX, newY);

            // Flaga dla en passant tylko przy skoku piona o dwa pola
            if (piece is Pawn && Mathf.Abs(newY - oldY) == 2)
            {
                piece.justMadeFirstMove = true;
                pieceWithFirstMove = piece;
            }
            piece.hasBeenMoved = true;
        }

        selectedPiece = null;

        // Mat i pat mają pierwszeństwo przed regułą 50 ruchów
        bool gameOver = CheckGameEnd(!piece.isWhite);
        if (!gameOver) Update50MoveRule(piece is Pawn || wasCapture);

        PromotePawn(piece);

        return true;
    }

    // Sprawdza mata, pata i szacha dla strony, która ma teraz ruch. Zwraca true, jeśli gra się skończyła.
    bool CheckGameEnd(bool colorToMove)
    {
        bool noMoves = IsItCheckmate(colorToMove);
        bool inCheck = IsKingInCheck(colorToMove); // na końcu, żeby flaga isChecked króla była aktualna

        if (noMoves && inCheck)
        {
            Debug.Log("Szach Mat. Koniec Gry");
            EndGame.EndTheGame();
        }
        else if (noMoves)
        {
            Debug.Log("Pat... Koniec Gry");
            EndGame.Pat();
        }
        else if (inCheck)
        {
            Debug.Log("Szach");
        }

        return noMoves;
    }





    // Funkcje związane z logiką szacha
    //
    // metoda do sprawdzenia stanu Króla
    public bool IsKingInCheck(bool isWhite)
    {
        BoardCreator board = FindFirstObjectByType<BoardCreator>();

        ChessPiece king = null;


        // Szukamy króla
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                ChessPiece piece = board.board[x, y];
                if (piece != null && piece is KIng && piece.isWhite == isWhite)
                {
                    king = piece;
                    break;
                }
            }
        }

        if (king == null)
        {
            Debug.LogWarning("Nie znaleziono króla!");
            return false;
        }

        king.isChecked = false;
        // Sprawdzenie, czy któraś z figur przeciwnika atakuje króla
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                ChessPiece piece = board.board[x, y];
                if (piece != null && piece.isWhite != isWhite)
                {
                    bool[,] attacks = piece.GetAttackedSquares();
                    if (attacks[king.currentX, king.currentY])
                    {
                        king.isChecked = true;
                        Debug.Log("Szach");
                        return true;
                    }
                }
            }
        }
        king.isChecked = false;
        return false;
    }


    // Zaznacza już kiedy jest szach i kiedy go nie ma
    // Teraz muszę dodać logikę, kiedy jest szach, muszę zrobić coś żeby go nie było

    // willKingBeSafe ma służyć do tego, że kiedy dam szacha to muszę wykonąć ruch który go zasłoni
    public bool WillKingBeSafe(ChessPiece piece, int targetX, int targetY)
    {
        var boardC = FindFirstObjectByType<BoardCreator>();

        // głęboka kopia tablicy referencji (nie tworzymy nowych obiektów ChessPiece, tylko nową macierz wskaźników)
        ChessPiece[,] backup = boardC.board.Clone() as ChessPiece[,];

        int origX = piece.currentX, origY = piece.currentY;

        // symulacja na ORYGINALE
        // En passant: bity pion stoi obok, na rzędzie startowym bijącego, a nie na polu docelowym
        if (piece is Pawn && boardC.board[targetX, targetY] == null && targetX != origX)
            boardC.board[targetX, origY] = null;

        boardC.board[origX, origY] = null;
        boardC.board[targetX, targetY] = piece;
        piece.currentX = targetX;
        piece.currentY = targetY;

        bool safe = !IsKingInCheck(piece.isWhite);

        // przywrócenie ze ZROBIENIEGO BACKUPU
        boardC.board = backup;
        piece.currentX = origX;
        piece.currentY = origY;

        return safe;
    }


    // Sprawdzam czy istnieje ruch blokujący szacha
    bool IsItCheckmate(bool colorToCheck)
    {
        BoardCreator board = FindFirstObjectByType<BoardCreator>();
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                Debug.Log("Znaleziono obiekt");
                ChessPiece piece = board.board[i, j];

                if (piece != null && colorToCheck == piece.isWhite)
                {
                    bool[,] moves = new bool[8, 8];
                    moves = piece.GetLegalMoves();
                    for (int k = 0; k < 8; k++)
                    {
                        for (int l = 0; l < 8; l++)
                        {

                            if (moves[k, l] && WillKingBeSafe(piece, k, l))
                            {

                                Debug.Log("Szach. Nie ma mata");
                                return false; // istnieje ruch

                            }
                        }



                    }

                }
            }
        }


        Debug.Log("Mat. Koniec Gry");
        return true; //Szach mat

    }







    // Funkcje związane z promocją piona
    
    void PromotePawn(ChessPiece pawn)
    {
        if (pawn is Pawn)
        {
            
            if ((pawn.isWhite && pawn.currentY == 7) || (!pawn.isWhite && pawn.currentY == 0))
            {
                isWaitingForPromotion = true;
               
                PawnPromotionUI promotionUI = FindFirstObjectByType<PawnPromotionUI>();
                BoardCreator board = FindFirstObjectByType<BoardCreator>();

                // Zatrzymaj pozycję i kolor przed zniszczeniem
                int x = pawn.currentX;
                int y = pawn.currentY;
                bool isWhite = pawn.isWhite;

                promotionUI.Show(isWhite, pieceName =>
                {
                    // Usuń pionka dopiero po wyborze
                    board.board[x, y] = null;
                    Destroy(pawn.gameObject);

                    SpawnPromotedPiece(pieceName, isWhite, x, y);
                    isWaitingForPromotion = false;
                });
            }
        }
    }

    void SpawnPromotedPiece(string pieceName, bool isWhite, int x, int y)
    {
        BoardCreator board = FindFirstObjectByType<BoardCreator>();

        GameObject prefab = null;

        switch (pieceName)
        {
            case "Queen":
                prefab = isWhite ? board.QueenWhite : board.QueenBlack;
                break;
            case "Rook":
                prefab = isWhite ? board.RookWhite : board.RookBlack;
                break;
            case "Bishop":
                prefab = isWhite ? board.BishopWhite : board.BishopBlack;
                break;
            case "Knight":
                prefab = isWhite ? board.KnightWhite : board.KnightBlack;
                break;
            default:
               
                return;
        }

        GameObject newPiece = Instantiate(
            prefab,
            new Vector3(x - 3.5f, y - 3.5f),
            Quaternion.identity
        );

        ChessPiece newChessPiece = newPiece.GetComponent<ChessPiece>();
        newChessPiece.SetPosition(x, y);
        newChessPiece.isWhite = isWhite;
        board.board[x, y] = newChessPiece;

  
    }

    
    private bool MakeCastle(ChessPiece piece, int x, int y)
    {
        if (!(piece is KIng king)) return false;
        if (piece.hasBeenMoved) return false;

        BoardCreator board = FindFirstObjectByType<BoardCreator>();

        // Roszada biała długa
        if (x == 2 && y == 0)
        {
            ChessPiece rook = board.board[0, 0];
            if (rook is Rook r && !r.hasBeenMoved)
            {
                board.board[4, 0] = null;
                board.board[2, 0] = king;
                king.SetPosition(2, 0);

                board.board[0, 0] = null;
                board.board[3, 0] = r;
                r.SetPosition(3, 0);

                king.hasBeenMoved = true;
                r.hasBeenMoved = true;
                return true;
            }
        }

        // Roszada biała krótka
        if (x == 6 && y == 0)
        {
            ChessPiece rook = board.board[7, 0];
            if (rook is Rook r && !r.hasBeenMoved)
            {
                board.board[4, 0] = null;
                board.board[6, 0] = king;
                king.SetPosition(6, 0);

                board.board[7, 0] = null;
                board.board[5, 0] = r;
                r.SetPosition(5, 0);

                king.hasBeenMoved = true;
                r.hasBeenMoved = true;
                return true;
            }
        }

        // Roszada czarna długa
        if (x == 2 && y == 7)
        {
            ChessPiece rook = board.board[0, 7];
            if (rook is Rook r && !r.hasBeenMoved)
            {
                board.board[4, 7] = null;
                board.board[2, 7] = king;
                king.SetPosition(2, 7);

                board.board[0, 7] = null;
                board.board[3, 7] = r;
                r.SetPosition(3, 7);

                king.hasBeenMoved = true;
                r.hasBeenMoved = true;
                return true;
            }
        }

        // Roszada czarna krótka
        if (x == 6 && y == 7)
        {
            ChessPiece rook = board.board[7, 7];
            if (rook is Rook r && !r.hasBeenMoved)
            {
                board.board[4, 7] = null;
                board.board[6, 7] = king;
                king.SetPosition(6, 7);

                board.board[7, 7] = null;
                board.board[5, 7] = r;
                r.SetPosition(5, 7);

                king.hasBeenMoved = true;
                r.hasBeenMoved = true;
                return true;
            }
        }

        return false;
    }

    private void CheckIfenPassant(ChessPiece pawn, int newX, int newY, BoardCreator boardCreator) 
    {
        if (pawn is Pawn)
        {
            

            if (boardCreator.board[newX, newY] == null && newX != pawn.currentX) // tzn. że wykryto enPassant
            { 
                int dir = pawn.isWhite ? -1 : 1;
                ChessPiece capturedPawn = boardCreator.board[newX,newY + dir];
                Destroy(capturedPawn.gameObject);
                boardCreator.board[newX, newY + dir] = null;

            }

            
        }
        
    }


    // Informację o biciu dostajemy z MovePiece, bo Destroy() usuwa obiekt dopiero
    // na końcu klatki i liczenie figur na scenie nie widziało bicia
    private void Update50MoveRule(bool pawnMoveOrCapture)
    {
        // Jeśli ruch pionkiem albo było bicie, resetuj licznik
        if (pawnMoveOrCapture)
        {
            halfMoveCounter = 0;
        }
        else
        {
            halfMoveCounter++;
        }

        if (halfMoveCounter >= 100)
        {
            Debug.Log("Remis przez regułę 50 posunięć");
            EndGame.DrawBy50MovesRule();
        }
    }



}








