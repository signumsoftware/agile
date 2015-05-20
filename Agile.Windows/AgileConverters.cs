using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using Signum.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using Signum.Entities.Files;
using Signum.Entities;

namespace Agile.Windows
{
    public static class AgileConverters
    {
        public static IValueConverter EmbeddedImageConverter = ConverterFactory.New((EmbeddedFileEntity file) =>
        {
            if (file == null)
                return null;

            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.StreamSource = new MemoryStream(file.BinaryFile);
            image.EndInit();

            return image;
        });

        public static IValueConverter ImageConverter = ConverterFactory.New((Lite<FileEntity> file) =>
        {
            if (file == null)
                return null;

            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.StreamSource = new MemoryStream(file.Retrieve().BinaryFile);
            image.EndInit();

            return image;
        }); 
    }
}
