using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;



namespace ShowFormComplex2_2008
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>

    public partial class Window1 : System.Windows.Window
    {

        public delegate void ExitDelegate();
        public Window1()
        {
            InitializeComponent();
            
            Stream str = this.GetType().Assembly.GetManifestResourceStream("ShowFormComplex2_2008.Resources.toolBarButton.jpg");
            if (str != null)
            {
                JpegBitmapDecoder decoder = new JpegBitmapDecoder(str, BitmapCreateOptions.None, BitmapCacheOption.None);
                ImageSource src = decoder.Frames[0];
                toolBarImgButton.Source = src;
                toolBarImgButton2.Source = src;
            }
            else
            {
                
            }
            if (Environment.GetCommandLineArgs().Length > 1)
            {

                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, new ExitDelegate(ExitMethod));
            }
        }

        public void ExitMethod()
        {
            System.Threading.Thread.Sleep(2000);
            
            Environment.Exit(0);
        }

        
       

    }
}