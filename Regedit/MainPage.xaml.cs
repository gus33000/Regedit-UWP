using RegistryRT;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

#nullable enable

namespace Regedit
{
    public sealed partial class MainPage : Page
    {
        private readonly ObservableCollection<Item> DataSource = new();

        private readonly Registry registry = new();

        private readonly string folderImageSource = "ms-appx:///Assets/folder.png";
        private readonly string computerImageSource = "ms-appx:///Assets/computer.png";
        private readonly string numbersImageSource = "ms-appx:///Assets/numbers.png";
        private readonly string textImageSource = "ms-appx:///Assets/text.png";

        public MainPage()
        {
            this.InitializeComponent();
            registry.InitNTDLLEntryPoints();

            GetRegistryData();
            //DataSource = GetRegistryData();

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

        private void GetRegistryData()
        {
            ObservableCollection<Item> list = new()
            {
                new Item()
                {
                    Name = "HKEY_CLASSES_ROOT",
                    Image = folderImageSource,
                    Hive = RegistryHive.HKEY_CLASSES_ROOT,
                    Path = ""
                },

                new Item()
                {
                    Name = "HKEY_CURRENT_USER",
                    Image = folderImageSource,
                    Hive = RegistryHive.HKEY_CURRENT_USER,
                    Path = ""
                },

                new Item()
                {
                    Name = "HKEY_LOCAL_MACHINE",
                    Image = folderImageSource,
                    Hive = RegistryHive.HKEY_LOCAL_MACHINE,
                    Path = ""
                },

                new Item()
                {
                    Name = "HKEY_USERS",
                    Image = folderImageSource,
                    Hive = RegistryHive.HKEY_USERS,
                    Path = ""
                },

                new Item()
                {
                    Name = "HKEY_CURRENT_CONFIG",
                    Image = folderImageSource,
                    Hive = RegistryHive.HKEY_CURRENT_CONFIG,
                    Path = ""
                }
            };

            foreach (Item element in list)
            {
                try
                {
                    registry.GetSubKeyList(element.Hive, element.Path, out string[] keys);
                    foreach (string key in keys)
                    {
                        element.Children.Add(new Item()
                        {
                            Name = key,
                            Hive = element.Hive,
                            Image = folderImageSource,
                            Path = element.Path + key + "\\"
                        });
                    }
                }
                catch
                {

                }
            }


            DataSource.Add(new Item()
            {
                Name = "Computer",
                Children = list,
                Image = computerImageSource,
                Expanded = true
            });
        }

        private void RegistryTree_Expanding(Microsoft.UI.Xaml.Controls.TreeView sender, Microsoft.UI.Xaml.Controls.TreeViewExpandingEventArgs args)
        {
            Item selectedItem = (Item)args.Item;

            _ = Windows.System.Threading.ThreadPool.RunAsync(async (source) =>
            {
                foreach (Item element in selectedItem.Children)
                {
                    if (element.Children.Count != 0)
                        break;

                    registry.GetSubKeyList(element.Hive, element.Path, out string[] keys);

                    if (keys == null)
                        continue;

                    foreach (string key in keys)
                    {
                        Item item = new()
                        {
                            Name = key,
                            Hive = element.Hive,
                            Image = folderImageSource,
                            Path = element.Path + key + "\\"
                        };

                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            element.Children.Add(item);
                        });
                    }
                }
            });
        }

        private void RegistryTree_ItemInvoked(Microsoft.UI.Xaml.Controls.TreeView sender, Microsoft.UI.Xaml.Controls.TreeViewItemInvokedEventArgs args)
        {
            Item item = args.InvokedItem as Item;

            if (item.Name == "Computer" && string.IsNullOrEmpty(item.Path))
            {
                PathTextBox.Text = item.Name;
                ValueListView.ItemsSource = null;
            }
            else
            {
                if (string.IsNullOrEmpty(item.Path))
                {
                    PathTextBox.Text = "Computer\\" + item.Hive.ToString();
                }
                else
                {
                    PathTextBox.Text = "Computer\\" + item.Hive.ToString() + "\\" + item.Path.TrimEnd('\\');
                }

                List<ValueItem> valueItems = new();

                RegistryType dtype = RegistryType.String;
                byte[] dbuf = System.Text.Encoding.Unicode.GetBytes("(value not set)");

                try
                {
                    registry.QueryValue(item.Hive, item.Path, null, out dtype, out dbuf);
                }
                catch { };

                string ddatastr = "(value not set)";

                if (dtype == RegistryType.String)
                {
                    ddatastr = System.Text.Encoding.Unicode.GetString(dbuf);
                }
                else if (dtype == RegistryType.Integer)
                {
                    ddatastr = BitConverter.ToInt32(dbuf, 0).ToString();
                }
                else if (dtype == RegistryType.Long)
                {
                    ddatastr = BitConverter.ToInt64(dbuf, 0).ToString();
                }
                else
                {
                    ddatastr = BitConverter.ToString(dbuf);
                }

                valueItems.Add(new ValueItem()
                {
                    Name = "(Default)",
                    Type = dtype.ToString(),
                    Data = ddatastr,
                    Image = textImageSource,
                    ParentItem = item
                });

                registry.GetValueList(item.Hive, item.Path, out string[] list);

                if (list != null && list.Length != 0)
                {
                    valueItems.AddRange(list.ToList().OrderBy(x => x).Select(x =>
                    {
                        RegistryType vtype = registry.GetValueInfo(item.Hive, item.Path, x, 0);

                        ValueItem vitem = new()
                        {
                            Name = x,
                            Type = vtype.ToString(),
                            ParentItem = item
                        };

                        registry.QueryValue(item.Hive, item.Path, x, out vtype, out byte[] buf);

                        if (vtype == RegistryType.String)
                        {
                            vitem.Data = System.Text.Encoding.Unicode.GetString(buf);
                            vitem.Image = textImageSource;
                        }
                        else if (vtype == RegistryType.Integer)
                        {
                            vitem.Data = BitConverter.ToInt32(buf, 0).ToString();
                            vitem.Image = numbersImageSource;
                        }
                        else if (vtype == RegistryType.Long)
                        {
                            vitem.Data = BitConverter.ToInt64(buf, 0).ToString();
                            vitem.Image = numbersImageSource;
                        }
                        else
                        {
                            vitem.Data = BitConverter.ToString(buf);
                            vitem.Image = numbersImageSource;
                        }

                        return vitem;
                    }));
                }

                ValueListView.ItemsSource = valueItems;
            }
        }

        private string GetRegistryString(RegistryHive hive, string path, string valuename)
        {
            string result = "";
            if (registry.QueryValue(hive, path, valuename, out RegistryType vtype, out byte[] buf) && buf != null && buf.Length > 0 && vtype == RegistryType.String)
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

    public class Item
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public bool Expanded { get; set; }
        public RegistryHive Hive { get; set; }

        public string Image { get; set; }
        public ObservableCollection<Item> Children { get; set; } = new ObservableCollection<Item>();

        public override string ToString()
        {
            return Name;
        }
    }

    public class ValueItem
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Data { get; set; }
        public string Image { get; set; }
        public Item ParentItem { get; set; }
    }
}
