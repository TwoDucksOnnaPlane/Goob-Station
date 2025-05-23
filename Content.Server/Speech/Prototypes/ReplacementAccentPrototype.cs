// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 SX_7 <sn1.test.preria.2002@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Server.Speech.Prototypes;

[Prototype("accent")]
public sealed partial class ReplacementAccentPrototype : IPrototype
{
    /// <inheritdoc/>
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     If this array is non-null, the full text of anything said will be randomly replaced with one of these words.
    /// </summary>
    [DataField]
    public string[]? FullReplacements;

    /// <summary>
    ///     If this dictionary is non-null and <see cref="FullReplacements"/> is null, any keys surrounded by spaces
    ///     (words) will be replaced by the value, attempting to intelligently keep capitalization.
    /// </summary>
    [DataField]
    public Dictionary<string, string>? WordReplacements;

    /// <summary>
    /// Allows you to substitute words, not always, but with some chance
    /// </summary>
    [DataField]
    public float ReplacementChance = 1f;
}