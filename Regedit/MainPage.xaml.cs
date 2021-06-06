using System;

using System.Collections.Generic;
using System.Collections.ObjectModel;

using System.Linq;

using Windows.ApplicationModel.Core;

using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

using RegistryRT;

namespace Regedit
{
    public sealed partial class MainPage : Page
    {
        private ObservableCollection<Item> DataSource = new ObservableCollection<Item>();

        private Registry registry = new Registry();

        private ImageSource folderImageSource;
        private ImageSource computerImageSource;
        private ImageSource numbersImageSource;
        private ImageSource textImageSource;

        public MainPage()
        {
            this.InitializeComponent();
            registry.InitNTDLLEntryPoints();

            folderImageSource = GetImage("folder.png");
            computerImageSource = GetImage("computer.png");
            numbersImageSource = GetImage("numbers.png");
            textImageSource = GetImage("text.png");

            DataSource = GetRegistryData();

            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;
            coreTitleBar.LayoutMetricsChanged += CoreTitleBar_LayoutMetricsChanged;
            // Set XAML element as a draggable region.
            Window.Current.SetTitleBar(AppTitleBar);

            var titleBar = ApplicationView.GetForCurrentView().TitleBar;

            // Set active window colors
            titleBar.BackgroundColor = Windows.UI.Colors.Transparent;
            titleBar.ButtonBackgroundColor = Windows.UI.Colors.Transparent;
            titleBar.ButtonHoverBackgroundColor = Windows.UI.Colors.Transparent;
            titleBar.ButtonPressedBackgroundColor = Windows.UI.Colors.Transparent;

            // Set inactive window colors
            titleBar.InactiveBackgroundColor = Windows.UI.Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Windows.UI.Colors.Transparent;
        }

        private ImageSource GetImage(string ImageSource)
        {
            var bitmap = new BitmapImage();
            var f = Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/" + ImageSource));
            f.AsTask().Wait();
            var o = f.AsTask().Result.OpenAsync(Windows.Storage.FileAccessMode.Read);
            o.AsTask().Wait();
            bitmap.SetSource(o.AsTask().Result);
            return bitmap;
        }

        private void CoreTitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
        {
            AppTitleBar.Height = sender.Height;
        }

        private ObservableCollection<Item> GetRegistryData()
        {
            var list = new ObservableCollection<Item>();

            list.Add(new Item()
            {
                Name = "HKEY_CLASSES_ROOT",
                Image = folderImageSource,
                Hive = RegistryHive.HKEY_CLASSES_ROOT,
                Path = ""
            });

            list.Add(new Item()
            {
                Name = "HKEY_CURRENT_USER",
                Image = folderImageSource,
                Hive = RegistryHive.HKEY_CURRENT_USER,
                Path = ""
            });

            list.Add(new Item()
            {
                Name = "HKEY_LOCAL_MACHINE",
                Image = folderImageSource,
                Hive = RegistryHive.HKEY_LOCAL_MACHINE,
                Path = ""
            });

            list.Add(new Item()
            {
                Name = "HKEY_USERS",
                Image = folderImageSource,
                Hive = RegistryHive.HKEY_USERS,
                Path = ""
            });

            list.Add(new Item()
            {
                Name = "HKEY_CURRENT_CONFIG",
                Image = folderImageSource,
                Hive = RegistryHive.HKEY_CURRENT_CONFIG,
                Path = ""
            });

            foreach (var element in list)
            {
                try
                {
                    string[] keys;
                    registry.GetSubKeyList(element.Hive, element.Path, out keys);
                    foreach (var key in keys)
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


            var root = new Item()
            {
                Name = "Computer",
                Children = list,
                Image = computerImageSource,
                Expanded = true
            };

            return new ObservableCollection<Item> { root };
        }

        private void RegistryTree_Expanding(TreeView sender, TreeViewExpandingEventArgs args)
        {
            var selectedItem = (Item)args.Item;

            _ = Windows.System.Threading.ThreadPool.RunAsync(async (source) =>
            {
                foreach (var element in selectedItem.Children)
                {
                    if (element.Children.Count != 0)
                        break;

                    string[] keys;
                    registry.GetSubKeyList(element.Hive, element.Path, out keys);

                    if (keys == null)
                        continue;

                    foreach (var key in keys)
                    {
                        var item = new Item()
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

        private void RegistryTree_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            var item = args.InvokedItem as Item;

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

                List<ValueItem> valueItems = new List<ValueItem>();

                RegistryType dtype = RegistryType.String;
                byte[] dbuf = System.Text.Encoding.Unicode.GetBytes("(value not set)");

                try
                {
                    registry.QueryValue(item.Hive, item.Path, null, out dtype, out dbuf);
                } catch { };

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

                string[] list;
                registry.GetValueList(item.Hive, item.Path, out list);

                if (list != null && list.Length != 0)
                {
                    valueItems.AddRange(list.ToList().OrderBy(x => x).Select(x =>
                    {
                        var vtype = registry.GetValueInfo(item.Hive, item.Path, x, 0);

                        var vitem = new ValueItem()
                        {
                            Name = x,
                            Type = vtype.ToString(),
                            ParentItem = item
                        };

                        byte[] buf;
                        registry.QueryValue(item.Hive, item.Path, x, out vtype, out buf);

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

        private async void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            byte[] buf;
            uint vtype;
            registry.QueryValue(RegistryHive.HKEY_LOCAL_MACHINE, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "BuildLabEx", out vtype, out buf);
            var vers = string.Join(".", System.Text.Encoding.Unicode.GetString(buf).Split(".").Take(2));
            registry.QueryValue(RegistryHive.HKEY_LOCAL_MACHINE, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId", out vtype, out buf);
            var osversion = System.Text.Encoding.Unicode.GetString(buf);
            registry.QueryValue(RegistryHive.HKEY_LOCAL_MACHINE, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName", out vtype, out buf);
            var osname = System.Text.Encoding.Unicode.GetString(buf);
            registry.QueryValue(RegistryHive.HKEY_LOCAL_MACHINE, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "RegisteredOrganization", out vtype, out buf);
            var RegisteredOrganization = "";
            if (buf != null && buf.Length > 0)
            {
                RegisteredOrganization = System.Text.Encoding.Unicode.GetString(buf);
            }
            registry.QueryValue(RegistryHive.HKEY_LOCAL_MACHINE, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "RegisteredOwner", out vtype, out buf);
            var RegisteredOwner = System.Text.Encoding.Unicode.GetString(buf);
            AboutContentDialog dialog = new AboutContentDialog(osname, RegisteredOwner, RegisteredOrganization, osversion, vers);
            await dialog.ShowAsync();
        }
    }

    public class Item
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public bool Expanded { get; set; }
        public RegistryHive Hive { get; set; }

        public ImageSource Image { get; set; }
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
        public ImageSource Image { get; set; }
        public Item ParentItem { get; set; }
    }
}
