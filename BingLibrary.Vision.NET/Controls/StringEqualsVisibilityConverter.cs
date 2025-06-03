﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BingLibrary.Vision.Controls
{
    public class StringEqualsVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            bool equals = value.ToString() == parameter.ToString();
            return equals ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
