using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using UnityEngine;

namespace Turn360To2D.Editor
{
    [CustomEditor(typeof(SixFaceProjectionOutput))]
    public sealed class SixFaceProjectionOutputEditor : UnityEditor.Editor
    {
        private static readonly Dictionary<SixFaceProjectionOutput, RecorderController> Controllers = new Dictionary<SixFaceProjectionOutput, RecorderController>();
        private static readonly Dictionary<SixFaceProjectionOutput, double> RecordingStartTimes = new Dictionary<SixFaceProjectionOutput, double>();

        public static bool IsRecording(SixFaceProjectionOutput output)
        {
            return output != null && Controllers.TryGetValue(output, out RecorderController controller) && controller.IsRecording();
        }

        public static double RecordingElapsedSeconds(SixFaceProjectionOutput output)
        {
            return RecordingStartTimes.TryGetValue(output, out double start) ? EditorApplication.timeSinceStartup - start : 0d;
        }

        public static void ExportCurrentFacesAsPng(SixFaceProjectionOutput output)
        {
            if (output == null) return;
            output.RenderAllFaces();
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string directory = Path.Combine("Assets", "StreamingAssets", "360TurnTo2D", timestamp);
            Directory.CreateDirectory(directory);

            foreach (ScreenDirection face in Enum.GetValues(typeof(ScreenDirection)))
            {
                RenderTexture source = output.GetOutput(face);
                if (source == null) continue;
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = source;
                Texture2D image = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                image.Apply(false, false);
                File.WriteAllBytes(Path.Combine(directory, timestamp + "_" + face + ".png"), image.EncodeToPNG());
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(image);
            }
            AssetDatabase.Refresh();
            Debug.Log("360 To 2D: exported six PNG images to " + directory);
        }

        public static void ExportImageSequenceAsPng(SixFaceProjectionOutput output)
        {
            if (output == null || output.SequenceFrameCount == 0)
            {
                EditorUtility.DisplayDialog("360 Turn To 2D", "请先选择并索引序列帧文件夹。", "OK");
                return;
            }

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string directory = Path.Combine("Assets", "StreamingAssets", "360TurnTo2D", timestamp);
            try
            {
                for (int frame = 0; frame < output.SequenceFrameCount; frame++)
                {
                    EditorUtility.DisplayProgressBar("360 Turn To 2D", $"正在转换第 {frame + 1}/{output.SequenceFrameCount} 帧", (frame + 1f) / output.SequenceFrameCount);
                    output.SetSequenceFrameForExport(frame);
                    foreach (ScreenDirection face in Enum.GetValues(typeof(ScreenDirection)))
                    {
                        RenderTexture source = output.GetOutput(face);
                        if (source == null) continue;
                        string faceDirectory = Path.Combine(directory, face.ToString());
                        Directory.CreateDirectory(faceDirectory);
                        RenderTexture previous = RenderTexture.active;
                        RenderTexture.active = source;
                        Texture2D image = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
                        image.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                        image.Apply(false, false);
                        File.WriteAllBytes(Path.Combine(faceDirectory, $"{timestamp}_{face}_{frame:D6}.png"), image.EncodeToPNG());
                        RenderTexture.active = previous;
                        UnityEngine.Object.DestroyImmediate(image);
                    }
                }
            }
            finally
            {
                output.ClearSequenceFrameOverride();
                EditorUtility.ClearProgressBar();
            }
            AssetDatabase.Refresh();
            Debug.Log("360 To 2D: exported six PNG sequences to " + directory);
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            SixFaceProjectionOutput output = (SixFaceProjectionOutput)target;
            EditorGUILayout.Space();
            DrawSequenceFolderIndexer(output);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Six 2D Outputs", EditorStyles.boldLabel);
            if (GUILayout.Button("Apply Cube Layout + Render Six Outputs"))
            {
                output.ApplyCubeLayout();
                output.RenderAllFaces();
            }
            if (GUILayout.Button("Start Convert")) ExportCurrentFacesAsPng(output);

            if (Application.isPlaying)
            {
                bool recording = IsRecording(output);
                if (!recording && GUILayout.Button("Start Six MP4 Recordings")) StartRecording(output);
                if (recording && GUILayout.Button("Stop Six MP4 Recordings")) StopRecording(output);
            }
            else
            {
                EditorGUILayout.HelpBox("Enter Play mode to record. Six MP4 files will be created in Recordings/360To2D/<Take>/<Face>/.", MessageType.Info);
            }
        }

        private static void DrawSequenceFolderIndexer(SixFaceProjectionOutput output)
        {
            EditorGUILayout.LabelField("Image Sequence Folder", EditorStyles.boldLabel);
            DefaultAsset currentFolder = string.IsNullOrEmpty(output.ImageSequenceFolder)
                ? null
                : AssetDatabase.LoadAssetAtPath<DefaultAsset>(output.ImageSequenceFolder);
            DefaultAsset selectedFolder = (DefaultAsset)EditorGUILayout.ObjectField("Frames Folder", currentFolder, typeof(DefaultAsset), false);

            if (selectedFolder != null)
            {
                string folderPath = AssetDatabase.GetAssetPath(selectedFolder);
                if (!AssetDatabase.IsValidFolder(folderPath))
                {
                    EditorGUILayout.HelpBox("Select a folder under Assets that contains the panorama frame images.", MessageType.Warning);
                }
                else if (GUILayout.Button("Index Folder as Image Sequence"))
                {
                    List<Texture2D> frames = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath })
                        .Select(AssetDatabase.GUIDToAssetPath)
                        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                        .Select(AssetDatabase.LoadAssetAtPath<Texture2D>)
                        .Where(texture => texture != null)
                        .ToList();
                    Undo.RecordObject(output, "Index 360 Image Sequence Folder");
                    output.SetImageSequence(folderPath, frames);
                    EditorUtility.SetDirty(output);
                    Debug.Log($"360 To 2D: indexed {frames.Count} frames from {folderPath} in filename order.");
                }
            }
            EditorGUILayout.LabelField("Indexed Frames", output.SequenceFrameCount.ToString());
        }

        public static void StartRecording(SixFaceProjectionOutput output)
        {
            output.RenderAllFaces();
            var settings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            settings.SetRecordModeToManual();
            settings.FrameRate = 30f;
            settings.CapFrameRate = true;
            string take = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            foreach (ScreenDirection face in Enum.GetValues(typeof(ScreenDirection)))
            {
                RenderTexture texture = output.GetOutput(face);
                if (texture == null) continue;
                var movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
                movie.name = face + " MP4";
                movie.Enabled = true;
                // Recorder 4.0.3 keeps these properties as the supported CoreEncoder shortcut,
                // but marks them obsolete in favour of its newer EncoderSettings API.
                #pragma warning disable 618
                movie.OutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;
                movie.VideoBitRateMode = VideoBitrateMode.High;
                #pragma warning restore 618
                movie.CaptureAudio = false;
                movie.ImageInputSettings = new RenderTextureInputSettings { RenderTexture = texture, FlipFinalOutput = true };
                string directory = Path.Combine("Assets", "StreamingAssets", "360TurnTo2D", take);
                Directory.CreateDirectory(directory);
                movie.OutputFile = Path.Combine(directory, take + "_" + face);
                settings.AddRecorderSettings(movie);
            }

            var controller = new RecorderController(settings);
            try
            {
                controller.PrepareRecording();
                if (!controller.StartRecording())
                {
                    controller.StopRecording();
                    Debug.LogError("360 To 2D Recorder failed to start. Check the Console for the Recorder diagnostic.");
                    return;
                }
                Controllers[output] = controller;
                RecordingStartTimes[output] = EditorApplication.timeSinceStartup;
                Debug.Log("360 To 2D: recording six MP4 outputs to Recordings/360To2D/" + take);
            }
            catch (Exception exception)
            {
                controller.StopRecording();
                Debug.LogException(exception);
            }
        }

        public static void StopRecording(SixFaceProjectionOutput output)
        {
            if (!Controllers.TryGetValue(output, out RecorderController controller)) return;
            controller.StopRecording();
            Controllers.Remove(output);
            RecordingStartTimes.Remove(output);
            Debug.Log("360 To 2D: six MP4 recordings stopped and finalized.");
        }
    }
}
