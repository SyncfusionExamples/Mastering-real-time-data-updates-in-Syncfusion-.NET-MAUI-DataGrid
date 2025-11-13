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
    internal class TextForegroundConverter : IValueConverter
    {        
        object? IValueConverter.Convert(object? value, Type targetType, object? parameter, CultureInfo info)
        {
            var data = value as double?;
            if (data != null && data > 10)
            {
                return Colors.Green;
            }
            else
            {
                return Colors.Red;
            }
        }
        object? IValueConverter.ConvertBack(object? value, Type targetType, object? parameter, CultureInfo info)
        {
            throw new NotImplementedException();
        }
    }
}
