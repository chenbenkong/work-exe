using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;

namespace WorkExe
{
    public class Config
    {
        public string BossPhotoPath { get; set; } = @"..\assets\boss.png";
        public string Size { get; set; } = "medium";
        public bool AlwaysOnTop { get; set; } = true;
        public bool ShowInTaskbar { get; set; } = false;
        public bool StartWithWindows { get; set; } = false;
        public double Opacity { get; set; } = 1.0;
        public List<string> HitLines { get; set; } = new List<string>
        {
            "啊！别打了！",
            "错了错了！",
            "老板饶命！",
            "好痛！"
        };
        public List<string> CannonChargeLines { get; set; } = new List<string>
        {
            "你要干什么？",
            "中国人会飞！",
            "等一下，我还没准备好！"
        };
        public List<string> CowLines { get; set; } = new List<string>
        {
            "等一下，哪来的牛？"
        };
        public List<string> SorryLines { get; set; } = new List<string>
        {
            "我错了！",
            "老板，我错了！",
            "再给我一次机会！"
        };

        public static Config CreateDefault()
        {
            return new Config();
        }

        public static string LinesToText(List<string> lines)
        {
            if (lines == null) return "";
            return string.Join(Environment.NewLine, lines);
        }

        public static List<string> TextToLines(string text)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(text)) return list;
            foreach (var raw in text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var line = raw.Trim();
                if (line.Length > 0) list.Add(line);
            }
            return list;
        }

        private static string ConfigPath
        {
            get
            {
                string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return Path.Combine(exeDir, "config.json");
            }
        }

        public static Config Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath, Encoding.UTF8);
                    var serializer = new JavaScriptSerializer();
                    return serializer.Deserialize<Config>(json) ?? new Config();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Config load failed: " + ex.Message);
            }
            return new Config();
        }

        public void Save()
        {
            try
            {
                var serializer = new JavaScriptSerializer();
                string json = serializer.Serialize(this);
                File.WriteAllText(ConfigPath, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Config save failed: " + ex.Message);
            }
        }
    }
}
