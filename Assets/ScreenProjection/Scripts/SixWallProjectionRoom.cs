using UnityEngine;

namespace Turn360To2D
{
    /// <summary>
    /// Replaces the visible mesh of a room Cube with six separately selectable
    /// wall planes.  All planes share one material which calculates its colour
    /// from the ray from the active rendering camera through the wall pixel.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class SixWallProjectionRoom : MonoBehaviour
    {
        private const string FacesRootName = "Six Projection Faces";

        [Tooltip("The equirectangular world-space projection material used by all six faces.")]
        [SerializeField] private Material projectionMaterial;

        [Tooltip("The original Cube MeshRenderer is disabled after its material is copied to the six faces.")]
        [SerializeField] private bool hideOriginalCubeMesh = true;

        private readonly struct FaceDefinition
        {
            public readonly string Name;
            public readonly Vector3 LocalPosition;
            public readonly Vector3 LocalEulerAngles;

            public FaceDefinition(string name, Vector3 localPosition, Vector3 localEulerAngles)
            {
                Name = name;
                LocalPosition = localPosition;
                LocalEulerAngles = localEulerAngles;
            }
        }

        // A Unity Quad faces local +Z.  The normals below intentionally face
        // outwards, so the projection shader's Cull Front renders the room side.
        private static readonly FaceDefinition[] Faces =
        {
            new FaceDefinition("Front",  new Vector3(0f,    0f,  0.5f), Vector3.zero),
            new FaceDefinition("Back",   new Vector3(0f,    0f, -0.5f), new Vector3(0f, 180f, 0f)),
            new FaceDefinition("Left",   new Vector3(-0.5f, 0f,  0f),   new Vector3(0f, -90f, 0f)),
            new FaceDefinition("Right",  new Vector3(0.5f,  0f,  0f),   new Vector3(0f, 90f, 0f)),
            new FaceDefinition("Top",    new Vector3(0f,  0.5f,  0f),   new Vector3(-90f, 0f, 0f)),
            new FaceDefinition("Bottom", new Vector3(0f, -0.5f,  0f),   new Vector3(90f, 0f, 0f))
        };

        private void OnEnable() => RebuildFaces();

        private void OnValidate() => RebuildFaces();

        [ContextMenu("Rebuild Six Projection Faces")]
        public void RebuildFaces()
        {
            MeshRenderer sourceRenderer = GetComponent<MeshRenderer>();
            if (projectionMaterial == null && sourceRenderer != null)
                projectionMaterial = sourceRenderer.sharedMaterial;

            if (hideOriginalCubeMesh && sourceRenderer != null)
                sourceRenderer.enabled = false;

            Transform facesRoot = transform.Find(FacesRootName);
            if (facesRoot == null)
            {
                GameObject root = new GameObject(FacesRootName);
                facesRoot = root.transform;
                facesRoot.SetParent(transform, false);
            }

            facesRoot.localPosition = Vector3.zero;
            facesRoot.localRotation = Quaternion.identity;
            facesRoot.localScale = Vector3.one;

            foreach (FaceDefinition face in Faces)
            {
                Transform faceTransform = facesRoot.Find(face.Name);
                if (faceTransform == null)
                {
                    GameObject faceObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    faceObject.name = face.Name;
                    faceTransform = faceObject.transform;
                    faceTransform.SetParent(facesRoot, false);

                    Collider primitiveCollider = faceObject.GetComponent<Collider>();
                    // OnValidate may run from a rendering or editor callback;
                    // delayed destruction is the only safe option in both modes.
                    if (primitiveCollider != null) Destroy(primitiveCollider);
                }

                faceTransform.localPosition = face.LocalPosition;
                faceTransform.localRotation = Quaternion.Euler(face.LocalEulerAngles);
                faceTransform.localScale = Vector3.one;

                MeshRenderer faceRenderer = faceTransform.GetComponent<MeshRenderer>();
                if (faceRenderer != null) faceRenderer.sharedMaterial = projectionMaterial;
            }
        }
    }
}
