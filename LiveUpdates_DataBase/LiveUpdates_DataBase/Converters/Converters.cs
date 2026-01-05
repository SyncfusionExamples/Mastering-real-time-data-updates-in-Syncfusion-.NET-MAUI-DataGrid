using System.Globalization;

namespace LiveUpdates_DataBase;

public sealed class SignToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null) return Colors.Gray;
        if (value is IConvertible c)
        {
            try
            {
                var d = System.Convert.ToDouble(c, CultureInfo.InvariantCulture);
                if (d > 0) return Colors.ForestGreen;
                if (d < 0) return Colors.IndianRed;
                return Colors.Gray;
            }
            catch { }
        }
        return Colors.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public sealed class SignToArrowConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null) return string.Empty;
        if (value is IConvertible c)
        {
            try
            {
                var d = System.Convert.ToDouble(c, CultureInfo.InvariantCulture);
                if (d > 0) return "▲"; // up triangle
                if (d < 0) return "▼"; // down triangle
                return string.Empty;
            }
            catch { }
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
