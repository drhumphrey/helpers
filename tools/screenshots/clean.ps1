# Makes the near-black margin around a captured window transparent, by flood fill from the edges,
# so screenshots sit nicely on light and dark backgrounds. Optionally scales the result down.
param(
    [Parameter(Mandatory = $true)] [string] $In,
    [Parameter(Mandatory = $true)] [string] $Out,
    [int] $MaxWidth = 0,
    [int] $Threshold = 24
)

Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

public static class EdgeClean
{
    public static Bitmap Run(Bitmap source, int threshold)
    {
        var bmp = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp)) { g.DrawImage(source, 0, 0, source.Width, source.Height); }

        var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
        var data = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        var stride = data.Stride;
        var bytes = new byte[stride * bmp.Height];
        Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);

        int w = bmp.Width, h = bmp.Height;
        var seen = new bool[w * h];
        var queue = new Queue<int>();
        Action<int, int> push = (x, y) =>
        {
            if (x < 0 || y < 0 || x >= w || y >= h) return;
            int i = y * w + x;
            if (seen[i]) return;
            int o = y * stride + x * 4;
            int b = bytes[o], gg = bytes[o + 1], r = bytes[o + 2];
            if (r > threshold || gg > threshold || b > threshold) return;
            seen[i] = true;
            queue.Enqueue(i);
        };

        for (int x = 0; x < w; x++) { push(x, 0); push(x, h - 1); }
        for (int y = 0; y < h; y++) { push(0, y); push(w - 1, y); }

        while (queue.Count > 0)
        {
            int i = queue.Dequeue();
            int x = i % w, y = i / w;
            int o = y * stride + x * 4;
            bytes[o + 3] = 0;
            push(x + 1, y); push(x - 1, y); push(x, y + 1); push(x, y - 1);
        }

        Marshal.Copy(bytes, 0, data.Scan0, bytes.Length);
        bmp.UnlockBits(data);
        return bmp;
    }
}
'@

$src = [System.Drawing.Bitmap]::FromFile($In)
$clean = [EdgeClean]::Run($src, $Threshold)
$src.Dispose()

if ($MaxWidth -gt 0 -and $clean.Width -gt $MaxWidth) {
    $scale = $MaxWidth / $clean.Width
    $w = [int]($clean.Width * $scale)
    $h = [int]($clean.Height * $scale)
    $small = New-Object System.Drawing.Bitmap $w, $h, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($small)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
    $g.DrawImage($clean, 0, 0, $w, $h)
    $g.Dispose()
    $clean.Dispose()
    $clean = $small
}

$clean.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
Write-Output ("cleaned " + $Out + " " + $clean.Width + "x" + $clean.Height)
$clean.Dispose()
