# アニメ調エフェクト用テクスチャ生成（白ベース + セル調の明暗。色は Effekseer / Unity 側で付ける）
# 使い方: powershell -NoProfile -ExecutionPolicy Bypass -File gen_anime_tex.ps1 -OutDir <絶対パス> [-Suffix v1]
param(
    [Parameter(Mandatory = $true)][string]$OutDir,
    [string]$Suffix = "v1"
)

$code = @"
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;

public static class AnimeTex
{
    // 多角形の内外判定（座標は -1..1）
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

    // 形の評価関数: (x, y) -> (トーン 0..1, 不透明か)。トーンは白ベースの明るさ
    public delegate bool Shape(double x, double y, out double tone);

    // 1コマを描く。ss x ss のスーパーサンプリングでアンチエイリアス。外周 margin は必ず透明
    static void DrawCell(Bitmap bmp, int ox, int oy, int size, Shape shape)
    {
        const int ss = 4;
        int margin = 6;
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
                    double y = ((py + (sy + 0.5) / ss) / size) * 2 - 1;
                    double t;
                    if (shape(x, y, out t)) { cov += 1; toneSum += t; }
                }
            }
            double a = cov / (ss * ss);
            double tone = cov > 0 ? toneSum / cov : 0;
            int v = (int)Math.Round(Math.Max(0, Math.Min(1, tone)) * 255);
            bmp.SetPixel(ox + px, oy + py, Color.FromArgb((int)Math.Round(a * 255), v, v, v));
        }
    }

    static Bitmap NewBmp(int w, int h)
    {
        var b = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(b)) g.Clear(Color.FromArgb(0, 0, 0, 0));
        return b;
    }

    // 衝撃リング: くっきりした輪（内側に細い明るい線）
    public static void Ring(string path, int size)
    {
        var bmp = NewBmp(size, size);
        DrawCell(bmp, 0, 0, size, (double x, double y, out double t) =>
        {
            double r = Math.Sqrt(x * x + y * y);
            t = 1.0;
            return r > 0.72 && r < 0.92;
        });
        bmp.Save(path, ImageFormat.Png);
    }

    // 爆発の閃光: ギザギザの星形（スパイクの長さを少し不揃いに）
    public static void Burst(string path, int size, int spikes, int seed)
    {
        var rng = new Random(seed);
        int n = spikes * 2;
        var px = new double[n]; var py = new double[n];
        for (int i = 0; i < n; i++)
        {
            double ang = Math.PI * 2 * i / n + (rng.NextDouble() - 0.5) * 0.12;
            double rad = (i % 2 == 0) ? 0.78 + rng.NextDouble() * 0.14 : 0.30 + rng.NextDouble() * 0.08;
            px[i] = Math.Cos(ang) * rad; py[i] = Math.Sin(ang) * rad;
        }
        var bmp = NewBmp(size, size);
        DrawCell(bmp, 0, 0, size, (double x, double y, out double t) =>
        {
            t = 1.0;
            return InPoly(x, y, px, py);
        });
        bmp.Save(path, ImageFormat.Png);
    }

    // 破片: 不揃いな多角形 + セル調3段（影 / 本体 / ハイライト）+ 暗い輪郭。2x2 シート
    public static void Chunks(string path, int cell, int seed)
    {
        var bmp = NewBmp(cell * 2, cell * 2);
        var rng = new Random(seed);
        for (int c = 0; c < 4; c++)
        {
            int verts = 5 + rng.Next(3);
            var px = new double[verts]; var py = new double[verts];
            double start = rng.NextDouble() * Math.PI * 2;
            for (int i = 0; i < verts; i++)
            {
                double ang = start + Math.PI * 2 * i / verts + (rng.NextDouble() - 0.5) * 0.5;
                double rad = 0.62 + rng.NextDouble() * 0.26;
                px[i] = Math.Cos(ang) * rad; py[i] = Math.Sin(ang) * rad;
            }
            double[] ix = Scale(px, 0.86), iy = Scale(py, 0.86);           // 輪郭の内側
            double[] bx = Shift(Scale(px, 0.80), -0.07), by = Shift(Scale(py, 0.80), -0.07); // 本体（左上へ寄せる）
            double[] hx = Shift(Scale(px, 0.36), -0.22), hy = Shift(Scale(py, 0.36), -0.22); // ハイライト
            DrawCell(bmp, (c % 2) * cell, (c / 2) * cell, cell, (double x, double y, out double t) =>
            {
                if (!InPoly(x, y, px, py)) { t = 0; return false; }
                if (!InPoly(x, y, ix, iy)) { t = 0.28; return true; }   // 輪郭
                if (InPoly(x, y, hx, hy)) { t = 1.0; return true; }     // ハイライト
                if (InPoly(x, y, bx, by)) { t = 0.80; return true; }    // 本体
                t = 0.52; return true;                                  // 影
            });
        }
        bmp.Save(path, ImageFormat.Png);
    }

    // 煙玉: 円の和集合 + セル調2段（影 / 明部）。2x2 シート
    public static void Smoke(string path, int cell, int seed)
    {
        var bmp = NewBmp(cell * 2, cell * 2);
        var rng = new Random(seed);
        for (int c = 0; c < 4; c++)
        {
            int k = 3 + rng.Next(2);
            var cx = new double[k]; var cy = new double[k]; var cr = new double[k];
            cx[0] = 0; cy[0] = 0.05; cr[0] = 0.52;
            for (int i = 1; i < k; i++)
            {
                double ang = Math.PI * 2 * i / (k - 1) + rng.NextDouble() * 0.8;
                cx[i] = Math.Cos(ang) * 0.36; cy[i] = Math.Sin(ang) * 0.30;
                cr[i] = 0.30 + rng.NextDouble() * 0.12;
            }
            DrawCell(bmp, (c % 2) * cell, (c / 2) * cell, cell, (double x, double y, out double t) =>
            {
                bool inside = false, lit = false;
                for (int i = 0; i < k; i++)
                {
                    double dx = x - cx[i], dy = y - cy[i];
                    if (dx * dx + dy * dy < cr[i] * cr[i]) inside = true;
                    // 明部 = 左上へずらして縮めた円
                    double lx = x - (cx[i] - 0.08), ly = y - (cy[i] - 0.10), lr = cr[i] * 0.86;
                    if (lx * lx + ly * ly < lr * lr) lit = true;
                }
                t = lit ? 1.0 : 0.70;
                return inside;
            });
        }
        bmp.Save(path, ImageFormat.Png);
    }

    static double[] Scale(double[] a, double s) { var r = new double[a.Length]; for (int i = 0; i < a.Length; i++) r[i] = a[i] * s; return r; }
    static double[] Shift(double[] a, double d) { var r = new double[a.Length]; for (int i = 0; i < a.Length; i++) r[i] = a[i] + d; return r; }

    // 確認用: 灰色の背景に合成し、コマ境界線を引く
    public static void Preview(string src, string dst, int cells)
    {
        using (var s = new Bitmap(src))
        using (var b = new Bitmap(s.Width, s.Height, PixelFormat.Format32bppArgb))
        using (var g = Graphics.FromImage(b))
        {
            g.Clear(Color.FromArgb(255, 40, 44, 52));
            g.DrawImage(s, 0, 0, s.Width, s.Height);
            using (var pen = new Pen(Color.FromArgb(255, 255, 0, 255), 1))
            {
                int step = s.Width / cells;
                for (int i = 0; i <= cells; i++) { g.DrawLine(pen, i * step, 0, i * step, s.Height); g.DrawLine(pen, 0, i * step, s.Width, i * step); }
            }
            b.Save(dst, ImageFormat.Png);
        }
    }
}
"@

Add-Type -TypeDefinition $code -ReferencedAssemblies System.Drawing

$texDir = Join-Path $OutDir "Texture"
New-Item -ItemType Directory -Force $texDir | Out-Null
$prevDir = Join-Path $OutDir "review"
New-Item -ItemType Directory -Force $prevDir | Out-Null

$ring   = Join-Path $texDir "bb_ring_$Suffix.png"
$burst  = Join-Path $texDir "bb_burst_$Suffix.png"
$chunks = Join-Path $texDir "bb_chunks_$Suffix.png"
$smoke  = Join-Path $texDir "bb_smoke_$Suffix.png"

[AnimeTex]::Ring($ring, 256)
[AnimeTex]::Burst($burst, 256, 8, 11)
[AnimeTex]::Chunks($chunks, 128, 7)
[AnimeTex]::Smoke($smoke, 128, 5)

[AnimeTex]::Preview($ring,   (Join-Path $prevDir "tex_ring.png"), 1)
[AnimeTex]::Preview($burst,  (Join-Path $prevDir "tex_burst.png"), 1)
[AnimeTex]::Preview($chunks, (Join-Path $prevDir "tex_chunks.png"), 2)
[AnimeTex]::Preview($smoke,  (Join-Path $prevDir "tex_smoke.png"), 2)

Get-ChildItem $texDir | Select-Object Name, Length
