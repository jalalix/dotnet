using Xunit;
using Jx = JalaliX.JalaliX;

namespace JalaliX.Tests;

public class JalaliXTests
{
	[Fact]
	public void FromPersianConvertsPersianAndArabicDigits()
	{
		Assert.Equal("1403/12/07", Jx.FromPersian("۱۴۰۳/۱۲/۰۷"));
		Assert.Equal("1403/12/07", Jx.FromPersian("١٤٠٣/١٢/٠٧"));
	}

	[Fact]
	public void ParseJalaliAcceptsDotSeparator()
	{
		Assert.Equal("2025-02-25", Jx.ParseJalali("1403.12.07").ToISODate());
	}

	[Fact]
	public void ParseJalaliPersianDigitsAndTime()
	{
		var d = Jx.ParseJalali("۱۴۰۳-۱۲-۰۷ 14:30");
		Assert.Equal("1403/12/07", d.Format("YYYY/MM/DD"));
		Assert.Equal(14, d.GetHours());
		Assert.Equal(30, d.GetMinutes());
	}

	[Fact]
	public void NormalizeQueryDateSentinelsAndTimestamps()
	{
		Assert.Null(Jx.NormalizeQueryDate(null));
		Assert.Null(Jx.NormalizeQueryDate("null"));
		Assert.Null(Jx.NormalizeQueryDate(""));
		Assert.Null(Jx.NormalizeQueryDate("undefined"));
		Assert.Null(Jx.NormalizeQueryDate("Invalid Date"));
		Assert.Null(Jx.NormalizeQueryDate("NaN"));
		Assert.Null(Jx.NormalizeQueryDate(0));
		Assert.Null(Jx.NormalizeQueryDate(-1));

		const long seconds = 1740430800L; // 2025-02-24 21:00:00 UTC / 2025-02-25 00:30:00 Tehran

		Assert.Equal(
			"2025-02-24 21:00:00",
			Jx.NormalizeQueryDate(seconds, new Dictionary<string, object?> { ["timezone"] = "UTC" }));

		Assert.Equal(
			"2025-02-25 00:30:00",
			Jx.NormalizeQueryDate(seconds * 1000, new Dictionary<string, object?> { ["timezone"] = "Asia/Tehran" }));
	}

	[Fact]
	public void NormalizeQueryDateJalaliAndGregorian()
	{
		Assert.Equal(
			"2025-02-25 00:00:00",
			Jx.NormalizeQueryDate("1403/12/07", new Dictionary<string, object?> { ["timezone"] = "UTC" }));

		Assert.Equal(
			"2025-02-25 14:30:00",
			Jx.NormalizeQueryDate("۱۴۰۳-۱۲-۰۷ 14:30:00", new Dictionary<string, object?> { ["timezone"] = "UTC" }));

		Assert.Equal(
			"2025-02-25 00:00:00",
			Jx.NormalizeQueryDate("2025.02.25"));

		Assert.Equal(
			"2025-02-25 12:30:45",
			Jx.NormalizeQueryDate("2025-02-25 12:30:45.123Z"));

		Assert.Equal(
			"not-a-date",
			Jx.NormalizeQueryDate("not-a-date"));

		// Wider default band than the old 1278-1600 heuristic.
		Assert.NotNull(Jx.NormalizeQueryDate("1200/01/01", new Dictionary<string, object?> { ["timezone"] = "UTC" }));
		Assert.NotNull(Jx.NormalizeQueryDate("1800/01/01", new Dictionary<string, object?> { ["timezone"] = "UTC" }));

		// Gregorian years stay Gregorian; override bounds when needed.
		Assert.Equal("2025-01-01 00:00:00", Jx.NormalizeQueryDate("2025-01-01"));

		Assert.NotEqual(
			"2025-01-01 00:00:00",
			Jx.NormalizeQueryDate("2025-01-01", new Dictionary<string, object?>
			{
				["timezone"] = "UTC",
				["jalaliYearMin"] = 1,
				["jalaliYearMax"] = 3000,
			}));
	}
}
