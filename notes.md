# Unity.Netcode

## The basics

- **Host** - Both a player and a server.
- **Server** - No player.
- **Client** - A player.

## Ownership

By default **a client has no authority to change the game state**.
This due to security reasons.
In other words, Either the server (default) or any connected and approved client owns each `NetworkObject` and can spawn
and despawn them.

When the clients cannot be trusted, such as in competitive games, the workflow goes as
follows:

1. The client press the "W" key.
2. The server receives the input and moves the player's transform accordingly.
3. The server sends the client the state of the game
4. The client updates its position.

However, to make things easier, we could skip points 2 and 3 and give the client the permission to update the game state
by itself.

### Getting started

To start using Netcode for gameobject, one should:

1. Create a NetworkManager empty gameobject and attach the `NetworkManager` Netcode script.
2. Attach the `NetworkObject` script to every object that should respond to and interact with Netcode.
3. Add the prefab to the `NetworkManager`'s NetworkPrefabs list.
4. Use `ClientNetworkTransform` instead of `NetworkTransform` to give permission to all the clients to move the player.

## Network variables

Sync the value of a variable. Requires the script to extend `NetworkBehaviour` and thus, to have a `NetworkObject`
script attached.

However, network variable only works for *value-type* variables, such as primitive types and `struct`s of primitive
types.
`class` and `object`, thus including `string`, are *ref-type* variables and so cannot be used.

### What about strings?

We can use array of chars of fixed dimension. Fortunately, `FixedString` provides us a higher level view. Here's an
example:

```csharp
private NetworkVariable<MyCustomData> randomNumber = new NetworkVariable<MyCustomData>( 
    new MyCustomData { 
        _int = 56, 
        _bool = true, 
    }, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

public struct MyCustomData : INetworkSerializable {
    public int _int; 
    public FixedString128Bytes message; 

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter { 
        serializer.SerializeValue(ref _int); 
        serializer.SerializeValue(ref message);
    }
}
```

Note: Each player instance has its own version of the network variable! One for each Script in the scene. One is the
owner, the others are the other players.

## Remote Procedure Calls

With the term *Procedure* we refer to `void` functions.
There are two types of RPCs: *ServerRpc* and *ClientRpc*.
Requires the script to extend `NetworkBehaviour` and thus, to have a `NetworkObject` script attached.

The procedure name must end with "ServerRpc" and "ClientRpc" respectively.
This helps to understand where the code is executed.

The RPC accepts any *value-type* arguments plus `string` objects.

```csharp
[ServerRpc] 
private void TestServerRpc(ServerRpcParams serverRpcParams) { 
    Debug.Log("TestServerRpc " + OwnerClientId + "; " + serverRpcParams.Receive.SenderClientId);
}
...
TestServerRpc(new());
```

### Rpc

Huge discovery! `[Rpc(SendTo.Everyone)]` are far too flexible and easy to use!

---

# Digressions

## Start() vs Awake()

Both are called exactly once in the lifetime of the script; however, Awake is called when the script object is
initialised, **regardless of whether the script is enabled**.