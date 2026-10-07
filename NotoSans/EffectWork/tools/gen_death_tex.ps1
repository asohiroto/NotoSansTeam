# 倒れる演出用テクスチャ: 輪に並んだ丸い玉（セル調 2 段: 白い芯 + 少し暗い縁）
# 使い方: powershell -NoProfile -ExecutionPolicy Bypass -File gen_death_tex.ps1 -OutDir <絶対パス> [-Suffix v1]
param(
    [Parameter(Mandatory = $true)][string]$OutDir,
    [string]$Suffix = "v1"
)

$code = @"
using System;
using System.Drawing;
using System.Drawing.Imaging;

public static class DeathTex
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

    // count 個の玉を半径 ringR の輪に並べる。offset は輪全体の回転（ラジアン）
    public static void OrbRing(string path, int size, int count, double ringR, double orbR, double offset)
    {
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        Draw(bmp, size, (double x, double y, out double t) =>
        {
            t = 0;
            for (int i = 0; i < count; i++)
            {
                double a = offset + Math.PI * 2 * i / count;
                double dx = x - Math.Cos(a) * ringR, dy = y - Math.Sin(a) * ringR;
                double d2 = dx * dx + dy * dy;
                if (d2 < orbR * orbR)
                {
                    // 芯（左上に寄せた小さな円）は白、残りは少し暗い縁
                    double hx = dx + orbR * 0.18, hy = dy - orbR * 0.18;
                    t = (hx * hx + hy * hy < (orbR * 0.62) * (orbR * 0.62)) ? 1.0 : 0.72;
                    return true;
                }
            }
            return false;
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
$ringA = Join-Path $texDir "death_orbs_a_$Suffix.png"
$ringB = Join-Path $texDir "death_orbs_b_$Suffix.png"
[DeathTex]::OrbRing($ringA, 256, 8, 0.80, 0.11, 0.0)
[DeathTex]::OrbRing($ringB, 256, 8, 0.80, 0.09, [Math]::PI / 8)
[DeathTex]::Preview($ringA, (Join-Path $prevDir "tex_orbs_a.png"))
[DeathTex]::Preview($ringB, (Join-Path $prevDir "tex_orbs_b.png"))
Get-ChildItem $texDir | Select-Object Name, Length
