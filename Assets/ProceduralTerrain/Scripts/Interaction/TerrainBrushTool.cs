using UnityEngine;

namespace ProceduralTerrain
{
    public class TerrainBrushTool : MonoBehaviour
    {
        [Header("Brush Settings")]
        public TerrainBrushMode currentMode = TerrainBrushMode.RaiseMountain;
        [Range(1f, 25f)]
        public float brushRadius = 6.0f;
        [Range(0.2f, 5f)]
        public float brushStrength = 1.8f;

        [Header("References")]
        public ProceduralWorldManager worldManager;
        public Camera targetCamera;

        [Header("Visual Feedback")]
        public bool showBrushCursor = true;
        public Material cursorMaterial;

        private GameObject cursorObject;
        private LineRenderer cursorLine;
        private float foliagePlacementCooldown = 0f;

        private void Start()
        {
            if (worldManager == null)
            {
                worldManager = FindFirstObjectByType<ProceduralWorldManager>();
            }
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            SetupCursor();
        }

        private void Update()
        {
            if (worldManager == null || targetCamera == null) return;

            foliagePlacementCooldown -= Time.deltaTime;

            // Don't interact if Alt is held (used for camera orbit)
            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
            {
                if (cursorObject != null) cursorObject.SetActive(false);
                return;
            }

            Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 500f))
            {
                // Verify hit object belongs to terrain
                if (hit.collider.gameObject == worldManager.gameObject)
                {
                    UpdateCursor(hit.point, hit.normal);

                    // Left Click Interaction
                    if (Input.GetMouseButton(0))
                    {
                        if (currentMode == TerrainBrushMode.PaintFoliage)
                        {
                            if (foliagePlacementCooldown <= 0f)
                            {
                                worldManager.ApplyBrush(hit.point, brushRadius, brushStrength, currentMode);
                                foliagePlacementCooldown = 0.18f;
                            }
                        }
                        else
                        {
                            worldManager.ApplyBrush(hit.point, brushRadius, brushStrength, currentMode);
                        }
                    }

                    if (Input.GetMouseButtonUp(0))
                    {
                        if (currentMode == TerrainBrushMode.LowerRiver)
                        {
                            worldManager.FinalizeRiverStroke();
                        }
                        else
                        {
                            worldManager.BakeCollisionMesh();
                        }
                    }

                    // Keyboard shortcuts for brush radius ([ and ] or scroll with shift)
                    if (Input.GetKey(KeyCode.LeftShift))
                    {
                        float scroll = Input.GetAxis("Mouse ScrollWheel");
                        if (Mathf.Abs(scroll) > 0.01f)
                        {
                            brushRadius = Mathf.Clamp(brushRadius + Mathf.Sign(scroll) * 1.0f, 1.5f, 25f);
                        }
                    }

                    return;
                }
            }

            if (cursorObject != null)
            {
                cursorObject.SetActive(false);
            }
        }

        private void SetupCursor()
        {
            cursorObject = new GameObject("BrushCursor");
            cursorObject.transform.SetParent(transform, false);

            cursorLine = cursorObject.AddComponent<LineRenderer>();
            cursorLine.useWorldSpace = false;
            cursorLine.loop = true;
            cursorLine.positionCount = 48;
            cursorLine.startWidth = 0.22f;
            cursorLine.endWidth = 0.22f;

            if (cursorMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
                cursorMaterial = new Material(shader) { name = "Mat_BrushCursor" };
            }
            cursorLine.material = cursorMaterial;
            cursorObject.SetActive(false);
        }

        private void UpdateCursor(Vector3 position, Vector3 normal)
        {
            if (cursorObject == null) return;
            cursorObject.SetActive(showBrushCursor);

            cursorObject.transform.position = position + Vector3.up * 0.12f;
            cursorObject.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal);

            Color brushCol = GetBrushColor(currentMode);
            cursorLine.startColor = brushCol;
            cursorLine.endColor = brushCol;

            int points = cursorLine.positionCount;
            for (int i = 0; i < points; i++)
            {
                float angle = i * Mathf.PI * 2f / points;
                float x = Mathf.Cos(angle) * brushRadius;
                float z = Mathf.Sin(angle) * brushRadius;
                cursorLine.SetPosition(i, new Vector3(x, 0, z));
            }
        }

        private Color GetBrushColor(TerrainBrushMode mode)
        {
            switch (mode)
            {
                case TerrainBrushMode.RaiseMountain:
                    return new Color(1.0f, 0.65f, 0.2f, 0.85f); // Warm Orange
                case TerrainBrushMode.LowerRiver:
                    return new Color(0.2f, 0.75f, 1.0f, 0.85f); // Sky Blue
                case TerrainBrushMode.FlattenPlateau:
                    return new Color(0.95f, 0.85f, 0.3f, 0.85f); // Golden Yellow
                case TerrainBrushMode.SmoothTerrain:
                    return new Color(0.75f, 0.5f, 0.95f, 0.85f); // Lavender
                case TerrainBrushMode.PaintFoliage:
                    return new Color(0.3f, 0.95f, 0.4f, 0.85f); // Fairytale Green
                case TerrainBrushMode.EraseFoliage:
                    return new Color(0.95f, 0.3f, 0.3f, 0.85f); // Coral Red
                default:
                    return Color.white;
            }
        }
    }
}
