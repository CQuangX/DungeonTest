using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Handles instantiating prefabs based on the generated DungeonData.
/// Also calculates wall positions.
/// </summary>
public static class DungeonVisualizer // Static class
{
    /// <summary>
    /// Calculates wall positions based on all walkable tiles (floors and doors).
    /// </summary>
    public static HashSet<Vector2Int> CalculateWallPositions(DungeonData data)
    {
        HashSet<Vector2Int> wallPositions        = new HashSet<Vector2Int>();
        HashSet<Vector2Int> allWalkablePositions = new HashSet<Vector2Int>(data.RoomFloorPositions);
        allWalkablePositions.UnionWith(data.CorridorFloorPositions);
        allWalkablePositions.UnionWith(data.DoorPositions); // Doors are walkable areas

        Vector2Int[] directions =
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };

        foreach (Vector2Int floorPos in allWalkablePositions)
        {
            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighborPos = floorPos + dir;
                if (!allWalkablePositions.Contains(neighborPos))
                {
                    wallPositions.Add(neighborPos);
                }
            }
        }

        return wallPositions;
    }

    /// <summary>
    /// Instantiates all the necessary prefabs into the scene.
    /// </summary>
    public static void InstantiateTiles(
        DungeonData data,
        List<GameObject> roomFloorPrefab,
        List<GameObject> roomWallPrefab,
        GameObject corridorFloorPrefab, // Can be null
        GameObject doorPrefab,
        Transform parent,
        Vector3 doorOffset) // Parent transform for generated objects
    {
        // Use Corridor Prefab if available, otherwise default to Floor Prefab
        GameObject effectiveCorridorPrefab = corridorFloorPrefab != null ? corridorFloorPrefab : roomFloorPrefab[0];

        // 1. Instantiate Room Floors
        if (roomFloorPrefab != null && roomFloorPrefab.Count > 0)
        {
            foreach (Vector2Int pos in data.RoomFloorPositions)
            {
                // Tìm phòng chứa vị trí này
                RectInt currentRoom = data.Rooms.FirstOrDefault(r => r.Contains(pos));
                if (currentRoom.width > 0) // Đảm bảo tìm thấy phòng hợp lệ
                {
                    int roomIndex = data.Rooms.IndexOf(currentRoom);
                    // Chọn prefab dựa trên index phòng (tất định)
                    int        prefabIndex       = roomIndex % roomFloorPrefab.Count;
                    GameObject chosenFloorPrefab = roomFloorPrefab[prefabIndex];
                    InstantiatePrefab(chosenFloorPrefab, pos, Quaternion.identity, parent, Vector3.zero);
                }
                else
                {
                    // Trường hợp hiếm: không tìm thấy phòng chứa sàn -> dùng prefab đầu tiên
                    InstantiatePrefab(roomFloorPrefab[0], pos, Quaternion.identity, parent, Vector3.zero);
                    Debug.LogWarning($"Could not find room containing floor tile at {pos}. Using default floor.");
                }
            }
        }
        else
        {
            Debug.LogError("Room Floor Prefabs list is empty or null in Visualizer!");
        }

        // 2. Instantiate Corridor Floors
        InstantiateSet(data.CorridorFloorPositions, effectiveCorridorPrefab, parent, Quaternion.identity, Vector3.zero);

        // 3. Instantiate Doors (with rotation)
        if (doorPrefab != null && data.DoorPositions.Count > 0)
        {
            foreach (Vector2Int pos in data.DoorPositions)
            {
                Quaternion doorRotation = CalculateDoorRotation(pos, data.RoomFloorPositions);
                InstantiatePrefab(doorPrefab, pos, doorRotation, parent, doorOffset);
            }
        }
        else if (doorPrefab == null && data.DoorPositions.Count > 0)
        {
            Debug.LogWarning("Door positions generated, but Door Prefab is not assigned.");
            // Optional: Instantiate floor tiles at door positions if no door prefab
            // InstantiateSet(data.DoorPositions, effectiveCorridorPrefab, parent, Quaternion.identity);
        }

        // 4. Instantiate Walls
        if (roomWallPrefab != null && roomWallPrefab.Count > 0)
        {
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right }; // Chỉ cần ktra 4 hướng chính cho tường

            foreach (Vector2Int wallPos in data.WallPositions)
            {
                GameObject chosenWallPrefab = null;
                RectInt    adjacentRoom     = new RectInt(); // Để lưu phòng tìm thấy

                // Tìm xem tường này có tiếp giáp với phòng nào không
                foreach (var dir in directions)
                {
                    Vector2Int neighborPos = wallPos + dir;
                    if (data.RoomFloorPositions.Contains(neighborPos))
                    {
                        // Tìm phòng chứa sàn đó
                        adjacentRoom = data.Rooms.FirstOrDefault(r => r.Contains(neighborPos));

                        if (adjacentRoom.width > 0) break; // Tìm thấy phòng rồi thì dừng
                    }
                }

                if (adjacentRoom.width > 0) // Nếu tường có giáp phòng
                {
                    int roomIndex = data.Rooms.IndexOf(adjacentRoom);
                    // Chọn prefab tường dựa trên index phòng
                    int prefabIndex = roomIndex % roomWallPrefab.Count;
                    chosenWallPrefab = roomWallPrefab[prefabIndex];
                }
                else // Nếu tường không giáp phòng nào (vd: tường bao, tường giữa hành lang)
                {
                    // Dùng prefab tường mặc định (vd: cái đầu tiên)
                    chosenWallPrefab = roomWallPrefab[0];
                }

                // TODO: Tính toán rotation cho tường nếu cần
                InstantiatePrefab(chosenWallPrefab, wallPos, Quaternion.identity, parent, Vector3.zero);
            }
        }
        else
        {
            Debug.LogError("Room Wall Prefabs list is empty or null in Visualizer!");
        }
        // TODO: Implement wall rotation calculation if needed
    }

    // Helper to instantiate all items in a set
    private static void InstantiateSet(HashSet<Vector2Int> positions, GameObject prefab, Transform parent, Quaternion rotation, Vector3 offset)
    {
        if (prefab == null)
        {
            Debug.LogError($"Attempting to instantiate tiles with a null prefab for set containing {positions.Count} elements.");

            return;
        }

        foreach (Vector2Int pos in positions)
        {
            InstantiatePrefab(prefab, pos, rotation, parent, offset);
        }
    }

    // Helper to instantiate a single prefab
    private static void InstantiatePrefab(GameObject prefab, Vector2Int gridPosition, Quaternion rotation, Transform parent, Vector3 offset)
    {
        if (prefab == null) return;
        Vector3 worldPosition           = new Vector3(gridPosition.x, 0, gridPosition.y); // Grid Y maps to World Z
        Vector3 worldOffset             = rotation * offset;
        Vector3 worldPositionWithOffset = worldPosition + worldOffset;

        GameObject instance = Object.Instantiate(prefab, worldPositionWithOffset, rotation, parent); // Use Object.Instantiate
        instance.name = $"{prefab.name}_{gridPosition.x}_{gridPosition.y}";
    }

    // Helper to calculate door rotation (relative to the adjacent ROOM tile)
    private static Quaternion CalculateDoorRotation(Vector2Int doorPos, HashSet<Vector2Int> roomFloors)
    {
        Vector2Int roomNeighborDir = Vector2Int.zero;

        if (roomFloors.Contains(doorPos + Vector2Int.up)) roomNeighborDir         = Vector2Int.up;
        else if (roomFloors.Contains(doorPos + Vector2Int.down)) roomNeighborDir  = Vector2Int.down;
        else if (roomFloors.Contains(doorPos + Vector2Int.right)) roomNeighborDir = Vector2Int.right;
        else if (roomFloors.Contains(doorPos + Vector2Int.left)) roomNeighborDir  = Vector2Int.left;

        // Assumes Door prefab faces +Z (forward) by default
        if (roomNeighborDir == Vector2Int.up || roomNeighborDir == Vector2Int.down)
        {
            // Room is North/South (World Z+/-), door should align East/West (World X+/-)
            return Quaternion.identity;
        }
        else if (roomNeighborDir == Vector2Int.right || roomNeighborDir == Vector2Int.left)
        {
            // Room is East/West (World X+/-), door should align North/South (World Z+/-)
            return Quaternion.Euler(0, 90, 0);
        }

        Debug.LogWarning($"Could not determine room adjacency for door rotation at {doorPos}. Using default rotation.");

        return Quaternion.identity; // Fallback
    }
}