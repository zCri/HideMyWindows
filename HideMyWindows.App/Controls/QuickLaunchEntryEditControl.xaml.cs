using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HideMyWindows.App.Controls
{
    /// <summary>
    /// Interaction logic for QuickLaunchEntryEditControl.xaml
    /// </summary>
    public partial class QuickLaunchEntryEditControl : UserControl
    {
        public static new readonly DependencyProperty NameProperty = DependencyProperty.Register(
            nameof(Name),
            typeof(string),
            typeof(QuickLaunchEntryEditControl),
            new PropertyMetadata()
        );

        public static readonly DependencyProperty PathProperty = DependencyProperty.Register(
            nameof(Path),
            typeof(string),
            typeof(QuickLaunchEntryEditControl),
            new PropertyMetadata()
        );

        public static readonly DependencyProperty ArgumentsProperty = DependencyProperty.Register(
            nameof(Arguments),
            typeof(string),
            typeof(QuickLaunchEntryEditControl),
            new PropertyMetadata()
        );

        public new string? Name
        {
            get => (string)GetValue(NameProperty);
            set => SetValue(NameProperty, value);
        }

        public string? Path
        {
            get => (string)GetValue(PathProperty);
            set => SetValue(PathProperty, value);
        }

        public string? Arguments
        {
            get => (string)GetValue(ArgumentsProperty);
            set => SetValue(ArgumentsProperty, value);
        }

        public QuickLaunchEntryEditControl()
        {
            InitializeComponent();
        }

        [RelayCommand]
        private void BrowsePath()
        {
            var dialog = new OpenFileDialog()
            {
                Filter = "Executable file|*.exe|All files|*.*",
                FileName = Path,
            };

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                Path = dialog.FileName;
            }
        }
    }
}
