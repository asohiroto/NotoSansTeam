# 弾用テクスチャ: セル調の玉と、上へ伸びる尾（白ベース、色はノードで付ける）
# 使い方: powershell -NoProfile -ExecutionPolicy Bypass -File gen_bullet_tex.ps1 -OutDir <絶対パス> [-Suffix v1]
param(
    [Parameter(Mandatory = $true)][string]$OutDir,
    [string]$Suffix = "v1"
)

$code = @"
using System;
using System.Drawing;
using System.Drawing.Imaging;

public static class BulletTex
{
    public delegate bool Shape(double x, double y, out double tone);

    static void Draw(Bitmap bmp, int size, Shape shape)
    {
        const int ss = 4;
        const int margin = 6;
        for (int py = 0; py < size; py++)
        for (int px = 0; px < size; px++)
        {
            double cov = 0, toneSum = 0;
            if (px >= margin && py >= margin && px < size - margin && py < size - margin)
            {
                for (int sy = 0; sy < ss; sy++)
                for (int sx = 0; sx < ss; sx++)
                {
                    double x = ((px + (sx + 0.5) / ss) / size) * 2 - 1;
                    double y = 1 - ((py + (sy + 0.5) / ss) / size) * 2;
                    double t;
                    if (shape(x, y, out t)) { cov += 1; toneSum += t; }
                }
            }
            double a = cov / (ss * ss);
            int v = (int)Math.Round(Math.Max(0, Math.Min(1, cov > 0 ? toneSum / cov : 0)) * 255);
            bmp.SetPixel(px, py, Color.FromArgb((int)Math.Round(a * 255), v, v, v));
        }
    }

    // セル調の玉: 縁（暗め）/ 本体 / 左上のハイライト（白）
    public static void Orb(string path, int size)
    {
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        Draw(bmp, size, (double x, double y, out double t) =>
        {
            double r = Math.Sqrt(x * x + y * y);
            t = 0;
            if (r > 0.90) return false;
            double hx = x + 0.30, hy = y - 0.30;
            if (hx * hx + hy * hy < 0.22 * 0.22) { t = 1.0; return true; } // ハイライト
            t = r > 0.74 ? 0.62 : 0.86;                                      // 縁 / 本体
            return true;
        });
        bmp.Save(path, ImageFormat.Png);
    }

    // 尾: 画像の中心（弾の位置）から上へ伸び、先が細くなる。根元は丸い
    public static void Tail(string path, int size)
    {
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        Draw(bmp, size, (double x, double y, out double t) =>
        {
            t = 1.0;
            const double baseW = 0.30, top = 0.93;
            if (y < 0)
            {
                return x * x + y * y < baseW * baseW;    // 丸い根元
            }
            if (y > top) return false;
            double s = y / top;                            // 0 = 根元, 1 = 先端
            double half = baseW * Math.Pow(1 - s, 1.3);
            return Math.Abs(x) < half;
        });
        bmp.Save(path, ImageFormat.Png);
    }

    public static void Preview(string src, string dst)
    {
        using (var s = new Bitmap(src))
        using (var b = new Bitmap(s.Width, s.Height, PixelFormat.Format32bppArgb))
        using (var g = Graphics.FromImage(b))
        {
            g.Clear(Color.FromArgb(255, 40, 44, 52));
            g.DrawImage(s, 0, 0, s.Width, s.Height);
            using (var pen = new Pen(Color.FromArgb(255, 255, 0, 255), 1)) g.DrawRectangle(pen, 0, 0, s.Width - 1, s.Height - 1);
            b.Save(dst, ImageFormat.Png);
        }
    }
}
"@
Add-Type -TypeDefinition $code -ReferencedAssemblies System.Drawing

$texDir = Join-Path $OutDir "Texture"
$prevDir = Join-Path $OutDir "review"
New-Item -ItemType Directory -Force $texDir, $prevDir | Out-Null
$orb = Join-Path $texDir "bullet_orb_$Suffix.png"
$tail = Join-Path $texDir "bullet_tail_$Suffix.png"
[BulletTex]::Orb($orb, 256)
[BulletTex]::Tail($tail, 256)
[BulletTex]::Preview($orb, (Join-Path $prevDir "tex_orb.png"))
[BulletTex]::Preview($tail, (Join-Path $prevDir "tex_tail.png"))
Get-ChildItem $texDir | Select-Object Name, Length
