// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using HideMyWindows.App.ViewModels.Pages;
using Wpf.Ui.Controls;

namespace HideMyWindows.App.Views.Pages
{
    public partial class WindowRulesPage : INavigableView<WindowRulesViewModel>
    {
        public WindowRulesViewModel ViewModel { get; }

        public WindowRulesPage(WindowRulesViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
    }
}
