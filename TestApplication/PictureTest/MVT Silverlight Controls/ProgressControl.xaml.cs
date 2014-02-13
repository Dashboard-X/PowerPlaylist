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

namespace MVT
{
    public partial class ProgressControl : UserControl
    {
        private Color _fill;
        private Color _background;
        private Brush _foreground;
        private string _text;

        public ProgressControl()
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(ProgressControl_Loaded);
        }

        void ProgressControl_Loaded(object sender, RoutedEventArgs e)
        {
            animation.Begin();
        }

        public Color FillColor
        {
            get
            {
                return _fill;
            }
            set
            {
                _fill = value;

                rad1.Color = value;
                rad2.Color = value;
                rad3.Color = value;
                rad4.Color = value;
            }
        }

        public Brush Foreground
        {
            get
            {
                return _foreground;
            }
            set
            {
                _foreground = value;
                txt.Foreground = value;
            }
        }

        public Color Background
        {
            get
            {
                return _background;
            }
            set
            {
                _background = value;
                Color tmp = Color.FromArgb(0, value.R, value.G, value.B);

                blanc1.Color = tmp;
                blanc2.Color = tmp;
                blanc3.Color = tmp;
                blanc4.Color = tmp;
                blanc5.Color = tmp;
                blanc6.Color = tmp;
            }
        }

        public string Text
        {
            get
            {
                return _text;
            }
            set
            {
                _text = value;

                txt.Text = _text;

                txt.SetValue(Canvas.LeftProperty, (ActualWidth - txt.ActualWidth) / 2);
                txt.SetValue(Canvas.TopProperty, (ActualHeight - txt.ActualHeight) / 2);
            }
        }
    }
}
