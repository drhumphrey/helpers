using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Helpers.Windows.Spelling;

// The Windows Spell Checking API (spellcheck.h). Method order must match the
// native vtables exactly; the order here is the order in the header.

[ComImport]
[Guid("8E018A9D-2415-4677-BF08-794EA61F94BB")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ISpellCheckerFactory
{
    IEnumString SupportedLanguages();

    [return: MarshalAs(UnmanagedType.Bool)]
    bool IsSupported([MarshalAs(UnmanagedType.LPWStr)] string languageTag);

    ISpellCheckerCom CreateSpellChecker([MarshalAs(UnmanagedType.LPWStr)] string languageTag);
}

[ComImport]
[Guid("B6FD0B71-E2BC-4653-8D05-F197E412770B")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ISpellCheckerCom
{
    nint GetLanguageTag();

    IEnumSpellingError Check([MarshalAs(UnmanagedType.LPWStr)] string text);

    IEnumString Suggest([MarshalAs(UnmanagedType.LPWStr)] string word);

    void Add([MarshalAs(UnmanagedType.LPWStr)] string word);

    void Ignore([MarshalAs(UnmanagedType.LPWStr)] string word);

    void AutoCorrect([MarshalAs(UnmanagedType.LPWStr)] string from, [MarshalAs(UnmanagedType.LPWStr)] string to);

    byte GetOptionValue([MarshalAs(UnmanagedType.LPWStr)] string optionId);

    IEnumString GetOptionIds();

    nint GetId();

    nint GetLocalizedName();

    uint AddSpellCheckerChanged(nint handler);

    void RemoveSpellCheckerChanged(uint cookie);

    nint GetOptionDescription([MarshalAs(UnmanagedType.LPWStr)] string optionId);

    IEnumSpellingError ComprehensiveCheck([MarshalAs(UnmanagedType.LPWStr)] string text);
}

[ComImport]
[Guid("803E3BD4-2828-4410-8290-418D1D73C762")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IEnumSpellingError
{
    /// <summary>Returns S_OK with an error, or S_FALSE at the end.</summary>
    [PreserveSig]
    int Next(out ISpellingError error);
}

[ComImport]
[Guid("B7C82D61-FBE8-4B47-9B27-6C0D2E0DE0A3")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ISpellingError
{
    uint GetStartIndex();

    uint GetLength();

    CorrectiveAction GetCorrectiveAction();

    nint GetReplacement();
}

internal enum CorrectiveAction
{
    None = 0,
    GetSuggestions = 1,
    Replace = 2,
    Delete = 3,
}

[ComImport]
[Guid("7AB36653-1796-484B-BDFA-E74F1DB7C1DC")]
internal class SpellCheckerFactoryClass
{
}
