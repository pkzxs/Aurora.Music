﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aurora.Music.Core.Storage;
using Aurora.Shared.Helpers;
using Aurora.Shared.MVVM;
using Windows.Storage;
using System.Diagnostics;
using Aurora.Music.Core.Player;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;
using Aurora.Music.Pages;
using Windows.UI.Text;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml.Media;
using Windows.Media.Playback;
using System.Collections.ObjectModel;
using Aurora.Music.Core;
using Windows.System.Threading;
using Aurora.Shared.Extensions;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;
using Aurora.Music.Core.Models;

namespace Aurora.Music.ViewModels
{
    class MainPageViewModel : ViewModelBase, IDisposable
    {
        public static MainPageViewModel Current;

        public List<HamPanelItem> HamList { get; set; } = new List<HamPanelItem>()
        {
            new HamPanelItem
            {
                Title = "Home",
                TargetType = typeof(HomePage),
                Icon="\uE80F",
                IsCurrent = true
            },
            new HamPanelItem
            {
                Title = "Library",
                Icon="\uE2AC",
                TargetType = typeof(LibraryPage)
            },
            new HamPanelItem
            {
                Title = "Playlist",
                Icon="\uE955",
                TargetType = typeof(HomePage)
            },
        };

        internal IPlayer GetPlayer()
        {
            if (player == null)
            {
                player = new AudioGraphPlayer();
            }
            return player;
        }

        public ObservableCollection<SongViewModel> NowPlayingList { get; set; } = new ObservableCollection<SongViewModel>();

        private IPlayer player;

        private bool _lastLeftTop;
        private bool isLeftTopDark;
        public bool IsLeftTopForeWhite
        {
            get { return isLeftTopDark; }
            set
            {
                _lastLeftTop = isLeftTopDark;
                SetProperty(ref isLeftTopDark, value);
                ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;
                titleBar.ForegroundColor = value ? Colors.Black : Colors.White;
            }
        }


        private bool needShowPanel = true;
        public bool NeedShowPanel
        {
            get { return needShowPanel; }
            set { SetProperty(ref needShowPanel, value); }
        }

        private Uri currentArtwork;
        public Uri CurrentArtwork
        {
            get { return currentArtwork; }
            set { SetProperty(ref currentArtwork, value); }
        }

        private double nowPlayingPosition;
        public double NowPlayingPosition
        {
            get { return nowPlayingPosition; }
            set { SetProperty(ref nowPlayingPosition, value); }
        }

        private bool? isPlaying;
        public bool? IsPlaying
        {
            get { return isPlaying; }
            set { SetProperty(ref isPlaying, value); }
        }

        internal void RestoreLastTitle()
        {
            NeedShowTitle = _lastneedshow;
            Title = _lasttitle;
            IsLeftTopForeWhite = _lastLeftTop;
        }

        private TimeSpan currentPosition;
        public TimeSpan CurrentPosition
        {
            get { return currentPosition; }
            set { SetProperty(ref currentPosition, value); }
        }

        private TimeSpan totalDuration;
        public TimeSpan TotalDuration
        {
            get { return totalDuration; }
            set { SetProperty(ref totalDuration, value); }
        }

        private string currentTitle;
        public string CurrentTitle
        {
            get { return currentTitle; }
            set { SetProperty(ref currentTitle, value); }
        }

        private string currentAlbum;
        private string lastUriPath;

        public string CurrentAlbum
        {
            get { return currentAlbum; }
            set { SetProperty(ref currentAlbum, value); }
        }

        private string nowListPreview = "-/-";

        internal void FastForward(bool v)
        {
            throw new NotImplementedException();
        }

        public string NowListPreview
        {
            get { return nowListPreview; }
            set { SetProperty(ref nowListPreview, value); }
        }

        private int currentIndex = -1;
        public int CurrentIndex
        {
            get { return currentIndex; }
            set { SetProperty(ref currentIndex, value); }
        }

        private bool _lastneedshow;
        private bool needHeader;
        public bool NeedShowTitle
        {
            get { return needHeader; }
            set
            {
                _lastneedshow = needHeader;
                SetProperty(ref needHeader, value);
            }
        }

        private string _lasttitle;
        private string title;
        public string Title
        {
            get { return title; }
            set
            {
                _lasttitle = title;
                SetProperty(ref title, value);
            }
        }

        public DelegateCommand GoPrevious
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    player?.Previous();
                });
            }
        }

        public DelegateCommand GoNext
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    player?.Next();
                });
            }
        }

        public DelegateCommand PlayPause
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    if (IsPlaying is bool b)
                    {
                        if (b)
                        {
                            player?.Pause();
                        }
                        else
                        {
                            player?.Play();
                        }
                    }
                    else
                    {
                        player?.Play();
                    }
                });
            }
        }

        public DelegateCommand GotoSettings
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    MainPage.Current.Navigate(typeof(SettingsPage));
                });
            }
        }

        public DelegateCommand GotoAbout
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    MainPage.Current.Navigate(typeof(AboutPage));
                });
            }
        }

        private bool? isShuffle = false;
        public bool? IsShuffle
        {
            get { return isShuffle; }
            set
            {
                SetProperty(ref isShuffle, value);

                player?.Shuffle(value);
            }
        }

        private bool? isLoop = false;
        public bool? IsLoop
        {
            get { return isLoop; }
            set
            {
                SetProperty(ref isLoop, value);

                player?.Loop(value);
            }
        }

        private bool darkAccent;
        public bool IsDarkAccent
        {
            get { return darkAccent; }
            set { SetProperty(ref darkAccent, value); }
        }

        public MainPageViewModel()
        {
            player = new AudioGraphPlayer();
            Current = this;
            player.StatusChanged += Player_StatusChanged;
            player.PositionUpdated += Player_PositionUpdated;
            var t = ThreadPool.RunAsync(async x =>
            {
                await FindFileChanges();
            });
        }

        private async Task FindFileChanges()
        {
            var addedFiles = await FileTracker.FindChanges();
            if (!addedFiles.IsNullorEmpty())
            {
                var reader = new FileReader();
                reader.NewSongsAdded += Reader_NewSongsAdded;
                await reader.ReadFileandSave(addedFiles);
            }
        }

        private async void Reader_NewSongsAdded(object sender, SongsAddedEventArgs e)
        {
            await new FileReader().AddToAlbums(e.NewSongs);
        }

        private async void Player_PositionUpdated(object sender, PositionUpdatedArgs e)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
            {
                CurrentPosition = e.Current;
                TotalDuration = e.Total;
            });
        }

        private async void Player_StatusChanged(object sender, StatusChangedArgs e)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
            {
                IsPlaying = player.IsPlaying;
                if (e.CurrentSong != null)
                {
                    var p = e.CurrentSong;
                    CurrentTitle = p.Title;
                    CurrentAlbum = p.Album;
                    if (!p.PicturePath.IsNullorEmpty())
                    {
                        if (lastUriPath == p.PicturePath)
                        {

                        }
                        else
                        {
                            CurrentArtwork = new Uri(p.PicturePath);
                            lastUriPath = p.PicturePath;
                        }
                    }
                    else
                    {
                        CurrentArtwork = null;
                    }
                    if (e.Items is IList<MediaPlaybackItem> l)
                    {
                        NowListPreview = $"{e.CurrentIndex + 1}/{l.Count}";
                        NowPlayingList.Clear();
                        for (int i = 0; i < l.Count; i++)
                        {
                            var prop = l[i].GetDisplayProperties();
                            NowPlayingList.Add(new SongViewModel(l[i].Source.CustomProperties[Consts.SONG] as Song)
                            {
                                Index = (uint)i,
                            });
                        }
                        CurrentIndex = Convert.ToInt32(e.CurrentIndex);
                    }
                }
            });
        }

        public void Dispose()
        {
            player.StatusChanged -= Player_StatusChanged;
            player.PositionUpdated -= Player_PositionUpdated;
            ((IDisposable)player).Dispose();
        }

        internal async Task InstantPlay(IList<Song> songs, int startIndex = 0)
        {
            await player.NewPlayList(songs, startIndex);
        }

        public Symbol NullableBoolToSymbol(bool? b)
        {
            if (b is bool bb)
            {
                return bb ? Symbol.Pause : Symbol.Play;
            }
            return Symbol.Play;
        }

        public double PositionToValue(TimeSpan t1, TimeSpan total)
        {
            if (total == null || total == default(TimeSpan))
            {
                return 0;
            }
            return 100 * (t1.TotalMilliseconds / total.TotalMilliseconds);
        }

    }


    class HamPanelItem : ViewModelBase
    {
        public string Title { get; set; }

        public Type TargetType { get; set; }

        public string Icon { get; set; }

        private bool isCurrent;
        public bool IsCurrent
        {
            get { return isCurrent; }
            set { SetProperty(ref isCurrent, value); }
        }

        public FontWeight ChangeWeight(bool b)
        {
            return b ? FontWeights.Bold : FontWeights.Normal;
        }

        public SolidColorBrush ChangeForeground(bool b)
        {
            return (SolidColorBrush)(b ? MainPage.Current.Resources["SystemControlBackgroundAccentBrush"] : MainPage.Current.Resources["SystemControlForegroundBaseHighBrush"]);
        }
    }
}
