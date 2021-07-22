using Windows.UI.Xaml.Controls;

namespace Regedit
{
    public sealed partial class AboutContentDialog : ContentDialog
    {
        public AboutContentDialog(string OSName, string RegisteredOwner, string RegisteredOrg, string Version, string BuildNumber)
        {
            this.InitializeComponent();
            this.RegisteredOwner.Text = "    " + RegisteredOwner;
            this.RegisteredOrg.Text = "    " + RegisteredOrg;
        }
    }
}
