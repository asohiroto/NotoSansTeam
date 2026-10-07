# リスポーン用テクスチャ: 足元（画像の中心）から上へ立つ光の柱（セル調: 白い芯 + 少し暗い縁）
# 使い方: powershell -NoProfile -ExecutionPolicy Bypass -File gen_respawn_tex.ps1 -OutDir <絶対パス> [-Suffix v1]
param(
    [Parameter(Mandatory = $true)][string]$OutDir,
    [string]$Suffix = "v1"
)

$code = @"
using System;
using System.Drawing;
using System.Drawing.Imaging;

public static class RespawnTex
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

    // 光の柱: 下端は地面（y=0）で平ら、上へ行くほど細くなって尖る
    public static void Pillar(string path, int size)
    {
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        Draw(bmp, size, (double x, double y, out double t) =>
        {
            t = 0;
            const double top = 0.95, baseHalf = 0.42;
            if (y < 0 || y > top) return false;
            double s = y / top;                              // 0 = 足元, 1 = 先端
            double half = baseHalf * (1 - Math.Pow(s, 2.2)); // 上の方で急に細くなる
            double ax = Math.Abs(x);
            if (ax >= half) return false;
            t = ax < half * 0.55 ? 1.0 : 0.70;               // 白い芯 / 縁
            return true;
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
            using (var pen = new Pen(Color.FromArgb(255, 255, 0, 255), 1))
            {
                g.DrawRectangle(pen, 0, 0, s.Width - 1, s.Height - 1);
                g.DrawLine(pen, 0, s.Height / 2, s.Width, s.Height / 2);
            }
            b.Save(dst, ImageFormat.Png);
        }
    }
}
"@
Add-Type -TypeDefinition $code -ReferencedAssemblies System.Drawing

$texDir = Join-Path $OutDir "Texture"
$prevDir = Join-Path $OutDir "review"
New-Item -ItemType Directory -Force $texDir, $prevDir | Out-Null
$pillar = Join-Path $texDir "respawn_pillar_$Suffix.png"
[RespawnTex]::Pillar($pillar, 256)
[RespawnTex]::Preview($pillar, (Join-Path $prevDir "tex_pillar.png"))
Get-ChildItem $texDir | Select-Object Name, Length
