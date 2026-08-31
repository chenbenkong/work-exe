using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace WorkExe
{
    public partial class MainWindow : Window
    {
        private Config _config;
        private CharacterEngine _engine;
        private TrayManager _tray;
        private HwndSource _hwndSource;
        private const int HOTKEY_ID = 9000;

        private bool _isDragging = false;
        private Point _dragOffset;
        private double _scale = 1.0;
        private double _baseWidth = 160;
        private double _baseHeight = 200;
        private const double BubbleExtra = 60;

        private GameMode _gameMode = GameMode.None;
        private enum GameMode { None, Whip, Cannon, Cow }

        private DispatcherTimer _bubbleTimer;
        private DispatcherTimer _whipTimer;
        private DispatcherTimer _cannonTimer;
        private DispatcherTimer _cowTimer;
        private DateTime _cannonChargeStart;
        private bool _cannonCharging = false;
        private bool _cannonFired = false;
        private int _cowDirection = 1;
        private double _cowX = 0;
        private double _cannonAngle = 0;
        private double _preGameLeft, _preGameTop, _preGameWidth, _preGameHeight;

        private IntPtr _hookId = IntPtr.Zero;
        private NativeMethods.HookProc _hookProc;
        private DispatcherTimer _watchdogTimer;
        private SettingsWindow _settingsWindow;
        private bool _isExiting = false;

        public MainWindow()
        {
            InitializeComponent();
            EnsureAssets();
            WhipImage.Source = LoadAssetImage("whip.png");
            CannonImage.Source = LoadAssetImage("cannon.png");
            CowImage.Source = LoadAssetImage("cow.png");
            _config = Config.Load();
            ApplySize(_config.Size);
            Topmost = _config.AlwaysOnTop;
            Opacity = _config.Opacity;
            ShowInTaskbar = _config.ShowInTaskbar;
            StartWatchdog();
            _engine = new CharacterEngine(_config);
            _engine.FrameChanged += (s, e) => UpdateFrame();
            _engine.StateFinished += OnStateFinished;
            UpdateFrame();

            _tray = new TrayManager();
            _tray.ShowRequested += (s, e) => { Show(); Visibility = Visibility.Visible; };
            _tray.HideRequested += (s, e) => { Visibility = Visibility.Hidden; };
            _tray.ExitRequested += (s, e) => CleanExit();
            _tray.RestoreRequested += (s, e) => EmergencyRestore();
            _tray.SettingsRequested += (s, e) => Dispatcher.Invoke(OpenSettings);

            CharacterImage.MouseLeftButtonDown += CharacterImage_MouseLeftButtonDown;
            CharacterImage.MouseLeftButtonUp += CharacterImage_MouseLeftButtonUp;
            CharacterImage.MouseMove += CharacterImage_MouseMove;
            CharacterImage.MouseRightButtonUp += CharacterImage_MouseRightButtonUp;
            CharacterImage.MouseRightButtonDown += CharacterImage_MouseRightButtonDown;
            MouseLeftButtonDown += MainWindow_MouseLeftButtonDown;
            MouseLeftButtonUp += MainWindow_MouseLeftButtonUp;
            KeyDown += MainWindow_KeyDown;
            KeyUp += MainWindow_KeyUp;

            PositionToBottomCenter();
        }

        private void EnsureAssets()
        {
            try
            {
                string dir = AssetGenerator.AssetsDir;
                bool missing = !Directory.Exists(dir) ||
                               Directory.GetFiles(dir, "*.png").Length == 0;
                if (missing)
                {
                    string photo = Path.Combine(AssetGenerator.ProjectAssetsDir, "boss.png");
                    AssetGenerator.Generate(File.Exists(photo) ? photo : null);
                }
            }
            catch { }
        }

        private void StartWatchdog()
        {
            _watchdogTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _watchdogTimer.Tick += WatchdogTick;
            _watchdogTimer.Start();
        }

        private void WatchdogTick(object sender, EventArgs e)
        {
            if (_config.AlwaysOnTop)
            {
                // 用 Win32 强制重新置顶，防止被其他程序抢走 Z 序
                var handle = new WindowInteropHelper(this).Handle;
                NativeMethods.SetWindowPos(handle, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0,
                    NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE |
                    NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
            }

            if (_gameMode == GameMode.None && Visibility == Visibility.Visible && !_isDragging)
            {
                var area = NativeMethods.GetWorkArea();
                double cx = Left + Width / 2;
                double cy = Top + Height / 2;
                if (cx < area.Left || cx > area.Right || cy < area.Top || cy > area.Bottom)
                {
                    Left = Math.Max(area.Left, Math.Min(cx - Width / 2, area.Right - Width));
                    Top = Math.Max(area.Top, Math.Min(cy - Height / 2, area.Bottom - Height));
                }
            }
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            _hwndSource = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            _hwndSource.AddHook(WndProc);
            NativeMethods.RegisterHotKey(_hwndSource.Handle, HOTKEY_ID,
                NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT | NativeMethods.MOD_SHIFT,
                NativeMethods.VK_Q);

            // 全局低级键盘钩子：窗口失焦时 Esc / 空格依然可用
            try
            {
                _hookProc = KeyboardHookCallback;
                _hookId = NativeMethods.SetWindowsHookEx(
                    NativeMethods.WH_KEYBOARD_LL, _hookProc,
                    NativeMethods.GetModuleHandle(null), 0);
            }
            catch { _hookId = IntPtr.Zero; }
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                var kb = (NativeMethods.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(
                    lParam, typeof(NativeMethods.KBDLLHOOKSTRUCT));
                uint vk = kb.vkCode;

                if (msg == NativeMethods.WM_KEYDOWN || msg == NativeMethods.WM_SYSKEYDOWN)
                {
                    if (vk == NativeMethods.VK_ESCAPE)
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            if (_gameMode != GameMode.None) ExitGameMode();
                        }));
                        return (IntPtr)1;
                    }
                    if (vk == NativeMethods.VK_SPACE && _gameMode == GameMode.Cannon)
                    {
                        Dispatcher.BeginInvoke(new Action(StartCannonCharge));
                        return (IntPtr)1;
                    }
                }
                else if (msg == NativeMethods.WM_KEYUP || msg == NativeMethods.WM_SYSKEYUP)
                {
                    if (vk == NativeMethods.VK_SPACE && _gameMode == GameMode.Cannon)
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            if (_cannonCharging && !_cannonFired) FireCannon();
                        }));
                        return (IntPtr)1;
                    }
                }
            }
            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                Dispatcher.Invoke(() => EmergencyRestore());
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void PositionToBottomCenter()
        {
            var area = NativeMethods.GetWorkArea();
            double screenW = area.Right - area.Left;
            double w = _baseWidth * _scale;
            double winH = _baseHeight * _scale + BubbleExtra;
            Left = area.Left + (screenW - w) / 2;
            Top = area.Bottom - winH - 8;
            Width = w;
            Height = winH;
        }

        private void ApplySize(string size)
        {
            switch (size.ToLower())
            {
                case "small": _scale = 0.65; break;
                case "large": _scale = 1.45; break;
                default: _scale = 1.0; break;
            }
            _config.Size = size.ToLower();
            Width = _baseWidth * _scale;
            Height = _baseHeight * _scale + BubbleExtra;
            CharacterImage.Width = _baseWidth * _scale;
            CharacterImage.Height = _baseHeight * _scale;
            if (_gameMode != GameMode.Whip)
                CharacterImage.Margin = new Thickness(0, BubbleExtra, 0, 0);
        }

        private BitmapImage LoadAssetImage(string name)
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Assets", name);
            if (!File.Exists(path)) return null;
            var img = new BitmapImage();
            img.BeginInit();
            img.UriSource = new Uri(path, UriKind.Absolute);
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.EndInit();
            img.Freeze();
            return img;
        }

        private void UpdateFrame()
        {
            CharacterImage.Source = _engine.CurrentFrame;
        }

        private void OnStateFinished(object sender, EventArgs e)
        {
            if (_engine.State == CharacterState.Kowtow || _engine.State == CharacterState.Hit)
            {
                _engine.SetState(CharacterState.Idle);
            }
        }

        private void CharacterImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_gameMode != GameMode.None) return;
            _isDragging = true;
            _dragOffset = e.GetPosition(this);
            CharacterImage.CaptureMouse();
            _engine.SetState(CharacterState.Drag);
        }

        private void CharacterImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;
            var pos = NativeMethods.GetCursorPosition();
            var area = NativeMethods.GetWorkArea();
            double newLeft = pos.X - _dragOffset.X;
            double newTop = pos.Y - _dragOffset.Y;
            double w = Width;
            double h = Height;
            newLeft = Math.Max(area.Left - w * 0.7, Math.Min(newLeft, area.Right - w * 0.3));
            newTop = Math.Max(area.Top, Math.Min(newTop, area.Bottom - h * 0.7));
            Left = newLeft;
            Top = newTop;
        }

        private void CharacterImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDragging) return;
            _isDragging = false;
            CharacterImage.ReleaseMouseCapture();
            _engine.SetState(CharacterState.Idle);
        }

        private bool _rightDownPending = false;
        private void CharacterImage_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _rightDownPending = true;
            e.Handled = true;
        }

        private void CharacterImage_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (!_rightDownPending) return;
            _rightDownPending = false;
            if (_gameMode != GameMode.None)
            {
                ExitGameMode();
                return;
            }
            ShowContextMenu();
        }

        private void ShowContextMenu()
        {
            var menu = new ContextMenu
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Colors.LightGray),
                BorderThickness = new Thickness(1)
            };
            menu.Resources["MenuItemStyle"] = (Style)Application.Current.Resources["RoundMenuItem"];
            AddMenuItem(menu, "鞭子抽打", () => StartWhip());
            AddMenuItem(menu, "大炮惩罚", () => StartCannon());
            AddMenuItem(menu, "召唤牛来", () => StartCow());
            menu.Items.Add(new Separator());
            AddMenuItem(menu, "说\"我错了\"", () => SaySorry());
            AddMenuItem(menu, "磕头", () => DoKowtow());
            AddMenuItem(menu, "原地待命", () => _engine.SetState(CharacterState.Idle));
            AddMenuItem(menu, "在桌面爬行", () => _engine.SetState(CharacterState.Crawl));
            menu.Items.Add(new Separator());
            AddMenuItem(menu, "调整人物大小 ▶", null);
            AddMenuItem(menu, _config.AlwaysOnTop ? "取消始终置顶" : "始终置顶", () => ToggleTopmost());
            AddMenuItem(menu, "暂时隐藏", () => { Visibility = Visibility.Hidden; });
            AddMenuItem(menu, "恢复默认状态", () => EmergencyRestore());
            menu.Items.Add(new Separator());
            AddMenuItem(menu, "设置...", () => OpenSettings());
            AddMenuItem(menu, "退出程序", () => CleanExit());
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
            menu.IsOpen = true;
        }

        private void AddMenuItem(ContextMenu menu, string header, Action action)
        {
            var item = new MenuItem { Header = header, Style = (Style)Application.Current.Resources["RoundMenuItem"] };
            if (action != null)
                item.Click += (s, e) => action();
            menu.Items.Add(item);
        }

        private void SaySorry()
        {
            var lines = _config.SorryLines;
            ShowBubble(lines.Count > 0 ? lines[new Random().Next(lines.Count)] : "我错了！");
        }

        private void DoKowtow()
        {
            _engine.SetState(CharacterState.Kowtow);
            ShowBubble("老板饶命！");
        }

        private void ToggleTopmost()
        {
            _config.AlwaysOnTop = !_config.AlwaysOnTop;
            Topmost = _config.AlwaysOnTop;
            _config.Save();
        }

        private void ShowBubble(string text)
        {
            BubbleText.Text = text;
            BubbleBorder.Visibility = Visibility.Visible;
            BubbleBorder.UpdateLayout();

            var area = NativeMethods.GetWorkArea();
            double charLeft, charTop;
            if (_gameMode == GameMode.Whip)
            {
                charLeft = _preGameLeft - area.Left;
                charTop = _preGameTop - area.Top + BubbleExtra;
            }
            else
            {
                charLeft = 0;
                charTop = BubbleExtra;
            }

            double bw = BubbleBorder.ActualWidth > 0 ? BubbleBorder.ActualWidth : 120;
            double bh = BubbleBorder.ActualHeight > 0 ? BubbleBorder.ActualHeight : 30;
            double bx = charLeft + (CharacterImage.Width - bw) / 2;
            double by = charTop - bh - 8;
            if (by < 0) by = charTop + 4;
            if (bx < 0) bx = 0;
            BubbleBorder.Margin = new Thickness(bx, by, 0, 0);

            if (_bubbleTimer == null)
            {
                _bubbleTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                _bubbleTimer.Tick += (s, e) => { BubbleBorder.Visibility = Visibility.Collapsed; _bubbleTimer.Stop(); };
            }
            _bubbleTimer.Stop();
            _bubbleTimer.Start();
        }

        private void MainWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_gameMode == GameMode.Whip)
            {
                _engine.SetState(CharacterState.Hit);
                var lines = _config.HitLines;
                if (lines.Count > 0) ShowBubble(lines[new Random().Next(lines.Count)]);
                WhipImage.Margin = new Thickness(WhipImage.Margin.Left + 10, WhipImage.Margin.Top - 10, 0, 0);
            }
        }

        private void MainWindow_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_gameMode == GameMode.Whip)
            {
                _engine.SetState(CharacterState.Idle);
            }
        }

        private void StartWhip()
        {
            if (_gameMode != GameMode.None) return;
            _gameMode = GameMode.Whip;
            ShowBubble("来呀！抽我呀！");
            _preGameLeft = Left; _preGameTop = Top; _preGameWidth = Width; _preGameHeight = Height;
            var area = NativeMethods.GetWorkArea();
            Width = area.Right - area.Left;
            Height = area.Bottom - area.Top;
            Left = area.Left;
            Top = area.Top;
            CharacterImage.Margin = new Thickness(_preGameLeft - area.Left, _preGameTop - area.Top + BubbleExtra, 0, 0);
            RootGrid.Background = Brushes.Transparent;
            WhipImage.Visibility = Visibility.Visible;
            _whipTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            _whipTimer.Tick += WhipTick;
            _whipTimer.Start();
        }

        private void WhipTick(object sender, EventArgs e)
        {
            var pos = NativeMethods.GetCursorPosition();
            var area = NativeMethods.GetWorkArea();
            WhipImage.Margin = new Thickness(pos.X - area.Left - 30, pos.Y - area.Top - 30, 0, 0);
        }

        private void StartCannon()
        {
            if (_gameMode != GameMode.None) return;
            _gameMode = GameMode.Cannon;
            _cannonFired = false;
            _cannonCharging = false;
            _engine.SetState(CharacterState.CannonReady);
            CannonImage.Visibility = Visibility.Visible;
            PositionCannon();
            ShowBubble("你要干什么？");
        }

        private void PositionCannon()
        {
            CannonImage.Width = 120 * _scale;
            CannonImage.Height = 120 * _scale;
            double cx = Width / 2;
            double cy = Height / 2;
            double canX = cx - 60 * _scale;
            double canY = cy - 30 * _scale;
            CannonImage.Margin = new Thickness(canX, canY, 0, 0);
        }

        private void StartCow()
        {
            if (_gameMode != GameMode.None) return;
            _gameMode = GameMode.Cow;
            _engine.SetState(CharacterState.CowAppear);
            var area = NativeMethods.GetWorkArea();
            double cx = Left + Width / 2;
            _cowDirection = cx < (area.Right - area.Left) / 2 ? 1 : -1;
            _cowX = _cowDirection > 0 ? area.Left - 120 : area.Right + 20;
            CowImage.Visibility = Visibility.Visible;
            CowImage.RenderTransform = new ScaleTransform(_cowDirection < 0 ? -1 : 1, 1, 50, 40);
            _cowTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            _cowTimer.Tick += CowTick;
            _cowTimer.Start();
            ShowBubble("等一下，哪来的牛？");
        }

        private void CowTick(object sender, EventArgs e)
        {
            var area = NativeMethods.GetWorkArea();
            double cx = Left + Width / 2;
            double speed = 9;
            if (Math.Abs(_cowX - cx) > 10)
            {
                _cowX += _cowDirection * speed;
            }
            else
            {
                _cowTimer.Stop();
                OnCowHit();
            }
            CowImage.Margin = new Thickness(_cowX - Left, Height - 80 * _scale, 0, 0);
        }

        private void OnCowHit()
        {
            _engine.SetState(CharacterState.FlyingOut);
            ShowBubble("啊——！");
            var anim = new DoubleAnimation
            {
                From = Left,
                To = _cowDirection > 0 ? SystemParameters.VirtualScreenWidth + 200 : -400,
                Duration = TimeSpan.FromSeconds(1.2),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            anim.Completed += (s, e) =>
            {
                ExitGameMode();
                PositionToBottomCenter();
                _engine.SetState(CharacterState.Idle);
            };
            BeginAnimation(Window.LeftProperty, anim);
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                ExitGameMode();
                e.Handled = true;
                return;
            }
            if (_gameMode == GameMode.Cannon && e.Key == Key.Space && !_cannonCharging && !_cannonFired)
            {
                StartCannonCharge();
                e.Handled = true;
            }
        }

        private void StartCannonCharge()
        {
            if (_cannonCharging || _cannonFired) return;
            _cannonCharging = true;
            _cannonChargeStart = DateTime.Now;
            _cannonTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            int lineIdx = 0;
            _cannonTimer.Tick += (s, ev) =>
            {
                var lines = _config.CannonChargeLines;
                if (lines.Count > 0)
                    ShowBubble(lines[lineIdx % lines.Count]);
                lineIdx++;
            };
            _cannonTimer.Start();
            var firstLines = _config.CannonChargeLines;
            if (firstLines.Count > 0) ShowBubble(firstLines[0]);
        }

        private void MainWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (_gameMode == GameMode.Cannon && e.Key == Key.Space && _cannonCharging && !_cannonFired)
            {
                FireCannon();
                e.Handled = true;
            }
        }

        private void FireCannon()
        {
            _cannonCharging = false;
            _cannonFired = true;
            _cannonTimer?.Stop();
            _engine.SetState(CharacterState.CannonFire);
            ShowBubble("起飞！");
            var area = NativeMethods.GetWorkArea();
            var animX = new DoubleAnimation
            {
                From = Left,
                To = area.Right + 100,
                Duration = TimeSpan.FromSeconds(2),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            var animY = new DoubleAnimation
            {
                From = Top,
                To = area.Top + 50,
                Duration = TimeSpan.FromSeconds(1),
                AutoReverse = true,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            animX.Completed += (s, e) =>
            {
                ExitGameMode();
                _engine.SetState(CharacterState.Idle);
                PositionToBottomCenter();
            };
            BeginAnimation(Window.LeftProperty, animX);
            BeginAnimation(Window.TopProperty, animY);
        }

        private void ExitGameMode()
        {
            _gameMode = GameMode.None;
            _whipTimer?.Stop();
            _cannonTimer?.Stop();
            _cowTimer?.Stop();
            _cannonCharging = false;
            _cannonFired = false;
            WhipImage.Visibility = Visibility.Collapsed;
            CannonImage.Visibility = Visibility.Collapsed;
            CowImage.Visibility = Visibility.Collapsed;
            CharacterImage.Margin = new Thickness(0, BubbleExtra, 0, 0);
            RootGrid.Background = null;
            PositionToBottomCenter();
            _engine.SetState(CharacterState.Idle);
        }

        private void EmergencyRestore()
        {
            ExitGameMode();
            BubbleBorder.Visibility = Visibility.Collapsed;
            PositionToBottomCenter();
            Topmost = _config.AlwaysOnTop;
            _engine.SetState(CharacterState.Idle);
        }

        private void OpenSettings()
        {
            if (_settingsWindow != null && _settingsWindow.IsLoaded)
            {
                _settingsWindow.Activate();
                return;
            }
            _settingsWindow = new SettingsWindow(_config);
            _settingsWindow.SettingsApplied += OnSettingsApplied;
            _settingsWindow.Closed += (s, e) => _settingsWindow = null;
            _settingsWindow.Show();
            _settingsWindow.Activate();
        }

        private void OnSettingsApplied(object sender, EventArgs e)
        {
            ApplySize(_config.Size);
            Topmost = _config.AlwaysOnTop;
            Opacity = _config.Opacity;
            ShowInTaskbar = _config.ShowInTaskbar;
            PositionToBottomCenter();
            ReloadAssets();
        }

        private void ReloadAssets()
        {
            try
            {
                WhipImage.Source = LoadAssetImage("whip.png");
                CannonImage.Source = LoadAssetImage("cannon.png");
                CowImage.Source = LoadAssetImage("cow.png");
                _engine.Reload();
                UpdateFrame();
            }
            catch { }
        }

        private void CleanExit()
        {
            if (_isExiting) return;
            _isExiting = true;
            _engine?.Stop();
            _tray?.Dispose();
            _whipTimer?.Stop();
            _cannonTimer?.Stop();
            _cowTimer?.Stop();
            _bubbleTimer?.Stop();
            _watchdogTimer?.Stop();

            if (_hookId != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
            if (_hwndSource != null)
            {
                NativeMethods.UnregisterHotKey(_hwndSource.Handle, HOTKEY_ID);
                _hwndSource.RemoveHook(WndProc);
            }
            _settingsWindow?.Close();
            Application.Current.Shutdown();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            CleanExit();
            base.OnClosing(e);
        }
    }
}
