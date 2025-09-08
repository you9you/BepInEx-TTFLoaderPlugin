using BepInEx;
using BepInEx.Unity.IL2CPP;
using System;
using System.IO;
using UnityEngine;
using TMPro;

namespace TTFLoader
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class TTFLoaderPlugin : BasePlugin
    {
        public const string PLUGIN_GUID = "com.github.you9you.ttfloader";
        public const string PLUGIN_NAME = "TTF Font Loader";
        public const string PLUGIN_VERSION = "1.0.0";

        private static string fontsDirectory;

        public override void Load()
        {
            Log.LogInfo($"Plugin {PLUGIN_NAME} is loaded!");

            // 初始化字体目录路径
            // fontsDirectory = Path.Combine(Paths.PluginPath, "Fonts");
            // if (!Directory.Exists(fontsDirectory))
            // {
            //     Directory.CreateDirectory(fontsDirectory);
            //     Log.LogWarning($"Fonts directory created: {fontsDirectory}");
            // }


            // 使用游戏根目录作为字体加载路径
            fontsDirectory = Paths.GameRootPath;


            // 从 Fonts 目录中查找并加载第一个可用字体
            LoadDefaultFontFromDirectory();

            Log.LogInfo($"TTF Loader initialized. Fonts directory: {fontsDirectory}");
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
                    Log.LogWarning("No TTF font files found in the Fonts directory.");
                    return;
                }

                foreach (string ttfPath in fontFiles)
                {
                    string fontName = Path.GetFileNameWithoutExtension(ttfPath);
                    TMP_FontAsset customFont = LoadTMPTTF(fontName);

                    if (customFont != null)
                    {
                        TMP_Settings.defaultFontAsset = customFont;
                        Log.LogInfo($"Successfully set default TMP font to: {fontName}");
                        return; // 成功加载一个就退出
                    }
                    else
                    {
                        Log.LogWarning($"Failed to load font: {fontName}, trying next...");
                    }
                }

                Log.LogError("Failed to load any font from the Fonts directory.");
            }
            catch (Exception ex)
            {
                Log.LogError($"Error loading default font from directory: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 加载 TTF 字体文件并返回 Unity Font 对象
        /// </summary>
        /// <param name="fontName">字体文件名（不含扩展名）</param>
        /// <returns>Unity Font 对象</returns>
        public Font LoadTTF(string fontName)
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
                Log.LogInfo($"Found TTF file: {ttfPath}");
                font = new Font(ttfPath); // 使用本地字体文件创建 Font 对象
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
                "SimHei"
            };

            foreach (string variant in variants)
            {
                font = Font.CreateDynamicFontFromOSFont(variant, 12);
                if (font != null)
                {
                    Log.LogInfo($"Loaded system font variant: {variant}");
                    return font;
                }
            }

            // 最终兜底方案：使用默认 Arial 字体
            Log.LogWarning($"Using fallback font 'Arial' for: {fontName}");
            return Font.CreateDynamicFontFromOSFont("Arial", 12);
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
                    Log.LogError($"Failed to load base font: {fontName}");
                    return null;
                }

                // 创建 TMP 字体资源
                TMP_FontAsset tmpFont = TMP_FontAsset.CreateFontAsset(baseFont);

                if (tmpFont == null)
                {
                    Log.LogError($"TMP_FontAsset.CreateFontAsset returned null for: {fontName}");
                    return null;
                }

                tmpFont.name = fontName;
                Log.LogInfo($"Successfully created TMP font: {fontName}");
                return tmpFont;
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to create TMP font {fontName}: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }
    }
}