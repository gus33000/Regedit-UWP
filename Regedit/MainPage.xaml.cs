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
    }
}
