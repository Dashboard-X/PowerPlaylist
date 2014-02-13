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
using PhotoAlbum;
using System.Diagnostics;

namespace GridAlbum
{
    /// <summary>
    /// Gridalbum
    /// </summary>
    public class GridAlbum : PhotoBase
    {
        private ProgressBar Progress = null;    //shows the download progress

        private Canvas MainContent;     //holds the main content

        private Image CurrentImage = null;  //indicates if an image is currently zoomed

        private int HighestIndex = 0;   //controls the z index

        private double MaxWidth = 0.0;      //holds the maximum width
        private double MaxHeight = 0.0;     //holds the maximum height

        /// <summary>
        /// instantiates a new album
        /// </summary>
        public GridAlbum()
        {
            Images.CollectionChanged += OnImagesCollectionChanged;
            DownloadProgressChanged += OnDownloadProgressChanged;
        }

        /// <summary>
        /// Occurs when the images collection was changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnImagesCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Image Child;
            
            if (Progress == null) GenerateContent();

            for (int i = MainContent.Children.Count-1; i >=0 ; i--)
            {
                if (MainContent.Children[i] is Image) MainContent.Children.RemoveAt(i);
            }

            for (int i = 0; i < Images.Count; i++)
            {
                Child = new Image() { Source = Images[i] };
                Child.Visibility = Visibility.Collapsed;
                MainContent.Children.Add(Child);
            }
        }

        /// <summary>
        /// updates the metrics of the album
        /// </summary>
        private void RecalculateMetrics()
        {
            int RowCount = (int)Math.Ceiling(Math.Sqrt(Images.Count));
            MaxWidth = Math.Round(Width / RowCount);
            MaxHeight = Math.Round(Height / RowCount);

            Image WorkingImage;
            int ImageID = 0;

            for (int i = 0; i < RowCount; i++)
            {
                for (int j = 0; j < RowCount; j++)
                {
                    ImageID = i * RowCount + j;

                    if (ImageID < Images.Count)
                    {
                        WorkingImage = MainContent.Children[ImageID] as Image;

                        WorkingImage.SetValue(Canvas.TopProperty, j * MaxHeight);
                        WorkingImage.SetValue(Canvas.LeftProperty, i * MaxWidth);
                        WorkingImage.Height = MaxHeight;


                        WorkingImage.Width = MaxWidth;
                        WorkingImage.Stretch = Stretch.Uniform;
                        WorkingImage.RenderTransform = new TranslateTransform();
                        WorkingImage.RenderTransformOrigin = new Point(0.5, 0.5);

                        WorkingImage.Visibility = Visibility.Visible;
                        WorkingImage.Cursor = Cursors.Hand;
                        WorkingImage.MouseLeftButtonUp += new MouseButtonEventHandler(OnImageClick);

                        WorkingImage.SetValue(Canvas.ZIndexProperty, ImageID);
                    }
                }
            }

            HighestIndex = ImageID;

        }

        /// <summary>
        /// Occurs when an Image is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnImageClick(object sender, MouseButtonEventArgs e)
        {
            Image obj = sender as Image;
            Storyboard story;

            if (obj.Opacity == 0.0) return;

            double arg01, arg02;
            double topprop, leftprop;

            if (obj != CurrentImage)
            {
                CurrentImage = obj;

                arg01 = 1.0;
                arg02 = 0.0;
            }
            else
            {
                CurrentImage = null;

                arg01 = 0.0;
                arg02 = 1.0;
            }

            foreach (Image elem in MainContent.Children)
            {
                if (elem != obj)
                {
                    story = GenerateStoryboard(arg01, arg02, elem, "Opacity");

                    story.Begin();
                }
                else
                {
                    leftprop = (double)elem.GetValue(Canvas.LeftProperty);
                    topprop = (double)elem.GetValue(Canvas.TopProperty);
                    
                    if (CurrentImage != null)
                    {
                        HighestIndex++;
                        elem.SetValue(Canvas.ZIndexProperty, HighestIndex);

                        story = GenerateStoryboard(elem.ActualWidth, Width, elem, "Width");
                        story.Begin();

                        story = GenerateStoryboard(elem.ActualHeight, Height, elem, "Height");
                        story.Begin();

                        story = GenerateStoryboard(0, leftprop * -1, elem, "(UIElement.RenderTransform).(TranslateTransform.X)");
                        story.Begin();

                        story = GenerateStoryboard(0, topprop * -1, elem, "(UIElement.RenderTransform).(TranslateTransform.Y)");
                        story.Begin();
                    }
                    else
                    {
                        story = GenerateStoryboard(elem.ActualWidth, MaxWidth, elem, "Width");
                        story.Begin();

                        story = GenerateStoryboard(elem.ActualHeight, MaxHeight, elem, "Height");
                        story.Begin();

                        story = GenerateStoryboard(leftprop * -1, 0, elem, "(UIElement.RenderTransform).(TranslateTransform.X)");
                        story.Begin();

                        story = GenerateStoryboard(topprop * -1, 0, elem, "(UIElement.RenderTransform).(TranslateTransform.Y)");
                        story.Begin();
                    }
                }
            }
        }

        /// <summary>
        /// Occurs when the Download progess has changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="value"></param>
        void OnDownloadProgressChanged(object sender, double value)
        {
            if (value == 100.0)
            {
                Progress.Value = value;
                Progress.Visibility = Visibility.Collapsed;

                DownloadProgressChanged -= OnDownloadProgressChanged;

                MainContent.Children.Remove(Progress);

                RecalculateMetrics();
            }
            else
            {
                Progress.Value = value;
            }
        }

        /// <summary>
        /// Generates the Content at startup
        /// </summary>
        private void GenerateContent()
        {
            Progress = new ProgressBar();
            Progress.Maximum = 100.0;
            Progress.Width = Width - 10.0;
            Progress.Height = 10.0;
            Progress.Margin = new Thickness(5.0, Height / 2, 5.0, Height / 2);

            MainContent = new Canvas();
            MainContent.Background = Background;
            this.Content = MainContent;
            MainContent.Children.Add(Progress);
        }

        /// <summary>
        /// Generates a storyboard
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="target"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        private Storyboard GenerateStoryboard(double from, double to, UIElement target, string property)
        {
            Storyboard story;
            DoubleAnimation dbl;
            
            story = new Storyboard();
            dbl = new DoubleAnimation();
            dbl.From = from;
            dbl.To = to;
            dbl.Duration = TimeSpan.FromMilliseconds(250);
            story.Duration = TimeSpan.FromMilliseconds(250);

            Storyboard.SetTarget(story, target);
            Storyboard.SetTargetProperty(story, new PropertyPath(property));
            story.Children.Add(dbl);

            return story;
        }
    }
}
