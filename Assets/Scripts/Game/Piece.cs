using UnityEngine;

[CreateAssetMenu(menuName = "match3/Piece")]
public class Piece : ScriptableObject
{
    public Sprite icon; // imagem da peça
    public int value; // valor da peça adicionado ao score
}