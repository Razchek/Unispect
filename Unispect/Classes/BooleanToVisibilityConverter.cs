using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Unispect
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var invert = false;

            if (parameter != null)
                invert = bool.Parse(parameter.ToString());

            var booleanValue = value != null && (bool)value;

            return ((booleanValue && !invert) || (!booleanValue && invert))
                ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}