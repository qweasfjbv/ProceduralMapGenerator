using Delaunay;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum GridType { 
    None = 0,
    MainRoom,
    HallWay,
    TmpHallWay = 500,
    Cellular = 600
}


public enum RandomSpawnType {
    Oval = 0,
    Rectangle,
}


[RequireComponent(typeof(AutoTiling))]
public class MapGenerator: MonoBehaviour
{

    [Header("Map Generate Variables")]
    [SerializeField] private GameObject gridPrefab;
    [SerializeField] private RandomSpawnType randomSpawnType;   // Shape of the Region for randomly selected room position
    [SerializeField] private Vector2Int spawnRegionSize;    // Size of Region
    [SerializeField] private int generateRoomCnt;           // Count of Rooms to spawn
    [SerializeField] private int selectRoomCnt;             // Count of Rooms to select
    [SerializeField] private int minRoomSize;               // Size boundary for big rooms
    [SerializeField] private int maxRoomSize;
    [SerializeField] private int smallMinRoomSize;          // Size boundary for small rooms
    [SerializeField] private int smallMaxRoomSize;          
    [SerializeField] private int overlapWidth;              // Width to determine corridor ('L' shape or straight )
    [SerializeField] private int hallwayWidth;              // Width of hallway ( Recommend : lesser than 'maxRoomSize' )
    [SerializeField, Range(1, 9)] private int smoothLevel;  // 9 : Disable smooth


    [Header("Visualize Generating Progress")]
    [SerializeField] private bool isVisualizeProgress;      // On/Off
    [SerializeField] private float roomSpawnTerm;           
    [SerializeField] private Color32 roomColor;             // Color of all rooms ( include hallway grids )
    [SerializeField] private Color32 delaunayLineColor;     // Line Color when Bowyer-Watson in execution
    [SerializeField] private Color32 mstLineColor;          // Line Color when Kruskal in excution
    [SerializeField] private float lineWidth;               // Width of all lines
    [SerializeField] private float lineDrawTerm;            // Time between creating and deleting a LineRenderer.


    private List<GameObject> rooms = new List<GameObject>();
    private HashSet<Delaunay.Vertex> vertices = new HashSet<Delaunay.Vertex>();
    private List<Edge> hallwayEdges;
    private List<GameObject> lineRenderers = new List<GameObject>();
    private List<(int index, Vector2 pos)> selectedRooms = new List<(int, Vector2)>();

    private int[,] map; 
    private int minX = int.MaxValue, minY = int.MaxValue;
    private int maxX = int.MinValue, maxY = int.MinValue;

    public int MinX { get => minX; } public int MinY { get => minY; }    


    private void Start()
    { 
        StartCoroutine(MapGenerateCoroutine());
    }

    private IEnumerator MapGenerateCoroutine()
    {
        yield return new WaitForSeconds(1f);
        Time.timeScale = 2.0f;

        yield return StartCoroutine(SpawnRooms());
        yield return new WaitForSeconds(5f);                // Wait for Physics Update
        FindMainRooms(selectRoomCnt);

        GenerateMapArr();                                   // 
        MainRoomFraming();                                  // Frame Main rooms with walls
        yield return StartCoroutine(ConnectRooms());        // Connect Rooms ( Delaunay Triangulation and MST )
        yield return StartCoroutine(GenerateHallways());
        CellularAutomata(smoothLevel);                      // Smooth ( I think it isn't necessary )
        
        MapArrNormalization();                              // Normalize Map for auto tiling
        OnMapGenComplete();                                 // Transfer Map Data for auto tiling
        if (isVisualizeProgress) yield return new WaitForSeconds(1f);

        GetComponent<AutoTiling>().TilingMap();
        ClearObjects();

        Time.timeScale = 1.0f;
    }

    /** Randomly Spawn Rooms **/
    private IEnumerator SpawnRooms()
    {

        // Randomly spawn rooms
        for (int i = 0; i < generateRoomCnt; i++)
        {
            switch (randomSpawnType) {
                case RandomSpawnType.Oval:
                    rooms.Add(Instantiate(gridPrefab, GetRandomPointInOval(spawnRegionSize), Quaternion.identity));
                    break;
                case RandomSpawnType.Rectangle:
                    rooms.Add(Instantiate(gridPrefab, GetRandomPointInRect(spawnRegionSize), Quaternion.identity));
                    break;
            }

            if (i > selectRoomCnt)  rooms[i].transform.localScale = GetRandomScale(smallMinRoomSize, smallMaxRoomSize);
            else                    rooms[i].transform.localScale = GetRandomScale(minRoomSize, maxRoomSize);
            
            yield return new WaitForSeconds(roomSpawnTerm);
        }

        // Dynamic for Physics Interaction
        for (int i = 0; i < generateRoomCnt; i++)
        {
            rooms[i].GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
            rooms[i].GetComponent<Rigidbody2D>().gravityScale = 0f;
        }
    }
    private Vector3 GetRandomPointInOval(Vector2Int size)
    {
        float theta = Random.Range(0, 2 * Mathf.PI);
        float rad = Mathf.Sqrt(Random.Range(0, 1f));

        return new Vector3(size.x * rad * Mathf.Cos(theta), size.y * rad * Mathf.Sin(theta));
    }
    private Vector3 GetRandomPointInRect(Vector2Int size)
    {
        float width = Random.Range(-size.x, size.x);
        float height = Random.Range(-size.y, size.y);
        return new Vector3(width, height, 0);
    }
    private Vector3 GetRandomScale(int minS, int maxS)
    {
        int x = Random.Range(minS, maxS) * 2;
        int y = Random.Range(minS, maxS) * 2;

        return new Vector3(x, y, 1);
    }
    private int RoundPos(float n, int m)
    {
        return Mathf.FloorToInt(((n + m - 1) / m)) * m;
    }

    /** Select Main Rooms**/
    private void FindMainRooms(int roomCount)
    {
        // Temporaray store each room's Size, Ratio, Index
        List<(float size, int index)> tmpRooms = new List<(float size, int index)>();

        for (int i = 0; i < rooms.Count; i++)
        {
            rooms[i].GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            rooms[i].GetComponent<BoxCollider2D>().isTrigger = true;
            rooms[i].transform.position = new Vector3(RoundPos(rooms[i].transform.position.x, 1), RoundPos(rooms[i].transform.position.y, 1), 1);


            Vector3 scale = rooms[i].transform.localScale;
            float size = scale.x * scale.y;                 // Calculate size of room
            float ratio = scale.x / scale.y;                // 
            if (ratio > 2f || ratio < 0.5f) continue;       // Ignore unbalance rooms
            tmpRooms.Add((size, i));
        }

        // Order by Room size
        var sortedRooms = tmpRooms.OrderByDescending(room => room.size).ToList();

        foreach (var room in rooms)
        {
            room.SetActive(false);
        }

        // Select Rooms ( Except narrow room )
        int count = 0;
        selectedRooms = new List<(int, Vector2)>();
        foreach (var roomInfo in sortedRooms)
        {
            if (count >= roomCount) break; 
            GameObject room = rooms[roomInfo.index];
            room.GetComponent<SpriteRenderer>().color = roomColor;
            room.SetActive(true);

            vertices.Add(new Delaunay.Vertex((int)room.transform.position.x, (int)room.transform.position.y));
            selectedRooms.Add((roomInfo.index, new Vector2((int)room.transform.position.x, (int)room.transform.position.y)));
            count++;
        }

    }

    /** Rasterize rooms into 2D Array Data **/
    private void GenerateMapArr()
    { 
        foreach (var room in rooms)
        {
            Vector3 pos = room.transform.position;
            Vector3 scale = room.transform.localScale;

            minX = Mathf.Min(minX, Mathf.FloorToInt(pos.x - scale.x));
            minY = Mathf.Min(minY, Mathf.FloorToInt(pos.y - scale.y));
            maxX = Mathf.Max(maxX, Mathf.CeilToInt(pos.x + scale.x));
            maxY = Mathf.Max(maxY, Mathf.CeilToInt(pos.y + scale.y));
        }

        int width = maxX - minX;
        int height = maxY - minY;
        map = new int[height, width];

        for (int i = 0; i < height; i++)
            for (int j = 0; j < width; j++) 
                map[i, j] = -1;

        for (int i = 0; i < rooms.Count; i++)
        {
            Vector3 pos = rooms[i].transform.position;
            Vector3 scale = rooms[i].transform.localScale;

            for (int x = (int)-scale.x / 2; x < scale.x / 2; x++)       // Store grid info in map arr
            {
                for (int y = (int)-scale.y / 2; y < scale.y / 2; y++)
                {
                    int mapX = Mathf.FloorToInt(pos.x - minX + x);
                    int mapY = Mathf.FloorToInt(pos.y - minY + y);
                    map[mapY, mapX] = i;
                }
            }
        }
    }
    private void MainRoomFraming()
    {
        foreach (var (index, pos) in selectedRooms)
        {
            int selectedId = index;

            rooms[selectedId].GetComponent<SpriteRenderer>().color = roomColor;

            int minIx = int.MaxValue, minIy = int.MaxValue;
            int maxIx = int.MinValue, maxIy = int.MinValue;

            for (int y = 0; y < map.GetLength(0); y++)
            {
                for (int x = 0; x < map.GetLength(1); x++)
                {
                    if (map[y, x] == selectedId)
                    {
                        minIx = Mathf.Min(minIx, x);
                        maxIx = Mathf.Max(maxIx, x);
                        minIy = Mathf.Min(minIy, y);
                        maxIy = Mathf.Max(maxIy, y);
                    }
                }
            }

            for (int y = minIy; y <= maxIy; y++)
            {
                for (int x = minIx; x <= maxIx; x++)
                {
                    if (x == minIx || x == maxIx || y == minIy || y == maxIy)
                    {
                        map[y, x] = -1;
                    }
                }
            }
        }
    }

    /** Connect Rooms **/
    private IEnumerator ConnectRooms()
    {
        yield return StartCoroutine(DelaunayTriangulation.TriangulateCoroutine(vertices, isVisualizeProgress, delaunayLineColor, lineWidth, lineDrawTerm));
        var triangles = DelaunayTriangulation.RetTriangles;
        var graph = new HashSet<Delaunay.Edge>();

        foreach (var triangle in triangles)
        {
            if (isVisualizeProgress)
            {
                var lines = triangle.GetVisualLines();
                foreach (var line in lines) Destroy(line);
            }
            graph.UnionWith(triangle.edges);
        }

        if(isVisualizeProgress) yield return new WaitForSeconds(1f);

        hallwayEdges = Kruskal.MinimumSpanningTree(graph);

        if (isVisualizeProgress)
        {
            foreach(var edge in hallwayEdges)
            {
                lineRenderers.Add(Visualization.Instance.SpawnLineRenderer(edge.a.ToVector3(), edge.b.ToVector3(), lineWidth, mstLineColor));
            }
        }
    }

    /** Generate Hallways **/
    private IEnumerator GenerateHallways()
    {
        Vector2Int size1 = new Vector2Int(2, 2);
        Vector2Int size2 = new Vector2Int(2, 2);

        foreach (Delaunay.Edge edge in hallwayEdges)
        {
            Delaunay.Vertex start = edge.a;
            Delaunay.Vertex end = edge.b;

            size1 = new Vector2Int((int)rooms[map[start.y-minY, start.x-minX]].transform.localScale.x, (int)rooms[map[start.y-minY, start.x-minX]].transform.localScale.y);
            size2 = new Vector2Int((int)rooms[map[end.y-minY, end.x-minX]].transform.localScale.x, (int)rooms[map[end.y-minY, end.x-minX]].transform.localScale.y);

            CreateHallwayLine(start, end, size1, size2);
            yield return new WaitForSeconds(lineDrawTerm * 2);
        }

        foreach (Delaunay.Edge edge in hallwayEdges)
        {
            Delaunay.Vertex start = edge.a;
            Delaunay.Vertex end = edge.b;

            size1 = new Vector2Int((int)rooms[map[start.y - minY, start.x - minX]].transform.localScale.x, (int)rooms[map[start.y - minY, start.x - minX]].transform.localScale.y);
            size2 = new Vector2Int((int)rooms[map[end.y - minY, end.x - minX]].transform.localScale.x, (int)rooms[map[end.y - minY, end.x - minX]].transform.localScale.y);

            CreateHallwayWidth(start, end, size1, size2);
            yield return new WaitForSeconds(lineDrawTerm * 2);
        }

    }
    private void CreateHallwayLine(Delaunay.Vertex start, Delaunay.Vertex end, Vector2Int startSize, Vector2Int endSize)
    {
        bool isHorizontalOverlap = Mathf.Abs(start.x - end.x) < ((startSize.x + endSize.x) / 2f - overlapWidth);
        bool isVerticalOverlap = Mathf.Abs(start.y - end.y) < ((startSize.y + endSize.y) / 2f - overlapWidth);


        if (isVerticalOverlap)          // Generate Horizontal Corridor
        {
            int startY = Mathf.Min(start.y + startSize.y / 2, end.y + endSize.y / 2) + Mathf.Max(start.y - startSize.y / 2, end.y - endSize.y / 2);
            startY /= 2;
            for (int x = Mathf.Min(start.x + startSize.x / 2, end.x + endSize.x / 2); x <= Mathf.Max(start.x - startSize.x / 2, end.x - endSize.x / 2); x++) 
            {
                InstantiateGrid(x, startY);
            }
        }
        else if (isHorizontalOverlap)   // Generate Vertical Corridor
        {
            int startX = Mathf.Min(start.x + startSize.x / 2, end.x + endSize.x / 2) + Mathf.Max(start.x - startSize.x / 2, end.x - endSize.x / 2);
            startX /= 2;
            for (int y = Mathf.Min(start.y + startSize.y / 2, end.y + endSize.y / 2); y <= Mathf.Max(start.y - startSize.y / 2, end.y - endSize.y / 2); y++) 
            {
                InstantiateGrid(startX, y);
            }
        }
        else                            //  Generate 'L' shaped corridor
        {
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
        }
    }
    private void CreateHallwayWidth(Delaunay.Vertex start, Delaunay.Vertex end, Vector2Int startSize, Vector2Int endSize)
    {
        bool isHorizontalOverlap = Mathf.Abs(start.x - end.x) < ((startSize.x + endSize.x) / 2f - overlapWidth);
        bool isVerticalOverlap = Mathf.Abs(start.y - end.y) < ((startSize.y + endSize.y) / 2f - overlapWidth);

        if (isVerticalOverlap)
        {
            int startY = Mathf.Min(start.y + startSize.y / 2, end.y + endSize.y / 2) + Mathf.Max(start.y - startSize.y / 2, end.y - endSize.y / 2);
            startY /= 2;
            for (int x = (int)Mathf.Min(start.x + startSize.x / 2, end.x + endSize.x / 2); x <= (int)Mathf.Max(start.x - startSize.x / 2, end.x - endSize.x / 2); x++)
            {
                AddHallwayWidth(x, startY);
            }
        }
        else if (isHorizontalOverlap)
        {
            int startX = Mathf.Min(start.x + startSize.x / 2, end.x + endSize.x / 2) + Mathf.Max(start.x - startSize.x / 2, end.x - endSize.x / 2);
            startX /= 2;
            for (int y = (int)Mathf.Min(start.y + startSize.y / 2, end.y + endSize.y / 2); y <= (int)Mathf.Max(start.y - startSize.y / 2, end.y - endSize.y / 2); y++)
            {
                AddHallwayWidth(startX, y);
            }
        }
        else
        {
            int mapCenterX = map.GetLength(0) / 2;
            int mapCenterY = map.GetLength(1) / 2;

            int midX = (start.x + end.x) / 2;
            int midY = (start.y + end.y) / 2;

            int quadrant = DetermineQuadrant(midX - mapCenterX - minX, midY - mapCenterY - minY);

            if (quadrant == 2 || quadrant == 3)
            {
                CreateStraightHallwayWidth(start.x, start.y, end.x, start.y);
                CreateStraightHallwayWidth(end.x, start.y, end.x, end.y); 
            }
            else if (quadrant == 1 || quadrant == 4)
            {
                CreateStraightHallwayWidth(start.x, start.y, start.x, end.y); 
                CreateStraightHallwayWidth(start.x, end.y, end.x, end.y); 
            }
        }
    }
    private void CreateStraightHallway(int startX, int startY, int endX, int endY)
    {
        for (int x = Mathf.Min(startX, endX); x <= Mathf.Max(startX, endX); x++)
        {
            for (int y = Mathf.Min(startY, endY); y <= Mathf.Max(startY, endY); y++)
            {
                InstantiateGrid(x, y);
            }
        }
    }
    private void CreateStraightHallwayWidth(int startX, int startY, int endX, int endY)
    {

        for (int x = Mathf.Min(startX, endX); x <= Mathf.Max(startX, endX); x++)
        {
            for (int y = Mathf.Min(startY, endY); y <= Mathf.Max(startY, endY); y++)
            {
                AddHallwayWidth(x, y);
            }
        }
    }
    private int DetermineQuadrant(int x, int y)
    {
        if (x >= 0 && y >= 0) return 1;
        if (x < 0 && y >= 0) return 2;
        if (x < 0 && y < 0) return 3;
        if (x >= 0 && y < 0) return 4;

        return -1;  // Never happen 
    }
    private void AddHallwayWidth(int x, int y)
    {
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                int px = x + i; int py = y + j;
                if (px < minX || py < minY || py >= maxY || px >= maxX) continue;
                if (map[py - minY, px - minX] == (int)GridType.TmpHallWay) continue;


                if (map[py - minY, px - minX] == -1 || !rooms[map[py - minY, px - minX]].activeSelf)
                {
                    map[py - minY, px - minX] = (int)GridType.TmpHallWay;
                    GameObject grid = Instantiate(gridPrefab, new Vector3(px + 0.5f, py + 0.5f, 0), Quaternion.identity);
                    grid.GetComponent<SpriteRenderer>().color = roomColor;
                }
            }
        }
    }

    /** Smoothing ( Additional ) **/
    private void CellularAutomata(int n)
    {
        for (int x = 0; x < maxX - minX; x++)
        {
            for (int y = 0; y < maxY - minY; y++)
            {
                if (map[y, x] == (int)GridType.TmpHallWay) continue;
                if ((map[y, x] != -1 && map[y, x] != (int)GridType.TmpHallWay && rooms[map[y, x]].activeSelf)) continue;


                int nonWallCount = 0;

                // Check around grids
                for (int offsetX = -1; offsetX <= 1; offsetX++)
                {
                    for (int offsetY = -1; offsetY <= 1; offsetY++)
                    {
                        int checkX = x + offsetX;
                        int checkY = y + offsetY;

                        if (checkX < 0 || checkX >= maxX - minX || checkY < 0 || checkY >= maxY - minY)
                        {
                            continue;
                        }
                        else if (map[checkY, checkX] == -1) continue;
                        else if (map[checkY, checkX] == (int)GridType.Cellular) continue;
                        else if (map[checkY, checkX] == (int)GridType.TmpHallWay || rooms[map[checkY, checkX]].activeSelf)
                        {
                            nonWallCount++;
                        }
                    }
                }

                if (nonWallCount >= n)
                {
                    map[y, x] = (int)GridType.Cellular;
                }
            }
        }


        for (int x = 0; x < maxX - minX; x++)
        {
            for (int y = 0; y < maxY - minY; y++)
            {
                if (map[y, x] == (int)GridType.Cellular) map[y, x] = (int)GridType.TmpHallWay;
            }
        }
    }

    private void InstantiateGrid(int x, int y)
    {
        if (map[y - minY, x - minX] == -1)
        {
            GameObject grid = Instantiate(gridPrefab, new Vector3(x + 0.5f, y + 0.5f, 0), Quaternion.identity);
            grid.GetComponent<SpriteRenderer>().color = roomColor;
            map[y - minY, x - minX] = (int)GridType.TmpHallWay;
        }
        else if (map[y - minY, x - minX] != (int)GridType.TmpHallWay)
        {
            if (rooms[map[y - minY, x - minX]].activeSelf) return;

            rooms[map[y - minY, x - minX]].SetActive(true);
            rooms[map[y - minY, x - minX]].GetComponent<SpriteRenderer>().color = roomColor;
        }
    }
    private void MapArrNormalization()
    {
        for (int i = 0; i < map.GetLength(0); i++)
        {
            for (int j = 0; j < map.GetLength(1); j++)
            {
                if (map[i, j] < 0 || (map[i, j] != (int)GridType.TmpHallWay && !rooms[map[i, j]].activeSelf))
                {
                    map[i, j] = (int)GridType.None;
                }
                else if (map[i, j] == (int)GridType.TmpHallWay) map[i, j] = (int)GridType.HallWay;
                else map[i, j] = (int)GridType.MainRoom;
            }
        }
    }
    private void ClearObjects()
    {
        for (int i = 0; i < rooms.Count; i++)
        {
            Destroy(rooms[i]);
        }
        rooms.Clear();

        GetComponent<AutoTiling>().Clear();
    }
    private void OnMapGenComplete()
    {
        GetComponent<AutoTiling>().SetMapInfos(ref map);

        if (isVisualizeProgress)
        {
            foreach (var lineRenderer in lineRenderers)
            {
                Destroy(lineRenderer);
            }
            lineRenderers.Clear();
        }
    }

    /** Getter Functions **/
    public GridType GetGridType(int x, int y)
    {
        if (y < 0 || x < 0 || (maxX - minX) <= x || (maxY - minY) <= y) return GridType.None;

        return (GridType)map[y, x];
    }


}
