using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles the logic for placing rooms within the dungeon bounds.
/// </summary>
public static class RoomPlacer // Making it static as it doesn't need instance state
{
    /// <summary>
    /// Attempts to place a specified number of rooms without overlapping.
    /// </summary>
    /// <returns>A list of RectInt representing the placed rooms.</returns>
    public static List<RectInt> PlaceRooms(
        int numberOfRoomsToPlace,
        Vector2Int maxDungeonSize,
        Vector2Int minRoomSize,
        Vector2Int maxRoomSize,
        int placementAttemptsPerRoom,
        int interRoomBuffer,
        System.Random random)
    {
        List<RectInt> placedRooms = new List<RectInt>();
        int roomsPlacedSuccessfully = 0;

        for (int i = 0; i < numberOfRoomsToPlace; i++)
        {
            bool roomPlacedThisIteration = false;
            for (int attempt = 0; attempt < placementAttemptsPerRoom; attempt++)
            {
                int roomWidth = random.Next(minRoomSize.x, maxRoomSize.x + 1);
                int roomHeight = random.Next(minRoomSize.y, maxRoomSize.y + 1);

                int xPos = random.Next(0, maxDungeonSize.x - roomWidth);
                int yPos = random.Next(0, maxDungeonSize.y - roomHeight);

                RectInt newRoom = new RectInt(xPos, yPos, roomWidth, roomHeight);
                RectInt newRoomWithBuffer = new RectInt(
                    newRoom.x - interRoomBuffer,
                    newRoom.y - interRoomBuffer,
                    newRoom.width + interRoomBuffer * 2,
                    newRoom.height + interRoomBuffer * 2
                );

                if (!DoesOverlap(newRoomWithBuffer, placedRooms, interRoomBuffer))
                {
                    placedRooms.Add(newRoom);
                    roomPlacedThisIteration = true;
                    roomsPlacedSuccessfully++;
                    break;
                }
            }
            // Optional: Log if a room couldn't be placed after all attempts
            // if (!roomPlacedThisIteration) { Debug.LogWarning($"Failed to place room {i + 1}"); }
        }
        Debug.Log($"Attempted to place {numberOfRoomsToPlace} rooms, successfully placed {roomsPlacedSuccessfully}.");
        return placedRooms;
    }

    // Helper function moved inside RoomPlacer
    private static bool DoesOverlap(RectInt roomToCheck, List<RectInt> existingRooms, int buffer)
    {
        foreach (RectInt existingRoom in existingRooms)
        {
            // Check against the existing room *with its buffer*
             RectInt existingRoomWithBuffer = new RectInt(
                    existingRoom.x - buffer,
                    existingRoom.y - buffer,
                    existingRoom.width + buffer * 2,
                    existingRoom.height + buffer * 2
                );
            if (roomToCheck.Overlaps(existingRoomWithBuffer))
            {
                return true;
            }
        }
        return false;
    }
}