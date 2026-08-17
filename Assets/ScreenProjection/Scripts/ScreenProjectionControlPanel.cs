using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace Turn360To2D
{
    /// <summary>Runtime UI bridge for entering dimensions and creating the four-screen open cube.</summary>
    public sealed class ScreenProjectionControlPanel : MonoBehaviour
    {
        [SerializeField] private EquirectangularScreenProjector projector;
        [SerializeField] private ScreenProjectionBuilder builder;
        [SerializeField] private InputField frontWidth;
        [SerializeField] private InputField frontHeight;
        [SerializeField] private InputField topWidth;
        [SerializeField] private InputField topHeight;
        [SerializeField] private InputField leftWidth;
        [SerializeField] private InputField leftHeight;
        [SerializeField] private InputField rightWidth;
        [SerializeField] private InputField rightHeight;
        [SerializeField] private InputField eyeToFrontDistance;

        public void Initialise(
            EquirectangularScreenProjector newProjector,
            ScreenProjectionBuilder newBuilder,
            InputField newFrontWidth, InputField newFrontHeight,
            InputField newTopWidth, InputField newTopHeight,
            InputField newLeftWidth, InputField newLeftHeight,
            InputField newRightWidth, InputField newRightHeight,
            InputField newEyeToFrontDistance)
        {
            projector = newProjector;
            builder = newBuilder;
            frontWidth = newFrontWidth;
            frontHeight = newFrontHeight;
            topWidth = newTopWidth;
            topHeight = newTopHeight;
            leftWidth = newLeftWidth;
            leftHeight = newLeftHeight;
            rightWidth = newRightWidth;
            rightHeight = newRightHeight;
            eyeToFrontDistance = newEyeToFrontDistance;
            PopulateFields();
        }

        private void Start()
        {
            PopulateFields();
        }

        public void CreateOrUpdateOpenCube()
        {
            projector.SetIdealPointToFrontScreenDistance(ParsePositive(eyeToFrontDistance.text, projector.IdealPointToFrontScreenDistance));
            SetSize(ScreenDirection.Front, frontWidth, frontHeight);
            SetSize(ScreenDirection.Top, topWidth, topHeight);
            SetSize(ScreenDirection.Left, leftWidth, leftHeight);
            SetSize(ScreenDirection.Right, rightWidth, rightHeight);
            builder.ArrangeAsOpenCube();
        }

        public void SyncManualScreenAdjustments()
        {
            builder.SyncFromScene();
            PopulateFields();
        }

        public void CreateCompleteSixSidedCube()
        {
            projector.SetIdealPointToFrontScreenDistance(ParsePositive(eyeToFrontDistance.text, projector.IdealPointToFrontScreenDistance));
            SetSize(ScreenDirection.Front, frontWidth, frontHeight);
            SetSize(ScreenDirection.Top, topWidth, topHeight);
            SetSize(ScreenDirection.Left, leftWidth, leftHeight);
            SetSize(ScreenDirection.Right, rightWidth, rightHeight);
            builder.ArrangeAsCompleteCube();
        }

        public void ApplyOnSiteEffectiveSizePreset()
        {
            builder.ApplyOnSiteEffectiveSizePreset();
            PopulateFields();
        }

        private void PopulateFields()
        {
            SetText(ScreenDirection.Front, frontWidth, frontHeight);
            SetText(ScreenDirection.Top, topWidth, topHeight);
            SetText(ScreenDirection.Left, leftWidth, leftHeight);
            SetText(ScreenDirection.Right, rightWidth, rightHeight);
            eyeToFrontDistance.text = projector.IdealPointToFrontScreenDistance.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private void SetSize(ScreenDirection direction, InputField widthInput, InputField heightInput)
        {
            ProjectionScreen screen = Find(direction);
            if (screen == null) return;
            screen.enabled = true;
            screen.width = ParsePositive(widthInput.text, screen.width);
            screen.height = ParsePositive(heightInput.text, screen.height);
        }

        private void SetText(ScreenDirection direction, InputField widthInput, InputField heightInput)
        {
            ProjectionScreen screen = Find(direction);
            if (screen == null) return;
            widthInput.text = screen.width.ToString("0.###", CultureInfo.InvariantCulture);
            heightInput.text = screen.height.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private ProjectionScreen Find(ScreenDirection direction)
        {
            foreach (ProjectionScreen screen in projector.Screens)
                if (screen.direction == direction) return screen;
            return null;
        }

        private static float ParsePositive(string value, float fallback)
        {
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) && parsed > 0f
                ? parsed
                : fallback;
        }
    }
}
