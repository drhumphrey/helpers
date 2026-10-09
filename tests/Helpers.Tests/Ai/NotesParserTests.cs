using Helpers.Core.Ai;

namespace Helpers.Tests.Ai;

public class NotesParserTests
{
    // One of Dave's own messages, used with his permission as test material.
    private const string Draft =
        "Ok all makes sense, on the text help which hopefully will include grammar help, maybe the read icon that pops up when you select, " +
        "maybe that should have a copy/(paste that changes to paste if something in clipboard)/read, does that make sense, so i'd select copy " +
        "then paste in our reader then work on it checking spelling and grammar, and maybe ai logic translator, then copy and paste back to source, and then can always read.";

    [Fact]
    public void ParsesNotesAndPinsThemToTheDraft()
    {
        var output =
            """
            Here are my notes:
            ```json
            [
              {"span": "maybe the read icon that pops up when you select", "kind": "missing", "note": "Which icon is this? The reader hasn't met it yet.", "fix": null},
              {"span": "paste in our reader", "kind": "unclear", "note": "Do you mean the Compose window?", "fix": "paste in Compose"},
              {"span": "ai logic translator", "kind": "logic", "note": "What would this do that the grammar check doesn't?"}
            ]
            ```
            """;

        var notes = NotesParser.Parse(output, Draft, AssistantAction.CheckMyThinking);

        Assert.Equal(3, notes.Count);
        Assert.All(notes, n => Assert.True(n.IsAnchored));
        Assert.Equal(NoteKind.Missing, notes[0].Kind);
        Assert.Equal(Draft.IndexOf("maybe the read icon", StringComparison.Ordinal), notes[0].Start);
        Assert.Equal("paste in Compose", notes[1].Fix);
        Assert.True(notes[1].HasFix);
        Assert.False(notes[2].HasFix);
        Assert.Equal("ai logic translator", Draft.Substring(notes[2].Start, notes[2].Length));
    }

    [Fact]
    public void TidyNotesGetAGrammarKindAndAChangeToNote()
    {
        var output = """[{"span":"so i'd select","fix":"so I'd select"},{"span":"then can always read","fix":"then I can always read"}]""";

        var notes = NotesParser.Parse(output, Draft, AssistantAction.Tidy);

        Assert.Equal(2, notes.Count);
        Assert.All(notes, n => Assert.Equal(NoteKind.Grammar, n.Kind));
        Assert.Equal("Change to “so I'd select”", notes[0].Note);
        Assert.True(notes[1].Start > notes[0].Start);
    }

    [Fact]
    public void ASpanThatCannotBeFoundIsKeptUnanchored()
    {
        var output = """[{"span":"words that are not in the draft","kind":"logic","note":"Still worth showing."}]""";

        var notes = NotesParser.Parse(output, Draft, AssistantAction.CheckMyThinking);

        Assert.Single(notes);
        Assert.False(notes[0].IsAnchored);
        Assert.Equal("Still worth showing.", notes[0].Note);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("The draft is clear.\n[]")]
    [InlineData("")]
    [InlineData("I could not find any problems.")]
    [InlineData("[{broken json")]
    public void NothingUsableGivesNoNotesAndNoCrash(string output)
    {
        Assert.Empty(NotesParser.Parse(output, Draft, AssistantAction.CheckMyThinking));
    }

    [Fact]
    public void LooseMatchingCopesWithQuotesAndSpacing()
    {
        var draft = "He said “it's fine” and  left.";
        var output = """[{"span":"said \"it's fine\" and left","kind":"unclear","note":"Who is he?"}]""";

        var notes = NotesParser.Parse(output, draft, AssistantAction.CheckMyThinking);

        Assert.Single(notes);
        Assert.True(notes[0].IsAnchored);
        Assert.Equal("said “it's fine” and  left", draft.Substring(notes[0].Start, notes[0].Length));
    }

    [Fact]
    public void ASpanTheModelMisCopiedByALetterIsStillPinned()
    {
        var draft = "writers oftern skip vital information when they are in a hurry";
        var output = """[{"span":"ofern skip vital information","fix":"often skip vital information"}]""";

        var notes = NotesParser.Parse(output, draft, AssistantAction.Tidy);

        Assert.Single(notes);
        Assert.True(notes[0].IsAnchored);
        Assert.Equal("oftern skip vital information", draft.Substring(notes[0].Start, notes[0].Length));
    }

    [Fact]
    public void FuzzyMatchingDoesNotInventAMatch()
    {
        var draft = "a completely different sentence about the weather today";
        var output = """[{"span":"ofern skip vital information","fix":"often skip vital information"}]""";

        var notes = NotesParser.Parse(output, draft, AssistantAction.Tidy);

        Assert.Single(notes);
        Assert.False(notes[0].IsAnchored);
    }

    [Fact]
    public void LaterNotesAreSearchedForAfterEarlierOnes()
    {
        var draft = "copy it, then copy it again";
        var output = """[{"span":"copy it","kind":"unclear","note":"first"},{"span":"copy it","kind":"unclear","note":"second"}]""";

        var notes = NotesParser.Parse(output, draft, AssistantAction.CheckMyThinking);

        Assert.Equal(0, notes[0].Start);
        Assert.Equal(14, notes[1].Start);
    }
}
