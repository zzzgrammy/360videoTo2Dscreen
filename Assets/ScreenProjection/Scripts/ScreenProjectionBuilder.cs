using System.Collections.Generic;
using UnityEngine;

namespace Turn360To2D
{
    /// <summary>Builds editable physical screen planes around the origin.</summary>
    [ExecuteAlways]
    public sealed class ScreenProjectionBuilder : MonoBehaviour
    {
        [SerializeField] private EquirectangularScreenProjector projector;
        [SerializeField] private Material screenMaterial;
        [SerializeField] private bool showScreensInScene = true;

        private readonly Dictionary<ScreenDirection, GameObject> screenObjects = new Dictionary<ScreenDirection, GameObject>();

        public void BuildEnabledScreens()
        {
            if (projector == null) projector = GetComponent<EquirectangularScreenProjector>();
            if (projector == null) return;

            foreach (ProjectionScreen screen in projector.Screens)
            {
                if (!screen.enabled) continue;
                GameObject screenObject = GetOrCreate(screen.direction);
                screenObject.transform.SetPositionAndRotation(screen.centre, screen.Rotation);
                screenObject.transform.localScale = new Vector3(screen.width, screen.height, 1f);
                screenObject.SetActive(showScreensInScene);

                MeshRenderer renderer = screenObject.GetComponent<MeshRenderer>();
                Material material = GetScreenMaterial(screen.direction);
                material.mainTexture = screen.output;
                renderer.sharedMaterial = material;
            }
        }

        /// <summary>
        /// Places Front, Top, Left and Right on shared edges around the origin.
        /// Front width/height define the opening; the side widths define depth.
        /// Any intentionally mismatched dimensions remain visible as overhang/gap for manual calibration.
        /// </summary>
        public void ArrangeAsOpenCube()
        {
            if (projector == null) projector = GetComponent<EquirectangularScreenProjector>();
            if (projector == null) return;

            ProjectionScreen front = Find(ScreenDirection.Front);
            ProjectionScreen top = Find(ScreenDirection.Top);
            ProjectionScreen left = Find(ScreenDirection.Left);
            ProjectionScreen right = Find(ScreenDirection.Right);
            if (front == null || top == null || left == null || right == null) return;

            // The average permits asymmetric left/right physical depths without shifting the front screen.
            float depth = Mathf.Max(0.001f, (left.width + right.width) * 0.5f);
            // The calibrated eye point is at X/Z = 0. The Front plane therefore sits exactly this far ahead.
            float frontDistance = projector.IdealPointToFrontScreenDistance;
            // A newly created open cube is floor-aligned: the vertical panels have their lower edges at Y = 0.
            front.centre = new Vector3(0f, front.height * 0.5f, frontDistance);
            front.eulerAngles = Vector3.zero;

            // With -90° X rotation, the Top screen's local -Y edge is its front edge (+Z).
            // Offset the centre backward by half its depth so that edge shares the Front top edge.
            top.centre = new Vector3(0f, front.height, frontDistance - top.height * 0.5f);
            // Rotate the ceiling panel's output axes 180° around its normal. This preserves the
            // same physical top plane and spherical sampling, while matching the panel's mounting orientation.
            top.eulerAngles = new Vector3(-90f, 180f, 0f);

            left.centre = new Vector3(-front.width * 0.5f, left.height * 0.5f, frontDistance - depth * 0.5f);
            left.eulerAngles = new Vector3(0f, -90f, 0f);

            right.centre = new Vector3(front.width * 0.5f, right.height * 0.5f, frontDistance - depth * 0.5f);
            right.eulerAngles = new Vector3(0f, 90f, 0f);

            // The Inspector button must visibly move existing Quads as well as update calibration data.
            projector.RenderOutputs();
            BuildEnabledScreens();
        }

        /// <summary>
        /// Builds a closed six-sided room from the active Front/Top dimensions. Back matches Front;
        /// Bottom matches Top. All six planes share their physical edges around the calibrated eye point.
        /// </summary>
        public void ArrangeAsCompleteCube()
        {
            if (projector == null) projector = GetComponent<EquirectangularScreenProjector>();
            if (projector == null) return;

            ProjectionScreen front = Find(ScreenDirection.Front);
            ProjectionScreen back = Find(ScreenDirection.Back);
            ProjectionScreen left = Find(ScreenDirection.Left);
            ProjectionScreen right = Find(ScreenDirection.Right);
            ProjectionScreen top = Find(ScreenDirection.Top);
            ProjectionScreen bottom = Find(ScreenDirection.Bottom);
            if (front == null || back == null || left == null || right == null || top == null || bottom == null) return;

            back.enabled = true;
            bottom.enabled = true;
            back.width = front.width;
            back.height = front.height;
            bottom.width = top.width;
            bottom.height = top.height;

            ArrangeAsOpenCube();

            float depth = Mathf.Max(0.001f, (left.width + right.width) * 0.5f);
            float frontDistance = projector.IdealPointToFrontScreenDistance;
            float backZ = frontDistance - depth;

            back.centre = new Vector3(0f, back.height * 0.5f, backZ);
            back.eulerAngles = new Vector3(0f, 180f, 0f);

            // The bottom shares the same X/Z footprint as Top but lies on the floor.
            bottom.centre = new Vector3(0f, 0f, frontDistance - bottom.height * 0.5f);
            bottom.eulerAngles = new Vector3(90f, 0f, 0f);

            projector.RenderOutputs();
            BuildEnabledScreens();
        }

        /// <summary>Moves Front, Left and Right so their physical lower edges lie on the world Y=0 line.</summary>
        public void AlignVerticalScreensBottomToZero()
        {
            if (projector == null) projector = GetComponent<EquirectangularScreenProjector>();
            if (projector == null) return;

            ProjectionScreen front = Find(ScreenDirection.Front);
            AlignBottom(front);
            AlignBottom(Find(ScreenDirection.Left));
            AlignBottom(Find(ScreenDirection.Right));

            // Keep the Top screen glued to the Front screen's upper edge after the walls move upward.
            ProjectionScreen top = Find(ScreenDirection.Top);
            if (front != null && top != null)
            {
                float frontTopY = front.centre.y + Mathf.Abs(front.Up.y) * front.height * 0.5f;
                top.centre = new Vector3(top.centre.x, frontTopY, top.centre.z);
            }
            BuildEnabledScreens();
        }

        /// <summary>Applies the latest supplied active display sizes (mm converted to Unity metres).</summary>
        public void ApplyOnSiteEffectiveSizePreset()
        {
            if (projector == null) projector = GetComponent<EquirectangularScreenProjector>();
            if (projector == null) return;

            projector.SetSourceLatitudeIsFlipped(true);

            SetSize(ScreenDirection.Front, 7.371f, 3.5f);
            SetSize(ScreenDirection.Left, 4.251f, 3.5f);
            SetSize(ScreenDirection.Right, 4.251f, 3.5f);
            SetSize(ScreenDirection.Top, 7.371f, 4.251f);
            ArrangeAsOpenCube();
            AlignVerticalScreensBottomToZero();
        }

        public void SyncFromScene()
        {
            if (projector == null) projector = GetComponent<EquirectangularScreenProjector>();
            if (projector == null) return;

            foreach (ProjectionScreen screen in projector.Screens)
            {
                if (!screenObjects.TryGetValue(screen.direction, out GameObject screenObject) || screenObject == null) continue;
                Transform transform = screenObject.transform;
                screen.centre = transform.position;
                screen.eulerAngles = transform.eulerAngles;
                screen.width = transform.localScale.x;
                screen.height = transform.localScale.y;
            }
        }

        private GameObject GetOrCreate(ScreenDirection direction)
        {
            if (screenObjects.TryGetValue(direction, out GameObject screenObject) && screenObject != null) return screenObject;

            Transform existing = transform.Find($"Screen_{direction}");
            screenObject = existing == null ? GameObject.CreatePrimitive(PrimitiveType.Quad) : existing.gameObject;
            screenObject.name = $"Screen_{direction}";
            screenObject.transform.SetParent(transform, true);
            screenObjects[direction] = screenObject;
            return screenObject;
        }

        private ProjectionScreen Find(ScreenDirection direction)
        {
            foreach (ProjectionScreen screen in projector.Screens)
                if (screen.direction == direction) return screen;
            return null;
        }

        private static void AlignBottom(ProjectionScreen screen)
        {
            if (screen == null) return;
            float verticalExtent = Mathf.Abs(screen.Up.y) * screen.height;
            screen.centre = new Vector3(screen.centre.x, verticalExtent * 0.5f, screen.centre.z);
        }

        private void SetSize(ScreenDirection direction, float width, float height)
        {
            ProjectionScreen screen = Find(direction);
            if (screen == null) return;
            screen.enabled = true;
            screen.width = width;
            screen.height = height;
        }

        private Material GetScreenMaterial(ScreenDirection direction)
        {
            // Each screen needs its own material, otherwise assigning the next RenderTexture overwrites all previews.
            string materialName = $"Screen Projection Preview - {direction}";
            Transform materialHolder = transform.Find("Preview Materials");
            if (materialHolder == null)
            {
                GameObject holder = new GameObject("Preview Materials");
                holder.transform.SetParent(transform, false);
                materialHolder = holder.transform;
            }

            Material existing = materialHolder.GetComponent<ScreenPreviewMaterialStore>()?.Get(direction);
            if (existing != null) return existing;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Texture");
            Material material = new Material(shader) { name = materialName };
            ScreenPreviewMaterialStore store = materialHolder.GetComponent<ScreenPreviewMaterialStore>();
            if (store == null) store = materialHolder.gameObject.AddComponent<ScreenPreviewMaterialStore>();
            store.Set(direction, material);
            return material;
        }
    }
}
