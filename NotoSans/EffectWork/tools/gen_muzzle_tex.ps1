# 発射（マズルフラッシュ）用テクスチャ。画像の中心が銃口、下半分に下向きの扇を描く
# 使い方: powershell -NoProfile -ExecutionPolicy Bypass -File gen_muzzle_tex.ps1 -OutDir <絶対パス> [-Suffix v1]
param(
    [Parameter(Mandatory = $true)][string]$OutDir,
    [string]$Suffix = "v1"
)

$code = @"
using System;
using System.Drawing;
using System.Drawing.Imaging;

public static class MuzzleTex
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

    // 下向きの扇: 真下からの角度 |θ| < spread。半径は角度ごとのトゲ（spikes 本）で不揃い
    static double FanRadius(double theta, double spread, int spikes, double rIn, double rOut, double[] jitter)
    {
        double u = (theta + spread) / (2 * spread) * spikes;     // 0..spikes
        int k = Math.Min(spikes - 1, (int)Math.Floor(u));
        double f = u - k;                                         // トゲ1本の中の位置 0..1
        double tri = 1 - Math.Abs(f - 0.5) * 2;                   // 中央で 1、両端で 0
        double edge = 1 - Math.Pow(Math.Abs(theta) / spread, 3);  // 扇の両端ほど短く
        return (rIn + (rOut * jitter[k] - rIn) * tri) * (0.55 + 0.45 * edge);
    }

    public static void Fan(string path, int size, int seed)
    {
        var rng = new Random(seed);
        const int spikes = 5;
        var jitter = new double[spikes];
        for (int i = 0; i < spikes; i++) jitter[i] = 0.82 + rng.NextDouble() * 0.18;
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        Draw(bmp, size, (double x, double y, out double t) =>
        {
            t = 1.0;
            if (y > 0.0) return false;                 // 銃口より上には描かない
            // 扇の頂点を銃口より少し上に置き、銃口の位置ですでに幅があるようにする
            double ay = y - 0.22;
            double r = Math.Sqrt(x * x + ay * ay);
            double theta = Math.Atan2(x, -ay);         // 真下が 0
            const double spread = 1.0;                 // 約 ±57°
            if (Math.Abs(theta) > spread) return false;
            return r < FanRadius(theta, spread, spikes, 0.70, 1.12, jitter);
        });
        bmp.Save(path, ImageFormat.Png);
    }

    // 芯: 細めの下向きの扇（トゲ3本）
    public static void Core(string path, int size, int seed)
    {
        var rng = new Random(seed);
        const int spikes = 3;
        var jitter = new double[spikes];
        for (int i = 0; i < spikes; i++) jitter[i] = 0.85 + rng.NextDouble() * 0.15;
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        Draw(bmp, size, (double x, double y, out double t) =>
        {
            t = 1.0;
            if (y > 0.0) return false;
            double ay = y - 0.14;
            double r = Math.Sqrt(x * x + ay * ay);
            double theta = Math.Atan2(x, -ay);
            const double spread = 0.7;
            if (Math.Abs(theta) > spread) return false;
            return r < FanRadius(theta, spread, spikes, 0.45, 0.78, jitter);
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
$fan = Join-Path $texDir "muzzle_fan_$Suffix.png"
$core = Join-Path $texDir "muzzle_core_$Suffix.png"
[MuzzleTex]::Fan($fan, 256, 21)
[MuzzleTex]::Core($core, 256, 4)
[MuzzleTex]::Preview($fan, (Join-Path $prevDir "tex_fan.png"))
[MuzzleTex]::Preview($core, (Join-Path $prevDir "tex_core.png"))
Get-ChildItem $texDir | Select-Object Name, Length
