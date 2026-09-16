using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

namespace Turn360To2D.Editor
{
    /// <summary>Editor entry point for creating and exporting a physical six-wall 360 cube.</summary>
    public sealed class Turn360To2DWindow : EditorWindow
    {
        private SixFaceProjectionOutput output;
        private Vector2 scrollPosition;

        // Shown before Cube0 exists, so a user can configure the room first.
        private SixFaceProjectionOutput.InputKind newInputKind;
        private Texture newStillImage;
        private VideoClip newVideoClip;
        private DefaultAsset newSequenceFolder;
        private float newWidth = 7.371f;
        private float newDepth = 4.251f;
        private float newHeight = 3.5f;

        [MenuItem("GM/360cube", false, 10)]
        public static void Open()
        {
            Turn360To2DWindow window = GetWindow<Turn360To2DWindow>("360cube");
            window.minSize = new Vector2(430f, 500f);
            window.Show();
        }

        private void OnEnable() => FindProjector();

        private void OnSelectionChange()
        {
            if (Selection.activeGameObject != null)
            {
                SixFaceProjectionOutput selected = Selection.activeGameObject.GetComponentInParent<SixFaceProjectionOutput>();
                if (selected != null) output = selected;
            }
            Repaint();
        }

        private void Update() => Repaint();

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("360cube", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("360 panorama → six physical 2D screen outputs", EditorStyles.miniLabel);
            EditorGUILayout.Space(8f);

            if (output == null)
            {
                DrawCreateCubeControls();
                DrawChineseUsage();
            }
            else
            {
                DrawProjectorControls(output);
                DrawChineseUsage();
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawCreateCubeControls()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("1. Create Cube0", EditorStyles.boldLabel);
            DrawNewInputControls();

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Cube size (metres)", EditorStyles.boldLabel);
            newWidth = Mathf.Max(0.001f, EditorGUILayout.FloatField("Width (X)", newWidth));
            newDepth = Mathf.Max(0.001f, EditorGUILayout.FloatField("Depth (Z)", newDepth));
            newHeight = Mathf.Max(0.001f, EditorGUILayout.FloatField("Height (Y)", newHeight));
            EditorGUILayout.HelpBox("Output width and height are calculated separately for every face from the panorama resolution, the viewing-camera position and the four wall corners.", MessageType.Info);

            EditorGUILayout.Space(10f);
            if (GUILayout.Button("Create Cube0 (Six Faces)", GUILayout.Height(30f)))
                CreateConfiguredCube();
        }

        private void DrawNewInputControls()
        {
            newInputKind = (SixFaceProjectionOutput.InputKind)GUILayout.Toolbar((int)newInputKind,
                new[] { "360 Image", "360 Video", "Image Sequence" });

            switch (newInputKind)
            {
                case SixFaceProjectionOutput.InputKind.StillImage:
                    newStillImage = (Texture)EditorGUILayout.ObjectField("360 Image", newStillImage, typeof(Texture), false);
                    break;
                case SixFaceProjectionOutput.InputKind.Video:
                    newVideoClip = (VideoClip)EditorGUILayout.ObjectField("360 Video", newVideoClip, typeof(VideoClip), false);
                    break;
                case SixFaceProjectionOutput.InputKind.ImageSequence:
                    newSequenceFolder = (DefaultAsset)EditorGUILayout.ObjectField("Frames Folder", newSequenceFolder, typeof(DefaultAsset), false);
                    if (newSequenceFolder != null && !AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(newSequenceFolder)))
                        EditorGUILayout.HelpBox("Choose a folder under Assets.", MessageType.Warning);
                    break;
            }
        }

        private void CreateConfiguredCube()
        {
            SixFaceProjectionOutput existing = UnityEngine.Object.FindObjectOfType<SixFaceProjectionOutput>();
            if (existing != null)
            {
                output = existing;
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            if (GameObject.Find("Cube0") != null)
            {
                EditorUtility.DisplayDialog("360cube", "A GameObject named Cube0 already exists. Rename or remove it before creating a new plugin Cube0.", "OK");
                return;
            }

            GameObject root = new GameObject("Cube0");
            Undo.RegisterCreatedObjectUndo(root, "Create 360cube Cube0");
            BoxCollider bounds = root.AddComponent<BoxCollider>();
            foreach (ScreenDirection direction in Enum.GetValues(typeof(ScreenDirection))) CreateFace(root.transform, direction.ToString());

            SixFaceProjectionOutput projector = root.AddComponent<SixFaceProjectionOutput>();
            ConfigureProjector(projector, bounds);
            output = projector;
            Selection.activeGameObject = root;
            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        private void ConfigureProjector(SixFaceProjectionOutput projector, BoxCollider bounds)
        {
            SerializedObject serialized = new SerializedObject(projector);
            serialized.Update();
            serialized.FindProperty("inputKind").enumValueIndex = (int)newInputKind;
            serialized.FindProperty("stillImage").objectReferenceValue = newStillImage;
            serialized.FindProperty("cubeWidth").floatValue = newWidth;
            serialized.FindProperty("cubeDepth").floatValue = newDepth;
            serialized.FindProperty("cubeHeight").floatValue = newHeight;
            float eyeToFront = serialized.FindProperty("eyeToFrontWall").floatValue;

            if (newInputKind == SixFaceProjectionOutput.InputKind.Video && newVideoClip != null)
            {
                GameObject source = new GameObject("360 Video Source");
                source.transform.SetParent(projector.transform, false);
                VideoPlayer player = source.AddComponent<VideoPlayer>();
                player.source = VideoSource.VideoClip;
                player.clip = newVideoClip;
                player.renderMode = VideoRenderMode.APIOnly;
                player.isLooping = true;
                player.playOnAwake = true;
                serialized.FindProperty("videoPlayer").objectReferenceValue = player;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();

            bounds.center = new Vector3(0f, newHeight * 0.5f, eyeToFront - newDepth * 0.5f);
            bounds.size = new Vector3(newWidth, newHeight, newDepth);
            projector.ApplyCubeLayout();

            if (newInputKind == SixFaceProjectionOutput.InputKind.ImageSequence && newSequenceFolder != null)
            {
                string folder = AssetDatabase.GetAssetPath(newSequenceFolder);
                if (AssetDatabase.IsValidFolder(folder)) projector.SetImageSequence(folder, LoadFrames(folder));
            }
        }

        private static void CreateFace(Transform parent, string faceName)
        {
            GameObject face = GameObject.CreatePrimitive(PrimitiveType.Cube);
            face.name = faceName;
            face.transform.SetParent(parent, false);
            Collider collider = face.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
        }

        private void DrawProjectorControls(SixFaceProjectionOutput projector)
        {
            SerializedObject serialized = new SerializedObject(projector);
            serialized.Update();

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("360 Input", EditorStyles.boldLabel);
            SerializedProperty inputKind = serialized.FindProperty("inputKind");
            inputKind.enumValueIndex = GUILayout.Toolbar(inputKind.enumValueIndex,
                new[] { "360 Image", "360 Video", "Image Sequence" });
            SixFaceProjectionOutput.InputKind kind = (SixFaceProjectionOutput.InputKind)inputKind.enumValueIndex;
            if (kind == SixFaceProjectionOutput.InputKind.StillImage)
                EditorGUILayout.PropertyField(serialized.FindProperty("stillImage"), new GUIContent("360 Image"));
            else if (kind == SixFaceProjectionOutput.InputKind.Video)
                EditorGUILayout.PropertyField(serialized.FindProperty("videoPlayer"), new GUIContent("Video Player"));
            else
                DrawSequenceFolder(serialized, projector);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Cube size (metres)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serialized.FindProperty("cubeWidth"), new GUIContent("Width (X)"));
            EditorGUILayout.PropertyField(serialized.FindProperty("cubeDepth"), new GUIContent("Depth (Z)"));
            EditorGUILayout.PropertyField(serialized.FindProperty("cubeHeight"), new GUIContent("Height (Y)"));
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Automatic output resolution", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serialized.FindProperty("outputResolutionScale"), new GUIContent("Resolution scale"));
            EditorGUILayout.PropertyField(serialized.FindProperty("maximumOutputDimension"), new GUIContent("Maximum dimension"));
            EditorGUILayout.HelpBox("Move the viewing camera to the best observation point, then click Generate. Generation uses the camera's current position and will not reset it.", MessageType.Info);

            if (serialized.ApplyModifiedProperties())
            {
                projector.ApplyCubeLayout();
                projector.RenderAllFaces();
                EditorUtility.SetDirty(projector);
            }

            DrawRecommendedDimensions(projector);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply Cube Size")) projector.ApplyCubeLayout();
                if (GUILayout.Button("Move Camera to Start")) projector.MoveViewingCameraToCalibratedStart();
            }

            DrawGenerateControls(projector, kind);
        }

        private static void DrawRecommendedDimensions(SixFaceProjectionOutput projector)
        {
            EditorGUILayout.Space(6f);
            if (!projector.TryGetSourceDimensions(out int sourceWidth, out int sourceHeight))
            {
                EditorGUILayout.HelpBox("Waiting for the 360 source dimensions. Video dimensions become available after the VideoPlayer is prepared.", MessageType.None);
                return;
            }

            EditorGUILayout.LabelField($"360 source: {sourceWidth} × {sourceHeight}", EditorStyles.miniBoldLabel);
            foreach (ScreenDirection direction in Enum.GetValues(typeof(ScreenDirection)))
            {
                if (projector.TryGetRecommendedOutputDimensions(direction, out int width, out int height))
                    EditorGUILayout.LabelField(direction.ToString(), $"{width} × {height}");
            }
            EditorGUILayout.HelpBox("These dimensions update with the camera position. They are locked when generation or recording starts.", MessageType.None);
        }

        private static void DrawGenerateControls(SixFaceProjectionOutput projector, SixFaceProjectionOutput.InputKind kind)
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("2. Generate Outputs", EditorStyles.boldLabel);
            bool recording = SixFaceProjectionOutputEditor.IsRecording(projector);
            string generateLabel = kind == SixFaceProjectionOutput.InputKind.StillImage
                ? "Generate Six PNG Images"
                : kind == SixFaceProjectionOutput.InputKind.ImageSequence
                    ? "Generate Six PNG Sequences"
                    : "Start Six MP4 Recordings";
            bool canStart = kind != SixFaceProjectionOutput.InputKind.Video || Application.isPlaying;
            using (new EditorGUI.DisabledScope(recording || !canStart))
            {
                if (GUILayout.Button(generateLabel, GUILayout.Height(30f)))
                {
                    if (kind == SixFaceProjectionOutput.InputKind.StillImage) SixFaceProjectionOutputEditor.ExportCurrentFacesAsPng(projector);
                    else if (kind == SixFaceProjectionOutput.InputKind.ImageSequence) SixFaceProjectionOutputEditor.ExportImageSequenceAsPng(projector);
                    else SixFaceProjectionOutputEditor.StartRecording(projector);
                }
            }

            if (recording)
            {
                float progress = Mathf.PingPong((float)EditorApplication.timeSinceStartup, 1f);
                Rect rect = GUILayoutUtility.GetRect(18f, 18f, "TextField");
                EditorGUI.ProgressBar(rect, progress, "Recording six MP4 outputs…");
                if (GUILayout.Button("Stop and Finalize MP4 Files")) SixFaceProjectionOutputEditor.StopRecording(projector);
            }
            else if (kind == SixFaceProjectionOutput.InputKind.Video && !Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play mode first. The VideoPlayer texture is captured live by Recorder.", MessageType.Info);
            }

            EditorGUILayout.HelpBox("Output: Assets/StreamingAssets/360TurnTo2D/<timestamp>/<timestamp>_<direction>…", MessageType.None);
        }

        private static void DrawSequenceFolder(SerializedObject serialized, SixFaceProjectionOutput projector)
        {
            SerializedProperty pathProperty = serialized.FindProperty("imageSequenceFolder");
            DefaultAsset current = string.IsNullOrEmpty(pathProperty.stringValue) ? null : AssetDatabase.LoadAssetAtPath<DefaultAsset>(pathProperty.stringValue);
            DefaultAsset folder = (DefaultAsset)EditorGUILayout.ObjectField("Frames Folder", current, typeof(DefaultAsset), false);
            if (folder != null && AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(folder))) pathProperty.stringValue = AssetDatabase.GetAssetPath(folder);
            EditorGUILayout.LabelField("Indexed Frames", projector.SequenceFrameCount.ToString());
            if (GUILayout.Button("Index Selected Folder"))
            {
                if (!AssetDatabase.IsValidFolder(pathProperty.stringValue))
                {
                    EditorUtility.DisplayDialog("360cube", "Choose a folder under Assets first.", "OK");
                    return;
                }
                serialized.ApplyModifiedProperties();
                projector.SetImageSequence(pathProperty.stringValue, LoadFrames(pathProperty.stringValue));
                EditorUtility.SetDirty(projector);
            }
        }

        private static List<Texture2D> LoadFrames(string folder)
        {
            return AssetDatabase.FindAssets("t:Texture2D", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Select(AssetDatabase.LoadAssetAtPath<Texture2D>)
                .Where(texture => texture != null)
                .ToList();
        }

        private static void DrawChineseUsage()
        {
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("使用步骤", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "1. 选择 360 图片、360 视频或序列帧文件夹，并输入 Cube0 的宽(X)、深(Z)、高(Y)。\n" +
                "2. 点击“Create Cube0 (Six Faces)”创建六面屏。\n" +
                "3. 移动主相机到最佳观测点；生成时会使用相机当前位置，不会自动复位。\n" +
                "4. 插件会依据原图分辨率、相机位置和每面四角，分别显示六面的建议输出尺寸。\n" +
                "5. Resolution scale=1 表示保留原始角分辨率；Maximum dimension 是显存安全上限。\n" +
                "6. 图片选择“Generate Six PNG Images”；序列帧选择“Generate Six PNG Sequences”。\n" +
                "7. 视频需进入 Play 模式后点击“Start Six MP4 Recordings”，结束时点击停止。\n" +
                "8. 输出保存至 Assets/StreamingAssets/360TurnTo2D/时间戳，文件名为“时间戳_方向”。",
                MessageType.Info);
        }

        private void FindProjector() => output = UnityEngine.Object.FindObjectOfType<SixFaceProjectionOutput>();
    }
}
