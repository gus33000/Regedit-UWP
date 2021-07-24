using System.Threading;
using System.Xml.Linq;
using TreeViewControl;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Regedit.RegistryTreeView
{
    public sealed partial class UniversalTreeView : UserControl
    {
        public UniversalTreeView()
        {
            this.InitializeComponent();
        }

        private void OnDataChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is DataBuilder Data)
            {
                if (Data.DataSource.Count > 0)
                {
                    foreach (Item item in Data.DataSource)
                    {
                        TreeView.RootNode.Add(CreateTreeNode(item));
                    }
                }

                Data.DataSource.CollectionChanged += DataSource_CollectionChanged;
            }
        }

        private void DataSource_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            foreach (Item item in e.NewItems)
            {
                TreeView.RootNode.Add(CreateTreeNode(item));
            }
        }

        private TreeNode CreateTreeNode(Item item)
        {
            TreeNode node = new TreeNode()
            {
                Data = item,
                IsExpanded = item.Expanded
            };

            /*item.PropertyChanged += (object sender, System.ComponentModel.PropertyChangedEventArgs e) =>
            {
                if (e.PropertyName == nameof(item.Expanded))
                {
                    if (node.IsExpanded)
                    {
                        node.IsExpanded = item.Expanded;
                        TreeView.SelectedItem = node;
                    }
                }
            };*/

            node.PropertyChanged += Node_PropertyChanged;

            if (item.Children.Count > 0)
            {
                foreach (Item newItem in item.Children)
                {
                    node.Add(CreateTreeNode(newItem));
                }
            }

            item.Children.CollectionChanged += (object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) =>
            {
                foreach (Item newItem in e.NewItems)
                {
                    node.Add(CreateTreeNode(newItem));
                }
            };

            return node;
        }

        private void Node_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            TreeNode node = (TreeNode)sender;
            Item item = (Item)node.Data;

            if (e.PropertyName == nameof(node.IsExpanded) && node.IsExpanded != item.Expanded)
            {
                if (node.IsExpanded)
                {
                    Data.ItemExpanded(item);
                }
                else
                {
                    Data.ItemCollapsed(item);
                }
            }
        }

        private void TreeView_ItemClick(object sender, ItemClickEventArgs e)
        {
            TreeNode node = (TreeNode)e.ClickedItem;
            Item item = (Item)node.Data;

            Data.ItemInvoked(item);
        }
    }
}
