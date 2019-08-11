using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Project03
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Model
        bool isShuffle = false;
        bool isPlay = false;
        int repeatStatus = 1;
        //Store name of current playlist
        string currentListName = "Current Playlist";
        //Store current media playing
        public MediaPlayer currentMediaPlayer = new MediaPlayer();
        //Timer for count music time
        DispatcherTimer _timer = new DispatcherTimer();
        //Store past music after Forward
        public int pastMusicIndex = -1;
        //Store root of treeview
        public BindingList<MenuItem> root = new BindingList<MenuItem>();
        //Store favorite playlist
        public BindingList<FavoritePlaylist> favoritePlaylist = new BindingList<FavoritePlaylist>();
        static class RepeatStatus
        {
            //Repeat unlimited list
            public static int isRepeatList = 0;
            //Repeat one time
            public static int isRepeatOff = 1;
            //Repeat current music unlimited
            public static int isRepeatOne = 2;
        }

        /// <summary>
        /// Contain Favorite Playlist
        /// </summary>
        public class FavoritePlaylist : INotifyPropertyChanged
        {
            string _name;
            BindingList<String> _titleList;
            BindingList<String> _urlList;
            public string Name
            {
                get => _name;
                set
                {
                    _name = value;
                    RaiseEventChanged();
                }
            }
            public BindingList<String> TitleList
            {
                get => _titleList;
                set
                {
                    _titleList = value;
                    RaiseEventChanged();
                }
            }

            public BindingList<String> UrlList
            {
                get => _urlList;
                set
                {
                    _urlList = value;
                    RaiseEventChanged();
                }
            }
            public event PropertyChangedEventHandler PropertyChanged;
            void RaiseEventChanged([CallerMemberName] string property = "")
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
            }
        }
        public class MenuItem : INotifyPropertyChanged
        {
            public MenuItem()
            {
                this.Items = new ObservableCollection<MenuItem>();
            }
            private MediaPlayer _media;
            private string _title;
            private bool _isSelected;
            private bool _isSelectedTreeView;
            public ObservableCollection<MenuItem> Items
            {
                get => _items;
                set
                {
                    _items = value;
                    RaiseEvent();
                }
            }

            public bool IsSelected
            {
                get => _isSelected;
                set
                {
                    _isSelected = value;
                    RaiseEvent();
                }
            }

            public string Title
            {
                get => _title;
                set
                {
                    _title = value;
                    RaiseEvent();
                }
            }

            public MediaPlayer media
            {
                get => _media;
                set
                {
                    _media = value;
                    RaiseEvent();
                }
            }

            public bool IsSelectedTreeView
            {
                get => _isSelectedTreeView;
                set
                {
                    _isSelectedTreeView = value;
                    RaiseEvent();
                }
            }

            private ObservableCollection<MenuItem> _items;

            public event PropertyChangedEventHandler PropertyChanged;
            void RaiseEvent([CallerMemberName]string propertyName = "")
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            }
        }
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            //Create root of currentListMedia
            currentPlayList.ItemsSource = root;
            MenuItem childItem1 = new MenuItem() { Title = currentListName, IsSelected = false, IsSelectedTreeView = false };
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += _timer_Tick;
            root.Add(childItem1);
        }
        /// <summary>
        /// TODO: Handle when people using slider bar to change time of current media
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void changeValueSliderbar(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (currentMediaPlayer != null)
            {
                TimeSpan span = new TimeSpan(0, (int)(SliderMusicBar.Value / 60), (int)(SliderMusicBar.Value % 60));
                currentTime.Content = String.Format("{0}", span.ToString(@"mm\:ss"));
                currentMediaPlayer.Position = span;
            }
        }
        /// <summary>
        /// TODO: Change status and update UI of repeat button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void repeatButtonClick(object sender, RoutedEventArgs e)
        {
            repeatStatus = ++repeatStatus % 3;

            if (repeatStatus == RepeatStatus.isRepeatList)
            {
                RepeatIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.Repeat;
            }
            else if (repeatStatus == RepeatStatus.isRepeatOff)
            {
                RepeatIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.RepeatOff;
            }
            else
            {
                RepeatIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.RepeatOne;
            }
        }
        /// <summary>
        /// TODO: Change status and update UI of shuffle button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void shuffleButtonClick(object sender, RoutedEventArgs e)
        {
            isShuffle = !isShuffle;
            if (isShuffle)
            {
                ShuffleIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.ShuffleVariant;
            }
            else
            {
                ShuffleIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.ShuffleDisabled;
            }
        }
        /// <summary>
        /// TODO: Handle when user click media file in current media list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void selectedMusic(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var music = currentPlayList.SelectedItem as MenuItem;
            if (music != null)
            {
                if (currentMediaPlayer != null && music.Title != currentListName)
                {
                    UpdatePastMusic();
                    StopCurrentMedia();
                }
                if (music.Title != currentListName)
                {
                    changeCurrentMedia(music);
                }
            }
        }

        #region Handle

        void UpdatePastMusic()
        {
            pastMusicIndex = findIndex(currentMediaPlayer);
        }
        void changeCurrentMedia(MenuItem music)
        {
            if(currentMediaPlayer != null)
            {
                UpdatePastMusic();
            }
            currentMediaPlayer = music.media;
            currentMediaNameTextBlock.Text = music.Title;
            currentMediaPlayer.Play();
            currentTime.Content = String.Format("{0}", currentMediaPlayer.Position.ToString(@"mm\:ss"));
            while (!currentMediaPlayer.NaturalDuration.HasTimeSpan)
            {

            }
            fullTime.Content = String.Format("{0}", currentMediaPlayer.NaturalDuration.TimeSpan.ToString(@"mm\:ss"));
            currentMediaPlayer.Stop();

            TimeSpan end = currentMediaPlayer.NaturalDuration.TimeSpan;

            SliderMusicBar.Maximum = end.TotalSeconds;
            SliderMusicBar.Value = 0;
            currentMediaPlayer.MediaEnded += CurrentMediaPlayer_MediaEnded;
        }

        void StopCurrentMedia()
        {
            _timer.Stop();
            currentMediaPlayer.Stop();
            SliderMusicBar.Value = 0;
            SliderMusicBar.Maximum = 0;
            currentMediaNameTextBlock.Text = "";
            fullTime.Content = "00:00";
            playMusic.Kind = MaterialDesignThemes.Wpf.PackIconKind.PlayCircle;
            isPlay = false;
        }

        int findIndex(MediaPlayer media)
        {
            int result = 0;
            for (int i = 0; i < root[0].Items.Count; i++)
            {
                if (root[0].Items[i].media == media)
                {
                    result = i;
                    break;
                }
            }
            return result;
        }

        void PlayCurrentMusic()
        {
            playMusic.Kind = MaterialDesignThemes.Wpf.PackIconKind.PauseCircle;
            if (currentMediaPlayer != null)
            {
                currentMediaPlayer.Play();
                _timer.Start();
            }
            isPlay = true;
        }

        void AddMediaIntoRoot(String url)
        {
            //Create child to add into CurrentListMedia
            MenuItem item = new MenuItem();
            item.media = new MediaPlayer();
            item.media.Open(new Uri(url));
            item.IsSelected = root[0].IsSelected;
            item.IsSelectedTreeView = false;
            //Split path to get name of media
            string[] tokens = url.Split(new String[] { "\\", "." }, StringSplitOptions.RemoveEmptyEntries);
            item.Title = tokens[tokens.Count() - 2];
            //Check if media is exist in currentListMedia
            if (!isExistInCurrentList(item.media))
            {
                root[0].Items.Add(item);
            }
            //Handle if dont have any media but play music is on
            if (currentMediaPlayer == null)
            {
                isPlay = false;
                playMusic.Kind = MaterialDesignThemes.Wpf.PackIconKind.PlayCircle;
            }
        }

        void PauseCurrentMusic()
        {
            playMusic.Kind = MaterialDesignThemes.Wpf.PackIconKind.PlayCircle;
            if (currentMediaPlayer != null)
            {
                currentMediaPlayer.Pause();
                _timer.Stop();
            }
            isPlay = false;
        } 

        void PlayMusicWithIndex(int index)
        {
            int indexCurrent = findIndex(currentMediaPlayer); 
            root[0].Items[indexCurrent].IsSelectedTreeView = false;
            root[0].Items[index].IsSelectedTreeView = true;
            PlayCurrentMusic();
            currentMediaPlayer.Play();
        }

        void RandomMusic()
        {
            int indexCurrent = findIndex(currentMediaPlayer);
            //Create random number generator 
            Random rng = new Random();
            //Update past position music
            UpdatePastMusic();
            int newIndex = rng.Next(indexCurrent + 1, root[0].Items.Count());
            PlayMusicWithIndex(newIndex);
        }

        void NextMusic(int indexCurrent)
        {
            //Update past music before forward 
            UpdatePastMusic();

            root[0].Items[indexCurrent].IsSelectedTreeView = false;
            root[0].Items[indexCurrent + 1].IsSelectedTreeView = true;
            PlayCurrentMusic();
            currentMediaPlayer.Play();

        }

        void ForwardMusic()
        {
            if (repeatStatus == RepeatStatus.isRepeatOne)
            {
                //Repeat music
                currentMediaPlayer.Position = new TimeSpan(0, 0, 0);
                SliderMusicBar.Value = 0;
                currentMediaPlayer.Play();
            }
            else if (repeatStatus == RepeatStatus.isRepeatOff)
            {
                //Get current index music of playlist
                int indexCurrent = findIndex(currentMediaPlayer);
                //Next music
                if (isShuffle == false && indexCurrent < (root[0].Items.Count - 1))
                {
                    NextMusic(indexCurrent);
                }//Random music
                else if (isShuffle == true && indexCurrent < (root[0].Items.Count - 1))
                {
                    RandomMusic();
                }
            }
            else if (repeatStatus == RepeatStatus.isRepeatList)
            {
                //Get current index music of playlist
                int indexCurrent = findIndex(currentMediaPlayer);
                ////Turn to next music
                //if (isShuffle == false)
                //{
                //    if (indexCurrent < (root[0].Items.Count - 1))
                //    {
                //        NextMusic(indexCurrent);
                //    }
                //    else
                //    {
                //        PlayMusicWithIndex(0);
                //    }
                //}
                //else if (isShuffle == true)
                //{
                //    RandomMusic();
                //}

                if (indexCurrent < (root[0].Items.Count - 1))
                {
                    if (isShuffle == false)
                    {
                        NextMusic(indexCurrent);
                    }
                    else
                    {
                        PlayMusicWithIndex(0);
                    }
                }
                else
                {
                    PlayMusicWithIndex(0);
                }

            }
        }

        void BackwardMusic(int indexCurrent)
        {
            root[0].Items[indexCurrent].IsSelectedTreeView = false;
            root[0].Items[pastMusicIndex].IsSelectedTreeView = true;
            PlayCurrentMusic();
            currentMediaPlayer.Play();
        }
        #endregion
        private void CurrentMediaPlayer_MediaEnded(object sender, EventArgs e)
        {
            ForwardMusic();
        }

        /// <summary>
        /// TODO: Change theme mode
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void darkThemeMode(object sender, RoutedEventArgs e)
        {
            if (themeToggle.IsChecked == true)
            {
                var bc = new BrushConverter();
                playlistGrid.Background = (Brush)bc.ConvertFrom("#353b48");
                controlMusicGrid.Background = (Brush)bc.ConvertFrom("#2f3640");
            }
            else
            {
                var bc = new BrushConverter();
                playlistGrid.Background = (Brush)bc.ConvertFrom("#bdc3c7");
                controlMusicGrid.Background = (Brush)bc.ConvertFrom("#34495e");
            }
        }
        /// <summary>
        /// TODO: Delete element music of currentMediaList
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void deleteMusic(object sender, RoutedEventArgs e)
        {
            if (root[0].IsSelected)
            {
                StopCurrentMedia();
                root[0].Items.Clear();
            }
            else
            {
                for (int i = 0; i < root[0].Items.Count(); i++)
                {
                    if (root[0].Items[i].IsSelected)
                    {
                        if (root[0].Items[i].media == currentMediaPlayer)
                        {
                            StopCurrentMedia();
                        }
                        root[0].Items.Remove(root[0].Items[i]);
                    }
                }
            }
        }

        /// <summary>
        /// TODO: Handle play music button click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlayMusicButtonClick(object sender, RoutedEventArgs e)
        {
            isPlay = !isPlay;
            if (isPlay)
            {
                PlayCurrentMusic();
            }
            else
            {
                PauseCurrentMusic();
            }
        }
        /// <summary>
        /// TODO: Increase time of media is playing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _timer_Tick(object sender, EventArgs e)
        {
            currentTime.Content = String.Format("{0}", currentMediaPlayer.Position.ToString(@"mm\:ss"));
            ++SliderMusicBar.Value;
        }
        /// <summary>
        /// Check media is exist in currentMediaList
        /// </summary>
        /// <param name="media">media need check</param>
        /// <returns></returns>
        bool isExistInCurrentList(MediaPlayer media)
        {
            for (int i = 0; i < root[0].Items.Count(); i++)
            {
                if (root[0].Items[i].media.Source == media.Source)
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// TODO: Handle when user choose media file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddMediaButtonClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "MP3 files (*.mp3)|*.mp3|All files (*.*)|*.*";


            if (openFileDialog.ShowDialog() == true)
            {
                AddMediaIntoRoot(openFileDialog.FileName);
            }
        }

        private void checkBoxChanged(object sender, RoutedEventArgs e)
        {
            if (root[0].IsSelected)
            {
                for (int i = 0; i < root[0].Items.Count(); i++)
                {
                    root[0].Items[i].IsSelected = true;
                }
            }
        }

        private void checkBoxUncheckedChanged(object sender, RoutedEventArgs e)
        {

            if (!root[0].IsSelected)
            {
                for (int i = 0; i < root[0].Items.Count(); i++)
                {
                    root[0].Items[i].IsSelected = false;
                }
            }
        }

        private void OpenFolderMediaButtonClick(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog open = new System.Windows.Forms.FolderBrowserDialog();
            open.ShowDialog();
            if (open.SelectedPath != "")
            {
                var directories = Directory.GetFiles(open.SelectedPath);

                foreach (var directory in directories)
                {
                    if (directory.Contains(".mp3"))
                    {
                        AddMediaIntoRoot(directory);
                    }
                }
                //Handle if dont have any media but play music is on
                if (currentMediaPlayer == null)
                {
                    isPlay = false;
                    playMusic.Kind = MaterialDesignThemes.Wpf.PackIconKind.PlayCircle;
                }
            }
        }

        private void FavoritePlaylistClick(object sender, RoutedEventArgs e)
        {
            var screen = new FavoriteListWindow();
            screen.ShowDialog();
        }

        private void BackwardButtonClick(object sender, RoutedEventArgs e)
        {
            //Get current index music of playlist
            int indexCurrent = findIndex(currentMediaPlayer);
            if(indexCurrent > 0)
            {
                BackwardMusic(indexCurrent);
            }
        }

        private void ForwardButtonClick(object sender, RoutedEventArgs e)
        {
            ForwardMusic();
        }

        private void AddFavoriteButtonClick(object sender, RoutedEventArgs e)
        {
            var screen = new AddFavoriteList();
            screen.ShowDialog();
        }
    }
}
