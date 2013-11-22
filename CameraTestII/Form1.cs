using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Windows.Forms.VisualStyles;
using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Video;
using AForge.Video.DirectShow;
using AForge.Vision;
using AForge.Vision.Motion;
using NAudio.Wave;

namespace CameraTestII
{
    
    public partial class Form1 : Form
    {
        private VideoCaptureDevice _camera;
        private delegate void SetTextCallback(string text, ref TextBox box);
        private HSLFiltering _filterHsl = new HSLFiltering();
        private readonly Grayscale _filterGrayscale = Grayscale.CommonAlgorithms.BT709;
        private readonly Erosion3x3 _filterErosion = new Erosion3x3();
        private bool _keypressed = false;
        private int frameCount = 0;
        private SineProvider sine;
        private WaveOut wout;
        private BlobCounter _blob = new BlobCounter();
        public static float frequency = 0;
        public static float volume = 0;
        private int[] filterH = new int[90];
        private float[] filterS = new float[90];
        private float[] filterL = new float[90];
        private void setText(string text, ref TextBox box)
        {
            if (box.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(setText);
                try
                {
                    Invoke(d, new object[] {text, box});
                }
                catch (ObjectDisposedException)
                {
                }
            }
            else
            {
                box.Text = text;
            }
        }
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            _camera = new VideoCaptureDevice(videoDevices[0].MonikerString);
            _camera.NewFrame += CalibrationHandler;
            _camera.Start();
            textBox1.Text = trackBar1.Value.ToString();
            sine = new SineProvider();
            sine.SetWaveFormat(16000, 1);
            wout = new WaveOut();
            wout.Init(sine);
            wout.Play();
        }

        private void CalibrationHandler(object sender, NewFrameEventArgs e)
        {
            Bitmap frame = new Bitmap(e.Frame);
            Color c = Color.FromArgb(255, 0, 0);
            if (!_keypressed)
            {
                for (int i = -2; i <= 2; i++)
                {
                    for (int j = -2; j <= 2; j++)
                    {
                        frame.SetPixel(100 + i, 100 + j, c);
                    }
                }
                setText("Please hold the pointer in marked area and press any key", ref textBox2);
                pictureBox1.Image = frame;
            }
            else if (frameCount < 90)
            {
                int h = 0;
                float s = 0, l = 0;
                for (int i = -2; i <= 2; i++)
                {
                    for (int j = -2; j <= 2; j++)
                    {
                        RGB col = new RGB(frame.GetPixel(100 + i, 100 + j));
                        HSL hsl = HSL.FromRGB(col);
                        h += hsl.Hue;
                        s += hsl.Saturation;
                        l += hsl.Luminance;
                    }
                }
                filterH[frameCount] = h/25;
                filterL[frameCount] = l/25;
                filterS[frameCount] = s/25;
                for (int i = -2; i <= 2; i++)
                {
                    for (int j = -2; j <= 2; j++)
                    {
                        frame.SetPixel(100 + i, 100 + j, c);
                    }
                }
                setText("Calibrating, frame: " + frameCount.ToString() + "/90", ref textBox2);
                frameCount++;
                pictureBox1.Image = frame;
            }
            else
            {
                int h = (int)filterH.Average();
                float s = filterS.Average();
                float l = filterL.Average();
                IntRange hRange = new IntRange(h - 36, h + 36);
                Range sRange = new Range(s - 0.1f, s + 0.1f);
                Range lRange = new Range(l - 0.1f, l + 0.1f);
                _filterHsl = new HSLFiltering(hRange, sRange, lRange);
                setText("", ref textBox2);
                _camera.NewFrame -= CalibrationHandler;
                _camera.NewFrame += NewFrameHandler;
            }
            
        }
        private void NewFrameHandler(object sender, NewFrameEventArgs e)
        {
            Bitmap frame = new Bitmap(e.Frame);
            UnmanagedImage unmanaged = UnmanagedImage.FromManagedImage(frame);
            Color c = Color.FromArgb(255, 255, 255);
            for (int i = -2; i <= 2; i++)
            {
                for (int j = -2; j <= 2; j++)
                {
                    unmanaged.SetPixel(100+i, 100+j, c);
                }
            }
            Color c2 = frame.GetPixel(100, 100);
            setText(c2.R.ToString(), ref textBoxR);
            setText(c2.G.ToString(), ref textBoxG);
            setText(c2.B.ToString(), ref textBoxB);
            HSL hsl = HSL.FromRGB(new RGB(c2));
            setText(hsl.Hue.ToString(), ref textBoxH);
            setText(hsl.Saturation.ToString(), ref textBoxS);
            setText(hsl.Luminance.ToString(), ref textBoxL);
            _filterHsl.ApplyInPlace(unmanaged);
            UnmanagedImage unmanaged2 = _filterGrayscale.Apply(unmanaged);
            _filterErosion.ApplyInPlace(unmanaged2);
            _filterErosion.ApplyInPlace(unmanaged2);
            _blob.ProcessImage(unmanaged2);
            Rectangle[] rects = _blob.GetObjectsRectangles();
            int xCent = -1, yCent = -1;
            if (rects.Length != 0)
            {
                Rectangle r = rects.Aggregate((r1, r2) => (r1.Height*r1.Width) > (r2.Height*r2.Width) ? r1 : r2);
                xCent = r.Left + r.Width/2;
                yCent = r.Top + r.Height/2;
            }
            if (xCent >= 2 && yCent >= 2 && xCent <= unmanaged.Width-2 && yCent <= unmanaged.Height-2)
            {
                for (int i = -2; i <= 2; i++)
                {
                    for (int j = -2; j <= 2; j++)
                    {
                        unmanaged2.SetPixel(xCent + i, yCent + j, 255);
                    }
                }
            }
            pictureBox2.Image = unmanaged2.ToManagedImage();
            pictureBox1.Image = frame;
            volume = 1 - yCent/960.0f;
            frequency = 440 + xCent;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            _camera = null;
            wout.Stop();
            Application.Exit();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _keypressed = true;
        }
    }
    public class SineProvider : WaveProvider32
    {
        private int sample;
        public override int Read(float[] buffer, int offset, int sampleCount)
        {
            float _amplitude = Form1.volume;
            float _frequency = Form1.frequency;
            int sampleRate = WaveFormat.SampleRate;
            for (int n = 0; n < sampleCount; n++)
            {
                buffer[n + offset] = (float)(_amplitude * Math.Sin((2 * Math.PI * sample * _frequency) / sampleRate));
                sample++;
                if (sample >= sampleRate) sample = 0;
            }
            
   
            return sampleCount;
        }
    }
}
