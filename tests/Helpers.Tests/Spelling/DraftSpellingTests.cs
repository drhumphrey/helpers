using Helpers.Core.Spelling;

namespace Helpers.Tests.Spelling;

public class DraftSpellingTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "HelpersTests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    public void DictionaryWordsAreNotErrors()
    {
        var checker = new FakeChecker("slnx", "Kokoro");
        var dictionary = new UserDictionary(Path.Combine(_folder, "dictionary.txt"));
        dictionary.Add("Kokoro");
        var spelling = new DraftSpelling(checker, dictionary);

        var errors = spelling.Check("Open the slnx file for Kokoro.", 0);

        Assert.Single(errors);
        Assert.Equal("slnx", DraftSpelling.WordAt("Open the slnx file for Kokoro.", errors[0]));
        Assert.Contains("Kokoro", checker.Ignored);
    }

    [Fact]
    public void AddToDictionaryWritesTheFileAndIgnoresTheWord()
    {
        var checker = new FakeChecker("slnx");
        var path = Path.Combine(_folder, "dictionary.txt");
        var spelling = new DraftSpelling(checker, new UserDictionary(path));

        spelling.AddToDictionary("slnx");

        Assert.Equal(["slnx"], File.ReadAllLines(path));
        Assert.Contains("slnx", checker.Ignored);
        Assert.Empty(spelling.Check("Open the slnx file.", 0));
    }

    [Fact]
    public void TheWordBeingTypedIsNotMarkedYet()
    {
        var spelling = new DraftSpelling(new FakeChecker("tomor"), new UserDictionary(Path.Combine(_folder, "d.txt")));

        Assert.Empty(spelling.Check("See you tomor", 13));
        Assert.Single(spelling.Check("See you tomor", 3));
    }

    [Fact]
    public void NoCheckerMeansNoErrorsAndNoCrash()
    {
        var spelling = new DraftSpelling(new FakeChecker(null), new UserDictionary(Path.Combine(_folder, "d.txt")));

        Assert.False(spelling.IsAvailable);
        Assert.Empty(spelling.Check("Anyhting goes.", 0));
    }

    [Fact]
    public void DictionaryReloadsFromItsFile()
    {
        var path = Path.Combine(_folder, "dictionary.txt");
        Directory.CreateDirectory(_folder);
        File.WriteAllLines(path, ["  Avalonia ", "", "sherpa"]);
        var dictionary = new UserDictionary(path);

        dictionary.Load();

        Assert.Equal(2, dictionary.Count);
        Assert.True(dictionary.Contains("avalonia"));
        Assert.False(dictionary.Add("SHERPA"));
    }

    /// <summary>Marks the given words wrong wherever they appear. Null words means "no checker installed".</summary>
    private sealed class FakeChecker : ISpellChecker
    {
        private readonly string[]? _wrong;

        public FakeChecker(params string[]? wrong)
        {
            _wrong = wrong;
        }

        public List<string> Ignored { get; } = [];

        public bool IsAvailable => _wrong is not null;

        public string LanguageTag => "en-GB";

        public IReadOnlyList<SpellingError> Check(string text)
        {
            var errors = new List<SpellingError>();
            foreach (var word in _wrong ?? [])
            {
                if (Ignored.Contains(word, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                var index = text.IndexOf(word, StringComparison.Ordinal);
                while (index >= 0)
                {
                    errors.Add(new SpellingError(index, word.Length));
                    index = text.IndexOf(word, index + word.Length, StringComparison.Ordinal);
                }
            }

            return errors.OrderBy(e => e.Start).ToList();
        }

        public IReadOnlyList<string> Suggest(string word) => [];

        public void Add(string word) => Ignored.Add(word);

        public void Ignore(string word) => Ignored.Add(word);
    }
}
