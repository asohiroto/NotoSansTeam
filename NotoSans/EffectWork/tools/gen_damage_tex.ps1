# 被ダメージ用テクスチャ: 漫画の「バン！」のような不揃いなギザギザ（外側用・内側用）
# 使い方: powershell -NoProfile -ExecutionPolicy Bypass -File gen_damage_tex.ps1 -OutDir <絶対パス> [-Suffix v1]
param(
    [Parameter(Mandatory = $true)][string]$OutDir,
    [string]$Suffix = "v1"
)

$code = @"
using System;
using System.Drawing;
using System.Drawing.Imaging;

public static class DamageTex
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

    static bool InPoly(double x, double y, double[] px, double[] py)
    {
        bool inside = false;
        for (int i = 0, j = px.Length - 1; i < px.Length; j = i++)
        {
            if (((py[i] > y) != (py[j] > y)) && (x < (px[j] - px[i]) * (y - py[i]) / (py[j] - py[i]) + px[i]))
                inside = !inside;
        }
        return inside;
    }

    // 不揃いなギザギザ: トゲの長さ・間隔・谷の深さをばらつかせる（規則的な星形に見えないように）
    public static void Jagged(string path, int size, int spikes, double rOutMin, double rOutMax, double rIn, int seed)
    {
        var rng = new Random(seed);
        int n = spikes * 2;
        var px = new double[n]; var py = new double[n];
        // 角度の刻みをばらつかせ、合計がちょうど 1 周になるよう正規化する
        var steps = new double[n];
        double sum = 0;
        for (int i = 0; i < n; i++) { steps[i] = 0.7 + rng.NextDouble() * 0.6; sum += steps[i]; }
        double ang = rng.NextDouble() * Math.PI * 2;
        for (int i = 0; i < n; i++)
        {
            double rad = (i % 2 == 0) ? rOutMin + rng.NextDouble() * (rOutMax - rOutMin) : rIn * (0.85 + rng.NextDouble() * 0.3);
            px[i] = Math.Cos(ang) * rad; py[i] = Math.Sin(ang) * rad;
            ang += steps[i] / sum * Math.PI * 2;
        }
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        Draw(bmp, size, (double x, double y, out double t) => { t = 1.0; return InPoly(x, y, px, py); });
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
$outer = Join-Path $texDir "dmg_outer_$Suffix.png"
$inner = Join-Path $texDir "dmg_inner_$Suffix.png"
[DamageTex]::Jagged($outer, 256, 11, 0.72, 0.95, 0.42, 8)
[DamageTex]::Jagged($inner, 256, 9, 0.45, 0.62, 0.26, 15)
[DamageTex]::Preview($outer, (Join-Path $prevDir "tex_outer.png"))
[DamageTex]::Preview($inner, (Join-Path $prevDir "tex_inner.png"))
Get-ChildItem $texDir | Select-Object Name, Length
