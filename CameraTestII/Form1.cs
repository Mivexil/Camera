using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Math.Geometry;
using AForge.Video;
using AForge.Video.DirectShow;
using NAudio.Wave;
using Point = System.Drawing.Point; //instead of AForge.Point

namespace CameraTestII
{
    public partial class Form1 : Form
    {
        private const double epsilon = 10; //Specifies the minimum movement along Y axis to be registered.
        private readonly BlobCounter _pointerBlobCounter = new BlobCounter(); //Pointer detector.
        private readonly BlobCounter _rectangleBlobCounter = new BlobCounter //Hit rectangle detector.
        {
            FilterBlobs = true,
            MinWidth = 40,
            MinHeight = 40,
            MaxWidth = 320,
            MaxHeight = 320
        };
        private readonly Erosion3x3 _filterErosion = new Erosion3x3(); //For eliminating noise.
        private readonly Grayscale _filterGrayscale = Grayscale.CommonAlgorithms.BT709; //Standard grayscale filter.
        private readonly Threshold _filterThreshold = new Threshold(0);
        private readonly YCbCrFiltering _filterYCbCr = new YCbCrFiltering(
            new Range(0, 1), 
            new Range(0.5f, 0.5f), 
            new Range(-0.5f, -0.5f)); //Filters out everything except the pointer.

        private readonly List<Point> _hitPoints = new List<Point>(); //List of coordinates of all "hits" (see MovementDetector).
        private readonly Queue<Point> _lastPoints = new Queue<Point>(); //Stores up to 5 last pointer positions since last hit.
        
        //NAudio stuff
        private IWavePlayer _waveOutDevice; //Our general sound output device
        private readonly WaveChannel32[] _noteChannels = new WaveChannel32[8]; //Each note gets its own wave channel to enable polyphony.
        private readonly WaveFileReader[] _readers = new WaveFileReader[8]; //For reading the resource .wav files.
        private WaveMixerStream32 _mixer; //Mixes all notes together :)

        private UnmanagedImage _blockMap; //Contains information about location of hit rectangles.
        private VideoCaptureDevice _camera; //Our general video input device.
       
        private bool _rectangleFilterSet; //Allows the InstrumentHandler to unregister itself after clicking the "Set" button.

        //Only a form constructor, nothing to see here
        public Form1()
        {
            InitializeComponent();
        }

        //We can't normally set TextBox values outside of the UI thread, so we need this function.
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
                catch (Exception e)
                {
                    Debug.Print("Exception while setting text: " + e.Message);
                }
            }
            else
            {
                box.Text = text;
            }
        }

        //Initializes camera and wave output
        private void Form1_Load(object sender, EventArgs e)
        {
            //TODO: add nicer error handling when no video devices are present or audio initialization fails
            var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice); //Obtain all video input devices.
            //TODO: default to first camera, allow choice of camera when more than one present
            _camera = new VideoCaptureDevice(videoDevices[1].MonikerString); //Initialize camera. For use with our external webcam. For regular use, change to [0].
            _mixer = new WaveMixerStream32 {AutoStop = false}; //We want to keep the mixer working even when not currently playing anything.
            _waveOutDevice = new DirectSoundOut(); //Initialize sound output.
            _waveOutDevice.Init(_mixer); //Attach mixer to sound output.
            _waveOutDevice.Play(); //Get ready for playing the wave files.
            //Read all note resource files.
            _readers[0] = new WaveFileReader(Resource1.C);
            _readers[1] = new WaveFileReader(Resource1.D);
            _readers[2] = new WaveFileReader(Resource1.E);
            _readers[3] = new WaveFileReader(Resource1.F);
            _readers[4] = new WaveFileReader(Resource1.G);
            _readers[5] = new WaveFileReader(Resource1.A);
            _readers[6] = new WaveFileReader(Resource1.B);
            _readers[7] = new WaveFileReader(Resource1.C1);
            for (int i = 0; i < 8; i++)
            {
                _noteChannels[i] = new WaveChannel32(_readers[i]); //Convert each file into a wave channel.
                _noteChannels[i].Position = _noteChannels[i].Length; //Set current position to the end, so that the sounds don't initially play
                _mixer.AddInputStream(_noteChannels[i]); //Add each note to mixer.
            }
            _camera.NewFrame += InstrumentHandler; //Register the first (calibration) handler.
            _camera.Start(); //And off we go! :)
        }

        //Given previous pointer positions, calculates whether the movement was upwards or downwards.
        private static bool[] SetDirections(Queue<Point> q)
        {
            var ret = new bool[4];
            for (int i = 1; i < 5; i++)
            {
                if (q.ToArray()[i].Y >= q.ToArray()[i - 1].Y)
                {
                    ret[i - 1] = true; //Pointer moved down.
                }
                else
                {
                    ret[i - 1] = false; //Pointer moved up.
                }
            }
            return ret;
        }

        //Tests in which rectangle lies the given point.
        //Return values 1-8 indicate which rectangle was hit. Values outside of that range indicate no rectangle was hit.
        private int CheckPosition(int x, int y)
        {
            return _blockMap.GetPixel(x, y).R;
        }

        //Initializes the map of hit rectangles locations.
        //TODO: remove all pixels which don't belong to hit rectangles (currently the _blockMap contains the thresholded image).
        private void InstrumentMap(List<List<IntPoint>> blocks)
        {
            foreach (var block in blocks) //For each quadrilateral (found in InstrumentHandler):
            {
                //Find the bounding rectangle of a given quadrilateral.
                int minX = block.Min(c => c.X);
                int maxX = block.Max(c => c.X);
                int minY = block.Min(c => c.Y);
                int maxY = block.Max(c => c.Y);
                //Find the center of bounding rectangle (with some assumptions, it should lie within the quadrilateral).
                var midPoint = new IntPoint((minX + maxX)/2, (minY + maxY)/2);
                //Fill the quadrilateral with a shade of grey according to its number, starting in the center.
                var filter = new PointedColorFloodFill
                {
                    Tolerance = Color.FromArgb(150, 92, 92), //TODO: not sure where this comes from (is that even necessary on a thresholded image?)
                    FillColor =
                        Color.FromArgb(blocks.IndexOf(block) + 1, blocks.IndexOf(block) + 1,
                            blocks.IndexOf(block) + 1),
                    StartingPoint = midPoint
                };
                filter.ApplyInPlace(_blockMap);
            }
        }

        //Handles frames while finding the hit rectangles.
        //TODO: A little refactoring into separate functions
        private void InstrumentHandler(object sender, NewFrameEventArgs e)
        {
            var frame = new Bitmap(e.Frame); //The framework can dispose of its own copy, so we need to create a local one.
            UnmanagedImage grayscaledImage = _filterGrayscale.Apply(UnmanagedImage.FromManagedImage(frame)); //Convert to grayscale unmanaged image.
            _filterErosion.ApplyInPlace(grayscaledImage); //Remove noise.
            _filterErosion.ApplyInPlace(grayscaledImage);
            //TODO: use BlobCounter thresholding instead
            _filterThreshold.ApplyInPlace(grayscaledImage); //Apply thresholding to separate the white rectangles.
            _rectangleBlobCounter.ProcessImage(grayscaledImage); //Find the rectangles on the image.
            _blockMap = grayscaledImage; //Start with _blockMap as our thresholded image.
            List<List<IntPoint>> blocks = new List<List<IntPoint>>(); //List of lists of corner points of each rectangle.
            foreach (Blob blob in _rectangleBlobCounter.GetObjectsInformation()) //For each rectangle:
            {
                List<IntPoint> edgePoints = _rectangleBlobCounter.GetBlobsEdgePoints(blob); //Obtain a list of this rectangle's edge points.
                if (edgePoints.Count > 4) //TODO: possibly redundant due to minWidth/minHeight of blobs
                {
                    List<IntPoint> corners = PointsCloud.FindQuadrilateralCorners(edgePoints); //Find corners of the rectangle.
                    Drawing.Polygon(_blockMap, corners, Color.Red); //Draw the rectangle. TODO: Change red to something making more sense on a grayscale image
                    foreach (IntPoint corner in corners)
                    {
                        for (int i = 0; i < corners.Count; i++)
                        {
                            //Draw corners of the rectangle as 5x5 points. 
                            Drawing.FillRectangle(_blockMap,
                                new Rectangle(corner.X - 2, corner.Y - 2, 5, 5),
                                Color.FromArgb(i*32 + 127 + 32, i*64, i*64)); //TODO: That's weird.
                        }
                    }
                    blocks.Add(corners); //Add the rectangle's corners to the list.
                }
            }
            blocks.Sort((a, b) => (a[0].X.CompareTo(b[0].X))); //Sort rectangles left to right based on upper left corner coordinates.
            InstrumentMap(blocks); //Prepare the _blockMap.
            FilteredPictureBox.Image = _blockMap.ToManagedImage(); //Show the filtered picture in the right window.
            OriginalPictureBox.Image = frame; //Show original picture in the left window.
            if (_rectangleFilterSet) //If user clicked the "set" button:
            {
                //Change the visible set of tuning bars to prepare for YCbCr filter tuning.
                RectangleFilterTextBox.Visible = false;
                RectangleFilterSetButton.Visible = false;
                RectangleFilterBar.Visible = false;
                RectangleFilterLabel.Visible = false;
                MinCbBar.Visible = true;
                MinCbTextBox.Visible = true;
                MinCbLabel.Visible = true;
                MaxCrBar.Visible = true;
                MaxCrTextBox.Visible = true;
                MaxCrLabel.Visible = true;
                //Unregister this handler and register the standard handler.
                _camera.NewFrame -= InstrumentHandler;
                _camera.NewFrame += NewFrameHandler;
            }
        }

        //Standard new frame handler.
        private void NewFrameHandler(object sender, NewFrameEventArgs e)
        {
            var frame = new Bitmap(e.Frame); //The framework can dispose of its own copy, so we need to create a local one.
            UnmanagedImage unmanaged = UnmanagedImage.FromManagedImage(frame); //Convert the bitmap to unmanaged one for faster processing.
            _filterYCbCr.ApplyInPlace(unmanaged); //Apply YCbCr filtering to separate the pointer.
            UnmanagedImage unmanaged2 = _filterGrayscale.Apply(unmanaged); //Convert the image to grayscale.
            _filterErosion.ApplyInPlace(unmanaged2);
            _filterErosion.ApplyInPlace(unmanaged2); //For noise removal.
            _pointerBlobCounter.ProcessImage(unmanaged2); //Find all objects in the image.
            Rectangle[] rects = _pointerBlobCounter.GetObjectsRectangles(); //Grab the objects' bounding rectangles.
            int xCent = -1, yCent = -1; //Initialize coordinates of center of the biggest objects.
            if (rects.Length != 0) //If we've found an object:
            {
                Rectangle r = rects.Aggregate((r1, r2) => (r1.Height*r1.Width) > (r2.Height*r2.Width) ? r1 : r2); //Get the rectangle with biggest area.
                //Set coordinates of center of the biggest object.
                xCent = r.Left + r.Width/2; 
                yCent = r.Top + r.Height/2;
            }
            if (xCent >= 2 && yCent >= 2 && xCent <= unmanaged.Width - 2 && yCent <= unmanaged.Height - 2) //If the coordinates aren't too close to the edges:
            {
                Drawing.FillRectangle(unmanaged2,
                    new Rectangle(xCent - 2, yCent - 2, 5, 5),
                    Color.FromArgb(255, 255, 255)); //Draw the current pointer position.
            }
            int x = MovementDetector(xCent, yCent, unmanaged2); //Check whether a hit occured, and if so, in which rectangle.
            if (x > 0) //If a hit occured:
            {
                _noteChannels[8 - x].Position = 0; //Play the appropiate note.
            }
            FilteredPictureBox.Image = unmanaged2.ToManagedImage(); //Show the filtered picture in the right window.
            OriginalPictureBox.Image = frame; //Show the original picture in the left window.
        }

        //Detects whether and where a hit occured.
        private int MovementDetector(int xCent, int yCent, UnmanagedImage unmanaged2)
        {
            //TODO: make it so that the hit points history is being drawn even if we've lost the pointer
            if (xCent == -1 || yCent == -1) return -3; //If we've lost the pointer, give up immediately and return a negative value.
            bool foundHitPoint = false; //Indicates whether a new hitpoint was found.
            if (_lastPoints.Count >= 5) //If there are already 5 points in the queue:
            {
                _lastPoints.Dequeue(); //Remove the last one.
            }
            _lastPoints.Enqueue(new Point(xCent, yCent)); //Add the current pointer position to the queue.
            if (_lastPoints.Count == 5) //If there are 5 points:
            {
                bool[] upDownBools = SetDirections(_lastPoints); //Check whether the pointer was moving up or down between every two successive frames.
                if (upDownBools[0] && upDownBools[1] && !upDownBools[2] && !upDownBools[3]) //If the pattern is "down-down-up-up"...
                {
                    if (_lastPoints.ToArray()[2].Y - _lastPoints.ToArray()[0].Y > epsilon &&
                        _lastPoints.ToArray()[2].Y - _lastPoints.ToArray()[4].Y > epsilon) //...and if the position change was big enough (to eliminate spurious movement):
                    {
                        _hitPoints.Add(_lastPoints.ToArray()[2]); //Add the lowest position in the queue as the new hit point.
                        foundHitPoint = true; //Indicate that a hitpoint was found.
                        _lastPoints.Clear(); //Clear the queue.
                    }
                }
            }
            foreach (Point p in _hitPoints) //For each hitpoint in the history:
            {
                if (p.X >= 2 && p.Y >= 2 && p.X <= unmanaged2.Width - 2 && p.Y <= unmanaged2.Height - 2) //If its coordinates aren't too close to the edges:
                {
                    Drawing.FillRectangle(unmanaged2,
                        new Rectangle(p.X - 2, p.Y - 2, 5, 5),
                        Color.FromArgb(255, 255, 255)); //Draw the point.
                }
            }
            if (foundHitPoint) //If a hitpoint was found:
            {
                int x = CheckPosition(_hitPoints.LastOrDefault().X, _hitPoints.LastOrDefault().Y); //Test whether the hit occured in any of the rectangles.
                if (x > 8 || x == 0) return -2; // If not, return a negative value.
                return x; //If yes, return the number of that rectangle.
            }
            return -1; //If no hitpoint was found, return a negative value.
        }

        //Shuts down the whole program when the form is closed.
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            _camera = null; //TODO: probably unnecessary.
            try
            {
                Environment.Exit(0); //Break the execution, including non-UI threads.
            }
            catch (Exception x)
            {
                Debug.Print("Exception raised on exit: " + x.Message); //TODO: don't know why those exceptions happen, they don't do anything though.
            }
        }
        
        //Tuning bars and buttons event handlers.
        private void RectangleFilterBar_Scroll(object sender, EventArgs e)
        {
            int rectangleFilterThreshold = RectangleFilterBar.Value;
            SetText(rectangleFilterThreshold.ToString(), RectangleFilterTextBox);
            _filterThreshold.ThresholdValue = rectangleFilterThreshold;
        }

        private void RectangleFilterSetButton_Click(object sender, EventArgs e)
        {
            _rectangleFilterSet = true;
        }

        private void MinCbBar_Scroll(object sender, EventArgs e)
        {
            float cbMin = (MinCbBar.Value / 300.0f) - 0.5f; //Map (0,300) to (-0.5,0.5)
            SetText(cbMin.ToString(), MinCbTextBox);
            _filterYCbCr.Cb = new Range(cbMin, 0.5f);
        }

        private void MaxCrBar_Scroll(object sender, EventArgs e)
        {
            float crMax = (MaxCrBar.Value / 300.0f) - 0.5f; //Map (0,300) to (-0.5,0.5)
            SetText(crMax.ToString(), MaxCrTextBox);
            _filterYCbCr.Cr = new Range(-0.5f, crMax);
        }
    }
}