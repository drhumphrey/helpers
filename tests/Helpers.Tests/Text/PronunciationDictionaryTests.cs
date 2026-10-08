using Helpers.Core.Text;

namespace Helpers.Tests.Text;

public class PronunciationDictionaryTests
{
    [Fact]
    public void ReplacesWholeWordsOnly()
    {
        var dictionary = new PronunciationDictionary();
        dictionary.Add("npm", "N P M");

        Assert.Equal("run N P M install", dictionary.Apply("run npm install"));
        Assert.Equal("the npmjs site", dictionary.Apply("the npmjs site"));
    }

    [Fact]
    public void IgnoresCaseByDefault()
    {
        var dictionary = new PronunciationDictionary();
        dictionary.Add("json", "jason");

        Assert.Equal("a jason file", dictionary.Apply("a JSON file"));
    }

    [Fact]
    public void CanBeCaseSensitive()
    {
        var dictionary = new PronunciationDictionary();
        dictionary.Add("PR", "pull request", caseSensitive: true);

        Assert.Equal("open a pull request", dictionary.Apply("open a PR"));
        Assert.Equal("good pr work", dictionary.Apply("good pr work"));
    }

    [Fact]
    public void HandlesWordsWithSymbols()
    {
        var dictionary = new PronunciationDictionary();
        dictionary.Add(".NET", "dot net");
        dictionary.Add("C#", "C sharp");

        Assert.Equal("dot net and C sharp", dictionary.Apply(".NET and C#"));
    }

    [Fact]
    public void AddingTheSameWordReplacesTheOldEntry()
    {
        var dictionary = new PronunciationDictionary();
        dictionary.Add("EVAR", "ee var");
        dictionary.Add("evar", "e-var");

        Assert.Single(dictionary.Entries);
        Assert.Equal("say e-var", dictionary.Apply("say EVAR"));
    }

    [Fact]
    public void RemoveReportsWhetherItRemovedAnything()
    {
        var dictionary = new PronunciationDictionary();
        dictionary.Add("UIA", "U I A");

        Assert.True(dictionary.Remove("uia"));
        Assert.False(dictionary.Remove("uia"));
        Assert.Equal("UIA stays", dictionary.Apply("UIA stays"));
    }

    [Fact]
    public void StarterListCoversCommonChatWords()
    {
        var starter = PronunciationDictionary.CreateStarter();

        Assert.Contains(starter.Entries, e => e.Word == "npm");
        Assert.Contains(starter.Entries, e => e.Word == "EVAR");
        Assert.Equal("run N P M then read the jason", starter.Apply("run npm then read the JSON"));
    }
}
