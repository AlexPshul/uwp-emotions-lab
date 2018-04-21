 # UWP Emotions Lab
This lab contains all the code base and a step by step guide to help you create an emotion aware UWP app. The application will use your camera to obtain a live feed and send frames to Face API to detect the captured emotion.

The complete solution is available here in the repo. You can check it out if you get stuck or just want to have a look.

## Get your own key
To obtain your own key, follow these steps:

 1. Go to your [Azure Portal](https://portal.azure.com).
 2. On the left panel, click the *Create a resource* option.
 3. Search for *Face API* and hit the *Create* button in the bottom.
 4. Fill out the fields,
	 1. For Israel, the best location would be probably *West Europe*.
	 2. Choose the *S0* price tier option.
5. Go to the newly created resource, to the *Quick Start* tab. In the 2nd section, the first line will provide you with your region specific url.
6. Go to the *Keys* tab. You will have 2 generated keys there. Both keys may be used.

## Create your camera preview
Now, you are going to create a camera preview to show on the screen.

1. Create a new UWP project.
2. Go to the MainPage.xaml and replace the initial grid with the following:
   ```xml
   <Grid Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
       <Grid.RowDefinitions>
           <RowDefinition Height="*"/>
           <RowDefinition Height="Auto"/>
       </Grid.RowDefinitions>

       <CaptureElement Name="CameraInput" Width="900" Height="900" HorizontalAlignment="Center" VerticalAlignment="Center"/>
       <Canvas Name="EmotionsCanvas" Width="900" Height="900" HorizontalAlignment="Center" VerticalAlignment="Center" Background="Transparent"/>

       <Button Name="StartButton" Content="START" HorizontalAlignment="Center" VerticalAlignment="Bottom" FontSize="40" Margin="10" Width="170" Grid.Row="1"/>
   </Grid>
   ```
   1. The *CaptureElement* will show you the camera feed.
   2. The *Canvas* will be used to draw the rectangle and add the emotion text.
   3. The *Button* will Start/Stop the detection process.

##### Note, we are going to reduce the size of the image we send to Face API to 900x900 pixels. So to avoid any calculations, the *CaptureElement* and the *Canvas* were also set to 900x900 pixels.

Continue in the *MainPage.xaml.cs* code behind:

3. Create the a private member of type *MediaCapture*.
   ```csharp
   private MediaCapture _mediaCapture;
   ```

4. Register to the *Loaded* event in the constructor:
   ```csharp
   public MainPage()
   {
       InitializeComponent();
       Loaded += OnLoaded;
   }
   ```
5. Add the async keyword to the *OnLoaded* method and add the following code:
   ```csharp
   _mediaCapture = new MediaCapture();
   MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings { StreamingCaptureMode = StreamingCaptureMode.Video };

   await _mediaCapture.InitializeAsync(settings);
   CameraInput.Source = _mediaCapture;
   await _mediaCapture.StartPreviewAsync();
   ```
     
6. Add permission to the application to use the camera.
To do that, find the *Package.appxmanifest* file in the Solution Explorer and open it.
Go to the *Capabilities* tab, scroll down and select the *Webcam* option.

7. Test your camera.
If your camera is "flickering", add the following line in the end of the *OnLoaded* method:
   ```csharp
   _mediaCapture.VideoDeviceController.TrySetPowerlineFrequency(PowerlineFrequency.FiftyHertz);
   ```


## Face API integration
In this part, you are going to take a frame from the video preview and send it to the *Face API*.

1. In the solution explorer, right click on your project and select the *Manage Nuget Packages...* option.
2. In the *Browse* tab, search for *Microsoft.ProjectOxford.Face* and add it to your project.

After the installation is complete, go back to the *MainPage.xaml.cs* code behind.

3. Add a private member of type *FaceServiceClient* and initialize it with the subscription key you got previously and the region specific url:
   ```csharp
   private readonly FaceServiceClient _client = new FaceServiceClient("SUBSCRIPTION KEY", "REGIONAL URL");
    ```
    This client will provide us with all the APIs in a simple C# form.
4. Create a new method which will provide us with the current frame:
   ```csharp
   private async Task<InMemoryRandomAccessStream> GetCurrentFrame()
   {
        using (VideoFrame frame = new VideoFrame(BitmapPixelFormat.Bgra8, 900, 900))
        using (VideoFrame currentFrame = await _mediaCapture.GetPreviewFrameAsync(frame))
        {
            InMemoryRandomAccessStream ms = new InMemoryRandomAccessStream()
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, ms);
            encoder.SetSoftwareBitmap(currentFrame.SoftwareBitmap);
            await encoder.FlushAsync();

            return ms;
       }
   }
   ```
   Note that the frame we are requesting is 900x900 pixels.
 
5. Create a method to process a detected face, but for now leave it empty. We will come to it later.

   ```csharp
   private void ProcessFace(Face face)
   {
   }
   ```
6. Create a method to start the detection process:
   ```csharp
    private async void StartDetecting()
    {
        while (!StartButton.Content?.Equals("START") ?? false)
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
    ```
    The code is a basic while loop which in each iteration will get the current camera frame, send it to the Face API for detection and process the received faces.
(The cleaning of the canvas children will make sense later.)
7. It's time to wire the button and make the actual calls.
For that to happen, register to the *Click* event of the button in the end of the constructor:
   ```csharp
   StartButton.Click += OnStartButtonClicked;
   ```
8. Start the detection loop in the *OnStartButtonClicked* method or stop it if it is already running:
   ```csharp
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
   ```
9. Time to test that everything is working properly!
Set a breakpoint inside the detection while loop and see that you get a result from the *Face API*.

## Process detected face
Finally, let's process the result by drawing a rectangle over the detected faces and a text with detected emotion.

1. Create two methods (*DrawFaceRectangle* and *DrawEmotionText*) and wire call them inside the previously created *ProcessFace* method:
   ```csharp
   private void ProcessFace(Face face)
   {
       DrawFaceRectangle(face.FaceRectangle);
       DrawEmotionText(face);
   }
      
   private void DrawFaceRectangle(FaceRectangle faceRectangle)
   {
   }
   
   private void DrawEmotionText(Face face)
   {
   }
   ```
2. Inside the *DrawFaceRectangle* method, create a *Rectangle* and place it over the detected face using the coordinates provided us by the API:
   ```csharp
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
   ```
3. Inside the *DrawEmotionText* method, create a *TextBlock* with the emotion text and place it right under the detected rectangle:
   ```csharp
   private void DrawEmotionText(Face face)
   {
       EmotionScores emotionScores = face.FaceAttributes.Emotion;
       KeyValuePair<string, float> highestScoreEmotion = emotionScores.ToRankedList().OrderByDescending(pair => pair.Value).FirstOrDefault();
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
   ```

# Let the show begin!
1. Run your code.
2. Hit the *START* button.
3. If everything went as planned, you should see your face inside a rectangle with a text of the emotion underneath.
4. Feel proud. :sunglasses:
