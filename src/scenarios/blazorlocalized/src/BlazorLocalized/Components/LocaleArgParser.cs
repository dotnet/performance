using System.Globalization;

namespace BlazorLocalized.Components
{
    public class LocaleArgParser
    {
        public static CultureInfo ToCultureInfo(string? locale) => 
            (locale == null || locale == "invariant") ? CultureInfo.InvariantCulture : new CultureInfo(locale);
    }
}
