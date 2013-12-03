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
using AForge.Math.Geometry;
using Point = System.Drawing.Point; //instead of AForge.Point
using NAudio.Wave;

namespace CameraTestII
{

    public partial class Form1 : Form
    {
        private IWavePlayer _waveOutDevice;
        private WaveMixerStream32 _mixer;
        private WaveFileReader[] readers = new WaveFileReader[8];
        //private WaveOffsetStream[] offsetStreams = new WaveOffsetStream[8];
        private WaveChannel32[] channel32s = new WaveChannel32[8];
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
        private Random randGen = new Random();
        private List<List<IntPoint>> blocks = new List<List<IntPoint>>();
        private UnmanagedImage _blockMap;

        /*Because we can't access controls from outside the UI thread normally*/
        public void SetText(string text, TextBox box)
        {
            if (box.InvokeRequired)
            {
                try
                {
                    Invoke(new Action<string, TextBox>(SetText), new object[] { text, box });
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
            //_camera.NewFrame += CalibrationHandler;
            _mixer = new WaveMixerStream32();
            _mixer.AutoStop = false;
            _waveOutDevice = new DirectSoundOut();
            _waveOutDevice.Init(_mixer);
            _waveOutDevice.Play();
            readers[0] = new WaveFileReader(Resource1.C);
            readers[1] = new WaveFileReader(Resource1.D);
            readers[2] = new WaveFileReader(Resource1.E);
            readers[3] = new WaveFileReader(Resource1.F);
            readers[4] = new WaveFileReader(Resource1.G); 
            readers[5] = new WaveFileReader(Resource1.A);
            readers[6] = new WaveFileReader(Resource1.B);
            readers[7] = new WaveFileReader(Resource1.C1);
            for (int i = 0; i < 8; i++)
            {
                channel32s[i] = new WaveChannel32(readers[i]);
                _mixer.AddInputStream(channel32s[i]);
            }
            _camera.NewFrame += InstrumentHandler;
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

        private int checkPosition(int X, int Y)
        {
            int position = -1;

            Color pixel = _blockMap.GetPixel(X, Y);
            position = pixel.R;

            //Console.WriteLine(position);
            return position;
        }

        private void InstrumentMap()
        {
            int minX = 0, minY = 0, maxX = 0, maxY = 0;
            foreach (List<IntPoint> block in blocks)
            {
                minX = block[0].X;
                maxX = block[0].X;
                minY = block[0].Y;
                maxY = block[0].Y;

                foreach (IntPoint corner in block)
                {
                    if (corner.X < minX)
                    {
                        minX = corner.X;
                    }
                    if (corner.X > maxX)
                    {
                        maxX = corner.X;
                    }
                    if (corner.Y < minY)
                    {
                        minY = corner.Y;
                    }
                    if (corner.Y > maxY)
                    {
                        maxY = corner.Y;
                    }
                }

                IntPoint midPoint;
                midPoint.X = (minX + maxX) / 2;
                midPoint.Y = (minY + maxY) / 2;

                PointedColorFloodFill filter = new PointedColorFloodFill();
                filter.Tolerance = Color.FromArgb(150, 92, 92);
                filter.FillColor = Color.FromArgb(blocks.IndexOf(block), blocks.IndexOf(block), blocks.IndexOf(block));
                filter.StartingPoint = midPoint;
                filter.ApplyInPlace(_blockMap);
            }
        }

        private void InstrumentHandler(object sender, NewFrameEventArgs e)
        {
            Bitmap frame = new Bitmap(e.Frame);
            UnmanagedImage unmanaged = UnmanagedImage.FromManagedImage(frame);

            UnmanagedImage unmanaged2 = _filterGrayscale.Apply(unmanaged);
            _filterErosion.ApplyInPlace(unmanaged2);
            _filterErosion.ApplyInPlace(unmanaged2);

            Threshold filter = new Threshold(20);
            filter.ApplyInPlace(unmanaged2);

            BlobCounter extractor = new BlobCounter();
            extractor.MinWidth = extractor.MinHeight = unmanaged2.Height / 15;
            extractor.MaxWidth = extractor.MaxHeight = unmanaged2.Height / 2;
            extractor.FilterBlobs = true;
            extractor.ProcessImage(unmanaged2);
            _blockMap = unmanaged2;

            foreach (Blob blob in extractor.GetObjectsInformation())
            {
                List<IntPoint> edgePoints = extractor.GetBlobsEdgePoints(blob);
                if (edgePoints.Count > 4)
                {
                    List<IntPoint> corners = PointsCloud.FindQuadrilateralCorners(edgePoints);
                    /*foreach (IntPoint corner in corners)
                    {
                        Drawing.Polygon(_blockMap, corners, Color.Red);
                        for (int i = 0; i < corners.Count; i++)
                        {
                            Drawing.FillRectangle(_blockMap,
                                new Rectangle(corner.X - 2, corner.Y - 2, 5, 5),
                                Color.FromArgb(i * 32 + 127 + 32, i * 64, i * 64));
                        }
                    }*/
                    blocks.Add(corners);
                }

            }
            blocks.Sort((a, b) => (a[0].X.CompareTo(b[0].X)));
            InstrumentMap();

            pictureBox2.Image = _blockMap.ToManagedImage();
            _camera.NewFrame -= InstrumentHandler;
            _camera.NewFrame += CalibrationHandler;

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

            if (randGen.Next(0, 10) == 7)
            {
                channel32s[randGen.Next(0, 8)].Position = 0;
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
                    unmanaged.SetPixel(100 + i, 100 + j, c);
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
                Rectangle r = rects.Aggregate((r1, r2) => (r1.Height * r1.Width) > (r2.Height * r2.Width) ? r1 : r2);
                xCent = r.Left + r.Width / 2;
                yCent = r.Top + r.Height / 2;
            }
            if (xCent >= 2 && yCent >= 2 && xCent <= unmanaged.Width - 2 && yCent <= unmanaged.Height - 2)
            {
                for (int i = -2; i <= 2; i++)
                {
                    for (int j = -2; j <= 2; j++)
                    {
                        unmanaged2.SetPixel(xCent + i, yCent + j, 255);
                    }
                }
            }
            Console.WriteLine(MovementDetector(xCent, yCent, unmanaged2));
            pictureBox2.Image = unmanaged2.ToManagedImage();
            pictureBox1.Image = frame;
        }

        private int MovementDetector(int xCent, int yCent, UnmanagedImage unmanaged2)
        {
            bool _foundHitPoint = false;
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
                        _foundHitPoint = true;
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
                            checkPosition(p.X, p.Y);
                        }
                    }
                }
            }
            if (_foundHitPoint)
            {
                return checkPosition(_lastPoints.ToArray()[2].X, _lastPoints.ToArray()[2].Y);
            }
            else return -1;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            _camera = null;
            try
            {
                Environment.Exit(0);
            }
            catch{}
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
            _filterHPerc = trackBarH.Value / 200.0f;
            SetText((_filterHPerc * 100) + "%", textBoxHF);
            HSLChanged();
        }

        private void trackBarS_Scroll(object sender, EventArgs e)
        {
            _filterSPerc = trackBarS.Value / 200.0f;
            SetText((_filterSPerc * 100) + "%", textBoxSF);
            HSLChanged();
        }

        private void trackBarL_Scroll(object sender, EventArgs e)
        {
            _filterLPerc = trackBarL.Value / 200.0f;
            SetText((_filterLPerc * 100) + "%", textBoxLF);
            HSLChanged();
        }

    }
    
}
