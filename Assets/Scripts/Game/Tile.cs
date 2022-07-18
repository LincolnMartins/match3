using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Tile : MonoBehaviour
{
    public int x; // posição X do tile no grid(array)
    public int y; // posição Y do tile no grid(array)
    [HideInInspector] public bool isEmpty; // verifica se o tile está vazio

    // Tiles vizinhos
    public Tile Left => x > 0 ? Board.Instance.Tiles[x-1,y] : null;
    public Tile Right => x < Board.Instance.Width-1 ? Board.Instance.Tiles[x+1,y] : null;
    public Tile Top => y > 0 ? Board.Instance.Tiles[x,y-1] : null;
    public Tile Bottom => y < Board.Instance.Height-1 ? Board.Instance.Tiles[x,y+1] : null;
    public Tile[] Neighbours => new[]
    {
        Left,
        Right,
        Top,
        Bottom
    };

    public GameObject piecePrefab; //Prefab das peças
    [HideInInspector] public GameObject pieceObj; //Objeto da peça posicionada no tile

    private Piece _piece; // Dados da peça posicionada no tile
    public Piece piece // Getter/Setter
    {
        get => _piece;

        set
        {
            if(_piece == value) return;
            _piece = value;
            if(pieceObj != null)
                pieceObj.GetComponent<Image>().sprite = _piece.icon;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        //Inicializa a peça setada no tile
        pieceObj = Instantiate(piecePrefab, transform.position, Quaternion.identity);
        pieceObj.transform.SetParent(transform);
        pieceObj.transform.localScale = new Vector3(0.8f,0.8f,0.8f);
        pieceObj.GetComponent<Image>().sprite = _piece.icon;
        pieceObj.name = "Piece ("+x+","+y+")";
    }

    // Chamado quando jogador clica no tile
    public void Click()
    {
        Board.Instance.Select(this);
    }

    //Captura todos os tiles com peças conectadas iguais e de seus vizinhos
    public List<Tile> GetMatchedTiles(List<Tile> checkedTiles = null)
    {
        var result = new List<Tile>{this};

        if(checkedTiles == null) checkedTiles = new List<Tile>{this};
        else checkedTiles.Add(this);

        foreach(var neighbour in Neighbours)
        {
            if(neighbour == null || checkedTiles.Contains(neighbour) || neighbour.piece != _piece) continue;

            if((Left != null && Left == neighbour && Right != null && Right.piece == neighbour.piece) || //se a peça esta no meio da linha OU
            (Right != null && Right == neighbour && neighbour.Right != null && neighbour.Right.piece == neighbour.piece) || //se a peça esta na ponta esquerda OU
            (Left != null && Left == neighbour && neighbour.Left != null && neighbour.Left.piece == neighbour.piece) || //se a peça esta na ponta direita OU
            (Top != null && Top == neighbour && Bottom != null && Bottom.piece == neighbour.piece) || //se a peça esta no meio da coluna OU
            (Bottom != null && Bottom == neighbour && neighbour.Bottom != null && neighbour.Bottom.piece == neighbour.piece) || //se a peça esta na ponta superior OU
            (Top != null && Top == neighbour && neighbour.Top != null && neighbour.Top.piece == neighbour.piece)) //se a peça esta na ponta inferior
                result.AddRange(neighbour.GetMatchedTiles(checkedTiles));
        }

        return result;
    }
}