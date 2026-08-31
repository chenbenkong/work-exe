using System;
using System.IO;
using System.Reflection;
using System.Windows;
using Microsoft.Win32;

namespace WorkExe
{
    public partial class SettingsWindow : Window
    {
        private Config _config;
        public event EventHandler SettingsApplied;

        private const string RunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "WorkExe";

        public SettingsWindow(Config config)
        {
            InitializeComponent();
            _config = config;
            LoadFromConfig();
        }

        private void LoadFromConfig()
        {
            TxtPhoto.Text = _config.BossPhotoPath;
            switch ((_config.Size ?? "medium").ToLower())
            {
                case "small": RbSmall.IsChecked = true; break;
                case "large": RbLarge.IsChecked = true; break;
                default: RbMedium.IsChecked = true; break;
            }
            SliderOpacity.Value = Math.Max(0.3, Math.Min(1.0, _config.Opacity));
            TxtOpacity.Text = (int)(SliderOpacity.Value * 100) + "%";
            ChkTopmost.IsChecked = _config.AlwaysOnTop;
            ChkTaskbar.IsChecked = _config.ShowInTaskbar;
            ChkAutoStart.IsChecked = _config.StartWithWindows;

            TxtHitLines.Text = Config.LinesToText(_config.HitLines);
            TxtCannonLines.Text = Config.LinesToText(_config.CannonChargeLines);
            TxtCowLines.Text = Config.LinesToText(_config.CowLines);
            TxtSorryLines.Text = Config.LinesToText(_config.SorryLines);
        }

        private void SliderOpacity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtOpacity != null)
                TxtOpacity.Text = (int)(SliderOpacity.Value * 100) + "%";
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "选择老板照片",
                Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.gif|所有文件|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                TxtPhoto.Text = dlg.FileName;
            }
        }

        private void BtnRegenerate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string src = TxtPhoto.Text;
                string dest = null;
                if (!string.IsNullOrWhiteSpace(src) && File.Exists(src))
                {
                    Directory.CreateDirectory(AssetGenerator.ProjectAssetsDir);
                    dest = Path.Combine(AssetGenerator.ProjectAssetsDir, "boss.png");
                    File.Copy(src, dest, true);
                }
                AssetGenerator.Generate(dest);
                TxtPhotoStatus.Text = "素材已重新生成，人物头像已更新。";
                TxtPhotoStatus.Foreground = System.Windows.Media.Brushes.Green;
            }
            catch (Exception ex)
            {
                TxtPhotoStatus.Text = "生成失败：" + ex.Message;
                TxtPhotoStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            _config.BossPhotoPath = TxtPhoto.Text;
            _config.Size = RbSmall.IsChecked == true ? "small"
                         : RbLarge.IsChecked == true ? "large" : "medium";
            _config.Opacity = SliderOpacity.Value;
            _config.AlwaysOnTop = ChkTopmost.IsChecked == true;
            _config.ShowInTaskbar = ChkTaskbar.IsChecked == true;
            _config.StartWithWindows = ChkAutoStart.IsChecked == true;

            _config.HitLines = Config.TextToLines(TxtHitLines.Text);
            _config.CannonChargeLines = Config.TextToLines(TxtCannonLines.Text);
            _config.CowLines = Config.TextToLines(TxtCowLines.Text);
            _config.SorryLines = Config.TextToLines(TxtSorryLines.Text);

            if (_config.HitLines.Count == 0) _config.HitLines.Add("啊！");
            if (_config.CannonChargeLines.Count == 0) _config.CannonChargeLines.Add("等一下！");
            if (_config.CowLines.Count == 0) _config.CowLines.Add("哪来的牛？");
            if (_config.SorryLines.Count == 0) _config.SorryLines.Add("我错了！");

            _config.Save();
            ApplyAutoStart(_config.StartWithWindows);
            SettingsApplied?.Invoke(this, EventArgs.Empty);
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            _config = Config.CreateDefault();
            LoadFromConfig();
            TxtPhotoStatus.Text = "已恢复默认设置，记得点“保存并应用”。";
            TxtPhotoStatus.Foreground = System.Windows.Media.Brushes.Gray;
        }

        private void ApplyAutoStart(bool enabled)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKey, true))
                {
                    if (key == null) return;
                    if (enabled)
                    {
                        string exe = Assembly.GetExecutingAssembly().Location;
                        key.SetValue(AppName, "\"" + exe + "\"");
                    }
                    else
                    {
                        if (key.GetValue(AppName) != null)
                            key.DeleteValue(AppName, false);
                    }
                }
            }
            catch { }
        }
    }
}
