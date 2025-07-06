using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data; // IValueConverter を使用するために必要

namespace Among_Us_ModManeger.Converters // 新しい名前空間
{
    /// <summary>
    /// bool値をVisibility (Visible/Collapsed) に変換するコンバーターです。
    /// True は Visible に、False は Collapsed に変換します。
    /// ConverterParameter に 'Inverse' を指定すると、変換が反転します。
    /// </summary>
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = false;
            if (value is bool)
            {
                boolValue = (bool)value;
            }
            else if (value is string stringValue)
            {
                bool.TryParse(stringValue, out boolValue);
            }

            // ConverterParameter が "Inverse" または "inverse" の場合、変換を反転
            if (parameter != null && parameter.ToString().Equals("Inverse", StringComparison.OrdinalIgnoreCase))
            {
                boolValue = !boolValue;
            }

            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // ConvertBack はこのコンバーターでは使用されないため、NotImplementedException をスローします。
            throw new NotImplementedException();
        }
    }
}
