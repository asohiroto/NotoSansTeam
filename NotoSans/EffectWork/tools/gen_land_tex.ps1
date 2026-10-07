# 着地用テクスチャ。画像の中心＝足元（地面）。白ベース、色はノードで付ける
# 使い方: powershell -NoProfile -ExecutionPolicy Bypass -File gen_land_tex.ps1 -OutDir <絶対パス> [-Suffix v1]
param(
    [Parameter(Mandatory = $true)][string]$OutDir,
    [string]$Suffix = "v1"
)

$code = @"
using System;
using System.Drawing;
using System.Drawing.Imaging;

public static class LandTex
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

    static bool InCircle(double x, double y, double cx, double cy, double r)
    {
        double dx = x - cx, dy = y - cy;
        return dx * dx + dy * dy < r * r;
    }

    // 土煙: 足元の左右に、外へ流れる煙のかたまり（円の和集合）。地面（y=0）より下は描かない
    // セル調 2 段: 上側を明るく（左上からの光）、下側を灰色
    public static void Dust(string path, int size)
    {
        // 右側の煙の円（左側は x を反転）。外へ行くほど小さく低い
        double[] cx = { 0.30, 0.52, 0.72, 0.86 };
        double[] cy = { 0.13, 0.11, 0.08, 0.05 };
        double[] cr = { 0.15, 0.13, 0.10, 0.06 };
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        Draw(bmp, size, (double x, double y, out double t) =>
        {
            t = 0;
            if (y < 0.0) return false;
            double ax = Math.Abs(x);
            bool inside = false, lit = false;
            for (int i = 0; i < cx.Length; i++)
            {
                if (InCircle(ax, y, cx[i], cy[i], cr[i])) inside = true;
                if (InCircle(ax, y, cx[i] - 0.03, cy[i] + 0.05, cr[i] * 0.82)) lit = true;
            }
            t = lit ? 1.0 : 0.68;
            return inside;
        });
        bmp.Save(path, ImageFormat.Png);
    }

    // 4 本光の小さな星
    static bool Star4(double x, double y, double cx, double cy, double s)
    {
        double ax = Math.Abs(x - cx) / s, ay = Math.Abs(y - cy) / s;
        return ax / 0.16 + ay / 1.0 < 1.0 || ax / 1.0 + ay / 0.16 < 1.0 || (ax * ax + ay * ay) < 0.06;
    }

    // 補充の輝き: 小さな星 3 つ（左・中央やや上・右）
    public static void Sparks(string path, int size)
    {
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        Draw(bmp, size, (double x, double y, out double t) =>
        {
            t = 1.0;
            return Star4(x, y, -0.42, -0.05, 0.22) || Star4(x, y, 0.0, 0.30, 0.30) || Star4(x, y, 0.44, 0.05, 0.20);
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
                g.DrawLine(pen, 0, s.Height / 2, s.Width, s.Height / 2); // 地面の線
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
$dust = Join-Path $texDir "land_dust_$Suffix.png"
$sparks = Join-Path $texDir "land_sparks_$Suffix.png"
[LandTex]::Dust($dust, 256)
[LandTex]::Sparks($sparks, 256)
[LandTex]::Preview($dust, (Join-Path $prevDir "tex_dust.png"))
[LandTex]::Preview($sparks, (Join-Path $prevDir "tex_sparks.png"))
Get-ChildItem $texDir | Select-Object Name, Length
