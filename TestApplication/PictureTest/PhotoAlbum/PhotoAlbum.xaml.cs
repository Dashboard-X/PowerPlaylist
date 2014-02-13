/*
 * 2008 by Mario Meir-Huber
 * Mail: i-mameir@microsoft.com
 *       mario@meirhuber.de
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Net;
using System.Xml.Linq;
using System.Xml;
using System.IO;
using System.Diagnostics;
using System.Windows.Resources;
using System.Windows.Media.Imaging;
using System.Globalization;


namespace PhotoAlbum
{
    /// <summary>
    /// Displays a Photoalbum
    /// </summary>
    public partial class PhotoAlbum : UserControl
    {
        private string _targetAlbum = string.Empty;         //the file to be used
        private Stack<string> _lst;                         //list of all files to be downloaded

        private double singlePercent = 0.0;                 //weights the downloader progress
        private int itemIndex = 0;                          //sets an index 

        private const double CalcFromW = 200.0;             //width - the control assumes everything is 200 width and scales the control
        private const double CalcFromH = 200.0;             //height -the control assumes everything is 200 heigth and scales the control

        private const double PictureHigh = 50.0;            //height of a picture
        private const double PictureLow = 37.5;             //width of a picture 

        private double FactorWidth = 1.0;                   //used to calculate the width scale
        private double FactorHeight = 1.0;                  //used to calculate the height scale

        private MVT.ProgressControl progressControl = null; //displays a status control
        private List<Point> _transformcoordinates;          //coordinates where to display an image
        private Dictionary<string, ImageDesc> _imageinfo;   //stores information for an image

        private TextBlock txtTitle;                     //displays a title for the control
        private Rectangle rtBackground;                 //the background for the title display
        private MVT.MinimizeGlassButton glassButton;    //a button to minimize the control
        
        private Random rotation;                    //randomizer for the rotation property

        private Image currentSelection = null;      //reference to the image

        private double ScaleY = 1.0;                //information on the y-axis scale
        private double ScaleX = 1.0;                //information on the x-axis scale

        private XElement xePhotoAlbum;              //linq to xml to get queries on the xml element

        private string title = string.Empty;        //title of an image
        private string description = string.Empty;  //description for an image
        private string linkto = string.Empty;       //link to another album (if applicable)

        private int zordermax = -1;             //identifies the highest Z Index
        private bool isMaximized = false;       //stores information if a picture has the focus (maximum size)

        /// <summary>
        /// displays a photo album
        /// </summary>
        public PhotoAlbum()
        {
            InitializeComponent();

            //sets event handlers
            this.SizeChanged += new SizeChangedEventHandler(PhotoAlbum_SizeChanged);
            this.Loaded += new RoutedEventHandler(PhotoAlbum_Loaded);

            _transformcoordinates = new List<Point>();

            //set 20 different positions where to place an image
            _transformcoordinates.Add(new Point(-230.0, -250.0));
            _transformcoordinates.Add(new Point(215.0, -215.0));
            _transformcoordinates.Add(new Point(230.0, 175.0));
            _transformcoordinates.Add(new Point(-220.0, 185.0));
            _transformcoordinates.Add(new Point(-30.0, -220.0));
            _transformcoordinates.Add(new Point(30.0, 145.0));
            _transformcoordinates.Add(new Point(-150.0, -60.0));
            _transformcoordinates.Add(new Point(180.0, 65.0));
            _transformcoordinates.Add(new Point(130.0, -90.0));
            _transformcoordinates.Add(new Point(-120.0, 70.0));
            _transformcoordinates.Add(new Point(40.0, -150.0));
            _transformcoordinates.Add(new Point(-75.0, 160.0));
            _transformcoordinates.Add(new Point(-240.0, -140.0));
            _transformcoordinates.Add(new Point(230.0, -20.0));
            _transformcoordinates.Add(new Point(-260.0, 50.0));
            _transformcoordinates.Add(new Point(100.0, -270.0));
            _transformcoordinates.Add(new Point(-50.0, -120.0));
            _transformcoordinates.Add(new Point(100.0, 80.0));
            _transformcoordinates.Add(new Point(-130.0, -170.0));

            //sets a random rotation for an image
            rotation = new Random(DateTime.Today.Millisecond);

            _imageinfo = new Dictionary<string, ImageDesc>();
        }

        /// <summary>
        /// occurs when the album has loaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void PhotoAlbum_Loaded(object sender, RoutedEventArgs e)
        {
            //loads the album from the server if there is one
            if (_targetAlbum != string.Empty)
            {
                FactorWidth = ActualWidth / CalcFromW;
                FactorHeight = ActualHeight / CalcFromH;

                UpdateAlbum();
            }
        }

        /// <summary>
        /// occurs when the albums size was changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void PhotoAlbum_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            FactorWidth = e.NewSize.Width / CalcFromW;
            FactorHeight = e.NewSize.Height / CalcFromH;
        }

        /// <summary>
        /// sets or gets the album to display
        /// </summary>
        public string TargetAlbum
        {
            get
            {
                return _targetAlbum;
            }
            set
            {
                _targetAlbum = value;
            }
        }

        /// <summary>
        /// this loads a new album
        /// </summary>
        private void UpdateAlbum()
        {
            WebClient wc = new WebClient();
            wc.DownloadStringAsync(new System.Uri(_targetAlbum));

            wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler(wc_DownloadStringCompleted);
        }

        /// <summary>
        /// this occurs when an asynchronous download of an album completed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void wc_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            //read the album 
            TextReader tr = new StringReader(e.Result);
            progressControl = new MVT.ProgressControl();
            progressControl.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));

            //display the progress control
            progressControl.SetValue(Canvas.LeftProperty, (ActualWidth - progressControl.ActualWidth)  / 2);
            progressControl.SetValue(Canvas.TopProperty, (ActualHeight - progressControl.ActualHeight)  / 2);

            xePhotoAlbum = XElement.Load(tr);
            _lst = new Stack<string>();

            //get pictures in album
            var result = from xe1 in xePhotoAlbum.Descendants("Picture")
                         select xe1.Value;
            
            foreach(var r in result)
            {
                _lst.Push(r);
            }

            //calculate percent per picture
            singlePercent = 100 / _lst.Count;

            LayoutRoot.Children.Add(progressControl);

            progressControl.Text = "0%";

            LoadContent();
        }

        /// <summary>
        /// this loads album files 
        /// </summary>
        private void LoadContent()
        {
            WebClient pictureClient = new WebClient();

            Uri tmp;

            if (_lst.Count > 0)
            {
                tmp = new Uri(_lst.Pop());
                pictureClient.OpenReadAsync(tmp);
                pictureClient.OpenReadCompleted += new OpenReadCompletedEventHandler(pictureClient_OpenReadCompleted);
                pictureClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(pictureClient_DownloadProgressChanged);
            }
            else
            {
                zordermax = itemIndex;
                itemIndex = -1;
                StartPhotoAlbum();
            }
        }

        /// <summary>
        /// generates the animations for a photo album
        /// </summary>
        private void StartPhotoAlbum()
        {
            Image img;
            Storyboard st;

            for(int i=0; i<LayoutRoot.Children.Count; i++)
            {
                if (LayoutRoot.Children[i] is Image)
                {
                    itemIndex++;
                    img = (Image)LayoutRoot.Children[i];

                    img.Visibility = Visibility.Visible;

                    st = GenerateAnimation((string)img.GetValue(Canvas.NameProperty), itemIndex);
                    LayoutRoot.Resources.Add(st);
                    st.Completed += new EventHandler(st_Completed);
                    st.Begin();
                }
            }

            LayoutRoot.Children.Remove(progressControl);
            progressControl = null;
        }

        /// <summary>
        /// this removes a storyboard when it completes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void st_Completed(object sender, EventArgs e)
        {
            LayoutRoot.Resources.Remove((Storyboard)sender);
        }

        /// <summary>
        /// this displays additional information for a picture
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void st1_Completed(object sender, EventArgs e)
        {
            double ActHeight, ActWidth;

            rtBackground = new Rectangle();
            txtTitle = new TextBlock();

            rtBackground.SetValue(Canvas.NameProperty, "rtBackground");
            txtTitle.SetValue(Canvas.NameProperty, "txtTitle");

            rtBackground.Fill = new SolidColorBrush(Color.FromArgb(130, 79, 128, 188));
            txtTitle.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));

            glassButton = new MVT.MinimizeGlassButton();
            glassButton.SetValue(Canvas.NameProperty, "minimize");

            ActHeight = currentSelection.Height * ScaleY;
            ActWidth = currentSelection.Width * ScaleX;

            glassButton.SetValue(Canvas.LeftProperty, ActualWidth - (ActualWidth - ActWidth) / 2 - 30.0);

            glassButton.SetValue(Canvas.TopProperty, (ActualHeight - ActHeight) / 2 + 10.0);
            glassButton.MouseLeftButtonUp += new MouseButtonEventHandler(glassButton_MouseLeftButtonUp);
            glassButton.SetValue(Canvas.ZIndexProperty, zordermax + 1);

            rtBackground.SetValue(Canvas.TopProperty, (ActualHeight - ActHeight) / 2 + 10.0);
            rtBackground.SetValue(Canvas.LeftProperty, ((ActualWidth - ActualWidth) / 2 + 30.0));
            rtBackground.Width = ActualWidth - 100.0;
            rtBackground.Height = 20.0;
            rtBackground.RadiusX = 15.0;
            rtBackground.RadiusY = 15.0;
            rtBackground.SetValue(Canvas.ZIndexProperty, zordermax + 2);

            txtTitle.SetValue(Canvas.TopProperty, (ActualHeight - ActHeight) / 2 + 10.0);
            txtTitle.SetValue(Canvas.LeftProperty, ((ActualWidth - ActualWidth) / 2 + 55.0));

            txtTitle.Text = title;
            txtTitle.SetValue(Canvas.ZIndexProperty, zordermax + 2);
            
            LayoutRoot.Children.Add(glassButton);
            LayoutRoot.Children.Add(rtBackground);
            LayoutRoot.Children.Add(txtTitle);
            LayoutRoot.Resources.Remove((Storyboard)sender);

            isMaximized = true;
        }

        /// <summary>
        /// minimizes the photo album when the minimize button was clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void glassButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Storyboard st;
            TransformGroup tg;
            ScaleTransform scale;

            LayoutRoot.Children.Remove(LayoutRoot.FindName("txtTitle") as TextBlock);
            LayoutRoot.Children.Remove(LayoutRoot.FindName("rtBackground") as Rectangle);

            string objName = (string)currentSelection.GetValue(Canvas.NameProperty);

            tg = (TransformGroup)currentSelection.RenderTransform;
            scale = (ScaleTransform)tg.Children[2];

            string[] tmparray = objName.Split('_');
            int index = Convert.ToInt32(tmparray[tmparray.Length - 1]) - 1;

            st = GenerateAnimation(objName, index);
            st.Completed += new EventHandler(st_moveout);

            LayoutRoot.Resources.Add(st);
            st.Begin();

            LayoutRoot.Children.Remove((MVT.MinimizeGlassButton)sender);

            for (int i = 0; i < LayoutRoot.Children.Count; i++)
            {
                if (LayoutRoot.Children[i] is Image)
                    LayoutRoot.Children[i].Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// enables the mouseover effect for pictures and removes the storyboard
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void st_moveout(object sender, EventArgs e)
        {
            Storyboard tmp = (Storyboard)sender;
            LayoutRoot.Resources.Remove(tmp);

            isMaximized = false;
        }

        /// <summary>
        /// shows the download progress
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void pictureClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            progressControl.Text = (itemIndex * singlePercent + (singlePercent * (e.ProgressPercentage / 100))).ToString() + "%";
        }

        /// <summary>
        /// generates a new animation when the photo album is loaded for each picture
        /// </summary>
        /// <param name="name"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private Storyboard GenerateAnimation(string name, int index)
        {
            Point element = _transformcoordinates[index];
            Storyboard retval;
            double rotate = Math.Round(rotation.NextDouble() * 360, 2);

            string story = "<Storyboard xmlns=\"http://schemas.microsoft.com/client/2007\">" +
                            "<DoubleAnimationUsingKeyFrames Storyboard.TargetName=\"" + name + "\" Storyboard.TargetProperty=\"(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.X)\" BeginTime=\"00:00:00\">" +
                            "<SplineDoubleKeyFrame KeyTime=\"00:00:00\" Value=\"0\" />" +
                            "<SplineDoubleKeyFrame KeyTime=\"00:00:01\" Value=\"" + element.X.ToString() + "\">" +
                            "<SplineDoubleKeyFrame.KeySpline>" +
                            "<KeySpline ControlPoint1=\"0,1\" ControlPoint2=\"1,1\"/>" +
                            "</SplineDoubleKeyFrame.KeySpline>" +
                            "</SplineDoubleKeyFrame>" +
                            "</DoubleAnimationUsingKeyFrames>" +
                            "<DoubleAnimationUsingKeyFrames Storyboard.TargetName=\"" + name + "\" Storyboard.TargetProperty=\"(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.Y)\" BeginTime=\"00:00:00\">" +
                            "<SplineDoubleKeyFrame KeyTime=\"00:00:00\" Value=\"0\" />" +
                            "<SplineDoubleKeyFrame KeyTime=\"00:00:01\" Value=\"" + element.Y.ToString() + "\">" +
                            "<SplineDoubleKeyFrame.KeySpline>" +
                            "<KeySpline ControlPoint1=\"0,1\" ControlPoint2=\"1,1\"/>" +
                            "</SplineDoubleKeyFrame.KeySpline>" +
                            "</SplineDoubleKeyFrame>" +
                            "</DoubleAnimationUsingKeyFrames>" +
                            "<DoubleAnimationUsingKeyFrames Storyboard.TargetName=\"" + name + "\" Storyboard.TargetProperty=\"(UIElement.RenderTransform).(TransformGroup.Children)[0].(RotateTransform.Angle)\" BeginTime=\"00:00:00\">" +
                            "<SplineDoubleKeyFrame KeyTime=\"00:00:00\" Value=\"0\" />" +
                            "<SplineDoubleKeyFrame KeyTime=\"00:00:01\" Value=\"" + rotate.ToString(new NumberFormatInfo() { CurrencyDecimalSeparator="." }) + "\">" +
                            "<SplineDoubleKeyFrame.KeySpline>" +
                            "<KeySpline ControlPoint1=\"0,1\" ControlPoint2=\"1,1\"/>" +
                            "</SplineDoubleKeyFrame.KeySpline>" +
                            "</SplineDoubleKeyFrame>" +
                            "</DoubleAnimationUsingKeyFrames>" +
                            "<DoubleAnimationUsingKeyFrames Storyboard.TargetName=\"" + name + "\" Storyboard.TargetProperty=\"(UIElement.RenderTransform).(TransformGroup.Children)[2].(ScaleTransform.ScaleY)\" BeginTime=\"00:00:00\">" +
                            "<SplineDoubleKeyFrame KeyTime=\"00:00:01\" Value=\"1\">" +
                            "<SplineDoubleKeyFrame.KeySpline>" +
                            "<KeySpline ControlPoint1=\"0,1\" ControlPoint2=\"1,1\"/>" +
                            "</SplineDoubleKeyFrame.KeySpline>" +
                            "</SplineDoubleKeyFrame>" +
                            "</DoubleAnimationUsingKeyFrames>" +
                            "<DoubleAnimationUsingKeyFrames Storyboard.TargetName=\"" + name + "\" Storyboard.TargetProperty=\"(UIElement.RenderTransform).(TransformGroup.Children)[2].(ScaleTransform.ScaleX)\" BeginTime=\"00:00:00\">" +
                            "<SplineDoubleKeyFrame KeyTime=\"00:00:01\" Value=\"1\">" +
                            "<SplineDoubleKeyFrame.KeySpline>" +
                            "<KeySpline ControlPoint1=\"0,1\" ControlPoint2=\"1,1\"/>" +
                            "</SplineDoubleKeyFrame.KeySpline>" +
                            "</SplineDoubleKeyFrame>" +
                            "</DoubleAnimationUsingKeyFrames>" +
                            "</Storyboard>";

            retval = (Storyboard)System.Windows.Markup.XamlReader.Load(story);

            return retval;
        }

        /// <summary>
        /// maximizes a photo if it is the selected one
        /// </summary>
        /// <param name="name"></param>
        /// <param name="scaleX"></param>
        /// <param name="scaleY"></param>
        /// <returns></returns>
        private Storyboard StoryBoardFadeIn(string name, double scaleX, double scaleY)
        {
            Storyboard retval;
            double rotate = Math.Round(rotation.NextDouble() * 360, 2);

            string story = "<Storyboard xmlns=\"http://schemas.microsoft.com/client/2007\">" +
                            "<DoubleAnimationUsingKeyFrames Storyboard.TargetName=\"" + name + "\" Storyboard.TargetProperty=\"(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.X)\" BeginTime=\"00:00:00\">" +
                            "<SplineDoubleKeyFrame KeyTime=\"00:00:01\" Value=\"0\">" +
                            "<SplineDoubleKeyFrame.KeySpline>" +
                            "<KeySpline ControlPoint1=\"0,1\" ControlPoint2=\"1,1\"/>" +
                            "</SplineDoubleKeyFrame.KeySpline>" +
                            "</SplineDoubleKeyFrame>" +
                            "</DoubleAnimationUsingKeyFrames>" +
                            "<DoubleAnimationUsingKeyFrames Storyboard.TargetName=\"" + name + "\" Storyboard.TargetProperty=\"(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.Y)\" BeginTime=\"00:00:00\">" +
                            "<SplineDoubleKeyFrame KeyTime=\"00:00:01\" Value=\"0\">" +
                            "<SplineDoubleKeyFrame.KeySpline>" +
                            "<KeySpline ControlPoint1=\"0,1\" ControlPoint2=\"1,1\"/>" +
                            "</SplineDoubleKeyFrame.KeySpline>" +
                            "</SplineDoubleKeyFrame>" +
                            "</DoubleAnimationUsingKeyFrames>" +
                            "<DoubleAnimationUsingKeyFrames Storyboard.TargetName=\"" + name + "\" Storyboard.TargetProperty=\"(UIElement.RenderTransform).(TransformGroup.Children)[0].(RotateTransform.Angle)\" BeginTime=\"00:00:00\">" +
                            "<SplineDoubleKeyFrame KeyTime=\"00:00:01\" Value=\"0\">" +
                            "<SplineDoubleKeyFrame.KeySpline>" +
                            "<KeySpline ControlPoint1=\"0,1\" ControlPoint2=\"1,1\"/>" +
                            "</SplineDoubleKeyFrame.KeySpline>" +
                            "</SplineDoubleKeyFrame>" +
                            "</DoubleAnimationUsingKeyFrames>" +
                            "<DoubleAnimationUsingKeyFrames Storyboard.TargetName=\"" + name + "\" Storyboard.TargetProperty=\"(UIElement.RenderTransform).(TransformGroup.Children)[2].(ScaleTransform.ScaleX)\" BeginTime=\"00:00:00\">" +
                            "<SplineDoubleKeyFrame KeyTime=\"00:00:01\" Value=\"" + scaleX.ToString(new NumberFormatInfo() { CurrencyDecimalSeparator="."}) + "\">" +
                            "<SplineDoubleKeyFrame.KeySpline>" +
                            "<KeySpline ControlPoint1=\"0,1\" ControlPoint2=\"1,1\"/>" +
                            "</SplineDoubleKeyFrame.KeySpline>" +
                            "</SplineDoubleKeyFrame>" +
                            "</DoubleAnimationUsingKeyFrames>" +
                            "<DoubleAnimationUsingKeyFrames Storyboard.TargetName=\"" + name + "\" Storyboard.TargetProperty=\"(UIElement.RenderTransform).(TransformGroup.Children)[2].(ScaleTransform.ScaleY)\" BeginTime=\"00:00:00\">" +
                            "<SplineDoubleKeyFrame KeyTime=\"00:00:01\" Value=\"" + scaleY.ToString(new NumberFormatInfo() { CurrencyDecimalSeparator = "." }) + "\">" +
                            "<SplineDoubleKeyFrame.KeySpline>" +
                            "<KeySpline ControlPoint1=\"0,1\" ControlPoint2=\"1,1\"/>" +
                            "</SplineDoubleKeyFrame.KeySpline>" +
                            "</SplineDoubleKeyFrame>" +
                            "</DoubleAnimationUsingKeyFrames>" +
                            "</Storyboard>";

            retval = (Storyboard)System.Windows.Markup.XamlReader.Load(story);

            return retval;
        }
        
        /// <summary>
        /// minimizes all photos
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private Storyboard StoryBoardFadeOut(string name, double angle)
        {
            Storyboard retval;

            string story = "<Storyboard xmlns=\"http://schemas.microsoft.com/client/2007\">" +
                            "<DoubleAnimationUsingKeyFrames Storyboard.TargetName=\"" + name + "\" Storyboard.TargetProperty=\"(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.X)\" BeginTime=\"00:00:00\">" +
                            "<SplineDoubleKeyFrame KeyTime=\"00:00:01\" Value=\"0\">" +
                            "<SplineDoubleKeyFrame.KeySpline>" +
                            "<KeySpline ControlPoint1=\"0,1\" ControlPoint2=\"1,1\"/>" +
                            "</SplineDoubleKeyFrame.KeySpline>" +
                            "</SplineDoubleKeyFrame>" +
                            "</DoubleAnimationUsingKeyFrames>" +
                            "<DoubleAnimationUsingKeyFrames Storyboard.TargetName=\"" + name + "\" Storyboard.TargetProperty=\"(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.Y)\" BeginTime=\"00:00:00\">" +
                            "<SplineDoubleKeyFrame KeyTime=\"00:00:01\" Value=\"0\">" +
                            "<SplineDoubleKeyFrame.KeySpline>" +
                            "<KeySpline ControlPoint1=\"0,1\" ControlPoint2=\"1,1\"/>" +
                            "</SplineDoubleKeyFrame.KeySpline>" +
                            "</SplineDoubleKeyFrame>" +
                            "</DoubleAnimationUsingKeyFrames>" +
                            "<DoubleAnimationUsingKeyFrames Storyboard.TargetName=\"" + name + "\" Storyboard.TargetProperty=\"(UIElement.RenderTransform).(TransformGroup.Children)[0].(RotateTransform.Angle)\" BeginTime=\"00:00:00\">" +
                            "<SplineDoubleKeyFrame KeyTime=\"00:00:00.5\" Value=\"" + (angle + 360).ToString(new NumberFormatInfo() { CurrencyDecimalSeparator = "." }) + "\" />" +
                            "<SplineDoubleKeyFrame KeyTime=\"00:00:01.0\" Value=\"" + (angle + 720).ToString(new NumberFormatInfo() { CurrencyDecimalSeparator = "." }) + "\">" +
                            "</SplineDoubleKeyFrame>" +
                            "</DoubleAnimationUsingKeyFrames>" +
                            "<DoubleAnimationUsingKeyFrames Storyboard.TargetName=\"" + name + "\" Storyboard.TargetProperty=\"(UIElement.RenderTransform).(TransformGroup.Children)[2].(ScaleTransform.ScaleX)\" BeginTime=\"00:00:00\">" +
                            "<SplineDoubleKeyFrame KeyTime=\"00:00:00.5\" Value=\"1\" />" +
                            "<SplineDoubleKeyFrame KeyTime=\"00:00:01\" Value=\"0\">" +
                            "<SplineDoubleKeyFrame.KeySpline>" +
                            "<KeySpline ControlPoint1=\"0,1\" ControlPoint2=\"1,1\"/>" +
                            "</SplineDoubleKeyFrame.KeySpline>" +
                            "</SplineDoubleKeyFrame>" +
                            "</DoubleAnimationUsingKeyFrames>" +
                            "<DoubleAnimationUsingKeyFrames Storyboard.TargetName=\"" + name + "\" Storyboard.TargetProperty=\"(UIElement.RenderTransform).(TransformGroup.Children)[2].(ScaleTransform.ScaleY)\" BeginTime=\"00:00:00\">" +
                            "<SplineDoubleKeyFrame KeyTime=\"00:00:00.5\" Value=\"1\" />" +
                            "<SplineDoubleKeyFrame KeyTime=\"00:00:01\" Value=\"0\">" +
                            "<SplineDoubleKeyFrame.KeySpline>" +
                            "<KeySpline ControlPoint1=\"0,1\" ControlPoint2=\"1,1\"/>" +
                            "</SplineDoubleKeyFrame.KeySpline>" +
                            "</SplineDoubleKeyFrame>" +
                            "</DoubleAnimationUsingKeyFrames>" +
                            "</Storyboard>";

            retval = (Storyboard)System.Windows.Markup.XamlReader.Load(story);

            return retval;
        }

        /// <summary>
        /// this highlights a picture
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private Storyboard StoryZoomToPicture(string name)
        {
            string story = "<Storyboard xmlns=\"http://schemas.microsoft.com/client/2007\">" +
                            "<PointAnimationUsingKeyFrames BeginTime=\"00:00:00\" Duration=\"00:00:00.0010000\" Storyboard.TargetName=\"" + name + "\" Storyboard.TargetProperty=\"(UIElement.RenderTransformOrigin)\">" +
				            "<SplinePointKeyFrame KeyTime=\"00:00:00\" Value=\"0.5,0.5\"/>" +
			                "</PointAnimationUsingKeyFrames>" +
                            "<DoubleAnimationUsingKeyFrames Storyboard.TargetName=\"" + name + "\" Storyboard.TargetProperty=\"(UIElement.RenderTransform).(TransformGroup.Children)[2].(ScaleTransform.ScaleY)\" BeginTime=\"00:00:00\">" +
                            "<SplineDoubleKeyFrame KeyTime=\"00:00:00.2\" Value=\"1.1\">" +
                            "<SplineDoubleKeyFrame.KeySpline>" +
                            "<KeySpline ControlPoint1=\"0,1\" ControlPoint2=\"1,1\"/>" +
                            "</SplineDoubleKeyFrame.KeySpline>" +
                            "</SplineDoubleKeyFrame>" +
                            "</DoubleAnimationUsingKeyFrames>" +
                            "<DoubleAnimationUsingKeyFrames Storyboard.TargetName=\"" + name + "\" Storyboard.TargetProperty=\"(UIElement.RenderTransform).(TransformGroup.Children)[2].(ScaleTransform.ScaleX)\" BeginTime=\"00:00:00\">" +
                            "<SplineDoubleKeyFrame KeyTime=\"00:00:00.2\" Value=\"1.05\">" +
                            "<SplineDoubleKeyFrame.KeySpline>" +
                            "<KeySpline ControlPoint1=\"0,1\" ControlPoint2=\"1,1\"/>" +
                            "</SplineDoubleKeyFrame.KeySpline>" +
                            "</SplineDoubleKeyFrame>" +
                            "</DoubleAnimationUsingKeyFrames></Storyboard>";

            return (Storyboard)System.Windows.Markup.XamlReader.Load(story);
        }

        /// <summary>
        /// this removes the highlighted picture
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private Storyboard StoryZoomOutOfPicture(string name)
        {
            string story = "<Storyboard xmlns=\"http://schemas.microsoft.com/client/2007\">" +
                            "<PointAnimationUsingKeyFrames BeginTime=\"00:00:00\" Duration=\"00:00:00.0010000\" Storyboard.TargetName=\"" + name + "\" Storyboard.TargetProperty=\"(UIElement.RenderTransformOrigin)\">" +
                            "<SplinePointKeyFrame KeyTime=\"00:00:00\" Value=\"0.5,0.5\"/>" +
                            "</PointAnimationUsingKeyFrames>" +
                            "<DoubleAnimationUsingKeyFrames Storyboard.TargetName=\"" + name + "\" Storyboard.TargetProperty=\"(UIElement.RenderTransform).(TransformGroup.Children)[2].(ScaleTransform.ScaleY)\" BeginTime=\"00:00:00\">" +
                            "<SplineDoubleKeyFrame KeyTime=\"00:00:00.2\" Value=\"1\">" +
                            "<SplineDoubleKeyFrame.KeySpline>" +
                            "<KeySpline ControlPoint1=\"0,1\" ControlPoint2=\"1,1\"/>" +
                            "</SplineDoubleKeyFrame.KeySpline>" +
                            "</SplineDoubleKeyFrame>" +
                            "</DoubleAnimationUsingKeyFrames>" +
                            "<DoubleAnimationUsingKeyFrames Storyboard.TargetName=\"" + name + "\" Storyboard.TargetProperty=\"(UIElement.RenderTransform).(TransformGroup.Children)[2].(ScaleTransform.ScaleX)\" BeginTime=\"00:00:00\">" +
                            "<SplineDoubleKeyFrame KeyTime=\"00:00:00.2\" Value=\"1\">" +
                            "<SplineDoubleKeyFrame.KeySpline>" +
                            "<KeySpline ControlPoint1=\"0,1\" ControlPoint2=\"1,1\"/>" +
                            "</SplineDoubleKeyFrame.KeySpline>" +
                            "</SplineDoubleKeyFrame>" +
                            "</DoubleAnimationUsingKeyFrames></Storyboard>";

            return (Storyboard)System.Windows.Markup.XamlReader.Load(story);
        }

        /// <summary>
        /// occurs when an image is loaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void pictureClient_OpenReadCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            itemIndex++;

            TransformGroup imageTransform = new TransformGroup();

            RotateTransform imageRotate = new RotateTransform();
            TranslateTransform imageTranslate = new TranslateTransform();
            ScaleTransform imageScale = new ScaleTransform();

            imageRotate.Angle = 18.0 * itemIndex;

            imageTransform.Children.Add(imageRotate);
            imageTransform.Children.Add(imageTranslate);
            imageTransform.Children.Add(imageScale);

            if (e.Error != null) Debug.WriteLine(e.Error);

            Image imgElement = new Image();

            BitmapImage bmpImg = new BitmapImage();
            bmpImg.SetSource(e.Result);

            imgElement.Source = bmpImg;

            if (FactorHeight == 0.0) FactorHeight = 1.0;
            if (FactorWidth == 0.0) FactorWidth = 1.0;

            imgElement.Height = PictureLow * FactorHeight;
            imgElement.Width = PictureHigh * FactorWidth;

            imgElement.SetValue(Canvas.LeftProperty, (ActualWidth - imgElement.ActualWidth) / 2 );
            imgElement.SetValue(Canvas.TopProperty, (ActualHeight - imgElement.ActualHeight) / 2);

            imgElement.RenderTransformOrigin = new Point(0.5, 0.5);
            imgElement.RenderTransform = imageTransform;

            imgElement.SetValue(Canvas.NameProperty, "_img_" + itemIndex.ToString());
            _imageinfo.Add("_img_" + itemIndex.ToString(), new ImageDesc() { LinkTo=e.Address.ToString() });
            imgElement.MouseLeftButtonUp += new MouseButtonEventHandler(imgElement_MouseLeftButtonUp);

            imgElement.Visibility = Visibility.Collapsed;
            imgElement.SetValue(Canvas.ZIndexProperty, itemIndex);

            imgElement.MouseEnter += new MouseEventHandler(imgElement_MouseEnter);
            imgElement.MouseLeave += new MouseEventHandler(imgElement_MouseLeave);

            imgElement.Cursor = Cursors.Hand;

            LayoutRoot.Children.Add(imgElement);

            LoadContent();
        }

        /// <summary>
        /// this removes the highlight for a picture
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void imgElement_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender == currentSelection) return;

            Image tmp = (Image)sender;
            Storyboard st = StoryZoomOutOfPicture((string)tmp.GetValue(Canvas.NameProperty));
            LayoutRoot.Resources.Add(st);

            st.Begin();
        }

        /// <summary>
        /// this highlights a picture and changes the z-index
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void imgElement_MouseEnter(object sender, MouseEventArgs e)
        {
            if (currentSelection == sender || isMaximized) return;

            Image tmp = (Image)sender;
            Storyboard st = StoryZoomToPicture((string)tmp.GetValue(Canvas.NameProperty));
            LayoutRoot.Resources.Add(st);

            tmp.SetValue(Canvas.ZIndexProperty, zordermax);

            zordermax++;

            st.Begin();
        }

        /// <summary>
        /// occurs when clicked on an image
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void imgElement_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Image img;
            Storyboard st;

            if (currentSelection == sender) 
            {   
                CheckContainSubElement();
                return;
            }

            for (int i = 0; i < LayoutRoot.Children.Count; i++)
            {
                if (LayoutRoot.Children[i] is Image)
                {
                    itemIndex++;
                    img = (Image)LayoutRoot.Children[i];

                    img.Visibility = Visibility.Collapsed;
                }
            }

            img = (Image)sender;

            if (img.ActualHeight > img.ActualWidth)
            {
                ScaleY = ActualHeight / img.ActualHeight * 0.95;
                ScaleX = ScaleY;
            }
            else
            {
                ScaleX = ActualWidth / img.ActualWidth * 0.95;
                ScaleY = ScaleX;
            }

            img.Visibility = Visibility.Visible;

            GetImageData(img.Name);

            st = StoryBoardFadeIn((string)img.GetValue(Canvas.NameProperty), ScaleX, ScaleY);

            st.Completed += new EventHandler(st1_Completed);
            LayoutRoot.Resources.Add(st);
            st.Begin();

            currentSelection = img;
        }

        /// <summary>
        /// checks if the image contains a sub element
        /// </summary>
        private void CheckContainSubElement()
        {
            Storyboard st3 = null;
            TransformGroup tg = null;
            double angle = 0.0;

            if (linkto != string.Empty)
            {
                for (int i = 0; i < LayoutRoot.Children.Count; i++)
                {
                    if (LayoutRoot.Children[i] is Image)
                    {
                        LayoutRoot.Children[i].Visibility = Visibility.Visible;

                        tg = (TransformGroup)LayoutRoot.Children[i].RenderTransform;

                        angle = (double)tg.Children[0].GetValue(RotateTransform.AngleProperty);

                        st3 = StoryBoardFadeOut((string)LayoutRoot.Children[i].GetValue(Canvas.NameProperty), 
                            Math.Round(angle,0));

                        LayoutRoot.Resources.Add(st3);
                        st3.Begin();

                        LayoutRoot.Children.Remove(txtTitle);
                        LayoutRoot.Children.Remove(rtBackground);
                        LayoutRoot.Children.Remove(glassButton);

                        isMaximized = true;
                    }
                }

                if (null != st3) st3.Completed += new EventHandler(storyout);
            }

        }

        /// <summary>
        /// removes all pictures and generates an animation for that
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void storyout(object sender, EventArgs e)
        {
            LayoutRoot.Children.Clear();

            _lst.Clear();

            itemIndex = 0;
            currentSelection = null;

            _imageinfo.Clear();

            TargetAlbum = linkto;
            isMaximized = false;

            UpdateAlbum();
        } 
        
        /// <summary>
        /// gets the image data
        /// </summary>
        /// <param name="p"></param>
        void GetImageData(string p)
        {
            var result = from xe1 in xePhotoAlbum.Descendants("picture")
                         where xe1.Descendants("Picture").ElementAt(0).Value == _imageinfo[p].LinkTo
                         select xe1;

            title = (from ti in result.Descendants("Label")
                    select ti.Value).ElementAt(0);

            linkto = (from ti in result.Descendants("LinkToAlbum")
                      select ti.Value).ElementAt(0);
        }
    }

    /// <summary>
    /// stores information on an image
    /// </summary>
    public class ImageDesc
    {
        /// <summary>
        /// title of the image to be displayed
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// link to another album
        /// </summary>
        public string LinkTo { get; set; }

        /// <summary>
        /// description of the album
        /// </summary>
        public string Description { get; set; }
    }
}