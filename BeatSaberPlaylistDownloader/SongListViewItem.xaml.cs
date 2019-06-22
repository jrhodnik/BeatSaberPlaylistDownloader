using BeatSaberPlaylistDownloader.Utilities;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace BeatSaberPlaylistDownloader
{
    /// <summary>
    /// Interaction logic for SongListViewItem.xaml
    /// </summary>
    public partial class SongListViewItem : UserControl
    {
        public string Title { get; private set; }
        public string Artist { get; private set; }

        public ObservableCollection<BeatSaberResultControl> Results { get; private set; } = new ObservableCollection<BeatSaberResultControl>();
        //public int MyProperty { get; set; }
        public SongListViewItem(string title, string artist)
        {
            InitializeComponent();
            Title = title;
            Artist = artist;
        }
        /// <summary>
        /// Populates Results property with list of results from beatsaver.com.
        /// </summary>
        public void SearchSong()
        {
            var query = HttpUtility.UrlEncode($"(metadata.songName:\"{Title}\") AND (name:\"{Artist}\")");
            var url = $"https://beatsaver.com/api/search/advanced/all?q={query}";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Stream receiveStream = response.GetResponseStream();
                StreamReader readStream = null;

                if (response.CharacterSet == null)
                {
                    readStream = new StreamReader(receiveStream);
                }
                else
                {
                    readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));
                }

                string data = readStream.ReadToEnd();

                response.Close();
                readStream.Close();

                dynamic deser = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(data);

                foreach (dynamic result in deser.docs)
                {
                    Dispatcher.Invoke(() =>
                    {
                        Results.Add(new BeatSaberResultControl(result.metadata.songName.ToString(), result.metadata.songSubName.ToString(), result.downloadURL.ToString(), result));
                    });
                }

                Dispatcher.Invoke(() =>
                {
                    tbResults.Text = $"{Results.Count} Results:";
                    if (Results.Count > 0)
                    {
                        cbResults.IsEnabled = true;

                        //For whatever reason the control wasn't rendering the binding'd items unless I dropped down first. This is a hack to fix that...
                        cbResults.IsDropDownOpen = true;
                        cbResults.IsDropDownOpen = false;

                        cbResults.SelectedIndex = 0;
                    }
                });

                Task.Factory.StartNew(() =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (Results.Count > 0)
                        {
                            //Check to see if song is already downloaded
                            if (Directory.Exists(MainWindow.CustomSongDirectory))
                            {
                                var songDirectories = Directory.GetDirectories(MainWindow.CustomSongDirectory);

                                foreach (var result in Results)
                                {
                                    string hash = result.JsonObject.hash;

                                    if (songDirectories.Any(x => x.ToLower().Contains(hash.ToLower())))
                                    {
                                        //For whatever reason the control wasn't rendering the binding'd items unless I dropped down first. This is a hack to fix that...
                                        cbResults.IsDropDownOpen = true;
                                        cbResults.IsDropDownOpen = false;

                                        cbResults.SelectedItem = result;
                                        break;
                                    }
                                }
                            }
                        }
                    });
                });
            }
        }

        private string GetSongPath(BeatSaberResultControl song)
        {
            return System.IO.Path.Combine(MainWindow.CustomSongDirectory, $"{song.JsonObject.hash}");
        }
        private void SetupDownloadUI(BeatSaberResultControl song)
        {
            if(Directory.Exists(GetSongPath(song)))
            {
                btnDownload.Visibility = Visibility.Collapsed;
                pbDownload.Visibility = Visibility.Collapsed;
                tblkDownloaded.Visibility = Visibility.Visible;
            }
            else{
                btnDownload.Visibility = Visibility.Visible;
                pbDownload.Visibility = Visibility.Collapsed;
                tblkDownloaded.Visibility = Visibility.Collapsed;
            }

            if (cbResults.SelectedItem != null)
            {
                btnDownload.IsEnabled = true;
            }
            else
            {
                btnDownload.IsEnabled = false;
            }
        }

        private void CbResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var song = (BeatSaberResultControl)cbResults.SelectedItem;
            if(song == null) { return; }
            SetupDownloadUI(song);
        }

        /// <summary>
        /// Returns a path to a .zip for the downloaded song.
        /// </summary>
        /// <param name="result"></param>
        private string GetTempDownloadPath(BeatSaberResultControl result)
        {
            return Path.Combine(Path.GetTempPath(), $"{result.Hash}.zip");
        }

        private void BtnDownload_Click(object sender, RoutedEventArgs e)
        {
            var song = (BeatSaberResultControl)cbResults.SelectedItem;

            if (!Directory.Exists(MainWindow.CustomSongDirectory))
            {
                MessageBox.Show("Beat Saber location is invalid.");
                return;
            }

            btnDownload.Visibility = Visibility.Collapsed;
            pbDownload.Visibility = Visibility.Visible;
            tblkDownloaded.Visibility = Visibility.Collapsed;
            cbResults.IsEnabled = false;

            using (WebClient wc = new WebClient())
            {
                wc.DownloadProgressChanged += wc_DownloadProgressChanged;
                wc.DownloadFileAsync(
                    new System.Uri($"https://beatsaver.com{song.DownloadUrl}"),
                    GetTempDownloadPath(song)
                );
                wc.DownloadFileCompleted += Wc_DownloadFileCompleted;
                this.GetLogger().Debug($"Downloading {song.Title} zip to {GetTempDownloadPath(song)}...");
            }
        }

        private void Wc_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            var song = (BeatSaberResultControl)cbResults.SelectedItem;
            Task.Factory.StartNew(() =>
            {
                if (e.Error != null)
                {
                    MessageBox.Show($"Error downloading '{song.Title}': {e.Error.Message}");
                    this.GetLogger().Error($"Error downloading '{song.Title}'.", e.Error);
                }
                else
                {
                    this.GetLogger().Debug($"Finished downloading {song.Title} zip. Extracting to {GetSongPath(song)}...");

                    var zipPath = GetTempDownloadPath(song);
                    var targetDir = GetSongPath(song);
                    FastZip zip = new FastZip();
                    zip.ExtractZip(zipPath, targetDir, null);

                    this.GetLogger().Debug($"Finished extracting {song.Title}. Complete!");
                }

                Dispatcher.Invoke(() =>
                {
                    SetupDownloadUI(song);
                    cbResults.IsEnabled = true;
                });
            });
        }

        void wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                pbDownload.Value = e.ProgressPercentage;
            });
        }
    }
}
