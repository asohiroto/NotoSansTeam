# ヒット用テクスチャ（白ベース・アニメ調）
# 使い方: powershell -NoProfile -ExecutionPolicy Bypass -File gen_hit_tex.ps1 -OutDir <絶対パス> [-Suffix v1]
param(
    [Parameter(Mandatory = $true)][string]$OutDir,
    [string]$Suffix = "v1"
)

$code = @"
using System;
using System.Drawing;
using System.Drawing.Imaging;

public static class HitTex
{
    public delegate bool Shape(double x, double y, out double tone);

    // 1枚を描く（4x4 スーパーサンプリング、外周 6px は必ず透明）
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
                    double y = 1 - ((py + (sy + 0.5) / ss) / size) * 2; // 上が +y
                    double t;
                    if (shape(x, y, out t)) { cov += 1; toneSum += t; }
                }
            }
            double a = cov / (ss * ss);
            int v = (int)Math.Round(Math.Max(0, Math.Min(1, cov > 0 ? toneSum / cov : 0)) * 255);
            bmp.SetPixel(px, py, Color.FromArgb((int)Math.Round(a * 255), v, v, v));
        }
    }

    // 4本光のスパーク: 縦横に細く尖った菱形を2本重ね、中心に小さな円
    public static void Sparkle(string path, int size)
    {
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        Draw(bmp, size, (double x, double y, out double t) =>
        {
            t = 1.0;
            double ax = Math.Abs(x), ay = Math.Abs(y);
            bool vertical = ax / 0.13 + ay / 0.92 < 1.0;   // 縦長の菱形
            bool horizontal = ax / 0.78 + ay / 0.11 < 1.0; // 横長の菱形（少し短く）
            bool core = x * x + y * y < 0.2 * 0.2;
            return vertical || horizontal || core;
        });
        bmp.Save(path, ImageFormat.Png);
    }

    // 放射線: 上半分（y=0.30..0.95）にだけある、根元が細く中ほどが太く先が尖った線
    public static void Ray(string path, int size)
    {
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        Draw(bmp, size, (double x, double y, out double t) =>
        {
            t = 1.0;
            if (y < 0.30 || y > 0.95) return false;
            double s = (y - 0.30) / 0.65;                  // 0 = 根元, 1 = 先端
            double half = 0.075 * Math.Sin(Math.PI * Math.Pow(s, 0.8)); // 中ほどが太い葉形
            return Math.Abs(x) < half;
        });
        bmp.Save(path, ImageFormat.Png);
    }

    // 放射線の束: 中心から等間隔に count 本（角度と長さを少し不揃いに）。根元 r0、先端 r1
    public static void RayBurst(string path, int size, int count, int seed)
    {
        var rng = new Random(seed);
        var ang = new double[count]; var len = new double[count]; var wid = new double[count];
        for (int i = 0; i < count; i++)
        {
            ang[i] = Math.PI * 2 * i / count + (rng.NextDouble() - 0.5) * 0.25;
            len[i] = 0.78 + rng.NextDouble() * 0.17;
            wid[i] = 0.045 + rng.NextDouble() * 0.02;
        }
        const double r0 = 0.40;
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        Draw(bmp, size, (double x, double y, out double t) =>
        {
            t = 1.0;
            for (int i = 0; i < count; i++)
            {
                double ca = Math.Cos(ang[i]), sa = Math.Sin(ang[i]);
                double along = x * ca + y * sa;     // 線の向きの距離
                double across = -x * sa + y * ca;   // 線と直角の距離
                if (along < r0 || along > len[i]) continue;
                double s = (along - r0) / (len[i] - r0);
                double half = wid[i] * Math.Sin(Math.PI * Math.Pow(s, 0.7));
                if (Math.Abs(across) < half) return true;
            }
            return false;
        });
        bmp.Save(path, ImageFormat.Png);
    }

    // 確認用: 暗い背景に合成
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

$sparkle = Join-Path $texDir "hit_sparkle_$Suffix.png"
$ray = Join-Path $texDir "hit_ray_$Suffix.png"
[HitTex]::Sparkle($sparkle, 256)
[HitTex]::Ray($ray, 256)
[HitTex]::Preview($sparkle, (Join-Path $prevDir "tex_sparkle.png"))
[HitTex]::Preview($ray, (Join-Path $prevDir "tex_ray.png"))
$rayBurst = Join-Path $texDir "hit_rayburst_$Suffix.png"
[HitTex]::RayBurst($rayBurst, 256, 6, 3)
[HitTex]::Preview($rayBurst, (Join-Path $prevDir "tex_rayburst.png"))
Get-ChildItem $texDir | Select-Object Name, Length
