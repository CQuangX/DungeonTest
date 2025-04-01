using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds the generated data representing the dungeon layout.
/// </summary>
public class DungeonData
{
    public List<RectInt>       Rooms                  { get; private set; }
    public HashSet<Vector2Int> RoomFloorPositions     { get; private set; }
    public HashSet<Vector2Int> CorridorFloorPositions { get; private set; }
    public HashSet<Vector2Int> DoorPositions          { get; private set; }
    public HashSet<Vector2Int> WallPositions          { get; set; } // Wall positions are calculated later

    // Constructor to initialize collections
    public DungeonData()
    {
        Rooms                  = new List<RectInt>();
        RoomFloorPositions     = new HashSet<Vector2Int>();
        CorridorFloorPositions = new HashSet<Vector2Int>();
        DoorPositions          = new HashSet<Vector2Int>();
        WallPositions          = new HashSet<Vector2Int>(); // Initially empty
    }

    // Helper method to easily populate room floors from the room list
    public void CalculateRoomFloorPositions()
    {
        RoomFloorPositions.Clear();
        foreach (RectInt room in Rooms)
        {
            for (int x = room.xMin; x < room.xMax; x++)
            {
                for (int y = room.yMin; y < room.yMax; y++)
                {
                    RoomFloorPositions.Add(new Vector2Int(x, y));
                }
            }
        }
    }
}