using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

class MakeIcon
{
    static Color Light = Color.FromArgb(255, 255, 255, 255);
    static Color Dark  = Color.FromArgb(255, 128, 128, 128);
    static Color Mid   = Color.FromArgb(255, 192, 192, 192);
    static Color Black = Color.FromArgb(255, 0, 0, 0);

    [STAThread]
    static void Main(string[] args)
    {
        string outPath = args.Length > 0 ? args[0] : "browser.ico";
        int size = 256;

        using (var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb))
        {
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                int pad = (int)(size * 0.04f);
                Rectangle outer = new Rectangle(pad, pad, size - 2 * pad, size - 2 * pad);

                // Светлая внешняя кайма
                using (var pen = new Pen(Light, size * 0.02f))
                    g.DrawRectangle(pen, outer);

                // Объёмная серебристая рамка: свет сверху-слева, тень снизу-справа
                int bevel = (int)(size * 0.10f);
                Rectangle face = new Rectangle(outer.X + bevel, outer.Y + bevel,
                    outer.Width - 2 * bevel, outer.Height - 2 * bevel);

                // Фон лицевой части — дымчато-голубой
                using (var bg = new LinearGradientBrush(face,
                    Color.FromArgb(255, 200, 225, 245),
                    Color.FromArgb(255, 140, 175, 215),
                    LinearGradientMode.Vertical))
                    g.FillRectangle(bg, face);
                using (var pen = new Pen(Dark, size * 0.015f))
                    g.DrawRectangle(pen, face);

                // Верхняя и левая грани рамки (светлые)
                using (var penLt = new Pen(Light, size * 0.035f))
                {
                    g.DrawLine(penLt, outer.Left, outer.Top, outer.Right, outer.Top);
                    g.DrawLine(penLt, outer.Left, outer.Top, outer.Left, outer.Bottom);
                }
                // Нижняя и правая грани рамки (тёмные) = 3D-фаска
                using (var penDk = new Pen(Dark, size * 0.035f))
                {
                    g.DrawLine(penDk, outer.Left, outer.Bottom, outer.Right, outer.Bottom);
                    g.DrawLine(penDk, outer.Right, outer.Top, outer.Right, outer.Bottom);
                }

                // Синяя буква "e" в центре (толстая, классическая)
                string eText = "e";
                using (var font = new Font("Arial", size * 0.58f, FontStyle.Bold, GraphicsUnit.Pixel))
                {
                    var sz = g.MeasureString(eText, font);
                    float x = face.X + (face.Width - sz.Width) / 2f;
                    float y = face.Y + (face.Height - sz.Height) / 2f;

                    // Простая тень
                    using (var sh = new SolidBrush(Color.FromArgb(90, 0, 0, 0)))
                        g.DrawString(eText, font, sh, x + size * 0.01f, y + size * 0.012f);

                    using (var tBrush = new LinearGradientBrush(
                        new RectangleF(face.X, face.Y, face.Width, face.Height),
                        Color.FromArgb(255, 90, 160, 230),
                        Color.FromArgb(255, 15, 60, 160),
                        LinearGradientMode.Vertical))
                        g.DrawString(eText, font, tBrush, x, y);
                }
            }

            using (var fs = new FileStream(outPath, FileMode.Create))
            {
                fs.WriteByte(0); fs.WriteByte(0);
                fs.WriteByte(1); fs.WriteByte(0);
                ushort count = 1;
                fs.WriteByte((byte)count); fs.WriteByte((byte)(count >> 8));

                using (var ms = new MemoryStream())
                {
                    bmp.Save(ms, ImageFormat.Png);
                    byte[] png = ms.ToArray();
                    fs.WriteByte(0); fs.WriteByte(0);
                    fs.WriteByte(0); fs.WriteByte(0);
                    ushort planes = 1; fs.WriteByte((byte)planes); fs.WriteByte((byte)(planes >> 8));
                    ushort bpp = 32; fs.WriteByte((byte)bpp); fs.WriteByte((byte)(bpp >> 8));
                    WriteLE32(fs, (uint)png.Length);
                    WriteLE32(fs, 22);
                    fs.Write(png, 0, png.Length);
                }
            }

            // Дополнительно: PNG-версия логотипа для start.html
            string pngPath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(outPath)), "logo.png");
            using (var pbmp = new Bitmap(bmp, 72, 72))
                pbmp.Save(pngPath, ImageFormat.Png);
        }

        Console.WriteLine("ICON_OK:" + Path.GetFullPath(outPath) + " size=" + new FileInfo(outPath).Length);
    }

    static void WriteLE32(Stream s, uint v)
    {
        s.WriteByte((byte)(v & 0xFF));
        s.WriteByte((byte)((v >> 8) & 0xFF));
        s.WriteByte((byte)((v >> 16) & 0xFF));
        s.WriteByte((byte)((v >> 24) & 0xFF));
    }
}