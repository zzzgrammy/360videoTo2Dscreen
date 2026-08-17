using System;
using UnityEngine;

namespace Turn360To2D
{
    [Serializable]
    public sealed class ProjectionScreen
    {
        public ScreenDirection direction;
        public bool enabled;
        [Min(1)] public int outputWidth = 1920;
        [Min(1)] public int outputHeight = 1080;
        [Min(0.001f)] public float width = 4f;
        [Min(0.001f)] public float height = 2.25f;

        [Tooltip("Screen centre in world coordinates. The ideal viewing point is always world origin.")]
        public Vector3 centre;

        [Tooltip("Screen orientation. Local +Z points from the origin towards the screen; local X and Y define its image axes.")]
        public Vector3 eulerAngles;

        [NonSerialized] public RenderTexture output;

        public Quaternion Rotation => Quaternion.Euler(eulerAngles);
        public Vector3 Right => Rotation * Vector3.right;
        public Vector3 Up => Rotation * Vector3.up;
    }
}
