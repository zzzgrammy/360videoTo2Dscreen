using UnityEngine;

namespace Turn360To2D
{
    /// <summary>
    /// Projects an equirectangular source onto physical cube surfaces using their true world positions.
    /// No face-specific UV correction is used: all seams are defined by one viewing point.
    /// </summary>
    [ExecuteAlways]
    public sealed class PhysicalCube360Projection : MonoBehaviour
    {
        [SerializeField] private Camera viewingCamera;
        [SerializeField] private Texture panorama;
        [SerializeField] private Shader projectionShader;

        private Material projectionMaterial;

        private void OnEnable() => Apply();
        private void OnValidate() => Apply();

        private void LateUpdate()
        {
            if (viewingCamera == null) viewingCamera = Camera.main;
            if (projectionMaterial != null && viewingCamera != null)
                projectionMaterial.SetVector("_ViewingPoint", viewingCamera.transform.position);
        }

        private void OnDisable()
        {
            if (projectionMaterial == null) return;
            if (Application.isPlaying) Destroy(projectionMaterial);
            else DestroyImmediate(projectionMaterial);
            projectionMaterial = null;
        }

        public void Apply()
        {
            if (viewingCamera == null) viewingCamera = Camera.main;
            if (panorama == null) panorama = Resources.Load<Texture2D>("006");
            if (projectionShader == null) projectionShader = Shader.Find("Turn360To2D/World Space 360 Screen");
            if (panorama == null || projectionShader == null) return;

            if (projectionMaterial == null || projectionMaterial.shader != projectionShader)
                projectionMaterial = new Material(projectionShader) { name = "Physical Cube 360 Projection" };

            projectionMaterial.SetTexture("_MainTex", panorama);
            projectionMaterial.SetVector("_ViewingPoint", viewingCamera == null ? Vector3.zero : viewingCamera.transform.position);
            foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>())
                renderer.sharedMaterial = projectionMaterial;
        }
    }
}
