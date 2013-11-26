using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Video;
using AForge.Video.DirectShow;
using Point = System.Drawing.Point; //instead of AForge.Point

namespace CameraTestII
{
    
    public partial class Form1 : Form
    {
        private VideoCaptureDevice _camera;
        private HSLFiltering _filterHSL = new HSLFiltering();
        private readonly Grayscale _filterGrayscale = Grayscale.CommonAlgorithms.BT709;
        private readonly Erosion3x3 _filterErosion = new Erosion3x3();
        private bool _keypressed;
        private readonly BlobCounter _blob = new BlobCounter();
        private int _filterHValue;
        private float _filterSValue, _filterLValue;
        private float _filterHPerc, _filterLPerc, _filterSPerc;
        private IntRange _filterHRange = new IntRange(0, 0);
        private Range _filterSRange = new Range(0, 0);
        private Range _filterLRange = new Range(0, 0);
        private const double _epsilon = 20;
        private List<Point> _hitPoints = new List<Point>();
        private Queue<Point> _lastPoints = new Queue<Point>();
        private Bitmap _lastFrame;
        /*Because we can't access controls from outside the UI thread normally*/
        public void SetText(string text, TextBox box)
        {
            if (box.InvokeRequired)
            {
                try
                {
                    Invoke(new Action<string, TextBox>(SetText), new object[] {text, box});
                }
                catch (ObjectDisposedException)
                {
                    //Basically it means we're closing the application, so who cares.
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
            SetText("Please hold the pointer in marked area and press the button to the right", textBox2);
        }

        private static bool[] SetDirections(Queue<Point> q)
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

        private void SetupHSLFilter(Bitmap frame)
        {
            UnmanagedImage unmanaged = UnmanagedImage.FromManagedImage(frame);
            _filterHValue = 0;
            _filterSValue = 0;
            _filterLValue = 0;
            for (int i = -2; i <= 2; i++)
            {
                for (int j = -2; j <= 2; j++)
                {
                    HSL hsl = HSL.FromRGB(new RGB(unmanaged.GetPixel(100 + i, 100 + j)));
                    _filterHValue += hsl.Hue;
                    _filterSValue += hsl.Saturation;
                    _filterLValue += hsl.Luminance;
                }
            }
            _filterHValue /= 25;
            _filterSValue /= 25;
            _filterLValue /= 25;
            HSLChanged();

        }
        private void CalibrationHandler(object sender, NewFrameEventArgs e)
        {
            _lastFrame = e.Frame;
            UnmanagedImage frame = UnmanagedImage.FromManagedImage(e.Frame);
            Color c = Color.FromArgb(255, 0, 0);
            for (int i = -2; i <= 2; i++)
            {
                for (int j = -2; j <= 2; j++)
                {
                    frame.SetPixel(100 + i, 100 + j, c);
                }
            }
            pictureBox1.Image = frame.ToManagedImage();
            if (_keypressed)
            {
                SetupHSLFilter(_lastFrame);
                SetText("", textBox2);
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
            _filterHSL.ApplyInPlace(unmanaged);
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
            MovementDetector(xCent, yCent, unmanaged2);
            pictureBox2.Image = unmanaged2.ToManagedImage();
            pictureBox1.Image = frame;
        }

        private void MovementDetector(int xCent, int yCent, UnmanagedImage unmanaged2)
        {
            if (_lastPoints.Count >= 5)
            {
                _lastPoints.Dequeue();
            }
            _lastPoints.Enqueue(new Point(xCent, yCent));
            if (_lastPoints.Count == 5)
            {
                bool[] upDownBools = SetDirections(_lastPoints);
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
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            _camera = null;
            Environment.Exit(0);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _keypressed = true;
        }

        private void HSLChanged()
        {
            SetText(((_filterHValue - (_filterHPerc / 2) * _filterHValue).ToString()), textBoxR);
            SetText(((_filterHValue + (_filterHPerc / 2) * _filterHValue).ToString()), textBoxH);
            SetText((_filterSValue - (_filterSPerc / 2) * _filterSValue).ToString(), textBoxG);
            SetText((_filterSValue + (_filterSPerc / 2) * _filterSValue).ToString(), textBoxS);
            SetText((_filterLValue - (_filterLPerc / 2) * _filterLValue).ToString(), textBoxB);
            SetText((_filterLValue + (_filterLPerc / 2) * _filterLValue).ToString(), textBoxL);
            _filterHRange = new IntRange((int)(_filterHValue - (_filterHPerc / 2) * _filterHValue), (int)(_filterHValue + (_filterHPerc / 2) * _filterHValue));
            _filterSRange = new Range(_filterSValue - (_filterSPerc / 2) * _filterSValue, _filterSValue + (_filterSPerc / 2) * _filterSValue);
            _filterLRange = new Range(_filterLValue - (_filterLPerc / 2) * _filterLValue, _filterLValue + (_filterLPerc / 2) * _filterLValue);
            _filterHSL = new HSLFiltering(_filterHRange, _filterSRange, _filterLRange);
        }
        private void trackBarH_Scroll(object sender, EventArgs e)
        {
            _filterHPerc = trackBarH.Value/ 200.0f;
            SetText((_filterHPerc*100) + "%", textBoxHF);
            HSLChanged();
        }

        private void trackBarS_Scroll(object sender, EventArgs e)
        {
            _filterSPerc = trackBarS.Value / 200.0f;
            SetText((_filterSPerc*100) + "%", textBoxSF);
            HSLChanged();
        }

        private void trackBarL_Scroll(object sender, EventArgs e)
        {
            _filterLPerc = trackBarL.Value / 200.0f;
            SetText((_filterLPerc*100) + "%", textBoxLF);
            HSLChanged();
        }

    }
}
