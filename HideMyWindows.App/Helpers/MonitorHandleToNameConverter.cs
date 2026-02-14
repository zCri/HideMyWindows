using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management;
using System.Windows.Data;
using Vanara.PInvoke;
using static Vanara.PInvoke.Gdi32;
using static Vanara.PInvoke.User32;

namespace HideMyWindows.App.Helpers
{
    public class MonitorHandleToNameConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not HMONITOR handle || handle.IsInvalid)
            {
                return string.Empty;
            }

            var mi = MONITORINFOEX.Default;
            if (!GetMonitorInfo(handle, ref mi))
            {
                return LocalizationUtils.GetString("UnknownMonitor");
            }

            var isPrimary = (mi.dwFlags & MonitorInfoFlags.MONITORINFOF_PRIMARY) != 0;
            var dd = DISPLAY_DEVICE.Default;
            string? monitorId = null;

            if (EnumDisplayDevices(mi.szDevice, 0, ref dd, EDD.EDD_GET_DEVICE_INTERFACE_NAME))
            {
                monitorId = dd.DeviceID;
            }

            var friendlyName = GetFriendlyNameFromWmi(monitorId) ?? dd.DeviceString;
            var displayName = string.IsNullOrWhiteSpace(friendlyName) ? mi.szDevice : friendlyName;

            return isPrimary ? $"{displayName} ({LocalizationUtils.GetString("Primary")})" : displayName;
        }

        private string? GetFriendlyNameFromWmi(string? deviceId)
        {
            if (string.IsNullOrEmpty(deviceId))
            {
                return null;
            }

            try
            {
                using var searcher = new ManagementObjectSearcher(@"Root\WMI", "SELECT * FROM WmiMonitorID");
                using var results = searcher.Get();

                foreach (var mBase in results)
                {
                    if (mBase is not ManagementObject mo) continue;

                    var instanceName = mo["InstanceName"]?.ToString();

                    var normalizedId = deviceId
                        .Replace(@"\\?\", "")
                        .Split('{')[0]
                        .Replace('#', '\\')
                        .TrimEnd('\\')
                        .ToUpper();

                    var idParts = normalizedId.Split('\\');
                    if (idParts.Length < 2 || string.IsNullOrEmpty(instanceName) || !instanceName.Contains(idParts[1]))
                    {
                        continue;
                    }

                    if (mo["UserFriendlyName"] is ushort[] nameArray)
                    {
                        var name = new string(nameArray.Select(u => (char)u).ToArray()).TrimEnd('\0');
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            return name;
                        }
                    }
                }
            }
            catch
            {
                // Ignored
            }

            return null;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}