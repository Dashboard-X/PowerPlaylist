/// 2009 by Mario Meir-Huber
/// Mail: mario@meirhuber.de

using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Media.Imaging;

public delegate void DownloadProgressChangedEvent(object sender, double value);

///Main Namespace for the Photo Album
namespace PhotoAlbum
{
    /// <summary>
    /// Base Class for all Photo Album implementations
    /// </summary>
    public class PhotoBase : UserControl
    {
        #region Private Fields

        private ObservableCollection<BitmapImage> _Images;  //images used in the photo album

        private int Count = 0;  //counter variable

        private Uri _UriDataSource;  //Links to a file that contains a Filelist

        #endregion

        #region Events

        /// <summary>
        /// Occurs when there is a change to the download Progress
        /// </summary>
        public event DownloadProgressChangedEvent DownloadProgressChanged;

        #endregion

        #region Constructors

        /// <summary>
        /// Instantiates a new Photo Album
        /// </summary>
        public PhotoBase()
        {
            CreateImageCollection();

            RadialGradientBrush BackgroundBrush = new RadialGradientBrush();
            
            BackgroundBrush.GradientOrigin = new Point(1.0,1.0);
            BackgroundBrush.RadiusX = 0.77;
            BackgroundBrush.RadiusY = 0.77;
            
            BackgroundBrush.GradientStops.Add(
                new GradientStop() { Color = new Color() { A = 255, R = 1, G = 43, B = 71 }}
                );

            BackgroundBrush.GradientStops.Add(
                new GradientStop() { Color = new Color() { A = 255, R = 193, G = 230, B = 255}, Offset = 1.0}
                );

            Background = BackgroundBrush;
        }

        #endregion

        #region Event Handling

        /// <summary>
        /// Occurs when the Collection is changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            foreach (BitmapImage img in _Images)
            {
                img.DownloadProgress -= OnDownloadProgressChanged;
                img.DownloadProgress += OnDownloadProgressChanged;
            }
        }

        /// <summary>
        /// Occurs when the Download Progress changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDownloadProgressChanged(object sender, DownloadProgressEventArgs e)
        {
            if (e.Progress != 100) return;

            Count += e.Progress;
            double res = Count / _Images.Count;

            if (DownloadProgressChanged != null) DownloadProgressChanged(this, res);
        }

        /// <summary>
        /// Occurs when the String was downloaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnUriDataSourceDownloaded(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error == null && !e.Cancelled)
            {
                if(e.UserState.ToString() == "txt") //the file is a simple text file
                {
                    ProcessEscapeSeparatedFile(e.Result);
                }
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the Images
        /// </summary>
        public ObservableCollection<BitmapImage> Images
        {
            get 
            {
                if (_Images == null) throw new NullReferenceException("Object is null");
                
                return this._Images; 
            }
        }
         
        /// <summary>
        /// Gets a URI to a Data Source. It currently supports
        /// a List of Images that a separated by a \n
        /// </summary>
        public Uri UriDataSource
        {
            get
            {
                return _UriDataSource;
            }
            set
            {
                if (_UriDataSource != value && Uri.IsWellFormedUriString(value.ToString(), UriKind.Absolute))
                {
                    _UriDataSource = value;

                    WebClient client = new WebClient();
                    client.DownloadStringCompleted += OnUriDataSourceDownloaded;
                    client.DownloadStringAsync(_UriDataSource, "txt");
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Processes a String that contains information on a picture
        /// </summary>
        /// <param name="Content">The String to parse</param>
        private void ProcessEscapeSeparatedFile(string Content)
        {
            var result = Content.Split('\n');

            if (_Images == null) CreateImageCollection();

            _Images.Clear();

            foreach (string itm in result)
            {
                if(Uri.IsWellFormedUriString(itm, UriKind.Absolute))
                {
                    _Images.Add(new BitmapImage()
                    {
                        UriSource = new Uri(itm, UriKind.Absolute)
                    });
                }
            }
        }

        /// <summary>
        /// Creates the Images Collection
        /// </summary>
        private void CreateImageCollection()
        {
            _Images = new ObservableCollection<BitmapImage>();

            _Images.CollectionChanged += OnCollectionChanged;
        }

        #endregion
    }
}
