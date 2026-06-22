// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using XIVIgnore.Core.Models;
using XIVIgnore.Core.Services;

namespace XIVIgnore.Core.Tests;

public class ExpiryCalculatorTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-06-20T12:00:00+00:00");

    [Fact] public void Amount_0_is_permanent() => Assert.Null(ExpiryCalculator.Compute(0, DurationUnit.Days, Now));
    [Fact] public void Negative_is_permanent() => Assert.Null(ExpiryCalculator.Compute(-3, DurationUnit.Hours, Now));

    [Fact] public void Minutes() => Assert.Equal(Now.AddMinutes(5), ExpiryCalculator.Compute(5, DurationUnit.Minutes, Now));
    [Fact] public void Hours() => Assert.Equal(Now.AddHours(2), ExpiryCalculator.Compute(2, DurationUnit.Hours, Now));
    [Fact] public void Days() => Assert.Equal(Now.AddDays(3), ExpiryCalculator.Compute(3, DurationUnit.Days, Now));
    [Fact] public void Weeks_are_7_days() => Assert.Equal(Now.AddDays(14), ExpiryCalculator.Compute(2, DurationUnit.Weeks, Now));
    [Fact] public void Months() => Assert.Equal(Now.AddMonths(1), ExpiryCalculator.Compute(1, DurationUnit.Months, Now));
}
