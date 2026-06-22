// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using System.Globalization;

namespace XIVIgnore.Core.Localization;

// Lean key->text lookup with a fallback chain: active language -> English -> the key itself.
// Dalamud-free so it stays unit-testable in Core. Language dictionaries are supplied from outside (the plugin).
public sealed class Localizer
{
    public const string FallbackLanguage = "en";

    private readonly Dictionary<string, Dictionary<string, string>> _languages;

    public Localizer(IReadOnlyDictionary<string, Dictionary<string, string>> languages, string active)
    {
        ArgumentNullException.ThrowIfNull(languages);
        _languages = new Dictionary<string, Dictionary<string, string>>(languages);
        SetActiveLanguage(active);
    }

    public string Active { get; private set; } = FallbackLanguage;

    /// <summary>Switches to the given language; unknown languages fall back to English.</summary>
    public void SetActiveLanguage(string lang)
        => Active = _languages.ContainsKey(lang) ? lang : FallbackLanguage;

    /// <summary>Resolves a key through the fallback chain (active language → English → the key itself).</summary>
    public string Get(string key)
    {
        if (_languages.TryGetValue(Active, out var a) && a.TryGetValue(key, out var v))
        {
            return v;
        }

        if (_languages.TryGetValue(FallbackLanguage, out var f) && f.TryGetValue(key, out var fv))
        {
            return fv;
        }

        return key;
    }

    /// <summary>Like <see cref="Get(string)"/>, but formats with <paramref name="args"/>; on an invalid format the raw template is kept.</summary>
    public string Get(string key, params object[] args)
    {
        var fmt = Get(key);
        try { return string.Format(CultureInfo.InvariantCulture, fmt, args); }
        catch (FormatException) { return fmt; }
    }
}
