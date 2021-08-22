using System;
using System.Diagnostics;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Video_Player
{
    public sealed partial class MyVideoControl : UserControl
    {
        private MediaPlaybackState playbackState;
        private TimeSpan a = TimeSpan.FromMilliseconds(0);
        private TimeSpan b = TimeSpan.FromMilliseconds(0);
        private ThreadPoolTimer PeriodicTimer;
        private bool pingpong = false;
        public MyVideoControl(StorageFile file)
        {
            InitializeComponent();

            mediaPlayer.Source = MediaSource.CreateFromStorageFile(file);

            mediaPlayer.MediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
            mediaPlayer.MediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
            mediaPlayer.MediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
            mediaPlayer.MediaPlayer.PlaybackSession.PlaybackRateChanged += PlaybackSession_PlaybackRateChanged;
        }

        private async void PlaybackSession_PlaybackRateChanged(MediaPlaybackSession sender, object args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                UpdatePlaybackSpeed();
            });
        }

        private async void MediaPlayer_MediaOpened(MediaPlayer sender, object args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                UpdateTimestamp();
                UpdatePlaybackSpeed();
                b = mediaPlayer.MediaPlayer.PlaybackSession.NaturalDuration;
            });
        }

        private async void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                switch (sender.PlaybackState)
                {
                    case MediaPlaybackState.Playing:
                        playPause.Content = new SymbolIcon(Symbol.Pause);
                        break;
                    case MediaPlaybackState.Paused:
                        playPause.Content = new SymbolIcon(Symbol.Play);
                        break;
                    default:
                        break;
                }
            });
        }

        /**
         * Update slider and timestamp from player position.
         */
        private void updateFromPlayer()
        {
            slider.ValueChanged -= slider_ValueChanged;
            slider.Value = mediaPlayer.MediaPlayer.PlaybackSession.Position.TotalSeconds / mediaPlayer.MediaPlayer.PlaybackSession.NaturalDuration.TotalSeconds * 100;
            UpdateTimestamp();
            slider.ValueChanged += slider_ValueChanged;
        }

        /**
         * Updates the video position and timestamp from the slider value.
         */
        private void UpdateFromSlider()
        {
            //System.Diagnostics.Debug.WriteLine("update from slider");
            mediaPlayer.MediaPlayer.PlaybackSession.Position = TimeSpan.FromMilliseconds(slider.Value / 100 * mediaPlayer.MediaPlayer.PlaybackSession.NaturalDuration.TotalMilliseconds);
            UpdateTimestamp();
        }

        /**
         * Updates the timestamp based on the media player position.
         */
        private void UpdateTimestamp()
        {
            int milliseconds = (int)mediaPlayer.MediaPlayer.PlaybackSession.Position.TotalMilliseconds;
            int seconds = milliseconds / 1000;
            int minutes = seconds / 60;
            int totalMilliseconds = (int)mediaPlayer.MediaPlayer.PlaybackSession.NaturalDuration.TotalMilliseconds;
            int totalSeconds = totalMilliseconds / 1000;
            int totalMinutes = totalSeconds / 60;
            seconds %= 60;
            totalSeconds %= 60;
            timestamp.Text = String.Format("{0}:{1:D2}/{2}:{3:D2}", minutes, seconds, totalMinutes, totalSeconds);
        }

        private void UpdatePlaybackSpeed()
        {
            videoSpeedLabel.Text = String.Format("{0:F2}", mediaPlayer.MediaPlayer.PlaybackSession.PlaybackRate);
        }

        private bool IsPositionBetweenAandB(TimeSpan position, TimeSpan fullVideoLength)
        {
            TimeSpan stop = b;
            TimeSpan pos = position;
            if (a > b)
            {
                stop += fullVideoLength;
                if (pos < b)
                {
                    pos += fullVideoLength;
                }
            }
            return a < pos && pos < stop;
        }

        private async void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                 {
                     updateFromPlayer();
                     /*if ((bool)toggleB.IsChecked && Math.Abs(mediaPlayer.MediaPlayer.PlaybackSession.Position.TotalMilliseconds - b.TotalMilliseconds) < 50)
                     {
                         mediaPlayer.MediaPlayer.PlaybackSession.Position = a;
                     }*/
                     TimeSpan position = mediaPlayer.MediaPlayer.PlaybackSession.Position;
                     TimeSpan fullVideoLength = mediaPlayer.MediaPlayer.PlaybackSession.NaturalDuration;
                     if (!IsPositionBetweenAandB(position, fullVideoLength))
                     {
                         mediaPlayer.MediaPlayer.PlaybackSession.Position = a;
                     }
                 });

        }

        public MediaPlayerElement MediaPlayer { get { return mediaPlayer; } }

        public void PlayPause(object sender = null, Windows.UI.Xaml.RoutedEventArgs e = null)
        {
            ReverseVideo(false);
            if (mediaPlayer.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
            {
                mediaPlayer.MediaPlayer.Pause();
            }
            else
            {
                mediaPlayer.MediaPlayer.Play();
            }
        }

        private void slider_ManipulationStarted(object sender, Windows.UI.Xaml.Input.ManipulationStartedRoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Slider manipulation started");
            PrepareForSeek();
        }

        private void Slider_ManipulationCompleted(object sender, Windows.UI.Xaml.Input.ManipulationCompletedRoutedEventArgs e)
        {
            UpdateFromSlider();
            FinishedSeeking();
        }

        /**
         * Prepare to use the seekbar to change the position of the video by removing listeners and saving the current playback state.
         */
        private void PrepareForSeek()
        {
            playbackState = mediaPlayer.MediaPlayer.PlaybackSession.PlaybackState;
            mediaPlayer.MediaPlayer.Pause();
            mediaPlayer.MediaPlayer.PlaybackSession.PositionChanged -= PlaybackSession_PositionChanged;
            Debug.WriteLine(mediaPlayer.MediaPlayer.PlaybackSession.PlaybackState);
        }

        /**
         * Re-add the media progress listener after seeking has been completed.
         */
        private void FinishedSeeking()
        {
            switch (playbackState)
            {
                case MediaPlaybackState.Playing:
                    mediaPlayer.MediaPlayer.Play();
                    break;
                case MediaPlaybackState.Paused:
                    break;
                default:
                    break;
            }
            mediaPlayer.MediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
        }

        private void slider_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            UpdateFromSlider();
        }

        private void toggle_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (sender == toggleA)
            {
                a = (bool)toggleA.IsChecked ? mediaPlayer.MediaPlayer.PlaybackSession.Position : TimeSpan.FromMilliseconds(0);
            }
            else if (sender == toggleB)
            {
                b = (bool)toggleB.IsChecked ? mediaPlayer.MediaPlayer.PlaybackSession.Position : mediaPlayer.MediaPlayer.PlaybackSession.NaturalDuration;
            }
            else if (sender == toggleLoop)
            {
                mediaPlayer.MediaPlayer.IsLoopingEnabled = (bool)toggleLoop.IsChecked;
            }
        }

        private void MenuFlyoutItem_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (sender == flipH)
            {
                scale.ScaleX *= -1;
            }
            else if (sender == rotateR || sender == rotateL)
            {
                rotate.Angle += sender == rotateR ? 90 : -90;
                UpdateLayout();
            }
        }


        private void ReverseVideo(bool start)
        {
            if (start)
            {
                var period = TimeSpan.FromMilliseconds(30);
                PeriodicTimer = ThreadPoolTimer.CreatePeriodicTimer((source) =>
                {
                    _ = Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                    {
                        if (mediaPlayer.MediaPlayer.PlaybackSession.Position > TimeSpan.Zero)
                        {
                            mediaPlayer.MediaPlayer.PlaybackSession.Position -= period;
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
                if (PeriodicTimer != null)
                {
                    PeriodicTimer.Cancel();
                }
                reverseButton.IsChecked = false;
            }
        }

        private void reverseButton_Checked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (mediaPlayer.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
            {
                PlayPause();
            }
            ReverseVideo((bool)reverseButton.IsChecked);
        }
    }
}
