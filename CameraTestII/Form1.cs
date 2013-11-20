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
        private readonly HSLFiltering _filterHsl = new HSLFiltering(new IntRange(170, 230), new Range(0.8f, 1), new Range(0, 0.9f));
        private readonly Grayscale _filterGrayscale = Grayscale.CommonAlgorithms.BT709;
        private readonly Erosion3x3 _filterErosion = new Erosion3x3();
        private SineProvider sine;
        private WaveOut wout;
        private BlobCounter _blob = new BlobCounter();
        private int frequency = 0;
        private int volume = 0;
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
            _camera.NewFrame += new NewFrameEventHandler(NewFrameHandler);
            _camera.Start();
            textBox1.Text = trackBar1.Value.ToString();
            sine = new SineProvider();
            sine.SetWaveFormat(16000, 1);
            wout = new WaveOut();
            wout.Init(sine);
            wout.Play();
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
            sine.Amplitude = 1 - yCent/960.0f;
            sine.Frequency = 440 + xCent;
        }
    }
    public class SineProvider : WaveProvider32
    {
        private int sample;
        public float Frequency { get; set; }
        public float Amplitude { get; set; }
        public override int Read(float[] buffer, int offset, int sampleCount)
        {
            int sampleRate = WaveFormat.SampleRate;
            for (int n = 0; n < sampleCount; n++)
            {
                buffer[n + offset] = (float)(Amplitude * Math.Sin((2 * Math.PI * sample * Frequency) / sampleRate));
                sample++;
                if (sample >= sampleRate) sample = 0;
            }
            
   
            return sampleCount;
        }
    }
}
