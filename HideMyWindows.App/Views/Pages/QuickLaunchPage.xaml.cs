using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using HideMyWindows.App.ViewModels.Pages;
using Wpf.Ui.Controls;

namespace HideMyWindows.App.Views.Pages
{
    public partial class QuickLaunchPage : INavigableView<QuickLaunchViewModel>
    {
        public QuickLaunchViewModel ViewModel { get; }

        public QuickLaunchPage(QuickLaunchViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
    }
}
