using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace DataGridLiveDataUpdateSample
{
    /// <summary>
    /// Converts a numeric value into a color for text foreground based on its threshold.
    /// Implements <see cref="IValueConverter"/> for data binding in .NET MAUI.
    /// </summary>
    internal class TextForegroundConverter : IValueConverter
    {
        /// <summary>
        /// Converts a numeric value to a color:
        /// - Green if the value is greater than 10.
        /// - Red if the value is less than or equal to 10.
        /// </summary>
        /// <param name="value">The source value (expected as double).</param>
        /// <param name="targetType">The target type for conversion.</param>
        /// <param name="parameter">Optional parameter for customization.</param>
        /// <param name="info">Culture information for conversion.</param>
        /// <returns>A <see cref="Color"/> representing the foreground color.</returns>
        object? IValueConverter.Convert(object? value, Type targetType, object? parameter, CultureInfo info)
        {
            var data = value as double?;
            if (data != null && data > 10)
            {
                return Colors.Green; // Positive threshold
            }
            else
            {
                return Colors.Red; // Negative or low value
            }
        }

        /// <summary>
        /// ConvertBack is not implemented as this converter is one-way.
        /// </summary>
        /// <param name="value">The converted value.</param>
        /// <param name="targetType">The target type.</param>
        /// <param name="parameter">Optional parameter.</param>
        /// <param name="info">Culture information.</param>
        /// <returns>Throws <see cref="NotImplementedException"/>.</returns>
        object? IValueConverter.ConvertBack(object? value, Type targetType, object? parameter, CultureInfo info)
        {
            throw new NotImplementedException();
        }
    }
}
