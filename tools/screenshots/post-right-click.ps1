# Posts a right-click to one of this app's own windows (a message to that window only; the mouse
# is not moved and no other program sees anything), then lists any new visible windows of the
# app, which is where a flyout would appear, and captures the first one.
#
#   powershell -NoProfile -File tools\screenshots\post-right-click.ps1 -Title Compose -X 795 -Y 157 -Out menu.png
#
# X and Y are in the window's client area, in physical pixels: at 150% scaling, multiply the
# position you see in the design by 1.5. The app must already be running.
param(
    [string] $Title = 'Compose',
    [int] $X = 795,
    [int] $Y = 157,
    [string] $Out
)

Add-Type -AssemblyName System.Drawing
Add-Type -Namespace Click -Name Native -MemberDefinition @'
[System.Runtime.InteropServices.DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(System.IntPtr context);
[System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)] public static extern System.IntPtr FindWindow(System.IntPtr className, string windowName);
[System.Runtime.InteropServices.DllImport("user32.dll")] public static extern bool PostMessage(System.IntPtr hWnd, uint msg, System.IntPtr wParam, System.IntPtr lParam);
[System.Runtime.InteropServices.DllImport("user32.dll")] public static extern System.IntPtr GetTopWindow(System.IntPtr hWnd);
[System.Runtime.InteropServices.DllImport("user32.dll")] public static extern System.IntPtr GetWindow(System.IntPtr hWnd, uint cmd);
[System.Runtime.InteropServices.DllImport("user32.dll")] public static extern bool IsWindowVisible(System.IntPtr hWnd);
[System.Runtime.InteropServices.DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(System.IntPtr hWnd, out uint pid);
[System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)] public static extern int GetClassName(System.IntPtr hWnd, System.Text.StringBuilder name, int max);
[System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)] public static extern int GetWindowText(System.IntPtr hWnd, System.Text.StringBuilder text, int max);
[System.Runtime.InteropServices.DllImport("user32.dll")] public static extern bool GetWindowRect(System.IntPtr hWnd, out RECT rect);
[System.Runtime.InteropServices.DllImport("user32.dll")] public static extern bool PrintWindow(System.IntPtr hWnd, System.IntPtr hdc, uint flags);
public struct RECT { public int Left, Top, Right, Bottom; }
'@

[void][Click.Native]::SetProcessDpiAwarenessContext([System.IntPtr]::new(-4))

$app = Get-Process Helpers.App -ErrorAction SilentlyContinue
if (-not $app) { Write-Output 'app not running'; exit 1 }
$hwnd = [Click.Native]::FindWindow([System.IntPtr]::Zero, $Title)
if ($hwnd -eq [System.IntPtr]::Zero) { Write-Output 'no window'; exit 1 }

function Get-OurWindows {
    $list = @()
    $h = [Click.Native]::GetTopWindow([System.IntPtr]::Zero)
    while ($h -ne [System.IntPtr]::Zero) {
        $pid0 = 0
        [void][Click.Native]::GetWindowThreadProcessId($h, [ref]$pid0)
        if ($pid0 -eq $app.Id -and [Click.Native]::IsWindowVisible($h)) {
            $cls = New-Object System.Text.StringBuilder 256
            [void][Click.Native]::GetClassName($h, $cls, 256)
            $txt = New-Object System.Text.StringBuilder 256
            [void][Click.Native]::GetWindowText($h, $txt, 256)
            $r = New-Object Click.Native+RECT
            [void][Click.Native]::GetWindowRect($h, [ref]$r)
            $list += [pscustomobject]@{ H = $h; Class = $cls.ToString(); Text = $txt.ToString(); Rect = ('{0},{1} {2}x{3}' -f $r.Left, $r.Top, ($r.Right - $r.Left), ($r.Bottom - $r.Top)); W = ($r.Right - $r.Left); Hgt = ($r.Bottom - $r.Top); L = $r.Left; T = $r.Top }
        }
        $h = [Click.Native]::GetWindow($h, 2)  # GW_HWNDNEXT
    }
    return $list
}

$before = Get-OurWindows
$lParam = [System.IntPtr]::new(($Y -shl 16) -bor ($X -band 0xFFFF))
[void][Click.Native]::PostMessage($hwnd, 0x0204, [System.IntPtr]::new(2), $lParam)   # WM_RBUTTONDOWN, MK_RBUTTON
Start-Sleep -Milliseconds 80
[void][Click.Native]::PostMessage($hwnd, 0x0205, [System.IntPtr]::Zero, $lParam)     # WM_RBUTTONUP
Start-Sleep -Milliseconds 700
$after = Get-OurWindows

Write-Output 'windows before:'
$before | ForEach-Object { Write-Output ('  ' + $_.Class + ' [' + $_.Text + '] ' + $_.Rect) }
Write-Output 'windows after:'
$after | ForEach-Object { Write-Output ('  ' + $_.Class + ' [' + $_.Text + '] ' + $_.Rect) }

$new = $after | Where-Object { $h = $_.H; -not ($before | Where-Object { $_.H -eq $h }) }
if ($new -and $Out) {
    $n = $new | Select-Object -First 1
    $bmp = New-Object System.Drawing.Bitmap $n.W, $n.Hgt
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $hdc = $g.GetHdc()
    $ok = [Click.Native]::PrintWindow($n.H, $hdc, 2)
    $g.ReleaseHdc($hdc)
    $bmp.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
    Write-Output ('captured new window to ' + $Out + ' printed=' + $ok)
}

# Close the menu again with a left-click posted to an empty part of the editor (light dismiss).
$dismiss = [System.IntPtr]::new((500 -shl 16) -bor 540)
[void][Click.Native]::PostMessage($hwnd, 0x0201, [System.IntPtr]::new(1), $dismiss)   # WM_LBUTTONDOWN, MK_LBUTTON
Start-Sleep -Milliseconds 60
[void][Click.Native]::PostMessage($hwnd, 0x0202, [System.IntPtr]::Zero, $dismiss)     # WM_LBUTTONUP
Start-Sleep -Milliseconds 400
$left = (Get-OurWindows | Where-Object { $_.Text -eq '' }).Count
Write-Output ('menu windows still open after dismiss: ' + $left)
