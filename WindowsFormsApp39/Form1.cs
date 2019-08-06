using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Alturos.Yolo;
using Alturos.Yolo.Model;
using Emgu.CV;
using openalprnet;

namespace WindowsFormsApp39
{
    public partial class Form1 : Form
    {
        VideoCapture capture;
        public Form1()
        {
            InitializeComponent();
            Run();
        }
        public static string AssemblyDirectory
        {
            get
            {
                var codeBase = Assembly.GetExecutingAssembly().CodeBase;
                var uri = new UriBuilder(codeBase);
                var path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        public Rectangle boundingRectangle(List<Point> points)
        {
            // Add checks here, if necessary, to make sure that points is not null,
            // and that it contains at least one (or perhaps two?) elements

            var minX = points.Min(p => p.X);
            var minY = points.Min(p => p.Y);
            var maxX = points.Max(p => p.X);
            var maxY = points.Max(p => p.Y);

            return new Rectangle(new Point(minX, minY), new Size(maxX - minX, maxY - minY));
        }

        private static Image cropImage(Image img, Rectangle cropArea)
        {
            var bmpImage = new Bitmap(img);
            return bmpImage.Clone(cropArea, bmpImage.PixelFormat);
        }
        

        public static Bitmap combineImages(List<Image> images)
        {
            //read all images into memory
            Bitmap finalImage = null;

            try
            {
                var width = 0;
                var height = 0;

                foreach (var bmp in images)
                {
                    width += bmp.Width;
                    height = bmp.Height > height ? bmp.Height : height;
                }

                //create a bitmap to hold the combined image
                finalImage = new Bitmap(width, height);

                //get a graphics object from the image so we can draw on it
                using (var g = Graphics.FromImage(finalImage))
                {
                    //set background color
                    g.Clear(Color.Black);

                    //go through each image and draw it on the final image
                    var offset = 0;
                    foreach (Bitmap image in images)
                    {
                        g.DrawImage(image,
                                    new Rectangle(offset, 0, image.Width, image.Height));
                        offset += image.Width;
                    }
                }

                return finalImage;
            }
            catch (Exception ex)
            {
                if (finalImage != null)
                    finalImage.Dispose();

                throw ex;
            }
            finally
            {
                //clean up memory
                foreach (var image in images)
                {
                    image.Dispose();
                }
            }
        }
        private void Run()
        {
            try
            {
               //capture = new VideoCapture("https://win17.yayin.com.tr/erzurumbeltr/Yakutiye_Medrese/playlist.m3u8");
               capture = new VideoCapture("rtsp://admin:123Enc456@192.168.1.152:554/cam/realmonitor?channel=1&subtype=0");
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message);
                return;
            }
            Application.Idle += ProcessFrame;
        }
        

        private void ProcessFrame(object sender, EventArgs e)
        {
            var img = capture.QuerySmallFrame();
            pictureBox1.Image = img.Bitmap;
            Detect();
        }
       

        private void Detect()
        {
            var configurationDetector = new ConfigurationDetector();
            var config = configurationDetector.Detect();
            var yolo = new YoloWrapper(config);
            var memoryStream = new MemoryStream();
            pictureBox1.Image.Save(memoryStream, ImageFormat.Png);
            var _items = yolo.Detect(memoryStream.ToArray()).ToList();
            AddDetailsToPictureBox(pictureBox1, _items);
        }
        void plakaoku1(object image)
        {
            processImageFile(image as Image);
        }
        private void resetControls()
        {
            
        }
        private void processImageFile(Image fileName)
        {
            resetControls();
            var region = "eu" ;
            String config_file = Path.Combine(AssemblyDirectory, "openalpr.conf");
            String runtime_data_dir = Path.Combine(AssemblyDirectory, "runtime_data");
            using (var alpr = new AlprNet(region, config_file, runtime_data_dir))
            {
                if (!alpr.IsLoaded())
                {
                    //lbxPlates.Items.Add("Error initializing OpenALPR");
                    return;
                }
                //picOriginal.ImageLocation = fileName;
                //picOriginal.Load();
                string xxx = DateTime.Now.Second + "" + DateTime.Now.Minute + ".png";
                fileName.Save(xxx,ImageFormat.Png);
                var results = alpr.Recognize(xxx);

                var images = new List<Image>(results.Plates.Count());
                var i = 1;
                foreach (var result in results.Plates)
                {
                    var rect = boundingRectangle(result.PlatePoints);
                    var img = Image.FromFile(xxx);
                    var cropped = cropImage(img, rect);
                    images.Add(cropped);
                    //textBox1.Text= EnBenzeyen(result.TopNPlates);
                    //lbxPlates.Items.Add("\t\t-- Plate #" + i++ + " --");
                    foreach (var plate in result.TopNPlates)
                    {
                        Console.WriteLine(string.Format(@"{0} {1}% {2}",
                                                          plate.Characters.PadRight(12),
                                                          plate.OverallConfidence.ToString("N1").PadLeft(8),
                                                          plate.MatchesTemplate.ToString().PadLeft(8)));
                        //lbxPlates.Items.Add(string.Format(@"{0} {1}% {2}",
                        //                                  plate.Characters.PadRight(12),
                        //                                  plate.OverallConfidence.ToString("N1").PadLeft(8),
                        //                                  plate.MatchesTemplate.ToString().PadLeft(8)));
                    }
                        //  listBox1.Invoke(new Action(()=>listBox1.Items.Add(string.Format(@"{0} {1}% {2}",
                        //  result.TopNPlates[0].Characters.PadRight(12),
                        //  result.TopNPlates[0].OverallConfidence.ToString("N1").PadLeft(8),
                        //   result.TopNPlates[0].MatchesTemplate.ToString().PadLeft(8)))));
                }

                if (images.Any())
                {
                    //picLicensePlate.Image = combineImages(images);
                }
            }
        }

        

        private void AddDetailsToPictureBox(PictureBox pictureBox1, List<YoloItem> items)
        {
            var img = pictureBox1.Image;
            var font = new Font("Arial", 18, FontStyle.Bold);
            var brush = new SolidBrush(Color.Red);
            var graphics = Graphics.FromImage(img);
            foreach (var item in items)
            {
                if (item.Type.Equals("vehicle") || item.Type.Equals("car"))
                {
                    Bitmap bmpImage = new Bitmap(img);
                    var x = item.X;
                    var y = item.Y;
                    var width = item.Width;
                    var height = item.Height;
                    var tung = item.Type;
                    var rect = new Rectangle(x, y, width, height);
                    var pen = new Pen(Color.LightGreen, 6);
                    var point = new Point(x, y);
                    graphics.DrawRectangle(pen, rect);
                    graphics.DrawString(item.Type, font, brush, point);
                    
                    
                    System.Threading.Thread plakaoku = new System.Threading.Thread((this.plakaoku1));
                    plakaoku.Start(bmpImage.Clone(rect, bmpImage.PixelFormat));
                }
            }
            pictureBox1.Image = img;

        }
       


        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }
    }
}
