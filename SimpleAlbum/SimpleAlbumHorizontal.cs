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
using System.Diagnostics;
using System.Windows.Media.Imaging;

///The main Namespace
namespace PhotoAlbum
{
    /// <summary>
    /// displays a simple album in a horizontal list
    /// </summary>
    public class SimpleAlbumHorizontal : PhotoBase
    {
        private StackPanel ImageItems;  //Container for all images

        private Image FirstImage, SecondImage;  //Images to swap between
            
        private bool IsFirstImage = true;   //currently displayed image

        private ScrollViewer Scroller;  //The Scrollviewer

        private ProgressBar Progress;   //indicates the Download Progress

        /// <summary>
        /// Instantiates a new Album
        /// </summary>
        public SimpleAlbumHorizontal()
        {
            this.Images.CollectionChanged += Images_CollectionChanged;
            this.DownloadProgressChanged += OnDownloadProgressChanged;
        }

        /// <summary>
        /// Occurs when the Download Progress was changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="value"></param>
        void OnDownloadProgressChanged(object sender, double value)
        {
            if (value == 100.0)
            {
                Scroller.Visibility = Visibility.Visible;

                if (ImageItems.Children.Count > 0)
                    OnImageClick(ImageItems.Children[0], null);

                Progress.Value = value;
                Progress.Visibility = Visibility.Collapsed;

                DownloadProgressChanged -= OnDownloadProgressChanged;
            }
            else
            {
                Progress.Value = value;
                Scroller.Visibility = Visibility.Collapsed;
            }

        }

        /// <summary>
        /// Occurs when new Images where added e.g.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Images_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (ImageItems == null) GenerateContent();

            ImageItems.Children.Clear();
            Image tmpImage;

            foreach (ImageSource image in Images)
            {
                tmpImage = new Image();

                tmpImage.Source = image;

                tmpImage.Height = 40;
                tmpImage.Margin = new Thickness(5.0);
                tmpImage.Opacity = 0.8;

                tmpImage.Cursor = Cursors.Hand;

                tmpImage.MouseEnter += (sender1, e1) =>
                    {
                        Storyboard story = new Storyboard();
                        DoubleAnimation dblanim = new DoubleAnimation();
                        dblanim.From = 0.8;
                        dblanim.To = 1.0;
                        dblanim.Duration = TimeSpan.FromMilliseconds(333);

                        Storyboard.SetTargetProperty(dblanim, new PropertyPath("Opacity"));
                        Storyboard.SetTarget(dblanim, (DependencyObject)sender1);
                        story.Children.Add(dblanim);
                        story.Begin();
                    };

                tmpImage.MouseLeave += (sender2, e2) =>
                {
                    Storyboard story = new Storyboard();
                    DoubleAnimation dblanim = new DoubleAnimation();
                    dblanim.From = 1.0;
                    dblanim.To = 0.8;
                    dblanim.Duration = TimeSpan.FromMilliseconds(333);

                    Storyboard.SetTargetProperty(dblanim, new PropertyPath("Opacity"));
                    Storyboard.SetTarget(dblanim, (DependencyObject)sender2);
                    story.Children.Add(dblanim);
                    story.Begin();
                };

                tmpImage.MouseLeftButtonUp += OnImageClick;

                ImageItems.Children.Add(tmpImage);
            }
        }

        /// <summary>
        /// Image is selected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnImageClick(object sender, MouseButtonEventArgs e)
        {
            Image send = sender as Image;

            if (send != null)
            {
                if (IsFirstImage)
                {
                    FirstImage.Source = send.Source;
                    FadeIn(FirstImage).Begin();
                    FadeOut(SecondImage).Begin();

                    IsFirstImage = false;
                }
                else
                {
                    SecondImage.Source = send.Source;
                    FadeIn(SecondImage).Begin();
                    FadeOut(FirstImage).Begin();

                    IsFirstImage = true;
                }
            }
        }

        /// <summary>
        /// Creates a Storyboard to fadein
        /// </summary>
        /// <param name="Target"></param>
        /// <returns></returns>
        private Storyboard FadeIn(Image Target)
        {
            Storyboard story = new Storyboard();
            DoubleAnimation dblanim = new DoubleAnimation();
            dblanim.From = 0.0;
            dblanim.To = 1.0;
            dblanim.Duration = TimeSpan.FromMilliseconds(333);

            Storyboard.SetTargetProperty(dblanim, new PropertyPath("Opacity"));
            Storyboard.SetTarget(dblanim, Target);
            story.Children.Add(dblanim);

            return story;
        }

        /// <summary>
        /// creates a storyboard to fadeout
        /// </summary>
        /// <param name="Target"></param>
        /// <returns></returns>
        private Storyboard FadeOut(Image Target)
        {
            Storyboard story = new Storyboard();
            DoubleAnimation dblanim = new DoubleAnimation();
            dblanim.From = 1.0;
            dblanim.To = 0.0;
            dblanim.Duration = TimeSpan.FromMilliseconds(333);

            Storyboard.SetTargetProperty(dblanim, new PropertyPath("Opacity"));
            Storyboard.SetTarget(dblanim, Target);
            story.Children.Add(dblanim);
            
            return story;
        }

        /// <summary>
        /// generates the main content
        /// </summary>
        private void GenerateContent()
        {
            Grid MainGrid = new Grid();
            Scroller = new ScrollViewer();
            Scroller.BorderThickness = new Thickness(0.0);

            Progress = new ProgressBar();

            double HeightDifference = (Height - 90.0) / 2;

            Progress.Margin = new Thickness(10.0, HeightDifference, 10.0, HeightDifference);
            Progress.Maximum = 100;
            Progress.Value = 0.0;

            MainGrid.Width = this.Width;
            MainGrid.Height = this.Height;
            MainGrid.Background = this.Background;
            MainGrid.Children.Add(Progress);

            MainGrid.RowDefinitions.Add(new RowDefinition());
            MainGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(75, GridUnitType.Pixel) });

            Scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            Scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            Scroller.SetValue(Grid.RowProperty, 1);

            ImageItems = new StackPanel();
            ImageItems.Orientation = Orientation.Horizontal;

            Scroller.Content = ImageItems;

            FirstImage = new Image();
            SecondImage = new Image();

            FirstImage.SetValue(Grid.RowProperty, 0);
            SecondImage.SetValue(Grid.RowProperty, 0);

            MainGrid.Children.Add(FirstImage);
            MainGrid.Children.Add(SecondImage);

            MainGrid.Children.Add(Scroller);

            Content = MainGrid;
        }

    }
}
