using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

namespace Turn360To2D
{
    /// <summary>
    /// Converts an equirectangular 360-degree texture into calibrated flat-screen views.
    /// Each output pixel is traced from Vector3.zero through the configured physical screen,
    /// making the world origin the exact ideal viewing point.
    /// </summary>
    [ExecuteAlways]
    public sealed class EquirectangularScreenProjector : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private Texture sourceTexture;
        [SerializeField] private Shader projectionShader;

        [Header("Calibration")]
        [Tooltip("Height in metres of the ideal viewing point (human eye) above the world Y = 0 floor.")]
        [Min(0f)] [SerializeField] private float idealViewingHeight = 1.65f;

        [Tooltip("Perpendicular distance in metres from the ideal viewing point to the Front screen plane.")]
        [Min(0.001f)] [SerializeField] private float idealPointToFrontScreenDistance = 2.874f;

        [Tooltip("Equirectangular V convention. Unity samples the top of an imported image at V = 1; the supplied 006 test panorama therefore uses this enabled convention.")]
        [SerializeField] private bool sourceLatitudeIsFlipped = true;

        [Header("Outputs")]
        [SerializeField] private List<ProjectionScreen> screens = new List<ProjectionScreen>();
        [SerializeField] private RenderTextureFormat renderTextureFormat = RenderTextureFormat.ARGB32;
        [SerializeField] private FilterMode outputFilter = FilterMode.Bilinear;

        private Material projectionMaterial;

        public IReadOnlyList<ProjectionScreen> Screens => screens;
        public float IdealViewingHeight => idealViewingHeight;
        public float IdealPointToFrontScreenDistance => idealPointToFrontScreenDistance;

        public void SetIdealPointToFrontScreenDistance(float distance)
        {
            idealPointToFrontScreenDistance = Mathf.Max(0.001f, distance);
        }

        public void SetSourceLatitudeIsFlipped(bool isFlipped)
        {
            sourceLatitudeIsFlipped = isFlipped;
        }

        public void SetSourceTexture(Texture texture)
        {
            sourceTexture = texture;
            RenderOutputs();
        }

        private void Reset()
        {
            screens = CreateDefaultScreens();
        }

        private void OnEnable()
        {
            EnsureScreenSlots();
            EnsureMaterial();
            EnsureOutputs();
            // RenderTextures are transient. They must be rebound immediately after a domain reload
            // or entering Play mode, not only after the next output-size change.
            GetComponent<ScreenProjectionBuilder>()?.BuildEnabledScreens();
        }

        private void Start()
        {
            // Covers component-enable ordering when the builder becomes active after this component.
            GetComponent<ScreenProjectionBuilder>()?.BuildEnabledScreens();
        }

        private void Update()
        {
            RenderOutputs();
        }

        private void OnValidate()
        {
            EnsureScreenSlots();
            EnsureMaterial();
            EnsureOutputs();
        }

        private void OnDisable()
        {
            ReleaseOutputs();
            if (projectionMaterial != null)
            {
                DestroyImmediate(projectionMaterial);
                projectionMaterial = null;
            }
        }

        /// <summary>Returns the live output texture for a named physical screen.</summary>
        public RenderTexture GetOutput(ScreenDirection direction)
        {
            ProjectionScreen screen = screens.Find(item => item.direction == direction);
            return screen == null ? null : screen.output;
        }

        public void RenderOutputs()
        {
            if (!EnsureMaterial()) return;

            Texture input = GetInputTexture();
            if (input == null) return;

            bool outputsChanged = EnsureOutputs();
            // Entering Play mode recreates the non-serialized RenderTextures. Rebind them to
            // the physical Quad materials immediately, otherwise the Quads retain released textures.
            if (outputsChanged)
                GetComponent<ScreenProjectionBuilder>()?.BuildEnabledScreens();
            foreach (ProjectionScreen screen in screens)
            {
                if (!screen.enabled || screen.output == null) continue;

                projectionMaterial.SetVector("_ScreenCentre", screen.centre);
                projectionMaterial.SetVector("_ScreenRight", screen.Right * screen.width);
                projectionMaterial.SetVector("_ScreenUp", screen.Up * screen.height);
                projectionMaterial.SetVector("_IdealViewingPoint", new Vector3(0f, idealViewingHeight, 0f));
                projectionMaterial.SetFloat("_SourceLatitudeIsFlipped", sourceLatitudeIsFlipped ? 1f : 0f);
                Graphics.Blit(input, screen.output, projectionMaterial);
            }
        }

        private Texture GetInputTexture()
        {
            if (videoPlayer != null && videoPlayer.texture != null) return videoPlayer.texture;
            return sourceTexture;
        }

        private bool EnsureMaterial()
        {
            if (projectionShader == null)
                projectionShader = Shader.Find("Turn360To2D/EquirectangularScreenProjection");
            if (projectionShader == null) return false;

            if (projectionMaterial == null || projectionMaterial.shader != projectionShader)
            {
                if (projectionMaterial != null) DestroyImmediate(projectionMaterial);
                projectionMaterial = new Material(projectionShader) { hideFlags = HideFlags.HideAndDontSave };
            }
            return true;
        }

        private bool EnsureOutputs()
        {
            bool changed = false;
            foreach (ProjectionScreen screen in screens)
            {
                if (screen.output != null && screen.output.width == screen.outputWidth && screen.output.height == screen.outputHeight && screen.output.IsCreated())
                    continue;

                ReleaseOutput(screen);
                screen.output = new RenderTexture(screen.outputWidth, screen.outputHeight, 0, renderTextureFormat)
                {
                    name = $"360To2D_{screen.direction}",
                    filterMode = outputFilter,
                    wrapMode = TextureWrapMode.Clamp
                };
                screen.output.Create();
                changed = true;
            }
            return changed;
        }

        private void ReleaseOutputs()
        {
            foreach (ProjectionScreen screen in screens)
            {
                ReleaseOutput(screen);
            }
        }

        private static void ReleaseOutput(ProjectionScreen screen)
        {
            if (screen.output == null) return;
            screen.output.Release();
            if (Application.isPlaying) Destroy(screen.output);
            else DestroyImmediate(screen.output);
            screen.output = null;
        }

        private static List<ProjectionScreen> CreateDefaultScreens()
        {
            return new List<ProjectionScreen>
            {
                Create(ScreenDirection.Front, true,  7.371f, 3.5f,   new Vector3(0, 0, 3),  new Vector3(0, 0, 0)),
                Create(ScreenDirection.Back,  false, new Vector3(0, 0, -3), new Vector3(0, 180, 0)),
                Create(ScreenDirection.Left,  true,  4.251f, 3.5f,   new Vector3(-3, 0, 0), new Vector3(0, -90, 0)),
                Create(ScreenDirection.Right, true,  4.251f, 3.5f,   new Vector3(3, 0, 0),  new Vector3(0, 90, 0)),
                Create(ScreenDirection.Top,   true,  7.371f, 4.251f, new Vector3(0, 3, 0),  new Vector3(-90, 0, 0)),
                Create(ScreenDirection.Bottom,false, 7.371f, 4.251f, new Vector3(0, -3, 0), new Vector3(90, 0, 0))
            };
        }

        private void EnsureScreenSlots()
        {
            if (screens == null || screens.Count == 0)
            {
                screens = CreateDefaultScreens();
                return;
            }

            foreach (ProjectionScreen defaultScreen in CreateDefaultScreens())
            {
                if (!screens.Exists(item => item.direction == defaultScreen.direction))
                    screens.Add(defaultScreen);
            }
        }

        private static ProjectionScreen Create(ScreenDirection direction, bool enabled, Vector3 centre, Vector3 eulerAngles)
        {
            return new ProjectionScreen { direction = direction, enabled = enabled, centre = centre, eulerAngles = eulerAngles };
        }

        private static ProjectionScreen Create(ScreenDirection direction, bool enabled, float width, float height, Vector3 centre, Vector3 eulerAngles)
        {
            return new ProjectionScreen
            {
                direction = direction,
                enabled = enabled,
                width = width,
                height = height,
                centre = centre,
                eulerAngles = eulerAngles
            };
        }
    }
}
