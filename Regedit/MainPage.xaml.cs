using Regedit.RegistryTreeView;
using RegistryRT;
using System;
using System.Linq;
using System.Threading;
using Windows.ApplicationModel.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

#nullable enable

namespace Regedit
{
    public sealed partial class MainPage : Page
    {
        public DataBuilder Data = new();

        public MainPage()
        {
            this.InitializeComponent();

            CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            if (coreTitleBar != null)
            {
                coreTitleBar.ExtendViewIntoTitleBar = true;
                coreTitleBar.LayoutMetricsChanged += CoreTitleBar_LayoutMetricsChanged;
            }

            // Set XAML element as a draggable region.
            Window.Current.SetTitleBar(AppTitleBar);

            ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;

            if (titleBar != null)
            {
                // Set active window colors
                titleBar.BackgroundColor = Windows.UI.Colors.Transparent;
                titleBar.ButtonBackgroundColor = Windows.UI.Colors.Transparent;
                titleBar.ButtonHoverBackgroundColor = Windows.UI.Colors.Transparent;
                titleBar.ButtonPressedBackgroundColor = Windows.UI.Colors.Transparent;

                // Set inactive window colors
                titleBar.InactiveBackgroundColor = Windows.UI.Colors.Transparent;
                titleBar.ButtonInactiveBackgroundColor = Windows.UI.Colors.Transparent;
            }
        }

        private void CoreTitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
        {
            AppTitleBar.Height = sender.Height;
        }

        private string GetRegistryString(RegistryHive hive, string path, string valuename)
        {
            string result = "";
            if (App.registry.QueryValue(hive, path, valuename, out RegistryType vtype, out byte[] buf) && buf != null && buf.Length > 0 && vtype == RegistryType.String)
            {
                result = System.Text.Encoding.Unicode.GetString(buf);
            }
            return result;
        }

        private async void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            string vers = string.Join(".", GetRegistryString(RegistryHive.HKEY_LOCAL_MACHINE, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "BuildLabEx").Split('.').Take(2));
            string osversion = GetRegistryString(RegistryHive.HKEY_LOCAL_MACHINE, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId");
            string osname = GetRegistryString(RegistryHive.HKEY_LOCAL_MACHINE, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName");
            string RegisteredOrganization = GetRegistryString(RegistryHive.HKEY_LOCAL_MACHINE, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "RegisteredOrganization");
            string RegisteredOwner = GetRegistryString(RegistryHive.HKEY_LOCAL_MACHINE, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "RegisteredOwner");

            AboutContentDialog dialog = new(osname, RegisteredOwner, RegisteredOrganization, osversion, vers);
            await dialog.ShowAsync();
        }

        private void RegistryTree_Expanding(Microsoft.UI.Xaml.Controls.TreeView sender, Microsoft.UI.Xaml.Controls.TreeViewExpandingEventArgs args)
        {
            Item item = (Item)args.Item;
            Data.ItemExpanded(item, SynchronizationContext.Current);
        }

        private void RegistryTree_ItemInvoked(Microsoft.UI.Xaml.Controls.TreeView sender, Microsoft.UI.Xaml.Controls.TreeViewItemInvokedEventArgs args)
        {
            Item item = (Item)args.InvokedItem;
            Data.ItemInvoked(item);
        }
    }
}
