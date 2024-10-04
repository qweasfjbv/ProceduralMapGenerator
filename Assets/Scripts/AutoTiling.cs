using UnityEngine;
using UnityEngine.Tilemaps;

public class AutoTiling : MonoBehaviour
{
    [Header("Tilemaps")]
    [SerializeField] private GameObject tilemaps;
    [SerializeField] private Tilemap floorTilemap;
    [SerializeField] private Tilemap wallTilemap;
    [SerializeField] private Tilemap wallTopTilemap;
    [SerializeField] private Tilemap cliffTilemap;
    [SerializeField] private Tilemap shadowTilemap;
    [SerializeField] private Tilemap colliderTilemap;

    [Header("Tiles")]
    [SerializeField] private Tile wall_Top_Left;
    [SerializeField] private Tile wall_Top_Right;
    [SerializeField] private Tile wall_Top_Center;
    [SerializeField] private Tile wall_Bottom_Left;
    [SerializeField] private Tile wall_Bottom_Right;
    [SerializeField] private Tile wall_Bottom;
    [SerializeField] private Tile wall_Top;
    [SerializeField] private Tile wall_Right;
    [SerializeField] private Tile wall_Left;
    [SerializeField] private Tile wall_Center;
    [SerializeField] private Tile wall_Center_Center;
    [SerializeField] private Tile wall_Center_Right;
    [SerializeField] private Tile wall_Center_Left;
    [SerializeField] private Tile floor;
    [SerializeField] private Tile cliff_0;
    [SerializeField] private Tile cliff_1;
    [SerializeField] private Tile wall_T;


    [Header("Shadow Tiles")]
    [SerializeField] private Tile shadow_Right_Top;
    [SerializeField] private Tile shadow_Right;
    [SerializeField] private Tile shadow_Right_Bottom;

    #region bitmasks

    readonly int TopMask = (1 << 1) | (1 << 3) | (1 << 5);
    readonly int TopLeftMask_0 = (1 << 3) | (1 << 1) | (1 << 0);
    readonly int TopRightMask_0 = (1 << 1) | (1 << 2) | (1 << 5);

    readonly int TopMatch = 1 << 1;
    readonly int TopLeftMatch_0 = 1 << 0;
    readonly int TopRightMatch_0 = 1 << 2;

    readonly int BottomMask = (1 << 3) | (1 << 5) | (1 << 7);
    readonly int LeftMask = (1 << 1) | (1 << 3) | (1 << 7);
    readonly int RightMask = (1 << 1) | (1 << 5) | (1 << 7);

    readonly int BottomLeftMask_0 = (1 << 3) | (1 << 6) | (1 << 7);
    readonly int BottomRightMask_0 = (1 << 5) | (1 << 7) | (1 << 8);
    readonly int TopLeftMask_1 = (1 << 5) | (1 << 7);
    readonly int TopRightMask_1 = (1 << 3) | (1 << 7);
    readonly int BottomLeftMask_1 = (1 << 1) | (1 << 5);
    readonly int BottomRightMask_1 = (1 << 1) | (1 << 3);

    readonly int BottomMatch = 1 << 7;
    readonly int LeftMatch = 1 << 3;
    readonly int RightMatch = 1 << 5;

    readonly int BottomLeftMatch_0 = 1 << 6;
    readonly int BottomRightMatch_0 = 1 << 8;
    readonly int TopLeftMatch_1 = (1 << 5) | (1 << 7);
    readonly int TopRightMatch_1 = (1 << 3) | (1 << 7);
    readonly int BottomLeftMatch_1 = (1 << 1) | (1 << 5);
    readonly int BottomRightMatch_1 = (1 << 1) | (1 << 3);

    readonly int ExceptionMask = (1 << 1) | (1 << 3) | (1 << 5) | (1 << 7);
    readonly int ExceptionMask_0 = (1 << 1) | (1 << 5) | (1 << 7);
    readonly int ExceptionMask_1 = (1 << 1) | (1 << 3) | (1 << 7);
    readonly int ExceptionMask_2 = (1 << 1) | (1 << 3) | (1 << 5);
    readonly int ExceptionMask_3 = (1 << 3) | (1 << 5) | (1 << 7);

    readonly int ExceptionMatch = (1 << 1) | (1 << 3) | (1 << 5) | (1 << 7);
    readonly int ExceptionMatch_0 = (1 << 1) | (1 << 5) | (1 << 7);
    readonly int ExceptionMatch_1 = (1 << 1) | (1 << 3) | (1 << 7);
    readonly int ExceptionMatch_2 = (1 << 1) | (1 << 3) | (1 << 5);
    readonly int ExceptionMatch_3 = (1 << 3) | (1 << 5) | (1 << 7);

    readonly int ExceptionMask_T1 = (1 << 0) | (1 << 1) | (1 << 2) | (1 << 3) | (1 << 5) | (1 << 4) | (1 << 7);
    readonly int ExceptionMask_T2 = (1 << 0) | (1 << 1) | (1 << 2) | (1 << 3) | (1 << 5) | (1 << 4) | (1 << 7);
    readonly int ExceptionMask_T3 = (1 << 0) | (1 << 1) | (1 << 2) | (1 << 3) | (1 << 5) | (1 << 4);

    readonly int ExceptionMatch_T1 = (1 << 0) | (1 << 7);
    readonly int ExceptionMatch_T2 = (1 << 2) | (1 << 7);
    readonly int ExceptionMatch_T3 = (1 << 0) | (1 << 2);

    readonly int ShadowMask = (1 << 0) | (1 << 3) | (1 << 6);

    readonly int ShadowTopMatch = (1 << 0) | (1 << 3);
    readonly int ShadowMidMatch = (1 << 0) | (1 << 3) | (1 << 6);
    readonly int ShadowBottomMatch = (1 << 3) | (1 << 6);

    #endregion

    private int[,] map;

    int[] dirX = { 1, 0, -1, 1, 0, -1, 1, 0, -1 };
    int[] dirY = { -1, -1, -1, 0, 0, 0, 1, 1, 1 };

    public void TilingMap()
    {
        // Sequence : Floor-Wall-Exception-Cliff-Shadow
        
        // Process Floor/Wall
        for (int i = map.GetLength(0) - 1; i >= 0; i--)
        {
            for (int j = map.GetLength(1) - 1; j >= 0; j--)
            {
                if (map[i, j] == (int)GridType.None)
                {
                    PlaceWallTile(j, i, 2);
                }
                else
                {
                    PlaceWallTile(j, i, 1);
                }
            }

        }

        // Process Exception Tiles
        for (int i = map.GetLength(0) - 1; i >= 0; i--)
        {
            for (int j = map.GetLength(1) - 1; j >= 0; j--)
            {
                PlaceExceptionTiles(j, i);
            }
        }

        for (int i = map.GetLength(0) - 1; i >= 0; i--)
        {
            for (int j = map.GetLength(1) - 1; j >= 0; j--)
            {
                ReplaceWallTile(j, i);
            }
        }

        // Process Cliffs, Shadows by wall/floor tiles
        for (int i = 0; i < map.GetLength(0); i++)
        {
            for (int j = 0; j < map.GetLength(1); j++)
            {
                PlaceShadowTile(j, i);
                PlaceCliffTile(j, i);
            }
        }
    }

    /** Tile Placing Functions**/
    private void PlaceWallTile(int x, int y, int tileType)
    {
        Tile tile = null;
        Vector3Int tilePos = new Vector3Int(x, y, 0);

        switch (tileType)       // tileType : 1: Floor, 2: Wall
        {
            case 1:
                tile = floor;
                floorTilemap.SetTile(tilePos, floor);
                break;
            case 2:
                tile = DetermineWallTile(x, y);
                if (tile == null) break;

                if (tile == wall_Left || tile == wall_Right)
                {
                    wallTilemap.SetTile(tilePos, tile);
                }
                else if (tile == wall_Top_Left || tile == wall_Top_Right)
                {
                    wallTilemap.SetTile(tilePos + new Vector3Int(0, 1, 0), tile);
                    if (tile == wall_Top_Left) wallTilemap.SetTile(tilePos, wall_Left);
                    else wallTilemap.SetTile(tilePos, wall_Right);
                }
                else if (tile == wall_Top_Center)
                {
                    wallTilemap.SetTile(tilePos + new Vector3Int(0, 1, 0), tile);
                    wallTopTilemap.SetTile(tilePos, wall_Center_Center);
                }
                else
                {
                    wallTilemap.SetTile(tilePos + new Vector3Int(0, 1, 0), tile);

                    if (tile == wall_Bottom_Left || tile == wall_Bottom_Right || tile == wall_Bottom)
                    {
                        if (tile == wall_Bottom_Left)       wallTopTilemap.SetTile(tilePos, wall_Center_Left);
                        else if (tile == wall_Bottom_Right) wallTopTilemap.SetTile(tilePos, wall_Center_Right);
                        else                                wallTopTilemap.SetTile(tilePos, wall_Center);
                    }
                    else
                    {
                        wallTopTilemap.SetTile(tilePos, wall_Center);
                    }
                }
                break;
            default:
                break;
        }
    }
    private Tile DetermineWallTile(int x, int y)
    {
        int pattern = CalculatePattern(x, y);   // Get Pattern in bits

        if (Matches(pattern, ExceptionMask, ExceptionMatch)) return wall_Top_Center;
        if (Matches(pattern, ExceptionMask_0, ExceptionMatch_0)) return wall_Top_Center;
        if (Matches(pattern, ExceptionMask_1, ExceptionMatch_1)) return wall_Top_Center;
        if (Matches(pattern, ExceptionMask_2, ExceptionMatch_2)) return wall_Top_Center;

        if (Matches(pattern, ExceptionMask_3, ExceptionMatch_3))
        {
            wallTilemap.SetTile(new Vector3Int(x, y, 0) + new Vector3Int(0, 1, 0), wall_Right);
            wallTilemap.SetTile(new Vector3Int(x, y, 0), wall_Right);
            return null;
        }

        if (Matches(pattern, TopMask, TopMatch)) return wall_Top;
        if (Matches(pattern, BottomMask, BottomMatch)) return wall_Bottom;
        if (Matches(pattern, LeftMask, LeftMatch)) return wall_Left;
        if (Matches(pattern, RightMask, RightMatch)) return wall_Right;
        if (Matches(pattern, TopLeftMask_0, TopLeftMatch_0)) return wall_Top_Left;
        if (Matches(pattern, TopRightMask_0, TopRightMatch_0)) return wall_Top_Right;
        if (Matches(pattern, BottomLeftMask_0, BottomLeftMatch_0)) return wall_Bottom_Left;
        if (Matches(pattern, BottomRightMask_0, BottomRightMatch_0)) return wall_Bottom_Right;

        if (Matches(pattern, TopLeftMask_1, TopLeftMatch_1)) return wall_Top_Left;
        if (Matches(pattern, TopRightMask_1, TopRightMatch_1)) return wall_Top_Right;
        if (Matches(pattern, BottomLeftMask_1, BottomLeftMatch_1)) return wall_Bottom_Left;
        if (Matches(pattern, BottomRightMask_1, BottomRightMatch_1)) return wall_Bottom_Right;

        return null;
    }
    private void ReplaceWallTile(int x, int y)
    {
        Vector3Int tilePos = new Vector3Int(x, y, 0);

        if ((wallTilemap.GetTile(tilePos) == wall_Top ||
            wallTilemap.GetTile(tilePos) == wall_Bottom_Right ||
            wallTilemap.GetTile(tilePos) == wall_Bottom_Left ||
            wallTilemap.GetTile(tilePos) == wall_Top_Center) && map[y, x] == (int)GridType.None)
        {
            wallTopTilemap.SetTile(tilePos, wallTilemap.GetTile(tilePos));
            wallTilemap.SetTile(tilePos, null);
        }
    }
    private void PlaceExceptionTiles(int x, int y)
    {
        Vector3Int tilePos = new Vector3Int(x, y, 0);
        int pattern = CalculatePattern(x, y);

        if (Matches(pattern, ExceptionMask_T1, ExceptionMatch_T1) || Matches(pattern, ExceptionMask_T2, ExceptionMatch_T2) || Matches(pattern, ExceptionMask_T3, ExceptionMatch_T3))
        {
            wallTilemap.SetTile(tilePos + new Vector3Int(0, 1, 0), wall_T);
            wallTilemap.SetTile(tilePos, wall_Right);
        }
    }
    private void PlaceShadowTile(int x, int y)
    {
        Vector3Int pos = new Vector3Int(x, y, 0);
        if (wallTilemap.GetTile(pos) != null || wallTopTilemap.GetTile(pos) != null) return;

        int pattern = CalculateShadowPattern(x, y);
        Tile shTile = null;

        if (Matches(pattern, ShadowMask, ShadowMidMatch)) shTile = shadow_Right;
        else if (Matches(pattern, ShadowMask, ShadowTopMatch)) shTile = shadow_Right_Top;
        else if (Matches(pattern, ShadowMask, ShadowBottomMatch)) shTile = shadow_Right_Bottom;

        shadowTilemap.SetTile(pos, shTile);
    }
    private void PlaceCliffTile(int x, int y)
    {
        Vector3Int tilePos = new Vector3Int(x, y, 0);
        Tile wallTile = wallTopTilemap.GetTile<Tile>(tilePos);

        if (wallTile == wall_Center_Left || wallTile == wall_Center_Right || wallTile == wall_Center)
        {
            if ((wallTilemap.GetTile<Tile>(tilePos - new Vector3Int(0, 1, 0)) == null && wallTopTilemap.GetTile<Tile>(tilePos - new Vector3Int(0, 1, 0)) == null) && floorTilemap.GetTile<Tile>(tilePos - new Vector3Int(0, 1, 0)) == null)
                cliffTilemap.SetTile(tilePos - new Vector3Int(0, 1, 0), cliff_0);
            if ((wallTilemap.GetTile<Tile>(tilePos - new Vector3Int(0, 2, 0)) == null && wallTopTilemap.GetTile<Tile>(tilePos - new Vector3Int(0, 2, 0)) == null) && floorTilemap.GetTile<Tile>(tilePos - new Vector3Int(0, 2, 0)) == null)
                cliffTilemap.SetTile(tilePos - new Vector3Int(0, 2, 0), cliff_1);
        }
    }

    /** Basic Calculation Functions **/
    private bool Matches(int pattern, int mask, int match)
    {
        return (pattern & mask) == match;
    }
    int CalculatePattern(int x, int y)
    {
        int pattern = 0;
        int bitIndex = 0;

        for (int i = 0; i < dirX.Length; i++)
        {
            int checkX = x + dirX[i];
            int checkY = y + dirY[i];

            // 맵 범위 내에서 검사
            if (checkX >= 0 && checkX < map.GetLength(1) && checkY >= 0 && checkY < map.GetLength(0))
            {
                if (map[checkY, checkX] != (int)GridType.None)
                {
                    pattern |= (1 << bitIndex);
                }
            }

            bitIndex++;
        }

        return pattern;
    }
    int CalculateShadowPattern(int x, int y)
    {
        int pattern = 0;
        int bitIndex = 0;

        for (int i = 0; i < dirX.Length; i++)
        {
            int checkX = x + dirX[i];
            int checkY = y + dirY[i];

            if (checkX >= 0 && checkX < map.GetLength(1) && checkY >= 0 && checkY < map.GetLength(0))
            {
                if (wallTopTilemap.GetTile(new Vector3Int(checkX, checkY, 0)) != null || wallTilemap.GetTile(new Vector3Int(checkX, checkY, 0)) != null)
                {
                    pattern |= (1 << bitIndex);
                }
            }

            bitIndex++;
        }

        return pattern;
    }

    /** Setter Functions **/
    public void SetMapInfos(ref int[,] map)
    {
        this.map = map;
        tilemaps.transform.position = new Vector3(GetComponent<MapGenerator>().MinX, GetComponent<MapGenerator>().MinY, 0);
    }

    /** Clear **/
    public void Clear()
    {

    }
}
