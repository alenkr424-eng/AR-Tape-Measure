using System.Collections;
using SmartARMeasure.Models;
using UnityEngine;

namespace SmartARMeasure.Visuals
{
    /// <summary>
    /// Interactive 3D AR marker sphere placed at user tap coordinates.
    /// Handles pop-in spawn animation, continuous pulse effect, shadow casting, and dynamic emission glow.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class MarkerObject : MonoBehaviour
    {
        [Header("Marker Design")]
        [SerializeField] private float baseRadius = 0.0075f; // 0.75 cm radius sphere in AR world space (reduced ~17%)
        [SerializeField] private float spawnAnimDuration = 0.35f;
        [SerializeField] private float pulseSpeed = 2.5f;
        [SerializeField] private float pulseScaleFactor = 0.15f;

        private MeshRenderer meshRenderer;
        private MaterialPropertyBlock mpb;
        private Vector3 targetBaseScale;
        private Coroutine spawnCoroutine;
        private bool isPulsing = true;
        private Color currentBaseColor = new Color(0f, 0.9f, 1f, 1f);

        private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorID = Shader.PropertyToID("_Color");

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            mpb = new MaterialPropertyBlock();

            // Configure shadow casting
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            meshRenderer.receiveShadows = true;

            // Generate procedural sphere mesh if missing
            MeshFilter filter = GetComponent<MeshFilter>();
            if (filter.sharedMesh == null)
            {
                filter.sharedMesh = CreateProceduralSphere(baseRadius);
            }

            // Create emission glowing material if standard material attached
            if (meshRenderer.sharedMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                Material mat = new Material(shader);
                mat.EnableKeyword("_EMISSION");
                meshRenderer.sharedMaterial = mat;
            }

            targetBaseScale = Vector3.one * (baseRadius * 2f);
            transform.localScale = Vector3.zero;
        }

        private void OnEnable()
        {
            if (AppSettings.Instance != null)
            {
                AppSettings.Instance.OnMarkerColorChanged += SetMarkerColor;
                SetMarkerColor(AppSettings.Instance.MarkerColor);
            }
        }

        private void OnDisable()
        {
            if (AppSettings.Instance != null)
            {
                AppSettings.Instance.OnMarkerColorChanged -= SetMarkerColor;
            }
        }

        private void Start()
        {
            PlaySpawnAnimation();
        }

        private void Update()
        {
            if (isPulsing)
            {
                AnimatePulse();
            }
        }

        /// <summary>
        /// Updates the marker base color and emission glow.
        /// </summary>
        public void SetMarkerColor(Color color)
        {
            currentBaseColor = color;
            meshRenderer.GetPropertyBlock(mpb);
            mpb.SetColor(BaseColorID, color);
            mpb.SetColor(ColorID, color);
            
            // Subtle emission glow
            Color emission = color * 0.8f;
            mpb.SetColor(EmissionColorID, emission);
            meshRenderer.SetPropertyBlock(mpb);
        }

        /// <summary>
        /// Triggers smooth pop-in spring/bounce scaling on spawn.
        /// </summary>
        public void PlaySpawnAnimation()
        {
            if (spawnCoroutine != null)
                StopCoroutine(spawnCoroutine);

            spawnCoroutine = StartCoroutine(SpawnRoutine());
        }

        private IEnumerator SpawnRoutine()
        {
            float elapsed = 0f;
            while (elapsed < spawnAnimDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / spawnAnimDuration;

                // Overshoot bounce curve: 1 + sin(p * pi) * overshoot
                float scaleMultiplier = Mathf.Sin(progress * Mathf.PI * 0.5f) + Mathf.Sin(progress * Mathf.PI) * 0.25f;
                transform.localScale = targetBaseScale * Mathf.Max(0f, scaleMultiplier);

                yield return null;
            }

            transform.localScale = targetBaseScale;
        }

        /// <summary>
        /// Continuous breathing/pulsing animation effect.
        /// </summary>
        private void AnimatePulse()
        {
            float wave = Mathf.Sin(Time.time * pulseSpeed);
            float currentPulse = 1.0f + (wave * pulseScaleFactor);
            transform.localScale = targetBaseScale * currentPulse;

            // Pulse emission intensity
            float emissionIntensity = 0.6f + (wave + 1.0f) * 0.3f;
            Color pulsedEmission = currentBaseColor * emissionIntensity;
            meshRenderer.GetPropertyBlock(mpb);
            mpb.SetColor(EmissionColorID, pulsedEmission);
            meshRenderer.SetPropertyBlock(mpb);
        }

        /// <summary>
        /// Creates a smooth procedural low-poly sphere mesh for AR markers.
        /// </summary>
        private Mesh CreateProceduralSphere(float radius, int subdivisions = 16)
        {
            Mesh mesh = new Mesh();
            mesh.name = "ARMarkerSphere";

            int lon = subdivisions;
            int lat = subdivisions;

            Vector3[] vertices = new Vector3[(lon + 1) * (lat + 1)];
            Vector2[] uvs = new Vector2[vertices.Length];
            int[] triangles = new int[lon * lat * 6];

            float pi = Mathf.PI;
            float _2pi = pi * 2f;

            for (int i = 0; i <= lat; i++)
            {
                float v = (float)i / lat;
                float latitude = (v - 0.5f) * pi;

                for (int j = 0; j <= lon; j++)
                {
                    float u = (float)j / lon;
                    float longitude = u * _2pi;

                    int index = i * (lon + 1) + j;

                    float x = Mathf.Cos(latitude) * Mathf.Cos(longitude);
                    float y = Mathf.Sin(latitude);
                    float z = Mathf.Cos(latitude) * Mathf.Sin(longitude);

                    vertices[index] = new Vector3(x, y, z) * radius;
                    uvs[index] = new Vector2(u, v);
                }
            }

            int triIndex = 0;
            for (int i = 0; i < lat; i++)
            {
                for (int j = 0; j < lon; j++)
                {
                    int current = i * (lon + 1) + j;
                    int next = current + lon + 1;

                    triangles[triIndex++] = current;
                    triangles[triIndex++] = next;
                    triangles[triIndex++] = current + 1;

                    triangles[triIndex++] = current + 1;
                    triangles[triIndex++] = next;
                    triangles[triIndex++] = next + 1;
                }
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}
