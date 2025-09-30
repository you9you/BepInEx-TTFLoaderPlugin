// 外部别名引用：允许引用来自不同程序集的相同命名空间下的类型，解决冲突。
// 这里的 'textrender' 别名很可能用于区分特定的 Unity.Font 类型。
extern alias textrender;

// 显式使用 textrender 外部别名中的 UnityEngine.Font 类型，将其命名为 Font。
// 这确保了代码中使用的是这个特定程序集（可能是某个插件或目标游戏依赖）的 Font 类型。
using Font = textrender::UnityEngine.Font;


using BepInEx;

// if BEPINEX_V6
// using BepInEx.Unity.Mono;


using System;
using System.IO;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

namespace TTFLoaderMono
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class TTFLoaderPlugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "com.github.you9you.ttfloader";
        public const string PLUGIN_NAME = "TTF Font Loader (Mono)";
        public const string PLUGIN_VERSION = "1.1.0";

        private static string fontsDirectory;

        // 用于替换 UnityEngine.UI.Text 组件的字体
        private static Font dynamicFont;

        /// <summary>
        /// BepInEx 插件初始化时调用，类似于 Unity 的 Awake
        /// </summary>
        void Awake()
        {
            Logger.LogInfo($"Plugin {PLUGIN_NAME} is loaded!");

            // 初始化字体目录路径
            // fontsDirectory = Path.Combine(BepInEx.Paths.PluginPath, "Fonts");
            // if (!Directory.Exists(fontsDirectory))
            // {
            //     Directory.CreateDirectory(fontsDirectory);
            //     Logger.LogWarning($"Fonts directory created: {fontsDirectory}");
            // }

            // 使用游戏根目录作为字体加载路径
            fontsDirectory = BepInEx.Paths.GameRootPath;

            Logger.LogInfo($"TTF Loader initialized. Fonts directory: {fontsDirectory}");
        }

        /// <summary>
        /// 插件启动时调用，类似于 Unity 的 Start
        /// </summary>
        void Start()
        {
            // 从预设的目录中查找并加载第一个可用的字体
            LoadDefaultFontFromDirectory();

            // 订阅场景加载完成事件，以便在新场景加载后应用字体
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        /// <summary>
        /// 插件销毁时调用，用于资源清理
        /// </summary>
        void OnDestroy()
        {
            // 取消订阅场景加载事件，避免内存泄漏或在插件卸载后调用不存在的方法
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        /// <summary>
        /// 场景加载完成事件的处理函数
        /// </summary>
        /// <param name="scene">已加载的场景</param>
        /// <param name="mode">场景加载模式</param>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 启动协程，延迟应用字体，确保新场景的 UI 组件已完全初始化
            StartCoroutine(ApplyFontAfterDelay());
        }

        /// <summary>
        /// 延迟应用字体的协程
        /// </summary>
        private IEnumerator ApplyFontAfterDelay()
        {
            yield return null; // 等待当前帧结束（等待 Unity UI 初始化）
            yield return null; // 再等一帧（更保险，确保所有 Start/Awake 完成）

            // 延迟后，对场景中的所有 Text 组件应用自定义字体
            ApplyCustomFontToAllTexts();
        }

        /// <summary>
        /// 查找场景中所有的 UnityEngine.UI.Text 组件并应用加载的动态字体
        /// </summary>
        private void ApplyCustomFontToAllTexts()
        {
            if (dynamicFont != null)
            {
                // 查找所有激活或非激活的 UnityEngine.UI.Text 组件
                var allTexts = FindObjectsOfType<UnityEngine.UI.Text>(true); // includeInactive = true
                foreach (var text in allTexts)
                {
                    // 可选：只替换使用默认字体（Arial）的文本
                    // if (text.font == null || text.font.name == "Arial" || text.font.name.Contains("Default"))
                    // {
                    //     text.font = baseFont;
                    // }

                    // 将所有找到的 UI.Text 组件的字体设置为加载的动态字体
                    text.font = dynamicFont;
                }
            }
        }

        /// <summary>
        /// 从预设的字体目录中枚举所有 TTF 文件，尝试加载第一个可用的字体
        /// 并将其用作默认的 TMP 字体或动态字体。
        /// </summary>
        private void LoadDefaultFontFromDirectory()
        {
            try
            {
                // 获取所有 .ttf 或 .TTF 文件（仅在顶层目录查找）
                string[] fontFiles = Directory.GetFiles(fontsDirectory, "*.ttf", SearchOption.TopDirectoryOnly);
                if (fontFiles.Length == 0)
                {
                    fontFiles = Directory.GetFiles(fontsDirectory, "*.TTF", SearchOption.TopDirectoryOnly);
                }

                if (fontFiles.Length == 0)
                {
                    Logger.LogWarning("No TTF font files found in the Fonts directory.");
                    return;
                }

                foreach (string ttfPath in fontFiles)
                {
                    string fontName = Path.GetFileNameWithoutExtension(ttfPath);
                    var customFont = LoadTMPTTF(fontName); // 尝试加载并创建 TMP_FontAsset

                    if (customFont != null)
                    {
                        // 注意：Mono 环境下无法为属性或索引器“TMP_Settings.defaultFontAsset”赋值 - 它是只读的
                        // TMP_Settings.defaultFontAsset = customFont;
                        TextMeshProUGUI textMeshProText = GetComponent<TextMeshProUGUI>();
                        textMeshProText.font = customFont;
                        Logger.LogInfo($"Successfully set default TMP font to: {fontName}");
                        return; // 成功加载一个就退出
                    }
                    else
                    {
                        Logger.LogWarning($"Failed to load font: {fontName}, trying next...");
                    }
                }

                Logger.LogError("Failed to load any font from the Fonts directory.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error loading default font from directory: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 加载 TTF 字体文件并返回 Unity Font 对象
        /// </summary>
        /// <param name="fontName">字体文件名（不含扩展名）</param>
        /// <param name="dynamic">是否创建动态字体（使用 CreateDynamicFontFromOSFont）</param>
        /// <returns>Unity Font 对象</returns>
        public Font LoadTTF(string fontName, bool dynamic = false)
        {
            Font font = null;

            // 尝试从指定的字体目录加载 .ttf 文件
            string ttfPath = Path.Combine(fontsDirectory, fontName + ".ttf");
            if (!File.Exists(ttfPath))
            {
                ttfPath = Path.Combine(fontsDirectory, fontName + ".TTF");
            }

            if (File.Exists(ttfPath))
            {
                Logger.LogInfo($"Found TTF file: {ttfPath}");
                // 根据 dynamic 参数创建字体：动态字体（OSFont）或普通字体（从文件路径）
                font = dynamic ? Font.CreateDynamicFontFromOSFont(ttfPath, 16) : new Font(ttfPath);
            }

            if (font != null)
                return font;

            // 如果找不到本地字体文件，尝试使用系统字体
            string[] variants = {
                fontName,
                $"{fontName}-Regular",
                $"{fontName} Regular",
                "Noto Sans SC",
                "Microsoft YaHei",
                "SimHei",
                "Arial"
            };

            foreach (string variant in variants)
            {
                // 尝试从系统加载动态字体（大小设为 16）
                font = Font.CreateDynamicFontFromOSFont(variant, 16);
                if (font != null)
                {
                    font.name = variant;
                    Logger.LogInfo($"Loaded system font variant: {variant}");
                    return font;
                }
            }

            // 最终兜底方案：使用默认 Arial 字体
            Logger.LogWarning($"Using fallback font 'Arial' for: {fontName}");
            Font fallbackFonts = Font.CreateDynamicFontFromOSFont("Arial", 16);
            if (fallbackFonts != null)
            {
                fallbackFonts.name = "Arial";
                return fallbackFonts;
            }

            // 彻底失败
            return null;
        }

        /// <summary>
        /// 加载 TTF 字体并创建 TMP_FontAsset 对象
        /// </summary>
        /// <param name="fontName">字体文件名（不含扩展名）</param>
        /// <returns>TMP_FontAsset 对象</returns>
        public TMP_FontAsset LoadTMPTTF(string fontName)
        {
            try
            {
                // 先加载基础 Unity Font
                Font baseFont = LoadTTF(fontName);
                if (baseFont == null)
                {
                    Logger.LogError($"Failed to load base font: {fontName}");
                    return null;
                }

                // 创建 TMP 字体资源 (可能为null)
                TMP_FontAsset tmpFont = TMP_FontAsset.CreateFontAsset(baseFont);

                if (tmpFont == null)
                {
                    Logger.LogError($"TMP_FontAsset.CreateFontAsset returned null for: {fontName}");
                    return null;
                }

                tmpFont.name = fontName;
                Logger.LogInfo($"Successfully created TMP font: {fontName}");
                return tmpFont;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to create TMP font {fontName}: {ex.Message}\n{ex.StackTrace}");

                Logger.LogInfo("Trying use UI.Text");
                // 尝试使用 LoadTTF 加载动态字体
                Font baseFont = LoadTTF(fontName, true);
                if (baseFont == null)
                {
                    Logger.LogError($"Failed to load base font: {fontName}");
                    return null;
                }
                dynamicFont = baseFont;
                ApplyCustomFontToAllTexts();
                return null;
            }
        }
    }
}