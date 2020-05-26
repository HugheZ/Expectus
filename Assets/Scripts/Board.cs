using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    [SerializeField]
    GameObject gem; //gem object

    Grid board;

    private static Board _instance;

    public static Board Instance { get { return _instance; } }


    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            //don't destroy, allows multiple boards but they will not be retrievable
            //Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    //board state value for all cards
    public CardScript[][] boardState;

    //list of gem placements
    public static List<Vector3Int> gemPlaces;

    //gem instances
    public static List<GameObject> gemInsts;

    // Start is called before the first frame update
    void Start()
    {
        Init();
    }

    /// <summary>
    /// Initializes the game board
    /// </summary>
    private void Init()
    {
        //Only need grid if not real grid
        board = GetComponent<Grid>();
        gemInsts = new List<GameObject>();

        boardState = new CardScript[5][];
        for (int i = 0; i < 5; i++)
        {
            boardState[i] = new CardScript[5];
            for (int j = 0; j < 5; j++) boardState[i][j] = null;
        }

        gemPlaces = new List<Vector3Int>();

        //set gem places
        for (int i = 0; i < 3; i++)
        {
            Vector3Int gemSpot = Vector3Int.zero;
            do
            {
                gemSpot.x = Random.Range(1, 4);
                gemSpot.y = Random.Range(1, 4);
            } while (gemPlaces.Contains(gemSpot));
            gemPlaces.Add(gemSpot);

            //Instantiate gems if not copy
            GameObject g = Instantiate(gem, board.CellToWorld(Board.IndexToRenderPoint(gemPlaces[i])), Quaternion.identity);
            gemInsts.Add(g);
        }
        string ret = "[" + gemPlaces[0] + ", " + gemPlaces[1] + ", " + gemPlaces[2] + "]";
        print(ret);
    }

    /// <summary>
    /// Checks if the move was valid
    /// </summary>
    /// <param name="row">The row index</param>
    /// <param name="column">The column index</param>
    /// <param name="dir">The direction to push, if valid</param>
    /// <returns>True if the move is valid, false otherwise</returns>
    public bool IsValidMove(CardScript card, int row, int column, Directions dir, CardScript[][] b = null)
    {
        bool copy = b != null;
        if (b == null) b = Board.Instance.boardState;

        //first, return false if this card can't push that direction
        if ((dir == Directions.DOWN && !card.canPushDown) || (dir == Directions.LEFT && !card.canPushLeft) ||
            (dir == Directions.RIGHT && !card.canPushRight) || (dir == Directions.UP && !card.canPushUp)) return false;

        //local val for if a gem is here (NEED TO SWAP COLUMN/ROW SINCE COLUMN IS X AND ROW IS Y)
        bool gemHere = false;
        foreach (Vector3 gem in gemPlaces)
        {
            if (gem.x == column && gem.y == row) gemHere = true;
        }

        //return false on graveyards
        if (row <= 0 || column <= 0 || row >= b.Length-1 || column >= b[0].Length - 1)
            return false;

        //return false if the space is taken or a gem is on that space and not pushing
        if ((b[row][column] != null || gemHere) && dir == Directions.NOPUSH)
            return false;

        //if the space is empty, return true
        if (b[row][column] == null && !gemHere && dir == Directions.NOPUSH)
            return true;

        //if a gem is here but no card to push, return false
        if (b[row][column] == null && gemHere)
            return false;

        //finally, check to see if we can push all cards in the given chain can be pushed
        if (CardCast(row, column, dir, copy, false, b)) return true;
        
        //card cast failed, return false
        return false;
    }

    /// <summary>
    /// Takes the given move
    /// </summary>
    /// <param name="toPlay">The card to play</param>
    /// <param name="row">The row index</param>
    /// <param name="column">The column index</param>
    /// <param name="dir">The direction to push, if valid</param>
    public void TakeMove(CardScript toPlay, int row, int column, Directions dir, CardScript[][] b = null)
    {
        bool isCopy = true;
        if (b == null)
        {
            b = Board.Instance.boardState;
            isCopy = false;
        }

        //check validity
        if(IsValidMove(toPlay, row, column, dir, b))
        {
            //Card cast to make space (will do nothing if we are placing in empty space)
            CardCast(row, column, dir, isCopy, true, b);

            //place this card here
            b[row][column] = toPlay;

            //update the card's render position if it's teh real board
            if(!isCopy) toPlay.transform.position = board.CellToWorld(Board.IndexToRenderPoint(new Vector3Int(column, row, 0)));
        }
    }

    /// <summary>
    /// Deep copies this board state
    /// </summary>
    /// <returns>A copy of the board state</returns>
    public CardScript[][] CopyState(CardScript[][] b = null)
    {
        if (b == null) b = Board.Instance.boardState;

        CardScript[][] bc = new CardScript[5][];

        for (int i = 0; i < b.Length; i++)
        {
            bc[i] = new CardScript[5];
            for (int j = 0; j < b[0].Length; j++)
            {
                bc[i][j] = b[i][j];
            }
        }

        //for (int i = 0; i < 3; i++) gemInsts[i].transform.position = board.CellToWorld(Board.IndexToRenderPoint(gemPlaces[i]));
        return bc;
    }

    public int IsNextToGem(int rowNum, int colNum)
    {
        int nextTo = 0;
        for(int i = 0; i < gemPlaces.Count; i++)
        {
            int x = gemPlaces[i].x;
            int y = gemPlaces[i].y;
            if ((rowNum == x - 1 && colNum == y) || (rowNum == x + 1 && colNum == y) ||
                (rowNum == x && colNum == y + 1) || (rowNum == x && colNum == y - 1))
                nextTo = i;
        }
        return nextTo;
    }

    /// <summary>
    /// Gets the card that owns the specific gem at index row / column
    /// To get who owns this gem, use card.isPlayerCard (true = p1, false = p2/AI)
    /// </summary>
    /// <param name="row">The 0-based row index</param>
    /// <param name="column">The 0-based column index</param>
    /// <returns>A card if there is a gem taken by a card, null if there is no gem here or no claim to this gem</returns>
    public CardScript CardOnGem(int row, int column, CardScript[][] b = null)
    {
        if (b == null) b = Board.Instance.boardState;

        Vector3Int found = gemPlaces.Find((Vector3Int ind) => { return ind.x == column && ind.y == row; });

        //return null if no gem here
        if (found == null) return null;

        //have a gem, find the card
        CardScript foundCard = b[found.y][found.x];

        //return the found card, else return null
        if (foundCard) return foundCard;

        return null;
    }

    /// <summary>
    /// Iterates through the board state and determines if a play is legal for pushing.
    /// The <paramref name="takeAction"/> parameter will actually take the push action and permute teh board state if set to true
    /// </summary>
    /// <param name="row">The row to place the pushing card</param>
    /// <param name="column">The column to place the pushing card</param>
    /// <param name="dir">The direction to push</param>
    /// <param name="takeAction">Whether to permute all cards in the boardState based on this change</param>
    /// <returns>True on legal action/success, false on illegal/failure</returns>
    public bool CardCast(int row, int column, Directions dir, bool isCopy, bool takeAction = false, CardScript[][] b = null)
    {
        if (b == null)
        {
            b = Board.Instance.boardState;
        }

        int localRow = row;
        int localCol = column;

        try
        {
            //loop while we have a pushable card
            while (b[localRow][localCol] != null)
            {
                //check to see if we can push this card, return false if we can't
                if (!b[localRow][localCol].CanPushThisWay(dir)) return false;
                
                //we can, so increment indexes by direction
                switch (dir)
                {
                    case Directions.UP: localRow--; break;
                    case Directions.DOWN: localRow++; break;
                    case Directions.LEFT: localCol--; break;
                    case Directions.RIGHT: localCol++; break;
                    default: break;
                }
            }

            //we got to the end of the chain without hitting a wall and without getting stopped
            //if we just wanted to check, return, else permute cards
            if (!takeAction) return true;
            else
            {
                //loop while we have a pushable card
                while (localRow != row || localCol != column)
                {
                    int preReturnRow = localRow;
                    int preReturnCol = localCol;

                    // decrement indexes by direction
                    switch (dir)
                    {
                        case Directions.UP: localRow++; break;
                        case Directions.DOWN: localRow--; break;
                        case Directions.LEFT: localCol++; break;
                        case Directions.RIGHT: localCol--; break;
                        default: break;
                    }

                    //update board state
                    CardScript moved = b[localRow][localCol];
                    b[localRow][localCol] = null;
                    b[preReturnRow][preReturnCol] = moved;

                    //update card position
                    if (!isCopy) moved.transform.position = board.CellToWorld(Board.IndexToRenderPoint(new Vector3Int(preReturnCol, preReturnRow, 0)));
                }
            }
        }catch(System.IndexOutOfRangeException e) //we caught the OOB exception, so the board is stopping the push
        {
            return false;
        }

        //we got here, so mutation was successful
        return true;
    }

    /// <summary>
    /// Calculates the score for each player in the game board
    /// </summary>
    /// <returns>A Vector2Int with x player 1 (Human, blue) score and y player 2 (AI, red) score</returns>
    public Vector2Int GetPlayerScores(CardScript[][] b = null)
    {
        if (b == null) b = Board.Instance.boardState;

        Vector2Int scores = Vector2Int.zero;

        foreach(Vector3Int gemPos in gemPlaces)
        {
            CardScript place = b[gemPos.y][gemPos.x];
            if (place && place.isPlayerCard) scores.x++;
            else if (place) scores.y++;
        }

        return scores;
    }

    /// <summary>
    /// Determines if the game is over by board fill
    /// </summary>
    /// <returns>True if the game is over, false otherwise</returns>
    public bool IsGameOver(CardScript[][] b = null)
    {
        if (b == null) b = Board.Instance.boardState;

        for (int i = 1; i < 4; i++)
        {
            for(int j = 1; j < 4; j++)
            {
                //if we have an empty space (no card and no gem) return not over
                if (b[i][j] == null && !gemPlaces.Contains(new Vector3Int(j,i,0)))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Converts the given 0-indexed index to a point to render for the board
    /// </summary>
    /// <param name="index">0-indexed index</param>
    /// <returns>Render point index in world space</returns>
    public static Vector3Int IndexToRenderPoint(Vector3Int index)
    {
        return new Vector3Int(index.x*2 - 4, -index.y*2 + 4, 0);
    }
}
