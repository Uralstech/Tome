using System.Globalization;
using System.Windows.Data;

namespace Tome.Converters;

public sealed class SafetyLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? "NSFW" : "SFW";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
