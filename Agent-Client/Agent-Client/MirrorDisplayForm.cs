using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Agent_Client
{
    public class MirrorDisplayForm : Form
    {
        private PictureBox pictureBox;

        public MirrorDisplayForm()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            //this.FormBorderStyle = FormBorderStyle.None;
            //this.WindowState = FormWindowState.Maximized;
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.TopMost = true;
            this.BackColor = Color.Black;
            this.KeyPreview = true;

            pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                SizeMode = PictureBoxSizeMode.Zoom
            };

            this.Controls.Add(pictureBox);

            this.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                    this.Close();
            };

            this.FormClosed += (_, __) =>
            {
                if (pictureBox.Image != null)
                {
                    pictureBox.Image.Dispose();
                    pictureBox.Image = null;
                }
                pictureBox.Dispose();
            };
        }

        public void UpdateImage(byte[] jpegBytes)
        {
            try
            {
                using var ms = new MemoryStream(jpegBytes);
                using var img = Image.FromStream(ms);
                var bmp = new Bitmap(img);

                if (pictureBox.Image != null)
                {
                    var old = pictureBox.Image;
                    pictureBox.Image = null;
                    old.Dispose();
                }

                pictureBox.Image = bmp;
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ UpdateImage failed: " + ex.Message);
            }
        }
    }
}
