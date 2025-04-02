using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System;
using System.Dynamic;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using System.Text.Json.Nodes;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.IO.Pipes;

namespace lllc_newVer_Updater
{

    class Program
    {
        public class LangJson
        {
            [JsonPropertyName("lang")]
            public string lang { get; set; }
        }
        public class verjson
        {
            [JsonPropertyName("version")]
            public string version { get; set; }
            [JsonPropertyName("resource_version")]
            public int resource_version { get; set; }
            [JsonPropertyName("notice")]
            public string notice { get; set; }
            [JsonPropertyName("need_fix")]
            public bool need_fix { get; set; }
        }
        public enum NodeType
        {
            Auto,
            ZhenJiang,
            GitHub,
            Tianyi
        }
        public static string LangPath = "";
        public static string LangConfigPath = "";
        public static string LangFontPath = "";
        public static string LangDatePath = "";
        public static string GamePath = "";


        public static int TimeOuted = 10;
        public static bool AutoUpdate = true;
        public static NodeType UpdateUri = NodeType.Auto;

        public static readonly Dictionary<NodeType, string> UrlDictionary = new Dictionary<NodeType, string>{
            { NodeType.Auto, "https://api.zeroasso.top/v2/download/files?file_name={0}" },
            { NodeType.ZhenJiang, "https://download.zeroasso.top/files/{0}" },
            { NodeType.Tianyi, "https://node.zeroasso.top/d/tianyi/{0}" }
        };

        private static  HttpClient Client = new HttpClient();

        public static string AppOldVersion = string.Empty;
        public static string AppUpdateVersion = string.Empty;
        public static string TMPOldVersion = string.Empty;
        public static string TMPUpdateVersion = string.Empty;
        public static string ResourceOldVersion = string.Empty;
        public static string ResourceUpdateVersion = string.Empty;
        public static string LLCLangName = "CN";
        public static string UpdateMessage = string.Empty;

        public static Action<string> LogError { get; set; }
        public static Action<string> LogWarning { get; set; }
        public static Action<string> LogInfo { get; set; }
        static void Main(string[] args)
        {
            try
            {
                GamePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                LangPath = Path.Combine(GamePath, "LimbusCompany_Data", "Lang");
                LangDatePath = Path.Combine(GamePath, "LimbusCompany_Data", "Lang", LLCLangName, "version.json");
                LangFontPath = Path.Combine(LangPath, LLCLangName, "Font");
                LangConfigPath = Path.Combine(LangPath, "config.json");
                LogError = (msg) => Console.WriteLine("Error:" + msg);
                LogWarning = (msg) => Console.WriteLine("Warn:" + msg);
                LogInfo = (msg) => Console.WriteLine("Info:" + msg);
                Main_update();
            } catch (Exception e)
            {
                Console.WriteLine("Error:" + e);
                Console.WriteLine("Something happend,set need_fix -> true (clear all version.json) for next fix");
                Console.WriteLine("starting game anyway LoL");
            }
            finally
            {
                Console.WriteLine("Starting Game...");
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = GamePath + "/LimbusCompany.exe";
                startInfo.Arguments = string.Join(" ", args.Skip(1));
                Process.Start(startInfo);
            }
        }
        static void Main_update()
        {
            if (!Directory.Exists(LangPath))
            {
                LogWarning("Can't Find Lang Path,creating...");
                Directory.CreateDirectory(LangPath);
            }
            if (!Directory.Exists(Path.Combine(GamePath, "LimbusCompany_Data", "Lang", LLCLangName)))
            {
                LogWarning("Can't Find Main Lang Path,creating...");
                Directory.CreateDirectory(Path.Combine(GamePath, "LimbusCompany_Data", "Lang", LLCLangName));
            }
            if (!Directory.Exists(LangFontPath))
            {
                LogWarning("Can't Find Lang Font Path,creating...");
                Directory.CreateDirectory(LangFontPath);
            }
            if (!File.Exists(LangDatePath))
            {
                LogWarning("Can't Find Lang Date Path,creating...");
                File.Create(LangDatePath).Close();
            }
            if (!File.Exists(LangConfigPath))
            {
                LogWarning("Can't Find Lang Date Path,creating and installing...");
                File.Create(LangConfigPath).Close();
                
                LangJson langJson = new LangJson() { 
                    lang = LLCLangName
                };
                string tempjsonString = JsonSerializer.Serialize(langJson);
                File.WriteAllText(LangConfigPath, tempjsonString);
            }
            LogInfo("LimbusCompany Lang Update Tool");
            LogInfo("开始检查...");
            //检查安装汉化,由于sb小金不给全多语言的json,就只能替换了
            string jsonString = File.ReadAllText(LangConfigPath);
            LangJson langJson1 = JsonSerializer.Deserialize<LangJson>(jsonString);
            if (langJson1.lang != LLCLangName)
            {
                LogWarning("检测到非汉化语言,正在替换...");
                langJson1.lang = LLCLangName;
                string tempjsonString = JsonSerializer.Serialize(langJson1);
                File.WriteAllText(LangConfigPath, tempjsonString);
            }
            if (!File.Exists(GamePath + "/7z.exe")|| !File.Exists(GamePath + "/7z.dll"))
            {
                LogError("Can't Find HotUpdate Need File(7z.exe/7z.dll). Skip Mod Update.");
                return;
            }
            if (AutoUpdate)
            {
                LogInfo("自动更新");
            }else{
                LogInfo("手动更新");
            }
            if (UpdateUri == NodeType.Auto)
            {
                LogInfo("自动选择更新源");
            }
            else
            {
                LogInfo("手动选择更新源");
            }
            LogInfo("LangPath: " + LangPath);
            LogInfo("GamePath: " + GamePath);
            LogInfo("AutoUpdate: " + AutoUpdate);
            LogInfo("开始检查更新...");
            CheckUpdate();
        }
        public static void CheckUpdate() {
            Client.Timeout = TimeSpan.FromSeconds(TimeOuted);
            Client.DefaultRequestHeaders.Add("User-Agent", "LLC-GameClient");
            LogInfo($"Check Mod Update From {UpdateUri}");
            ModUpdate();
            LogInfo("Check Chinese Font Asset Update");
            ChineseFontUpdate();
        }
        private static void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }
            foreach (var directory in Directory.GetDirectories(sourceDir))
            {
                var destDirectory = Path.Combine(destDir, Path.GetFileName(directory));
                CopyDirectory(directory, destDirectory);
            }
        }
        static void ModUpdate()
        {
            try
            {
                JsonObject localJson = null;
                JsonObject serverJson = null;
                int latestTextVersion = 0;
                int localTextVersion = 0;
                bool updateod = false;
                try
                {
                    localJson = JsonNode.Parse(File.ReadAllText(LangDatePath)).AsObject();
                    var appOldVersion = localJson["version"].GetValue<string>();
                    localTextVersion = localJson["resource_version"].GetValue<int>();
                    if (localJson["need_fix"].GetValue<bool>())
                    {
                        LogWarning("Need Fix,fixing...");
                        Directory.Delete(Path.Combine(LangPath, LLCLangName), true);
                        updateod = true;
                    }
                }
                catch (System.Text.Json.JsonException ex)
                {
                    LogWarning($"Local JSON parsing failed: {ex} \n Suppose need update,In Fact this mean good :)");
                    updateod = true;
                }
                try
                {
                    var response = Client.GetStringAsync("https://api.zeroasso.top/v2/resource/get_version").GetAwaiter()
                        .GetResult();
                    serverJson = JsonNode.Parse(response).AsObject();
                    var tag = serverJson["version"].GetValue<string>();
                    latestTextVersion = serverJson["resource_version"].GetValue<int>();

                }
                catch (System.Text.Json.JsonException ex)
                {
                    LogWarning($"Server JSON parsing failed: {ex},RETURNING!");
                    return;
                }
                if (serverJson != null && latestTextVersion > localTextVersion || updateod)
                {
                    LogInfo("New text resource found. Download resource.");
                    var updatelog = $"LimbusLocalize_Resource_{latestTextVersion}.7z";
                    var downloadUri = UpdateUri == NodeType.GitHub
                        ? $"https://github.com/LocalizeLimbusCompany/LLC_Release/releases/download/{latestTextVersion}/{updatelog}"
                        : string.Format(UrlDictionary[UpdateUri], "Resource/" + updatelog);
                    var filename = Path.Combine(GamePath, updatelog);
                    if (!File.Exists(filename))
                        DownloadFile(downloadUri, filename);
                    string tempPath = LangPath;
                    //解压
                    UnarchiveFile(filename, tempPath);
                    string TargetFolder = Path.Combine(LangPath, "BepInEx\\plugins\\LLC\\Localize\\CN");
                    CopyDirectory(TargetFolder, Path.Combine(LangPath, LLCLangName));
                    Directory.Delete(Path.Combine(tempPath, "BepInEx\\"),true);
                    //更新版本号
                    ResourceOldVersion = localTextVersion.ToString();
                    ResourceUpdateVersion = latestTextVersion.ToString();
                    UpdateMessage = serverJson["notice"].GetValue<string>().Replace("\\n", "\n");
                    LogInfo("Mod Update Success.");
                    verjson newJson = new verjson()
                    {
                        version = serverJson["version"].GetValue<string>(),
                        resource_version = latestTextVersion,
                        notice = serverJson["notice"].GetValue<string>(),
                        need_fix = false
                    };
                    string jsonString = JsonSerializer.Serialize(newJson);
                    File.WriteAllText(LangDatePath, jsonString);
                }
                else
                {
                    LogInfo("No new mod or resource found.");
                }
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                    LogWarning(
                        "Maybe the timeout time is too short? Please try to change the timeout amount in Com.Bright.LocalizeLimbusCompany.cfg file.");
                LogWarning($"Mod update failed::\n{ex}");
                //更新need_fix
                verjson newJson = new verjson()
                {
                    version = "0.0.0",
                    resource_version = 0,
                    notice = "Update Failed",
                    need_fix = true
                };
                string jsonString = JsonSerializer.Serialize(newJson);
                File.WriteAllText(LangDatePath, jsonString);
            }
        }
        private static void ChineseFontUpdate()
        {
            try
            {
                LogInfo("Chinese Font Asset Update");
                if (!Directory.Exists(LangFontPath))
                {
                    LogWarning("Can't Find Font Path,creating...");
                    Directory.CreateDirectory(LangFontPath);
                }
                var fontPath = LangFontPath + "/languagefont.ttf";
                if (!File.Exists(LangFontPath + "/languagefont.ttf"))
                {
                    LogWarning("Can't Find Font File,copying...");
                    File.Copy(GamePath+ "/languagefont.ttf", LangFontPath + "/languagefont.ttf");
                }
                // 看0协怎么改吧,md直接AssetBundle.LoadFromFile也是个人物


                //var releaseUri = UpdateUri == NodeType.GitHub
                //    ? "https://api.github.com/repos/LocalizeLimbusCompany/LLC_ChineseFontAsset/releases/latest"
                //    : "https://api.zeroasso.top/v2/get_api/get/repos/LocalizeLimbusCompany/LLC_ChineseFontAsset/releases/latest";
                //var response = Client.GetStringAsync(releaseUri).GetAwaiter().GetResult();
                //var latest = JsonNode.Parse(response).AsObject();
                //var latestReleaseTag = int.Parse(latest["tag_name"].GetValue<string>());
                //var fontPath = LangFontPath + "/languagefont.ttf";
                //var lastWriteTime = File.Exists(fontPath)
                //    ? int.Parse(TimeZoneInfo.ConvertTime(new FileInfo(fontPath).LastWriteTime,
                //        TimeZoneInfo.FindSystemTimeZoneById("China Standard Time")).ToString("yyMMdd"))
                //    : 0;
                //if (lastWriteTime < latestReleaseTag)
                //{
                //    string updatelog;
                //    string downloadUri;
                //    if (UpdateUri == NodeType.GitHub)
                //    {
                //        updatelog = $"tmpchinesefont_BIE_{latestReleaseTag}.7z";
                //        downloadUri =
                //            $"https://github.com/LocalizeLimbusCompany/LLC_ChineseFontAsset/releases/download/{latestReleaseTag}/{updatelog}";
                //    }
                //    else
                //    {
                //        updatelog = "tmpchinesefont_BIE.7z";
                //        downloadUri = string.Format(UrlDictionary[UpdateUri], updatelog);
                //    }

                //    var filename = Path.Combine(GamePath, updatelog);
                //    if (!File.Exists(filename))
                //        DownloadFile(downloadUri, filename);
                //    UnarchiveFile(filename, GamePath);
                //    TMPUpdateVersion = latestReleaseTag.ToString();
                //    TMPOldVersion = lastWriteTime.ToString();
                //    LogInfo("Chinese Font Asset Update Success.");
                //}
                //else
                //{
                //    LogInfo("No new font asset found.");
                //}
            }
            catch (Exception ex)
            {
                LogWarning($"Font asset update failed:\n{ex}");
            }
        }

        private static void DownloadFile(string uri, string filePath)
        {
            try
            {
                LogInfo($"Download {uri} To {filePath}");
                var response = Client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();

                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    response.Content.CopyToAsync(fileStream).GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                LogWarning($"{uri} Error!!!:\n{ex}");
                throw; // 重新抛出异常，保留原始堆栈跟踪信息
            }
        }


        private static void UnarchiveFile(string sourceFile, string destinationPath)
        {
            try
            {
                LogInfo($"Unarchiving {sourceFile} To {destinationPath}");
                var processStartInfo = new ProcessStartInfo
                {
                    Arguments = $"x \"{sourceFile}\" -o\"{destinationPath}\" -y",
                    FileName = GamePath + "/7z.exe",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                var process = Process.Start(processStartInfo);
                if (process == null) return;
                process.OutputDataReceived += (_, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data)) LogInfo("Output: " + e.Data);
                };
                process.ErrorDataReceived += (_, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data)) LogError("Error: " + e.Data);
                };
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                File.Delete(sourceFile);
            }
            catch (Exception ex)
            {
                LogWarning($"Unarchive file failed:\n{ex}");
            }
        }

        public static void GenUpdateText()
        {
            var updateMessage = "您的模组已更新至最新版本！\n更新内容：";
            if (!string.IsNullOrEmpty(AppOldVersion) && !string.IsNullOrEmpty(AppUpdateVersion))
                updateMessage +=
                    $"\n程序更新：v{AppOldVersion} => v{AppUpdateVersion}";
            if (!string.IsNullOrEmpty(ResourceUpdateVersion) &&
                !string.IsNullOrEmpty(ResourceOldVersion))
                updateMessage +=
                    $"\n文本更新：v{ResourceOldVersion} => v{ResourceUpdateVersion}";
            if (!string.IsNullOrEmpty(TMPUpdateVersion) &&
                !string.IsNullOrEmpty(TMPOldVersion))
                updateMessage += $"\n字体更新：v{TMPOldVersion} => v{TMPUpdateVersion}";
            if (!string.IsNullOrEmpty(UpdateMessage))
                updateMessage += "\n更新提示：\n" + UpdateMessage;
            LogInfo(updateMessage);
        }
    }
}
