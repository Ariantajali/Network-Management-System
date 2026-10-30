using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Agent_Client.Services
{
    public static class ScreenCaptureService
    {
        public static byte[] CaptureScreen(int quality = 80, int maxWidth = 1600, int maxHeight = 900)
        {
            var bounds = Screen.PrimaryScreen.Bounds;
            using var bmp = new Bitmap(bounds.Width, bounds.Height);
            using var g = Graphics.FromImage(bmp);
            g.CopyFromScreen(0, 0, 0, 0, bounds.Size);

            Image resized = bmp;
            if (bmp.Width > maxWidth || bmp.Height > maxHeight)
                resized = new Bitmap(bmp, new Size(maxWidth, maxHeight));

            using var ms = new MemoryStream();
            var enc = ImageCodecInfo.GetImageDecoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
            var ep = new EncoderParameters(1);
            ep.Param[0] = new EncoderParameter(Encoder.Quality, quality);
            resized.Save(ms, enc, ep);

            if (!ReferenceEquals(resized, bmp)) resized.Dispose();
            return ms.ToArray();
        }
    }
}
