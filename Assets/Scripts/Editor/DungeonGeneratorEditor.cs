using UnityEngine;
using UnityEditor; // Cần cho Editor Scripting

[CustomEditor(typeof(DungeonGenerator))] // Chỉ định script này tùy chỉnh cho DungeonGenerator
public class DungeonGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Vẽ các biến public mặc định của script DungeonGenerator
        DrawDefaultInspector();

        // Lấy đối tượng DungeonGenerator đang được chọn
        DungeonGenerator generator = (DungeonGenerator)target;

        // Thêm một khoảng trống
        EditorGUILayout.Space();

        // Tạo nút bấm "Generate Dungeon"
        if (GUILayout.Button("Generate Dungeon"))
        {
            generator.GenerateDungeon(); // Gọi hàm sinh dungeon
        }
        if (GUILayout.Button("Clear Dungeon"))
        {
            generator.ClearPreviousDungeon(); // Gọi hàm xóa dungeon
        }

        
        
    }
}