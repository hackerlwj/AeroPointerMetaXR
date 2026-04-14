# 🚁 AeroPointerMetaXR (空中指针)

> **AeroPointerMetaXR** 是一个基于 Meta XR 平台的混合现实 (MR) 空间交互系统。本项目致力于探索在三维空间中对飞行器（无人机）等目标进行直觉化、高沉浸式的交互、指引与数据可视化。

## 📖 项目简介

AeroPointer (空中指针) 旨在打破传统的二维控制界面，利用 Meta Quest 系列头显的空间计算能力，构建一个全新的混合现实交互前端。本项目利用 Unity 引擎进行开发，并大量使用了自定义 Shader (ShaderLab/HLSL) 来实现高性能的底层图形渲染（如空间全息指针、点云或三维高斯溅射渲染展示）。

## ✨ 核心特性

- **🥽 深度空间交互**: 集成 Meta XR SDK，支持高精度的裸手追踪 (Hand Tracking) 与手柄交互，实现自然的三维空间目标指引。
- **⚡ 高性能定制渲染**: 包含大量的自定义 ShaderLab 与 HLSL 代码，专为移动端 XR 设备（如 Quest 3）优化，确保沉浸式体验下的高帧率渲染。
- **🚁 无人机/实体交互模型**: 为“空中指针”概念设计的交互逻辑，可用于无人机航线规划、空间目标标定或实时三维环境地图的回放展示。
- **🛠️ 跨系统集成准备**: 前端逻辑与视图分离，为后续通过 ROS/TCP/UDP 接入真实无人机飞控系统或建图后端（如 3D Gaussian Splatting）预留了接口。

## 🛠️ 技术栈

- **游戏引擎**: Unity 3D (推荐 2022.3 LTS 及以上版本)
- **开发语言**: C# (核心交互逻辑)
- **图形渲染**: ShaderLab, HLSL, Wolfram Language (用于复杂算法或着色器原型推导)
- **XR 框架**: Meta XR All-in-One SDK, XR Interaction Toolkit

## 📂 项目结构

```text
AeroPointerMetaXR/
├── Assets/              # 游戏资源目录（包含核心场景、C#脚本、材质、Shader等）
├── Packages/            # Unity 依赖包管理配置 (包含 Meta XR 插件等)
├── ProjectSettings/     # Unity 工程核心设置 (图形 API、XR 配置、物理层等)
├── .vscode/             # 编辑器配置
└── README.md            # 项目说明文档

```
## 🚀 快速开始

### 1. 环境准备

- 下载并安装 **Unity Hub** 以及 **Unity 2022.3 LTS** (或更高版本)。
- 在安装 Unity 时，请务必勾选 **Android Build Support** 及其子选项 (OpenJDK, Android SDK & NDK Tools)，因为 Meta Quest 基于 Android 系统。
- 准备一台 Meta Quest 设备 (Quest 2 / Quest 3 / Quest Pro)，并开启开发者模式。

### 2. 获取项目

```bash
git clone https://github.com/hackerlwj/AeroPointerMetaXR.git
```

### 3. 在 Unity 中打开

- 打开 Unity Hub，点击 **Add**，选择刚刚克隆的 `AeroPointerMetaXR` 文件夹。
- 首次打开项目时，Unity 会自动解析并下载 `Packages` 目录下的依赖（如 Meta XR SDK），这可能需要几分钟时间。

### 4. 编译与运行

1. 在 Unity 编辑器中，进入 `File -> Build Settings`。
2. 将 **Platform** 切换为 **Android**，点击 `Switch Platform`。
3. 将 Meta Quest 设备通过数据线连接到电脑。
4. 在 `Run Device` 中选择你的 Quest 设备。
5. 点击 `Build And Run`，编译生成的 APK 将自动部署并在头显中运行。

## 🧑‍💻 开发者 / 团队

- **hackerlwj** - 主导开发与图形学渲染逻辑设计

## 📄 许可证

本项目采用 MIT 许可证 - 详情请参阅 LICENSE 文件。

---

If you have any questions or suggestions, feel free to open an issue or pull request!
```
