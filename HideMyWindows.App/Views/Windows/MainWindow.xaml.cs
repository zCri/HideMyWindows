// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using HideMyWindows.App.ViewModels.Windows;
using System.Windows.Controls;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace HideMyWindows.App.Views.Windows
{
    public partial class MainWindow : INavigationWindow
    {
        public MainWindowViewModel ViewModel { get; }

        public MainWindow(
            MainWindowViewModel viewModel,
            INavigationService navigationService,
            INavigationViewPageProvider navigationViewPageProvider,
            ISnackbarService snackbarService,
            IContentDialogService contentDialogService
        )
        {
            SystemThemeWatcher.Watch(this);

            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();

            snackbarService.SetSnackbarPresenter(SnackbarPresenter);
            contentDialogService.SetDialogHost(RootContentDialog);

            navigationService.SetNavigationControl(NavigationView);
            SetPageService(navigationViewPageProvider);

            ViewModel.Initialize();
        }

        #region INavigationWindow methods

        public INavigationView GetNavigation() => NavigationView;

        public bool Navigate(Type pageType) => NavigationView.Navigate(pageType);

        public void SetPageService(INavigationViewPageProvider navigationViewPageProvider) => NavigationView.SetPageProviderService(navigationViewPageProvider);

        public void ShowWindow() => Show();

        public void CloseWindow() => Close();

        public void SetServiceProvider(IServiceProvider serviceProvider)
        {
            throw new NotImplementedException();
        }

        #endregion INavigationWindow methods

        private void ShowMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var window = Application.Current.MainWindow;
            if (window is null) return;

            if (window.Visibility != Visibility.Visible)
                window.Show();

            if (window.WindowState == WindowState.Minimized)
                window.WindowState = WindowState.Normal;

            window.Activate();
            window.Topmost = true;
            window.Topmost = false;
        }

        private void QuitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
