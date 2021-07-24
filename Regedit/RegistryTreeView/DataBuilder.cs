using RegistryRT;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;

namespace Regedit.RegistryTreeView
{
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

    public class DataBuilder : INotifyPropertyChanged
    {
        public readonly ObservableCollection<Item> DataSource = new();
        public readonly ObservableCollection<ValueItem> ValueSource = new();

        private string _Path = "";
        public string Path
        {
            get => _Path;
            set
            {
                if (_Path != value)
                {
                    _Path = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Path)));
                }
            }
        }

        private readonly string folderImageSource = "ms-appx:///Assets/folder.png";
        private readonly string computerImageSource = "ms-appx:///Assets/computer.png";
        private readonly string numbersImageSource = "ms-appx:///Assets/numbers.png";
        private readonly string textImageSource = "ms-appx:///Assets/text.png";

        public event PropertyChangedEventHandler PropertyChanged;

        public DataBuilder()
        {
            GetRegistryData();
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
                    App.registry.GetSubKeyList(element.Hive, element.Path, out string[] keys);
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

        public void ItemExpanded(Item selectedItem, SynchronizationContext synccontext)
        {
            _ = Windows.System.Threading.ThreadPool.RunAsync(async (source) =>
            {
                foreach (Item element in selectedItem.Children)
                {
                    if (element.Children.Count != 0)
                        break;

                    App.registry.GetSubKeyList(element.Hive, element.Path, out string[] keys);

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

                        synccontext.Post(s =>
                        {
                            element.Children.Add(item);
                        }, this);
                    }
                }
            });
        }

        public void ItemInvoked(Item item)
        {
            if (item.Name == "Computer" && string.IsNullOrEmpty(item.Path))
            {
                Path = item.Name;
                ValueSource.Clear();
            }
            else
            {
                if (string.IsNullOrEmpty(item.Path))
                {
                    Path = "Computer\\" + item.Hive.ToString();
                }
                else
                {
                    Path = "Computer\\" + item.Hive.ToString() + "\\" + item.Path.TrimEnd('\\');
                }

                RegistryType dtype = RegistryType.String;
                byte[] dbuf = System.Text.Encoding.Unicode.GetBytes("(value not set)");

                try
                {
                    App.registry.QueryValue(item.Hive, item.Path, null, out dtype, out dbuf);
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

                ValueSource.Add(new ValueItem()
                {
                    Name = "(Default)",
                    Type = dtype.ToString(),
                    Data = ddatastr,
                    Image = textImageSource,
                    ParentItem = item
                });

                App.registry.GetValueList(item.Hive, item.Path, out string[] list);

                if (list != null && list.Length != 0)
                {
                    var valuerange = (list.ToList().OrderBy(x => x).Select(x =>
                    {
                        RegistryType vtype = App.registry.GetValueInfo(item.Hive, item.Path, x, 0);

                        ValueItem vitem = new()
                        {
                            Name = x,
                            Type = vtype.ToString(),
                            ParentItem = item
                        };

                        App.registry.QueryValue(item.Hive, item.Path, x, out vtype, out byte[] buf);

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

                    foreach (var value in valuerange)
                        ValueSource.Add(value);
                }
            }
        }
    }
}
