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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ELearningCrawlerGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel ViewModel
        {
            get { return this.DataContext as MainWindowViewModel; }
        }

        public MainWindow()
        {
            InitializeComponent();

            this.loginProgress.Visibility = Visibility.Hidden;
        }

        private async void loginBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(this.usernameBox.Text) || string.IsNullOrEmpty(this.passwordBox.Password))
            {
                DisplayErrorBox("Anmeldename oder Kennwort darf nicht leer sein.");
                return;
            }

            this.loginGroupBox.IsEnabled = false;
            this.coursesDataGrid.IsEnabled = false;
            this.loginProgress.Visibility = Visibility.Visible;

            // TODO error handling
            bool success = await this.ViewModel.Login(this.passwordBox.Password);

            try
            {
                await this.ViewModel.LoadCourses();
            }
            catch (Exception ex)
            {
                DisplayErrorBox(ex.Message);
            }

            this.loginProgress.Visibility = Visibility.Hidden;
            this.coursesDataGrid.IsEnabled = true;
            this.loginGroupBox.IsEnabled = true;
        }

        private void usernameBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                this.passwordBox.Focus();
        }

        private void passwordBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                loginBtn_Click(this, new RoutedEventArgs());
        }

        private void downloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.ViewModel.Courses.Count == 0)
                DisplayErrorBox("Keine Kurse ausgewählt");

            this.ViewModel.DownloadMaterials();
        }

        private void DisplayErrorBox(string message)
        {
            MessageBox.Show(message, "Doh!", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
