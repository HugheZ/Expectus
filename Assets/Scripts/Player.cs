using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading;

public class Player : MonoBehaviour
{
    public static float PUSHOFFSET = .25f; //offset to render pushing cards

    private bool isAI = false;

    public bool isPlayer; //is this player 1?

    //The player's list of cards in their deck
    public List<CardScript> deck;

    //The player's list of visible cards in their hand
    public CardScript[] hand;

    //The player's locations for cards
    public List<Transform> handLocations; //EXPECTS SIZE 3

    private Minimaxer aiComponent;

    //number of gems this player has
    public int gemCounter;

    [SerializeField]
    List<SpriteRenderer> gemSpots; //gem game objects for counter, expects 3

    [SerializeField]
    Sprite gemNotMine; //sprite for not having gem
    [SerializeField]
    Sprite gemMine; //sprite for having gem

    [SerializeField]
    Text deckText; //text for cards left in the deck

    ///////////////HUMAN CONTROL FIELDS//////////////////

    bool isTurn = false; //is it this player's turn
    bool isValid = false; //is the current place valid
    bool isChoosingFromHand = true; //if the player is currently choosing a card from their hand
    bool isPlacingCard = false; //if the player is placing their chosen card
    int chosenIndex = -1; //the index of the chosen card from their hand
    int placedRow = -1; //the row the chosen card will be placed to
    int placedColumn = -1; //the column the chosen card will be placed to
    Directions pushDir = Directions.NOPUSH; //how we will push

    [SerializeField]
    SpriteRenderer choiceOutline; //outline for choice
    [SerializeField]
    Color validColorTint; //color to tint if valid
    [SerializeField]
    Color invalidColorTint; //color to tint if invalid

    /////////////////////////////////////////////////////

    ///////////////AI NOTIFICATION FIELDS////////////////
    public Wheel AILoadingWheel;
    Thread _calcThread;
    Response calcedMove = null;
    /////////////////////////////////////////////////////

    // Start is called before the first frame update
    void Start()
    {
        //hand = new CardScript[3];
    }

    // Update is called once per frame
    void Update()
    {
        //if is turn, allow player to do things
        if (isTurn && !isAI)
        {
            //if we are choosing from the hand, allow for up and down movement within hand size
            if (isChoosingFromHand)
            {
                //allow up if not at first card
                if((Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) && chosenIndex > 0)
                {
                    chosenIndex--;
                    choiceOutline.transform.position = hand[chosenIndex].transform.position;
                }
                else if ((Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) && chosenIndex < 2)
                {
                    chosenIndex++;
                    choiceOutline.transform.position = hand[chosenIndex].transform.position;
                }
                else if(Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
                {
                    //selected a card, move to board
                    //parent choice otuline
                    choiceOutline.transform.SetParent(hand[chosenIndex].transform);
                    choiceOutline.transform.localPosition = new Vector3(0, 0, 0);
                    //swap controller values
                    isChoosingFromHand = false;
                    isPlacingCard = true;
                    placedRow = 1;
                    placedColumn = 2;
                    hand[chosenIndex].GetComponent<SpriteRenderer>().sortingOrder = 3;
                    //move card
                    MoveCard(Directions.NOPUSH, Directions.DOWN);
                }
            }
            else if (isPlacingCard) //now the card is on the board
            {
                if(Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Backspace))
                {
                    //back was hit, return to place
                    choiceOutline.transform.SetParent(null);
                    hand[chosenIndex].transform.position = handLocations[chosenIndex].position;
                    choiceOutline.transform.position = hand[chosenIndex].transform.position;
                    choiceOutline.color = validColorTint;
                    hand[chosenIndex].GetComponent<SpriteRenderer>().sortingOrder = 2;

                    //swap controller values
                    isChoosingFromHand = true;
                    isPlacingCard = false;
                }// else we get inputs for moving cards around
                else if(Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) //LEFT
                {
                    MoveCard(pushDir, Directions.LEFT);
                }
                else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) //DOWN
                {
                    MoveCard(pushDir, Directions.DOWN);
                }
                else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) //RIGHT
                {
                    MoveCard(pushDir, Directions.RIGHT);
                }
                else if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) //UP
                {
                    MoveCard(pushDir, Directions.UP);

                }// else we check for selecting this place
                else if ((Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)) && isValid)
                {
                    hand[chosenIndex].GetComponent<SpriteRenderer>().sortingOrder = 2;
                    choiceOutline.gameObject.SetActive(false);
                    SubmitTurn(chosenIndex, placedRow, placedColumn, pushDir);
                }
            }
        }

        //AI response
        //if(isAI && calcedMove != null)
        //{
        //    Response local = calcedMove;
        //    calcedMove = null;
        //    _calcThread.Join();
        //    AILoadingWheel.gameObject.SetActive(false);
        //    SubmitTurn(local.cardIndex, local.rowNum, local.colNum, local.dir);
        //}
    }

    /// <summary>
    /// Sets this player to be an AI
    /// </summary>
    public void SetAI()
    {
        isAI = true;
        aiComponent = new Minimaxer(isPlayer);
    }


    /// <summary>
    /// Notifies this player that it is its turn
    /// </summary>
    public void NotifyIsTurn()
    {
        if (isAI) TakeAITurn();
        else TakeHumanTurn();
    }

    /// <summary>
    /// Gives this player a deck of cards
    /// </summary>
    /// <param name="deck">The deck assigned by the manager</param>
    public void GiveDeck(List<CardScript> deck)
    {
        //assign local deck copy
        this.deck = deck;
        
        //retrieve top 3 cards and give to hand
        for(int i = 0; i < 3; i++)
        {
            DrawCard(i);
        }
    }

    /// <summary>
    /// Draws a single card from the deck and adds it to the hand
    /// Does nothing if already 3 cards in hand
    /// </summary>
    /// <param name="indx">The index to draw to</param>
    private void DrawCard(int indx)
    {
        if(indx < 3)
        {
            hand[indx] = deck[0];
            //unhide the card
            hand[indx].mustBeHidden = false;
            hand[indx].gameObject.SetActive(true);

            deck.RemoveAt(0);

            //set the deck text
            deckText.text = string.Format("{0} / 16", deck.Count);

            //place the card
            hand[indx].transform.position = handLocations[indx].position;
        }
    }

    /// <summary>
    /// Moves the card by the given direction
    /// Updates card state such that it is in the center if no card exists, else around that card
    /// </summary>
    /// <param name="currentPush">Our current push state around the given card, NOPUSH if empty space</param>
    /// <param name="move">The direction we are moving, DOWN for first spawn</param>
    void MoveCard(Directions currentPush, Directions move)
    {
        int newRow = placedRow;
        int newCol = placedColumn;

        //see if we need to wobble around the card
        if(Board.Instance.boardState[placedRow][placedColumn] != null)
        {
            //if we are going around the card, swap the direction and return
            if(!GameManager.AreOpposingDirections(currentPush, move))
            {
                //set push direction and rerender
                pushDir = GameManager.GetOpposingDirections(move);

                //set is valid
                isValid = (Board.Instance.IsValidMove(hand[chosenIndex], placedRow, placedColumn, pushDir));

                RenderCard();

                //we are good, so return
                return;
            }

            //else we are moving away, allow rest of logic to continue
        }

        //change next indices depending on movement direction
        switch (move)
        {
            case Directions.UP: newRow--; break;
            case Directions.DOWN: newRow++; break;
            case Directions.LEFT: newCol--; break;
            case Directions.RIGHT: newCol++; break;
        }

        //if either index is less than 1 or greater than 3, break since in graveyard or beyond
        if (newCol > 3 || newCol < 1 || newRow > 3 || newRow < 1) return;

        //check to see if we are in a blank spot or around another card
        if (Board.Instance.boardState[newRow][newCol] != null)
        {
            //went to an occupied square, just take the push direction of our entry
            pushDir = move;
        }
        else pushDir = Directions.NOPUSH; //else no push required

        //update our column values
        placedColumn = newCol;
        placedRow = newRow;

        //check is valid
        isValid = (Board.Instance.IsValidMove(hand[chosenIndex], placedRow, placedColumn, pushDir));

        //rerender
        RenderCard();
    }

    /// <summary>
    /// Renders the card by the controlling variables on the board
    /// </summary>
    void RenderCard()
    {
        //get offset
        Vector3 offset = Vector3.zero;
        switch (pushDir)
        {
            case Directions.UP: offset.y = -PUSHOFFSET; break;
            case Directions.LEFT: offset.x = PUSHOFFSET; break;
            case Directions.DOWN: offset.y = PUSHOFFSET; break;
            case Directions.RIGHT: offset.x = -PUSHOFFSET; break;
            default: break;
        }

        //get render position
        Vector3Int pos = Board.IndexToRenderPoint(new Vector3Int(placedColumn, placedRow, 0));

        Vector3 finalPos = pos + offset;

        hand[chosenIndex].transform.position = finalPos;

        //change card color by move validity
        choiceOutline.color = isValid ? validColorTint : invalidColorTint;
    }

    /// <summary>
    /// Submits the current hand to the manager
    /// </summary>
    /// <param name="card">The card to place</param>
    /// <param name="row">The row to place in</param>
    /// <param name="col">The column to place in</param>
    /// <param name="push">The push direction, if applciable</param>
    void SubmitTurn(int indx, int row, int col, Directions push)
    {
        print(string.Format("Turn Submitted:\nCard: {0}\nIndex: {1}\nAt: [{2},{3}]\nPush: {4}", hand[indx].name, indx, row, col, push));
        //reset controlling values
        isTurn = false;

        CardScript card = hand[indx];

        //draw card
        DrawCard(indx);

        //let manager know
        GameManager.Instance.NotifyOfTurn(card, row, col, push);
    }

    /// <summary>
    /// Take turn logic for human interaction
    /// </summary>
    private void TakeHumanTurn()
    {
        /*
         * (1) Swap state to select card and position and push
         * (2) Place the card
         * (3) Tell manager
         */
        
        //allow choosing cards
        isTurn = true;
        isValid = false;
        isChoosingFromHand = true;
        isPlacingCard = false;
        chosenIndex = 0;
        placedRow = 2;
        placedColumn = 2;

        //init card chooser
        choiceOutline.color = validColorTint;
        choiceOutline.gameObject.SetActive(true);
        choiceOutline.transform.position = hand[chosenIndex].transform.position;
    }

    /// <summary>
    /// Take turn logic for our expectiminimax AI
    /// </summary>
    public void TakeAITurn()
    {
        //...TODO
        /*
         * (1) Run AI
         * (2) Get returned card and position
         * (3) Play
         */

        //AILoadingWheel.gameObject.SetActive(true);
        //_calcThread = new Thread(AITurnBackground);
        AITurnBackground();
    }

    /// <summary>
    /// Thinks of AI turn in the background
    /// </summary>
    /// <returns>IEnum wait</returns>
    void AITurnBackground()
    {  
        Response local = aiComponent.ConsiderTurn(deck, hand);
        //calcedMove = resp;
        SubmitTurn(local.cardIndex, local.rowNum, local.colNum, local.dir);
    }

    /// <summary>
    /// Gives this player a gem
    /// </summary>
    public void GiveGem()
    {
        gemSpots[gemCounter].sprite = gemMine;
        gemCounter++;
    }

    /// <summary>
    /// Takes a gem from this player
    /// </summary>
    public void TakeGem()
    {
        gemCounter--;
        gemSpots[gemCounter].sprite = gemNotMine;
    }
}
