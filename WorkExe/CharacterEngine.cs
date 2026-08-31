using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace WorkExe
{
    public enum CharacterState
    {
        Idle,
        Drag,
        Kowtow,
        Crawl,
        Hit,
        CannonReady,
        CannonFire,
        CowAppear,
        CowHit,
        FlyingOut
    }

    public class CharacterEngine
    {
        private DispatcherTimer _timer;
        private Dictionary<CharacterState, List<BitmapSource>> _frames = new Dictionary<CharacterState, List<BitmapSource>>();
        private int _frameIndex = 0;
        private CharacterState _state = CharacterState.Idle;
        private Config _config;

        public CharacterState State
        {
            get { return _state; }
            private set
            {
                _state = value;
                _frameIndex = 0;
                FrameChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public double BaseWidth { get; set; } = 160;
        public double BaseHeight { get; set; } = 200;
        public BitmapSource CurrentFrame
        {
            get
            {
                if (_frames.ContainsKey(State) && _frames[State].Count > 0)
                    return _frames[State][_frameIndex % _frames[State].Count];
                return null;
            }
        }

        public event EventHandler FrameChanged;
        public event EventHandler StateFinished;

        public CharacterEngine(Config config)
        {
            _config = config;
            LoadAssets();
            _timer = new DispatcherTimer(DispatcherPriority.Render);
            _timer.Interval = TimeSpan.FromMilliseconds(120);
            _timer.Tick += OnTick;
            _timer.Start();
        }

        private string AssetsDir
        {
            get
            {
                string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return Path.Combine(exeDir, "Assets");
            }
        }

        private void LoadAssets()
        {
            foreach (CharacterState s in Enum.GetValues(typeof(CharacterState)))
            {
                _frames[s] = new List<BitmapSource>();
            }

            LoadState(CharacterState.Idle, "idle");
            LoadState(CharacterState.Drag, "drag");
            LoadState(CharacterState.Kowtow, "kowtow");
            LoadState(CharacterState.Crawl, "crawl");
            LoadState(CharacterState.Hit, "hit");
            LoadState(CharacterState.CannonReady, "cannon_ready");
            LoadState(CharacterState.CannonFire, "cannon_fire");
            LoadState(CharacterState.CowAppear, "cow_appear");
            LoadState(CharacterState.CowHit, "cow_hit");
            LoadState(CharacterState.FlyingOut, "flying_out");
        }

        private void LoadState(CharacterState state, string prefix)
        {
            string dir = AssetsDir;
            for (int i = 0; i < 10; i++)
            {
                string path = Path.Combine(dir, $"{prefix}_{i}.png");
                if (File.Exists(path))
                {
                    try
                    {
                        var src = LoadPng(path);
                        if (src != null) _frames[state].Add(src);
                    }
                    catch { }
                }
            }
            if (_frames[state].Count == 0)
            {
                string fallback = Path.Combine(dir, $"{prefix}.png");
                if (File.Exists(fallback))
                {
                    var src = LoadPng(fallback);
                    if (src != null) _frames[state].Add(src);
                }
            }
        }

        private BitmapSource LoadPng(string path)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (_frames.ContainsKey(State) && _frames[State].Count > 1)
            {
                _frameIndex++;
                FrameChanged?.Invoke(this, EventArgs.Empty);
                if (_frameIndex >= _frames[State].Count)
                {
                    _frameIndex = 0;
                    if (State == CharacterState.Kowtow || State == CharacterState.Hit)
                    {
                        StateFinished?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
        }

        public void SetState(CharacterState state)
        {
            State = state;
        }

        public void Reload()
        {
            foreach (var key in new List<CharacterState>(_frames.Keys))
            {
                _frames[key].Clear();
            }
            LoadAssets();
            _frameIndex = 0;
        }

        public void Stop()
        {
            _timer?.Stop();
        }
    }
}
