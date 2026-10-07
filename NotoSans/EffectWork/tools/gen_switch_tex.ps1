# モード切替用テクスチャ: 円周に沿った 2 本の矢印（回転記号）
# 使い方: powershell -NoProfile -ExecutionPolicy Bypass -File gen_switch_tex.ps1 -OutDir <絶対パス> [-Suffix v1]
param(
    [Parameter(Mandatory = $true)][string]$OutDir,
    [string]$Suffix = "v1"
)

$code = @"
using System;
using System.Drawing;
using System.Drawing.Imaging;

public static class SwitchTex
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

    // 角度 a が [start, start+len] に入っているときの、start からの位置（ラジアン）。入っていなければ -1
    static double AngleInto(double a, double start, double len)
    {
        double d = a - start;
        while (d < 0) d += Math.PI * 2;
        while (d >= Math.PI * 2) d -= Math.PI * 2;
        return d <= len ? d : -1;
    }

    // 反時計回りの矢印を 2 本（180° ずらし）。帯の半径 R、太さ W、弧の長さ arcLen、矢じりの長さ headLen
    public static void Arrows(string path, int size)
    {
        const double R = 0.70, W = 0.075, arcLen = 2.2, headLen = 0.42, headW = 0.17;
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        Draw(bmp, size, (double x, double y, out double t) =>
        {
            t = 1.0;
            double r = Math.Sqrt(x * x + y * y);
            double a = Math.Atan2(y, x);
            for (int k = 0; k < 2; k++)
            {
                double start = Math.PI * k + 0.35;
                // 弧の帯（根元側は細く始める）
                double s = AngleInto(a, start, arcLen);
                if (s >= 0)
                {
                    double w = W * Math.Min(1.0, 0.35 + s / 0.6);
                    if (Math.Abs(r - R) < w) return true;
                }
                // 矢じり: 弧の終わりから先へ、幅が 0 になるまで細くなる三角形
                double h = AngleInto(a, start + arcLen, headLen);
                if (h >= 0)
                {
                    double hw = headW * (1 - h / headLen);
                    if (Math.Abs(r - R) < hw) return true;
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
$arrows = Join-Path $texDir "switch_arrows_$Suffix.png"
[SwitchTex]::Arrows($arrows, 256)
[SwitchTex]::Preview($arrows, (Join-Path $prevDir "tex_arrows.png"))
Get-ChildItem $texDir | Select-Object Name, Length
