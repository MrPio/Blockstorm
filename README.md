# Blockstorm

An FPS game in the style of Minecraft. This is my first attempt at writing a **voxel engine** and **multiplayer** system
in Unity.

<div>    
     <img src="https://github.com/user-attachments/assets/8ccb789e-1b06-46de-9cff-e70d94923898" width="23.5%"/>
     <img src="https://github.com/user-attachments/assets/5a127610-b014-49ff-b222-7d3d095be44c" width="23.5%"/>
     <img src="https://github.com/user-attachments/assets/2e7b5210-39d5-4c19-b4e8-bee7c4441b09" width="23.5%"/>
     <img src="https://github.com/user-attachments/assets/86425bf6-7405-4ffc-82f2-4e9ccc84e551" width="23.5%"/>
</div>

## 🧊 What is a Voxel Engine?

For example, a cube map of size 128x128x10 has 163,840 cubes, which are 1,966,080 triangles and 5,898,240 faces. If each
cube is a separate game object, Unity draws each one in a separate **batch**, i.e. a GPU operation.
This is the fastest way to create a map, but it has a huge computational cost in the rendering phase, even for such a
small map.

We could try enabling **static batching** on each cube to reduce the number of batches, but that doesn't change the fact
that many unnecessary, hidden faces are drawn.

A **Voxel Engine** draws only the visible part of the map. The hidden faces, which are many, are not rendered. Also, the
map is divided into chunks so that 1 batch operation can draw multiple blocks at once.

### 🎯 The Voxel Raycast problem

When digging or placing a block, we need to detect which voxel the player is looking at.
A common approach to solving this problem is to draw a line of increasing length in front of the camera until it reaches
a solid voxel or the reach length.

```c#
for (var lenght = 0.1f; lenght < Reach; lenght += 0.1f)
{
    var pos = Vector3Int.FloorToInt(_transform.position + _transform.forward * (length));
    var blockType = _wm.GetVoxel(pos);
    ...
}
```

Now, as the player may be looking at a block with a steep angle, we need to find a balance between how precise we want
the Raycast to be, i.e. a small value for the step increment, and how fast we want it to be, i.e. how many steps are
taken.
In the example above, a value of 0.1 has been chosen.

However, as this whole operation is performed on each player's camera movement, it needs to be done as quickly as
possible.

I, on the other hand, decided to use the higher-level `Physics.Raycast' Unity feature.

```c#
if (Physics.Raycast(ray, out hit, Reach, 1 << LayerMask.NameToLayer("Ground")))
    if (hit.collider != null)
    {
        var pos = Vector3Int.FloorToInt(hit.point + cameraTransform.forward * 0.05f);
        var blockType = _wm.GetVoxel(pos);
        ...
    }
```

This solution is not only faster, but also more general, as it can be applied to enemy damage detection. In fact, by
changing the layer on which the ray is cast to "Enemy", we can detect whether the shot has reached the enemy and, if so,
which part of his body has been hit, thanks to the multiple colliders.

## 🌍 Relay service
The relay service was used in conjunction with the lobby service to provide remote multiplayer.
To avoid interfering with the host's router, firewall, and port forwarding, the relay system inserts a server between the host and the clients.

In this project, to switch between localhost and remote modes you should:
- Deactivate the `LobbyManager`,
- Activate the `DebugManager`,
- Set the `NetworkManager`'s transport protocol type to "Relay Unity Transport".

## 🧭 Roadmap

|               Task               | Done |
|:--------------------------------:|:----:|
|         Player movement          |  ✅   |
|        Basic Voxel Engine        |  ✅   |
|   UV mapping and texture atlas   |  ✅   |
| Texturepack and BlockTypes array |  ✅   |
|        Chunk subdivision         |  ✅   |
|          Map generator           |  ✅   |
|   Creating and deleting blocks   |  ✅   |
| Submeshes for transparent blocks |  ✅   |
|       Inventory management       |  ✅   |
|    Multiplayer implementation    |  ✅  |

## 🖼️ In-Game Screenshots

![vlcsnap-2024-09-08-14h37m25s975](https://github.com/user-attachments/assets/6e773bf0-e7f6-4977-895f-f0c4c2a02303)
![vlcsnap-2024-09-08-14h37m23s070](https://github.com/user-attachments/assets/95e0ca90-fefa-4160-8668-bea6450d50b4)
![vlcsnap-2024-09-08-14h44m27s061](https://github.com/user-attachments/assets/2dae2157-ea19-4524-9a41-401877e1571f)
![vlcsnap-2024-09-08-14h37m08s955](https://github.com/user-attachments/assets/37200ac3-7a12-4037-ae95-052df54203a6)
![vlcsnap-2024-09-08-14h36m59s045](https://github.com/user-attachments/assets/b2af876f-f75f-4b74-90d8-f5308801eb91)
![vlcsnap-2024-09-08-14h36m45s626](https://github.com/user-attachments/assets/bf54fc4a-0273-49b3-b591-fc658eff5816)
![vlcsnap-2024-09-08-14h36m28s344](https://github.com/user-attachments/assets/4e98c68d-87a9-4c83-b004-a9ef5411c85d)
![vlcsnap-2024-09-08-14h36m09s382](https://github.com/user-attachments/assets/188ca242-5733-48fb-b506-abe65919ddb6)
![vlcsnap-2024-09-08-14h35m24s158](https://github.com/user-attachments/assets/e64ba5c1-3041-43cf-bf11-8f1dedc8171e)
![vlcsnap-2024-09-08-14h35m07s167](https://github.com/user-attachments/assets/7bde99ac-556f-45c4-b82b-a0b1a4dcab5f)
![vlcsnap-2024-09-08-14h34m59s535](https://github.com/user-attachments/assets/fe733db0-9d56-4832-923c-e0e58d895620)



## 🖼️ Misc

https://github.com/MrPio/Blockstorm/assets/22773005/e25463cc-ab7e-43fe-b7b9-2c0adc6ca4b8

![Screenshot 2024-07-15 101936](https://github.com/user-attachments/assets/1917d90b-392e-472f-b486-5979aca787d6)

![Screenshot 2024-07-15 102200](https://github.com/user-attachments/assets/3ebc0807-0c6c-4289-87a8-3210c02b3cf3)

<div style="display: flex; width: 100%;">
 <img src="https://github.com/user-attachments/assets/50fe4225-7ecb-42bd-a3fc-61d3ce5af7d5" width="49.5%"/>
 <img src="https://github.com/user-attachments/assets/1c7d7ac3-9246-466e-809d-1fc4be379fd4" width="49.5%"/>
</div>

 <img src="https://github.com/user-attachments/assets/38f9ff7a-28a6-48d4-ac46-6d05bb4515fb" width="99.5%"/>

 <img src="https://github.com/user-attachments/assets/01e77cc1-1a6c-4853-b3a5-6198f852cb83" width="99.5%"/>
 <img src="https://github.com/user-attachments/assets/ae6f41fe-fbc0-426d-9954-c7348e0317d6" width="99.5%"/>
 <img src="https://github.com/user-attachments/assets/8bb092cd-4cd3-4303-81a5-691fe1208851" width="99.5%"/>



## 👥 Authors

|     Author      |      Github profile      |
|:---------------:|:------------------------:|
| Valerio Morelli | https://github.com/MrPio |
