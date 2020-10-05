using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Drawing.Imaging;

namespace GraphicalPixelChange
{
    public partial class Form1 : Form
    {
        // лист где храним все готовые обработанные битмапы
        List<Bitmap> _bitmaps;

        public Form1()
        {
            InitializeComponent();
        }

        private async void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var sw = Stopwatch.StartNew();
            // блокируем все кнопки
            menuStrip1.Enabled = trackBar1.Enabled = false;
            // размер листа 100
            _bitmaps = new List<Bitmap>(100);
            Bitmap sourceBitmap;

            // запускаем диалоговое окно выбора изображения
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                // битмап исходного изображения
                sourceBitmap = LoadBitmap(openFileDialog1.FileName);
                pictureBox1.Image = null;
                // добавляем в лист все пиксели изображения (W*H)
                await Task.Run(new Action(() => Cycle(sourceBitmap)));
            }
            // разблокируем их
            menuStrip1.Enabled = trackBar1.Enabled = true;

            sw.Stop();
            this.Text = sw.Elapsed.ToString();
        }

        // метод для загрузки исходного изображения, без занимания его самого
        // чтобы мы его "отпустили" и смогли потом использовать и открывать
        public static Bitmap LoadBitmap(string fileName)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                return new Bitmap(fs);
        }

        public unsafe static byte[,,] BitmapToByteRgb(Bitmap bmp)
        {
            int width = bmp.Width,
                height = bmp.Height;
            byte[,,] res = new byte[4, height, width];
            BitmapData bd = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            try
            {
                byte* curpos;
                for (int h = 0; h < height; h++)
                {
                    curpos = ((byte*)bd.Scan0) + h * bd.Stride;
                    for (int w = 0; w < width; w++)
                    {
                        res[3, h, w] = *(curpos++);
                        res[2, h, w] = *(curpos++);
                        res[1, h, w] = *(curpos++);
                        res[0, h, w] = *(curpos++);

                    }
                }
            }
            finally
            {
                bmp.UnlockBits(bd);
            }
            return res;
        }

        private void Cycle(Bitmap sourceBitmap)
        {
            var sw = Stopwatch.StartNew();
            // заполняем лист пикселей
            int pixelsCount = sourceBitmap.Height * sourceBitmap.Width;
            List<Pixels> pixels = new List<Pixels>(pixelsCount);
            //for (var y = 0; y < sourceBitmap.Height; y++)
            //{
            //    for (var x = 0; x < sourceBitmap.Width; x++)
            //    {
            //        pixels.Add(new Pixels() { point = new Point(x, y), color = sourceBitmap.GetPixel(x, y) });
            //    }
            //}
            var arr = BitmapToByteRgb(sourceBitmap);
            for (var y = 0; y < sourceBitmap.Height; y++)
            {
                for (var x = 0; x < sourceBitmap.Width; x++)
                {
                    pixels.Add(new Pixels() { point = new Point(x, y), color = Color.FromArgb(arr[0, y, x], arr[1, y, x], arr[2, y, x], arr[3, y, x]) });
                }
            }


            sw.Stop();
            MessageBox.Show(sw.Elapsed.ToString());

            Random rnd = new Random();
            int end = pixelsCount;
            Bitmap tempBitmap = new Bitmap(sourceBitmap.Width, sourceBitmap.Height);
            for (var i = 1; i < 100; i++)
            {
                List<Pixels> pixelListInState = new List<Pixels>(pixelsCount / 100);
                for (var j = 0; j < pixelsCount / 100; j++)
                {
                    int rndNum = rnd.Next(end);
                    pixelListInState.Add(new Pixels() { point = new Point(pixels[rndNum].point.X, pixels[rndNum].point.Y), color = pixels[rndNum].color });
                    pixels.RemoveAt(rndNum);
                    end--;
                }
                foreach (var tempPix in pixelListInState)
                {
                    tempBitmap.SetPixel(tempPix.point.X, tempPix.point.Y, tempPix.color);
                }
                _bitmaps.Add(new Bitmap(tempBitmap)); // добавляем битмап в список

                // строка состояния
                this.Invoke(new Action(() =>
                {
                    this.Text = $"{i} %";
                }));
            }
            _bitmaps.Add(sourceBitmap); // последнее изображение

            // вывод изображения, на котором сейчас трекбар
            this.Invoke(new Action(() =>
            {
                pictureBox1.Image = _bitmaps.ElementAt(trackBar1.Value - 1);
            }));

            // конечное состояние 
            this.Invoke(new Action(() =>
            {
                this.Text = $"100 %";
            }));
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            // при прокрутке будем подгружать изображения из листа bitmap, в случае если изображение есть и список не нулевой
            if (_bitmaps.ElementAt(trackBar1.Value - 1) != null || _bitmaps.Count != 0)
                pictureBox1.Image = _bitmaps.ElementAt(trackBar1.Value - 1);
        }
    }
}
