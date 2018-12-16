using Microsoft.Graphics.Canvas;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Capture;
using Windows.Graphics.Imaging;
using Windows.Media.Audio;
using Windows.Media.Core;
using Windows.Media.Editing;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace MediaCompositionDemo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        MediaComposition mediaComposition = null;
        public MainPage()
        {
            this.InitializeComponent();

            if (mediaComposition == null)
            {
                mediaComposition = new MediaComposition();
            }
        }

        public void UpdateSource()
        {
            MediaStreamSource streamSource = mediaComposition.GeneratePreviewMediaStreamSource((int)mediaPlayerElement.ActualWidth, (int)mediaPlayerElement.ActualHeight);
            var source = MediaSource.CreateFromMediaStreamSource(streamSource);
            mediaPlayerElement.Source = source;
        }

        private async void AddClip_Click(object sender, RoutedEventArgs e)
        {
            Windows.Storage.Pickers.FileOpenPicker fileOpenPicker = new Windows.Storage.Pickers.FileOpenPicker();
            fileOpenPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
            fileOpenPicker.FileTypeFilter.Add(".mp4");
            var file = await fileOpenPicker.PickSingleFileAsync();
            if (file == null)
                return;
            MediaClip clip = await MediaClip.CreateFromFileAsync(file);
            mediaComposition.Clips.Add(clip);

            UpdateSource();
        }

        System.Threading.SynchronizationContext SynchronizationContext = System.Threading.SynchronizationContext.Current;

        private async void SaveClip_Click(object sender, RoutedEventArgs e)
        {
            Windows.Storage.Pickers.FileSavePicker fileSavePicker = new Windows.Storage.Pickers.FileSavePicker();
            fileSavePicker.FileTypeChoices.Add("MP4 files", new List<string>() { ".mp4" });
            fileSavePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;

            var file = await fileSavePicker.PickSaveFileAsync();
            if (file == null)
            {
                return;
            }

            btnSave.Visibility = Visibility.Collapsed;
            progressRing.Visibility = Visibility.Visible;
            tbProgress.Visibility = Visibility.Visible;

            var render = mediaComposition.RenderToFileAsync(file, MediaTrimmingPreference.Precise);
            render.Progress = new AsyncOperationProgressHandler<Windows.Media.Transcoding.TranscodeFailureReason, double>((reson, value) =>
              {
                  SynchronizationContext.Post((param) =>
                  {
                      tbProgress.Text = value.ToString("0.00")+"%";
                  },null);
              });
            render.Completed = new AsyncOperationWithProgressCompletedHandler<Windows.Media.Transcoding.TranscodeFailureReason, double>((reason, status) =>
              {
                  string msg = "Successful";

                  var re = reason.GetResults();
                  if (re!= Windows.Media.Transcoding.TranscodeFailureReason.None|| status!= AsyncStatus.Completed)
                  {
                      msg = "Unsuccessful";
                  }
                  SynchronizationContext.Post((param) =>
                  {
                      btnSave.Visibility = Visibility.Visible;
                      progressRing.Visibility = Visibility.Collapsed;
                      tbProgress.Visibility = Visibility.Collapsed;
                      tbProgress.Text = msg;
                  }, null);
              });
        }

        private async void AddAudio_Click(object sender, RoutedEventArgs e)
        {
            // Add background audio
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.MusicLibrary;
            picker.FileTypeFilter.Add(".mp3");
            picker.FileTypeFilter.Add(".wav");
            picker.FileTypeFilter.Add(".flac");
            Windows.Storage.StorageFile audioFile = await picker.PickSingleFileAsync();
            if (audioFile == null)
            {
                return;
            }

            // These files could be picked from a location that we won't have access to later
            var storageItemAccessList = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList;
            storageItemAccessList.Add(audioFile);

            var backgroundTrack = await BackgroundAudioTrack.CreateFromFileAsync(audioFile);

            mediaComposition.BackgroundAudioTracks.Add(backgroundTrack);

            UpdateSource();
        }

        private void AddOverlay_Click(object sender, RoutedEventArgs e)
        {
            var colorClip = MediaClip.CreateFromColor(Windows.UI.Color.FromArgb(255, 125, 0, 0), TimeSpan.FromSeconds(10));
            
            var colorOverlay = new MediaOverlay(colorClip, new Rect(10, 10, 50, 50), 1);

            MediaOverlayLayer colorLayer = new MediaOverlayLayer();
            colorLayer.Overlays.Add(colorOverlay);
            
            mediaComposition.OverlayLayers.Add(colorLayer);
            
            UpdateSource();
        }

        Direct3D11CaptureFramePool _franePool = null;
        GraphicsCaptureSession _session = null;
        TimeSpan _timeSpan;
        long performanceFrequency;

        private async void CpatureScreen_Click(object sender, RoutedEventArgs e)
        {
            Windows.Graphics.Capture.GraphicsCapturePicker graphicsCapturePicker = new Windows.Graphics.Capture.GraphicsCapturePicker();
            Windows.Graphics.Capture.GraphicsCaptureItem graphicsCaptureItem = await graphicsCapturePicker.PickSingleItemAsync();

            if (graphicsCaptureItem == null)
                return;
            CanvasDevice canvasDevice = new CanvasDevice();
            _franePool = Direct3D11CaptureFramePool.Create(canvasDevice, Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized, 2, graphicsCaptureItem.Size);

            _franePool.FrameArrived +=async (s, args) => 
            {
                using (var frame = _franePool.TryGetNextFrame())
                {
                    var canvasBitmap = CanvasBitmap.CreateFromDirect3D11Surface(canvasDevice, frame.Surface);

                    //await CpatureToImageFile(canvasBitmap);
                    CreateClipInMemory(canvasDevice, canvasBitmap);
                }
            };

            graphicsCaptureItem.Closed += (s, o) => 
            {
                CaptureStop_Click(null,null);
            };

            _session = _franePool.CreateCaptureSession(graphicsCaptureItem);

            timeSpans = new List<TimeSpan>();
            count = 0;

            _session.StartCapture();

            QueryPerformanceCounter(out long qpc);
            QueryPerformanceFrequency(out long frq);
            performanceFrequency = frq;
            var milliseconds = 1000f * qpc / performanceFrequency;
            _timeSpan = TimeSpan.FromMilliseconds(milliseconds);

            CaptureAudio();

            btnCaptureStop.Visibility = Visibility.Visible;
            btnCaptureStart.Visibility = Visibility.Collapsed;
        }

        private async void CaptureStop_Click(object sender, RoutedEventArgs e)
        {
            _session.Dispose();
            _franePool.Dispose();

            _audioGraph?.Stop();
            if (_audioFile != null)
            {
                var audioTrack = await BackgroundAudioTrack.CreateFromFileAsync(_audioFile);
                mediaComposition.BackgroundAudioTracks.Add(audioTrack);
            }

            //await CreateClipFromImageFile();

            UpdateSource();

            btnCaptureStop.Visibility = Visibility.Collapsed;
            btnCaptureStart.Visibility = Visibility.Visible;
        }

        #region Capture screen and create MediaClip in Memory
        /// <summary>
        /// TODO: need to dispose CanvasRenderTarget.
        /// </summary>
        /// <param name="canvasDevice"></param>
        /// <param name="canvasBitmap"></param>
        private void CreateClipInMemory(CanvasDevice canvasDevice, CanvasBitmap canvasBitmap)
        {
            CanvasRenderTarget rendertarget = null;
            QueryPerformanceCounter(out long counter);
            var currentTime = TimeSpan.FromMilliseconds(1000f * counter / performanceFrequency);
            try
            {
                rendertarget = new CanvasRenderTarget(canvasDevice, canvasBitmap.SizeInPixels.Width, canvasBitmap.SizeInPixels.Height,canvasBitmap.Dpi,canvasBitmap.Format,canvasBitmap.AlphaMode);
                using (CanvasDrawingSession ds = rendertarget.CreateDrawingSession())
                {
                    ds.Clear(Colors.Transparent);
                    ds.DrawImage(canvasBitmap);
                }
                mediaComposition.Clips.Add(MediaClip.CreateFromSurface(rendertarget, currentTime - _timeSpan));
            }
            catch
            {
            }
            finally
            {
                _timeSpan = currentTime;
            }
        }
        #endregion

        #region Capture screen and save it to disk.
        int count = 0;
        List<TimeSpan> timeSpans = new List<TimeSpan>();

        private async Task CpatureToImageFile(CanvasBitmap WB)
        {
            try
            {
                string FileName = $"capture{count++}.";
                Guid BitmapEncoderGuid = BitmapEncoder.JpegEncoderId;
                FileName += "jpg";

                var file = await Windows.Storage.ApplicationData.Current.TemporaryFolder
                    .CreateFileAsync(FileName, CreationCollisionOption.ReplaceExisting);

                using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoderGuid, stream);
                    byte[] pixels = WB.GetPixelBytes();
                    encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                              (uint)WB.SizeInPixels.Width,
                              (uint)WB.SizeInPixels.Height,
                              96.0,
                              96.0,
                              pixels);
                    await encoder.FlushAsync();
                }

                QueryPerformanceCounter(out long qpc);
                var milliseconds = 1000f * qpc / performanceFrequency;
                var currentTime = TimeSpan.FromMilliseconds(milliseconds);

                timeSpans.Add(currentTime - _timeSpan);
                _timeSpan = currentTime;
            }
            catch (Exception e)
            {
            }
        }

        public async Task CreateClipFromImageFile()
        {
            for (int i = 0; i < count; i++)
            {
                string FileName = $"capture{i}.";
                FileName += "jpg";
                var file = await Windows.Storage.ApplicationData.Current.TemporaryFolder.GetFileAsync(FileName);

                mediaComposition.Clips.Add(await MediaClip.CreateFromImageFileAsync(file, timeSpans[i]));
            }
        }
        #endregion

        AudioGraph _audioGraph;
        IStorageFile _audioFile;

        public async void CaptureAudio()
        {
            AudioGraphSettings audioGraphSettings = new AudioGraphSettings(Windows.Media.Render.AudioRenderCategory.Speech);
            var result = await AudioGraph.CreateAsync(audioGraphSettings);
            if(result.Status!= AudioGraphCreationStatus.Success)
            {
                return;
            }
            _audioGraph = result.Graph;

            var deviceInputNodeResult = await _audioGraph.CreateDeviceInputNodeAsync(Windows.Media.Capture.MediaCategory.Speech);
            if(deviceInputNodeResult.Status!=  AudioDeviceNodeCreationStatus.Success)
            {
                return;
            }
            var deviceInputNode = deviceInputNodeResult.DeviceInputNode;

            _audioFile = await Windows.Storage.ApplicationData.Current.TemporaryFolder
                    .CreateFileAsync("speech", CreationCollisionOption.ReplaceExisting);

            var mediaEncodingProfile = MediaEncodingProfile.CreateMp3(AudioEncodingQuality.High);
            var fileOutputNodeResult = await _audioGraph.CreateFileOutputNodeAsync(_audioFile, mediaEncodingProfile);
            if (fileOutputNodeResult.Status !=  AudioFileNodeCreationStatus.Success)
            {
                return;
            }
            var fileOutputNode = fileOutputNodeResult.FileOutputNode;

            deviceInputNode.AddOutgoingConnection(fileOutputNode);

            _audioGraph.Start();
        }

        [DllImport("Kernel32.dll")]
        public static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

        [DllImport("Kernel32.dll")]
        public static extern bool QueryPerformanceFrequency(out long lpFrequency);
    }
}
