using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

namespace Turn360To2D
{
    /// <summary>Creates six independent 2D outputs from a 360 equirectangular source and physical cube geometry.</summary>
    [ExecuteAlways]
    public sealed class SixFaceProjectionOutput : MonoBehaviour
    {
        public enum InputKind { StillImage, Video, ImageSequence }

        [Header("360 Input")]
        [SerializeField] private InputKind inputKind;
        [SerializeField] private Texture stillImage;
        [SerializeField] private VideoPlayer videoPlayer;
        [Tooltip("Editor-only source folder used to rebuild the serialized image-sequence index.")]
        [SerializeField] private string imageSequenceFolder;
        [SerializeField] private List<Texture2D> imageSequence = new List<Texture2D>();
        [Min(0.01f)] [SerializeField] private float sequenceFramesPerSecond = 25f;
        [SerializeField] private bool loopSequence = true;

        [Header("Physical Cube (metres)")]
        [Min(0.001f)] [SerializeField] private float cubeWidth = 7.371f;
        [Min(0.001f)] [SerializeField] private float cubeDepth = 4.251f;
        [Min(0.001f)] [SerializeField] private float cubeHeight = 3.5f;
        [Min(0f)] [SerializeField] private float viewingHeight = 1.65f;
        [Min(0.001f)] [SerializeField] private float eyeToFrontWall = 2.874f;

        [Header("Live Projection Viewer")]
        [Tooltip("The camera whose current position is used as the live projection centre. Leave empty to use Main Camera.")]
        [SerializeField] private Camera viewingCamera;

        [Header("2D Outputs")]
        [Tooltip("Reference pixel height. Each face automatically calculates its width from its physical screen aspect ratio.")]
        [Min(1)] [SerializeField] private int outputHeight = 1080;
        [SerializeField] private RenderTextureFormat outputFormat = RenderTextureFormat.ARGB32;
        [SerializeField] private Shader blitShader;

        private readonly Dictionary<ScreenDirection, RenderTexture> outputs = new Dictionary<ScreenDirection, RenderTexture>();
        private Material blitMaterial;
        private readonly Dictionary<ScreenDirection, Material> displayMaterials = new Dictionary<ScreenDirection, Material>();
        private int forcedSequenceFrame = -1;

        public float CubeWidth { get => cubeWidth; set { cubeWidth = Mathf.Max(0.001f, value); ApplyCubeLayout(); } }
        public float CubeDepth { get => cubeDepth; set { cubeDepth = Mathf.Max(0.001f, value); ApplyCubeLayout(); } }
        public float CubeHeight { get => cubeHeight; set { cubeHeight = Mathf.Max(0.001f, value); ApplyCubeLayout(); } }
        public float ViewingHeight { get => viewingHeight; set { viewingHeight = Mathf.Max(0f, value); ApplyCubeLayout(); } }
        public float EyeToFrontWall { get => eyeToFrontWall; set { eyeToFrontWall = Mathf.Max(0.001f, value); ApplyCubeLayout(); } }
        public IReadOnlyDictionary<ScreenDirection, RenderTexture> Outputs => outputs;
        public int SequenceFrameCount => imageSequence.Count;
        public string ImageSequenceFolder => imageSequenceFolder;

        /// <summary>Moves the configured viewing camera to the optional calibrated start point.</summary>
        public void MoveViewingCameraToCalibratedStart() => PlaceViewerAtCalibratedStartingPoint();

        private void OnEnable()
        {
            ApplyCubeLayout();
            RenderAllFaces();
        }

        private void OnValidate()
        {
            outputHeight = Mathf.Max(1, outputHeight);
            ApplyCubeLayout();
            RenderAllFaces();
        }

        private void LateUpdate()
        {
            // This is a live perspective projection, not a fixed pre-baked
            // cube map. A moved viewer must get a newly projected six-face
            // image so the walls match the 360 dome from that same position.
            RenderAllFaces();
        }

        private void OnDisable()
        {
            foreach (RenderTexture output in outputs.Values)
                if (output != null)
                {
                    if (RenderTexture.active == output) RenderTexture.active = null;
                    output.Release();
                    DestroyUnityObject(output);
                }
            outputs.Clear();
            DestroyUnityObject(blitMaterial);
            foreach (Material material in displayMaterials.Values) DestroyUnityObject(material);
            displayMaterials.Clear();
        }

        // Single source of truth for the physical viewer / projection origin.
        private Vector3 CalibratedViewingPoint => new Vector3(0f, viewingHeight, 0f);

        private Camera ActiveViewingCamera => viewingCamera != null ? viewingCamera : Camera.main;

        private void PlaceViewerAtCalibratedStartingPoint()
        {
            Camera camera = ActiveViewingCamera;
            if (camera != null)
                camera.transform.position = CalibratedViewingPoint;
        }

        private Vector3 CurrentViewingPoint
        {
            get
            {
                Camera camera = ActiveViewingCamera;
                return camera != null ? camera.transform.position : CalibratedViewingPoint;
            }
        }

        public RenderTexture GetOutput(ScreenDirection direction)
        {
            outputs.TryGetValue(direction, out RenderTexture output);
            return output;
        }

        public void SetSequenceFrameForExport(int frameIndex)
        {
            forcedSequenceFrame = frameIndex;
            RenderAllFaces();
        }

        public void ClearSequenceFrameOverride() => forcedSequenceFrame = -1;

        /// <summary>Stores an already filename-sorted frame index. Editor tools create this from a folder.</summary>
        public void SetImageSequence(string folderPath, List<Texture2D> frames)
        {
            imageSequenceFolder = folderPath;
            imageSequence = frames ?? new List<Texture2D>();
            forcedSequenceFrame = -1;
            RenderAllFaces();
        }

        public void ApplyCubeLayout()
        {
            Transform cube = transform;
            // Do not move the viewing camera here.  The active camera is the
            // observation point and may have been positioned manually before
            // export; every generated face must use that exact current point.

            const float thickness = 0.02f;
            float frontCentreZ = eyeToFrontWall + thickness * 0.5f;
            float backCentreZ = eyeToFrontWall - cubeDepth - thickness * 0.5f;
            float middleZ = eyeToFrontWall - cubeDepth * 0.5f;
            SetFace(ScreenDirection.Front, new Vector3(0f, cubeHeight * 0.5f, frontCentreZ), new Vector3(cubeWidth, cubeHeight, thickness));
            SetFace(ScreenDirection.Back, new Vector3(0f, cubeHeight * 0.5f, backCentreZ), new Vector3(cubeWidth, cubeHeight, thickness));
            SetFace(ScreenDirection.Left, new Vector3(-cubeWidth * 0.5f - thickness * 0.5f, cubeHeight * 0.5f, middleZ), new Vector3(thickness, cubeHeight, cubeDepth));
            SetFace(ScreenDirection.Right, new Vector3(cubeWidth * 0.5f + thickness * 0.5f, cubeHeight * 0.5f, middleZ), new Vector3(thickness, cubeHeight, cubeDepth));
            SetFace(ScreenDirection.Top, new Vector3(0f, cubeHeight + thickness * 0.5f, middleZ), new Vector3(cubeWidth, thickness, cubeDepth));
            SetFace(ScreenDirection.Bottom, new Vector3(0f, -thickness * 0.5f, middleZ), new Vector3(cubeWidth, thickness, cubeDepth));

            void SetFace(ScreenDirection direction, Vector3 position, Vector3 scale)
            {
                Transform face = FindFace(direction);
                if (face == null) return;
                face.localPosition = position;
                face.localScale = scale;
            }

            // Property setters and editor controls can call this method before
            // ExecuteAlways receives its next LateUpdate. Render now so every
            // physical screen updates as one atomic layout change.
            RenderAllFaces();
        }

        public void RenderAllFaces()
        {
            Texture source = GetSource();
            if (source == null || !EnsureMaterials()) return;
            // Same ray field as Inside 360 Dome: direction from the current
            // viewing camera through every point of the physical screen.
            Vector3 viewingPoint = CurrentViewingPoint;

            foreach (ScreenDirection direction in Enum.GetValues(typeof(ScreenDirection)))
            {
                Transform face = FindFace(direction);
                if (face == null) continue;
                RenderTexture output = EnsureOutput(direction);
                // Always sample from the actual inner plane of the physical
                // screen. This guarantees that a Front/Left or Front/Right
                // shared mesh edge is also the exact same sampled world point.
                Vector3 planeCentre = GetInteriorPlaneCentre(direction, face);
                // Front/Back run horizontally along X. Left/Right run horizontally along Z.
                // Using Transform.right for unrotated side-wall Cubes incorrectly used X and
                // collapsed their actual width into the wall thickness.
                Vector3 right = direction == ScreenDirection.Left
                    ? Vector3.forward * cubeDepth
                    : direction == ScreenDirection.Right
                        ? -Vector3.forward * cubeDepth
                        : Vector3.right * cubeWidth;
                Vector3 up = direction == ScreenDirection.Top
                    ? -Vector3.forward * cubeDepth
                    : direction == ScreenDirection.Bottom
                        ? Vector3.forward * cubeDepth
                        : (direction == ScreenDirection.Left || direction == ScreenDirection.Right ? -Vector3.up : Vector3.up) * cubeHeight;
                if (direction == ScreenDirection.Top || direction == ScreenDirection.Bottom) right = Vector3.right * cubeWidth;

                blitMaterial.SetVector("_ScreenCentre", planeCentre);
                blitMaterial.SetVector("_ScreenRight", right);
                blitMaterial.SetVector("_ScreenUp", up);
                blitMaterial.SetVector("_ViewingPoint", viewingPoint);
                Graphics.Blit(source, output, blitMaterial);

                MeshRenderer renderer = face.GetComponent<MeshRenderer>();
                // The scene preview must use the same world-space ray as the
                // dome.  Do not display the blitted RenderTexture here: its
                // mesh UV orientation differs per cube wall and can introduce
                // false seams while the observer moves.
                if (renderer != null) renderer.sharedMaterial = GetFaceMaterial(direction, source);
            }
        }

        private Texture GetSource()
        {
            if (inputKind == InputKind.Video) return videoPlayer != null ? videoPlayer.texture : null;
            if (inputKind == InputKind.ImageSequence)
            {
                if (imageSequence.Count == 0) return null;
                int index = forcedSequenceFrame >= 0 ? forcedSequenceFrame : Mathf.FloorToInt((Application.isPlaying ? Time.time : Time.realtimeSinceStartup) * sequenceFramesPerSecond);
                index = loopSequence ? Mathf.Abs(index % imageSequence.Count) : Mathf.Clamp(index, 0, imageSequence.Count - 1);
                return imageSequence[index];
            }
            return stillImage;
        }

        private bool EnsureMaterials()
        {
            if (blitShader == null) blitShader = Shader.Find("Turn360To2D/Six Face Equirectangular Blit");
            if (blitShader == null) return false;
            if (blitMaterial == null) blitMaterial = new Material(blitShader) { hideFlags = HideFlags.HideAndDontSave };
            return true;
        }

        private RenderTexture EnsureOutput(ScreenDirection direction)
        {
            GetOutputDimensions(direction, out int width, out int height);
            if (outputs.TryGetValue(direction, out RenderTexture output) && output != null && output.width == width && output.height == height) return output;
            if (output != null)
            {
                if (RenderTexture.active == output) RenderTexture.active = null;
                output.Release();
                DestroyUnityObject(output);
            }
            output = new RenderTexture(width, height, 0, outputFormat) { name = $"{direction}_2D_Output", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            output.Create();
            outputs[direction] = output;
            return output;
        }

        private void GetOutputDimensions(ScreenDirection direction, out int width, out int height)
        {
            float physicalWidth = direction == ScreenDirection.Left || direction == ScreenDirection.Right ? cubeDepth : cubeWidth;
            float physicalHeight = direction == ScreenDirection.Top || direction == ScreenDirection.Bottom ? cubeDepth : cubeHeight;
            height = Mathf.Max(1, outputHeight);
            width = Mathf.Max(1, Mathf.RoundToInt(height * physicalWidth / physicalHeight));

            // H.264/MP4 encoders require even dimensions.  The tiny one-pixel
            // padding is preferable to dropping Top or Bottom from a six-way
            // video recording when a physical aspect ratio rounds to an odd size.
            if ((width & 1) != 0) width++;
            if ((height & 1) != 0) height++;
        }

        private Material GetFaceMaterial(ScreenDirection direction, Texture panoramaSource)
        {
            if (displayMaterials.TryGetValue(direction, out Material existing) && existing != null)
            {
                existing.mainTexture = panoramaSource;
                return existing;
            }
            Shader shader = Shader.Find("Turn360To2D/Live Cube 360 Dome Match");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");
            Material material = new Material(shader) { name = $"{direction} Live 360 Dome Preview", hideFlags = HideFlags.HideAndDontSave };
            material.mainTexture = panoramaSource;
            displayMaterials[direction] = material;
            return material;
        }

        private Transform FindFace(ScreenDirection direction)
        {
            string prefix = direction.ToString();
            foreach (Transform child in transform)
                if (child.name.StartsWith(prefix, StringComparison.Ordinal)) return child;
            return null;
        }

        private static Vector3 GetInteriorPlaneCentre(ScreenDirection direction, Transform face)
        {
            float halfThickness = direction == ScreenDirection.Left || direction == ScreenDirection.Right
                ? face.lossyScale.x * 0.5f
                : direction == ScreenDirection.Top || direction == ScreenDirection.Bottom
                    ? face.lossyScale.y * 0.5f
                    : face.lossyScale.z * 0.5f;
            switch (direction)
            {
                case ScreenDirection.Front: return face.position - face.forward * halfThickness;
                case ScreenDirection.Back: return face.position + face.forward * halfThickness;
                case ScreenDirection.Left: return face.position + face.right * halfThickness;
                case ScreenDirection.Right: return face.position - face.right * halfThickness;
                case ScreenDirection.Top: return face.position - face.up * halfThickness;
                default: return face.position + face.up * halfThickness;
            }
        }

        private static void DestroyUnityObject(UnityEngine.Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(value);
            else UnityEngine.Object.DestroyImmediate(value);
        }
    }
}
