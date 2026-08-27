using BepInEx;
using BepInEx.Unity.IL2CPP;
using System;
using System.IO;
using UnityEngine;
using TMPro;

namespace TTFLoaderIL2CPP
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class TTFLoaderPlugin : BasePlugin
    {
        public const string PLUGIN_GUID = "com.github.you9you.ttfloader";
        public const string PLUGIN_NAME = "TTF Font Loader (IL2CPP)";
        public const string PLUGIN_VERSION = "1.1.0";

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
                        // 直接赋值（若该 TMP 版本的 defaultFontAsset 属性可写）
                        bool ok = false;
                        try
                        {
                            TMP_Settings.defaultFontAsset = customFont;
                            ok = true;
                        }
                        catch (System.Exception ex)
                        {
                            Log.LogInfo($"Direct assignment failed, using reflection: {ex.Message}");
                        }

                        // 反射回退：写入 TMP_Settings 的私有背板字段
                        if (!ok)
                        {
                            ok = SetTMPDefaultFontViaReflection(customFont);
                        }

                        if (ok)
                        {
                            Log.LogInfo($"Successfully set default TMP font to: {fontName}");
                            return; // 成功加载一个就退出
                        }

                        Log.LogWarning($"Could not set default TMP font for: {fontName}");
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
        /// 通过反射写入 TMP_Settings 的私有背板字段，设置默认字体。
        ///
        /// TMP_Settings.defaultFontAsset 属性在部分 TMP 版本中只读（无 setter），
        /// 直接赋值会失败。此时通过反射写入其背板字段 m_defaultFontAsset。
        /// 字段名已通过对 Unity.TextMeshPro.dll 元数据的分析确认。
        /// </summary>
        /// <param name="fontAsset">要设置的 TMP_FontAsset</param>
        /// <returns>是否设置成功</returns>
        private bool SetTMPDefaultFontViaReflection(TMP_FontAsset fontAsset)
        {
            try
            {
                var settingsType = typeof(TMPro.TMP_Settings);

                // 候选字段名（不同 TMP 版本内部字段名可能不同）
                string[] fieldNames = {
                    "m_defaultFontAsset",
                    "s_defaultFontAsset",
                    "k_DefaultFontAsset"
                };

                // 1) 尝试静态字段
                foreach (string name in fieldNames)
                {
                    var field = settingsType.GetField(name,
                        System.Reflection.BindingFlags.Static |
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Public);
                    if (field != null)
                    {
                        field.SetValue(null, fontAsset);
                        Log.LogInfo($"Successfully set default TMP font via static field '{name}'");
                        return true;
                    }
                }

                // 2) 尝试实例字段，需获取单例实例
                object instance = GetTMPSettingsInstance();
                if (instance != null)
                {
                    foreach (string name in fieldNames)
                    {
                        var field = settingsType.GetField(name,
                            System.Reflection.BindingFlags.Instance |
                            System.Reflection.BindingFlags.NonPublic |
                            System.Reflection.BindingFlags.Public);
                        if (field != null)
                        {
                            field.SetValue(instance, fontAsset);
                            Log.LogInfo($"Successfully set default TMP font via instance field '{name}'");
                            return true;
                        }
                    }
                }

                // 3) 最后尝试通过可写属性直接设置
                var prop = settingsType.GetProperty("defaultFontAsset",
                    System.Reflection.BindingFlags.Static |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public);
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(null, fontAsset);
                    Log.LogInfo("Successfully set default TMP font via property");
                    return true;
                }

                Log.LogWarning("Could not find TMP_Settings backing field for default font");
                return false;
            }
            catch (Exception ex)
            {
                Log.LogError($"Error setting TMP default font via reflection: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// 获取 TMP_Settings 的单例实例。
        /// 依次尝试：instance 属性、s_instance 静态字段、已加载的对象查找。
        /// 注意：IL2CPP 版本使用泛型 FindObjectsOfTypeAll 以避免 System.Type 与 Il2CppSystem.Type 不兼容问题。
        /// </summary>
        /// <returns>TMP_Settings 实例，找不到返回 null</returns>
        private object GetTMPSettingsInstance()
        {
            var settingsType = typeof(TMPro.TMP_Settings);

            // 1) 通过 instance 属性
            var instanceProp = settingsType.GetProperty("instance",
                System.Reflection.BindingFlags.Static |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public);
            if (instanceProp != null && instanceProp.CanRead)
            {
                object inst = instanceProp.GetValue(null);
                if (inst != null) return inst;
            }

            // 2) 通过 s_instance 静态字段
            var sInstanceField = settingsType.GetField("s_instance",
                System.Reflection.BindingFlags.Static |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public);
            if (sInstanceField != null)
            {
                object inst = sInstanceField.GetValue(null);
                if (inst != null) return inst;
            }

            // 3) 查找已加载的 TMP_Settings 对象（使用泛型版本避免 Type 类型不兼容）
            try
            {
                var loaded = Resources.FindObjectsOfTypeAll<TMPro.TMP_Settings>();
                if (loaded != null && loaded.Length > 0)
                {
                    return loaded[0];
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning($"FindObjectsOfTypeAll<TMP_Settings> failed: {ex.Message}");
            }

            return null;
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