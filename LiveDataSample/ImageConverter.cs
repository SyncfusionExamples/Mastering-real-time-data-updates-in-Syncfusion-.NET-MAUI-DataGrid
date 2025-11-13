using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveDataSample
{
    public class ImageConverter : IValueConverter
    {
        private double? data;

        #region IValueConverter implementation
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            this.data = double.Parse(value!.ToString()!.ToCharArray());
            var fontFamily = GetFontFamily();
            if (this.data != null && this.data > 0)
            {
                if (parameter is Label)
                {
                    var label = parameter as Label;
                    label!.FontFamily = fontFamily;
                    label!.TextColor = Color.FromRgb(76, 175, 80);
                    return "\ue705";
                }

                return " " + string.Format("{0:0.00}", this.data);
            }
            else
            {
                if (parameter is Label)
                {
                    var label = parameter as Label;
                    label!.FontFamily = fontFamily;
                    label!.TextColor = Color.FromRgb(239, 83, 80);
                    return "\ue704";
                }

                return string.Format("{0:0.00}", this.data);
            }
        }
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return this.data!;
        }

        #endregion

        #region Internal Methods
        private string GetFontFamily()
        {
            return "MauiSampleFontIcon";
        }
        #endregion
    }
}
