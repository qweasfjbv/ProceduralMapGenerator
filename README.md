
<!-- Ctrl + k + v-->

# :wrench: Procedural Map Generator

This procedural map generation idea is not solely my own. (Please [refer to the link](https://www.reddit.com/r/gamedev/comments/1dlwc4/procedural_dungeon_generation_algorithm_explained/) for more details.) 


I referenced a [reddit post](https://www.reddit.com/r/gamedev/comments/1dlwc4/procedural_dungeon_generation_algorithm_explained/) and made some modifications in an effort to create a more flexible map generator. 
There may be inefficient or incorrect parts, so if you notice anything, please let me know via email, and I will do my best to fix it ASAP.



### Features

* **Flexibility** : with lots of parameters
* **Toggle Visualization** : apply it directly to your project
* **Everything is modularized** : easily customizable.

### Compatibility

| Unity Version | Compatibility |
|:-------------:|:-------------:|
| **2022.3.47f** |:heavy_check_mark:|
| **2022.3.48f** |:heavy_check_mark:|

If only C# scripts are used, then other versions are also sufficiently usable.

<br/>

## Overview

![MapGenerator-SampleScene-WindowsMacLinux-Unity2022 3 47f1_DX11_2024-10-0420-02-47-ezgif com-video-to-gif-converter](https://github.com/user-attachments/assets/3b7ef7ae-a4f7-40cb-a34e-88eb1ab18d51)

This is visualized process of Procedural Map Generation.

It can be used for `Loguelike`, `DungeonCrawl` or other games if you need random map in runtime.

You can use it by starting coroutine named `MapGeneratedCoroutine` in `MapGenerator.cs`

```cs
// If you want to call in other script,
// apply Singleton on @MapGenerator in scene

StartCoroutine(MapGenerateCoroutine());
```

<br/>

## Parameters

There are lots of parameters and comments are also there.
But I thought some of the comments would be insufficient for the users.

( Cuz I'm not very good at **English**... )

So, I will provide additional explanations about the parameters here.

![image](https://github.com/user-attachments/assets/02890b6f-a1a7-497e-b358-3170987e9ea2)

- ### Random Spawn Type

`RandomSpawnType` means shape of spawnable region. There are two shapes. (Oval and Rectangle)

Below that, `SpawnRegionSize` represents the width and height of that type. 
However, this does not guarantee that the map will be created in that shape. 

Since spawnign locations, physics simulations, room selections... **Everything is random**, no one can predict what shape the map will take.

- ### Room Cnts
`SelectRoomCnt` refers to the number of rooms to be selected, and it generates rooms with sizes between `minRoomSize` and `maxRoomSize` based on this count.

`GenerateRoomCnt` is the total count of rooms generated at the beginning. Naturally, `SelectRoomCnt` is included in this.
And the remaining rooms, excluding `SelectRoom`, help create more diverse maps by increasing the distance between `SelectRoom`.

- ### Overlap Width

<image src="https://github.com/user-attachments/assets/5cc58741-7ef4-4271-85fc-3241c452ef0f" width="40%"></image>

If the overlapping area is wide, a straight hallway can be installed between the rooms; however, if the overlapping area is narrow, the hallways may protrude awkwardly from both sides of the rooms. 
Therefore, `OverlapWidth` is a parameter that defines the criteria for this.

- ### Hallway Width 

This parameter doesn't work now.

I'll update later.

- ### Smooth Level 

I implemented smoothing using [**cellular automata**](https://en.wikipedia.org/wiki/Cellular_automaton). The `SmoothLevel` serves as a parameter for the cellular automata; as the value decreases, the result becomes smoother, while as the value increases, it becomes more angular.

**Recommendation** : You can set values from 1 to 8, but if you are using smoothing, I recommend values of 4 or higher.

<br/>

## Examples

<image src="https://github.com/user-attachments/assets/c4f67b1d-d2ab-481e-8270-ea2173e80dfd" width="50%"></image>

<image src="https://github.com/user-attachments/assets/f114a85b-cbc9-406f-83bb-ba7cca822e6f" width="50%"></image>

<br/>

## Progress

As mentioned earlier, this is not entirely my idea. It has been modified to suit my needs. If you would like to check the original idea, please refer to the [link.](https://www.reddit.com/r/gamedev/comments/1dlwc4/procedural_dungeon_generation_algorithm_explained/)

This desrciption is intended to assist those who wish to customize this code or make it more efficient.

<br/>

- ### Randomly Spawn Rooms and Select Main Rooms

First, we should spawn rooms in region set before.

You need to create enough rooms to increase the distance between the Main Rooms, and some of these will also be used as hallways.

Then, you need to select a Main Room that meets the criteria. I chose a room that is large enough and has a balanced proportion without being skewed to one side

```cs
        for (int i = 0; i < rooms.Count; i++)
        {
            // Other codes...

            float size = scale.x * scale.y;                 // Calculate size of room
            float ratio = scale.x / scale.y;                // 
            if (ratio > 2f || ratio < 0.5f) continue;       // Ignore unbalance rooms
            tmpRooms.Add((size, i));
        }

      // Then sort tmpRooms, select rooms from 0-index 
```

<br/>

- ### Rasterize rooms into 2D Array data

After physics simulation, we must rasterize data into 2D Array.

This process is important but not difficult, so you will likely understand it after just looking at the code once.

Please refer to the `GenerateMapArr` and `MainRoomFraming` function in `MapGenerator.cs`

![image](https://github.com/user-attachments/assets/65a3694c-2fba-45c0-a039-8c5163b6bfe1)


<br/>

- ### Connect Rooms

![MapGenerator-SampleScene-WindowsMacLinux-Unity2022 3 47f1_DX11_2024-10-0420-02-47-ezgif com-video-to-gif-converter (1)](https://github.com/user-attachments/assets/c9cc5dcf-fd64-45f0-ac65-3d1de6491bbd)


To fully understand this process, prior knowledge of [Delaunay triangulation](https://en.wikipedia.org/wiki/Delaunay_triangulation) and [Minimum Spanning Tree (MST)](https://en.wikipedia.org/wiki/Minimum_spanning_tree) is required.

After connecting all the rooms through Delaunay triangulation, we need to create the MST to simplify the edges.

**MST** is a tree structure that includes all vertices in a given graph while minimizing the total weight of the edges.

We can find the MST using [Kruskal's algoritm](https://en.wikipedia.org/wiki/Kruskal%27s_algorithm). and it isn't complex.

<br/>

The issue is Delaunay triangulation. 

I used the [Bowyer-Watson algorithm](https://en.wikipedia.org/wiki/Bowyer%E2%80%93Watson_algorithm) to compute the Delaunay triangulation, and watching a [YouTube video](https://www.youtube.com/watch?v=GctAunEuHt4) will help in understanding it.

The Bowyer-Watson algorithm features a `SuperTriangle`, which represents an infinitely **large triangle**. 

As a result, in the actual game, there may be cases where not all rooms are connected, requiring either increasing the size of the SuperTriangle or implementing separate exception handling.

<br/>

- ### Generate Hallways

All the rooms have been connected through the above process. Now, we need to appropriately generate corridors to connect the rooms. 

This process is influenced by the previously mentioned parameter, `Overlap Width`, where a larger overlapping area results in straight corridors, and a smaller overlapping area leads to L-shaped corridors.

In particular, if the L-shaped corridors are created randomly, a rectangular-shaped map may be generated. To prevent this, the corners of the 'L' are designed to face the center of the map.

```cs
// Get center of vertices
int mapCenterX = map.GetLength(0) / 2;
int mapCenterY = map.GetLength(1) / 2;

int midX = (start.x + end.x) / 2;
int midY = (start.y + end.y) / 2;

int quadrant = DetermineQuadrant(midX - mapCenterX - minX, midY - mapCenterY - minY);

// Determine hallway ('L' or flipped 'L')
if (quadrant == 2 || quadrant == 3)
{
    CreateStraightHallway(start.x, start.y,end.x, start.y);    // Generate horizontal hallway first
    CreateStraightHallway(end.x, start.y, end.x, end.y);       // Then vertical hallway
}
else if (quadrant == 1 || quadrant == 4)
{
    CreateStraightHallway(start.x, start.y, start.x, end.y);   // Generate ertical hallway first
    CreateStraightHallway(start.x, end.y, end.x, end.y);       // Then horizontal hallway
}
```

<br/>


# :wrench: Auto Tiler

## Overview

## Parameters



