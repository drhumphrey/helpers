using Helpers.Core.Words;

namespace Helpers.Tests.Words;

/// <summary>A tiny WordNet database in the real format, written to a temp folder.</summary>
public class WordNetTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "HelpersTests", Guid.NewGuid().ToString("N"));

    public WordNetTests()
    {
        Directory.CreateDirectory(_folder);

        // Offsets in WordNet are byte positions into the data file, so the second line's offset is the first line's length plus its newline.
        const string first = "00000000 03 n 02 parcel 0 package 0 001 @ 00000000 n 0000 | a wrapped container; \"the parcel arrived this morning\"";
        var second = first.Length + 1;
        var line1 = first.Replace("@ 00000000", $"@ {second:D8}");
        var line2 = $"{second:D8} 03 n 01 container 0 000 | any object that can be used to hold things";
        var dataNoun = line1 + "\n" + line2 + "\n";
        var indexNoun =
            "  1 This software and database is being provided to you, the LICENSEE...\n" +
            "parcel n 1 1 @ 1 0 00000000\n" +
            "package n 1 1 @ 1 0 00000000\n" +
            $"container n 1 0 1 0 {second:D8}\n";
        File.WriteAllText(Path.Combine(_folder, "data.noun"), dataNoun);
        File.WriteAllText(Path.Combine(_folder, "index.noun"), indexNoun);
        File.WriteAllText(Path.Combine(_folder, "noun.exc"), "parcels parcel\n");
        foreach (var suffix in new[] { "verb", "adj", "adv" })
        {
            File.WriteAllText(Path.Combine(_folder, "index." + suffix), string.Empty);
            File.WriteAllText(Path.Combine(_folder, "data." + suffix), string.Empty);
        }
    }

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public void FindsMeaningExamplesAndSynonyms()
    {
        var wordNet = new WordNet(_folder);

        var meanings = wordNet.Lookup("Parcel");

        Assert.Single(meanings);
        Assert.Equal(PartOfSpeech.Noun, meanings[0].Part);
        Assert.Equal("a wrapped container", meanings[0].Definition);
        Assert.Equal("the parcel arrived this morning", meanings[0].Examples[0]);
        Assert.Equal(["package"], meanings[0].Synonyms);
    }

    [Theory]
    [InlineData("parcels")]
    [InlineData("containers")]
    public void TriesTheBaseFormOfPluralsAndTenses(string word)
    {
        Assert.NotEmpty(new WordNet(_folder).Lookup(word));
    }

    [Fact]
    public void UnknownWordsGiveNothing()
    {
        Assert.Empty(new WordNet(_folder).Lookup("zzzz"));
    }

    [Fact]
    public void MissingDatabaseIsNotAvailable()
    {
        var wordNet = new WordNet(Path.Combine(_folder, "missing"));

        Assert.False(wordNet.IsAvailable);
        Assert.Empty(wordNet.Lookup("parcel"));
    }
}
