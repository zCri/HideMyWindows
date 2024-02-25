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
    /// Interaction logic for WindowsVersionIncompatibleDialog.xaml
    /// </summary>
    public partial class WindowsVersionIncompatibleDialog : UserControl
    {
        public static readonly DependencyProperty AcknowledgedProperty = DependencyProperty.Register(
            nameof(Acknowledged),
            typeof(bool),
            typeof(WindowsVersionIncompatibleDialog),
            new PropertyMetadata()
        );

        public bool? Acknowledged
        {
            get => (bool)GetValue(AcknowledgedProperty);
            set => SetValue(AcknowledgedProperty, value);
        }

        public WindowsVersionIncompatibleDialog()
        {
            InitializeComponent();
        }
    }
}
