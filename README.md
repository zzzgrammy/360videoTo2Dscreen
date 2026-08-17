# 360cube

> 将等距柱状（Equirectangular）360° 全景内容，实时映射到自定义物理尺寸的六面 Cube，并导出为六路 2D 图片、图片序列或 MP4 视频。

![360cube 工作流程](Docs/images/360cube-workflow.svg)

## 功能

- 创建可配置宽度、深度、高度的 `Cube0` 六面屏幕。
- 支持 360 全景图片、视频与图片序列帧输入。
- 使用当前相机作为最佳观测点；移动观测点后，六面画面实时同步更新。
- 六面预览与球幕使用同一套世界空间射线投影，接缝连续。
- 导出独立的 `Front`、`Back`、`Left`、`Right`、`Top`、`Bottom` 输出。
- 图片导出为 PNG，序列帧导出为六路 PNG 序列，视频使用 Unity Recorder 录制六路 MP4。
- 视频尺寸自动补齐到偶数像素，兼容 H.264 / MP4 编码要求。

## 环境要求

- Unity `2022.3 LTS`
- Universal Render Pipeline `14.x`
- 视频 MP4 导出另需安装 [Unity Recorder](https://docs.unity3d.com/Packages/com.unity.recorder@4.0/manual/index.html) `4.0.3` 或兼容版本

## 快速开始

1. 将本项目中的 `Assets/ScreenProjection` 导入 Unity 项目。
2. 若需要导出 MP4，在 `Window → Package Manager` 安装 **Recorder**。
3. 从 Unity 顶部菜单打开 `GM → 360cube`。
4. 选择输入类型：`360 Image`、`360 Video` 或 `Image Sequence`。
5. 输入 Cube0 的物理尺寸：宽度 `X`、深度 `Z`、高度 `Y`，然后点击 **Create Cube0 (Six Faces)**。
6. 把主相机移动到最佳观测点。预览与最终生成都使用此刻相机位置，不会自动重置。
7. 选择生成方式：
   - 图片：**Generate Six PNG Images**
   - 序列帧：**Generate Six PNG Sequences**
   - 视频：进入 Play 模式，点击 **Start Six MP4 Recordings**；达到所需时长后点击停止。

## 投影原理

对每一个六面墙像素，插件都计算从当前相机位置指向该墙面世界坐标点的方向，并以这个方向采样 360 全景图。

```text
当前相机（观测点） → 墙面像素的世界坐标 → 360 全景图采样
```

因此，六块墙面并不是由六张独立的静态贴图拼接而成；相机移动后，它们会重新按同一个观测点取样。只要在该观测点观察，六面屏幕呈现的结果就与置身同一张 360 球幕中一致。

## 输出文件

输出目录自动创建在：

```text
Assets/StreamingAssets/360TurnTo2D/<时间戳>/
```

示例：

```text
20260817_153000_Front.png
20260817_153000_Back.png
20260817_153000_Left.png
20260817_153000_Right.png
20260817_153000_Top.png
20260817_153000_Bottom.png
```

图片序列会在各方向目录中按帧号保存。视频录制为手动停止模式：点击停止后 Recorder 才会写入并封装最终 MP4 文件。

## 仓库结构

```text
Assets/ScreenProjection/
├── Editor/       # GM → 360cube 编辑器窗口与导出工具
├── Materials/    # 预览材质
├── Scripts/      # 六面布局、实时投影与相机控制
└── Shaders/      # 360 等距柱状投影 Shader
Docs/images/      # README 图片
```

## 发布为 `.unitypackage`

在 Unity 的 Project 面板中选中 `Assets/ScreenProjection`，选择：

```text
Assets → Export Package… → Include dependencies → Export
```

建议将导出的 `360cube.unitypackage` 附加到 GitHub Release，方便不使用 Git 的用户下载导入。请在 Release 说明中注明：视频 MP4 导出需要用户自行安装 Unity Recorder。

## 上传到 GitHub

### 1. 在 GitHub 创建空仓库

在 GitHub 点击 **New repository**，例如命名为 `360cube`。创建时不要勾选 README、`.gitignore` 或 License，因为本地已经准备好前两项。

### 2. 在项目根目录执行

```powershell
git init
git add .
git commit -m "Initial release of 360cube"
git branch -M main
git remote add origin https://github.com/<你的用户名>/360cube.git
git push -u origin main
```

如果 GitHub 使用 SSH，将远程地址替换为：

```powershell
git remote add origin git@github.com:<你的用户名>/360cube.git
```

### 3. 创建可下载版本（推荐）

1. 在 Unity 导出 `360cube.unitypackage`。
2. 在 GitHub 仓库右侧点击 **Releases → Draft a new release**。
3. 输入标签，例如 `v0.1.0`，并填写版本说明。
4. 将 `.unitypackage` 拖入附件区域，最后点击 **Publish release**。

## 注意事项

- 360 输入必须是标准等距柱状全景：横向 360°，纵向 180°。
- 观测点是画面连续性的基准；从其他位置观看平面会产生正常的透视差。
- 视频录制需要在 Play 模式运行，并由用户手动停止。
- 生成目录已在 `.gitignore` 中忽略，避免将大体积结果文件提交到 Git。
- 本仓库尚未声明许可证；公开发布前请根据你的发布意图添加 `LICENSE` 文件。
