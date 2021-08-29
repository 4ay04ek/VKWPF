using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Imaging;
using Ookii.Dialogs.Wpf;
using System.IO;
using System.Threading;

namespace VKWPF
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string startingFolderPath;
        private bool workingState = false;
        private Thread main_thread;
        public MainWindow()
        {
            InitializeComponent();
        }
        private void WindowLoad(object sender, RoutedEventArgs e)
        {
            var t = new FileInfo("cache.txt");
            if (t.Exists)
            {
                StreamReader f = new StreamReader("cache.txt");
                string[] cache = f.ReadToEnd().Split('|');
                if (cache.Length == 4)
                {
                    startingFolderPath = cache[0].Replace("\r\n", "");
                    folder_path.Text = startingFolderPath;
                    login.Text = cache[1];
                    password.Text = cache[2];
                    pauseTime.Text = cache[3];
                }
                f.Close();
            }
        }
        private void OpenFolder(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog();
            if (dialog.ShowDialog() == true)
            {
                folder_path.Text = dialog.SelectedPath;
            }
        }
        private void StartWorking(object sender, RoutedEventArgs e)
        {
            if (!workingState)
            {
                mainButtonImage.Source = new BitmapImage(new Uri("Resources/pause-button.png", UriKind.Relative));
                folder_path.IsEnabled = false;
                login.IsEnabled = false;
                password.IsEnabled = false;
                pauseTime.IsEnabled = false;
                folderButton.IsEnabled = false;
                workingState = true;
                VK vk = new VK(folder_path.Text, login.Text, password.Text, startingFolderPath != folder_path.Text, Convert.ToInt32(pauseTime.Text));
                vk.setProgressVisuals(currentSending, log);
                main_thread = new Thread(vk.start);
                main_thread.Start();
            }
            else
            {
                mainButtonImage.Source = new BitmapImage(new Uri("Resources/playico.png", UriKind.Relative));
                folder_path.IsEnabled = true;
                login.IsEnabled = true;
                password.IsEnabled = true;
                pauseTime.IsEnabled = true;
                folderButton.IsEnabled = true;
                workingState = false;
                currentSending.Source = new BitmapImage();
                log.Text = "";
                main_thread.Abort();
            }
        }
        private void AppClose(object sender, CancelEventArgs e)
        {
            if (main_thread != null && main_thread.IsAlive) main_thread.Abort();
            StreamWriter cache = new StreamWriter("cache.txt", false);
            cache.WriteLine(folder_path.Text + "|" + login.Text + "|" + password.Text + "|" + pauseTime.Text);
            cache.Close();
        }
    }
}
