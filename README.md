# TTF Font Loader Plugin

此 BepInEx 插件可让 Unity IL2CPP/Mono 游戏直接加载并使用 TTF 字体文件，以实现游戏内字体的替换显示。


## 功能特性

- 直接加载位于游戏根目录下的 `.ttf` 字体文件
- 自动将找到的第一个 TTF 字体设为 TMP 默认字体
- 支持 TextMesh Pro 字体资源创建
- 兼容系统字体兜底机制（如 Arial、微软雅黑等）
- 解决`XUnity.AutoTranslator 5.4.5`中`OverrideFontTextMeshPro`与`FallbackFontTextMeshPro`失效问题

## 确认你的目标游戏使用的是`Mono`还是`IL2CPP`： 

你可以通过检查游戏目录中的 `GameAssembly.dll` 来判断： 
- 如果存在`GameAssembly.dll` → IL2CPP
- 如果存在`Managed`文件夹和`.dll` 文件 → Mono

## 安装方法

1. 根据游戏类型选择对应版本`IL2CPP`或`Mono`。
2. 将编译好的 `TTFLoader-<IL2CPP/Mono>.dll` 放入游戏目录下的 `BepInEx/plugins/` 文件夹中。
3. 将你的 `.ttf` 字体文件放置于游戏根目录下（与游戏主程序同级目录）。

## 使用方式

插件会在启动时自动扫描游戏根目录中的所有 `.ttf` 文件，并尝试将第一个有效的字体设置为 TMP 的默认字体。

例如：
```
游戏目录/
├── Game.exe
├── BepInEx/
│   └── plugins/
│       └── TTFLoader-<IL2CPP/Mono>.dll
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

- Unity 2021.x
- Unity 2023.x
- BepInEx 6.x (IL2CPP)
- BepInEx 5.x (Mono)
- TextMesh Pro (TMP)

> ⚠️ 因`XUnity.AutoTranslator (Mono)`尚不支持`BepInEx 6.x (Mono)`，因此暂未适配。

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
- IL2CPP版本会直接设置为 TextMesh Pro 的默认字体。
- Mono版本在 TMP 失效时，会使用`UI.Text`加载字体。
- 插件目前仅支持设置全局默认字体，不支持针对特定 UI 组件的字体替换。
- 仅在`2023.2.20f1 BepInEx6.0.0-be.738 IL2CPP` `2021.3.15f1 BepInEx5.4.23.4 Mono`中测试，其他版本请自行测试。
