// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using XIVIgnore.Core.Localization;

namespace XIVIgnore.Core.Tests.Localization;

public class LocalizerTests
{
    private static Localizer Make()
    {
        var langs = new Dictionary<string, Dictionary<string, string>>
        {
            ["en"] = new() { ["greet"] = "Hello", ["with"] = "Hi {0}@{1}", ["onlyEn"] = "EnOnly" },
            ["de"] = new() { ["greet"] = "Hallo", ["with"] = "Hi {0}@{1}" },
        };
        return new Localizer(langs, "de");
    }

    [Fact]
    public void Get_ReturnsActiveLanguageValue()
        => Assert.Equal("Hallo", Make().Get("greet"));

    [Fact]
    public void Get_FallsBackToEnglish_WhenKeyMissingInActive()
        => Assert.Equal("EnOnly", Make().Get("onlyEn"));

    [Fact]
    public void Get_ReturnsKey_WhenMissingEverywhere()
        => Assert.Equal("nope", Make().Get("nope"));

    [Fact]
    public void Get_FormatsArguments()
        => Assert.Equal("Hi Velvet@Ragnarok", Make().Get("with", "Velvet", "Ragnarok"));

    [Fact]
    public void SetActiveLanguage_SwitchesResolution()
    {
        var loc = Make();
        loc.SetActiveLanguage("en");
        Assert.Equal("Hello", loc.Get("greet"));
    }

    [Fact]
    public void SetActiveLanguage_UnknownFallsBackToEnglish()
    {
        var loc = Make();
        loc.SetActiveLanguage("fr");
        Assert.Equal("Hello", loc.Get("greet")); // fr unbekannt → en
    }
}
