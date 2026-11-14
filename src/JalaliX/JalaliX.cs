namespace Jalalix;

// <summary>
// Jalali (Persian / Shamsi) calendar. Pure .NET port of @jalalix/typescript.
// Instant-preserving IANA timezones via <see cref="TimeZoneInfo"/>.
// </summary>
public sealed class JalaliX
{
    public static readonly string[] PersianNumbers = { "۰", "۱", "۲", "۳", "۴", "۵", "۶", "۷", "۸", "۹" };

    private sealed class WallParts
    {
        public int Year, Month, Day, Hour, Minute, Second, Ms;
    }

    private sealed class JalaliDate
    {
        public int Year = 1348, Month = 10, Day = 11, Hours, Min, Sec, Ms;
        public bool IsLeapYear;
    }

    private long _ms;
    private string _timezoneId = "UTC";
    private JalaliDate _jalali = new();
    private bool _isConverted;
    private readonly double _leapYearDivision = 0.24219858156;
    private readonly int _firstDayOfWeek = 6;
    private readonly long _dateLength = 86_400_000L;
    private readonly string[] _weekDaysNames = { "شنبه", "یکشنبه", "دوشنبه", "سه شنبه", "چهار شنبه", "پنج شنبه", "جمعه" };
    private readonly string[] _weekDaysLetters = { "ش", "ی", "د", "س", "چ", "پ", "ج" };
    private readonly string[] _weekDaysGregorianNames = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
    private readonly string[] _weekDaysGregorianLetters = { "S", "M", "T", "W", "T", "F", "S" };
    private readonly string[] _monthsNames = { "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند" };
    private readonly string[] _monthsGregorianNames = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
    private readonly string _am = "ق.ظ.";
    private readonly string _pm = "ب.ظ.";
    private readonly string _amGregorian = "AM";
    private readonly string _pmGregorian = "PM";

    public JalaliX(long? milliseconds = null)
    {
        _ms = milliseconds ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    private static string HostTimezone()
    {
        try { return TimeZoneInfo.Local.Id; }
        catch { return "UTC"; }
    }

    private TimeZoneInfo ResolveTimeZone()
    {
        if (string.Equals(_timezoneId, "UTC", StringComparison.OrdinalIgnoreCase) ||
            _timezoneId == "Etc/UTC")
            return TimeZoneInfo.Utc;

        try { return TimeZoneInfo.FindSystemTimeZoneById(_timezoneId); }
        catch (Exception ex) { throw new ArgumentException($"Invalid IANA timezone: {_timezoneId}", ex); }
    }

    private WallParts GetWallParts()
    {
        var tz = ResolveTimeZone();
        var dto = DateTimeOffset.FromUnixTimeMilliseconds(_ms);
        var local = TimeZoneInfo.ConvertTime(dto, tz);

        return new WallParts
        {
            Year = local.Year,
            Month = local.Month,
            Day = local.Day,
            Hour = local.Hour,
            Minute = local.Minute,
            Second = local.Second,
            Ms = (int)(((_ms % 1000) + 1000) % 1000)
        };
    }

    private long WallToUtc(int y, int m, int d, int h, int mi, int s, int msVal)
    {
        var want = new DateTimeOffset(y, m, d, h, mi, s, TimeSpan.Zero).ToUnixTimeMilliseconds();
        var utc = want;
        var tz = ResolveTimeZone();

        for (var i = 0; i < 14; i++)
        {
            var probe = DateTimeOffset.FromUnixTimeMilliseconds(utc);
            var local = TimeZoneInfo.ConvertTime(probe, tz);
            var got = new DateTimeOffset(local.Year, local.Month, local.Day, local.Hour, local.Minute, local.Second, TimeSpan.Zero)
                .ToUnixTimeMilliseconds();
            var diff = want - got;
            if (diff == 0) break;
            utc += diff;
        }

        var normalizedMs = ((msVal % 1000) + 1000) % 1000;

        return (utc / 1000) * 1000 + normalizedMs;
    }

    private JalaliDate ToJalali()
    {
        if (!_isConverted)
        {
            var wall = GetWallParts();
            var civilDay = new DateTimeOffset(wall.Year, wall.Month, wall.Day, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds() / _dateLength;
            var baseDay = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds() / _dateLength;
            var diffDays = (int)(civilDay - baseDay);

            _jalali.Year = 1348;
            _jalali.Month = 10;
            _jalali.Day = 11;

            if (diffDays > 0)
            {
                for (var i = 0; i < diffDays; i++)
                {
                    _jalali.Day++;

                    if (_jalali.Month < 7 && _jalali.Day > 31) { _jalali.Month++; _jalali.Day = 1; }
                    else if (_jalali.Month > 6 && _jalali.Month < 12 && _jalali.Day > 30) { _jalali.Month++; _jalali.Day = 1; }
                    else if (_jalali.Month == 12)
                    {
                        var leap = CheckLeapYear(_jalali.Year);

                        if (leap && _jalali.Day > 30) { _jalali.Year++; _jalali.Month = 1; _jalali.Day = 1; }
                        else if (!leap && _jalali.Day > 29) { _jalali.Year++; _jalali.Month = 1; _jalali.Day = 1; }
                    }
                }
            }
            else if (diffDays < 0)
            {
                for (var i = 0; i < -diffDays; i++)
                {
                    _jalali.Day--;

                    if (_jalali.Day < 1)
                    {
                        if (_jalali.Month == 1)
                        {
                            _jalali.Year--; _jalali.Month = 12;
                            _jalali.Day = CheckLeapYear(_jalali.Year) ? 30 : 29;
                        }
                        else if (_jalali.Month < 8) { _jalali.Month--; _jalali.Day = 31; }
                        else if (_jalali.Month < 13) { _jalali.Month--; _jalali.Day = 30; }
                    }
                }
            }

            _jalali.Hours = wall.Hour;
            _jalali.Min = wall.Minute;
            _jalali.Sec = wall.Second;
            _jalali.Ms = wall.Ms;
            _jalali.IsLeapYear = CheckLeapYear(_jalali.Year);
            _isConverted = true;
        }

        var w = GetWallParts();
        _jalali.Hours = w.Hour;
        _jalali.Min = w.Minute;
        _jalali.Sec = w.Second;
        _jalali.Ms = w.Ms;

        return _jalali;
    }

    private long ToGregorian(int year, int month, int day, int hours, int min, int sec, int msVal)
    {
        var jy = 1348; var jm = 10; var jd = 11;
        var diffDays = 0;
        var after = year > 1348 || (year == 1348 && month >= 10) || (year == 1348 && month == 10 && day >= 11);
        var before = year < 1348 || (year == 1348 && month < 10) || (year == 1348 && month == 10 && day < 11);

        if (after)
        {
            while (!(jy == year && jm == month && jd == day))
            {
                diffDays++; jd++;

                if (jm < 7 && jd > 31) { jm++; jd = 1; }
                else if (jm > 6 && jm < 12 && jd > 30) { jm++; jd = 1; }
                else if (jm == 12)
                {
                    var leap = CheckLeapYear(jy);

                    if (leap && jd > 30) { jy++; jm = 1; jd = 1; }
                    else if (!leap && jd > 29) { jy++; jm = 1; jd = 1; }
                }
            }
        }
        else if (before)
        {
            while (!(jy == year && jm == month && jd == day))
            {
                diffDays--; jd--;

                if (jd < 1)
                {
                    if (jm == 1) { jy--; jm = 12; jd = CheckLeapYear(jy) ? 30 : 29; }
                    else if (jm < 8) { jm--; jd = 31; }
                    else if (jm < 13) { jm--; jd = 30; }
                }
            }
        }

        var wallAsUtc = diffDays * _dateLength + (hours * 3600L + min * 60L + sec) * 1000L + msVal;
        var civil = DateTimeOffset.FromUnixTimeMilliseconds(wallAsUtc).UtcDateTime;

        return WallToUtc(civil.Year, civil.Month, civil.Day, civil.Hour, civil.Minute, civil.Second, civil.Millisecond);
    }

    private bool CheckLeapYear(int year)
    {
        if (year <= 0) return false;

        var checkYear = (year + 2346) * _leapYearDivision;

        return checkYear - Math.Floor(checkYear) < _leapYearDivision;
    }

    private JalaliDate Reset()
    {
        _isConverted = false;
        _jalali.Year = 1348; _jalali.Month = 10; _jalali.Day = 11;

        return ToJalali();
    }

    private static string AddZero(int number) => number is > -1 and < 10 ? $"0{number}" : $"{number}";

    private int GetWallWeekday()
    {
        var wall = GetWallParts();

        return (int)new DateTime(wall.Year, wall.Month, wall.Day, 0, 0, 0, DateTimeKind.Utc).DayOfWeek;
    }

    private long NormalizeDate(int days) => SetTime(_ms + days * _dateLength);

    private long NormalizeJalali(int year, int month, int day)
    {
        var y = year; var m = month; var d = day;

        if (m < 1)
        {
            var yearDiff = (int)Math.Ceiling((1 - m) / 12.0);
            y -= yearDiff; m = 12 * yearDiff + m;
        }
        else if (m > 12)
        {
            var yearDiff = (int)Math.Floor((m - 1) / 12.0);
            y += yearDiff; m -= 12 * yearDiff;
        }

        if (m > 6 && m < 12 && d > 30) d = 30;
        else if (m == 12)
        {
            var leap = CheckLeapYear(y);
            if (leap && d > 30) d = 30;
            else if (!leap && d > 29) d = 29;
        }

        var wall = GetWallParts();

        return SetTime(ToGregorian(y, m, d, wall.Hour, wall.Minute, wall.Second, wall.Ms));
    }

    private string NormalizeTimezone(string? timezone)
    {
        if (timezone is null) return _timezoneId;

        var trimmed = timezone.Trim();

        if (trimmed.Length == 0) throw new ArgumentException("Invalid IANA timezone: empty");

        var lowered = trimmed.ToLowerInvariant();

        if (lowered is "default" or "local" or "system") return HostTimezone();
        if (lowered is "utc" or "gmt") return "UTC";

        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(trimmed);
            return tz.Id is "Etc/UTC" or "GMT" ? "UTC" : (trimmed.Contains('/') ? trimmed : tz.Id);
        }
        catch
        {
            // On Windows, try converting; on Unix IANA should work.
            if (string.Equals(trimmed, "UTC", StringComparison.OrdinalIgnoreCase)) return "UTC";
            throw new ArgumentException($"Invalid IANA timezone: {trimmed}");
        }
    }

    public override string ToString()
    {
        var j = ToJalali();

        return $"{WeekDayName()} {AddZero(j.Day)} {_monthsNames[j.Month - 1]} {AddZero(j.Year)} {j.Hours}:{AddZero(j.Min)}:{AddZero(j.Sec)}{UtcTimezoneString()}";
    }

    public string ToDateString()
    {
        var j = ToJalali();

        return $"{WeekDayName()} {AddZero(j.Day)} {_monthsNames[j.Month - 1]} {AddZero(j.Year)}";
    }

    public string ToLocaleDateString()
    {
        var j = ToJalali();

        return $"{AddZero(j.Year)}/{AddZero(j.Month)}/{AddZero(j.Day)}";
    }

    public string ToLocaleTimeString()
    {
        var j = ToJalali();

        return $"{j.Hours % 12}:{AddZero(j.Min)}:{AddZero(j.Sec)} {GetMeridiem()}";
    }

    public int GetFullYear() => ToJalali().Year;
    public int GetMonth() => ToJalali().Month;
    public int GetDate() => ToJalali().Day;
    public int GetDay() => (GetWallWeekday() - _firstDayOfWeek + 7 + 1) % 7;
    public int GetHours() => GetWallParts().Hour;
    public int GetMinutes() => GetWallParts().Minute;
    public int GetSeconds() => GetWallParts().Second;
    public int GetMilliseconds() => (int)(((_ms % 1000) + 1000) % 1000);
    public long GetTime() => _ms;

    public long SetTime(long time)
    {
        _ms = time;
        Reset();

        return GetTime();
    }

    public long SetMilliseconds(int msVal)
    {
        var wall = GetWallParts();

        return SetTime(WallToUtc(wall.Year, wall.Month, wall.Day, wall.Hour, wall.Minute, wall.Second, msVal));
    }

    public long SetSeconds(int sec, int? msVal = null)
    {
        var wall = GetWallParts();

        return SetTime(WallToUtc(wall.Year, wall.Month, wall.Day, wall.Hour, wall.Minute, sec, msVal ?? wall.Ms));
    }

    public long SetMinutes(int min, int? sec = null, int? msVal = null)
    {
        var wall = GetWallParts();

        return SetTime(WallToUtc(wall.Year, wall.Month, wall.Day, wall.Hour, min, sec ?? wall.Second, msVal ?? wall.Ms));
    }

    public long SetHours(int hours, int? min = null, int? sec = null, int? msVal = null)
    {
        var wall = GetWallParts();

        return SetTime(WallToUtc(wall.Year, wall.Month, wall.Day, hours, min ?? wall.Minute, sec ?? wall.Second, msVal ?? wall.Ms));
    }

    public long SetDate(int date)
    {
        var j = ToJalali();
        NormalizeDate(date - j.Day);

        return GetTime();
    }

    public long SetMonth(int month, int? date = null)
    {
        var j = ToJalali();
        NormalizeJalali(j.Year, month + 1, j.Day);
        Reset();

        if (date is not null)
        {
            j = ToJalali();
            NormalizeDate(date.Value - j.Day);
            Reset();
        }

        return GetTime();
    }

    public long SetFullYear(int year, int? month = null, int? date = null)
    {
        var j = ToJalali();
        NormalizeJalali(year, month is null ? j.Month : month.Value + 1, j.Day);
        Reset();

        if (date is not null)
        {
            j = ToJalali();
            NormalizeDate(date.Value - j.Day);
            Reset();
        }

        return GetTime();
    }

    public static JalaliX Create(object date, IDictionary<string, object?>? options = null)
    {
        var output = new JalaliX(ParseInput(date));

        if (options is not null && options.TryGetValue("timezone", out var tz) && tz is string s)
            return output.SetTimezone(s);

        return output;
    }

    public static JalaliX Create(long unixMs, string? timezone = null)
    {
        var output = new JalaliX(unixMs);

        return timezone is null ? output : output.SetTimezone(timezone);
    }

    public static JalaliX Create(string isoDate, string? timezone = null)
    {
        var output = new JalaliX(ParseIsoString(isoDate));

        return timezone is null ? output : output.SetTimezone(timezone);
    }

    private static long ParseInput(object value) => value switch
    {
        JalaliX j => j.GetTime(),
        long l => l,
        int i => i,
        double d => (long)d,
        DateTimeOffset dto => dto.ToUnixTimeMilliseconds(),
        DateTime dt => new DateTimeOffset(DateTime.SpecifyKind(dt, dt.Kind == DateTimeKind.Unspecified ? DateTimeKind.Utc : dt.Kind)).ToUnixTimeMilliseconds(),
        string s => ParseIsoString(s),
        _ => throw new ArgumentException($"JalaliX.create: unsupported date type {value.GetType()}")
    };

    private static long ParseIsoString(string value)
    {
        var s = value.Trim();

        if (System.Text.RegularExpressions.Regex.IsMatch(s, @"^\d{4}-\d{2}-\d{2}$"))
        {
            var parts = s.Split('-').Select(int.Parse).ToArray();
            return new DateTimeOffset(parts[0], parts[1], parts[2], 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();
        }

        if (DateTimeOffset.TryParse(s, out var dto))
            return dto.ToUnixTimeMilliseconds();

        throw new ArgumentException($"JalaliX.create: invalid date string \"{value}\"");
    }

    public static JalaliX ParseJalali(string value, IDictionary<string, object?>? options = null)
    {
        var normalized = FromPersian(value).Trim();
        var match = System.Text.RegularExpressions.Regex.Match(
            normalized,
            @"^(\d{4})[/-](\d{1,2})[/-](\d{1,2})(?:[ T](\d{1,2}):(\d{1,2})(?::(\d{1,2}))?)?$");

        if (!match.Success) throw new ArgumentException($"JalaliX.parseJalali: invalid Jalali date \"{value}\"");

        var year = int.Parse(match.Groups[1].Value);
        var month = int.Parse(match.Groups[2].Value);
        var day = int.Parse(match.Groups[3].Value);
        var hours = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : 0;
        var minutes = match.Groups[5].Success ? int.Parse(match.Groups[5].Value) : 0;
        var seconds = match.Groups[6].Success ? int.Parse(match.Groups[6].Value) : 0;

        if (month is < 1 or > 12 || day is < 1 or > 31)
            throw new ArgumentException($"JalaliX.parseJalali: invalid Jalali date \"{value}\"");

        var output = Create(DateTimeOffset.UtcNow, options);
        output.SetFullYear(year, month - 1, day);
        output.SetHours(hours, minutes, seconds, 0);

        return output;
    }

    public static bool IsValidDate(object? date) => date is JalaliX or DateTime or DateTimeOffset;

    public static string ToPersian(string str)
    {
        for (var i = 0; i <= 9; i++)
            str = str.Replace(i.ToString(), PersianNumbers[i]);

        return str;
    }

    public static string FromPersian(string str)
    {
        var output = str;

        for (var i = 0; i <= 9; i++)
            output = output.Replace(PersianNumbers[i], i.ToString());

        return output;
    }

    public static bool Compare(JalaliX date, JalaliX comparing) => date.GetTime() < comparing.GetTime();

    public JalaliX Clone()
    {
        var output = new JalaliX(_ms) { _timezoneId = _timezoneId };

        return output;
    }

    public string GetTimezone() => _timezoneId;

    public int GetWeekOfYear()
    {
        var dayOfYear = GetDayOfYear();
        var dayOfWeek = Clone().GetStartOfYear().GetDayISO();

        return (int)Math.Ceiling((dayOfYear + dayOfWeek - 1) / 7.0);
    }

    public int GetDayOfYear()
    {
        var j = ToJalali();

        if (j.Month < 7) return j.Day + (j.Month - 1) * 31;
        if (j.Month < 12) return j.Day + 186 + (j.Month - 7) * 30;

        return j.Day + 336;
    }

    public int GetDayISO() => ((GetDay() + 6) % 7) + 1;

    public int GetHoursISO()
    {
        var hours = GetHours();

        return hours > 12 ? hours - 12 : hours;
    }

    public string GetMeridiem() => ToJalali().Hours > 12 ? _pm : _am;

    public JalaliX GetStartOfYear() => AddDays(-1 * GetDayOfYear() + 1).GetStartOfDay();
    public JalaliX GetStartOfMonth() => AddDays(-1 * GetDate() + 1).GetStartOfDay();
    public JalaliX GetStartOfWeek() => AddDays(-1 * GetDayISO() + 1).GetStartOfDay();
    public JalaliX GetStartOfDay() { SetHours(0, 0, 0, 0); return this; }

    public JalaliX GetEndOfYear()
    {
        var last = ToJalali().IsLeapYear ? 366 : 365;

        return AddDays(last - GetDayOfYear()).GetEndOfDay();
    }

    public JalaliX GetEndOfMonth()
    {
        var j = ToJalali();
        var last = 31;

        if (j.Month > 6 && j.Month < 12) last = 30;
        else if (j.Month == 12) last = j.IsLeapYear ? 30 : 29;

        return AddDays(last - GetDate()).GetEndOfDay();
    }

    public JalaliX GetEndOfWeek() => AddDays(7 - GetDayISO()).GetEndOfDay();
    public JalaliX GetEndOfDay() { SetHours(23, 59, 59, 999); return this; }

    public JalaliX SetTimezone(string? timezone)
    {
        var output = new JalaliX(_ms) { _timezoneId = NormalizeTimezone(timezone) };

        return output;
    }

    public JalaliX AddYears(int year)
    {
        var j = ToJalali();
        NormalizeJalali(j.Year + year, j.Month, j.Day);
        Reset();

        return this;
    }

    public JalaliX AddMonths(int month)
    {
        var j = ToJalali();
        NormalizeJalali(j.Year, j.Month + month, j.Day);
        Reset();

        return this;
    }

    public JalaliX AddWeeks(int weeks)
    {
        NormalizeDate(weeks * 7);
        Reset();

        return this;
    }

    public JalaliX AddDays(int days)
    {
        NormalizeDate(days);
        Reset();

        return this;
    }

    public string ToISODate(string calendar = "gregorian")
    {
        if (calendar == "jalali") return Format("YYYY-MM-DD");

        var wall = GetWallParts();

        return $"{wall.Year}-{AddZero(wall.Month)}-{AddZero(wall.Day)}";
    }

    public string Format(string str, object? persianOrOptions = null, bool? gregorian = null)
    {
        var persian = false;
        var useGregorian = false;

        if (persianOrOptions is IDictionary<string, object?> dict)
        {
            if (dict.TryGetValue("persianDigits", out var pd) && pd is bool b) persian = b;
            if (dict.TryGetValue("calendar", out var cal) && cal is string cs) useGregorian = cs == "gregorian";
        }
        else if (persianOrOptions is bool pb)
        {
            persian = pb;
            useGregorian = gregorian == true;
        }

        var placeholders = new List<string>();

        string Ph(string val)
        {
            var i = placeholders.Count;
            placeholders.Add(val);
            return $"\uE000{i}\uE001";
        }

        var output = str;
        var wall = GetWallParts();
        var year = useGregorian ? wall.Year : GetFullYear();
        var month = useGregorian ? wall.Month : GetMonth();
        var weekOfYear = useGregorian ? GregorianWeekOfYear() : GetWeekOfYear();
        var dayOfYear = useGregorian ? GregorianDayOfYear() : GetDayOfYear();
        var date = useGregorian ? wall.Day : GetDate();
        var monthName = useGregorian ? _monthsGregorianNames[month - 1] : _monthsNames[month - 1];
        var weekDayNameVal = useGregorian ? _weekDaysGregorianNames[GetWallWeekday()] : WeekDayName();
        var weekDayLetter = useGregorian ? _weekDaysGregorianLetters[GetWallWeekday()] : _weekDaysLetters[(GetDay() + _firstDayOfWeek) % 7];
        var meridiem = useGregorian ? (GetHours() >= 12 ? _pmGregorian : _amGregorian) : GetMeridiem();
        var yearStr = year.ToString();

        void Replace(string token, string val)
        {
            if (output.Contains(token))
                output = output.Replace(token, Ph(val));
        }

        Replace("YYYY", yearStr);
        Replace("YYY", yearStr.Length >= 3 ? yearStr[^3..] : yearStr);
        Replace("YY", yearStr.Length >= 2 ? yearStr[^2..] : yearStr);
        Replace("Y", yearStr[^1..]);
        Replace("MMM", monthName);
        Replace("MM", AddZero(month));
        Replace("M", month.ToString());
        Replace("W", AddZero(weekOfYear));
        Replace("w", weekOfYear.ToString());
        Replace("DW", GetDayISO().ToString());
        Replace("dw", (useGregorian ? (GetWallWeekday() == 0 ? 7 : GetWallWeekday()) : GetDay()).ToString());
        Replace("WN", weekDayNameVal);
        Replace("wn", weekDayNameVal);
        Replace("WL", weekDayLetter);
        Replace("wl", weekDayLetter);
        Replace("DDDD", AddZero(dayOfYear));
        Replace("DDD", dayOfYear.ToString());
        Replace("DD", AddZero(date));
        Replace("D", date.ToString());

        var hours = GetHours();
        Replace("HH", AddZero(hours));
        Replace("H", hours.ToString());
        var hoursIso = hours > 12 ? hours - 12 : (hours == 0 ? 12 : hours);
        Replace("hh", AddZero(hoursIso));
        Replace("h", hoursIso.ToString());
        Replace("mm", AddZero(GetMinutes()));
        Replace("m", GetMinutes().ToString());
        Replace("ss", AddZero(GetSeconds()));
        Replace("s", GetSeconds().ToString());
        Replace("sss", GetMilliseconds().ToString());
        Replace("SSS", GetMilliseconds().ToString());
        Replace("A", meridiem);
        Replace("a", meridiem);
        Replace("X", (_ms / 1000).ToString());
        Replace("x", _ms.ToString());

        for (var i = 0; i < placeholders.Count; i++)
            output = output.Replace($"\uE000{i}\uE001", placeholders[i]);

        return persian ? ToPersian(output) : output;
    }

    private string WeekDayName() => _weekDaysNames[(GetDay() + _firstDayOfWeek) % 7];

    private string UtcTimezoneString()
    {
        var wall = GetWallParts();
        var asUtc = new DateTimeOffset(wall.Year, wall.Month, wall.Day, wall.Hour, wall.Minute, wall.Second, TimeSpan.Zero)
            .ToUnixTimeMilliseconds() + wall.Ms;
        var offsetMin = (int)Math.Round((asUtc - _ms) / 60_000.0);
        var sign = offsetMin >= 0 ? "+" : "-";
        var abs = Math.Abs(offsetMin);

        return $" GMT{sign}{AddZero(abs / 60)}{AddZero(abs % 60)}";
    }

    private int GregorianDayOfYear()
    {
        var wall = GetWallParts();
        var start = new DateTimeOffset(wall.Year, 1, 1, 0, 0, 0, TimeSpan.Zero).AddDays(-1);
        var civil = new DateTimeOffset(wall.Year, wall.Month, wall.Day, 0, 0, 0, TimeSpan.Zero);

        return (int)Math.Round((civil - start).TotalDays);
    }

    private int GregorianWeekOfYear()
    {
        var wall = GetWallParts();
        var d = new DateTime(wall.Year, wall.Month, wall.Day, 0, 0, 0, DateTimeKind.Utc);
        var dow = (int)d.DayOfWeek;

        if (dow == 0) dow = 7;

        d = d.AddDays(4 - dow);
        var yearStart = new DateTime(d.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        return (int)Math.Ceiling((((d - yearStart).TotalDays) + 1) / 7.0);
    }
}
