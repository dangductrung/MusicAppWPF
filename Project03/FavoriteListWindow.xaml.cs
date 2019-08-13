using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static Project03.MainWindow;

namespace Project03
{
    /// <summary>
    /// Interaction logic for FavoriteListWindow.xaml
    /// </summary>
    public partial class FavoriteListWindow : Window
    {
        BindingList<FavoritePlaylist> tempPlaylist = new BindingList<FavoritePlaylist>();
        public FavoritePlaylist choose = new FavoritePlaylist();
        public FavoriteListWindow(BindingList<FavoritePlaylist> playlist)
        {
            InitializeComponent();
            tempPlaylist = playlist;
            FavoriteList.ItemsSource = tempPlaylist;
            FavoriteList.SelectionChanged += FavoriteListSelected;
        }

        private void FavoriteListSelected(object sender, RoutedEventArgs e)
        {
            if(FavoriteList.SelectedIndex >= 0)
            {
                MusicList.ItemsSource = tempPlaylist[FavoriteList.SelectedIndex].List;
            }
        }

        private void deletePlaylist(object sender, RoutedEventArgs e)
        {
            if(FavoriteList.SelectedIndex >= 0)
            {
                tempPlaylist.Remove(tempPlaylist[FavoriteList.SelectedIndex]);
                FavoriteList.SelectedIndex = -1;
            }
        }

        private void ChoosePlaylistButtonClick(object sender, RoutedEventArgs e)
        {
            if (FavoriteList.SelectedIndex >= 0)
            {
                choose = tempPlaylist[FavoriteList.SelectedIndex];
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Nothing was chosen!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
