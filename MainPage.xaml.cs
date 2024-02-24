using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.Core;
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
            List<StorageFile> list = e.Parameter as List<StorageFile>;
            list?.ForEach(AddTab);
            ResizeTabs();
        }

        private void MainPage_SizeChanged(object sender, SizeChangedEventArgs e) => ResizeTabs();

        private void CoreWindow_CharacterReceived(CoreWindow sender, CharacterReceivedEventArgs args)
        {
            TabViewItem tab = (TabViewItem)Tabs.SelectedItem;
            MyVideoControl videoControl = tab.Content as MyVideoControl;
            switch (args.KeyCode)
            {
                case ' ':
                    videoControl.PlayPause();
                    break;
                case ']':
                    videoControl.MediaPlayer.MediaPlayer.PlaybackSession.PlaybackRate =
                        Math.Min(1, videoControl.MediaPlayer.MediaPlayer.PlaybackSession.PlaybackRate + 0.1);
                    break;
                case '[':
                    videoControl.MediaPlayer.MediaPlayer.PlaybackSession.PlaybackRate =
                        Math.Max(0.1, videoControl.MediaPlayer.MediaPlayer.PlaybackSession.PlaybackRate - 0.1);
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
            if (!fileTypes.Contains(file.FileType.ToLower())) return;
            OpenButton.Visibility = Visibility.Collapsed;
            MyVideoControl videoControl = new MyVideoControl(file);
            TabViewItem tab = new TabViewItem()
            {
                MinWidth = 60,
                Header = file.Name,
                AllowDrop = true,
                Content = videoControl
            };
            videoControl.Tab = tab;
            Tabs.TabItems.Add(tab);
            if (Tabs.TabItems.Count == 1)
            {
                Tabs.SelectedIndex = 0;
            }
        }

        public void ResizeTabs()
        {
            foreach (TabViewItem tabViewItem in Tabs.TabItems)
            {
                tabViewItem.MaxWidth = Math.Min(400, (Window.Current.Bounds.Width - 200) / Tabs.TabItems.Count);
                tabViewItem.IsClosable = tabViewItem.IsSelected || tabViewItem.MaxWidth > 80;
            }
        }

        private void Tab_CloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {
            MyVideoControl videoControl = args.Tab.Content as MyVideoControl;
            videoControl.MediaPlayer.MediaPlayer.Pause();
            videoControl.MediaPlayer.MediaPlayer.Dispose();
            sender.IsAddTabButtonVisible = true;
            sender.TabItems.Remove(args.Tab);
            sender.IsAddTabButtonVisible = false;
            ResizeTabs();
            if (Tabs.TabItems.Count == 0)
            {
                CoreApplication.Exit();
            }
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
                        if (fileTypes.Contains(appFile.FileType.ToLower()))
                        {
                            AddTab(appFile);
                        }
                    }
                    ResizeTabs();
                }
                locked = false;
            }
        }

        private void Drag_over(object sender, DragEventArgs e) => e.AcceptedOperation = DataPackageOperation.Copy;

        private async void OpenFilePicker()
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker
            {
                ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail,
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop
            };

            fileTypes.ForEach(picker.FileTypeFilter.Add);

            var files = await picker.PickMultipleFilesAsync();
            files.Where(file => file != null).ToList().ForEach(AddTab);
            ResizeTabs();

        }

        private void OpenButton_Click(object sender, RoutedEventArgs e) => OpenFilePicker();

        private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e) => ResizeTabs();
    }
}
