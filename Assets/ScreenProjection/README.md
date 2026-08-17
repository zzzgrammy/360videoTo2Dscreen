# 360 video to flat-screen outputs

Add `EquirectangularScreenProjector` to any GameObject and assign its `VideoPlayer` (or a texture). The video must use standard equirectangular 360 mapping (longitude across X, latitude across Y).

For the supplied single-frame test image, use **Tools → 360 To 2D → Use 006.png as Test Frame**. The image lives at `Assets/360/006.png` and is an `8192 × 4096` equirectangular panorama.

Use **Tools → 360 To 2D → Create 006.png Inside-Dome Preview** to create `Inside360Dome_006.mat` and a 20 m-radius sphere with its inner surface visible. The sphere is centred at the default eye point `(0, 1.65, 0)` for immediate inspection from inside.

The world X/Z origin is fixed; set **Ideal Viewing Height** on the projector to place the ideal viewing point at human-eye height (default `1.65 m`), i.e. `(0, 1.65, 0)`. **Ideal Point To Front Screen Distance** defaults to the measured `2.874 m`; the open-cube layout uses it as the perpendicular distance to the Front plane. For every enabled screen, set:

- `centre`: physical centre of the screen in metres relative to origin;
- `eulerAngles`: its image axes; local X is screen-right and local Y is screen-up;
- `width`, `height`, and output resolution.

The default profile enables Front, Top, Left, and Right. Back and Bottom are already present and disabled, so switching to a six-sided configuration requires only enabling them.

Each active direction produces a separate `RenderTexture`, retrievable through `GetOutput(ScreenDirection)`. These are the four real-time 2D video frames; a video encoder (Unity Recorder or FFmpeg) should consume each output RenderTexture to generate MP4 files. Encoding is intentionally separate so it does not constrain the preview or calibration frame rate.

## Visual calibration

`ScreenProjectionBuilder` creates editable Quad GameObjects below the projector (`Screen_Front`, `Screen_Top`, `Screen_Left`, `Screen_Right`). Click **Arrange as Front + Top + Left + Right Open Cube**, then **Create / Update Enabled Screens**. The four planes share their front, top, left and right edges as an open cube around origin. Move/rotate/scale an individual Quad in the Scene view afterwards, then click **Apply Manual Screen Transforms** to copy the adjustment back into projection calibration.

Use **Align Front / Left / Right Bottom Edges to Y = 0** to put the lower edges of the three vertical screens exactly on the world horizontal baseline. It does not change their size, rotation or the top screen.

**Apply On-site Effective Size (mm to m)** uses the latest supplied active areas: Front `7371 x 3500 mm`, Left `4251 x 3500 mm`, Right `4251 x 3500 mm`, Top `7371 x 4251 mm`. It converts to metres, creates the open cube, and aligns the wall bottoms to `Y = 0`; it does not set video output pixel resolution.

`ScreenProjectionControlPanel` is the runtime bridge for eight UGUI InputFields (front/top/left/right width and height). Wire its **CreateOrUpdateOpenCube** method to a button. It creates the four-sided open cube from the entered dimensions; positions remain independently adjustable in the Scene view.

Use **Arrange + Create Complete Six-Sided Cube** (or the runtime **Create Complete 6-Sided Cube** button) to enable Back and Bottom. Back copies Front's size, Bottom copies Top's size, and both are placed on the shared closed-room footprint.

For runtime inspection use **Tools → 360 To 2D → Enable Runtime Mouse Look on Main Camera**. In Play mode, click the Game view to lock the cursor; move the mouse to look around the full 360° horizontally and up/down to inspect the ceiling or floor. Press `Esc` to release the cursor.
