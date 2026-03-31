using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using System;

namespace AmongUsModManager.Models
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
            => (value is bool b && b) ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => value is Visibility v && v == Visibility.Visible;
    }

    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
            => (value is bool b && !b) ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => value is Visibility v && v == Visibility.Collapsed;
    }

    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
            => !string.IsNullOrEmpty(value as string) ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => null!;
    }


    public class BoolToBlackBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush Black       = new(Colors.Black);
        private static readonly SolidColorBrush Transparent = new(Colors.Transparent);
        public object Convert(object value, Type targetType, object parameter, string language)
            => (value is bool b && b) ? Black : Transparent;
        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }

    public class BoolToForegroundConverter : IValueConverter
    {
        private static readonly SolidColorBrush AccentBrush = new(Colors.SeaGreen);
        private static readonly SolidColorBrush TransparentBrush = new(Colors.Transparent);
        public object Convert(object value, Type targetType, object parameter, string language)
            => (value is bool b && b) ? AccentBrush : TransparentBrush;
        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}
