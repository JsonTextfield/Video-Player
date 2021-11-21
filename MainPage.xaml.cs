using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Video_Player
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        private bool locked = false;
        private readonly List<string> fileTypes = new List<string>() { ".mp4", ".mov", ".flv", ".wmv", ".mkv" };
        public MainPage()
        {
            this.InitializeComponent();

            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;
            coreTitleBar.LayoutMetricsChanged += CoreTitleBar_LayoutMetricsChanged;

            Window.Current.SetTitleBar(CustomDragRegion);
            Window.Current.CoreWindow.CharacterReceived += CoreWindow_CharacterReceived;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter != null && e.Parameter is List<StorageFile> list)
            {
                var files = list;
                foreach (var f in files)
                {
                    AddTab(f);
                }
            }
        }

        private async void showMessage(string message)
        {
            var messageDialog = new MessageDialog(message);

            // Set the command that will be invoked by default
            messageDialog.DefaultCommandIndex = 0;

            // Set the command to be invoked when escape is pressed
            messageDialog.CancelCommandIndex = 1;

            // Show the message dialog
            await messageDialog.ShowAsync();
        }

        private void MainPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ResizeTabs();
        }


        private void CoreWindow_CharacterReceived(CoreWindow sender, CharacterReceivedEventArgs args)
        {
            TabViewItem tab = (TabViewItem)Tabs.SelectedItem;
            MyVideoControl videoControl = tab.Content as MyVideoControl;
            switch (args.KeyCode)
            {
                case ' ':
                case 'k':
                    videoControl.PlayPause();
                    break;
                case ']':
                    videoControl.MediaPlayer.MediaPlayer.PlaybackSession.PlaybackRate += 0.1;
                    break;
                case '[':
                    videoControl.MediaPlayer.MediaPlayer.PlaybackSession.PlaybackRate -= 0.1;
                    break;
                case 'j':
                    videoControl.MediaPlayer.MediaPlayer.PlaybackSession.Position -= TimeSpan.FromSeconds(5);
                    break;
                case 'l':
                    videoControl.MediaPlayer.MediaPlayer.PlaybackSession.Position += TimeSpan.FromSeconds(5);
                    break;
                case 'm':
                    videoControl.MediaPlayer.MediaPlayer.IsMuted = !videoControl.MediaPlayer.MediaPlayer.IsMuted;
                    tab.IconSource = new Microsoft.UI.Xaml.Controls.SymbolIconSource()
                    {
                        Symbol = videoControl.MediaPlayer.MediaPlayer.IsMuted ? Symbol.Mute : Symbol.Volume
                    };
                    break;
                default:
                    System.Diagnostics.Debug.WriteLine(args.KeyCode);
                    break;
            }
        }

        private void CoreTitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
        {
            if (FlowDirection == FlowDirection.LeftToRight)
            {
                CustomDragRegion.MinWidth = sender.SystemOverlayRightInset;
                ShellTitlebarInset.MinWidth = sender.SystemOverlayLeftInset;
            }
            else
            {
                CustomDragRegion.MinWidth = sender.SystemOverlayLeftInset;
                ShellTitlebarInset.MinWidth = sender.SystemOverlayRightInset;
            }

            CustomDragRegion.Height = ShellTitlebarInset.Height = sender.Height;
        }

        public void AddTab(StorageFile file)
        {
            OpenButton.Visibility = Visibility.Collapsed;
            MyVideoControl videoControl = new MyVideoControl(file);
            TabViewItem tab = new TabViewItem() { MinWidth = 40, Header = file.Name, AllowDrop = true, Content = videoControl };
            videoControl.Tab = tab;
            Tabs.TabItems.Add(tab);
            ResizeTabs();
            if (Tabs.TabItems.Count == 1)
            {
                Tabs.SelectedIndex = 0;
            }
        }

        private void ResizeTabs()
        {
            Tabs.CloseButtonOverlayMode = TabViewCloseButtonOverlayMode.OnPointerOver;
            foreach (TabViewItem tabViewItem in Tabs.TabItems)
            {
                tabViewItem.MaxWidth = Math.Min(400, (Window.Current.Bounds.Width - 200) / Tabs.TabItems.Count);
            }
        }

        private void Tab_CloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {
            MyVideoControl videoControl = args.Tab.Content as MyVideoControl;
            videoControl.MediaPlayer.MediaPlayer.Pause();
            //videoControl.MediaPlayer.MediaPlayer.Dispose();
            sender.IsAddTabButtonVisible = true;
            sender.TabItems.Remove(args.Tab);
            sender.IsAddTabButtonVisible = false;
            ResizeTabs();
        }

        private async void Page_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                if (locked) return;
                locked = true;
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count > 0)
                {
                    foreach (StorageFile appFile in items.OfType<StorageFile>())
                    {
                        if (fileTypes.Contains(appFile.FileType))
                        {
                            AddTab(appFile);
                        }
                    }
                }
                locked = false;
            }
        }

        private void Drag_over(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
        }

        private async void OpenFilePicker()
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker
            {
                ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail,
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop
            };

            foreach (var fileType in fileTypes)
            {
                picker.FileTypeFilter.Add(fileType);
            }

            var files = await picker.PickMultipleFilesAsync();
            if (files.Count > 0)
            {
                // Application now has read/write access to the picked file(s)
                foreach (StorageFile file in files)
                {
                    if (file != null)
                    {
                        AddTab(file);
                    }
                }
            }

        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFilePicker();
        }
    }
}
