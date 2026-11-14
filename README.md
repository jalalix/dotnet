# JalaliX

Jalali (Persian / Shamsi) calendar for .NET. Pure C# / `TimeZoneInfo` — target **net8.0** (auto-falls back to net6.0 when SDK 8 is unavailable; override with `JalaliXTfm=net6.0`).

```csharp
using JalaliX;

var date = JalaliX.Create("2025-02-25");

date.Format("YYYY/MM/DD");  // 1403/12/07
date.Format("DD MMM YYYY", new Dictionary<string, object?> { ["persianDigits"] = true });

JalaliX.ParseJalali("1403/12/07").ToISODate();  // 2025-02-25
```

## Months

- `GetMonth()` -> 1..12 (Farvardin = 1)
- `SetMonth(m)` / `SetFullYear(y, m)` -> 0..11, same as JS `Date` (Farvardin = 0)

## Timezone

Internal storage is a **UTC Unix millisecond instant** plus an IANA timezone id. `SetTimezone` is **instant-preserving**.

```csharp
var d = JalaliX.Create(1_740_430_800_000L);
var tehran = d.SetTimezone("Asia/Tehran");
tehran.Format("YYYY/MM/DD");  // 1403/12/07
tehran.Format("HH:mm:ss");    // 00:30:00
```

- **Aliases:** `utc` / `gmt` -> `UTC`; `local` / `default` / `system` -> host zone.
- **Invalid zone** -> `ArgumentException`.

## Test

```bash
dotnet test
```

## License

MIT
