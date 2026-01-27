using System.Globalization;

namespace LiveUpdates_DataBase;

/// <summary>
/// Sign to Color converter
/// </summary>
public sealed class SignToColorConverter : IValueConverter
{
    /// <summary>
    /// Converts a numeric converter to a color based on its sign.
    /// </summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return Colors.Gray;
        }

        if (value is IConvertible convert)
        {
            var converter = System.Convert.ToDouble(convert, CultureInfo.InvariantCulture);
            if (converter > 0)
            {
                return Colors.ForestGreen;
            }
            else if (converter < 0)
            {
                return Colors.IndianRed;
            }
            else
            {
                return Colors.Gray;
            }
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
    /// Converts a numeric converter to an arrow symbol based on its sign.
    /// </summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return string.Empty;
        }

        if (value is IConvertible convert)
        {
            var converter = System.Convert.ToDouble(convert, CultureInfo.InvariantCulture);
            if (converter > 0)
            {
                return "▲";
            }
            else if (converter < 0)
            {
                return "▼";
            }
            else
            {
                return string.Empty;
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Convert back to any values.
    /// </summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}