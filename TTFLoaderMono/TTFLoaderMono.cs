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
using System.Collections.Generic;
using System.Reflection;
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
                // 注意：不直接调用 FindObjectsOfType<T>(bool includeInactive)，
                // 该重载仅在 Unity 5.3+ 存在，在更旧的 Unity 运行时上会抛出 MissingMethodException。
                // 改用反射按需选择可用重载，保证跨版本兼容。
                foreach (var text in FindAllTextComponents(includeInactive: true))
                {
                    // 将所有找到的 UI.Text 组件的字体设置为加载的动态字体
                    text.font = dynamicFont;
                }
            }
        }

        /// <summary>
        /// 通过反射查找所有 UI.Text 组件，兼容不同 Unity 版本中
        /// Object.FindObjectsOfType 重载的可用情况，避免 MissingMethodException。
        /// </summary>
        /// <param name="includeInactive">是否包含挂载在非激活 GameObject 上的组件</param>
        /// <returns>找到的 UI.Text 组件列表</returns>
        private List<UnityEngine.UI.Text> FindAllTextComponents(bool includeInactive)
        {
            Type textType = typeof(UnityEngine.UI.Text);
            Type objectType = typeof(UnityEngine.Object);
            object found = null;

            // 优先尝试带 includeInactive 参数的泛型重载（Unity 5.3+）
            // 签名：T[] FindObjectsOfType<T>(bool includeInactive)
            MethodInfo includeInactiveMethod = objectType.GetMethod(
                "FindObjectsOfType",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new[] { typeof(bool) },
                null);

            if (includeInactiveMethod != null)
            {
                found = includeInactiveMethod
                    .MakeGenericMethod(textType)
                    .Invoke(null, new object[] { includeInactive });
            }

            // 回退：使用无参泛型重载（所有 Unity 版本都有），
            // 该重载只返回激活对象，但可保证不崩溃。
            if (found == null)
            {
                MethodInfo basicMethod = objectType.GetMethod(
                    "FindObjectsOfType",
                    BindingFlags.Static | BindingFlags.Public,
                    null,
                    Type.EmptyTypes,
                    null);

                if (basicMethod != null)
                {
                    found = basicMethod
                        .MakeGenericMethod(textType)
                        .Invoke(null, null);
                }
            }

            var result = new List<UnityEngine.UI.Text>();
            if (found is object[] array)
            {
                foreach (var obj in array)
                {
                    if (obj is UnityEngine.UI.Text text)
                    {
                        result.Add(text);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 通过反射设置 TMP_Settings 的默认字体。
        /// TMP_Settings.defaultFontAsset 属性在部分 TMP 版本中只读（无 setter），
        /// 因此直接写入其私有背板字段 m_defaultFontAsset（TMP 标准内部实现）。
        /// </summary>
        /// <param name="fontAsset">要设置的 TMP_FontAsset</param>
        /// <param name="fontName">字体名（用于日志）</param>
        /// <returns>是否设置成功</returns>
        private bool SetTMPDefaultFont(TMP_FontAsset fontAsset, string fontName)
        {
            try
            {
                Type settingsType = typeof(TMP_Settings);

                // 候选字段名（不同 TMP 版本内部字段名可能不同）
                string[] fieldNames = {
                    "m_defaultFontAsset",
                    "s_defaultFontAsset",
                    "k_DefaultFontAsset"
                };

                // 1) 尝试静态字段（某些 TMP 版本的 defaultFontAsset 直接读静态字段）
                foreach (string name in fieldNames)
                {
                    FieldInfo field = settingsType.GetField(name, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                    if (field != null)
                    {
                        field.SetValue(null, fontAsset);
                        Logger.LogInfo($"Successfully set default TMP font via static field '{name}': {fontName}");
                        return true;
                    }
                }

                // 2) 尝试实例字段（TMP 3.x 标准实现：getter 读取单例 s_instance 的 m_defaultFontAsset）
                object instance = GetTMPSettingsInstance(settingsType);
                if (instance != null)
                {
                    foreach (string name in fieldNames)
                    {
                        FieldInfo field = settingsType.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        if (field != null)
                        {
                            field.SetValue(instance, fontAsset);
                            Logger.LogInfo($"Successfully set default TMP font via instance field '{name}': {fontName}");
                            return true;
                        }
                    }
                }

                // 3) 最后尝试通过可写属性直接设置（某些 TMP 版本有 setter）
                PropertyInfo prop = settingsType.GetProperty("defaultFontAsset", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(null, fontAsset);
                    Logger.LogInfo($"Successfully set default TMP font via property: {fontName}");
                    return true;
                }

                Logger.LogWarning($"Could not find TMP_Settings backing field, will use UI.Text fallback for: {fontName}");
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error setting TMP default font: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// 获取 TMP_Settings 的单例实例。
        /// 依次尝试：instance 属性、s_instance 静态字段、已加载的对象查找。
        /// </summary>
        /// <param name="settingsType">TMP_Settings 类型</param>
        /// <returns>TMP_Settings 实例，找不到返回 null</returns>
        private object GetTMPSettingsInstance(Type settingsType)
        {
            // 1) 通过 instance 属性（TMP 3.x 标准）
            PropertyInfo instanceProp = settingsType.GetProperty("instance", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (instanceProp != null && instanceProp.CanRead)
            {
                object inst = instanceProp.GetValue(null);
                if (inst != null)
                    return inst;
            }

            // 2) 通过 s_instance 静态字段
            FieldInfo sInstanceField = settingsType.GetField("s_instance", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (sInstanceField != null)
            {
                object inst = sInstanceField.GetValue(null);
                if (inst != null)
                    return inst;
            }

            // 3) 查找已加载的 TMP_Settings 对象（ScriptableObject 资源）
            foreach (object obj in Resources.FindObjectsOfTypeAll(settingsType))
            {
                if (obj != null)
                    return obj;
            }

            return null;
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
                        // 通过反射设置 TMP_Settings 的私有背板字段
                        // 因为 defaultFontAsset 属性在部分版本中只读
                        if (SetTMPDefaultFont(customFont, fontName))
                        {
                            return; // 成功加载一个就退出
                        }

                        // TMP 资源创建成功但无法设置默认字体，
                        // 回退到 UI.Text 动态字体方案（加载系统/本地字体替换 UI.Text）
                        Logger.LogWarning($"TMP default font setting failed, falling back to UI.Text dynamic font: {fontName}");
                        Font fallback = LoadTTF(fontName, true);
                        if (fallback != null)
                        {
                            dynamicFont = fallback;
                            ApplyCustomFontToAllTexts();
                            Logger.LogInfo($"Set UI.Text dynamic font to: {fontName}");
                            return;
                        }
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