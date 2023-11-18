using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HideMyWindows.App.Helpers
{
    /// <summary>
    /// This class is used to display appropriate information in the <see cref="ComboBox"/> in <see cref="DashboardPage"/>.
    /// I initially wanted to use <see cref="ProcessToNamePidStringConverter"/> but there is no easy way that I am aware of to apply the converter to the selected text field of the element.
    /// If anybody has a better fix please make a PR.
    /// </summary>
    public class ProcessProxy
    {
        public Process Process { get; set; }
        private static ProcessToNamePidStringConverter ProcessToNamePidStringConverter { get; } = new();

        public ProcessProxy(Process process)
        {
            Process = process;
        }

        public override string ToString()
        {
            return ProcessToNamePidStringConverter.Convert(Process, typeof(string), null!, CultureInfo.CurrentCulture) as string ?? "";
        }
    }
}
