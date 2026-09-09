using System.Globalization;

namespace ClientXMLApp.Services
{
    public static class CalendarDate
    {
        public const string Format = "yyyy-MM-dd";

        public static bool TryParse(string? value, out DateTime date)
        {
            return DateTime.TryParseExact(
                (value ?? string.Empty).Trim(),
                Format,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out date);
        }
    }
}
