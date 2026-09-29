using System.Globalization;
using PersonalFinance.Domain.Enums;

namespace PersonalFinance.Client.Formatting;

// Russian month/day names are hardcoded on purpose: Blazor WASM only ships the ICU
// shard matching the browser's language, so CultureInfo("ru-RU") can't be relied on.
public static class Fmt
{
    private const char Minus = '−';

    private static readonly NumberFormatInfo Numbers = new()
    {
        NumberDecimalSeparator = ".",
        NumberGroupSeparator = " ",
        NumberGroupSizes = new[] { 3 }
    };

    private static readonly string[] MonthsNominative =
        { "Январь", "Февраль", "Март", "Апрель", "Май", "Июнь", "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь" };

    private static readonly string[] MonthsGenitive =
        { "января", "февраля", "марта", "апреля", "мая", "июня", "июля", "августа", "сентября", "октября", "ноября", "декабря" };

    private static readonly string[] MonthsShort =
        { "янв.", "февр.", "мар.", "апр.", "мая", "июн.", "июл.", "авг.", "сент.", "окт.", "нояб.", "дек." };

    private static readonly string[] MonthsAxis =
        { "ЯНВ", "ФЕВ", "МАР", "АПР", "МАЙ", "ИЮН", "ИЮЛ", "АВГ", "СЕН", "ОКТ", "НОЯ", "ДЕК" };

    private static readonly string[] Weekdays =
        { "воскресенье", "понедельник", "вторник", "среда", "четверг", "пятница", "суббота" };

    private static readonly Dictionary<string, string> PrefixSymbols = new(StringComparer.OrdinalIgnoreCase)
    {
        ["USD"] = "$",
        ["EUR"] = "€",
        ["GBP"] = "£"
    };

    private static readonly Dictionary<string, string> SuffixSymbols = new(StringComparer.OrdinalIgnoreCase)
    {
        ["RUB"] = "₽",
        ["UZS"] = "сум",
        ["KZT"] = "₸"
    };

    /// <summary>"$12 480.65", "−$42.30", "1 200.00 сум".</summary>
    public static string Money(decimal amount, string currency, bool cents = true)
    {
        var number = Math.Abs(amount).ToString(cents ? "N2" : "N0", Numbers);
        var sign = amount < 0 ? Minus.ToString() : string.Empty;
        return sign + WithSymbol(number, currency);
    }

    /// <summary>"+$3 200.00" for income, "−$42.30" for expense.</summary>
    public static string Signed(decimal amount, TransactionType type, string currency) =>
        (type == TransactionType.Income ? "+" : Minus.ToString()) + Money(Math.Abs(amount), currency);

    /// <summary>Explicit sign for a delta: "+$2 263.60" / "−$120.00".</summary>
    public static string Delta(decimal amount, string currency) =>
        (amount >= 0 ? "+" : Minus.ToString()) + Money(Math.Abs(amount), currency);

    /// <summary>Whole part with symbol for a KPI value, e.g. "$12 480" — pair with <see cref="Cents"/> in a sup.</summary>
    public static string Whole(decimal amount, string currency)
    {
        var whole = Math.Truncate(Math.Abs(amount)).ToString("N0", Numbers);
        var sign = amount < 0 ? Minus.ToString() : string.Empty;
        return sign + (PrefixSymbols.TryGetValue(currency, out var prefix) ? prefix + whole : whole);
    }

    public static string Cents(decimal amount, string currency)
    {
        var cents = ((int)Math.Round((Math.Abs(amount) - Math.Truncate(Math.Abs(amount))) * 100m)) % 100;
        var suffix = PrefixSymbols.ContainsKey(currency) ? string.Empty : " " + Symbol(currency);
        return "." + cents.ToString("00", CultureInfo.InvariantCulture) + suffix;
    }

    public static string Percent(decimal value, int decimals = 1) =>
        value.ToString("F" + decimals, CultureInfo.InvariantCulture) + "%";

    public static string SignedPercent(decimal value) =>
        (value >= 0 ? "+" : Minus.ToString()) + Percent(Math.Abs(value));

    public static string Symbol(string currency) =>
        PrefixSymbols.TryGetValue(currency, out var prefix) ? prefix
        : SuffixSymbols.TryGetValue(currency, out var suffix) ? suffix
        : currency.ToUpperInvariant();

    private static string WithSymbol(string number, string currency) =>
        PrefixSymbols.TryGetValue(currency, out var prefix) ? prefix + number : number + " " + Symbol(currency);

    public static string MonthYear(int year, int month) => $"{MonthsNominative[month - 1]} {year}";

    public static string MonthName(int month) => MonthsNominative[month - 1];

    public static string MonthAxis(int month) => MonthsAxis[month - 1];

    /// <summary>"15 сент."</summary>
    public static string ShortDate(DateOnly date) => $"{date.Day} {MonthsShort[date.Month - 1]}";

    /// <summary>"15 сентября"</summary>
    public static string DayMonth(DateOnly date) => $"{date.Day} {MonthsGenitive[date.Month - 1]}";

    /// <summary>"15 сентября, вторник" (year added when it isn't the current one).</summary>
    public static string DayHeading(DateOnly date, DateOnly today)
    {
        var text = DayMonth(date);
        if (date.Year != today.Year)
        {
            text += " " + date.Year;
        }

        return text + ", " + Weekdays[(int)date.DayOfWeek];
    }

    public static string Plural(int count, string one, string few, string many)
    {
        var mod100 = count % 100;
        var mod10 = count % 10;
        var word = mod100 is >= 11 and <= 14 ? many
            : mod10 == 1 ? one
            : mod10 is >= 2 and <= 4 ? few
            : many;
        return $"{count} {word}";
    }

    public static string Operations(int count) => Plural(count, "операция", "операции", "операций");

    public static string Initials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "?";
        }

        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 1
            ? parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant()
            : $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[1][0])}";
    }

    public static string Greeting(DateTime now) => now.Hour switch
    {
        < 5 => "Доброй ночи",
        < 12 => "Доброе утро",
        < 18 => "Добрый день",
        _ => "Добрый вечер"
    };
}
