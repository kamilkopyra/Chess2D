using System;
using UnityEngine;
using UnityEngine.UI;

public class PawnPromotionUI : MonoBehaviour
{
    public GameObject panel;
    public Button queenButton, rookButton, bishopButton, knightButton;

    public Sprite whiteQueen, whiteRook, whiteBishop, whiteKnight;
    public Sprite blackQueen, blackRook, blackBishop, blackKnight;

    private Action<string> callback;

    public void Show(bool isWhite, Action<string> onPieceChosen)
    {
        callback = onPieceChosen;

        queenButton.image.sprite = isWhite ? whiteQueen : blackQueen;
        rookButton.image.sprite = isWhite ? whiteRook : blackRook;
        bishopButton.image.sprite = isWhite ? whiteBishop : blackBishop;
        knightButton.image.sprite = isWhite ? whiteKnight : blackKnight;

        panel.SetActive(true);
    }

    public void ChooseQueen() => Choose("Queen");
    public void ChooseRook() => Choose("Rook");
    public void ChooseBishop() => Choose("Bishop");
    public void ChooseKnight() => Choose("Knight");

    private void Choose(string pieceName)
    {
        panel.SetActive(false);
        callback?.Invoke(pieceName);
    }

 
}

