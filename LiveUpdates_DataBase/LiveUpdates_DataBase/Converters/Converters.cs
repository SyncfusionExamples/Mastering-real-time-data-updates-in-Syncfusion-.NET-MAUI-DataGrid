using System.Globalization;

namespace LiveUpdates_DataBase;

/// <summary>
/// Sign to Color converter
/// </summary>
public sealed class SignToColorConverter : IValueConverter
{
    /// <summary>
    /// Converts a numeric value to a color based on its sign.
    /// </summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
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

    /// <summary>
    /// Convert back to any values.
    /// </summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

/// <summary>
/// Sign to Arrow converter
/// </summary>
public sealed class SignToArrowConverter : IValueConverter
{
    /// <summary>
    /// Converts a numeric value to an arrow symbol based on its sign.
    /// </summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
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

    /// <summary>
    /// Convert back to any values.
    /// </summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
