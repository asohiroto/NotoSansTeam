# 透過キャプチャ（白ベースのエフェクト）に Unity 側で掛ける色を乗算し、暗い背景に合成して縦に並べる
# 使い方: tint_preview.ps1 -Out <png> -Rows "ラベル|src.png|#RRGGBB,ラベル|src.png|#RRGGBB"
param(
    [Parameter(Mandatory = $true)][string]$Out,
    [Parameter(Mandatory = $true)][string]$Rows
)

$code = @"
using System;
using System.Drawing;
using System.Drawing.Imaging;

public static class TintPreview
{
    static int Clamp(int v) { return Math.Max(0, Math.Min(255, v)); }

    public static void Compose(string[] labels, string[] srcs, string[] tints, string outPath)
    {
        Color bg = Color.FromArgb(255, 40, 44, 52);
        int labelW = 150;
        var bmps = new Bitmap[srcs.Length];
        int w = 0, h = 0;
        for (int i = 0; i < srcs.Length; i++)
        {
            bmps[i] = new Bitmap(srcs[i]);
            w = Math.Max(w, bmps[i].Width);
            h += bmps[i].Height;
        }
        using (var dst = new Bitmap(labelW + w, h, PixelFormat.Format32bppArgb))
        {
            using (var g = Graphics.FromImage(dst)) g.Clear(bg);
            int oy = 0;
            for (int i = 0; i < bmps.Length; i++)
            {
                Color tint = ColorTranslator.FromHtml(tints[i]);
                var s = bmps[i];
                for (int y = 0; y < s.Height; y++)
                for (int x = 0; x < s.Width; x++)
                {
                    Color c = s.GetPixel(x, y);
                    double a = c.A / 255.0;
                    int r = (int)Math.Round(bg.R * (1 - a) + c.R * tint.R / 255.0 * a);
                    int gg = (int)Math.Round(bg.G * (1 - a) + c.G * tint.G / 255.0 * a);
                    int b = (int)Math.Round(bg.B * (1 - a) + c.B * tint.B / 255.0 * a);
                    dst.SetPixel(labelW + x, oy + y, Color.FromArgb(255, Clamp(r), Clamp(gg), Clamp(b)));
                }
                using (var g = Graphics.FromImage(dst))
                using (var font = new Font("Yu Gothic UI", 13, FontStyle.Bold))
                    g.DrawString(labels[i], font, Brushes.White, 8, oy + 8);
                oy += s.Height;
                s.Dispose();
            }
            dst.Save(outPath, ImageFormat.Png);
        }
    }
}
"@
Add-Type -TypeDefinition $code -ReferencedAssemblies System.Drawing

$labels = @(); $srcs = @(); $tints = @()
foreach ($row in $Rows.Split(',')) {
    $parts = $row.Split('|')
    $labels += $parts[0]; $srcs += $parts[1]; $tints += $parts[2]
}
[TintPreview]::Compose([string[]]$labels, [string[]]$srcs, [string[]]$tints, $Out)
"saved $Out"
