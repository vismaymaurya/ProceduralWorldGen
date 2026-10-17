using UnityEditor;
using UnityEngine;

namespace ProceduralTerrain
{
    [CustomEditor(typeof(ProceduralWorldManager))]
    public class ProceduralWorldEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            ProceduralWorldManager manager = (ProceduralWorldManager)target;

            DrawDefaultInspector();

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Quick Generation Actions", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🎲 Randomize Seed & Generate", GUILayout.Height(36)))
            {
                Undo.RecordObject(manager, "Randomize Island Seed");
                manager.RandomizeSeed();
                EditorUtility.SetDirty(manager);
            }

            if (GUILayout.Button("🔄 Re-Generate Island", GUILayout.Height(36)))
            {
                Undo.RecordObject(manager, "Generate Procedural Island");
                manager.GenerateWorld();
                EditorUtility.SetDirty(manager);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);
            if (GUILayout.Button("Rebuild Mesh Only (Instant)", GUILayout.Height(28)))
            {
                Undo.RecordObject(manager, "Rebuild Island Mesh");
                manager.RebuildMesh();
                EditorUtility.SetDirty(manager);
            }
        }
    }
}
