using System.Runtime.Versioning;

// Everything in this project talks to Win32 or COM. The attribute makes the
// compiler warn if any of it is called from code that could run on macOS.
[assembly: SupportedOSPlatform("windows")]
