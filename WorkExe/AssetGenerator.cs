using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;

namespace WorkExe
{
    /// <summary>
    /// 用 GDI+ 生成透明 PNG 动作素材。不依赖 Python 或任何外部图像库。
    /// </summary>
    public static class AssetGenerator
    {
        private const int W = 200;
        private const int H = 250;

        public static string AssetsDir
        {
            get
            {
                string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return Path.Combine(exeDir, "Assets");
            }
        }

        public static string ProjectAssetsDir
        {
            get
            {
                // 开发目录：..\\assets
                string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return Path.Combine(exeDir, "..", "assets");
            }
        }

        public static void Generate(string photoPath = null)
        {
            Directory.CreateDirectory(AssetsDir);
            Bitmap face = null;
            if (!string.IsNullOrWhiteSpace(photoPath) && File.Exists(photoPath))
            {
                try { face = CropFace(photoPath, 90); }
                catch { face = null; }
            }

            Save(MakeIdle(face), "idle.png");
            Save(MakeIdle(face), "drag.png");
            Save(MakeKowtow(face), "kowtow_0.png");
            Save(MakeKowtow(face), "kowtow_1.png");
            Save(MakeCrawl(face), "crawl_0.png");
            Save(MakeCrawl(face), "crawl_1.png");
            Save(MakeHit(face), "hit.png");
            Save(MakeCannonReady(face), "cannon_ready.png");
            Save(MakeCannonFire(face), "cannon_fire.png");
            Save(MakeFlyingOut(face), "flying_out.png");
            var cow = MakeCow();
            Save(cow, "cow.png");
            Save(cow, "cow_appear.png");
            Save(cow, "cow_hit.png");
            Save(MakeWhip(), "whip.png");
            Save(MakeCannon(), "cannon.png");
            Save(MakeAppIcon(), "app.ico");
        }

        private static Bitmap CropFace(string path, int size)
        {
            using (var src = new Bitmap(path))
            {
                int s = Math.Min(src.Width, src.Height);
                int left = (src.Width - s) / 2;
                int top = (int)(src.Height * 0.08);
                int cropH = Math.Min(s, src.Height - top);
                var rect = new Rectangle(left, top, s, cropH);
                using (var cropped = src.Clone(rect, src.PixelFormat))
                {
                    var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
                    using (var g = Graphics.FromImage(bmp))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        g.DrawImage(cropped, 0, 0, size, size);
                    }
                    return bmp;
                }
            }
        }

        private static Bitmap NewCanvas()
        {
            return new Bitmap(W, H, PixelFormat.Format32bppArgb);
        }

        private static void DrawShadow(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var brush = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
            {
                g.FillEllipse(brush, 40, 232, 120, 18);
            }
        }

        private static void DrawSuit(Graphics g, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var brush = new SolidBrush(color))
            {
                // 躯干
                using (var path = RoundedRect(60, 110, 80, 92, 20))
                    g.FillPath(brush, path);
                // 手臂
                using (var path = RoundedRect(38, 128, 24, 84, 10))
                    g.FillPath(brush, path);
                using (var path = RoundedRect(138, 128, 24, 84, 10))
                    g.FillPath(brush, path);
            }
            using (var legBrush = new SolidBrush(Color.FromArgb(255, 50, 50, 55)))
            {
                using (var path = RoundedRect(68, 196, 26, 50, 8))
                    g.FillPath(legBrush, path);
                using (var path = RoundedRect(106, 196, 26, 50, 8))
                    g.FillPath(legBrush, path);
            }
            // 领带
            using (var tieBrush = new SolidBrush(Color.FromArgb(255, 200, 55, 55)))
            {
                g.FillPolygon(tieBrush, new[]
                {
                    new Point(100, 116), new Point(90, 178),
                    new Point(100, 194), new Point(110, 178)
                });
            }
        }

        private static GraphicsPath RoundedRect(int x, int y, int w, int h, int r)
        {
            var path = new GraphicsPath();
            int d = r * 2;
            path.AddArc(x, y, d, d, 180, 90);
            path.AddLine(x + r, y, x + w - r, y);
            path.AddArc(x + w - d, y, d, d, 270, 90);
            path.AddLine(x + w, y + r, x + w, y + h - r);
            path.AddArc(x + w - d, y + h - d, d, d, 0, 90);
            path.AddLine(x + w - r, y + h, x + r, y + h);
            path.AddArc(x, y + h - d, d, d, 90, 90);
            path.AddLine(x, y + h - r, x, y + r);
            path.CloseFigure();
            return path;
        }

        private static void CompositeFace(Bitmap img, Bitmap face, int yOffset, int size)
        {
            using (var g = Graphics.FromImage(img))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                if (face == null)
                {
                    DrawPlaceholderFace(g, (W - 90) / 2, yOffset, 90);
                }
                else
                {
                    int x = (W - size) / 2;
                    g.DrawImage(face, x, yOffset, size, size);
                }
            }
        }

        private static void DrawPlaceholderFace(Graphics g, int x, int y, int size)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var skin = new SolidBrush(Color.FromArgb(255, 255, 224, 204)))
            using (var pen = new Pen(Color.FromArgb(255, 200, 160, 140), 2))
            {
                g.FillEllipse(skin, x, y, size, size);
                g.DrawEllipse(pen, x, y, size, size);
            }
            float fx = x, fy = y, fs = size;
            using (var eye = new SolidBrush(Color.FromArgb(255, 60, 45, 35)))
            {
                float ew = fs * 0.09f;
                g.FillEllipse(eye, fx + fs * 0.25f, fy + fs * 0.38f, ew, ew);
                g.FillEllipse(eye, fx + fs * 0.66f, fy + fs * 0.38f, ew, ew);
            }
            using (var mouth = new Pen(Color.FromArgb(255, 180, 80, 80), 3))
            {
                g.DrawArc(mouth, fx + fs * 0.28f, fy + fs * 0.55f, fs * 0.44f, fs * 0.22f, 0, 180);
            }
            using (var hair = new Pen(Color.FromArgb(255, 55, 42, 32), 10))
            {
                g.DrawArc(hair, fx - 5f, fy - 10f, fs + 10f, fs * 0.75f, 180, 180);
            }
        }

        private static Bitmap MakeIdle(Bitmap face)
        {
            var img = NewCanvas();
            using (var g = Graphics.FromImage(img))
            {
                DrawShadow(g);
                DrawSuit(g, Color.FromArgb(255, 65, 95, 155));
            }
            CompositeFace(img, face, 20, 90);
            return img;
        }

        private static Bitmap MakeKowtow(Bitmap face)
        {
            var img = NewCanvas();
            using (var g = Graphics.FromImage(img))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                DrawSuit(g, Color.FromArgb(255, 65, 95, 155));
                // 汗水
                using (var drop = new SolidBrush(Color.FromArgb(200, 150, 200, 255)))
                {
                    g.FillPolygon(drop, new[]
                    {
                        new Point(150, 95), new Point(156, 118), new Point(162, 95)
                    });
                }
            }
            CompositeFace(img, face, 108, 70);
            return img;
        }

        private static Bitmap MakeCrawl(Bitmap face)
        {
            var img = NewCanvas();
            using (var g = Graphics.FromImage(img))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var brush = new SolidBrush(Color.FromArgb(255, 65, 95, 155)))
                {
                    using (var path = RoundedRect(38, 158, 124, 52, 22))
                        g.FillPath(brush, path);
                    using (var path = RoundedRect(28, 172, 28, 44, 8))
                        g.FillPath(brush, path);
                    using (var path = RoundedRect(144, 172, 28, 44, 8))
                        g.FillPath(brush, path);
                }
            }
            CompositeFace(img, face, 100, 62);
            return img;
        }

        private static Bitmap MakeHit(Bitmap face)
        {
            var img = NewCanvas();
            using (var g = Graphics.FromImage(img))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                DrawSuit(g, Color.FromArgb(255, 65, 95, 155));
                // 抱头的手臂
                using (var brush = new SolidBrush(Color.FromArgb(255, 65, 95, 155)))
                {
                    using (var path = RoundedRect(32, 74, 26, 70, 10))
                        g.FillPath(brush, path);
                    using (var path = RoundedRect(142, 74, 26, 70, 10))
                        g.FillPath(brush, path);
                }
                // 抖动线
                using (var pen = new Pen(Color.FromArgb(220, 255, 105, 160), 3))
                {
                    g.DrawLine(pen, 26, 46, 14, 34);
                    g.DrawLine(pen, 174, 46, 186, 34);
                }
            }
            CompositeFace(img, face, 32, 80);
            return img;
        }

        private static Bitmap MakeCannonReady(Bitmap face)
        {
            var img = NewCanvas();
            CompositeFace(img, face, 18, 110);
            using (var g = Graphics.FromImage(img))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                // 眼泪
                using (var tear = new SolidBrush(Color.FromArgb(220, 150, 200, 255)))
                {
                    g.FillPolygon(tear, new[]
                    {
                        new Point(62, 88), new Point(56, 122), new Point(68, 88)
                    });
                    g.FillPolygon(tear, new[]
                    {
                        new Point(138, 88), new Point(144, 122), new Point(132, 88)
                    });
                }
            }
            return img;
        }

        private static Bitmap MakeCannonFire(Bitmap face)
        {
            var img = NewCanvas();
            CompositeFace(img, face, 28, 90);
            using (var g = Graphics.FromImage(img))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var pen = new Pen(Color.FromArgb(180, 255, 200, 60), 4))
                {
                    g.DrawLine(pen, 26, 52, 6, 40);
                    g.DrawLine(pen, 26, 104, 6, 116);
                }
            }
            return img;
        }

        private static Bitmap MakeFlyingOut(Bitmap face)
        {
            var img = NewCanvas();
            CompositeFace(img, face, 40, 80);
            using (var g = Graphics.FromImage(img))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var pen = new Pen(Color.FromArgb(180, 255, 255, 255), 4))
                {
                    g.DrawLine(pen, 30, 62, 6, 50);
                    g.DrawLine(pen, 30, 112, 6, 124);
                    g.DrawLine(pen, 30, 162, 12, 172);
                }
            }
            return img;
        }

        private static Bitmap MakeCow()
        {
            var img = NewCanvas();
            using (var g = Graphics.FromImage(img))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var body = new SolidBrush(Color.White))
                using (var pen = new Pen(Color.FromArgb(255, 80, 80, 80), 2))
                {
                    using (var path = RoundedRect(38, 118, 124, 84, 26))
                    {
                        g.FillPath(body, path);
                        g.DrawPath(pen, path);
                    }
                    g.FillEllipse(body, 138, 98, 42, 52);
                    g.DrawEllipse(pen, 138, 98, 42, 52);
                    using (var path = RoundedRect(48, 194, 22, 52, 6))
                    {
                        g.FillPath(body, path);
                        g.DrawPath(pen, path);
                    }
                    using (var path = RoundedRect(130, 194, 22, 52, 6))
                    {
                        g.FillPath(body, path);
                        g.DrawPath(pen, path);
                    }
                }
                using (var spot = new SolidBrush(Color.FromArgb(255, 35, 35, 35)))
                {
                    g.FillEllipse(spot, 58, 128, 32, 30);
                    g.FillEllipse(spot, 118, 158, 26, 32);
                    g.FillEllipse(spot, 154, 116, 10, 10);
                }
                using (var horn = new SolidBrush(Color.FromArgb(255, 205, 185, 125)))
                {
                    g.FillPolygon(horn, new[]
                    {
                        new Point(150, 104), new Point(143, 82), new Point(160, 98)
                    });
                    g.FillPolygon(horn, new[]
                    {
                        new Point(172, 104), new Point(183, 82), new Point(167, 98)
                    });
                }
                using (var dust = new SolidBrush(Color.FromArgb(120, 180, 160, 140)))
                {
                    g.FillEllipse(dust, 16, 208, 34, 28);
                    g.FillEllipse(dust, 0, 214, 30, 26);
                }
            }
            return img;
        }

        private static Bitmap MakeWhip()
        {
            var img = new Bitmap(120, 120, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(img))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var handle = new SolidBrush(Color.FromArgb(255, 120, 80, 50)))
                using (var path = RoundedRect(50, 58, 20, 52, 5))
                {
                    g.FillPath(handle, path);
                }
                using (var lash = new Pen(Color.FromArgb(255, 85, 55, 32), 4))
                {
                    g.DrawArc(lash, 18, 8, 84, 62, 200, 140);
                }
            }
            return img;
        }

        private static Bitmap MakeCannon()
        {
            var img = new Bitmap(180, 180, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(img))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var wheel = new SolidBrush(Color.FromArgb(255, 60, 60, 62)))
                using (var pen = new Pen(Color.FromArgb(255, 30, 30, 30), 3))
                {
                    g.FillEllipse(wheel, 20, 128, 42, 42);
                    g.DrawEllipse(pen, 20, 128, 42, 42);
                    g.FillEllipse(wheel, 118, 128, 42, 42);
                    g.DrawEllipse(pen, 118, 128, 42, 42);
                }
                using (var metal = new SolidBrush(Color.FromArgb(255, 96, 96, 100)))
                using (var pen = new Pen(Color.FromArgb(255, 55, 55, 58), 2))
                {
                    using (var path = RoundedRect(30, 108, 120, 32, 5))
                    {
                        g.FillPath(metal, path);
                        g.DrawPath(pen, path);
                    }
                    using (var path = RoundedRect(50, 18, 80, 100, 8))
                    {
                        g.FillPath(metal, path);
                        g.DrawPath(pen, path);
                    }
                    using (var path = RoundedRect(45, 12, 90, 24, 7))
                    {
                        g.FillPath(metal, path);
                        g.DrawPath(pen, path);
                    }
                }
            }
            return img;
        }

        private static Bitmap MakeAppIcon()
        {
            var img = new Bitmap(64, 64, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(img))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var bg = new SolidBrush(Color.FromArgb(255, 255, 105, 180)))
                {
                    g.FillEllipse(bg, 2, 2, 60, 60);
                }
                using (var face = new SolidBrush(Color.White))
                {
                    g.FillEllipse(face, 14, 14, 36, 36);
                }
                using (var eye = new SolidBrush(Color.FromArgb(255, 50, 50, 50)))
                {
                    g.FillEllipse(eye, 22, 26, 7, 7);
                    g.FillEllipse(eye, 35, 26, 7, 7);
                }
                using (var mouth = new Pen(Color.FromArgb(255, 180, 40, 80), 2))
                {
                    g.DrawArc(mouth, 22, 32, 20, 14, 0, 180);
                }
            }
            return img;
        }

        private static void Save(Bitmap img, string name)
        {
            string path = Path.Combine(AssetsDir, name);
            if (name.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
            {
                SaveIcon(img, path);
            }
            else
            {
                img.Save(path, ImageFormat.Png);
            }
            img.Dispose();
        }

        private static void SaveIcon(Bitmap img, string path)
        {
            // PNG 内嵌的 ICO（Vista+ 支持）
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write((ushort)0);      // reserved
                bw.Write((ushort)1);      // type: icon
                bw.Write((ushort)1);      // count

                byte[] pngBytes;
                using (var ms = new MemoryStream())
                {
                    img.Save(ms, ImageFormat.Png);
                    pngBytes = ms.ToArray();
                }

                bw.Write((byte)img.Width);    // width
                bw.Write((byte)img.Height);   // height
                bw.Write((byte)0);            // colors
                bw.Write((byte)0);            // reserved
                bw.Write((ushort)1);          // planes
                bw.Write((ushort)32);         // bpp
                bw.Write((uint)pngBytes.Length);
                bw.Write(6 + 16);             // offset
                bw.Write(pngBytes);
            }
        }
    }
}
