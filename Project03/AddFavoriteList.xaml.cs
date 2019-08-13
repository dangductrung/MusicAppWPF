using System;
using System.Collections.Generic;
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

namespace Project03
{
    /// <summary>
    /// Interaction logic for AddFavoriteList.xaml
    /// </summary>
    public partial class AddFavoriteList : Window
    {
        public string Name = "";
        public AddFavoriteList(string title)
        {
            InitializeComponent();
            this.Title = title;
        }

        private void OKButtonClick(object sender, RoutedEventArgs e)
        {
            if(favoriteListName.Text != "")
            {
                Name = favoriteListName.Text;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Invalid!", "Invalid", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }
    }
}
