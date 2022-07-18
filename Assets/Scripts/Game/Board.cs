using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Board : MonoBehaviour
{
    public static Board Instance { get; private set; } //Singleton

    public int Width; //Largura do grid
    public int Height; //Altura do grid

    public GameObject rowPrefab; //Prefab das colunas do grid
    public GameObject tilePrefab; //Prefab dos tiles do grid
    
    public Tile[,] Tiles { get; private set; } //Array contendo numero total de Tiles do grid
    public Piece[] Pieces; //Array contendo os dados das peças do jogo

    private List<Tile> _selection = new List<Tile>(); //Array de peças selecionadas pelo jogador (max 2 peças por vez)

    //Audio Sources
    public AudioSource matchSound;
    public AudioSource failSound;
    public AudioSource resetSound;

    public Text scoreText; //texto da pontuação na tela
    private int Score = 0; // pontuação

    private bool canPlay; //variavel que controla quando o jogador pode ou não mover as peças

    // Chamado quando o objeto é instanciado
    void Awake()
    {
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        Tiles = new Tile[Height, Width];

        for (int y = 0; y < Height; y++)
        {
            //Gera as colunas do grid
            GameObject newRow = Instantiate(rowPrefab, new Vector2(0,y), Quaternion.identity);
            newRow.transform.SetParent(transform);
            newRow.transform.localScale = new Vector3(1,1,1);
            newRow.name = "Row("+y+")";

            //Preenche a matriz do grid com os tiles
            for (int x = 0; x < Width; x++)
            {
                GameObject newTile = Instantiate(tilePrefab, new Vector2(x,y), Quaternion.identity);
                newTile.transform.SetParent(newRow.transform);
                newTile.transform.localScale = new Vector3(1,1,1);
                newTile.name = "Tile("+x+","+y+")";
                Tiles[x,y] = newTile.GetComponent<Tile>();
                Tiles[x,y].x = x;
                Tiles[x,y].y = y;
            }
        }

        InitGame();        
    }

    // Update is called once per frame
    void Update()
    {
        //Mantém o texto do score do jogador atualizado
        if (Score > int.Parse(scoreText.text))
            scoreText.text = (int.Parse(scoreText.text) + 1).ToString();
        else if (Score < int.Parse(scoreText.text))
            scoreText.text = (int.Parse(scoreText.text) - 1).ToString();
    }

    //Inicia o jogo
    private void InitGame()
    {
        foreach (var tile in Tiles)
        {
            tile.piece = Pieces[Random.Range(0, Pieces.Length)]; //Distribui as peças do jogo no grid
            tile.isEmpty = false;

            //Evita o match de peças distribuídas no início
            while(tile.GetMatchedTiles().Count >= 2)
                tile.piece = Pieces[Random.Range(0, Pieces.Length)];
            
            canPlay = true;
        }
    }

    //Seleciona o Tile
    public void Select(Tile tile)
    {
        if(!canPlay) return;
        if(!_selection.Contains(tile)) _selection.Add(tile);
        if(_selection.Count < 2) { tile.gameObject.GetComponent<AudioSource>().Play(); return; } //reproduz som caso seja a primeira peça selecionada

        if(_selection[1] != _selection[0].Left && _selection[1] != _selection[0].Right && _selection[1] != _selection[0].Top && _selection[1] != _selection[0].Bottom)
        {
            _selection[0] = _selection[1];
            _selection.RemoveAt(1);
        }
        else
        {
            canPlay = false;
            StartCoroutine(SwapPieces(_selection[0], _selection[1], 0.5f));
        }
    }

    //Troca duas peças de lugar
    public IEnumerator SwapPieces(Tile tile1, Tile tile2, float time)
    {
        //move as peças
        for (float t = 0; t <= 1*time; t += Time.deltaTime)
        {
            tile1.pieceObj.transform.position = Vector2.Lerp(tile1.pieceObj.transform.position, tile2.transform.position, t/time);
            tile2.pieceObj.transform.position = Vector2.Lerp(tile2.pieceObj.transform.position, tile1.transform.position, t/time);
            yield return null;
        }

        //troca as peças de lugar na hierarquia da cena
        var tmp1 = tile2.pieceObj;        
        tile2.pieceObj = tile1.pieceObj;        
        tile1.pieceObj = tmp1;

        tile1.pieceObj.transform.SetParent(tile1.transform);
        tile2.pieceObj.transform.SetParent(tile2.transform);

        //troca os metadados dos tiles
        var tmp2 = tile1.piece;
        tile1.piece = tile2.piece;
        tile2.piece = tmp2;

        //Verifica o match, caso nao haja um match retorna as peças pro lucar
        if(CanMatch(tile1, tile2)) Match();
        else if(_selection.Count > 1)
        {
            failSound.Play(); //reproduz som de falha
            StartCoroutine(SwapPieces(_selection[0], _selection[1], 0.5f));
            canPlay = true;
        }

        _selection.Clear(); //Limpa seleção de peças
    }

    //Verifica de os tiles selecionados podem dar match ou não após serem movidos
    private bool CanMatch(Tile tile1, Tile tile2)
    {
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                if(Tiles[x,y].GetMatchedTiles().Count >= 2 && (Tiles[x,y] == tile1 || Tiles[x,y] == tile2))
                    return true;

        return false;
    }

    //Realiza o match das peças combinadas
    private void Match()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                var matchTiles = Tiles[x,y].GetMatchedTiles();
                if(matchTiles.Count < 3) continue;
                foreach (var tile in matchTiles)
                {
                    Score += tile.piece.value;
                    StartCoroutine(DestroyPiece(tile, 0.2f));
                }
                matchSound.Play(); //reproduz som de match
            }
        }

        StartCoroutine(RefillBoard());
    }

    //Destroi as peças do grid
    private IEnumerator DestroyPiece(Tile tile, float time)
    {
        //animação das peças sumindo
        for (float t = 0; t <= 1*time; t += Time.deltaTime)
        {
            tile.pieceObj.transform.localScale = tile.pieceObj.transform.localScale - new Vector3(0.1f,0.1f,0.1f)*t/time;
            if(tile.pieceObj.transform.localScale.x <= 0 && tile.pieceObj.transform.localScale.y <= 0 && tile.pieceObj.transform.localScale.z <= 0) break;
            yield return null;
        }

        tile.pieceObj.gameObject.SetActive(false);
        tile.isEmpty = true;
    }

    //Ajusta as peças nas colunas do grid
    private IEnumerator RefillBoard()
    {
        yield return new WaitForSeconds(.2f);

        var count = 0;
        for (int x = Width-1; x >= 0; x--)
        {
            for (int y = Height-1; y >= 0; y--)
            {
                //verifica se o tile está vazio
                if(Tiles[x,y].isEmpty) count++; //se a peça estiver inativa adiciona +1 ao contador
                else if(count > 0)
                {
                    MoveDown(new Vector2(x,y), count, 1000); // se o tile nao estiver vazio move a peça o numero de casas vazias que houve abaixo
                    yield return null;
                }
            }

            count = 0;
        }
        
        yield return FillEmptyTiles();
    }

    // move as peças para o tile inferior
    private void MoveDown(Vector2 initialpos, int tilecount, float time)
    {
        int count = 0;
        while(count < tilecount)
        {
            var tile = Tiles[(int)initialpos.x,(int)initialpos.y+count];
            if(tile.Bottom == null) continue;
            
            //move a peça
            for (float t = 0; t <= 1*time; t += Time.deltaTime)
                tile.pieceObj.transform.position = Vector2.Lerp(tile.pieceObj.transform.position, tile.Bottom.transform.position, t/time);

            tile.pieceObj.transform.SetParent(tile.Bottom.gameObject.transform);
            tile.Bottom.pieceObj.transform.SetParent(tile.gameObject.transform);

            //troca as peças de lugar na hierarquia da cena
            var tmp1 = tile.Bottom.pieceObj;
            tile.Bottom.pieceObj = tile.pieceObj;
            tile.pieceObj = tmp1;
            
            //troca os metadados dos tiles
            var tmp2 = tile.piece;
            tile.piece = tile.Bottom.piece;
            tile.Bottom.piece = tmp2;

            tile.isEmpty = true;
            tile.Bottom.isEmpty = false;

            count++;
        }
    }

    //Preenche os espaços vazios do grid
    private IEnumerator FillEmptyTiles()
    {
        foreach (var tile in Tiles)
        {
            if(!tile.isEmpty) continue;
            else tile.isEmpty = false;

            //Gera nova peça e a posiciona acima da coluna
            tile.piece = Pieces[Random.Range(0, Pieces.Length)];
            tile.pieceObj.transform.localScale = new Vector3(0.8f,0.8f,0.8f);
            tile.pieceObj.transform.position = new Vector3(tile.pieceObj.transform.position.x,tile.pieceObj.transform.position.y+350,tile.pieceObj.transform.position.z);
            tile.pieceObj.SetActive(true);
            
            //move a peça até o tile
            StartCoroutine(Fall(tile, 0.2f));
        }

        yield return new WaitForSeconds(.3f);
        SearchForNewMatches();
    }

    //Animação da peça caindo sobre o grid
    private IEnumerator Fall(Tile tile, float time)
    {
        for (float t = 0; t <= 1*time; t += Time.deltaTime)
        {
            tile.pieceObj.transform.position = Vector2.Lerp(tile.pieceObj.transform.position, tile.transform.position, t/time);
            yield return null;
        }
    }

    // Busca por novos matches feitos após preenchimento do grid
    private void SearchForNewMatches()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                var matchTiles = Tiles[x,y].GetMatchedTiles();
                if(matchTiles.Count >= 3)
                {
                    Match();
                    return;
                }
            }
        }
        
        canPlay = true;
    }

    public void ResetGame()
    {
        //Reposiciona todas as peças
        foreach (var tile in Tiles)
        {
            tile.pieceObj.transform.localScale = new Vector3(0.8f,0.8f,0.8f);
            tile.pieceObj.gameObject.transform.position = tile.gameObject.transform.position;
            tile.pieceObj.gameObject.SetActive(true);
        }
        
        resetSound.Play(); //executa som de reset

        //Reinicia o jogo
        Score = 0;
        scoreText.text = Score.ToString();
        InitGame();
    }
}