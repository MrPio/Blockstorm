# Blockstorm

 An FPS game in the style of Minecraft. This is my first attempt at writing a **voxel engine** and **multiplayer** system in Unity.


## 🧊 What is a Voxel Engine?

 For example, a cube map of size 128x128x10 has 163,840 cubes, which is 1,966,080 triangles and 5,898,240 faces. If each cube is a separate game object, Unity draws each one in a separate **batch**, i.e. a GPU operation.
 This is the fastest way to create a map, but has a huge computational cost in the rendering phase, even for such a small map.

 We could try enabling **static batching** on each cube to reduce the number of batches, but that doesn't change the fact that a lot of unnecessary, hidden faces are drawn.

 A **Voxel Engine** draws only the visible part of the map. The hidden faces, which are many, are not rendered. Also, the map is divided into chunks so that 1 batch operation can draw multiple blocks at once.

## 🧭 Roadmap

|        Task        |                 Done                |
|:------------------:|:-----------------------------------:|
| Player movement |      ✅       |
| Basic Voxel Engine |      ✅       |
| UV mapping and texture atlas                   | ✅ |
| Texturepack and BlockTypes array                   | ✅ |
| Chunk subdivision                   |   ✅     |
| Map generator | ✅ |
| Creating and deleting blocks | ✅ |
| Submeshes for transparent blocks | ✅ |
| Inventory management | ✅ |
| Multiplayer implementation | 🕓  |

## 🖼️ Misc

https://github.com/MrPio/Blockstorm/assets/22773005/e25463cc-ab7e-43fe-b7b9-2c0adc6ca4b8

![Screenshot 2024-07-15 101936](https://github.com/user-attachments/assets/1917d90b-392e-472f-b486-5979aca787d6)

![Screenshot 2024-07-15 102200](https://github.com/user-attachments/assets/3ebc0807-0c6c-4289-87a8-3210c02b3cf3)

<div style="display: flex; width: 100%;">
 <img src="https://github.com/user-attachments/assets/50fe4225-7ecb-42bd-a3fc-61d3ce5af7d5" width="49.5%"/>
 <img src="https://github.com/user-attachments/assets/1c7d7ac3-9246-466e-809d-1fc4be379fd4" width="49.5%"/>
</div>



## 👥 Authors
|        Author       |            Github profile           |
|:-------------------:|:-----------------------------------:|
|   Valerio Morelli   |       https://github.com/MrPio      |
