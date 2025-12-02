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
    /// Converts numeric values into font icons and formatted text for display in the UI.
    /// Implements <see cref="IValueConverter"/> for data binding in .NET MAUI.
    /// </summary>
    public class ImageConverter : IValueConverter
    {
        private double? data;

        #region IValueConverter implementation

        /// <summary>
        /// Converts a numeric value into either:
        /// - A font icon (up or down arrow) when bound to a Label.
        /// - A formatted numeric string when bound to text.
        /// </summary>
        /// <param name="value">The source value to convert (expected numeric).</param>
        /// <param name="targetType">The target binding type.</param>
        /// <param name="parameter">Optional parameter (Label for styling).</param>
        /// <param name="culture">Culture info for formatting.</param>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Parse numeric value
            this.data = double.Parse(value!.ToString()!.ToCharArray());
            var fontFamily = GetFontFamily();

            if (this.data != null && this.data > 0)
            {
                // Positive value: green color and upward arrow
                if (parameter is Label label)
                {
                    label.FontFamily = fontFamily;
                    label.TextColor = Color.FromRgb(76, 175, 80); // Green
                    return "\ue705"; // Up arrow icon
                }

                return " " + string.Format("{0:0.00}", this.data);
            }
            else
            {
                // Negative or zero: red color and downward arrow
                if (parameter is Label label)
                {
                    label.FontFamily = fontFamily;
                    label.TextColor = Color.FromRgb(239, 83, 80); // Red
                    return "\ue704"; // Down arrow icon
                }

                return string.Format("{0:0.00}", this.data);
            }
        }

        /// <summary>
        /// Converts back to the original numeric value.
        /// </summary>
        /// <param name="value">The converted value.</param>
        /// <param name="targetType">The target type.</param>
        /// <param name="parameter">Optional parameter.</param>
        /// <param name="culture">Culture info.</param>
        /// <returns>The original numeric value.</returns>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return this.data!;
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Gets the font family used for icon rendering.
        /// </summary>
        /// <returns>The font family name.</returns>
        private string GetFontFamily()
        {
            return "MauiSampleFontIcon";
        }

        #endregion
    }
}

