using UnityEngine;
using System.Collections.Generic; // Keep for internal use if needed

/// <summary>
/// Main orchestrator for procedural dungeon generation.
/// Holds parameters and coordinates calls to specialized generation classes.
/// </summary>
public class DungeonGenerator : MonoBehaviour
{
    [Header("Dungeon Parameters")] [Tooltip("Seed for random number generation. Same seed = same dungeon.")]
    public int seed = 0;

    [Tooltip("Total number of rooms to attempt placing (minimum 8 required by spec).")] [Min(8)]
    public int numberOfRooms = 10;

    [Tooltip("Maximum dimensions of the dungeon grid.")]
    public Vector2Int maxDungeonSize = new Vector2Int(100, 100);

    [Tooltip("Minimum width and height of a room.")]
    public Vector2Int minRoomSize = new Vector2Int(5, 5);

    [Tooltip("Maximum width and height of a room.")]
    public Vector2Int maxRoomSize = new Vector2Int(15, 15);

    [Tooltip("Width of the corridors in grid cells. NOTE: Width > 1 complicates logic.")] [Min(1)]
    public int corridorWidth = 1;

    [Tooltip("How many times to try placing each room before giving up.")]
    public int roomPlacementAttempts = 50;

    [Tooltip("Minimum empty space between rooms during placement.")]
    public int interRoomBuffer = 2;

    [Header("Prefabs")] [Tooltip("Prefab for room floors.")]
    public List<GameObject> roomFloorPrefabs = new List<GameObject>();

    [Tooltip("Prefab for walls.")] public List<GameObject> roomWallPrefabs = new List<GameObject>();

    [Tooltip("Optional: Prefab for corridor floors (falls back to Floor Prefab if null).")]
    public GameObject corridorFloorPrefab;

    [Tooltip("Prefab for doors.")] public GameObject doorPrefab;

    [Header("Visual Offsets")] [Tooltip("Offset vị trí bổ sung áp dụng cho prefab cửa khi được tạo.")]
    public Vector3 doorVisualOffset = Vector3.zero;

    [Header("Generation Control")] [Tooltip("Generate the dungeon automatically when the game starts.")]
    public bool generateOnStart = true;

    // --- Private Internal Variables ---
    private System.Random random;
    private Transform     dungeonHolder; // Parent object for generated tiles
    private DungeonData   currentDungeonData; // Holds the data for the current dungeon

    // --- Unity Methods ---
    void Start()
    {
        if (generateOnStart)
        {
            GenerateDungeon();
        }
    }

    // --- Public Methods ---

    /// <summary>
    /// Clears the previous dungeon and generates a new one based on current parameters.
    /// </summary>
    public void GenerateDungeon()
    {
        InitializeGeneration(); // Sets up random seed, clears old dungeon

        if (roomFloorPrefabs == null || roomFloorPrefabs.Count == 0)
        {
            Debug.LogError($"{gameObject.name}: Room Floor Prefabs list is empty! Assign at least one prefab.");

            return;
        }

        if (roomWallPrefabs == null || roomWallPrefabs.Count == 0)
        {
            Debug.LogError($"{gameObject.name}: Room Wall Prefabs list is empty! Assign at least one prefab.");

            return;
        }

        // 1. Place Rooms
        List<RectInt> placedRooms = RoomPlacer.PlaceRooms(
            numberOfRooms, maxDungeonSize, minRoomSize, maxRoomSize,
            roomPlacementAttempts, interRoomBuffer, random
        );

        if (placedRooms.Count < 2)
        {
            Debug.LogError($"{gameObject.name}: Not enough rooms placed ({placedRooms.Count}) to proceed with connection. Generation stopped.");

            return;
        }

        if (placedRooms.Count < 8)
        {
            Debug.LogWarning($"{gameObject.name}: Placed {placedRooms.Count} rooms, which is less than the recommended minimum of 8.");
        }

        // 2. Prepare DungeonData
        currentDungeonData = new DungeonData();
        currentDungeonData.Rooms.AddRange(placedRooms); // Add placed rooms to data
        currentDungeonData.CalculateRoomFloorPositions(); // Calculate initial floor positions

        // 3. Generate Corridors and Doors (populates data within currentDungeonData)
        CorridorGenerator.GenerateCorridorsAndDoors(currentDungeonData, corridorWidth, random);

        // 4. Calculate Wall Positions (based on generated floors/doors)
        currentDungeonData.WallPositions = DungeonVisualizer.CalculateWallPositions(currentDungeonData);

        // 5. Instantiate everything based on the final DungeonData
        DungeonVisualizer.InstantiateTiles(
            currentDungeonData,
            roomFloorPrefabs, roomWallPrefabs, corridorFloorPrefab, doorPrefab,
            dungeonHolder, doorVisualOffset // Pass the parent transform
        );

        Debug.Log($"{gameObject.name}: Dungeon generated with seed {seed}. Placed {currentDungeonData.Rooms.Count} rooms.");
    }

    /// <summary>
    /// Clears any previously generated dungeon objects. Made public for editor script access.
    /// </summary>
    public void ClearPreviousDungeon()
    {
        // Find existing holder robustly, works in editor and play mode
        while (transform.Find("Generated Dungeon") != null)
        {
            Transform existingHolder = transform.Find("Generated Dungeon");
            if (Application.isPlaying)
            {
                Destroy(existingHolder.gameObject);
            }
            else
            {
                DestroyImmediate(existingHolder.gameObject);
            }
        }

        dungeonHolder      = null; // Clear reference
        currentDungeonData = null; // Clear data
    }

    // --- Internal Helper Methods ---

    private void InitializeGeneration()
    {
        ClearPreviousDungeon(); // Clear first
        random = new System.Random(seed);

        // Create a parent object to keep the hierarchy clean
        dungeonHolder               = new GameObject("Generated Dungeon").transform;
        dungeonHolder.parent        = this.transform;
        dungeonHolder.localPosition = Vector3.zero;
    }
}