using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Text;

// 「ゲームクリアー！」のロゴ画像と、それを押し出した3Dモデル（OBJ）を作る
public static class ClearLogoGen
{
    const int W = 2048;
    const int H = 1024;
    // 画像の上の細い帯：濃い紺で塗りつぶし、モデルの側面の色に使う（ロゴ本体はこの下に描く）
    const int TOP = 32;
    const float WorldWidth = 1.4f;
    const float HalfThickness = 0.0375f;

    static readonly Color Navy = Color.FromArgb(255, 16, 24, 64);
    static readonly Color Cream = Color.FromArgb(255, 255, 246, 214);

    public static void Run(string fontPath, string pngPath, string objPath, bool flipX)
    {
        Bitmap bmp = DrawLogo(fontPath);
        bmp.Save(pngPath, ImageFormat.Png);
        BuildMesh(bmp, objPath, flipX);
        bmp.Dispose();
    }

    // ------------------------------------------------------------ 画像

    static Bitmap DrawLogo(string fontPath)
    {
        Bitmap bmp = new Bitmap(W, H, PixelFormat.Format32bppArgb);
        Graphics g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.CompositingQuality = CompositingQuality.HighQuality;
        g.Clear(Color.Transparent);

        PrivateFontCollection pfc = new PrivateFontCollection();
        pfc.AddFontFile(fontPath);
        FontFamily family = pfc.Families[0];

        float cx = W / 2.0f;
        float cy = TOP + (H - TOP) / 2.0f;
        Random rand = new Random(20261018);

        DrawBurst(g, cx, cy, 930.0f, 420.0f, rand);

        // 派手さを足す飾り（文字の後ろ）
        DrawArrowArc(g, 245.0f, 585.0f, 150.0f, 245.0f, 105.0f, 150.0f, false);
        DrawArrowArc(g, 1805.0f, 470.0f, 150.0f, 245.0f, -75.0f, 150.0f, false);
        // 紙吹雪は文字の後ろ（文字に重ならないように）
        DrawConfetti(g, rand);

        // 文字：上の行は青、下の行は大きく橙
        DrawTextLine(g, family, "ゲーム", cx, 295.0f, 800.0f, -4.0f,
            Color.FromArgb(255, 205, 248, 255), Color.FromArgb(255, 95, 205, 248), Color.FromArgb(255, 36, 112, 226), Color.FromArgb(255, 20, 70, 170));
        DrawTextLine(g, family, "クリアー！", cx, 660.0f, 1580.0f, -4.0f,
            Color.FromArgb(255, 255, 244, 120), Color.FromArgb(255, 255, 168, 36), Color.FromArgb(255, 232, 76, 20), Color.FromArgb(255, 170, 40, 10));

        // 星・キラキラ・紙吹雪
        DrawStar(g, 205.0f, 240.0f, 105.0f, -15.0f);
        DrawStar(g, 1850.0f, 215.0f, 125.0f, 12.0f);
        DrawStar(g, 135.0f, 820.0f, 85.0f, 10.0f);
        DrawStar(g, 1925.0f, 810.0f, 105.0f, -10.0f);
        DrawStar(g, 1560.0f, 110.0f, 70.0f, 20.0f);
        DrawStar(g, 480.0f, 925.0f, 62.0f, -8.0f);
        DrawStar(g, 1560.0f, 940.0f, 72.0f, 14.0f);
        DrawSparkle(g, 420.0f, 150.0f, 62.0f);
        DrawSparkle(g, 1700.0f, 345.0f, 56.0f);
        DrawSparkle(g, 330.0f, 470.0f, 46.0f);
        DrawSparkle(g, 1010.0f, 100.0f, 50.0f);
        DrawSparkle(g, 1230.0f, 960.0f, 52.0f);
        DrawSparkle(g, 880.0f, 955.0f, 44.0f);

        g.ResetClip();
        // 上の帯（モデルの側面の色）
        g.FillRectangle(new SolidBrush(Navy), 0, 0, W, TOP);
        g.Dispose();
        return bmp;
    }

    static Pen RoundPen(Color color, float width)
    {
        Pen pen = new Pen(color, width);
        pen.LineJoin = LineJoin.Round;
        pen.StartCap = LineCap.Round;
        pen.EndCap = LineCap.Round;
        return pen;
    }

    // ギザギザの太陽（背景）：外へとがった光
    static void DrawBurst(Graphics g, float cx, float cy, float rx, float ry, Random rand)
    {
        int spikes = 20;
        List<PointF> pts = new List<PointF>();
        for (int i = 0; i < spikes * 2; i++)
        {
            double a = Math.PI * 2.0 * i / (spikes * 2) - Math.PI / 2.0;
            bool outer = i % 2 == 0;
            double f = outer ? 1.0 + (rand.NextDouble() - 0.5) * 0.10 : 0.74;
            pts.Add(new PointF((float)(cx + rx * f * Math.Cos(a)), (float)(cy + ry * f * Math.Sin(a))));
        }
        GraphicsPath path = new GraphicsPath();
        path.AddPolygon(pts.ToArray());

        g.DrawPath(RoundPen(Cream, 64.0f), path);
        g.DrawPath(RoundPen(Navy, 36.0f), path);

        PathGradientBrush brush = new PathGradientBrush(path);
        brush.CenterPoint = new PointF(cx, cy);
        brush.CenterColor = Color.FromArgb(255, 255, 252, 214);
        brush.SurroundColors = new Color[] { Color.FromArgb(255, 255, 188, 40) };
        g.FillPath(brush, path);

        // 光の筋：1つおきに、白っぽく重ねる
        SolidBrush ray = new SolidBrush(Color.FromArgb(52, 255, 255, 255));
        for (int i = 0; i < spikes; i += 2)
        {
            PointF tip = pts[i * 2 % pts.Count];
            PointF before = pts[(i * 2 + pts.Count - 1) % pts.Count];
            PointF after = pts[(i * 2 + 1) % pts.Count];
            g.FillPolygon(ray, new PointF[] { new PointF(cx, cy), before, tip, after });
        }
    }

    // 縁取りつきの文字1行。幅は targetWidth に合わせる
    static void DrawTextLine(Graphics g, FontFamily family, string text, float cx, float cy, float targetWidth, float rotation,
        Color top, Color mid, Color bottom, Color innerEdge)
    {
        GraphicsPath path = new GraphicsPath();
        path.AddString(text, family, (int)FontStyle.Regular, 300.0f, new PointF(0, 0), StringFormat.GenericTypographic);
        RectangleF b = path.GetBounds();
        float scale = targetWidth / b.Width;
        Matrix m = new Matrix();
        m.Translate(-(b.X + b.Width / 2.0f), -(b.Y + b.Height / 2.0f));
        m.Scale(scale, scale, MatrixOrder.Append);
        m.Rotate(rotation, MatrixOrder.Append);
        m.Translate(cx, cy, MatrixOrder.Append);
        path.Transform(m);
        RectangleF bounds = path.GetBounds();

        // 影（下にずらした濃い紺）→ 外側のクリーム色の縁 → 紺の縁 → 塗り
        GraphicsPath shadow = (GraphicsPath)path.Clone();
        Matrix move = new Matrix();
        move.Translate(0.0f, 16.0f);
        shadow.Transform(move);
        g.DrawPath(RoundPen(Color.FromArgb(255, 8, 12, 40), 40.0f), shadow);
        g.DrawPath(RoundPen(Cream, 62.0f), path);
        g.DrawPath(RoundPen(Navy, 38.0f), path);

        LinearGradientBrush fill = new LinearGradientBrush(new RectangleF(bounds.X, bounds.Y - 1, bounds.Width, bounds.Height + 2), top, bottom, 90.0f);
        ColorBlend blend = new ColorBlend(3);
        blend.Colors = new Color[] { top, mid, bottom };
        blend.Positions = new float[] { 0.0f, 0.55f, 1.0f };
        fill.InterpolationColors = blend;
        g.FillPath(fill, path);

        // 文字の中だけに、つや（上半分の白）と、内側のふち
        Region saved = g.Clip;
        g.SetClip(path, CombineMode.Replace);
        LinearGradientBrush gloss = new LinearGradientBrush(new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height * 0.5f),
            Color.FromArgb(120, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), 90.0f);
        g.FillRectangle(gloss, bounds.X, bounds.Y, bounds.Width, bounds.Height * 0.5f);
        g.DrawPath(RoundPen(Color.FromArgb(160, innerEdge.R, innerEdge.G, innerEdge.B), 12.0f), path);
        g.Clip = saved;
    }

    // 5つの角の星
    static void DrawStar(Graphics g, float cx, float cy, float r, float rotationDeg)
    {
        PointF[] pts = new PointF[10];
        for (int i = 0; i < 10; i++)
        {
            double a = (Math.PI * 2.0 * i / 10.0) - Math.PI / 2.0 + rotationDeg * Math.PI / 180.0;
            float rr = i % 2 == 0 ? r : r * 0.46f;
            pts[i] = new PointF((float)(cx + rr * Math.Cos(a)), (float)(cy + rr * Math.Sin(a)));
        }
        GraphicsPath path = new GraphicsPath();
        path.AddPolygon(pts);
        g.DrawPath(RoundPen(Cream, r * 0.30f), path);
        g.DrawPath(RoundPen(Navy, r * 0.16f), path);
        PathGradientBrush brush = new PathGradientBrush(path);
        brush.CenterPoint = new PointF(cx, cy);
        brush.CenterColor = Color.FromArgb(255, 255, 250, 150);
        brush.SurroundColors = new Color[] { Color.FromArgb(255, 255, 186, 24) };
        g.FillPath(brush, path);
    }

    // 4つの角のキラキラ（細い星）
    static void DrawSparkle(Graphics g, float cx, float cy, float r)
    {
        PointF[] pts = new PointF[8];
        for (int i = 0; i < 8; i++)
        {
            double a = (Math.PI * 2.0 * i / 8.0) - Math.PI / 2.0;
            float rr = i % 2 == 0 ? r : r * 0.24f;
            pts[i] = new PointF((float)(cx + rr * Math.Cos(a)), (float)(cy + rr * Math.Sin(a)));
        }
        GraphicsPath path = new GraphicsPath();
        path.AddPolygon(pts);
        g.DrawPath(RoundPen(Navy, r * 0.22f), path);
        g.FillPath(new SolidBrush(Color.White), path);
    }

    // 曲がった矢印（ぐるりんダイブの回る矢印に合わせる）
    static void DrawArrowArc(Graphics g, float cx, float cy, float rx, float ry, float startDeg, float sweepDeg, bool unused)
    {
        List<PointF> pts = new List<PointF>();
        int n = 40;
        for (int i = 0; i <= n; i++)
        {
            double a = (startDeg + sweepDeg * i / n) * Math.PI / 180.0;
            pts.Add(new PointF((float)(cx + rx * Math.Cos(a)), (float)(cy + ry * Math.Sin(a))));
        }
        PointF[] line = pts.ToArray();
        PointF end = line[n];
        PointF prev = line[n - 2];
        float tx = end.X - prev.X, ty = end.Y - prev.Y;
        float len = (float)Math.Sqrt(tx * tx + ty * ty);
        tx /= len; ty /= len;
        float nx = -ty, ny = tx;
        PointF tip = new PointF(end.X + tx * 70.0f, end.Y + ty * 70.0f);
        PointF left = new PointF(end.X + nx * 52.0f, end.Y + ny * 52.0f);
        PointF right = new PointF(end.X - nx * 52.0f, end.Y - ny * 52.0f);

        Color blue = Color.FromArgb(255, 70, 176, 248);
        g.DrawLines(RoundPen(Cream, 64.0f), line);
        g.DrawLines(RoundPen(Navy, 50.0f), line);
        g.DrawPolygon(RoundPen(Cream, 40.0f), new PointF[] { tip, left, right });
        g.DrawPolygon(RoundPen(Navy, 26.0f), new PointF[] { tip, left, right });
        g.DrawLines(RoundPen(blue, 30.0f), line);
        g.FillPolygon(new SolidBrush(blue), new PointF[] { tip, left, right });
        g.DrawLines(RoundPen(Color.FromArgb(150, 255, 255, 255), 9.0f), line);
    }

    // 紙吹雪：色とりどりの小さな四角
    static void DrawConfetti(Graphics g, Random rand)
    {
        Color[] palette = new Color[]
        {
            Color.FromArgb(255, 70, 200, 250), Color.FromArgb(255, 255, 110, 170), Color.FromArgb(255, 110, 220, 110),
            Color.FromArgb(255, 255, 150, 40), Color.FromArgb(255, 170, 110, 240), Color.FromArgb(255, 255, 230, 60),
        };
        for (int i = 0; i < 46; i++)
        {
            float x, y;
            // 外側のふちに多く、あとはばらまく
            if (i % 3 == 0)
            {
                x = 60 + (float)rand.NextDouble() * (W - 120);
                y = TOP + 40 + (float)rand.NextDouble() * (H - TOP - 80);
            }
            else if (i % 3 == 1)
            {
                x = (rand.Next(2) == 0 ? 60 : W - 60) + (float)(rand.NextDouble() - 0.5) * 220.0f;
                y = TOP + 50 + (float)rand.NextDouble() * (H - TOP - 100);
            }
            else
            {
                x = 60 + (float)rand.NextDouble() * (W - 120);
                y = (rand.Next(2) == 0 ? TOP + 50 : H - 50) + (float)(rand.NextDouble() - 0.5) * 110.0f;
            }
            x = Math.Max(40.0f, Math.Min(W - 40.0f, x));
            y = Math.Max(TOP + 40.0f, Math.Min(H - 40.0f, y));

            float w = 34.0f + (float)rand.NextDouble() * 22.0f;
            float h = w * 0.55f;
            float rot = (float)rand.NextDouble() * 180.0f;
            Color c = palette[rand.Next(palette.Length)];

            GraphicsState state = g.Save();
            g.TranslateTransform(x, y);
            g.RotateTransform(rot);
            g.FillRectangle(new SolidBrush(Cream), -w / 2 - 9, -h / 2 - 9, w + 18, h + 18);
            g.FillRectangle(new SolidBrush(Navy), -w / 2 - 5, -h / 2 - 5, w + 10, h + 10);
            g.FillRectangle(new SolidBrush(c), -w / 2, -h / 2, w, h);
            g.Restore(state);
        }
    }

    // ------------------------------------------------------------ 3Dモデル

    static void BuildMesh(Bitmap bmp, string objPath, bool flipX)
    {
        // 1) 不透明な部分を、4×4ピクセルごとの平均で粗い格子にする（まわりに空白の1マスを足す）
        int cell = 4;
        int gw = W / cell + 2;
        int gh = (H - TOP) / cell + 2;
        float[,] v = new float[gw, gh];
        BitmapData data = bmp.LockBits(new Rectangle(0, 0, W, H), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        byte[] raw = new byte[data.Stride * H];
        System.Runtime.InteropServices.Marshal.Copy(data.Scan0, raw, 0, raw.Length);
        for (int gy = 0; gy < gh - 2; gy++)
        {
            for (int gx = 0; gx < gw - 2; gx++)
            {
                int sum = 0;
                for (int dy = 0; dy < cell; dy++)
                {
                    for (int dx = 0; dx < cell; dx++)
                    {
                        int px = gx * cell + dx;
                        int py = TOP + gy * cell + dy;
                        sum += raw[py * data.Stride + px * 4 + 3];
                    }
                }
                v[gx + 1, gy + 1] = sum / (255.0f * cell * cell);
            }
        }
        bmp.UnlockBits(data);

        // 2) マーチングスクエアで輪郭をたどる（内側が進行方向の左：画面は y が下向き）
        Dictionary<int, PointF> points = new Dictionary<int, PointF>();
        Dictionary<int, int> next = new Dictionary<int, int>();
        for (int y = 0; y < gh - 1; y++)
        {
            for (int x = 0; x < gw - 1; x++)
            {
                float a = v[x, y], b = v[x + 1, y], c = v[x + 1, y + 1], d = v[x, y + 1];
                int idx = (a >= 0.5f ? 8 : 0) | (b >= 0.5f ? 4 : 0) | (c >= 0.5f ? 2 : 0) | (d >= 0.5f ? 1 : 0);
                if (idx == 0 || idx == 15) { continue; }

                int T = EdgeH(x, y, gw), R = EdgeV(x + 1, y, gw), B = EdgeH(x, y + 1, gw), L = EdgeV(x, y, gw);
                points[T] = new PointF(x + Lerp(a, b), y);
                points[B] = new PointF(x + Lerp(d, c), y + 1);
                points[L] = new PointF(x, y + Lerp(a, d));
                points[R] = new PointF(x + 1, y + Lerp(b, c));

                switch (idx)
                {
                    case 8: next[L] = T; break;
                    case 4: next[T] = R; break;
                    case 2: next[R] = B; break;
                    case 1: next[B] = L; break;
                    case 12: next[L] = R; break;
                    case 3: next[R] = L; break;
                    case 9: next[B] = T; break;
                    case 6: next[T] = B; break;
                    case 14: next[L] = B; break;
                    case 13: next[B] = R; break;
                    case 11: next[R] = T; break;
                    case 7: next[T] = L; break;
                    case 10:
                        if ((a + b + c + d) * 0.25f >= 0.5f) { next[R] = T; next[L] = B; }
                        else { next[L] = T; next[R] = B; }
                        break;
                    case 5:
                        if ((a + b + c + d) * 0.25f >= 0.5f) { next[T] = L; next[B] = R; }
                        else { next[T] = R; next[B] = L; }
                        break;
                }
            }
        }

        // 3) つながった輪にして、簡略化する
        HashSet<int> visited = new HashSet<int>();
        List<List<PointF>> loops = new List<List<PointF>>();
        foreach (int start in new List<int>(next.Keys))
        {
            if (visited.Contains(start)) { continue; }
            List<PointF> loop = new List<PointF>();
            int cur = start;
            bool closed = false;
            while (true)
            {
                visited.Add(cur);
                loop.Add(points[cur]);
                int nx;
                if (!next.TryGetValue(cur, out nx)) { break; }
                cur = nx;
                if (cur == start) { closed = true; break; }
                if (visited.Contains(cur)) { break; }
            }
            if (!closed || loop.Count < 8) { continue; }
            List<PointF> simple = SimplifyClosed(loop, 0.8f);
            if (simple.Count >= 4 && Perimeter(simple) > 14.0f) { loops.Add(simple); }
        }

        // 4) OBJ に書き出す
        List<float[]> positions = new List<float[]>();
        List<float[]> uvs = new List<float[]>();
        List<float[]> normals = new List<float[]>();
        StringBuilder faces = new StringBuilder();

        float sideU = 0.5f;
        float sideV = 1.0f - 16.5f / H;
        float vMax = (H - TOP) / (float)H;
        float halfHeight = (H - TOP) / (float)W * WorldWidth * 0.5f;
        float halfWidth = WorldWidth * 0.5f;
        float sx = flipX ? -1.0f : 1.0f;

        // 前（-Z を向く）と後ろ（+Z を向く）の板：四角1枚ずつ。画像の透明部分は、素材のアルファで切り抜く
        AddQuad(positions, uvs, normals, faces,
            new float[] { -halfWidth, -halfHeight, -HalfThickness }, new float[] { halfWidth, -halfHeight, -HalfThickness },
            new float[] { halfWidth, halfHeight, -HalfThickness }, new float[] { -halfWidth, halfHeight, -HalfThickness },
            new float[] { 0, 0 }, new float[] { 1, 0 }, new float[] { 1, vMax }, new float[] { 0, vMax }, new float[] { 0, 0, -1 }, sx);
        AddQuad(positions, uvs, normals, faces,
            new float[] { halfWidth, -halfHeight, HalfThickness }, new float[] { -halfWidth, -halfHeight, HalfThickness },
            new float[] { -halfWidth, halfHeight, HalfThickness }, new float[] { halfWidth, halfHeight, HalfThickness },
            new float[] { 0, 0 }, new float[] { 1, 0 }, new float[] { 1, vMax }, new float[] { 0, vMax }, new float[] { 0, 0, 1 }, sx);

        // 側面：輪郭に沿って、前の板と後ろの板をつなぐ
        int segmentCount = 0;
        foreach (List<PointF> loop in loops)
        {
            int n = loop.Count;
            float[][] world = new float[n][];
            for (int i = 0; i < n; i++)
            {
                // 格子の座標 → ピクセル（マスの中心）→ ワールド（y は上向き）
                float px = (loop[i].X - 1.0f) * cell + cell * 0.5f;
                float py = (loop[i].Y - 1.0f) * cell + cell * 0.5f;
                world[i] = new float[] { (px - W * 0.5f) / W * WorldWidth, -(py - (H - TOP) * 0.5f) / W * WorldWidth };
            }
            // 各辺の外向きの法線（進行方向の右）
            float[][] edgeNormal = new float[n][];
            for (int i = 0; i < n; i++)
            {
                float[] p0 = world[i], p1 = world[(i + 1) % n];
                float tx = p1[0] - p0[0], ty = p1[1] - p0[1];
                float len = (float)Math.Sqrt(tx * tx + ty * ty);
                if (len < 1e-9f) { len = 1.0f; }
                edgeNormal[i] = new float[] { ty / len, -tx / len };
            }
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                float[] n0 = SmoothNormal(edgeNormal[(i + n - 1) % n], edgeNormal[i]);
                float[] n1 = SmoothNormal(edgeNormal[i], edgeNormal[j]);
                AddSideQuad(positions, uvs, normals, faces, world[i], world[j], n0, n1, sideU, sideV, sx);
                segmentCount++;
            }
        }

        StringBuilder obj = new StringBuilder();
        obj.AppendLine("# ClearLogo (generated)");
        obj.AppendLine("o ClearLogo");
        CultureInfo ci = CultureInfo.InvariantCulture;
        foreach (float[] p in positions) { obj.AppendLine("v " + (p[0] * sx).ToString("0.######", ci) + " " + p[1].ToString("0.######", ci) + " " + p[2].ToString("0.######", ci)); }
        foreach (float[] t in uvs) { obj.AppendLine("vt " + t[0].ToString("0.######", ci) + " " + t[1].ToString("0.######", ci)); }
        foreach (float[] nn in normals) { obj.AppendLine("vn " + (nn[0] * sx).ToString("0.######", ci) + " " + nn[1].ToString("0.######", ci) + " " + nn[2].ToString("0.######", ci)); }
        obj.AppendLine("usemtl ClearLogo");
        obj.Append(faces.ToString());
        File.WriteAllText(objPath, obj.ToString(), new UTF8Encoding(false));
        Console.WriteLine("loops=" + loops.Count + " segments=" + segmentCount + " verts=" + positions.Count);
    }

    static float[] SmoothNormal(float[] a, float[] b)
    {
        // 緩やかな曲がりは、なめらかにつなぐ。急な角は、そのまま角にする
        float dot = a[0] * b[0] + a[1] * b[1];
        if (dot < 0.64f) { return b; }
        float x = a[0] + b[0], y = a[1] + b[1];
        float len = (float)Math.Sqrt(x * x + y * y);
        return len < 1e-9f ? b : new float[] { x / len, y / len };
    }

    // 側面の四角。法線の向きに合わせて、三角形の並びを決める（OBJ は反時計回りが表）
    static void AddSideQuad(List<float[]> pos, List<float[]> uv, List<float[]> nor, StringBuilder faces,
        float[] p0, float[] p1, float[] n0, float[] n1, float u, float v, float sx)
    {
        float[] a = new float[] { p0[0], p0[1], -HalfThickness };
        float[] b = new float[] { p1[0], p1[1], -HalfThickness };
        float[] c = new float[] { p1[0], p1[1], HalfThickness };
        float[] d = new float[] { p0[0], p0[1], HalfThickness };
        int start = pos.Count;
        pos.Add(a); pos.Add(b); pos.Add(c); pos.Add(d);
        for (int i = 0; i < 4; i++) { uv.Add(new float[] { u, v }); }
        nor.Add(new float[] { n0[0], n0[1], 0 }); nor.Add(new float[] { n1[0], n1[1], 0 });
        nor.Add(new float[] { n1[0], n1[1], 0 }); nor.Add(new float[] { n0[0], n0[1], 0 });

        // ファイルの座標（x を sx 倍したもの）で、反時計回りに外を向くようにする
        float[] fa = new float[] { a[0] * sx, a[1], a[2] }, fb = new float[] { b[0] * sx, b[1], b[2] }, fc = new float[] { c[0] * sx, c[1], c[2] };
        float[] fn = new float[] { (n0[0] + n1[0]) * 0.5f * sx, (n0[1] + n1[1]) * 0.5f, 0 };
        bool ccw = Dot(Cross(Sub(fb, fa), Sub(fc, fa)), fn) > 0.0f;
        int i0 = start + 1, i1 = start + 2, i2 = start + 3, i3 = start + 4;   // OBJ は1始まり
        if (ccw) { AddFace(faces, i0, i1, i2); AddFace(faces, i0, i2, i3); }
        else { AddFace(faces, i0, i2, i1); AddFace(faces, i0, i3, i2); }
    }

    // 前後の板の四角
    static void AddQuad(List<float[]> pos, List<float[]> uv, List<float[]> nor, StringBuilder faces,
        float[] a, float[] b, float[] c, float[] d, float[] ua, float[] ub, float[] uc, float[] ud, float[] n, float sx)
    {
        int start = pos.Count;
        pos.Add(a); pos.Add(b); pos.Add(c); pos.Add(d);
        uv.Add(ua); uv.Add(ub); uv.Add(uc); uv.Add(ud);
        for (int i = 0; i < 4; i++) { nor.Add(n); }

        float[] fa = new float[] { a[0] * sx, a[1], a[2] }, fb = new float[] { b[0] * sx, b[1], b[2] }, fc = new float[] { c[0] * sx, c[1], c[2] };
        float[] fn = new float[] { n[0] * sx, n[1], n[2] };
        bool ccw = Dot(Cross(Sub(fb, fa), Sub(fc, fa)), fn) > 0.0f;
        int i0 = start + 1, i1 = start + 2, i2 = start + 3, i3 = start + 4;
        if (ccw) { AddFace(faces, i0, i1, i2); AddFace(faces, i0, i2, i3); }
        else { AddFace(faces, i0, i2, i1); AddFace(faces, i0, i3, i2); }
    }

    static void AddFace(StringBuilder sb, int a, int b, int c)
    {
        sb.Append("f ").Append(a).Append('/').Append(a).Append('/').Append(a).Append(' ')
          .Append(b).Append('/').Append(b).Append('/').Append(b).Append(' ')
          .Append(c).Append('/').Append(c).Append('/').Append(c).AppendLine();
    }

    static float[] Sub(float[] a, float[] b) { return new float[] { a[0] - b[0], a[1] - b[1], a[2] - b[2] }; }
    static float[] Cross(float[] a, float[] b) { return new float[] { a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0] }; }
    static float Dot(float[] a, float[] b) { return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]; }

    static int EdgeH(int x, int y, int gw) { return 2 * (y * gw + x); }
    static int EdgeV(int x, int y, int gw) { return 2 * (y * gw + x) + 1; }
    static float Lerp(float a, float b) { float d = b - a; return Math.Abs(d) < 1e-6f ? 0.5f : Math.Max(0.0f, Math.Min(1.0f, (0.5f - a) / d)); }

    static float Perimeter(List<PointF> p)
    {
        float s = 0;
        for (int i = 0; i < p.Count; i++)
        {
            PointF a = p[i], b = p[(i + 1) % p.Count];
            s += (float)Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
        }
        return s;
    }

    // 閉じた輪を、許す誤差の範囲で、点を減らす（Douglas-Peucker）
    static List<PointF> SimplifyClosed(List<PointF> loop, float tol)
    {
        int n = loop.Count;
        int far = 0;
        float best = -1;
        for (int i = 1; i < n; i++)
        {
            float dx = loop[i].X - loop[0].X, dy = loop[i].Y - loop[0].Y;
            float d = dx * dx + dy * dy;
            if (d > best) { best = d; far = i; }
        }
        bool[] keep = new bool[n];
        keep[0] = true; keep[far] = true;
        Rdp(loop, 0, far, tol, keep);
        Rdp(loop, far, n, tol, keep);   // n は 0 番目に戻る
        List<PointF> result = new List<PointF>();
        for (int i = 0; i < n; i++) { if (keep[i]) { result.Add(loop[i]); } }
        return result;
    }

    static void Rdp(List<PointF> pts, int first, int last, float tol, bool[] keep)
    {
        int n = pts.Count;
        PointF a = pts[first % n], b = pts[last % n];
        float maxD = -1;
        int index = -1;
        for (int i = first + 1; i < last; i++)
        {
            float d = PointLineDistance(pts[i % n], a, b);
            if (d > maxD) { maxD = d; index = i; }
        }
        if (index >= 0 && maxD > tol)
        {
            keep[index % n] = true;
            Rdp(pts, first, index, tol, keep);
            Rdp(pts, index, last, tol, keep);
        }
    }

    static float PointLineDistance(PointF p, PointF a, PointF b)
    {
        float dx = b.X - a.X, dy = b.Y - a.Y;
        float len2 = dx * dx + dy * dy;
        if (len2 < 1e-9f) { return (float)Math.Sqrt((p.X - a.X) * (p.X - a.X) + (p.Y - a.Y) * (p.Y - a.Y)); }
        return Math.Abs(dy * p.X - dx * p.Y + b.X * a.Y - b.Y * a.X) / (float)Math.Sqrt(len2);
    }
}
