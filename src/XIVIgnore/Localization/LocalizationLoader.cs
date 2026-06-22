// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using System.Reflection;
using System.Text.Json;
using Dalamud.Plugin.Services;
using XIVIgnore.Core.Localization;

namespace XIVIgnore.Localization;

// Reads the embedded language JSONs and builds the Core localizer.
// Maps the two-letter UiLanguage code to a supported language (English otherwise).
public static class LocalizationLoader
{
    private static readonly string[] Supported = { "en", "de" };

    /// <summary>Builds the localizer from the embedded JSONs; starts in the language matching the UI.</summary>
    public static Localizer Build(string uiLanguage, IPluginLog log)
    {
        ArgumentNullException.ThrowIfNull(log);
        var asm = Assembly.GetExecutingAssembly();
        var langs = new Dictionary<string, Dictionary<string, string>>();
        foreach (var code in Supported)
        {
            var dict = Read(asm, code, log);
            if (dict != null)
            {
                langs[code] = dict;
            }
        }
        return new Localizer(langs, MapLanguage(uiLanguage));
    }

    // Returns the supported language, English otherwise.
    public static string MapLanguage(string uiLanguage)
        => Array.IndexOf(Supported, uiLanguage) >= 0 ? uiLanguage : "en";

    private static Dictionary<string, string>? Read(Assembly asm, string code, IPluginLog log)
    {
        // Resource name ends with "Localization.<code>.json".
        var name = asm.GetManifestResourceNames()
                      .FirstOrDefault(n => n.EndsWith($"Localization.{code}.json", StringComparison.Ordinal));
        if (name == null) { log.Warning($"[i18n] language resource missing: {code}"); return null; }
        try
        {
            using var s = asm.GetManifestResourceStream(name)!;
            using var r = new StreamReader(s);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(r.ReadToEnd());
        }
        catch (Exception ex)
        {
            log.Error(ex, $"[i18n] language resource unreadable: {code}");
            return null;
        }
    }
}
