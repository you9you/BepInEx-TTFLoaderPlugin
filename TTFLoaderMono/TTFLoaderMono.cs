extern alias textrender;

// 显式使用 textrender 中的 Font
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
        private static Font dynamicFont;

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

        void Start()
        {
            // 从 Fonts 目录中查找并加载第一个可用字体
            LoadDefaultFontFromDirectory();

            // 订阅场景加载完成事件
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            // 取消订阅，避免内存泄漏
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 延迟一帧，确保新场景的 UI 已完全初始化
            StartCoroutine(ApplyFontAfterDelay());
        }

        private IEnumerator ApplyFontAfterDelay()
        {
            yield return null; // 等待当前帧结束
            yield return null; // 再等一帧（更保险）

            ApplyCustomFontToAllTexts();
        }

        private void ApplyCustomFontToAllTexts()
        {
            var allTexts = FindObjectsOfType<UnityEngine.UI.Text>(true); // includeInactive = true
            foreach (var text in allTexts)
            {
                // 可选：只替换使用默认字体（Arial）的文本
                // if (text.font == null || text.font.name == "Arial" || text.font.name.Contains("Default"))
                // {
                //     text.font = baseFont;
                // }
                text.font = dynamicFont;
            }
        }
        /// <summary>
        /// 从 Fonts 目录中枚举所有字体文件，尝试加载第一个可用的字体作为默认 TMP 字体
        /// </summary>
        private void LoadDefaultFontFromDirectory()
        {
            try
            {
                // 获取所有 .ttf 或 .TTF 文件
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
                    var customFont = LoadTMPTTF(fontName); // 使用 var 或明确的 TMP_FontAsset 类型

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
        /// <returns>Unity Font 对象</returns>
        public Font LoadTTF(string fontName, bool dynamic = false)
        {
            Font font = null;

            // 首先尝试从插件目录中的 Fonts 文件夹加载 .ttf 文件
            string ttfPath = Path.Combine(fontsDirectory, fontName + ".ttf");
            if (!File.Exists(ttfPath))
            {
                ttfPath = Path.Combine(fontsDirectory, fontName + ".TTF");
            }

            if (File.Exists(ttfPath))
            {
                Logger.LogInfo($"Found TTF file: {ttfPath}");
                font = dynamic ? Font.CreateDynamicFontFromOSFont(ttfPath, 16) : new Font(ttfPath);
            }

            if (font != null)
                return font;

            // 如果找不到本地字体，尝试使用系统字体
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

                // 创建 TMP 字体资源
                // FIXME: return null
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
                // 先加载基础 Unity Font
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