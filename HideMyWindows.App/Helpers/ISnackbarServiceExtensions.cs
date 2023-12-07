using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wpf.Ui.Controls;

namespace HideMyWindows.App.Helpers
{
    public static class ISnackbarServiceExtensions
    {
        public static void Show(this ISnackbarService snackbarService, string title, string message, ControlAppearance appearance, IconElement? icon)
        {
            snackbarService.Show(title, message, appearance, icon, snackbarService.DefaultTimeOut);
        }
    }
}
