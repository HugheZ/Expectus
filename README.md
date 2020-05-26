# Expectus
An expectiminimax implementation for Joustus

### Contributors
* [Mark Brestensky](https://github.com/BrestenskyMW1)
* [Nathan Stoner](https://github.com/Naxhi)

## Joustus
Joustus is a turn-based card game developed by Yacht Club Games as part of the Shovel Knight: King of Cards expansion. During play, each player will take turns placing cards onto the field in empty spaces. Cards can additionally push other cards by their given direction, cascading into other cards in the push direction. So long as no pushed cards have an opposing push arrow, the push is valid. Additionally, 3 gems spawn on the grid at the beginning of a match. While cards cannot be placed onto gems, they can be pushed onto gems. When the grid is full, the player with the most of their color cards on gems will win the game.

Our simplified implementation uses a 3x3 grid with additional graveyard spaces, places 3 gems randomly, and standardizes each side's randomly shuffled deck.

## Expectiminimax
Expectiminimax is an adapted minimax algorithm that has chance nodes in the minimax search tree for random events. Each of these stochastic nodes is evaluated by summing every possible card's probability multiplied by its expected utility.

Expectiminimax has a much worse runtime than minimax due to the branching factor of chance nodes. To circumvent this, this implementation reduces memory footprint by reusing card objects, minimizing board duplication, and implementing alpha-beta pruning. The last of these is more difficult to imlement for expectiminimax, but it is possible due to our bounded utility metric. Additionally, the algorithm only searches to a depth of 5, again to reduce the likelihood of a crash.


### Fair Use
Although this project makes use of some of the sprites from Shovel Knight, the use of these assets is covered by US Copyright fair use laws for education and research. The purpose of this game is to evaluate the fitness of expectiminimax to solve Joustus, a grid-based puzzle game. Sprite assets were used only to capture the game for evaluation. All code is otherwise developed in-house.
