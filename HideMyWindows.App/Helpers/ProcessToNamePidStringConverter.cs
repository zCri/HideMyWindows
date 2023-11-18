using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;

namespace HideMyWindows.App.Helpers
{
    /// <summary>
    /// Not used because there is not a simple way (that I am aware of) to use the converter in the selected text field of the ComboBox element.
    /// Instead, to display the selected process' information to the user I am using <see cref="ProcessProxy"/> as a proxy object of which i override the ToString method.
    /// That said, if anybody has a better fix please make a PR.
    /// </summary>
    public class ProcessToNamePidStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null) return "";
            if (value is not Process process)
            {
                throw new ArgumentException("ExceptionProcessToNamePidStringConverterValueMustBeAProcess");
            }

            return $"{process.ProcessName} ({process.Id})";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
