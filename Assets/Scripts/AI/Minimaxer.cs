using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Class to implement ExpectiMiniMax on the game Joustus
/// 
/// Has visible access to:
/// - current board state
/// - current hand
/// - opposing hand via GameManager.Instance.GetOtherPlayerHand()
/// - current deck (to simulate knowing what's in your deck before the game starts, but does not use index values to gain upper hand)
/// 
/// Does not have access to:
/// - order of deck
/// - opposing deck (since this would increase memory beyond bounds, we would not be able to permute it, and a normal game would not have a player know their opponent's deck)
/// 
/// </summary>
public class Minimaxer
{
    private int numGemsOwned;
    private int numGemsOppOwns;
    private GameManager gm;
    bool isPlayer1; //whether player 1 or two

    /////////////////////////////////
    /// <summary>
    /// Constructors for minimax
    /// </summary>
    public Minimaxer()
    {
        numGemsOwned = 0;
        numGemsOppOwns = 0;
        gm = GameManager.Instance;
        isPlayer1 = false;
    }
    public Minimaxer(int val1, int val2)
    {
        numGemsOwned = val1;
        numGemsOppOwns = val2;
        gm = GameManager.Instance;
        isPlayer1 = false;
    }
    public Minimaxer(bool isPlayer1)
    {
        numGemsOwned = 0;
        numGemsOppOwns = 0;
        gm = GameManager.Instance;
        this.isPlayer1 = isPlayer1;
    }
    /////////////////////////////////

    
    /// <summary>
    /// Updates the AI of gems for both sides
    /// </summary>
    /// <param name="b">The board state</param>
    /// <returns></returns>
    public int[] UpdateGems(CardScript[][] b)
    {
        int[] score = { 0, 0 };
        for (int i = 1; i < 4; i++)
        {
            for (int j = 1; j < 4; j++)
            {
                if (Board.Instance.CardOnGem(i, j, b))
                {
                    CardScript gemCard = Board.Instance.CardOnGem(i, j, b);
                    if (gemCard.isPlayerCard)
                        score[0]++;
                    else
                        score[1]++;
                }
            }
        }
        return score;
    }


    /// <summary>
    /// Launch method for minimax, return a response to fill the board if ending the game now is favorable to avoid extra searching
    /// </summary>
    /// <param name="deck">Our deck</param>
    /// <param name="myHand">Our hand</param>
    /// <returns>The next best move as given by ExpectiMiniMax</returns>
    public Response ConsiderTurn(List<CardScript> deck, CardScript[] myHand)
    {
        Response turn = new Response();
        int emptySpaces = 0;
        int emptyRow = 0;
        int emptyCol = 0;
        for (int i = 1; i < 4; i++)
            for (int j = 1; j < 4; j++)
                if (Board.Instance.boardState[i][j] == null)
                {
                    emptySpaces++;
                    emptyRow = i;
                    emptyCol = j;
                }
        if (emptySpaces == 1 && numGemsOwned > numGemsOppOwns)
        {
            turn.cardIndex = 0;
            turn.colNum = emptyCol;
            turn.rowNum = emptyRow;
            turn.dir = Directions.NOPUSH;
        }
        else
        {
            //try to run minimax with game state (our hands, board) and start with max node
            turn = TryMinimax(myHand, gm.GetOtherPlayerHand(isPlayer1), NodeType.MAXNODE, 5, 0);
        }
        return turn;
    }

    /// <summary>
    /// Main minimax algorithm, flips back and forth between MAX and MIN nodes
    /// </summary>
    /// <param name="hand">This iteration's hand, makes local copy</param>
    /// <param name="nodetype">MAX/MIN</param>
    /// <param name="depth">How deep we are</param>
    /// <param name="h">Heuristic depth, at which point we cut off search</param>
    /// <param name="α">ALPHA value</param>
    /// <param name="β">BETA value</param>
    /// <param name="board">Our current state, makes copy to this</param>
    /// <returns></returns>
    private Response TryMinimax(CardScript[] hand, CardScript[] theirHand, NodeType nodetype, int depth, int h, float α = float.MinValue, float β = float.MaxValue, CardScript[][] board = null)
    {
        //make local copies
        if (board == null)
        {
            board = Board.Instance.CopyState();
        }

        Response bestMove = new Response();
        int iHave = numGemsOwned;
        int theyHave = numGemsOppOwns;

        //set utility to be max float/min float depending on whether we are looking for min or max respectively
        bestMove.utility = (nodetype == NodeType.MINNODE || nodetype == NodeType.CHANCEMIN) ? float.MaxValue : float.MinValue;

        //if this node is terminal or depth is deep enough, return response
        if(IsTerminal(board) || depth == 0)
        {
            bestMove.cardIndex = -1;
            bestMove.colNum = -1;
            bestMove.rowNum = -1;
            bestMove.utility = Utility(board);

            return bestMove;
        }
        //if we reach the bottom of the tree, calculate the score of the node
        //if (depth == h)
        //{
        //    Vector2Int score = Board.Instance.GetPlayerScores(boardLocal);
        //    iHave = score.y;
        //    theyHave = score.x;
        //    int scoreDiff = iHave - theyHave;
        //    if (maxScore > scoreDiff)
        //        bestMove = move;
        //}
        //else
        //{
        //if deterministic max, find min with chance draw
        if (nodetype == NodeType.MAXNODE)
        {
            //get all possible moves
            List<Response> possibleMoves = FindAllPossibleMove(hand, board);

            //for each possible moves, do it
            foreach (Response move in possibleMoves)
            {
                //make the move
                CardScript[][] boardLocal = Board.Instance.CopyState(board);
                Board.Instance.TakeMove(hand[move.cardIndex], move.rowNum, move.colNum, move.dir, boardLocal);
                //permute new hand
                CardScript[] handLocal = (CardScript[])hand.Clone();
                handLocal[move.cardIndex] = null;

                //go a level deeper with min node
                Response nextBestNode;
                if(depth == 5)
                    nextBestNode = TryMinimax(handLocal, theirHand, NodeType.MINNODE, depth - 1, h, α, β, boardLocal);
                else
                    nextBestNode = TryMinimax(handLocal, theirHand, NodeType.CHANCEMIN, depth - 1, h, α, β, boardLocal);

                //check to see if that choice is higher than our utility
                if (nextBestNode.utility > bestMove.utility)
                {
                    bestMove.dir = move.dir;
                    bestMove.utility = nextBestNode.utility;
                    bestMove.cardIndex = move.cardIndex;
                    bestMove.colNum = move.colNum;
                    bestMove.rowNum = move.rowNum;
                }

                //set pruning value (MAX(nextBestNode.utility, α))
                α = Mathf.Max(nextBestNode.utility, α);
                //break if α >= β
                if (α >= β)
                {
                    break;
                }
            }

        } //if deterministic min, find max with chance draw
        if (nodetype == NodeType.MINNODE)
        {
            //get all possible moves from opponent
            List<Response> possibleMoves = FindAllPossibleMove(theirHand, board);

            //for each possible moves from opponent, do it
            foreach (Response move in possibleMoves)
            {
                //make the move
                CardScript[][] boardLocal = Board.Instance.CopyState(board);
                Board.Instance.TakeMove(theirHand[move.cardIndex], move.rowNum, move.colNum, move.dir, boardLocal);
                //permute new hand
                CardScript[] handLocal = (CardScript[])theirHand.Clone();
                handLocal[move.cardIndex] = null;

                Response nextBestNode = TryMinimax(hand, handLocal, NodeType.CHANCEMAX, depth - 1, h, α, β, boardLocal);
                //check to see if that choice is lower than our utility
                if (nextBestNode.utility < bestMove.utility)
                {
                    bestMove.dir = move.dir;
                    bestMove.utility = nextBestNode.utility;
                    bestMove.cardIndex = move.cardIndex;
                    bestMove.colNum = move.colNum;
                    bestMove.rowNum = move.rowNum;
                }

                //set pruning value (MIN(nextBestNode.utility, β))
                β = Mathf.Min(nextBestNode.utility, β);
                //break if α >= β
                if (α >= β)
                {
                    break;
                }
            }

        } //if chancemin node, weight all deterministic maxes with probability, then run the max choice
        if (nodetype == NodeType.CHANCEMAX)
        {
            //permute new hand
            CardScript[] handLocal = (CardScript[])hand.Clone();
            int replaceIndex = Array.FindIndex(handLocal, elem => elem == null);
            float utility = 0;
            //for each probable pull from MY deck, get its result
            foreach (CardScript possible in GameManager.Instance.GetOtherPlayerDeck(!isPlayer1)) {
                //replace the card
                handLocal[replaceIndex] = possible;

                //calculate best
                Response nextBestNode = TryMinimax(handLocal, theirHand, NodeType.MAXNODE, depth - 1, h, α, β, board);
                //offset utility with probability
                utility += nextBestNode.utility * Probability(GameManager.Instance.GetOtherPlayerDeck(!isPlayer1), handLocal[replaceIndex]);

                //set pruning value (MAX(utility, α))
                α = Mathf.Max(utility, α);
                //break if α >= β
                if (α >= β)
                {
                    break;
                }
            }
            //set utility of best move
            bestMove.utility = utility;

        } //if chancemax node, weight all deterministic mins with probability, then run the min choice
        if (nodetype == NodeType.CHANCEMIN)
        {
            //permute new hand
            CardScript[] handLocal = (CardScript[])theirHand.Clone();
            int replaceIndex = Array.FindIndex(handLocal, elem => elem == null);
            //Debug.Log("replaceIndex: " + replaceIndex);
            float utility = 0;
            //for each probable pull from THEIR deck, get its result
            foreach (CardScript possible in GameManager.Instance.GetOtherPlayerDeck(isPlayer1))
            {
                //replace the card
                handLocal[replaceIndex] = possible;

                //calculate best
                Response nextBestNode = TryMinimax(hand, handLocal, NodeType.MINNODE, depth - 1, h, α, β, board);
                //offset utility with probability
                utility += nextBestNode.utility * Probability(GameManager.Instance.GetOtherPlayerDeck(isPlayer1), handLocal[replaceIndex]);
                
                //set pruning value (MIN(utility, β))
                β = Mathf.Min(utility, β);
                //break if α >= β
                if (α >= β)
                {
                    break;
                }
            }
            //set utility of best move
            bestMove.utility = utility;
        }
        

        return bestMove;
    }

    /// <summary>
    /// Generates a list of all possible responses given our hand and board state
    /// </summary>
    /// <param name="hand">Our complete hand of cards</param>
    /// <param name="board">The current board staet</param>
    /// <returns> List(Response) of all possible moves</returns>
    private List<Response> FindAllPossibleMove(CardScript[] hand, CardScript[][] board)
    {
        List<Response> reses = new List<Response>();
        for (int c = 0; c < hand.Length; c++)
        {
            //look at all possible down pushes
            for (int i = 1; i < 4; i++)
            {
                for (int j = 1; j < 4; j++)
                {
                    if (Board.Instance.IsValidMove(hand[c], i, j, Directions.NOPUSH, board))
                    {
                        Response res = new Response();
                        res.cardIndex = c;
                        res.rowNum = i;
                        res.colNum = j;
                        res.dir = Directions.NOPUSH;
                        reses.Add(res);
                    }
                    else
                    {
                        //if i can push it down
                        if (hand[c].canPushDown && Board.Instance.IsValidMove(hand[c], i, j, Directions.DOWN, board))
                        {
                            //if this improves the score, set this to be my returned move
                            Response res = new Response();
                            res.cardIndex = c;
                            res.rowNum = i;
                            res.colNum = j;
                            res.dir = Directions.DOWN;
                            reses.Add(res);
                        }
                        //if i can push it down
                        if (hand[c].canPushLeft && Board.Instance.IsValidMove(hand[c], i, j, Directions.LEFT, board))
                        {
                            //if this improves the score, set this to be my returned move
                            Response res = new Response();
                            res.cardIndex = c;
                            res.rowNum = i;
                            res.colNum = j;
                            res.dir = Directions.LEFT;
                            reses.Add(res);
                        }
                        //if i can push it down
                        if (hand[c].canPushRight && Board.Instance.IsValidMove(hand[c], i, j, Directions.RIGHT, board))
                        {
                            //if this improves the score, set this to be my returned move
                            Response res = new Response();
                            res.cardIndex = c;
                            res.rowNum = i;
                            res.colNum = j;
                            res.dir = Directions.RIGHT;
                            reses.Add(res);
                        }
                        //if i can push it down
                        if (hand[c].canPushUp && Board.Instance.IsValidMove(hand[c], i, j, Directions.UP, board))
                        {
                            //if this improves the score, set this to be my returned move
                            Response res = new Response();
                            res.cardIndex = c;
                            res.rowNum = i;
                            res.colNum = j;
                            res.dir = Directions.UP;
                            reses.Add(res);
                        }
                    } //end of else

                } //end of j for
            } //end of i for

        } //end of hand for (c)
            
        return reses;
    }

    /// <summary>
    /// Calculates the given utility of the board from the instance.
    /// Since each AI seeks to maximize its score, the score is
    /// MyScore - TheirScore.
    /// Player1: scores.x - scores.y
    /// Player2: scores.y - scores.x
    /// </summary>
    /// <param name="board"></param>
    /// <returns>An int utility [-3,3]</returns>
    int Utility(CardScript[][] board)
    {
        if (!Board.Instance) return 0;

        Vector2Int scores = Board.Instance.GetPlayerScores(board);

        if (isPlayer1) return scores.x - scores.y;
        else return scores.y - scores.x;
    }

    /// <summary>
    /// Gets the probability of drawing this card.
    /// NOTE: even though we look directly at the deck, we don't know what card we will draw next.
    ///         This is because we have a pre-made deck and know which cards we have already played but allows us to not have a played list.
    /// </summary>
    /// <param name="deck">The deck to draw from</param>
    /// <param name="drawn">The card we are measuring</param>
    /// <returns>float probability of this drawn card</returns>
    float Probability(List<CardScript> deck, CardScript drawn)
    {
        return deck.Count(elem => elem == drawn) / (float)deck.Count();
    }

    /// <summary>
    /// Determines if the board state is terminal
    /// </summary>
    /// <param name="board">The board state for this iteration</param>
    /// <returns>True if this is an end game state</returns>
    bool IsTerminal(CardScript[][] board)
    {
        return Board.Instance.IsGameOver(board);
    }

}

/// <summary>
/// Response datatype for holding move information between ExpectiMiniMax search nodes
/// </summary>
public class Response
{
    public int cardIndex = -1;
    public int rowNum = 0;
    public int colNum = 0;
    public Directions dir = Directions.NOPUSH;
    public float utility = 0;
}


//Dictionary<CardScript, float> probs = new Dictionary<CardScript, float>();
//var group = deck.GroupBy(i => i);
//        foreach(var g in group)
//        {
//            probs.Add(g.Key, g.Count() / (float) deck.Count());
//        }
//        return probs;