# TTF Font Loader Plugin

一个 BepInEx 插件，用于在 Unity IL2CPP 游戏中直接加载 TTF 字体文件，并自动设置为 TextMesh Pro 的默认字体。

## 功能特性

- 直接加载位于游戏根目录下的 `.ttf` 字体文件
- 自动将找到的第一个 TTF 字体设为 TMP 默认字体
- 支持 TextMesh Pro 字体资源创建
- 兼容系统字体兜底机制（如 Arial、微软雅黑等）
- 解决`XUnity.AutoTranslator 5.4.5`中`OverrideFontTextMeshPro`与`FallbackFontTextMeshPro`失效问题

## 安装方法

1. 将编译好的 `TTFLoader.dll` 放入游戏目录下的 `BepInEx/plugins/` 文件夹中。
2. 将你的 `.ttf` 字体文件放置于游戏根目录下（与游戏主程序同级目录）。

## 使用方式

插件会在启动时自动扫描游戏根目录中的所有 `.ttf` 文件，并尝试将第一个有效的字体设置为 TMP 的默认字体。

例如：
```
游戏目录/
├── Game.exe
├── BepInEx/
│   └── plugins/
│       └── TTFLoader.dll
├── NotoSansSC-Regular.ttf   ← 插件会加载这个字体
├── arialuni.TTF
└── other_font.ttf
```

## 支持的字体格式

- `.ttf` （小写）
- `.TTF` （大写）

## 日志输出

插件通过 BepInEx 的日志系统记录加载过程，可在以下路径查看日志：

```
BepInEx/LogOutput.log
```

示例日志：
```
[Info   : TTF Font Loader] Plugin TTF Font Loader is loaded!
[Info   : TTF Font Loader] Successfully set default TMP font to: NotoSansSC-Regular
```

## 兼容性

- Unity 2023.x
- BepInEx 6.x (IL2CPP)
- TextMesh Pro (TMP)

## API 接口（供其他插件调用）

如果你是开发者，并希望手动加载字体，可以使用如下公共方法：

### 加载 Unity Font

```csharp
Font font = TTFLoaderPlugin.Instance.LoadTTF("NotoSansSC-Regular");
```

### 加载 TMP_FontAsset

```csharp
TMP_FontAsset tmpFont = TTFLoaderPlugin.Instance.LoadTMPTTF("NotoSansSC-Regular");
```

> ⚠️ 注意：这些方法需要确保字体文件存在于游戏根目录中。

## 注意事项

- 若未找到任何 `.ttf` 文件，插件将不会更改默认字体。
- 插件不会覆盖已有的 TMP 设置，除非成功加载了新的默认字体。
- 插件目前仅支持设置全局默认字体，不支持针对特定 UI 组件的字体替换。
- 仅在`2023.2.20f1 IL2CPP`中测试，其他版本请自行测试。
