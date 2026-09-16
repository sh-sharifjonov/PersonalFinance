using System.Globalization;
using PersonalFinance.Domain.Enums;

namespace PersonalFinance.Client.Formatting;

/// <summary>Russian-locale formatting shared across pages (amounts, dates, initials).</summary>
public static class Format
{
    private static readonly NumberFormatInfo AmountFormat = new()
    {
        NumberGroupSeparator = " ",
        NumberDecimalSeparator = ".",
        NumberGroupSizes = new[] { 3 }
    };

    private static readonly string[] MonthsGenitive =
    {
        "января", "февраля", "марта", "апреля", "мая", "июня",
        "июля", "августа", "сентября", "октября", "ноября", "декабря"
    };

    private static readonly string[] MonthsNominative =
    {
        "январь", "февраль", "март", "апрель", "май", "июнь",
        "июль", "август", "сентябрь", "октябрь", "ноябрь", "декабрь"
    };

    private static readonly string[] MonthsShort =
    {
        "янв", "фев", "мар", "апр", "май", "июн",
        "июл", "авг", "сен", "окт", "ноя", "дек"
    };

    private static readonly string[] Weekdays =
    {
        "воскресенье", "понедельник", "вторник", "среда", "четверг", "пятница", "суббота"
    };

    /// <summary>A balance or total: "$12 480.65", "−$42.30". No leading "+" for positive values.</summary>
    public static string Money(decimal value)
    {
        var sign = value < 0 ? "−" : string.Empty;
        return $"{sign}${Math.Abs(value).ToString("N2", AmountFormat)}";
    }

    /// <summary>A transaction amount whose direction always shows: "+$4 200.00" / "−$42.30".</summary>
    public static string SignedAmount(decimal amount, TransactionType type)
    {
        var sign = type == TransactionType.Income ? "+" : "−";
        return $"{sign}${Math.Abs(amount).ToString("N2", AmountFormat)}";
    }

    public static string DayMonth(DateOnly date) => $"{date.Day} {MonthsGenitive[date.Month - 1]}";

    public static string DayMonthWeekday(DateOnly date) =>
        $"{DayMonth(date)}, {Weekdays[(int)date.ToDateTime(TimeOnly.MinValue).DayOfWeek]}";

    public static string ShortMonth(int month) => MonthsShort[month - 1];

    public static string MonthName(int month) => MonthsNominative[month - 1];

    /// <summary>Picks the Russian plural form for a count (e.g. Plural(3, "счёт", "счёта", "счетов")).</summary>
    public static string Plural(int count, string one, string few, string many)
    {
        var mod10 = count % 10;
        var mod100 = count % 100;
        if (mod10 == 1 && mod100 != 11) return one;
        if (mod10 is >= 2 and <= 4 && (mod100 < 10 || mod100 >= 20)) return few;
        return many;
    }

    public static string DateDotted(DateOnly date) => date.ToString("dd.MM.yyyy");

    /// <summary>A Phosphor icon suffix ("house", "coffee", …) for a category, with a safe fallback.</summary>
    public static string CategoryIcon(string? icon) => string.IsNullOrWhiteSpace(icon) ? "tag" : icon;

    public static string Initials(string displayName)
    {
        var parts = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return "?";
        }

        var first = parts[0][0];
        var second = parts.Length > 1 ? parts[1][0] : (parts[0].Length > 1 ? parts[0][1] : parts[0][0]);
        return $"{first}{second}".ToUpperInvariant();
    }
}
