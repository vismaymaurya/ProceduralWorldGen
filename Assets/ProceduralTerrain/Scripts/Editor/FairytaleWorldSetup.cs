using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ProceduralTerrain
{
    [InitializeOnLoad]
    public static class FairytaleWorldSetup
    {
        private const string MaterialsPath = "Assets/ProceduralTerrain/Materials";
        private const string SetupKey = "ProceduralWorld_HasAutoSetup_v1";

        static FairytaleWorldSetup()
        {
            EditorApplication.delayCall += CheckAutoSetup;
        }

        private static void CheckAutoSetup()
        {
            if (!SessionState.GetBool(SetupKey, false))
            {
                SessionState.SetBool(SetupKey, true);
                SetupWorldInCurrentScene();
            }
        }

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
            sunLight.color = new Color(1.0f, 0.96f, 0.88f); // Warm sun
            sunLight.intensity = 1.35f;
            sunLight.shadows = LightShadows.Soft;

            // Setup Camera
            Camera cam = Camera.main;
            if (cam != null)
            {
                DioramaCameraController camCtrl = cam.GetComponent<DioramaCameraController>();
                if (camCtrl == null) camCtrl = cam.gameObject.AddComponent<DioramaCameraController>();
                camCtrl.focalPoint = new Vector3(0, 5, 0);
                camCtrl.distance = 65f;
                camCtrl.pitch = 45f;
                camCtrl.yaw = 45f;
                camCtrl.UpdateCameraTransformImmediate();

                // Ensure URP Additional Camera Data enables depth texture for water foam
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

            // Generate initial island
            manager.GenerateWorld();

            // Mark scene dirty and save
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("<color=#44FF88><b>[Fairytale World]</b> Successfully generated procedural fairytale terrain scene!</color>");
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
