// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using XIVIgnore.Core.Models;

namespace XIVIgnore.Core.Services;

public static class ExpiryCalculator
{
    /// <summary>amount ≤ 0 → null (permanent); otherwise now + duration.</summary>
    public static DateTimeOffset? Compute(int amount, DurationUnit unit, DateTimeOffset now)
    {
        if (amount <= 0)
        {
            return null;
        }

        return unit switch
        {
            DurationUnit.Minutes => now.AddMinutes(amount),
            DurationUnit.Hours => now.AddHours(amount),
            DurationUnit.Days => now.AddDays(amount),
            DurationUnit.Weeks => now.AddDays(amount * 7),
            DurationUnit.Months => now.AddMonths(amount),
            _ => null,
        };
    }
}
