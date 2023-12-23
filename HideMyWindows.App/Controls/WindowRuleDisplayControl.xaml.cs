using HideMyWindows.App.Models;
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
    /// Interaction logic for WindowRuleDisplayControl.xaml
    /// </summary>
    public partial class WindowRuleDisplayControl : UserControl
    {
        public static readonly DependencyProperty WindowRuleProperty = DependencyProperty.Register(
            nameof(WindowRule),
            typeof(WindowRule),
            typeof(WindowRuleDisplayControl),
            new PropertyMetadata()
        );

        public WindowRule? WindowRule
        {
            get => (WindowRule )GetValue(WindowRuleProperty);
            set => SetValue(WindowRuleProperty, value);
        }

        public WindowRuleDisplayControl()
        {
            InitializeComponent();
        }
    }
}
