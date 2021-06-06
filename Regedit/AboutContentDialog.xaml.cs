using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Regedit
{
    public sealed partial class AboutContentDialog : ContentDialog
    {
        public AboutContentDialog(string OSName, string RegisteredOwner, string RegisteredOrg, string Version, string BuildNumber)
        {
            this.InitializeComponent();
            VersionText.Text = "Version " + Version + " (OS build " + BuildNumber + ")";
            this.RegisteredOwner.Text = "    " + RegisteredOwner;
            this.RegisteredOrg.Text = "    " + RegisteredOrg;
            this.CopyrightText.Text = $"The {OSName} operating system and its user interface are protected by trademark and other pending or existing intellectual property rights in the United States and other countries/regions";
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }
    }
}
