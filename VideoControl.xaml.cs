using Microsoft.Graphics.Canvas;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Graphics.Imaging;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Video_Player
{
    public sealed partial class VideoControl : UserControl
    {
        private MediaPlaybackState playbackState;
        private ThreadPoolTimer periodicTimer;

        private double loopMarkerA = 0;
        private double loopMarkerB = 0;

        public MediaPlayer MediaPlayer { get { return mediaPlayerElement.MediaPlayer; } }

        public TabViewItem Tab { get; internal set; }

        //private bool pingpong = false;
        public VideoControl(StorageFile file)
        {
            InitializeComponent();

            mediaPlayerElement.Source = MediaSource.CreateFromStorageFile(file);

            MediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
            MediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
            MediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
            MediaPlayer.PlaybackSession.PlaybackRateChanged += PlaybackSession_PlaybackRateChanged;
        }

        private async void PlaybackSession_PlaybackRateChanged(MediaPlaybackSession sender, object args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, UpdatePlaybackSpeed);
        }

        private async void MediaPlayer_MediaOpened(MediaPlayer sender, object args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                UpdateTimestamp();
                UpdatePlaybackSpeed();
                rangeSlider.RangeEnd = rangeSlider.Maximum = loopMarkerB = sender.PlaybackSession.NaturalDuration.TotalMilliseconds;
            });
        }

        private async void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                playPause.Content = sender.PlaybackState switch
                {
                    MediaPlaybackState.Playing => new SymbolIcon(Symbol.Pause),
                    MediaPlaybackState.Paused => new SymbolIcon(Symbol.Play),
                    _ => playPause.Content,
                };
                ((TabTitle)Tab.Header).ShowIcon(sender.PlaybackState == MediaPlaybackState.Playing);
            });
        }

        /**
         * Update slider and timestamp from player position.
         */
        private void UpdateFromPlayer()
        {
            slider.ValueChanged -= Slider_ValueChanged;
            slider.Value =
                MediaPlayer.PlaybackSession.Position.TotalSeconds
                / MediaPlayer.PlaybackSession.NaturalDuration.TotalSeconds
                * 100;
            UpdateTimestamp();
            slider.ValueChanged += Slider_ValueChanged;
        }

        /**
         * Updates the video position and timestamp from the slider value.
         */
        private void UpdateFromSlider()
        {
            MediaPlayer.PlaybackSession.Position = TimeSpan.FromMilliseconds(
                slider.Value / 100 * MediaPlayer.PlaybackSession.NaturalDuration.TotalMilliseconds
            );
            UpdateTimestamp();
        }

        /**
         * Updates the timestamp based on the media player position.
         */
        private void UpdateTimestamp()
        {
            TimeSpan currentTime = MediaPlayer.PlaybackSession.Position;
            TimeSpan totalTime = MediaPlayer.PlaybackSession.NaturalDuration;

            timestamp.Text = totalTime.Hours > 0 ?
                $"{currentTime:h':'mm':'ss}/{totalTime:h':'mm':'ss}" :
                $"{currentTime:m':'ss}/{totalTime:m':'ss}";
        }


        private void UpdatePlaybackSpeed()
        {
            videoSpeedLabel.Text = string.Format("{0:F2}", MediaPlayer.PlaybackSession.PlaybackRate);
        }

        private bool IsPositionBetweenAandB(double position, double fullVideoLength)
        {
            double stop = loopMarkerB;
            double pos = position;
            if (loopMarkerA > loopMarkerB)
            {
                stop += fullVideoLength;
                if (pos < loopMarkerB)
                {
                    pos += fullVideoLength;
                }
            }
            return loopMarkerA <= pos && pos < stop;
        }

        private async void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                 {
                     UpdateFromPlayer();
                     if (!IsPositionBetweenAandB(sender.Position.TotalMilliseconds, sender.NaturalDuration.TotalMilliseconds))
                     {
                         sender.Position = TimeSpan.FromMilliseconds(loopMarkerA);
                     }
                 }
            );
        }

        public void MuteUnmute()
        {
            MediaPlayer.IsMuted = !MediaPlayer.IsMuted;
            muteToggle.Content = new SymbolIcon(MediaPlayer.IsMuted ? Symbol.Mute : Symbol.Volume);
        }

        public void PlayPause(object sender = null, RoutedEventArgs e = null)
        {
            ReverseVideo(false);
            if (MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
            {
                MediaPlayer.Pause();
            }
            else
            {
                MediaPlayer.Play();
            }
        }

        private void Slider_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            Debug.WriteLine("Slider manipulation started");
            PrepareForSeek();
        }

        private void Slider_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            UpdateFromSlider();
            FinishedSeeking();
        }

        /**
         * Prepare to use the seekbar to change the position of the video
         * by removing listeners and saving the current playback state.
         */
        private void PrepareForSeek()
        {
            playbackState = MediaPlayer.PlaybackSession.PlaybackState;
            MediaPlayer.Pause();
            MediaPlayer.PlaybackSession.PositionChanged -= PlaybackSession_PositionChanged;
            Debug.WriteLine(MediaPlayer.PlaybackSession.PlaybackState);
        }

        /**
         * Re-add the media progress listener after seeking has been completed.
         */
        private void FinishedSeeking()
        {
            if (playbackState == MediaPlaybackState.Playing)
            {
                MediaPlayer.Play();
            }
            MediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
        }

        private void Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e) => UpdateFromSlider();

        private void Toggle_Click(object sender, RoutedEventArgs e)
        {
            if (sender == toggleA)
            {
                rangeSlider.RangeStart = loopMarkerA =
                    (bool)toggleA.IsChecked ?
                    MediaPlayer.PlaybackSession.Position.TotalMilliseconds :
                    0;
            }
            else if (sender == toggleB)
            {
                rangeSlider.RangeEnd = loopMarkerB =
                    (bool)toggleB.IsChecked ?
                    MediaPlayer.PlaybackSession.Position.TotalMilliseconds :
                    MediaPlayer.PlaybackSession.NaturalDuration.TotalMilliseconds;
            }
            else if (sender == toggleLoop)
            {
                MediaPlayer.IsLoopingEnabled = (bool)toggleLoop.IsChecked;
            }
            else if (sender == toggleInvert)
            {
                (rangeSlider.Foreground, rangeSlider.Background) = (rangeSlider.Background, rangeSlider.Foreground);
                SetLoopMarkers();
            }
            else if (sender == muteToggle)
            {
                MuteUnmute();
            }
        }

        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender == flipH)
            {
                MediaPlayer.PlaybackSession.IsMirroring = !MediaPlayer.PlaybackSession.IsMirroring;
            }
            else if (sender == rotateR || sender == rotateL)
            {
                int rotationValue = ((int)MediaPlayer.PlaybackSession.PlaybackRotation + (sender == rotateR ? 1 : 3)) % 4;
                MediaPlayer.PlaybackSession.PlaybackRotation = (MediaRotation)rotationValue;
            }
            else if (sender == take_snapshot)
            {
                SavePicture();
            }
        }
        public async void SavePicture()
        {
            CanvasDevice canvasDevice = CanvasDevice.GetSharedDevice();
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                SoftwareBitmap frameServerDest = new SoftwareBitmap(
                    BitmapPixelFormat.Rgba8,
                    (int)MediaPlayer.PlaybackSession.NaturalVideoWidth,
                    (int)MediaPlayer.PlaybackSession.NaturalVideoHeight,
                    BitmapAlphaMode.Premultiplied
                );

                using CanvasBitmap canvasBitmap = CanvasBitmap.CreateFromSoftwareBitmap(canvasDevice, frameServerDest);
                MediaPlayer.CopyFrameToVideoSurface(canvasBitmap);

                SoftwareBitmap softwareBitmapImg = await SoftwareBitmap.CreateCopyFromSurfaceAsync(canvasBitmap);
                FileSavePicker savePicker = new FileSavePicker
                {
                    SuggestedStartLocation = PickerLocationId.Desktop,
                    SuggestedFileName = "Snapshot" + DateTime.Now.ToString("yyyyMMddhhmmss"),
                };
                savePicker.FileTypeChoices.Add("Image", new List<string>() { ".jpg" });
                StorageFile savefile = await savePicker.PickSaveFileAsync();
                if (savefile == null) return;


                using var stream = await savefile.OpenAsync(FileAccessMode.ReadWrite);
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
                encoder.SetSoftwareBitmap(softwareBitmapImg);
                await encoder.FlushAsync();
            });
        }

        private void ReverseVideo(bool start)
        {
            if (start)
            {
                var period = TimeSpan.FromMilliseconds(30);
                periodicTimer = ThreadPoolTimer.CreatePeriodicTimer((source) =>
                {
                    _ = Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                    {
                        if (MediaPlayer.PlaybackSession.Position > TimeSpan.Zero)
                        {
                            MediaPlayer.PlaybackSession.Position -= period;
                        }
                        else
                        {
                            source.Cancel();
                            reverseButton.IsChecked = false;
                        }
                    });

                }, period);
            }
            else
            {
                periodicTimer?.Cancel();
                reverseButton.IsChecked = false;
            }
        }

        private void ReverseButton_Checked(object sender, RoutedEventArgs e)
        {
            if (MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
            {
                PlayPause();
            }
            ReverseVideo((bool)reverseButton.IsChecked);
        }

        private void RangeSlider_ValueChanged(object sender, RangeChangedEventArgs e) => SetLoopMarkers();
        private void SetLoopMarkers()
        {
            loopMarkerA = (bool)toggleInvert.IsChecked ? rangeSlider.RangeEnd : rangeSlider.RangeStart;
            loopMarkerB = (bool)toggleInvert.IsChecked ? rangeSlider.RangeStart : rangeSlider.RangeEnd;
        }

        private void MediaPlayer_Tapped(object sender, TappedRoutedEventArgs e) => PlayPause();

        private void MediaPlayer_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e) => PlayPause();
    }
}
