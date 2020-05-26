Expectus: The Joustus AI

This AI is an expectiminimax AI implemented for Shovel Knight's Joustus.

Run Instructions:____________________________________________________________
│
├─ Open the Build folder and run the .exe file
│
├─ Ensure that the game is running in a 16:9 resolution (this allows all UI
│  to properly load on the screen.
│
├─ In the main menu, select the number of AIs you wish to play
│  with from the dropdown menu.
│
└─ Press the play button

Game Instructions:___________________________________________________________

The rules for Joustus are in the Main Menu's About page. Controls are here:

wasd + arrows: move up and down card selection, move card on board

space / left click : select card / place card

*cards can push others by navigating to an already-occupied space. The
card will hover around the side you will push from, and you can change
which direction to push from by using the arrow keys.

Colors:______________________________________________________________________

A red boarder means that the card is an illegal position

A green boarder means that the card can be placed in this position

By default, cards cannot be placed onto gems or into the 'graveyard'
(the outside section of the board)



**NOTE: the game is currently playable to completion in player vs player 
(save for small 'board cannot be completed' cases) but not in AI vs AI 
or Player vs AI. Player vs AI returns a player-only game and AI vs AI will
crash on an infinite loop through code.

**NOTE: the AI is found in Assets/Scripts/AI/Minimaxer.cs
