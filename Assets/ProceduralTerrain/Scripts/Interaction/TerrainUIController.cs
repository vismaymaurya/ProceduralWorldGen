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

        private string seedInputString = "42";
        private string toastMessage = "";
        private float notificationTimer = 0f;

        private void Start()
        {
            if (worldManager == null) worldManager = FindFirstObjectByType<ProceduralWorldManager>();
            if (brushTool == null) brushTool = FindFirstObjectByType<TerrainBrushTool>();

            if (worldManager != null)
            {
                seedInputString = worldManager.settings.seed.ToString();
            }
        }

        private void Update()
        {
            if (notificationTimer > 0f)
            {
                notificationTimer -= Time.deltaTime;
            }

            // Toggle UI visibility with H key
            if (Input.GetKeyDown(KeyCode.H))
            {
                showUI = !showUI;
            }

            // Keyboard number shortcuts to switch brush
            if (brushTool != null)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) { brushTool.currentMode = TerrainBrushMode.RaiseMountain; ShowToast("Tool: Raise Mountain (1)"); }
                if (Input.GetKeyDown(KeyCode.Alpha2)) { brushTool.currentMode = TerrainBrushMode.LowerTerrain; ShowToast("Tool: Lower Terrain / Valley (2)"); }
                if (Input.GetKeyDown(KeyCode.Alpha3)) { brushTool.currentMode = TerrainBrushMode.LowerRiver; ShowToast("Tool: River / Stream (3)"); }
                if (Input.GetKeyDown(KeyCode.Alpha4)) { brushTool.currentMode = TerrainBrushMode.FlattenPlateau; ShowToast("Tool: Flatten Plateau (4)"); }
                if (Input.GetKeyDown(KeyCode.Alpha5)) { brushTool.currentMode = TerrainBrushMode.SmoothTerrain; ShowToast("Tool: Smooth Terrain (5)"); }
                if (Input.GetKeyDown(KeyCode.Alpha6)) { brushTool.currentMode = TerrainBrushMode.PaintFoliage; ShowToast("Tool: Plant Foliage (6)"); }
                if (Input.GetKeyDown(KeyCode.Alpha7)) { brushTool.currentMode = TerrainBrushMode.EraseFoliage; ShowToast("Tool: Erase Foliage (7)"); }

                // [ and ] keys for brush size
                if (Input.GetKeyDown(KeyCode.LeftBracket))
                {
                    brushTool.brushRadius = Mathf.Max(1.5f, brushTool.brushRadius - 1.0f);
                    ShowToast($"Brush Radius: {brushTool.brushRadius:F1}");
                }
                if (Input.GetKeyDown(KeyCode.RightBracket))
                {
                    brushTool.brushRadius = Mathf.Min(25.0f, brushTool.brushRadius + 1.0f);
                    ShowToast($"Brush Radius: {brushTool.brushRadius:F1}");
                }
            }

            // Space to randomize seed & regenerate
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (worldManager != null)
                {
                    worldManager.RandomizeSeed();
                    seedInputString = worldManager.settings.seed.ToString();
                    ShowToast($"🎲 New Island Generated! Seed: {worldManager.settings.seed}");
                }
            }
        }

        public void ShowToast(string message)
        {
            toastMessage = message;
            notificationTimer = 2.5f;
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
            DrawBrushSettingsBar();
            DrawBottomToolbar();
            DrawToast();

            if (showSettingsPanel)
            {
                DrawSettingsPanel();
            }
        }

        private void DrawToast()
        {
            if (notificationTimer <= 0f) return;
            float w = 360;
            float h = 44;
            float x = (Screen.width - w) * 0.5f;
            float y = 90;
            Rect rect = new Rect(x, y, w, h);
            GUI.Box(rect, "", panelStyle);

            GUILayout.BeginArea(rect);
            GUILayout.Space(8);
            GUILayout.Label(toastMessage, titleStyle);
            GUILayout.EndArea();
        }

        private void DrawTopHeader()
        {
            float w = Mathf.Min(480, Screen.width - 24);
            float h = 58;
            float x = (Screen.width - w) * 0.5f;
            Rect rect = new Rect(x, 12, w, h);
            GUI.Box(rect, "", panelStyle);

            GUILayout.BeginArea(rect);
            GUILayout.Space(6);
            GUILayout.Label("Fairytale Island Generator", titleStyle);
            GUILayout.Label("Right-Drag: Orbit | WASD: Pan | Scroll: Zoom | [ / ]: Brush Size | Space: New Island", sliderLabelStyle);
            GUILayout.EndArea();
        }

        private void DrawBrushSettingsBar()
        {
            if (brushTool == null) return;

            float w = Mathf.Min(560, Screen.width - 20);
            float h = 42;
            float x = (Screen.width - w) * 0.5f;
            float y = Screen.height - 74 - 14 - h - 6;

            Rect rect = new Rect(x, y, w, h);
            GUI.Box(rect, "", panelStyle);

            GUILayout.BeginArea(rect);
            GUILayout.BeginHorizontal();
            GUILayout.Space(4);

            string modeName = brushTool.currentMode switch
            {
                TerrainBrushMode.RaiseMountain => "Raise Mountain",
                TerrainBrushMode.LowerTerrain => "Lower Valley",
                TerrainBrushMode.LowerRiver => "River Stream",
                TerrainBrushMode.FlattenPlateau => "Plateau",
                TerrainBrushMode.SmoothTerrain => "Smooth",
                TerrainBrushMode.PaintFoliage => "Plant Foliage",
                TerrainBrushMode.EraseFoliage => "Erase Foliage",
                _ => brushTool.currentMode.ToString()
            };
            GUILayout.Label($"<b>Tool:</b> {modeName}", labelStyle, GUILayout.Width(130));

            GUILayout.Label($"Size: {brushTool.brushRadius:F1}", sliderLabelStyle, GUILayout.Width(62));
            brushTool.brushRadius = GUILayout.HorizontalSlider(brushTool.brushRadius, 1.5f, 20.0f, GUILayout.Width(130));

            GUILayout.Space(10);
            GUILayout.Label($"Power: {brushTool.brushStrength:F1}", sliderLabelStyle, GUILayout.Width(68));
            brushTool.brushStrength = GUILayout.HorizontalSlider(brushTool.brushStrength, 0.2f, 3.5f, GUILayout.Width(130));

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawBottomToolbar()
        {
            float screenW = Screen.width;
            float barW = Mathf.Min(840, screenW - 20);
            float barH = 74;
            float x = (screenW - barW) * 0.5f;
            float y = Screen.height - barH - 14;

            Rect rect = new Rect(x, y, barW, barH);
            GUI.Box(rect, "", panelStyle);

            GUILayout.BeginArea(rect);
            GUILayout.BeginHorizontal();
            GUILayout.Space(6);

            // Brush Tools
            DrawBrushButton("1 Raise", TerrainBrushMode.RaiseMountain, 76);
            DrawBrushButton("2 Lower", TerrainBrushMode.LowerTerrain, 76);
            DrawBrushButton("3 River", TerrainBrushMode.LowerRiver, 76);
            DrawBrushButton("4 Plateau", TerrainBrushMode.FlattenPlateau, 80);
            DrawBrushButton("5 Smooth", TerrainBrushMode.SmoothTerrain, 76);
            DrawBrushButton("6 Foliage", TerrainBrushMode.PaintFoliage, 76);
            DrawBrushButton("7 Erase", TerrainBrushMode.EraseFoliage, 70);

            GUILayout.Space(8);

            // Randomize Seed & Settings
            if (GUILayout.Button("🎲 New Island", buttonActiveStyle, GUILayout.Width(104), GUILayout.Height(48)))
            {
                if (worldManager != null)
                {
                    worldManager.RandomizeSeed();
                    seedInputString = worldManager.settings.seed.ToString();
                    ShowToast($"🎲 New Island Generated! Seed: {worldManager.settings.seed}");
                }
            }

            string toggleText = showSettingsPanel ? "⚙ Close" : "⚙ Settings";
            if (GUILayout.Button(toggleText, buttonNormalStyle, GUILayout.Width(74), GUILayout.Height(48)))
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

            float w = 330;
            float h = 570;
            float x = Screen.width - w - 20;
            float y = Mathf.Max(20, Screen.height - h - 130);

            Rect rect = new Rect(x, y, w, h);
            GUI.Box(rect, "", panelStyle);

            GUILayout.BeginArea(rect);
            GUILayout.Space(4);
            GUILayout.Label("Terrain & River Settings", titleStyle);
            GUILayout.Space(6);

            // 1. One-click Random Island generation
            if (GUILayout.Button("🎲 Generate Random Island", buttonActiveStyle, GUILayout.Height(34)))
            {
                worldManager.RandomizeSeed();
                seedInputString = worldManager.settings.seed.ToString();
                ShowToast($"🎲 Random Island Generated! Seed: {worldManager.settings.seed}");
            }

            GUILayout.Space(6);
            GUILayout.Label($"Island Seed: {settings.seed}", labelStyle);
            GUILayout.BeginHorizontal();
            seedInputString = GUILayout.TextField(seedInputString, GUILayout.Width(140), GUILayout.Height(26));
            if (GUILayout.Button("Apply", buttonNormalStyle, GUILayout.Width(62), GUILayout.Height(26)))
            {
                if (int.TryParse(seedInputString, out int parsed))
                {
                    settings.seed = parsed;
                    worldManager.GenerateWorld();
                    ShowToast($"Island Seed Set: {settings.seed}");
                }
            }
            if (GUILayout.Button("🎲 Roll", buttonNormalStyle, GUILayout.Width(58), GUILayout.Height(26)))
            {
                worldManager.RandomizeSeed();
                seedInputString = settings.seed.ToString();
                ShowToast($"New Seed Rolled: {settings.seed}");
            }
            GUILayout.EndHorizontal();

            if (worldManager.CustomRiverPaths.Count > 0)
            {
                GUILayout.Space(4);
                if (GUILayout.Button("🌊 Clear Custom Rivers", buttonNormalStyle, GUILayout.Height(24)))
                {
                    worldManager.ClearCustomRivers();
                    ShowToast("Custom Rivers Cleared");
                }
            }

            GUILayout.Space(6);
            GUILayout.Label($"Terrain Height: {settings.maxHeight:F0}m", sliderLabelStyle);
            float oldMaxHeight = settings.maxHeight;
            float newMaxHeight = GUILayout.HorizontalSlider(settings.maxHeight, 10f, 50f);
            if (Mathf.Abs(newMaxHeight - oldMaxHeight) > 0.15f)
            {
                settings.maxHeight = newMaxHeight;
                worldManager.ScaleTerrainHeight(oldMaxHeight, newMaxHeight);
            }

            GUILayout.Label($"River Depth: {settings.riverDepth:F2} (Shallow Channel)", sliderLabelStyle);
            settings.riverDepth = GUILayout.HorizontalSlider(settings.riverDepth, 0.1f, 1.8f);

            GUILayout.Label($"River Width: {settings.riverWidth:F1} units", sliderLabelStyle);
            settings.riverWidth = GUILayout.HorizontalSlider(settings.riverWidth, 1.5f, 6.0f);

            GUILayout.Label($"Water Level: {settings.waterLevel:F1}m", sliderLabelStyle);
            float oldWaterLevel = settings.waterLevel;
            float newWaterLevel = GUILayout.HorizontalSlider(settings.waterLevel, 1.5f, 7.0f);
            if (Mathf.Abs(newWaterLevel - oldWaterLevel) > 0.05f)
            {
                settings.waterLevel = newWaterLevel;
                worldManager.RebuildMesh();
                if (worldManager.FoliageController != null)
                {
                    worldManager.FoliageController.AdjustAllFoliageHeights(worldManager);
                }
                worldManager.ReconformRiversToTerrain();
            }

            GUILayout.Label($"River Count: {settings.riverCount}", sliderLabelStyle);
            settings.riverCount = Mathf.RoundToInt(GUILayout.HorizontalSlider(settings.riverCount, 0, 6));

            GUILayout.Label($"Island Radius: {settings.islandRadius:F2}", sliderLabelStyle);
            settings.islandRadius = GUILayout.HorizontalSlider(settings.islandRadius, 0.35f, 0.88f);

            GUILayout.Label($"Coastline Warping: {settings.coastlineWarpStrength:F1}", sliderLabelStyle);
            settings.coastlineWarpStrength = GUILayout.HorizontalSlider(settings.coastlineWarpStrength, 0f, 25f);

            bool newFlat = GUILayout.Toggle(settings.flatShaded, " Low-Poly Flat Shading");
            if (newFlat != settings.flatShaded)
            {
                settings.flatShaded = newFlat;
                worldManager.RebuildMesh();
            }

            GUILayout.Space(8);
            if (GUILayout.Button("🔄 Re-generate Island", buttonNormalStyle, GUILayout.Height(30)))
            {
                worldManager.GenerateWorld();
                ShowToast($"Island Re-generated! Seed: {settings.seed}");
            }

            GUILayout.EndArea();
        }
    }
}
