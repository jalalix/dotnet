using System.Text.Json;
using Xunit;
using Jx = JalaliX.JalaliX;

namespace JalaliX.Tests;

public class GoldenTests
{
    private static readonly JsonElement Golden = Load();

    private static JsonElement Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "testdata", "golden.json");

        if (!File.Exists(path))
        {
            path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "testdata", "golden.json"));
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var root = doc.RootElement.Clone();

        if (root.GetProperty("version").GetInt32() != 1)
            throw new InvalidOperationException("golden.json version != 1");

        return root;
    }

    private static Jx CreateFrom(JsonElement spec)
    {
        if (spec.TryGetProperty("unixMs", out var unix))
        {
            var d = Jx.Create(unix.GetInt64());

            if (spec.TryGetProperty("timezone", out var tz))
                d = d.SetTimezone(tz.GetString());

            return d;
        }

        return Jx.Create(spec.GetProperty("isoDate").GetString()!);
    }

    private static void Eq<T>(T expected, T actual, string? id)
    {
        Assert.True(
            EqualityComparer<T>.Default.Equals(expected, actual),
            $"{id}: expected {expected}, got {actual}");
    }

    [Fact]
    public void Calendar()
    {
        foreach (var c in Golden.GetProperty("calendar").EnumerateArray())
        {
            var d = CreateFrom(c.GetProperty("create"));
            var expect = c.GetProperty("expect");
            var id = c.GetProperty("id").GetString();
            Eq(expect.GetProperty("formatYYYYMMDD").GetString(), d.Format("YYYY/MM/DD"), id);
            Eq(expect.GetProperty("toISODateGregorian").GetString(), d.ToISODate(), id);
            Eq(expect.GetProperty("toISODateJalali").GetString(), d.ToISODate("jalali"), id);
        }
    }

    [Fact]
    public void Parse()
    {
        foreach (var c in Golden.GetProperty("parse").EnumerateArray())
        {
            var d = Jx.ParseJalali(c.GetProperty("input").GetString()!);
            var expect = c.GetProperty("expect");
            var id = c.GetProperty("id").GetString();
            Eq(expect.GetProperty("toISODateGregorian").GetString(), d.ToISODate(), id);

            if (expect.TryGetProperty("formatYYYYMMDD", out var fmt))
                Eq(fmt.GetString(), d.Format("YYYY/MM/DD"), id);
        }
    }

    [Fact]
    public void Format()
    {
        foreach (var c in Golden.GetProperty("format").EnumerateArray())
        {
            if (c.TryGetProperty("toPersian", out var toPersian))
            {
                Assert.Equal(toPersian.GetProperty("expect").GetString(), Jx.ToPersian(toPersian.GetProperty("input").GetString()!));
                var fromPersian = c.GetProperty("fromPersian");
                Assert.Equal(fromPersian.GetProperty("expect").GetString(), Jx.FromPersian(fromPersian.GetProperty("input").GetString()!));
                continue;
            }

            var d = CreateFrom(c.GetProperty("create"));
            Dictionary<string, object?>? options = null;

            if (c.TryGetProperty("options", out var opts))
            {
                options = new Dictionary<string, object?>();
                if (opts.TryGetProperty("persianDigits", out var pd))
                    options["persianDigits"] = pd.GetBoolean();
                if (opts.TryGetProperty("calendar", out var cal))
                    options["calendar"] = cal.GetString();
            }

            var id = c.GetProperty("id").GetString();
            Eq(c.GetProperty("expect").GetString(), d.Format(c.GetProperty("pattern").GetString()!, options), id);
        }
    }

    [Fact]
    public void Timezone()
    {
        foreach (var c in Golden.GetProperty("timezone").EnumerateArray())
        {
            var id = c.GetProperty("id").GetString();

            if (c.TryGetProperty("setTimezoneAlias", out var alias))
            {
                var d = Jx.Create(c.GetProperty("unixMs").GetInt64()).SetTimezone(alias.GetString());
                Eq(c.GetProperty("expectTimezone").GetString(), d.GetTimezone(), id);
                continue;
            }

            var dt = Jx.Create(c.GetProperty("unixMs").GetInt64()).SetTimezone(c.GetProperty("withTimezone").GetString());
            var expect = c.GetProperty("expect");
            Eq(expect.GetProperty("jalaliSlash").GetString(), dt.Format("YYYY/MM/DD"), id);

            if (expect.TryGetProperty("jalaliIso", out var ji))
                Eq(ji.GetString(), dt.ToISODate("jalali"), id);

            if (expect.TryGetProperty("wallGregorian", out var wg))
                Eq(wg.GetString(), dt.ToISODate(), id);

            if (expect.TryGetProperty("wallTime", out var wt))
                Eq(wt.GetString(), dt.Format("HH:mm:ss"), id);

            Eq(expect.GetProperty("hours").GetInt32(), dt.GetHours(), id);
            Eq(expect.GetProperty("minutes").GetInt32(), dt.GetMinutes(), id);

            if (expect.TryGetProperty("getTime", out var gt))
                Eq(gt.GetInt64(), dt.GetTime(), id);
        }
    }

    [Fact]
    public void Mutations()
    {
        foreach (var c in Golden.GetProperty("mutations").EnumerateArray())
        {
            var d = CreateFrom(c.GetProperty("create"));
            var id = c.GetProperty("id").GetString();

            if (c.TryGetProperty("setFullYear", out var sfy))
            {
                d.SetFullYear(sfy.GetProperty("year").GetInt32(), sfy.GetProperty("month0").GetInt32(), sfy.GetProperty("day").GetInt32());
                var expect = c.GetProperty("expect");
                Eq(expect.GetProperty("getMonth").GetInt32(), d.GetMonth(), id);
                Eq(expect.GetProperty("formatYYYYMMDD").GetString(), d.Format("YYYY/MM/DD"), id);
                continue;
            }

            foreach (var opEl in c.GetProperty("ops").EnumerateArray())
            {
                var op = opEl.GetString()!;
                if (op.StartsWith("addMonths:", StringComparison.Ordinal))
                    d = d.AddMonths(int.Parse(op.Split(':')[1]));
                else if (op == "getStartOfMonth")
                    d = d.GetStartOfMonth();
            }

            Eq(c.GetProperty("expect").GetProperty("formatYYYYMMDD").GetString(), d.Format("YYYY/MM/DD"), id);
        }
    }
}
