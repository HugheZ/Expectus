using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardScript : MonoBehaviour
{
    public bool canPushLeft;
    public bool canPushRight;
    public bool canPushUp;
    public bool canPushDown;
    public bool inGraveyard;
    public bool selectedromHand;
    public Sprite blueCard;
    public Sprite redCard;
    public bool isPlayerCard;
    public bool mustBeHidden;

    // Start is called before the first frame update
    void Start()
    {
        if (!isPlayerCard)
            GetComponent<SpriteRenderer>().sprite = redCard;
        else
            GetComponent<SpriteRenderer>().sprite = blueCard;
    }

    /// <summary>
    /// Checks to see if we can push the card in the given direction
    /// </summary>
    /// <param name="dir">The direction the other card is pushing</param>
    public bool CanPushThisWay(Directions dir)
    {
        if ((dir == Directions.UP && !canPushDown) ||
            (dir == Directions.LEFT && !canPushRight) ||
            (dir == Directions.RIGHT && !canPushLeft) ||
            (dir == Directions.DOWN && !canPushUp)) return true;
        return false;
    }
}
