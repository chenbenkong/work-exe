# WorkExe 桌面互动人物

一个基于 C# / WPF / .NET Framework 4.8 的 Windows 桌面宠物/互动人物程序。上传老板照片后，可将其作为人物头像，并通过右键菜单触发各种互动玩法。

## 在线仓库

https://github.com/chenbenkong/work-exe

## 功能特性

- 透明无边框窗口，默认位于主显示器底部中央
- 左键拖拽，松开停留，限制不可拖出工作区
- 支持小 / 中 / 大三种尺寸
- 始终置顶、暂时隐藏、托盘恢复
- 右键圆角菜单：鞭子抽打、大炮惩罚、召唤牛来、说“我错了”、磕头、原地待命、桌面爬行、调整大小、始终置顶、暂时隐藏、恢复默认、退出
- 粉色气泡台词，靠近边缘自动向内展开
- 全局快捷键 `Ctrl + Alt + Shift + Q`：紧急恢复
- 退出时自动清理临时窗口、鼠标监听、托盘图标和动画定时器

## 快速开始

### 方式一：直接下载 Release（推荐）

1. 打开仓库 Actions 页面，找到最新成功的构建。
2. 下载 `WorkExe-Release-Zip` 工件。
3. 解压后双击 `Run.bat` 即可运行。

### 方式二：本地构建

1. 克隆本仓库：
   ```bash
   git clone https://github.com/chenbenkong/work-exe.git
   cd work-exe
   ```
2. 安装依赖：
   - Visual Studio 2019/2022 或 Build Tools（带 MSBuild）
   - Python 3.x + Pillow
   - nuget.exe
3. 双击 `Build-and-Run.bat`，脚本会依次生成素材、还原包、编译、运行。

## 替换老板照片

1. 准备一张正面清晰照片，命名为 `boss.png`。
2. 放到项目根目录的 `assets/` 文件夹下，覆盖默认占位图。
3. 重新运行 `scripts/generate_assets.py` 或 `Build-and-Run.bat` 即可重新生成带该头像的动作素材。

## 修改台词

编辑项目根目录的 `config.json`：

- `HitLines`：被鞭子/点击时的台词
- `CannonChargeLines`：大炮蓄力时的三段台词
- `CowLines`：牛出现时的台词
- `SorryLines`：说“我错了”时的台词

修改后重新运行程序即可生效（无需重新编译）。

## 测试报告

构建脚本与 GitHub Actions 会执行编译自检。程序启动后建议验证：

1. 透明显示与拖拽
2. 右键圆角菜单弹出
3. 说“我错了”、磕头、爬行动作
4. 鞭子模式：鼠标跟随，点击人物触发受击
5. 大炮模式：空格蓄力，三段台词，松开发射飞出屏幕
6. 牛模式：牛从对侧出现并撞飞人物
7. Esc / 右键退出玩法
8. `Ctrl + Alt + Shift + Q` 紧急恢复
9. 托盘图标右键显示/隐藏/恢复/退出
10. 退出后进程完全清理

## 已知限制

- 本版本使用程序生成的占位动作素材。真实照片生成全套动作需要配合 AI 图像模型或手工绘制素材替换 `WorkExe/Assets/` 下的 PNG。
- 多显示器、窗口边缘识别、攀爬跟随窗口、血迹伤口等不在实现范围内。

## 目录结构

```
work-exe/
├── .github/workflows/build.yml   # GitHub Actions 自动构建
├── WorkExe/                      # WPF 项目源码
│   ├── MainWindow.xaml(.cs)      # 主窗口与交互逻辑
│   ├── CharacterEngine.cs        # 人物状态/帧管理
│   ├── TrayManager.cs            # 托盘图标
│   ├── NativeMethods.cs          # Win32 API 封装
│   ├── Config.cs                 # 配置读写
│   ├── Assets/                   # 生成的素材
│   └── ...
├── scripts/generate_assets.py    # 素材生成脚本
├── assets/boss.png               # 老板照片（可替换）
├── config.json                   # 运行时配置
├── Run.bat                       # 运行脚本
├── Build-and-Run.bat             # 构建并运行脚本
└── README.md                     # 本文件
```

## 最终 EXE 路径

本地构建：

```
WorkExe\bin\Release\WorkExe.exe
```

Release 工件：

```
WorkExe-Release.zip/WorkExe/WorkExe.exe
```
