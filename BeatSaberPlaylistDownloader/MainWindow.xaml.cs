using BeatSaberPlaylistDownloader.Utilities;
using CsvHelper;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace BeatSaberPlaylistDownloader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string[] Header { get; private set; } = null;
        public List<IDictionary<string, object>> Results = new List<IDictionary<string, object>>();
        public static string CustomSongDirectory = null;

        public MainWindow()
        {
            Logger.Setup();
            InitializeComponent();
            var pl = new PathLogic();

            var bsPath = pl.GetInstallationPath();

            if (Directory.Exists(bsPath))
            {
                SetCustomSongPath(bsPath);
            }
        }

        private void SetCustomSongPath(string beatSaberLocation)
        {
            CustomSongDirectory = System.IO.Path.Combine(beatSaberLocation, @"Beat Saber_Data\CustomLevels");
            tbSongPath.Text = beatSaberLocation;
        }

        private void btnPlaylistBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.DefaultExt = "csv";
            ofd.Filter = "Comma Separated Value File (*.csv)|*.csv";
            ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            if (ofd.ShowDialog() == false)
            {
                return;
            }
            tbPlaylist.Text = ofd.FileName;
            try
            {
                using (var reader = new StreamReader(ofd.FileName))
                {
                    using (var csv = new CsvReader(reader))
                    {
                        csv.Read();
                        csv.ReadHeader();
                        Header = csv.Context.HeaderRecord;

                        cbSongNameColumn.ItemsSource = Header;
                        cbSongArtistColumn.ItemsSource = Header;

                        int count = 0;
                        while (csv.Read())
                        {
                            var rec = csv.GetRecord<dynamic>();
                            Results.Add(rec as IDictionary<string, object>);
                            count++;


                            //Here for debugging...
                            //if (count > 10)
                            //{
                            //    return;
                            //}
                        }

                        this.GetLogger().Info($"Loaded {count} songs from CSV.");
                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading CSV file: {ex}", "Error Reading CSV", MessageBoxButton.OK, MessageBoxImage.Error);
                this.GetLogger().Error($"Error reading CSV file {ofd.FileName}", ex);
            }
        }

        private void BtnLoadSongs_Click(object sender, RoutedEventArgs e)
        {
            List<SongListViewItem> items = new List<SongListViewItem>();
            foreach(var song in Results)
            {
                var title = song[(string)cbSongNameColumn.SelectedItem].ToString();
                var artist = song[(string)cbSongArtistColumn.SelectedItem].ToString();

                items.Add(new SongListViewItem(title, artist));
            }

            lvSongs.ItemsSource = items;

            Task.Factory.StartNew(() => { 
                foreach(var song in items)
                {
                    song.SearchSong();
                    Thread.Sleep(1100);
                }
                this.GetLogger().Info("Loaded all songs.");
            });
        }

        private void BtnBrowseSongPath_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.SelectedPath = tbSongPath.Text;
                if (Directory.Exists(tbSongPath.Text))
                {
                    dialog.SelectedPath = CustomSongDirectory;
                }
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();

                if(result == System.Windows.Forms.DialogResult.OK)
                {
                    SetCustomSongPath(dialog.SelectedPath);
                }
            }
        }
    }
}
