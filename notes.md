# Unity.Netcode

- **Host** - Both a player and a server.
- **Server** - No player.
- **Client** - A player.

By default **a client has no authority to change the game state**. 
This due to security reasons.
When the clients cannot be trusted, such as in competitive games, the workflow goes as
follows:

1. The client press the "W" key.
2. The server receives the input and moves the player's transform accordingly.
3. The server sends the client the state of the game
4. The client updates its position.

However, to make things easier, we could skip points 2 and 3 and give the client the permission to update the game state by itself.
