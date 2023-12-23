using HideMyWindows.App.Services.ProcessWatcher;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Wpf.Ui.Appearance;

namespace HideMyWindows.App.Helpers
{
    public class ProcessWatcherTypeEnumToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is not string enumString)
            {
                throw new ArgumentException("ExceptionProcessWatcherTypeEnumToVisibilityConverterParameterMustBeAnEnumName");
            }

            if (!Enum.IsDefined(typeof(ProcessWatcherType), value))
            {
                throw new ArgumentException("ExceptionProcessWatcherTypeEnumToVisibilityConverterValueMustBeAnEnum");
            }

            var enumValue = Enum.Parse(typeof(ProcessWatcherType), enumString);

            return enumValue.Equals(value) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
