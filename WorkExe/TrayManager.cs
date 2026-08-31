using System;
using System.Drawing;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;

namespace WorkExe
{
    public class TrayManager : IDisposable
    {
        private NotifyIcon _notifyIcon;
        private ContextMenuStrip _menu;

        public event EventHandler ShowRequested;
        public event EventHandler HideRequested;
        public event EventHandler ExitRequested;
        public event EventHandler RestoreRequested;

        public TrayManager()
        {
            _menu = new ContextMenuStrip();
            _menu.Items.Add("显示", null, (s, e) => ShowRequested?.Invoke(this, EventArgs.Empty));
            _menu.Items.Add("隐藏", null, (s, e) => HideRequested?.Invoke(this, EventArgs.Empty));
            _menu.Items.Add("恢复默认位置", null, (s, e) => RestoreRequested?.Invoke(this, EventArgs.Empty));
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add("退出", null, (s, e) => ExitRequested?.Invoke(this, EventArgs.Empty));

            _notifyIcon = new NotifyIcon();
            _notifyIcon.Text = "WorkExe 桌面互动人物";
            _notifyIcon.ContextMenuStrip = _menu;
            _notifyIcon.Visible = true;
            _notifyIcon.DoubleClick += (s, e) => ShowRequested?.Invoke(this, EventArgs.Empty);

            string icoPath = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "Assets", "app.ico");
            if (System.IO.File.Exists(icoPath))
            {
                try { _notifyIcon.Icon = new Icon(icoPath); }
                catch
                {
                    _notifyIcon.Icon = SystemIcons.Application;
                }
            }
            else
            {
                _notifyIcon.Icon = SystemIcons.Application;
            }
        }

        public void ShowBalloon(string title, string text)
        {
            _notifyIcon?.ShowBalloonTip(2000, title, text, ToolTipIcon.Info);
        }

        public void Dispose()
        {
            _notifyIcon?.Dispose();
            _menu?.Dispose();
        }
    }
}
