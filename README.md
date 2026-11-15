# JalaliX - .NET

Jalali (Persian / Shamsi) calendar for .NET. Pure C# / `TimeZoneInfo` - target **net8.0** (auto-falls back to net6.0 when SDK 8 is unavailable; override with `JalaliXTfm=net6.0`).

[![license](https://img.shields.io/badge/license-MIT-blue.svg)](./LICENSE)

```csharp
using JalaliX;

var date = JalaliX.Create("2025-02-25");

date.Format("YYYY/MM/DD"); // 1403/12/07
date.Format("DD MMM YYYY", new Dictionary<string, object?> { ["persianDigits"] = true });

JalaliX.ParseJalali("1403/12/07").ToISODate(); // 2025-02-25
```

`ToISODate()` uses wall-clock getters in the active timezone.

## Install

```bash
dotnet add package JalaliX
```

```csharp
using JalaliX;
```

## Usage

### create / format

```csharp
var now = JalaliX.Create(DateTimeOffset.UtcNow, new Dictionary<string, object?> { ["timezone"] = "Asia/Tehran" });

now.Format("YYYY/MM/DD");
now.Format("WN، DD MMM YYYY");
now.Format("YYYY/MM/DD", new Dictionary<string, object?> { ["persianDigits"] = true });
now.Format("DD MMM YYYY", new Dictionary<string, object?> { ["calendar"] = "gregorian" });
```

`Format` also accepts the older positional form: `Format(str, persianDigits, gregorian)`.

### ParseJalali

```csharp
JalaliX.ParseJalali("1403/12/07");
JalaliX.ParseJalali("۱۴۰۳-۱۲-۰۷ 14:30");
JalaliX.ParseJalali("1403.12.07");
```

Named `ParseJalali` to avoid confusion with `DateTime.Parse`. Accepts `/`, `-`, or `.` as the date separator.

### NormalizeQueryDate

Soft-normalize loose API/query date values to a Gregorian `YYYY-MM-DD HH:mm:ss` string (or `null`). Does not throw on junk input.

```csharp
JalaliX.NormalizeQueryDate("1403/12/07", new Dictionary<string, object?> { ["timezone"] = "Asia/Tehran" });
// "2025-02-25 00:00:00"

JalaliX.NormalizeQueryDate("undefined"); // null
JalaliX.NormalizeQueryDate(1740430800000L, new Dictionary<string, object?> { ["timezone"] = "Asia/Tehran" });
// "2025-02-25 00:30:00"
```

Handles sentinels, unix timestamps (seconds or milliseconds; non-positive → `null`), Persian/Arabic-Indic digits, `/` `-` `.` separators, fractional seconds, optional `Z` / offset suffixes.

Detects Jalali when the year is in `jalaliYearMin`…`jalaliYearMax` (default `1`…`1899`) **and** the civil date is valid; otherwise Gregorian when valid, or returns the digit-cleaned string. Non-string/non-number values pass through unchanged.

### ToISODate

```csharp
var d = JalaliX.Create("2025-02-25T00:30:00+03:30");

d.ToISODate(); // 2025-02-25
d.ToISODate("jalali"); // 1403-12-07
```

### Add / start / end

```csharp
JalaliX.Create("2025-02-25").AddMonths(1).GetStartOfMonth().Format("YYYY/MM/DD");
// e.g. 1404/01/01

var d = JalaliX.Create(1740430800000L, new Dictionary<string, object?> { ["timezone"] = "Asia/Tehran" });
d.AddDays(7).Format("YYYY/MM/DD");
d.GetEndOfWeek().Format("YYYY/MM/DD");
```

### Persian / Arabic digits

```csharp
JalaliX.ToPersian("1403/12/07"); // ۱۴۰۳/۱۲/۰۷
JalaliX.FromPersian("۱۴۰۳/۱۲/۰۷"); // 1403/12/07
JalaliX.FromPersian("١٤٠٣/١٢/٠٧"); // 1403/12/07 (Arabic-Indic)
```

## Timezone

Internal storage is always a **UTC Unix millisecond instant** plus an IANA timezone id. `SetTimezone` is **instant-preserving**: the timestamp stays the same; only the zone (and therefore wall clock / Jalali day) changes.

```csharp
var d = JalaliX.Create(1740430800000L);

var tehran = d.SetTimezone("Asia/Tehran");
tehran.Format("YYYY/MM/DD"); // 1403/12/07
tehran.Format("HH:mm"); // 00:30

var utc = d.SetTimezone("UTC");
utc.Format("YYYY/MM/DD"); // 1403/12/06
utc.Format("HH:mm"); // 21:00

tehran.GetTime() == utc.GetTime(); // true
```

- **Aliases:** `utc` / `gmt` → `UTC`; `local` / `default` / `system` → host zone from `TimeZoneInfo.Local`.
- **Invalid zone** → `ArgumentException`.

```csharp
JalaliX.Create(DateTimeOffset.UtcNow, new Dictionary<string, object?> { ["timezone"] = "Asia/Tehran" });
```

## Months

Easy to get wrong:

- `GetMonth()` → **1…12** (Farvardin = 1)
- `SetMonth(m)` / `SetFullYear(y, m)` → **0…11**, same as JavaScript `Date` (Farvardin = 0)

```csharp
var d = JalaliX.Create(DateTimeOffset.UtcNow);
d.SetFullYear(1404, 0, 1); // 1 Farvardin 1404
d.GetMonth(); // 1
```

## Format tokens

| Token               | Meaning                         |
| ------------------- | ------------------------------- |
| `YYYY` `YY`         | year                            |
| `MMM` `MM` `M`      | month name / number             |
| `DD` `D`            | day                             |
| `HH` `hh` `mm` `ss` | time                            |
| `A` `a`             | meridiem (ق.ظ. / ب.ظ. or AM/PM) |
| `WN` `WL`           | weekday name / letter           |
| `w` `DDD`           | week of year / day of year      |
| `X` `x`             | unix seconds / ms               |

## API

Use `IDictionary<string, object?>` for options (`timezone`, `jalaliYearMin`, `jalaliYearMax`, `persianDigits`, `calendar`).

### Static

| API                                   | Description                                                                                                  |
| ------------------------------------- | ------------------------------------------------------------------------------------------------------------ |
| `PersianNumbers`                      | Array of Persian digit characters `۰`…`۹`.                                                                   |
| `ArabicNumbers`                       | Array of Arabic-Indic digit characters `٠`…`٩`.                                                              |
| `Create(date, options?)`              | Build from unix ms, ISO/`YYYY-MM-DD` string, `DateTime`, `DateTimeOffset`, or another `JalaliX`.             |
| `Create(unixMs, timezone?)`           | Build from Unix milliseconds with optional timezone.                                                         |
| `Create(isoDate, timezone?)`          | Build from ISO date string with optional timezone.                                                           |
| `ParseJalali(str, options?)`          | Parse a Jalali date/time string (`/` `-` `.`, Persian/Arabic digits, optional time).                         |
| `NormalizeQueryDate(value, options?)` | Soft-normalize loose query values → Gregorian `YYYY-MM-DD HH:mm:ss`, cleaned string, passthrough, or `null`. |
| `IsValidDate(value)`                  | `true` if `value` is `JalaliX`, `DateTime`, or `DateTimeOffset`.                                             |
| `ToPersian(str)`                      | Replace Western digits `0-9` with Persian `۰-۹`.                                                             |
| `FromPersian(str)`                    | Replace Persian `۰-۹` and Arabic-Indic `٠-٩` with Western digits.                                            |
| `Compare(a, b)`                       | `true` if `a` is strictly before `b` (by unix ms).                                                           |

### Instance - getters

| API                 | Description                                                           |
| ------------------- | --------------------------------------------------------------------- |
| `GetFullYear()`     | Jalali year in the active timezone.                                   |
| `GetMonth()`        | Jalali month **1…12** (Farvardin = 1).                                |
| `GetDate()`         | Jalali day of month **1…31**.                                         |
| `GetDay()`          | Weekday **1…7** (Saturday = 1 … Friday = 7).                          |
| `GetHours()`        | Wall-clock hours in the active timezone.                              |
| `GetMinutes()`      | Wall-clock minutes.                                                   |
| `GetSeconds()`      | Wall-clock seconds.                                                   |
| `GetMilliseconds()` | Wall-clock milliseconds.                                              |
| `GetTime()`         | Unix timestamp in milliseconds (UTC instant).                         |
| `GetTimezone()`     | Active IANA timezone id.                                              |
| `GetWeekOfYear()`   | Jalali week-of-year (1-based).                                        |
| `GetDayOfYear()`    | Day of Jalali year (1…365/366).                                       |
| `GetDayISO()`       | ISO-style weekday index used internally for week math.                |
| `GetHoursISO()`     | Hours in 0…23 for formatting helpers.                                 |
| `GetMeridiem()`     | Localized meridiem string (ق.ظ./ب.ظ. or AM/PM depending on calendar). |

### Instance - setters

| API                                | Description                                              |
| ---------------------------------- | -------------------------------------------------------- |
| `SetTime(ms)`                      | Set the UTC instant from unix milliseconds.              |
| `SetMilliseconds(msVal)`           | Set milliseconds (wall clock).                           |
| `SetSeconds(sec, ms?)`             | Set seconds and optional milliseconds.                   |
| `SetMinutes(min, sec?, ms?)`       | Set minutes and optional lower units.                    |
| `SetHours(hours, min?, sec?, ms?)` | Set hours and optional lower units.                      |
| `SetDate(date)`                    | Set Jalali day of month.                                 |
| `SetMonth(month, date?)`           | Set Jalali month **0…11** (Farvardin = 0); optional day. |
| `SetFullYear(year, month?, date?)` | Set Jalali year; optional month **0…11** and day.        |
| `SetTimezone(timezone)`            | Instant-preserving zone change; returns a new instance.  |

### Instance - math, range, format

| API                     | Description                                                                 |
| ----------------------- | --------------------------------------------------------------------------- |
| `AddYears(n)`           | Add calendar years (mutates / returns `this`).                              |
| `AddMonths(n)`          | Add calendar months.                                                        |
| `AddWeeks(n)`           | Add weeks.                                                                  |
| `AddDays(n)`            | Add days.                                                                   |
| `GetStartOfYear()`      | Start of Jalali year (new instance).                                        |
| `GetStartOfMonth()`     | Start of Jalali month.                                                      |
| `GetStartOfWeek()`      | Start of week.                                                              |
| `GetStartOfDay()`       | Start of day.                                                               |
| `GetEndOfYear()`        | End of Jalali year.                                                         |
| `GetEndOfMonth()`       | End of Jalali month.                                                        |
| `GetEndOfWeek()`        | End of week.                                                                |
| `GetEndOfDay()`         | End of day.                                                                 |
| `Format(str, options?)` | Format with tokens; dictionary or positional `persianDigits` / `gregorian`. |
| `ToISODate(calendar?)`  | `YYYY-MM-DD` in `"gregorian"` (default) or `"jalali"`.                      |
| `Clone()`               | Copy with the same instant and timezone.                                    |
| `ToString()`            | Human-readable Jalali date/time string.                                     |
| `ToDateString()`        | Jalali weekday + date string.                                               |
| `ToLocaleDateString()`  | `YYYY/MM/DD` in Jalali.                                                     |
| `ToLocaleTimeString()`  | 12-hour time with meridiem.                                                 |

## Test

```bash
dotnet test
```

## Links

- https://jalalix.com
- https://github.com/jalalix/dotnet
- [CHANGELOG.md](./CHANGELOG.md)

## License

MIT
