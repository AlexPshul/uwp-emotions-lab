using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Microsoft.ProjectOxford.Common.Contract;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UWPEmotions
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly FaceServiceClient _client = new FaceServiceClient("SUBSCRIPTION KEY", "REGIONAL URL");
        private MediaCapture _mediaCapture;

        public MainPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            StartButton.Click += OnStartButtonClicked;
        }

        private async void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            _mediaCapture = new MediaCapture();
            MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings { StreamingCaptureMode = StreamingCaptureMode.Video };

            await _mediaCapture.InitializeAsync(settings);
            CameraInput.Source = _mediaCapture;
            await _mediaCapture.StartPreviewAsync();

            _mediaCapture.VideoDeviceController.TrySetPowerlineFrequency(PowerlineFrequency.FiftyHertz);
        }

        private async void StartDetecting()
        {
            while (!StartButton.Content.Equals("START"))
            {
                using (InMemoryRandomAccessStream currentFrame = await GetCurrentFrame())
                {
                    Face[] faces = await _client.DetectAsync(currentFrame.AsStream(), false, false, new[] { FaceAttributeType.Emotion });
                    EmotionsCanvas.Children.Clear();
                    foreach (Face face in faces)
                    {
                        ProcessFace(face);
                    }
                }
            }

            EmotionsCanvas.Children.Clear();
        }

        private void ProcessFace(Face face)
        {
            DrawFaceRectangle(face.FaceRectangle);
            DrawEmotionText(face);
        }

        private void DrawFaceRectangle(FaceRectangle faceRectangle)
        {
            Rectangle rectangle = new Rectangle
            {
                Stroke = new SolidColorBrush(Colors.Yellow),
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Colors.Transparent),
                Width = faceRectangle.Width,
                Height = faceRectangle.Height
            };

            Canvas.SetLeft(rectangle, faceRectangle.Left);
            Canvas.SetTop(rectangle, faceRectangle.Top);
            EmotionsCanvas.Children.Add(rectangle);
        }

        private void DrawEmotionText(Face face)
        {
            EmotionScores emotionScores = face.FaceAttributes.Emotion;
            KeyValuePair<string, float> highestScoreEmotion = emotionScores.ToRankedList().FirstOrDefault();
            TextBlock emotionText = new TextBlock
            {
                FontSize = 30,
                Foreground = new SolidColorBrush(Colors.Yellow),
                Text = $"{highestScoreEmotion.Key} ({highestScoreEmotion.Value * 100:0.##)}%"
            };

            Canvas.SetLeft(emotionText, face.FaceRectangle.Left);
            Canvas.SetTop(emotionText, face.FaceRectangle.Top + face.FaceRectangle.Height + 2);

            EmotionsCanvas.Children.Add(emotionText);
        }

        private async Task<InMemoryRandomAccessStream> GetCurrentFrame()
        {
            using (VideoFrame frame = new VideoFrame(BitmapPixelFormat.Bgra8, 900, 900))
            using (VideoFrame currentFrame = await _mediaCapture.GetPreviewFrameAsync(frame))
            {
                InMemoryRandomAccessStream ms = new InMemoryRandomAccessStream();
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, ms);
                encoder.SetSoftwareBitmap(currentFrame.SoftwareBitmap);
                await encoder.FlushAsync();

                return ms;
            }
        }

        private void OnStartButtonClicked(object sender, RoutedEventArgs e)
        {
            if (StartButton.Content.Equals("START"))
            {
                StartButton.Content = "STOP";
                StartDetecting();
            }
            else
            {
                StartButton.Content = "START";
            }
        }
    }
}