// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using HideMyWindows.App.Services.TourService;
using HideMyWindows.App.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace HideMyWindows.App.Views.Pages
{
    public partial class DesktopPreviewPage : INavigableView<DesktopPreviewViewModel>, INavigationAware
    {
        public DesktopPreviewViewModel ViewModel { get; }

        public DesktopPreviewPage(DesktopPreviewViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }

        public async Task OnNavigatedToAsync()
        {
            ViewModel.ResetSelectedMonitor();
            ViewModel.StartCapture();
        }

        public async Task OnNavigatedFromAsync()
        {
            ViewModel.StopCapture();
        }
    }
}
