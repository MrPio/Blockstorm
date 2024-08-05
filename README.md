# Blockstorm

An FPS game in the style of Minecraft. This is my first attempt at writing a **voxel engine** and **multiplayer** system
in Unity.

<div>
     <img src="https://github.com/user-attachments/assets/7628268a-8225-457e-aebf-7ae64729517a" width="23.5%"/>
     <img src="https://github.com/user-attachments/assets/2e8074e3-2208-40bc-b29e-de46f0550b8a" width="23.5%"/>
     <img src="https://github.com/user-attachments/assets/99a70a98-be7c-4ecb-a639-39c17c39f987" width="23.5%"/>
     <img src="https://github.com/user-attachments/assets/4ccc262c-ec67-42af-af12-be87df5cf1d9" width="23.5%"/>
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
|    Multiplayer implementation    |  🕓  |

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




## 👥 Authors

|     Author      |      Github profile      |
|:---------------:|:------------------------:|
| Valerio Morelli | https://github.com/MrPio |
