using System;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace Regedit.RegistryTreeView
{
    public partial class UniversalTreeView
    {
        public static DependencyProperty DataProperty = DependencyProperty.RegisterAttached
            (
                nameof(Data),
                typeof(DataBuilder),
                typeof(UniversalTreeView),
                new PropertyMetadata(null, OnDataChanged)
            );

        private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UniversalTreeView c)
            {
                c.OnDataChanged(e);
            }
        }

        public DataBuilder Data { get => (DataBuilder)GetValue(DataProperty); set => SetValue(DataProperty, value); }
    }
}
