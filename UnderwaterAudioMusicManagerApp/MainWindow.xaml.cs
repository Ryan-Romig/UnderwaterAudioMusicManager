﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using System.IO;
using System.Text;
using System.Windows.Media.Animation;
using System.Linq;
using System.Windows.Data;
//using WMPLib; for old windows media player way




namespace UnderwaterAudioMusicManagerApp
{
    //Color Pallet//

    /* 
    Main ---  #009BD8
    2nd Blue -- #00B7DD
    Blue Green-- #00CFCC
    Green  -----#41E3AB
    Light Green --- #A4F187
    Yello --- #F9F871
    */



    public partial class MainWindow : Window
    {
        //private void setTitleBarDragging()
        //{
        //    SourceInitialized += (s, e) =>
        //    {
        //        IntPtr handle = (new WindowInteropHelper(this)).Handle;
        //        HwndSource.FromHwnd(handle).AddHook(new HwndSourceHook(WindowProc));
        //    };

        //}


        private static IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case 0x0024:
                    WmGetMinMaxInfo(hwnd, lParam);
                    handled = true;
                    break;
            }
            return (IntPtr)0;
        }
        private static void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
        {
            MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));
            int MONITOR_DEFAULTTONEAREST = 0x00000002;
            IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            if (monitor != IntPtr.Zero)
            {
                MONITORINFO monitorInfo = new MONITORINFO();
                GetMonitorInfo(monitor, monitorInfo);
                RECT rcWorkArea = monitorInfo.rcWork;
                RECT rcMonitorArea = monitorInfo.rcMonitor;
                mmi.ptMaxPosition.x = Math.Abs(rcWorkArea.left - rcMonitorArea.left);
                mmi.ptMaxPosition.y = Math.Abs(rcWorkArea.top - rcMonitorArea.top);
                mmi.ptMaxSize.x = Math.Abs(rcWorkArea.right - rcWorkArea.left);
                mmi.ptMaxSize.y = Math.Abs(rcWorkArea.bottom - rcWorkArea.top);
            }
            Marshal.StructureToPtr(mmi, lParam, true);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            /// <summary>x coordinate of point.</summary>
            public int x;
            /// <summary>y coordinate of point.</summary>
            public int y;
            /// <summary>Construct a point of coordinates (x,y).</summary>
            public POINT(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        };

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class MONITORINFO
        {
            public int cbSize = Marshal.SizeOf(typeof(MONITORINFO));
            public RECT rcMonitor = new RECT();
            public RECT rcWork = new RECT();
            public int dwFlags = 0;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
            public static readonly RECT Empty = new RECT();
            public int Width { get { return Math.Abs(right - left); } }
            public int Height { get { return bottom - top; } }
            public RECT(int left, int top, int right, int bottom)
            {
                this.left = left;
                this.top = top;
                this.right = right;
                this.bottom = bottom;
            }
            public RECT(RECT rcSrc)
            {
                left = rcSrc.left;
                top = rcSrc.top;
                right = rcSrc.right;
                bottom = rcSrc.bottom;
            }
            public bool IsEmpty { get { return left >= right || top >= bottom; } }
            public override string ToString()
            {
                if (this == Empty) { return "RECT {Empty}"; }
                return "RECT { left : " + left + " / top : " + top + " / right : " + right + " / bottom : " + bottom + " }";
            }
            public override bool Equals(object obj)
            {
                if (!(obj is Rect)) { return false; }
                return (this == (RECT)obj);
            }
            /// <summary>Return the HashCode for this struct (not garanteed to be unique)</summary>
            public override int GetHashCode() => left.GetHashCode() + top.GetHashCode() + right.GetHashCode() + bottom.GetHashCode();
            /// <summary> Determine if 2 RECT are equal (deep compare)</summary>
            public static bool operator ==(RECT rect1, RECT rect2) { return (rect1.left == rect2.left && rect1.top == rect2.top && rect1.right == rect2.right && rect1.bottom == rect2.bottom); }
            /// <summary> Determine if 2 RECT are different(deep compare)</summary>
            public static bool operator !=(RECT rect1, RECT rect2) { return !(rect1 == rect2); }
        }

        [DllImport("user32")]
        internal static extern bool GetMonitorInfo(IntPtr hMonitor, MONITORINFO lpmi);

        [DllImport("User32")]
        internal static extern IntPtr MonitorFromWindow(IntPtr handle, int flags);
        //above is for proper borderless window    

        public class SliderTools : DependencyObject
        {
            public static bool GetMoveToPointOnDrag(DependencyObject obj) { return (bool)obj.GetValue(MoveToPointOnDragProperty); }
            public static void SetMoveToPointOnDrag(DependencyObject obj, bool value) { obj.SetValue(MoveToPointOnDragProperty, value); }
            public static readonly DependencyProperty MoveToPointOnDragProperty = DependencyProperty.RegisterAttached("MoveToPointOnDrag", typeof(bool), typeof(SliderTools), new PropertyMetadata
            {
                PropertyChangedCallback = (obj, changeEvent) =>
                {
                    var slider = (Slider)obj;
                    if ((bool)changeEvent.NewValue)
                        slider.MouseMove += (obj2, mouseEvent) =>
                        {
                            if (mouseEvent.LeftButton == MouseButtonState.Pressed)
                                slider.RaiseEvent(new MouseButtonEventArgs(mouseEvent.MouseDevice, mouseEvent.Timestamp, MouseButton.Left)
                                {
                                    RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent,
                                    Source = mouseEvent.Source,
                                });
                        };
                }
            });
        }






        //----------------------------------------------------------------------------------//
        //----------------------------------------------------------------------------------//
        //----------------------------------------------------------------------------------//




        //-------------------------------Start Logic-------------------------------------------//

        //---------------------Declare Constants----------------------//
        int mediumResponsiveWindowSize = 570;
        UnderwaterAudioMediaPlayer player = new UnderwaterAudioMediaPlayer();
        
        
        string supportedMusicFileTypes = UnderwaterAudioMediaPlayer.supportedMusicFileTypes;        


        private void createMediaEventHandlers()
        {            
            player.MediaOpened += new EventHandler(player_MediaOpened);
            player.MediaEnded += new EventHandler(player_MediaEnded);
        }

        private void createTimerEventTickAndSetInterval()
        {
            player.timer.Tick += new EventHandler(timer_Tick);
            player.timer.Interval = TimeSpan.FromMilliseconds(200);
        }
        public int timer1TickCount;
        


        public MainWindow()
        {
            InitializeComponent();
            createMediaEventHandlers();
            createTimerEventTickAndSetInterval();
            libraryListView.ItemsSource = player.mediaLibrary;





        }

        //Event Handlers//
        private void player_MediaEnded(object sender, EventArgs e)
        {
            player.loadSongIntoPlayer(player.mediaLibrary[0]);
            player.play();
            
        }



        private void player_MediaOpened(object sender, EventArgs e)
        {
            
            trackProgressSlider.Maximum = player.NaturalDuration.TimeSpan.TotalSeconds;            
            trackDurationLabel.Content = player.NaturalDuration.TimeSpan.ToString(@"m\:ss");
            nowPlayingLabel.Content = player.currentMedia.fileName;
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            
            timeElapsedLabel.Content = player.Position.ToString(@"m\:ss");
            
            if (player.NaturalDuration.HasTimeSpan)
            {
                trackProgressBar.Value = (player.Position.TotalSeconds / player.NaturalDuration.TimeSpan.TotalSeconds) * 100;

            }

        }
        private string getFilePathFromSelectedFile(string itemName, Dictionary<string, Track> list)
        {
            if (itemName != null && list.ContainsKey(itemName.ToString()))
            {
                return list[itemName].filePath;

            }
            return null;
        }

        private void handleShuffleClick()
        {
            toggleShuffle();
            if (player.isRepeat)
            {
                toggleRepeat();
            }   

        }
       

        private void shuffleButton_Click(object sender, RoutedEventArgs e)
        {
            handleShuffleClick();


        }


        private void trackProgressSlider_Change(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (player.Source != null)
            {
                player.Position = TimeSpan.FromSeconds(trackProgressSlider.Value);
                trackProgressBar.Value = player.Position.TotalSeconds / player.NaturalDuration.TimeSpan.TotalSeconds;

            }

        }

        private void volumeSlider_Change(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

            double playerVolume = volumeSlider.Value / 100;
            player.Volume = playerVolume;


        }

        private void mainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (mainWindow.Width <= mediumResponsiveWindowSize)
            {

                volumeSlider.Visibility = Visibility.Collapsed;
                volumeButton.Visibility = Visibility.Visible;

            }
            if (mainWindow.Width > mediumResponsiveWindowSize)
            {

                volumeSlider.Visibility = Visibility.Visible;
                volumeButton.Visibility = Visibility.Collapsed;


            }
        }


        //-------------------------------------------------------------------------------//
       //--------------------Functions--------------------------------------------------//



        private void loadSongIntoPlayer(System.Uri filePath)
        {            
            player.Open(filePath);
            player.Stop();      
            
        }

        private double getWindowSize()
        {
            return mainWindow.Width;
        }

        private void swapPlayToPauseButton()
        {
            playButton.Visibility = Visibility.Collapsed;
            pauseButton.Visibility = Visibility.Visible;

        }
        private void swapPauseToPlayButton()
        {
            playButton.Visibility = Visibility.Visible;
            pauseButton.Visibility = Visibility.Collapsed;
        }

        
        private void startPlaying()
        {
            player.play();
            swapPlayToPauseButton();
            player.timer.Start();
        
        }

        private void pausePlaying()
        {
            player.pause();

            swapPauseToPlayButton();
            player.timer.Stop();
        }

       

        private Track searchForSelectedFile(string itemName, Dictionary<string, Track> list)
        {
            if (itemName != null && list.ContainsKey(itemName.ToString()))
            {
                return list[itemName];

            }
            return null;
        }

        private void rewindButton_Click(object sender, RoutedEventArgs e)
        {   
          



        }

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            closeApp();
        }


        private void selectNextSong()
        {


        }



        private void forwardButton_Click(object sender, RoutedEventArgs e)
        {
            selectNextSong();
   
        }




        private void importButton_Click(object sender, RoutedEventArgs e)
        {
            openImportDialog();
            

        }

        private void openImportDialog()
        {
            OpenFileDialog openFilePopup = new OpenFileDialog();
            openFilePopup.Multiselect = true;
            openFilePopup.Filter = supportedMusicFileTypes;
            //openFilePopup.Filter = "Music |*.mp3;*.wav";
            if (openFilePopup.ShowDialog() == true)
            {

                string[] songFileName = openFilePopup.SafeFileNames;
               string[] songFilePath = openFilePopup.FileNames;

                for(int i= 0; i < songFilePath.Length; i++)
                {
                    Track track = new Track();
                    getTrackTags(track, i,songFileName,songFilePath);
                    Dictionary<string, Track> dic = new Dictionary<string, Track>();
                    foreach(Track song in player.mediaLibrary)
                    {
                        if(dic.ContainsKey(song.filePath) != true)
                        {
                            dic.Add(song.filePath, song);

                        }
                    }

                    if(dic.ContainsKey(track.filePath) != true)
                    {
                        player.mediaLibrary.Add(track);

                    }

                }

            }
            
        }


        private void getTrackTags(Track track, int i,string[] songFileName, string[] songFilePath)
        {
            track.fileName = System.IO.Path.GetFileNameWithoutExtension(songFileName[i]);
            track.filePath = songFilePath[i];
            var tag = TagLib.File.Create(track.filePath);
            track.artist = tag.Tag.FirstPerformer;
            track.duration = tag.Properties.Duration;
            track.genre = tag.Tag.FirstGenre;
            track.album = tag.Tag.Album;
            track.songName = tag.Tag.Title;

        }


        private void pauseButton_Click(object sender, RoutedEventArgs e)
        {
            if
                (player.playState == UnderwaterAudioMediaPlayer.playerPlaying)
            {
                pausePlaying();

            }            
        }
      
        private void swapShuffleIcons()
        {
            if (player.isShuffle)
            {
                shuffleButton.Visibility = Visibility.Visible;
                shuffleButtonOn.Visibility = Visibility.Collapsed;
            }
            else if(player.isShuffle == false)
            {
                shuffleButton.Visibility = Visibility.Collapsed;
                shuffleButtonOn.Visibility = Visibility.Visible;
            }
            
        }
        private void toggleShuffle()
        {
            if(player.isShuffle)
            {
                swapShuffleIcons();
                player.isShuffle = false;
                
                

            }
            else if(player.isShuffle == false)
            {

                swapShuffleIcons();
         
                player.isShuffle = true;
                




            }

        }
        private void toggleRepeat()
        {
            if (player.isRepeat)
            {
                
                player.isRepeat = false;
                repeatButton.Visibility = Visibility.Visible;
                repeatButtonOn.Visibility = Visibility.Collapsed;

            }
            else if (player.isRepeat != true)
            {
                
                player.isRepeat = true;
                repeatButton.Visibility = Visibility.Collapsed;
                repeatButtonOn.Visibility = Visibility.Visible;


            }

        }


        private void handlePlayButtonClick()
        {

            player.play();
            swapPlayToPauseButton();
           
            
        }

        private void Slider_OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var slider = (Slider)sender;
                Point position = e.GetPosition(slider);
                double d = 1.0d / slider.ActualWidth * position.X;
                var p = slider.Maximum * d;
                slider.Value = p;
                if(player.Source != null)
                {
                    trackProgressBar.Value = trackProgressBar.Value = (player.Position.TotalSeconds / player.NaturalDuration.TimeSpan.TotalSeconds) * 100;
                }
                else
                {
                    trackProgressSlider.Maximum = 100;
                    trackProgressBar.Value = trackProgressSlider.Value;
                }
            }
        }

        private void playlistBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
          
        }

        private void playlistBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

           
        }
        private void savePlaylist(string playlistName)
        {

            System.IO.StreamWriter playlist = new System.IO.StreamWriter(playlistName + ".plst");
           
            foreach (var track in player.mediaLibrary)
            {                 
                playlist.WriteLine(track.filePath);
            }
            playlist.Close();

        }

        private void closeApp()
        {
            savePlaylist("library");
            Close();

        }
        // private void loadMusicFromDefaultMusicFolderIntoLibraryOnProgramStart()
        //{
        //    string defaultMusicFolderPath = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)).FullName;
        //    string[] filesInDefaultDirectory = Directory.GetFiles(defaultMusicFolderPath, "*.*").Where(file => file.ToLower().EndsWith(".mp3") || file.ToLower().EndsWith(".wav") || file.ToLower().EndsWith(".flac") || file.ToLower().EndsWith(".m4a") || file.ToLower().EndsWith(".mp4") || file.ToLower().EndsWith(".wma") || file.ToLower().EndsWith(".aac")).ToArray(); //|| file.ToLower().EndsWith(".EXT")

        //    for (int i=0;i < filesInDefaultDirectory.Length; i++)
        //    {
        //        Track track = new Track();
        //        track.fileName = System.IO.Path.GetFileNameWithoutExtension(filesInDefaultDirectory[i]);
        //        track.filePath = filesInDefaultDirectory[i];
        //        var tag = TagLib.File.Create(track.filePath);
        //        track.artist = tag.Tag.FirstPerformer;
        //        track.duration = tag.Properties.Duration;
        //        track.genre = tag.Tag.FirstGenre;
        //        track.album = tag.Tag.Album;
        //        track.songName = tag.Tag.Title;
        //        musicLibrary.Add(track.fileName.ToString(), track); // sets up library database
        //        playlistBox.Items.Add(track.fileName); // this is just a representation of whats in the library database. 

               


        //    }
        //    //libraryListView.ItemsSource = musicLibrary;


        //}

        //public DispatcherTimer timer1 = new DispatcherTimer(DispatcherPriority.Render);
        private void showDelphinPage()
        {
            libraryPage.Visibility = Visibility.Collapsed;
            playlistPage.Visibility = Visibility.Collapsed;
            delphinPage.Visibility = Visibility.Visible;
            tabTitleLabel.Content = "Delphin";
            tabTitleLabel.Visibility = Visibility.Visible;
            slideRightToLeftAnimation(titleLabelBar);
        }
        private void delphinNavTab_Click(object sender, RoutedEventArgs e)
        {
            showDelphinPage();
        }
        private void slideRightToLeftAnimation(UIElement element)
        {
            element.Visibility = Visibility.Visible;
            TranslateTransform trans = new TranslateTransform();
            element.RenderTransform = trans;
            DoubleAnimation anim1 = new DoubleAnimation(mainWindow.ActualWidth, 0, TimeSpan.FromMilliseconds(350));
            trans.BeginAnimation(TranslateTransform.XProperty, anim1);
            
        }
        
    private void showLibraryPage()
        {
            libraryPage.Visibility = Visibility.Visible;
            delphinPage.Visibility = Visibility.Collapsed;
            playlistPage.Visibility = Visibility.Collapsed;
            tabTitleLabel.Content = "Library";
            slideRightToLeftAnimation(titleLabelBar);
            


        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            showLibraryPage();
        }

       
        private void repeatButton_Click(object sender, RoutedEventArgs e)
        {
            toggleRepeat();
            if (player.isShuffle)
            {
                toggleShuffle();
            }
        }
        private void hideDelphinPage()
        {

        }
        private void showPlaylistPage()
        {
            libraryPage.Visibility = Visibility.Collapsed;
            delphinPage.Visibility = Visibility.Collapsed;
            playlistPage.Visibility = Visibility.Visible;
            tabTitleLabel.Content = "Playlists";
            slideRightToLeftAnimation(titleLabelBar);

        }
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            showPlaylistPage();
        }

        private void addToPlaylistButton_Click(object sender, RoutedEventArgs e)
        {                 

        }

        private void playlistListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //timer.Stop();
            //if (playlistListBox.SelectedItem != null)
            //{
            //    loadSongIntoPlayer(new System.Uri(getFilePathFromSelectedFile(playlistListBox.SelectedItem.ToString(), musicLibrary)));
            //    startPlaying();
            //}
        }

        private Track[] convertDictionaryValuesToArray(Dictionary<String, Track> dictionary)
        {
            return (new List<Track>(dictionary.Values)).ToArray();
        }

        private String[] convertDictionaryKeysToArray(Dictionary<String, Track> dictionary)
        {
            return (new List<string>(dictionary.Keys)).ToArray();


        }

        private string pickRandomSong(List<Track> playlist)
        {
            Random random = new Random();

            return playlist[random.Next(playlist.Count)].ToString();

        }






        private void closeButtonBackground_MouseEnter(object sender, MouseEventArgs e)
        {
            var element = closeButtonBackground as Border;
            element.Background = new SolidColorBrush(Color.FromArgb(100, 200, 0, 0));
        }

        private void closeButtonBackground_MouseLeave(object sender, MouseEventArgs e)
        {
           
            var element = closeButtonBackground as Border;
            element.Background = null;
            closeButton.Width = 14;
            closeButton.Height = 14;
        }




        private void titleLabelBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            showLibraryPage();
        }


        private void closeButtonBackground_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            var element = closeButtonBackground as Border;
            element.Background = new SolidColorBrush(Color.FromArgb(100, 200, 0, 0));
            closeButton.Width = 10;
            closeButton.Height = 10;

        }

        private void closeButtonBackground_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            closeApp();
        }



        private void closeButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            closeButton.Width = 10;
            closeButton.Height = 10;
        }

        private void playButton_Click(object sender, RoutedEventArgs e)
        {
            player.loadSongIntoPlayer(player.currentMedia);
            handlePlayButtonClick();          

        }
    }
}
