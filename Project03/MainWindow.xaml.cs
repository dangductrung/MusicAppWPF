using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;


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
            BindingList<MenuItem> _list = new BindingList<MenuItem>();

            public BindingList<MenuItem> List
            {
                get => _list;
                set
                {
                    _list = value;
                    RaiseEventChanged();
                }
            }

            public string Name
            {
                get => _name;
                set
                {
                    _name = value;
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
            //Register hook
            Subscribe();

            root.Add(childItem1);
            //Load favorite playlist from text file
            if (File.Exists("../../FavoritePlaylist.txt"))
            {
                LoadFavoritePlaylistWithTextFile("../../FavoritePlaylist.txt");
            }

            LoadCurrentPlaylistWithTextFile("../../LastPlaylist.txt");

        }
        /// <summary>
        /// TODO: Handle when people using slider bar to change time of current media
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void changeValueSliderbar(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Math.Abs(currentMediaPlayer.Position.TotalSeconds - SliderMusicBar.Value) > 1)
            {
                if (currentMediaPlayer != null)
                {
                    TimeSpan span = new TimeSpan(0, (int)(SliderMusicBar.Value / 60), (int)(SliderMusicBar.Value % 60));
                    currentTime.Content = String.Format("{0}", span.ToString(@"mm\:ss"));
                    currentMediaPlayer.Position = span;
                }
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

            changeRepeat();
        }
        /// <summary>
        /// TODO: Change status and update UI of shuffle button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void shuffleButtonClick(object sender, RoutedEventArgs e)
        {
            isShuffle = !isShuffle;
            changeShuffle();
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
                if (music.Title != currentListName && currentMediaPlayer.Source != music.media.Source && currentMediaPlayer != null)
                {
                    UpdatePastMusic();
                    currentMediaPlayer.Stop();
                    changeCurrentMedia(music);
                    PlayCurrentMusic();
                }
            }
        }

        #region Handle

        void changeRepeat()
        {
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

        void changeShuffle()
        {
            if (isShuffle)
            {
                ShuffleIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.ShuffleVariant;
            }
            else
            {
                ShuffleIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.ShuffleDisabled;
            }
        }

        void UpdatePastMusic()
        {
            pastMusicIndex = findIndex(currentMediaPlayer);
        }
        void changeCurrentMedia(MenuItem music)
        {
            if (currentMediaPlayer != null)
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
            _timer.Stop();
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

        void changePlayIcon()
        {
            if(isPlay)
            {
                playMusic.Kind = MaterialDesignThemes.Wpf.PackIconKind.PauseCircle;
            }
            else
            {
                playMusic.Kind = MaterialDesignThemes.Wpf.PackIconKind.PlayCircle;
            }
        }

        void PlayCurrentMusic()
        {
            playMusic.Kind = MaterialDesignThemes.Wpf.PackIconKind.PauseCircle;
            if (currentMediaPlayer.Source != null)
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
                if (indexCurrent < (root[0].Items.Count - 1))
                {
                    if (isShuffle == false)
                    {
                        NextMusic(indexCurrent);
                    }
                    else
                    {
                        RandomMusic();
                    }
                }
                else
                {
                    PlayMusicWithIndex(0);
                }

            }
        }

        void BackwardMusic()
        {
            if(currentMediaPlayer.Source != null)
            {
                int indexCurrent = findIndex(currentMediaPlayer);
                if (pastMusicIndex >= 0 && pastMusicIndex <= root[0].Items.Count)
                {
                    root[0].Items[indexCurrent].IsSelectedTreeView = false;
                    root[0].Items[pastMusicIndex].IsSelectedTreeView = true;
                    PlayCurrentMusic();
                    currentMediaPlayer.Play();
                }
            }
        }

        bool isExistPlaylist(string name)
        {
            bool result = false;
            foreach (var element in favoritePlaylist)
            {
                if (element.Name == name)
                {
                    result = true;
                }
            }
            return result;
        }

        void SaveCurrentPlaylistWithTextFile(string url)
        {

            var write = new StreamWriter(url);
            if (root[0].Items.Count > 0)
            {
                write.WriteLine($"IsShuffle={isShuffle.ToString()}");
                write.WriteLine($"IsPlay={isPlay.ToString()}");
                write.WriteLine($"RepeatStatus={repeatStatus.ToString()}");
                write.WriteLine($"PastMusicIndex={pastMusicIndex.ToString()}");
                write.WriteLine($"CurrentMediaSelectedIndex={findIndex(currentMediaPlayer).ToString()}");
                write.WriteLine($"CurrentPositionTime={currentMediaPlayer.Position.ToString()}");
                write.WriteLine($"Count={root[0].Items.Count.ToString()}");
                foreach (var element in root[0].Items)
                {
                    write.WriteLine($"Source={element.media.Source}");
                    write.WriteLine($"Title={element.Title}");
                    write.WriteLine($"IsSelected={element.IsSelected.ToString()}");
                    write.WriteLine($"IsSelectedTreeView={element.IsSelectedTreeView.ToString()}");
                }
            }
            write.Close();
        }

        void LoadCurrentPlaylistWithTextFile(string url)
        {
            var reader = new StreamReader(url);
            string temp = "";
            //Read shuffle status
            temp = reader.ReadLine();
            if(temp != null)
            {
                //Split key with value
                string[] tokens = temp.Split('=');
                isShuffle = bool.Parse(tokens[1]);
                changeShuffle();

                //Read play status
                temp = "";
                temp = reader.ReadLine();
                //Split key with value
                tokens = temp.Split('=');
                isPlay = bool.Parse(tokens[1]);
                changePlayIcon();

                //Read repeat status
                temp = "";
                temp = reader.ReadLine();
                //Split key with value
                tokens = temp.Split('=');
                repeatStatus = int.Parse(tokens[1]);
                changeRepeat();

                //Read past music index 
                temp = "";
                temp = reader.ReadLine();
                //Split key with value
                tokens = temp.Split('=');
                pastMusicIndex = int.Parse(tokens[1]);

                //Read current media selected index
                temp = "";
                temp = reader.ReadLine();
                //Split key with value
                tokens = temp.Split('=');
                int currentIndex = int.Parse(tokens[1]);

                //Read current position time
                temp = "";
                temp = reader.ReadLine();
                //Split key with value
                tokens = temp.Split('=');

                string timeSpanString = tokens[1];
                string[] timeSpanTokens = timeSpanString.Split(':');
                string[] second = timeSpanTokens[2].Split('.');
                TimeSpan currentPosition = new TimeSpan(int.Parse(timeSpanTokens[0]), int.Parse(timeSpanTokens[1]), int.Parse(second[0]));

                //Read size
                temp = "";
                temp = reader.ReadLine();
                //Split key with value
                tokens = temp.Split('=');
                int count = int.Parse(tokens[1]);

                root[0].Items.Clear();
                for (int i = 0; i < count; i++)
                {
                    string source = "";
                    string title = "";
                    bool isSelected = false;
                    bool isSelectedTreeView = false;
                    //Read source
                    temp = "";
                    temp = reader.ReadLine();
                    //Split key with value
                    tokens = temp.Split('=');
                    source = tokens[1];

                    //Read title;
                    temp = "";
                    temp = reader.ReadLine();
                    //Split key with value
                    tokens = temp.Split('=');
                    title = tokens[1];

                    //Read select status
                    temp = "";
                    temp = reader.ReadLine();
                    //Split key with value
                    tokens = temp.Split('=');
                    isSelected = bool.Parse(tokens[1]);

                    //Read treeview status
                    temp = "";
                    temp = reader.ReadLine();
                    //Split key with value
                    tokens = temp.Split('=');
                    isSelectedTreeView = bool.Parse(tokens[1]);

                    MenuItem element = new MenuItem();
                    element.media = new MediaPlayer();
                    element.media.Open(new Uri(source));
                    element.Title = title;
                    element.IsSelected = isSelected;
                    element.IsSelectedTreeView = isSelectedTreeView;

                    root[0].Items.Add(element);
                }

                changeCurrentMedia(root[0].Items[currentIndex]);
                PlayCurrentMusic();
                SliderMusicBar.Value = currentPosition.TotalSeconds;

                root[0].Items[currentIndex].IsSelectedTreeView = true;

                reader.Close();
            }
            
        }

        void SaveCurrentPlaylistWithXMLFile(string url)
        {
            var doc = new XmlDocument();

            var root = doc.CreateElement("CurrentPlaylist");
            root.SetAttribute("IsShuffle", isShuffle.ToString());
            root.SetAttribute("IsPlay", isPlay.ToString());
            root.SetAttribute("RepeatStatus", repeatStatus.ToString());
            root.SetAttribute("PastMusicIndex", pastMusicIndex.ToString());
            root.SetAttribute("CurrentMediaSelectedIndex", findIndex(currentMediaPlayer).ToString());
            root.SetAttribute("CurrentPositionTime", currentMediaPlayer.Position.ToString());

            var playlist = doc.CreateElement("Playlist");
            root.AppendChild(playlist);
            foreach(var element in this.root[0].Items)
            {
                var line = doc.CreateElement("Music");
                line.SetAttribute("Source", element.media.Source.ToString());
                line.SetAttribute("Title", element.Title);
                line.SetAttribute("IsSelected", element.IsSelected.ToString());
                line.SetAttribute("IsSelectedTreeView", element.IsSelectedTreeView.ToString());
                playlist.AppendChild(line);
            }
            doc.AppendChild(root);
            doc.Save(url);
        }

        void LoadCurrentPlaylistWithXMLFile(string url)
        {
            var doc = new XmlDocument();
            doc.Load(url);

            var root = doc.DocumentElement;
            
            isShuffle = bool.Parse(root.Attributes["IsShuffle"].Value);
            isPlay = bool.Parse(root.Attributes["IsPlay"].Value);
            repeatStatus = int.Parse(root.Attributes["RepeatStatus"].Value);
            pastMusicIndex = int.Parse(root.Attributes["PastMusicIndex"].Value);

            int currentIndex = int.Parse(root.Attributes["CurrentMediaSelectedIndex"].Value);
            string timeSpanString = root.Attributes["CurrentPositionTime"].Value;
            string[] timeSpanTokens = timeSpanString.Split(':');
            string[] second = timeSpanTokens[2].Split('.');
            TimeSpan currentPosition = new TimeSpan(int.Parse(timeSpanTokens[0]), int.Parse(timeSpanTokens[1]), int.Parse(second[0]));

            foreach (XmlNode node in root.FirstChild.ChildNodes)
            {
                MenuItem element = new MenuItem();
                element.media = new MediaPlayer();
                element.media.Open(new Uri(node.Attributes["Source"].Value));
                element.Title = node.Attributes["Title"].Value;
                element.IsSelected = bool.Parse(node.Attributes["IsSelected"].Value);
                element.IsSelectedTreeView = bool.Parse(node.Attributes["IsSelectedTreeView"].Value);
                this.root[0].Items.Add(element);
            }
            changeCurrentMedia(this.root[0].Items[currentIndex]);
            PlayCurrentMusic();
            SliderMusicBar.Value = currentPosition.TotalSeconds;
        }

        void SaveFavoritePlaylistWithTextFile(string url)
        {
            var write = new StreamWriter(url);
            write.WriteLine($"CountPlaylist={favoritePlaylist.Count.ToString()}");
            foreach (var playlist in favoritePlaylist)
            {
                write.WriteLine($"CountMedia={playlist.List.Count.ToString()}");
                write.WriteLine($"Name={playlist.Name}");
                foreach (var element in playlist.List)
                {
                    write.WriteLine($"Title={element.Title}");
                    write.WriteLine($"Source={element.media.Source}");
                }
            }
            write.Close();
        }

        void LoadFavoritePlaylistWithTextFile(string url)
        {
            var reader = new StreamReader(url);
            //Read favorite list size
            string temp = reader.ReadLine();
            if(temp != null)
            {
                string[] tokens = temp.Split('=');
                int favoriteListSize = int.Parse(tokens[1]);
                for (int i = 0; i < favoriteListSize; i++)
                {
                    var playlist = new FavoritePlaylist();
                    //Read playlist size
                    temp = "";
                    temp = reader.ReadLine();
                    tokens = temp.Split('=');
                    int count = int.Parse(tokens[1]);
                    //Read playlist name
                    temp = "";
                    temp = reader.ReadLine();
                    tokens = temp.Split('=');
                    playlist.Name = tokens[1];

                    for (int j = 0; j < count; j++)
                    {
                        var element = new MenuItem();
                        element.media = new MediaPlayer();
                        string title = "";
                        string source = "";
                        //Read title
                        temp = "";
                        temp = reader.ReadLine();
                        tokens = temp.Split('=');
                        title = tokens[1];
                        //Read source 
                        temp = "";
                        temp = reader.ReadLine();
                        tokens = temp.Split('=');
                        source = tokens[1];

                        element.Title = title;
                        element.media.Open(new Uri(source));
                        playlist.List.Add(element);
                    }
                    favoritePlaylist.Add(playlist);
                }
            }
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
                root[0].IsSelected = false;
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
            else
            {
                bool check = true;
                for (int i = 0; i < root[0].Items.Count(); i++)
                {
                    if(root[0].Items[i].IsSelected == false)
                    {
                        check = false;
                        break;
                    }
                }
                root[0].IsSelected = check;
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
            var screen = new FavoriteListWindow(favoritePlaylist);
            if (screen.ShowDialog() == true)
            {
                root[0].Items.Clear();
                foreach (var element in screen.choose.List)
                {
                    root[0].Items.Add(element);
                }
            }
        }

        private void BackwardButtonClick(object sender, RoutedEventArgs e)
        {
            BackwardMusic();
        }

        private void ForwardButtonClick(object sender, RoutedEventArgs e)
        {
            ForwardMusic();
        }

        private void AddFavoriteButtonClick(object sender, RoutedEventArgs e)
        {
            if (root[0].Items.Count > 0)
            {
                var screen = new AddFavoriteList("Favorite Playlist");
                if (screen.ShowDialog() == true)
                {
                    if (!isExistPlaylist(screen.Name))
                    {
                        var playlist = new FavoritePlaylist();
                        playlist.Name = screen.Name;
                        foreach (var element in root[0].Items)
                        {
                            playlist.List.Add(element);
                        }
                        favoritePlaylist.Add(playlist);
                    }
                    else
                    {
                        MessageBox.Show("Playlist name is existed", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                }
            }
        }

        private void SavePlaylistButtonClick(object sender, RoutedEventArgs e)
        {
            var screen = new SaveFileDialog();
            if(screen.ShowDialog() == true)
            {
                string path = screen.FileName;
                string[] tokens = path.Split('.');
                if(tokens[tokens.Count() - 1] == "txt")
                {
                    SaveCurrentPlaylistWithTextFile(screen.FileName);
                    MessageBox.Show("Successful!", "Notification", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if(tokens[tokens.Count() - 1] == "xml")
                {
                    SaveCurrentPlaylistWithXMLFile(screen.FileName);
                    MessageBox.Show("Successful!", "Notification", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void OpenPlaylistButtonClick(object sender, RoutedEventArgs e)
        {
            var screen = new OpenFileDialog();
            if (screen.ShowDialog() == true)
            {
                string path = screen.FileName;
                string[] tokens = path.Split('.');
                if (tokens[tokens.Count() - 1] == "txt")
                {
                    LoadCurrentPlaylistWithTextFile(screen.FileName);
                    MessageBox.Show("Successful!", "Notification", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (tokens[tokens.Count() - 1] == "xml")
                {
                    LoadCurrentPlaylistWithXMLFile(screen.FileName);
                    MessageBox.Show("Successful!", "Notification", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            //Save favorite playlist into text file
            SaveFavoritePlaylistWithTextFile("../../FavoritePlaylist.txt");
            //Save last playlist
            SaveCurrentPlaylistWithTextFile("../../LastPlaylist.txt");
            Unsubscribe();
        }

        //Hook keyboard
        private Gma.System.MouseKeyHook.IKeyboardMouseEvents _hook;

        public void Subscribe()
        {
            _hook = Gma.System.MouseKeyHook.Hook.GlobalEvents();
            _hook.KeyUp += _hook_KeyUp1;
        }

        private void _hook_KeyUp1(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            
            if(e.Control && (e.KeyCode == System.Windows.Forms.Keys.P))
            {
                if(isPlay)
                {
                    PauseCurrentMusic();
                }
                else
                {
                    PlayCurrentMusic();
                }
            }
            else if(e.Control && (e.KeyCode == System.Windows.Forms.Keys.F))
            {
                ForwardMusic();
            }
            else if (e.Control && (e.KeyCode == System.Windows.Forms.Keys.B))
            {
                BackwardMusic();
            }
        }
        public void Unsubscribe()
        {
            _hook.KeyUp -= _hook_KeyUp1;
            _hook.Dispose();
        }

    }
}
