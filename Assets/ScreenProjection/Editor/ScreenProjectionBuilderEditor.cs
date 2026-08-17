using UnityEditor;
using UnityEngine;

namespace Turn360To2D.Editor
{
    [CustomEditor(typeof(ScreenProjectionBuilder))]
    public sealed class ScreenProjectionBuilderEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            ScreenProjectionBuilder builder = (ScreenProjectionBuilder)target;
            if (GUILayout.Button("Apply On-site Effective Size (mm to m)")) builder.ApplyOnSiteEffectiveSizePreset();
            if (GUILayout.Button("Arrange + Create Front / Top / Left / Right Open Cube")) builder.ArrangeAsOpenCube();
            if (GUILayout.Button("Arrange + Create Complete Six-Sided Cube")) builder.ArrangeAsCompleteCube();
            if (GUILayout.Button("Align Front / Left / Right Bottom Edges to Y = 0")) builder.AlignVerticalScreensBottomToZero();
            if (GUILayout.Button("Create / Update Enabled Screens")) builder.BuildEnabledScreens();
            if (GUILayout.Button("Apply Manual Screen Transforms")) builder.SyncFromScene();
        }
    }
}
