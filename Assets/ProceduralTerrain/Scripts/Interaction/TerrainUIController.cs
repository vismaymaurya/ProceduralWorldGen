using UnityEngine;

namespace ProceduralTerrain
{
    public class TerrainUIController : MonoBehaviour
    {
        [Header("References")]
        public ProceduralWorldManager worldManager;
        public TerrainBrushTool brushTool;

        [Header("UI State")]
        public bool showUI = true;
        public bool showSettingsPanel = false;

        private GUIStyle panelStyle;
        private GUIStyle titleStyle;
        private GUIStyle buttonNormalStyle;
        private GUIStyle buttonActiveStyle;
        private GUIStyle labelStyle;
        private GUIStyle sliderLabelStyle;
        private bool stylesInitialized = false;

        private void Start()
        {
            if (worldManager == null) worldManager = FindFirstObjectByType<ProceduralWorldManager>();
            if (brushTool == null) brushTool = FindFirstObjectByType<TerrainBrushTool>();
        }

        private void Update()
        {
            // Toggle UI visibility with H key
            if (Input.GetKeyDown(KeyCode.H))
            {
                showUI = !showUI;
            }

            // Keyboard number shortcuts to switch brush
            if (brushTool != null)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) brushTool.currentMode = TerrainBrushMode.RaiseMountain;
                if (Input.GetKeyDown(KeyCode.Alpha2)) brushTool.currentMode = TerrainBrushMode.LowerRiver;
                if (Input.GetKeyDown(KeyCode.Alpha3)) brushTool.currentMode = TerrainBrushMode.FlattenPlateau;
                if (Input.GetKeyDown(KeyCode.Alpha4)) brushTool.currentMode = TerrainBrushMode.SmoothTerrain;
                if (Input.GetKeyDown(KeyCode.Alpha5)) brushTool.currentMode = TerrainBrushMode.PaintFoliage;
                if (Input.GetKeyDown(KeyCode.Alpha6)) brushTool.currentMode = TerrainBrushMode.EraseFoliage;
            }

            // Space to randomize seed & regenerate
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (worldManager != null) worldManager.RandomizeSeed();
            }
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;

            Texture2D panelTex = MakeSolidTex(new Color(0.12f, 0.14f, 0.18f, 0.88f));
            Texture2D btnNormalTex = MakeSolidTex(new Color(0.20f, 0.23f, 0.28f, 0.90f));
            Texture2D btnHoverTex = MakeSolidTex(new Color(0.28f, 0.32f, 0.38f, 0.95f));
            Texture2D btnActiveTex = MakeSolidTex(new Color(0.25f, 0.55f, 0.90f, 0.95f));

            panelStyle = new GUIStyle
            {
                normal = { background = panelTex },
                padding = new RectOffset(16, 16, 12, 12),
                border = new RectOffset(6, 6, 6, 6)
            };

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.95f, 0.95f, 0.95f) }
            };

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.85f, 0.88f, 0.92f) }
            };

            sliderLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = new Color(0.75f, 0.78f, 0.82f) }
            };

            buttonNormalStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { background = btnNormalTex, textColor = new Color(0.92f, 0.92f, 0.95f) },
                hover = { background = btnHoverTex, textColor = Color.white },
                active = { background = btnActiveTex, textColor = Color.white },
                alignment = TextAnchor.MiddleCenter
            };

            buttonActiveStyle = new GUIStyle(buttonNormalStyle)
            {
                normal = { background = btnActiveTex, textColor = Color.white },
                hover = { background = btnActiveTex, textColor = Color.white }
            };

            stylesInitialized = true;
        }

        private Texture2D MakeSolidTex(Color col)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, col);
            tex.Apply();
            return tex;
        }

        private void OnGUI()
        {
            if (!showUI) return;
            InitStyles();

            DrawTopHeader();
            DrawBottomToolbar();
            if (showSettingsPanel)
            {
                DrawSettingsPanel();
            }
        }

        private void DrawTopHeader()
        {
            float w = 340;
            float h = 60;
            Rect rect = new Rect(20, 20, w, h);
            GUI.Box(rect, "", panelStyle);

            GUILayout.BeginArea(rect);
            GUILayout.Space(6);
            GUILayout.Label("Fairytale Island Generator", titleStyle);
            GUILayout.Label("Right-Drag: Orbit | WASD: Pan | Scroll: Zoom | Space: New Island", sliderLabelStyle);
            GUILayout.EndArea();
        }

        private void DrawBottomToolbar()
        {
            float screenW = Screen.width;
            float barW = 720;
            float barH = 74;
            float x = (screenW - barW) * 0.5f;
            float y = Screen.height - barH - 18;

            Rect rect = new Rect(x, y, barW, barH);
            GUI.Box(rect, "", panelStyle);

            GUILayout.BeginArea(rect);
            GUILayout.BeginHorizontal();
            GUILayout.Space(8);

            // Brush Tools
            DrawBrushButton("1 Mountain", TerrainBrushMode.RaiseMountain, 92);
            DrawBrushButton("2 River", TerrainBrushMode.LowerRiver, 82);
            DrawBrushButton("3 Plateau", TerrainBrushMode.FlattenPlateau, 85);
            DrawBrushButton("4 Smooth", TerrainBrushMode.SmoothTerrain, 82);
            DrawBrushButton("5 Foliage", TerrainBrushMode.PaintFoliage, 82);
            DrawBrushButton("6 Erase", TerrainBrushMode.EraseFoliage, 75);

            GUILayout.Space(12);

            // Randomize Seed & Settings
            if (GUILayout.Button("🎲 New Island", buttonNormalStyle, GUILayout.Width(100), GUILayout.Height(48)))
            {
                if (worldManager != null) worldManager.RandomizeSeed();
            }

            string toggleText = showSettingsPanel ? "⚙ Close" : "⚙ Settings";
            if (GUILayout.Button(toggleText, buttonNormalStyle, GUILayout.Width(78), GUILayout.Height(48)))
            {
                showSettingsPanel = !showSettingsPanel;
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawBrushButton(string label, TerrainBrushMode mode, float width)
        {
            bool isActive = brushTool != null && brushTool.currentMode == mode;
            GUIStyle style = isActive ? buttonActiveStyle : buttonNormalStyle;

            if (GUILayout.Button(label, style, GUILayout.Width(width), GUILayout.Height(48)))
            {
                if (brushTool != null) brushTool.currentMode = mode;
            }
        }

        private void DrawSettingsPanel()
        {
            if (worldManager == null) return;
            var settings = worldManager.settings;

            float w = 310;
            float h = 420;
            float x = Screen.width - w - 20;
            float y = Screen.height - h - 100;

            Rect rect = new Rect(x, y, w, h);
            GUI.Box(rect, "", panelStyle);

            GUILayout.BeginArea(rect);
            GUILayout.Space(4);
            GUILayout.Label("Terrain Settings", titleStyle);
            GUILayout.Space(6);

            GUILayout.Label($"Seed: {settings.seed}", labelStyle);
            if (GUILayout.Button("Generate with Seed", buttonNormalStyle, GUILayout.Height(26)))
            {
                worldManager.GenerateWorld();
            }

            GUILayout.Space(8);
            GUILayout.Label($"Island Radius: {settings.islandRadius:F2}", sliderLabelStyle);
            settings.islandRadius = GUILayout.HorizontalSlider(settings.islandRadius, 0.35f, 0.88f);

            GUILayout.Label($"Mountain Height: {settings.maxHeight:F0}", sliderLabelStyle);
            settings.maxHeight = GUILayout.HorizontalSlider(settings.maxHeight, 12f, 45f);

            GUILayout.Label($"Water Level: {settings.waterLevel:F1}", sliderLabelStyle);
            settings.waterLevel = GUILayout.HorizontalSlider(settings.waterLevel, 1.5f, 7.0f);

            GUILayout.Label($"River Count: {settings.riverCount}", sliderLabelStyle);
            settings.riverCount = Mathf.RoundToInt(GUILayout.HorizontalSlider(settings.riverCount, 0, 6));

            GUILayout.Label($"Coastline Warping: {settings.coastlineWarpStrength:F1}", sliderLabelStyle);
            settings.coastlineWarpStrength = GUILayout.HorizontalSlider(settings.coastlineWarpStrength, 0f, 25f);

            bool newFlat = GUILayout.Toggle(settings.flatShaded, " Low-Poly Flat Shading");
            if (newFlat != settings.flatShaded)
            {
                settings.flatShaded = newFlat;
                worldManager.RebuildMesh();
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Re-generate Mesh", buttonNormalStyle, GUILayout.Height(30)))
            {
                worldManager.GenerateWorld();
            }

            GUILayout.EndArea();
        }
    }
}
