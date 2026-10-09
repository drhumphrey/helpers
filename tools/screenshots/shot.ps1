# Captures one of this app's own windows, by title, through PrintWindow: only that window's
# pixels, even when another window is in front. Never captures anything else on the screen.
param(
    [Parameter(Mandatory = $true)] [string] $Title,
    [Parameter(Mandatory = $true)] [string] $Out
)

Add-Type -AssemblyName System.Drawing
Add-Type -Namespace Shot -Name Native -MemberDefinition @'
[System.Runtime.InteropServices.DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(System.IntPtr context);
[System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)] public static extern System.IntPtr FindWindow(System.IntPtr className, string windowName);
[System.Runtime.InteropServices.DllImport("user32.dll")] public static extern bool GetWindowRect(System.IntPtr hWnd, out RECT rect);
[System.Runtime.InteropServices.DllImport("user32.dll")] public static extern bool IsWindowVisible(System.IntPtr hWnd);
[System.Runtime.InteropServices.DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(System.IntPtr hWnd, out uint pid);
[System.Runtime.InteropServices.DllImport("user32.dll")] public static extern bool PrintWindow(System.IntPtr hWnd, System.IntPtr hdc, uint flags);
public struct RECT { public int Left, Top, Right, Bottom; }
'@

[void][Shot.Native]::SetProcessDpiAwarenessContext([System.IntPtr]::new(-4))

$hwnd = [Shot.Native]::FindWindow([System.IntPtr]::Zero, $Title)
if ($hwnd -eq [System.IntPtr]::Zero) {
    Write-Output ("no window titled '" + $Title + "'")
    exit 1
}

if (-not [Shot.Native]::IsWindowVisible($hwnd)) {
    Write-Output ("window '" + $Title + "' exists but is hidden")
    exit 2
}

$pid0 = 0
[void][Shot.Native]::GetWindowThreadProcessId($hwnd, [ref]$pid0)
$proc = Get-Process -Id $pid0 -ErrorAction SilentlyContinue
if ($proc.ProcessName -ne 'Helpers.App') {
    Write-Output ("window '" + $Title + "' belongs to " + $proc.ProcessName + "; not capturing")
    exit 1
}

$r = New-Object Shot.Native+RECT
[void][Shot.Native]::GetWindowRect($hwnd, [ref]$r)
$w = $r.Right - $r.Left
$h = $r.Bottom - $r.Top
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.Clear([System.Drawing.Color]::FromArgb(255, 40, 44, 52))
$hdc = $g.GetHdc()
$ok = [Shot.Native]::PrintWindow($hwnd, $hdc, 2)
$g.ReleaseHdc($hdc)
$bmp.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose()
$bmp.Dispose()
Write-Output ("saved " + $Out + " " + $w + "x" + $h + " printed=" + $ok)
