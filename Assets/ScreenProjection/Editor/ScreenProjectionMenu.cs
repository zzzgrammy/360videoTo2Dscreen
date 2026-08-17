using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Turn360To2D.Editor
{
    public static class ScreenProjectionMenu
    {
        private const string TestFramePath = "Assets/360/006.png";
        private const string DomeMaterialPath = "Assets/ScreenProjection/Materials/Inside360Dome_006.mat";

        [MenuItem("Tools/360 To 2D/Create Open Cube Calibrator")]
        public static void CreateOpenCubeCalibrator()
        {
            EquirectangularScreenProjector projector = Object.FindObjectOfType<EquirectangularScreenProjector>();
            if (projector == null)
            {
                GameObject root = new GameObject("360 To 2D Calibrator");
                projector = root.AddComponent<EquirectangularScreenProjector>();
            }

            ScreenProjectionBuilder builder = projector.GetComponent<ScreenProjectionBuilder>();
            if (builder == null) builder = projector.gameObject.AddComponent<ScreenProjectionBuilder>();
            builder.BuildEnabledScreens();
            ScreenProjectionRuntimeUiBuilder.Create(projector, builder);
            Selection.activeGameObject = projector.gameObject;
            EditorUtility.SetDirty(projector.gameObject);
        }

        [MenuItem("Tools/360 To 2D/Build Clean demo1 Baseline")]
        public static void BuildCleanDemo1Baseline()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name != "demo1")
            {
                EditorUtility.DisplayDialog("360 To 2D", "Please open Assets/Scenes/demo1 first. This command deliberately does not modify the old SampleScene.", "OK");
                return;
            }

            EquirectangularScreenProjector projector = Object.FindObjectOfType<EquirectangularScreenProjector>();
            if (projector == null)
            {
                GameObject root = new GameObject("360 Projection Baseline");
                projector = root.AddComponent<EquirectangularScreenProjector>();
            }

            Texture2D frame = AssetDatabase.LoadAssetAtPath<Texture2D>(TestFramePath);
            if (frame == null)
            {
                EditorUtility.DisplayDialog("360 To 2D", "Test frame was not found at " + TestFramePath, "OK");
                return;
            }

            projector.SetSourceLatitudeIsFlipped(true);
            projector.SetSourceTexture(frame);
            ScreenProjectionBuilder builder = projector.GetComponent<ScreenProjectionBuilder>();
            if (builder == null) builder = projector.gameObject.AddComponent<ScreenProjectionBuilder>();
            builder.ArrangeAsCompleteCube();
            builder.BuildEnabledScreens();

            Camera camera = Camera.main;
            if (camera != null)
            {
                PanoramaMouseLook mouseLook = camera.GetComponent<PanoramaMouseLook>();
                if (mouseLook == null) mouseLook = camera.gameObject.AddComponent<PanoramaMouseLook>();
                mouseLook.ConfigureIdealViewingPoint(projector.IdealViewingHeight);
            }

            ScreenProjectionRuntimeUiBuilder.Create(projector, builder);
            EditorUtility.SetDirty(projector.gameObject);
            EditorSceneManager.MarkSceneDirty(activeScene);
            Selection.activeGameObject = projector.gameObject;
        }

        [MenuItem("Tools/360 To 2D/Use 006.png as Test Frame")]
        public static void UseTestFrame()
        {
            EquirectangularScreenProjector projector = Object.FindObjectOfType<EquirectangularScreenProjector>();
            if (projector == null)
            {
                CreateOpenCubeCalibrator();
                projector = Object.FindObjectOfType<EquirectangularScreenProjector>();
            }

            Texture2D frame = AssetDatabase.LoadAssetAtPath<Texture2D>(TestFramePath);
            if (frame == null)
            {
                EditorUtility.DisplayDialog("360 To 2D", "Test frame was not found at " + TestFramePath, "OK");
                return;
            }

            projector.SetSourceTexture(frame);
            ScreenProjectionBuilder builder = projector.GetComponent<ScreenProjectionBuilder>();
            if (builder != null) builder.BuildEnabledScreens();
            EditorUtility.SetDirty(projector.gameObject);
            Selection.activeGameObject = projector.gameObject;
        }

        [MenuItem("Tools/360 To 2D/Create 006.png Inside-Dome Preview")]
        public static void CreateInsideDomePreview()
        {
            Texture2D frame = AssetDatabase.LoadAssetAtPath<Texture2D>(TestFramePath);
            Shader shader = Shader.Find("Turn360To2D/Inside 360 Dome");
            if (frame == null || shader == null)
            {
                EditorUtility.DisplayDialog("360 To 2D", "The test image or inside-dome shader is still importing. Please try again in a moment.", "OK");
                return;
            }

            const string materialFolder = "Assets/ScreenProjection/Materials";
            if (!AssetDatabase.IsValidFolder(materialFolder)) AssetDatabase.CreateFolder("Assets/ScreenProjection", "Materials");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(DomeMaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, DomeMaterialPath);
            }
            material.shader = shader;
            material.mainTexture = frame;
            EditorUtility.SetDirty(material);

            GameObject dome = GameObject.Find("360 Inside Dome Preview");
            if (dome == null)
            {
                dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                dome.name = "360 Inside Dome Preview";
                dome.transform.position = new Vector3(0f, 1.65f, 0f);
                dome.transform.localScale = Vector3.one * 20f;
                Object.DestroyImmediate(dome.GetComponent<Collider>());
            }
            dome.GetComponent<MeshRenderer>().sharedMaterial = material;
            Selection.activeGameObject = dome;
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Tools/360 To 2D/Enable Runtime Mouse Look on Main Camera")]
        public static void EnableRuntimeMouseLook()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                EditorUtility.DisplayDialog("360 To 2D", "No Main Camera was found. Tag the desired camera as MainCamera, then run this command again.", "OK");
                return;
            }

            PanoramaMouseLook mouseLook = camera.GetComponent<PanoramaMouseLook>();
            if (mouseLook == null) mouseLook = camera.gameObject.AddComponent<PanoramaMouseLook>();
            Selection.activeGameObject = camera.gameObject;
            EditorUtility.SetDirty(camera.gameObject);
        }
    }
}
