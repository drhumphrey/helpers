/*
    Read Aloud
    Select text in any app (Word, Outlook, Teams, Chrome, PDFs...) and press Ctrl+Alt+Space.
    Press it again to stop.

    Needs AutoHotkey v2 (free from autohotkey.com). Double-click this file to run it.
    Right-click the tray icon to change the voice or speed, or to start it with Windows.

    How it works: it sends Ctrl+C to grab whatever's selected, puts your clipboard back
    exactly as it was, then speaks the text with the Windows voice you've picked.

    Notes
    - It can't read from apps running as administrator unless this script runs as admin too.
    - Any SAPI 5 voice installed on the PC shows up in the Voice menu, including Microsoft's
      natural voices if you add NaturalVoiceSAPIAdapter. No changes needed here.
    - To make a standalone .exe, right-click this file and choose Compile Script.
*/

#Requires AutoHotkey v2.0
#SingleInstance Force

; ---------- Settings ----------
ReadKey     := "^!Space"         ; ^ = Ctrl, ! = Alt, + = Shift, # = Win
ReadKeyName := "Ctrl+Alt+Space"  ; how the shortcut is shown in the tray menu
SayLink     := true              ; say "link" rather than reading web addresses out in full
; ------------------------------

Speeds       := [["Slow", -2], ["Normal", 0], ["A bit faster", 2], ["Fast", 4], ["Very fast", 6]]
IniFile      := A_ScriptDir "\ReadAloud.ini"
StartupLink  := A_Startup "\Read Aloud.lnk"
Voice        := ComObject("SAPI.SpVoice")
CurrentVoice := ""

; Use the saved voice, otherwise a UK English one (a natural, offline one if installed)
if !UseVoice(IniRead(IniFile, "Settings", "Voice", "")) && !UseVoice(DefaultVoiceName())
    CurrentVoice := Voice.Voice.GetDescription()
try Voice.Rate := Integer(IniRead(IniFile, "Settings", "Rate", "0"))

BuildTrayMenu()
Hotkey ReadKey, ReadSelection

if (IniRead(IniFile, "Settings", "Welcomed", "0") != "1") {
    TrayTip "Select text anywhere and press " ReadKeyName ".`nRight-click the tray icon for voices.", "Read Aloud is running", "Iconi Mute"
    Save("Welcomed", 1)
}


; ---------- Reading ----------

ReadSelection(*) {
    if (Voice.Status.RunningState = 2) {      ; already talking, so this press means stop
        StopSpeaking()
    } else {
        text := GetSelectedText()
        if (text = "") {
            ShowTip("Nothing selected")
        } else {
            try {
                Voice.Speak(Tidy(text), 1 | 2 | 16)   ; async, cut off anything playing, plain text
            } catch {
                ShowTip("That voice failed. Pick another from the tray menu.")
            }
        }
    }
    KeyWait RegExReplace(ReadKey, "[\^!+#*~$<>]")    ; ignore key repeat if the shortcut is held
}

StopSpeaking(*) => Voice.Speak("", 1 | 2)

GetSelectedText() {
    saved := ClipboardAll()
    A_Clipboard := ""
    ; Terminals treat Ctrl+C as "cancel" when nothing's selected, so use Ctrl+Insert there
    if WinActive("ahk_exe WindowsTerminal.exe") || WinActive("ahk_class ConsoleWindowClass")
        Send "^{Insert}"
    else
        Send "^c"
    text := ClipWait(1) ? A_Clipboard : ""      ; Teams and Outlook can take a moment
    A_Clipboard := saved                        ; put the clipboard back as it was
    return Trim(text)
}

Tidy(text) {
    if SayLink
        text := RegExReplace(text, "i)\b(?:https?://|www\.)\S+", "link")
    text := RegExReplace(text, "m)^\h*[\x{2022}\x{25E6}\x{25AA}\x{00B7}\x{F0B7}*-]\h+")  ; bullet points
    text := RegExReplace(text, "\h*\R\s*", "`n")                    ; collapse blank lines
    text := RegExReplace(text, "(?<![.!?:;,])`n", ". ")              ; pause at lines with no punctuation
    return StrReplace(text, "`n", " ")
}


; ---------- Voices ----------

DefaultVoiceName() {
    best := "", bestScore := -1
    uk := Voice.GetVoices("Language=809")      ; 809 = English (UK)
    loop uk.Count {
        name := uk.Item(A_Index - 1).GetDescription()
        score := (InStr(name, "Natural") ? 2 : 0) + (InStr(name, "Online") ? 0 : 1)
        if (score > bestScore)
            best := name, bestScore := score
    }
    return best
}

UseVoice(name) {
    global CurrentVoice
    if (name = "")
        return false
    voices := Voice.GetVoices()
    loop voices.Count {
        v := voices.Item(A_Index - 1)
        if (v.GetDescription() = name) {
            try {
                Voice.Voice := v
            } catch {
                return false
            }
            CurrentVoice := name
            return true
        }
    }
    return false
}


; ---------- Tray menu ----------

BuildTrayMenu() {
    tray := A_TrayMenu
    tray.Delete()                                  ; drop AutoHotkey's default items
    tray.Add("Shortcut: " ReadKeyName, (*) => 0)
    tray.Disable("Shortcut: " ReadKeyName)
    tray.Add("Stop speaking", StopSpeaking)
    tray.Add()
    tray.Add("Voice", VoiceMenu())
    tray.Add("Speed", SpeedMenu())
    tray.Add()
    tray.Add("Start with Windows", ToggleStartup)
    if FileExist(StartupLink)
        tray.Check("Start with Windows")
    tray.Add("Exit", (*) => ExitApp())
    A_IconTip := "Read Aloud (" ReadKeyName ")"
    try TraySetIcon(A_WinDir "\System32\SndVol.exe")
}

VoiceMenu() {
    m := Menu()
    voices := Voice.GetVoices()
    loop voices.Count {
        name := voices.Item(A_Index - 1).GetDescription()
        m.Add(name, PickVoice)
        if (name = CurrentVoice)
            m.Check(name)
    }
    return m
}

PickVoice(name, pos, m) {
    try m.Uncheck(CurrentVoice)
    if !UseVoice(name) {
        ShowTip("Couldn't load that voice")
        try m.Check(CurrentVoice)
        return
    }
    m.Check(name)
    Save("Voice", name)
    try Voice.Speak("Hello, this is how I sound.", 1 | 2)
}

SpeedMenu() {
    m := Menu()
    for s in Speeds {
        m.Add(s[1], SetSpeed.Bind(s[2]))
        if (Voice.Rate = s[2])
            m.Check(s[1])
    }
    return m
}

SetSpeed(rate, name, pos, m) {
    for s in Speeds
        m.Uncheck(s[1])
    m.Check(name)
    Voice.Rate := rate
    Save("Rate", rate)
}

ToggleStartup(name, pos, m) {
    if FileExist(StartupLink) {
        FileDelete StartupLink
        m.Uncheck(name)
    } else {
        FileCreateShortcut A_ScriptFullPath, StartupLink, A_ScriptDir
        m.Check(name)
    }
}


; ---------- Helpers ----------

Save(key, value) {
    try IniWrite value, IniFile, "Settings", key
}

ShowTip(msg) {
    ToolTip msg
    SetTimer () => ToolTip(), -1500
}
