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

namespace BeatSaberPlaylistDownloader
{
    /// <summary>
    /// Interaction logic for BeatSaberResultControl.xaml
    /// </summary>
    public partial class BeatSaberResultControl : UserControl
    {
        public string Title { get; private set; }
        public string Artist { get; private set; }
        public string DownloadUrl { get; private set; }
        public dynamic JsonObject { get; private set; }

        public string SongDescription { get { return $"{Title} - {Artist}"; } }

        public string Mapper { get { return JsonObject.metadata.levelAuthorName; } }

        public int Rating { get; private set; }
        public int Downloads { get; private set; }
        public string Hash { get { return JsonObject.hash; } }

        public BeatSaberResultControl(string title, string artist, string downloadUrl, dynamic jsonObject)
        {
            InitializeComponent();
            Title = title;
            Artist = artist;
            DownloadUrl = downloadUrl;
            JsonObject = jsonObject;
            Rating = JsonObject.stats.upVotes - JsonObject.stats.downVotes;
            Downloads = JsonObject.stats.downloads;
        }
    }
}
