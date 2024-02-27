using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Video_Player
{
    public sealed partial class TabTitle : UserControl
    {
        public TabTitle(string text)
        {
            this.InitializeComponent();
            title.Text = text;
        }

        public void ShowIcon(bool b) 
        {
            icon.Visibility = b ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
