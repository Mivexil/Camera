using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Math;
using AForge.Video;
using AForge.Video.DirectShow;
using NAudio.Wave;
using Point = System.Drawing.Point;

namespace CameraTestII
{
    
    public partial class Form1 : Form
    {
        private VideoCaptureDevice _camera;
        private delegate void SetTextCallback(string text, ref TextBox box);
        private delegate void SetValCallback(int val, ref ProgressBar bar);
        private HSLFiltering _filterHsl = new HSLFiltering();
        private readonly Grayscale _filterGrayscale = Grayscale.CommonAlgorithms.BT709;
        private readonly Erosion3x3 _filterErosion = new Erosion3x3();
        private bool _keypressed;
        private int _frameCount;
        private SineProvider _sine;
        private WaveOut _wout;
        private readonly BlobCounter _blob = new BlobCounter();
        public static float Frequency = 0;
        public static float Volume = 0;
        private readonly int[] _filterH = new int[30];
        private readonly float[] _filterS = new float[30];
        private readonly float[] _filterL = new float[30];
        private int _filterHValue;
        private float _filterSValue, _filterLValue;
        private float _filterHPerc, _filterLPerc, _filterSPerc;
        private const double _epsilon = 20;
        private List<Point> _hitPoints = new List<Point>();
        private Queue<Point> _lastPoints = new Queue<Point>();

        private double CalcLength(Point? p)
        {
            if (p != null)
            {
                return Math.Sqrt(p.Value.X*p.Value.X + p.Value.Y*p.Value.Y);
            }
            else return 0;
        }
        private void SetText(string text, ref TextBox box)
        {
            if (box.InvokeRequired)
            {
                SetTextCallback d = SetText;
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

        private void SetVal(int value, ref ProgressBar bar)
        {
            if (bar.InvokeRequired)
            {
                SetValCallback d = SetVal;
                try
                {
                    Invoke(d, new object[] {value, bar});
                }
                catch (ObjectDisposedException)
                {
                    
                }
            }
            else
            {
                bar.Value = value;
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
            textBoxHF.Text = trackBarH.Value.ToString();
            //_sine = new SineProvider();
            //_sine.SetWaveFormat(16000, 1);
            //_wout = new WaveOut();
            //_wout.Init(_sine);
            //_wout.Play();
        }

        private static bool[] Directions(Queue<Point> q)
        {
            bool[] ret = new bool[4];
            for (int i = 1; i < 5; i++)
            {
                if (q.ToArray()[i].Y >= q.ToArray()[i - 1].Y)
                {
                    ret[i - 1] = true;
                }
                else
                {
                    ret[i - 1] = false;
                }
            }
            return ret;
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
                SetText("Please hold the pointer in marked area and press any key", ref textBox2);
                pictureBox1.Image = frame;
            }
            else if (_frameCount < 30)
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
                _filterH[_frameCount] = h/25;
                _filterL[_frameCount] = l/25;
                _filterS[_frameCount] = s/25;
                for (int i = -2; i <= 2; i++)
                {
                    for (int j = -2; j <= 2; j++)
                    {
                        frame.SetPixel(100 + i, 100 + j, c);
                    }
                }
                SetText("Calibrating, frame: " + _frameCount.ToString() + "/30", ref textBox2);
                _frameCount++;
                pictureBox1.Image = frame;
            }
            else
            {
                _filterHValue = (int)_filterH.Average();
                _filterSValue = _filterS.Average();
                _filterLValue = _filterL.Average();
                IntRange hRange = new IntRange((int)(_filterHValue - (_filterHPerc/2)*_filterHValue), (int)(_filterHValue + (_filterHPerc/2)*_filterHValue));
                Range sRange = new Range(_filterSValue - (_filterSPerc/2)*_filterSValue, _filterSValue + (_filterSPerc/2)*_filterSValue);
                Range lRange = new Range(_filterLValue - (_filterLPerc / 2) * _filterLValue, _filterLValue + (_filterLPerc / 2) * _filterLValue);
                _filterHsl = new HSLFiltering(hRange, sRange, lRange);
                SetText("", ref textBox2);
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
            SetText(((_filterHValue - (_filterHPerc / 2) * _filterHValue).ToString()), ref textBoxR);
            SetText((_filterSValue - (_filterSPerc / 2) * _filterSValue).ToString(), ref textBoxG);
            SetText((_filterLValue - (_filterLPerc / 2) * _filterLValue).ToString(), ref textBoxB);
            SetText(((_filterHValue + (_filterHPerc / 2) * _filterHValue).ToString()), ref textBoxH);
            SetText((_filterSValue + (_filterSPerc / 2) * _filterSValue).ToString(), ref textBoxS);
            SetText((_filterLValue + (_filterLPerc / 2) * _filterLValue).ToString(), ref textBoxL);
            IntRange hRange = new IntRange((int)(_filterHValue - (_filterHPerc / 2) * _filterHValue), (int)(_filterHValue + (_filterHPerc / 2) * _filterHValue));
            Range sRange = new Range(_filterSValue - (_filterSPerc / 2) * _filterSValue, _filterSValue + (_filterSPerc / 2) * _filterSValue);
            Range lRange = new Range(_filterLValue - (_filterLPerc / 2) * _filterLValue, _filterLValue + (_filterLPerc / 2) * _filterLValue);
            _filterHsl = new HSLFiltering(hRange, sRange, lRange);
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
            if (_lastPoints.Count >= 5)
            {
                _lastPoints.Dequeue();
            }
            _lastPoints.Enqueue(new Point(xCent, yCent));
            if (_lastPoints.Count == 5)
            {
                bool[] upDownBools = Directions(_lastPoints);
                if (upDownBools[0] && upDownBools[1] && !upDownBools[2] && !upDownBools[3])
                {
                    if (_lastPoints.ToArray()[2].Y - _lastPoints.ToArray()[0].Y > _epsilon &&
                        _lastPoints.ToArray()[2].Y - _lastPoints.ToArray()[4].Y > _epsilon)
                    {
                        _hitPoints.Add(_lastPoints.ToArray()[2]);
                        _lastPoints.Clear();
                    }
                }
            }
            foreach (Point p in _hitPoints)
            {
                for (int i = -2; i <= 2; i++)
                {
                    for (int j = -2; j <= 2; j++)
                    {
                        if (p.X + i >= 0 && p.X + i <= unmanaged2.Width && p.Y + i >= 0 && p.Y + i <= unmanaged2.Height)
                        {
                            unmanaged2.SetPixel(p.X + i, p.Y + j, 255);
                        }
                    }
                }
            }
            pictureBox2.Image = unmanaged2.ToManagedImage();
            pictureBox1.Image = frame;
            Volume = 1 - yCent/960.0f;
            Frequency = 440 + xCent;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            _camera = null;
            //_wout.Stop();
            Environment.Exit(0);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _keypressed = true;
        }

        private void trackBarH_Scroll(object sender, EventArgs e)
        {
            _filterHPerc = trackBarH.Value/ 200.0f;
            SetText(_filterHPerc.ToString(), ref textBoxHF);
        }

        private void trackBarS_Scroll(object sender, EventArgs e)
        {
            _filterSPerc = trackBarS.Value / 200.0f;
            SetText(_filterSPerc.ToString(), ref textBoxSF);
        }

        private void trackBarL_Scroll(object sender, EventArgs e)
        {
            _filterLPerc = trackBarL.Value / 200.0f;
            SetText(_filterLPerc.ToString(), ref textBoxLF);
        }

    }
    public class SineProvider : WaveProvider32
    {
        private int _sample;
        public override int Read(float[] buffer, int offset, int sampleCount)
        {
            float amplitude = Form1.Volume;
            float frequency = Form1.Frequency;
            int sampleRate = WaveFormat.SampleRate;
            for (int n = 0; n < sampleCount; n++)
            {
                buffer[n + offset] = (float)(amplitude * Math.Sin((2 * Math.PI * _sample * frequency) / sampleRate));
                _sample++;
                if (_sample >= sampleRate) _sample = 0;
            }
            
   
            return sampleCount;
        }
    }
}
