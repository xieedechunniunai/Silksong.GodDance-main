# Silksong.GodDance - 机驱舞神模组 / Silksong.GodDance - CogWork Dancers Mod

[English](#english) | [中文](#中文)

<a name="中文"></a>
## 🌟 项目简介

**Silksong.GodDance** 是一个为《空洞骑士：丝之歌》游戏设计的Boss战斗增强模组。该模组重新设计了游戏中的"机驱舞者"（CogWork Dancers）Boss战，增加了新的攻击模式、难度调整和独特的存档切换机制。

### ✨ 主要特性

- **智能存档切换系统** - 在Boss房间内通过演奏乐器自动切换专用存档
- **多阶段战斗增强** - 为Boss的每个阶段添加了新的攻击模式和机制
- **动态难度调整** - 根据战斗阶段自动调整Boss属性和行为
- **资源持久化管理** - 智能的资源加载和缓存系统
- **多语言支持** - 支持中英文界面显示

### 🎮 安装说明

#### 系统要求
- 《空洞骑士：丝之歌》游戏本体
- BepInEx 模组加载器
- .NET Framework 4.8+

#### 安装步骤
1. 下载最新版本的模组文件
2. 将模组文件复制到游戏的BepInEx插件目录
3. 启动游戏，模组将自动加载

### 🔧 使用说明

#### 进入Boss战
1. 前往机驱舞者Boss房间（Cog_Dancers）
2. 在房间内使用乐器演奏至少3秒钟
3. 模组将自动切换到专用Boss存档
4. 开始挑战增强版的机驱舞者Boss

#### 战斗特性
- **第一阶段**：基础攻击模式，相对简单
- **第二阶段**：新增环绕攻击和伤害区域
- **第三阶段**：隐身机制和高速移动
- **第四阶段**：终极挑战模式

### 🛠️ 技术架构

#### 核心组件
- **Plugin.cs** - 主插件入口，负责模组初始化和场景管理
- **GodDance.cs** - Boss行为控制器，管理战斗逻辑
- **SaveSwitchManager.cs** - 存档切换系统
- **AssetManager.cs** - 资源管理器

#### 关键特性
- 基于Harmony的代码修补
- 异步资源加载和缓存
- 事件驱动的状态管理
- 持久化数据存储

### 📁 项目结构
Silksong.GodDance-main/ 
├── Assets/ 
│ └── GodDance.dat # Boss专用存档文件 
├── Source/ 
│ ├── Plugin.cs # 主插件类 
│ ├── Behaviours/ 
│ │ ├── GodDance.cs # Boss行为控制器 
│ │ ├── SaveSwitchManager.cs # 存档切换管理器 
│ │ └── singleGodDance.cs # 单个舞者控制器 
│ ├── Patches/ 
│ │ └── GodDancePatches.cs # Harmony补丁 
│ ├── AssetManager.cs # 资源管理器 
│ ├── DamageHero.cs # 伤害处理 
│ ├── Log.cs # 日志系统 
│ └── PreloadOperation.cs # 预加载操作 
├── CHANGELOG.md # 更新日志 
├── LICENSE.md # 许可证文件 
└── README.md # 项目说明

### 🐛 故障排除

#### 常见问题
1. **模组未加载**
   - 检查BepInEx是否正确安装
   - 确认模组文件放置在正确目录

2. **存档切换失败**
   - 确保在Boss房间内演奏乐器
   - 检查游戏存档权限

3. **资源加载错误**
   - 重启游戏尝试重新加载
   - 检查游戏文件完整性

### 🤝 贡献指南

欢迎提交Issue和Pull Request来改进这个模组！

### 📄 许可证

本项目采用MIT许可证 - 详见 [LICENSE.md](LICENSE.md) 文件。

### 📞 联系方式

- GitHub仓库: [https://github.com/xieedechunniunai/Silksong.GodDance-main](https://github.com/xieedechunniunai/Silksong.GodDance-main)
- 问题反馈: 请在GitHub Issues中提交

---

<a name="english"></a>
# Silksong.GodDance - CogWork Dancers Mod

## 🌟 Project Introduction

**Silksong.GodDance** is a Boss battle enhancement mod for the game "Hollow Knight: Silksong". This mod redesigns the "CogWork Dancers" Boss fight, adding new attack patterns, difficulty adjustments, and a unique save switching mechanism.

### ✨ Key Features

- **Intelligent Save Switching System** - Automatically switch to dedicated Boss saves by playing instruments in the Boss room
- **Multi-phase Battle Enhancement** - Adds new attack patterns and mechanics for each Boss phase
- **Dynamic Difficulty Adjustment** - Automatically adjusts Boss attributes and behavior based on combat phase
- **Resource Persistence Management** - Intelligent resource loading and caching system
- **Multi-language Support** - Supports both English and Chinese interface display

### 🎮 Installation Instructions

#### System Requirements
- "Hollow Knight: Silksong" game
- BepInEx mod loader
- .NET Framework 4.8+

#### Installation Steps
1. Download the latest version of the mod files
2. Copy mod files to the game's BepInEx plugins directory
3. Launch the game, the mod will load automatically

### 🔧 Usage Instructions

#### Entering Boss Battle
1. Go to the CogWork Dancers Boss room (Cog_Dancers)
2. Play an instrument in the room for at least 3 seconds
3. The mod will automatically switch to the dedicated Boss save
4. Begin challenging the enhanced CogWork Dancers Boss

#### Battle Features
- **Phase 1**: Basic attack patterns, relatively simple
- **Phase 2**: New surrounding attacks and damage zones
- **Phase 3**: Stealth mechanics and high-speed movement
- **Phase 4**: Ultimate challenge mode

### 🛠️ Technical Architecture

#### Core Components
- **Plugin.cs** - Main plugin entry, responsible for mod initialization and scene management
- **GodDance.cs** - Boss behavior controller, manages combat logic
- **SaveSwitchManager.cs** - Save switching system
- **AssetManager.cs** - Resource manager

#### Key Features
- Harmony-based code patching
- Asynchronous resource loading and caching
- Event-driven state management
- Persistent data storage

### 📁 Project Structure

Silksong.GodDance-main/ 
├── Assets/ 
│ └── GodDance.dat # Boss-specific save file 
├── Source/ 
│ ├── Plugin.cs # Main plugin class 
│ ├── Behaviours/ 
│ │ ├── GodDance.cs # Boss behavior controller 
│ │ ├── SaveSwitchManager.cs # Save switching manager 
│ │ └── singleGodDance.cs # Single dancer controller 
│ ├── Patches/ 
│ │ └── GodDancePatches.cs

### 🐛 Troubleshooting

#### Common Issues
1. **Mod Not Loading**
   - Check if BepInEx is correctly installed
   - Confirm mod files are placed in the correct directory

2. **Save Switching Failed**
   - Ensure you're playing an instrument in the Boss room
   - Check game save permissions

3. **Resource Loading Errors**
   - Restart the game to try reloading
   - Check game file integrity

### 🤝 Contribution Guidelines

Welcome to submit Issues and Pull Requests to improve this mod!

### 📄 License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

### 📞 Contact

- GitHub Repository: [https://github.com/xieedechunniunai/Silksong.GodDance-main](https://github.com/xieedechunniunai/Silksong.GodDance-main)
- Issue Reporting: Please submit via GitHub Issues
