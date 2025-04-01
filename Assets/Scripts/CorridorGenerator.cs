using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Handles connecting rooms with corridors and identifying door locations.
/// </summary>
public static class CorridorGenerator // Static class
{
    /// <summary>
    /// Connects all rooms and identifies corridor/door positions.
    /// </summary>
    public static void GenerateCorridorsAndDoors(
        DungeonData data, // Pass DungeonData to populate directly
        int corridorWidth,
        System.Random random)
    {
        if (data.Rooms.Count < 2) return; // Cannot connect less than 2 rooms

        // --- Simple Connectivity Logic ---
        List<RectInt> connectedRooms   = new List<RectInt>();
        List<RectInt> unconnectedRooms = new List<RectInt>(data.Rooms);

        connectedRooms.Add(unconnectedRooms[0]);
        unconnectedRooms.RemoveAt(0);

        while (unconnectedRooms.Count > 0)
        {
            RectInt bestConnectedRoom   = new RectInt();
            RectInt bestUnconnectedRoom = new RectInt();
            float   minDistance         = float.MaxValue;

            foreach (var connectedRoom in connectedRooms)
            {
                foreach (var unconnectedRoom in unconnectedRooms)
                {
                    float dist = Vector2.Distance(connectedRoom.center, unconnectedRoom.center);
                    if (dist < minDistance)
                    {
                        minDistance         = dist;
                        bestConnectedRoom   = connectedRoom;
                        bestUnconnectedRoom = unconnectedRoom;
                    }
                }
            }

            if (bestUnconnectedRoom.width > 0)
            {
                CreateCorridor(data, bestConnectedRoom, bestUnconnectedRoom, corridorWidth, random); // Pass data
                connectedRooms.Add(bestUnconnectedRoom);
                unconnectedRooms.Remove(bestUnconnectedRoom);
            }
            else
            {
                Debug.LogError("CorridorGenerator: Could not find next room to connect.");

                break; // Avoid infinite loop
            }
        }

        // Optional: Add extra connections for loops (more complex)
    }

    private static void CreateCorridor(
        DungeonData data, // Takes DungeonData
        RectInt roomFrom,
        RectInt roomTo,
        int corridorWidth,
        System.Random random)
    {
        Vector2Int       startPos     = Vector2Int.RoundToInt(roomFrom.center);
        Vector2Int       endPos       = Vector2Int.RoundToInt(roomTo.center);
        List<Vector2Int> corridorPath = new List<Vector2Int>();

        Vector2Int currentPos = startPos;
        corridorPath.Add(currentPos);

        bool horizontalFirst = random.Next(0, 2) == 0;

        // Move Horizontal then Vertical (or vice-versa)
        if (horizontalFirst)
        {
            while (currentPos.x != endPos.x)
            {
                currentPos.x += (int)Mathf.Sign(endPos.x - currentPos.x);
                corridorPath.Add(currentPos);
            }

            while (currentPos.y != endPos.y)
            {
                currentPos.y += (int)Mathf.Sign(endPos.y - currentPos.y);
                corridorPath.Add(currentPos);
            }
        }
        else
        {
            while (currentPos.y != endPos.y)
            {
                currentPos.y += (int)Mathf.Sign(endPos.y - currentPos.y);
                corridorPath.Add(currentPos);
            }

            while (currentPos.x != endPos.x)
            {
                currentPos.x += (int)Mathf.Sign(endPos.x - currentPos.x);
                corridorPath.Add(currentPos);
            }
        }

        // --- Process Path for Doors and Corridor Floors ---
        Vector2Int[]        directions           = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        HashSet<Vector2Int> currentCorridorDoors = new HashSet<Vector2Int>(); // Doors for *this* corridor run

        for (int i = 0; i < corridorPath.Count; i++)
        {
            Vector2Int tile = corridorPath[i];

            // Skip tiles that are already part of a room floor
            if (data.RoomFloorPositions.Contains(tile)) continue;

            bool isDoor = false;
            // Check if neighbor is a room floor
            foreach (var dir in directions)
            {
                if (data.RoomFloorPositions.Contains(tile + dir))
                {
                    isDoor = true;

                    break;
                }
            }

            if (isDoor)
            {
                // Only add if it's not already marked as a door by another corridor
                if (!data.DoorPositions.Contains(tile))
                {
                    data.DoorPositions.Add(tile);
                    currentCorridorDoors.Add(tile); // Track doors added by this specific corridor
                }
            }
            else
            {
                // It's a corridor floor tile, only add if not already a door
                if (!data.DoorPositions.Contains(tile))
                {
                    data.CorridorFloorPositions.Add(tile);
                }
            }
        }

        // Ensure corridor tiles are not marked as doors (doors take precedence)
        data.CorridorFloorPositions.ExceptWith(currentCorridorDoors);

        // --- Handle Corridor Width > 1 (Simplified - Needs more robust logic for wide doors/walls) ---
        if (corridorWidth > 1)
        {
            HashSet<Vector2Int> expandedCorridor = new HashSet<Vector2Int>(data.CorridorFloorPositions);
            int                 halfWidth        = corridorWidth / 2;
            foreach (Vector2Int pathTile in new List<Vector2Int>(data.CorridorFloorPositions))
            {
                for (int w = 1; w <= halfWidth; w++)
                {
                    // Simple expansion - doesn't handle corners or ensure proper door width well
                    expandedCorridor.Add(pathTile + Vector2Int.up * w);
                    expandedCorridor.Add(pathTile + Vector2Int.down * w);
                    expandedCorridor.Add(pathTile + Vector2Int.left * w);
                    expandedCorridor.Add(pathTile + Vector2Int.right * w);
                }
            }

            expandedCorridor.ExceptWith(data.RoomFloorPositions); // Don't expand into rooms
            expandedCorridor.ExceptWith(data.DoorPositions); // Don't expand into doors
            data.CorridorFloorPositions.UnionWith(expandedCorridor); // Add the expanded tiles
        }
    }
}