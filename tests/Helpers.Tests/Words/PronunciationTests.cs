using Helpers.Core.Words;

namespace Helpers.Tests.Words;

public class PronunciationTests
{
    [Fact]
    public void ExpandsMisakiVowelsToIpa()
    {
        Assert.Equal("ɹˌɛdʒɪstɹˈeɪʃən", Pronunciation.ToIpa("ɹ ˌ ɛ ʤ ɪ s t ɹ ˈ A ʃ ᵊ n"));
        Assert.Equal("pˈɑːsəl", Pronunciation.ToIpa("p ˈ ɑ ː s ᵊ l"));
        Assert.Equal("ɡˈəʊ", Pronunciation.ToIpa("ɡ ˈ Q"));
    }

    [Theory]
    [InlineData("ɹ ˌ ɛ ʤ ɪ s t ɹ ˈ A ʃ ᵊ n", "re-jis-TRAY-shuhn")]
    [InlineData("p ˈ ɑ ː s ᵊ l", "PAH-suhl")]
    [InlineData("θ ˈ ɪ ŋ k ɪ ŋ", "THING-king")]
    [InlineData("ɡ ˈ Q", "GOH")]
    public void RespellsWithTheStressedSyllableInCapitals(string phonemes, string expected)
    {
        Assert.Equal(expected, Pronunciation.Respell(phonemes));
    }

    [Fact]
    public void ReadsTheLexiconFileWhenPresent()
    {
        var path = Path.Combine(Path.GetTempPath(), "HelpersTests", Guid.NewGuid().ToString("N") + ".txt");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllLines(path, ["parcel p ˈ ɑ ː s ᵊ l", "kokoro k ˈ O k ə ɹ O"]);
        try
        {
            var pronunciation = new Pronunciation(path);

            Assert.True(pronunciation.IsAvailable);
            Assert.Equal("pˈɑːsəl", pronunciation.Ipa("Parcel"));
            Assert.Null(pronunciation.Ipa("nothing"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void MissingLexiconGivesNothingAndNoCrash()
    {
        var pronunciation = new Pronunciation(Path.Combine(Path.GetTempPath(), "no-such-file.txt"));

        Assert.False(pronunciation.IsAvailable);
        Assert.Null(pronunciation.Respelling("parcel"));
    }
}
