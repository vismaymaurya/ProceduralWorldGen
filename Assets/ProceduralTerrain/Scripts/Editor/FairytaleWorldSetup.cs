using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ProceduralTerrain
{
    public static class FairytaleWorldSetup
    {
        private const string MaterialsPath = "Assets/ProceduralTerrain/Materials";

        [MenuItem("Fairytale World/Setup Complete Scene", false, 1)]
        public static void SetupWorldInCurrentScene()
        {
            EnsureMaterialsDirectory();
            Material terrainMat = EnsureMaterial("Mat_StylizedTerrain", "ProceduralTerrain/StylizedTerrain");
            Material waterMat = EnsureMaterial("Mat_StylizedWater", "ProceduralTerrain/StylizedWater");
            Material waterfallMat = EnsureMaterial("Mat_StylizedWaterfall", "ProceduralTerrain/StylizedWaterfall");

            // Setup Directional Light for fairytale warm diorama lighting
            Light sunLight = Object.FindFirstObjectByType<Light>();
            if (sunLight == null || sunLight.type != LightType.Directional)
            {
                GameObject sunObj = new GameObject("Directional Light");
                sunLight = sunObj.AddComponent<Light>();
                sunLight.type = LightType.Directional;
            }

            sunLight.transform.rotation = Quaternion.Euler(50f, -40f, 0f);
            sunLight.color = new Color(1.0f, 0.96f, 0.88f);
            sunLight.intensity = 1.35f;
            sunLight.shadows = LightShadows.Soft;

            // Setup Camera
            Camera cam = Camera.main;
            if (cam != null)
            {
                DioramaCameraController camCtrl = cam.GetComponent<DioramaCameraController>();
                if (camCtrl == null) camCtrl = cam.gameObject.AddComponent<DioramaCameraController>();
                camCtrl.focalPoint = new Vector3(0, 4, 0);
                camCtrl.distance = 68f;
                camCtrl.pitch = 45f;
                camCtrl.yaw = 45f;
                camCtrl.UpdateCameraTransformImmediate();

                UniversalAdditionalCameraData camData = cam.GetComponent<UniversalAdditionalCameraData>();
                if (camData != null)
                {
                    camData.requiresDepthTexture = true;
                }
            }

            // Setup Procedural World Manager
            ProceduralWorldManager manager = Object.FindFirstObjectByType<ProceduralWorldManager>();
            if (manager == null)
            {
                GameObject worldObj = new GameObject("ProceduralWorld");
                manager = worldObj.AddComponent<ProceduralWorldManager>();
            }

            manager.terrainMaterial = terrainMat;
            manager.waterMaterial = waterMat;
            manager.waterfallMaterial = waterfallMat;
            manager.EnsureControllers();

            // Setup Brush Tool and UI Controller
            TerrainBrushTool brushTool = manager.GetComponent<TerrainBrushTool>();
            if (brushTool == null) brushTool = manager.gameObject.AddComponent<TerrainBrushTool>();
            brushTool.worldManager = manager;
            brushTool.targetCamera = cam;

            TerrainUIController uiController = manager.GetComponent<TerrainUIController>();
            if (uiController == null) uiController = manager.gameObject.AddComponent<TerrainUIController>();
            uiController.worldManager = manager;
            uiController.brushTool = brushTool;

            // Generate initial island preview in editor
            manager.GenerateWorld();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("<color=#44FF88><b>[Fairytale World]</b> Scene configured successfully!</color>");
        }

        private static void EnsureMaterialsDirectory()
        {
            if (!AssetDatabase.IsValidFolder("Assets/ProceduralTerrain"))
            {
                AssetDatabase.CreateFolder("Assets", "ProceduralTerrain");
            }
            if (!AssetDatabase.IsValidFolder(MaterialsPath))
            {
                AssetDatabase.CreateFolder("Assets/ProceduralTerrain", "Materials");
            }
        }

        private static Material EnsureMaterial(string matName, string shaderName)
        {
            string path = $"{MaterialsPath}/{matName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find(shaderName);
                if (shader == null)
                {
                    shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                }
                mat = new Material(shader) { name = matName };
                AssetDatabase.CreateAsset(mat, path);
                AssetDatabase.SaveAssets();
            }
            return mat;
        }
    }
}
