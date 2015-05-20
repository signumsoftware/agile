using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using System.Windows;
using Signum.Windows;

namespace Agile.Windows
{
    public static class AgileImageLoader
    {
        public static BitmapSource GetImageSortName(string name)
        {
            return Signum.Windows.ImageLoader.LoadIcon(PackUriHelper.Reference("Images/" + name, typeof(AgileImageLoader)));
        }
    }
}
