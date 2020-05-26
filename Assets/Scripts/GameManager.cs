using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    //list of all cards for any deck
    public List<CardScript> allCards;

    //turn swapper bool, keeps track of the current player
    private bool isLeftPlayerTurn = true;

    //local copy of number of AI players
    private int numAIs;

    //board state value for all cards
    public Board boardState;

    //Both player values
    public Player player1;
    public Player player2;

    //canvas for end game
    [SerializeField]
    Canvas endGameHUD;

    //text for end-game canvas
    [SerializeField]
    Text endGameText;
    


    // Start is called before the first frame update
    void Start()
    {
        //initialize board state
        boardState = Board.Instance;

        //get ai players, default to 0
        numAIs = GameSettings.Instance ? GameSettings.Instance.AICount : 0;

        //set player bool identities
        player1.isPlayer = true;
        player2.isPlayer = false;

        //if AI is 1, set player2 to ai, and if AI is 2, set both, else do nothing
        switch (numAIs)
        {
            case 1:
                player2.SetAI();
                break;
            case 2:
                player2.SetAI();
                player1.SetAI();
                break;
            default: break;
        }

        //divvy out decks, they will be copies of each other
        DistributeDecks();

        //notify p1 of turn
        Invoke("EnableNextPlayer", .1f);
    }

    /// <summary>
    /// Enables the next player to move in order
    /// </summary>
    private void EnableNextPlayer()
    {
        if (isLeftPlayerTurn) player1.NotifyIsTurn();
        else player2.NotifyIsTurn();

        isLeftPlayerTurn = !isLeftPlayerTurn;
    }

    /// <summary>
    /// Notifies that the player has played their card
    /// </summary>
    /// <param name="played">The card played</param>
    /// <param name="row">The row the card was played at</param>
    /// <param name="column">The column the card was played at</param>
    /// <param name="pushDir">The direction we are pushing, if applicable</param>
    public void NotifyOfTurn(CardScript played, int row, int column, Directions pushDir)
    {
        /*
         * (1) Validate play
         * (2) Place the card
         * (3) Check if game is over
         *  ┣ (3.1) If so, end the game
         *  ┗ (3.2) Else swap player control
         */

        if(boardState.IsValidMove(played, row, column, pushDir)){
            boardState.TakeMove(played, row, column, pushDir);

            Vector2Int scores = boardState.GetPlayerScores();

            //update score gems
            int times;

            //update scores
            UpdatePlayerScore(player1, scores.x);
            UpdatePlayerScore(player2, scores.y);

            //if over, end the game
            if (boardState.IsGameOver())
            {
                endGameHUD.gameObject.SetActive(true);

                //get winner, the player with more gems, else a draw
                int winNum = player1.gemCounter < player2.gemCounter ? 2 : player2.gemCounter < player1.gemCounter ? 1 : 0;
                string endText;

                if(winNum == 0)
                {
                    endText = "The round has ended in a draw...";
                }
                else
                {
                    endText = string.Format("Player {0} has won the game!", winNum);
                }

                endGameText.text = endText;
            }
            else //else swap turns
            {
                Invoke("EnableNextPlayer", .1f);
            }
        }
    }

    /// <summary>
    /// Updates the given player's gem counter score
    /// </summary>
    /// <param name="p">The player to update</param>
    /// <param name="score">The player's new gem score</param>
    private void UpdatePlayerScore(Player p, int score)
    {
        int times;

        if (score > p.gemCounter)
        {
            times = score - p.gemCounter;
            for (int i = 0; i < times; i++)
            {
                p.GiveGem();
            }
        }
        else
        {
            times = p.gemCounter - score;
            for (int i = 0; i < times; i++)
            {
                p.TakeGem();
            }
        }
    }

    /// <summary>
    /// Generates decks of size 16 and distributes them to the players
    /// </summary>
    void DistributeDecks()
    {
        List<CardScript> deck1 = new List<CardScript>();
        List<CardScript> deck2 = new List<CardScript>();

        for (int index = 0; index < 16; index++)
        {
            //give player1 card
            CardScript card = Instantiate(allCards[index], new Vector2(0, 0), Quaternion.identity);
            card.gameObject.SetActive(false);
            card.isPlayerCard = true;
            card.mustBeHidden = true;
            deck1.Add(card);
            //give player 2 card
            card = Instantiate(allCards[index], new Vector2(0, 0), Quaternion.identity);
            card.gameObject.SetActive(false);
            card.isPlayerCard = false;
            card.mustBeHidden = true;
            deck2.Add(card);
        }

        //shuffle
        Shuffle(deck1);
        Shuffle(deck2);

        //distribute
        player1.GiveDeck(deck1);
        player2.GiveDeck(deck2);
    }

    /// <summary>
    /// Shuffles the given list of cards in place by Fisher-Yates
    /// </summary>
    /// <param name="cards">Cards to shuffle</param>
    private void Shuffle(List<CardScript> cards)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            CardScript temp = cards[i];
            int randomIndex = Random.Range(0, cards.Count);
            cards[i] = cards[randomIndex];
            cards[randomIndex] = temp;
        }
    }

    /// <summary>
    /// Returns the other player's hand
    /// </summary>
    /// <param name="isPlayer1">Are you the left player?</param>
    /// <returns>Player 2's hand if player 1, else player 1's hand</returns>
    public CardScript[] GetOtherPlayerHand(bool isPlayer1)
    {
        if (isPlayer1) return player2.hand;
        else return player1.hand;
    }

    /// <summary>
    /// Returns the other player's deck
    /// </summary>
    /// <param name="isPlayer1">Are you the left player?</param>
    /// <returns>Player 2's deck if player 1, else player 1's deck</returns>
    public List<CardScript> GetOtherPlayerDeck(bool isPlayer1)
    {
        if (isPlayer1) return player2.deck;
        else return player1.deck;
    }

    public static bool AreOpposingDirections(Directions first, Directions second)
    {
        return (first == Directions.UP && second == Directions.DOWN) ||
                (first == Directions.RIGHT && second == Directions.LEFT) ||
                (first == Directions.DOWN && second == Directions.UP) ||
                (first == Directions.LEFT && second == Directions.RIGHT);
    }

    public static Directions GetOpposingDirections(Directions dir)
    {
        switch (dir)
        {
            case Directions.UP: return Directions.DOWN;
            case Directions.LEFT: return Directions.RIGHT;
            case Directions.DOWN: return Directions.UP;
            case Directions.RIGHT: return Directions.LEFT;
            default: return Directions.NOPUSH;
        }
    }
}

public enum Directions { UP, DOWN, LEFT, RIGHT, NOPUSH }

public enum NodeType { MAXNODE, MINNODE, CHANCEMAX, CHANCEMIN }