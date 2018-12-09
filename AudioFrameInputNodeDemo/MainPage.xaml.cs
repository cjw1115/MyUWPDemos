using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Audio;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace AudioFrameInputNodeDemo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        AudioGraph audioGraph;
        AudioFrameInputNode audioFrameInputNode;
        AudioDeviceOutputNode deviceOutputNode;
        Stream fileStream;

        public MainPage()
        {
            this.InitializeComponent();
        }
        public async Task Init()
        {
            AudioGraphSettings audioGraphSettings = new AudioGraphSettings(Windows.Media.Render.AudioRenderCategory.Media);
            var result = await AudioGraph.CreateAsync(audioGraphSettings);
            if (result == null || result.Status != AudioGraphCreationStatus.Success)
            {
                return;
            }
            audioGraph = result.Graph;

            var createAudioDeviceOutputResult = await audioGraph.CreateDeviceOutputNodeAsync();
            if(createAudioDeviceOutputResult==null|| createAudioDeviceOutputResult.Status!= AudioDeviceNodeCreationStatus.Success)
            {
                return;
            }
            deviceOutputNode = createAudioDeviceOutputResult.DeviceOutputNode;

            AudioEncodingProperties audioEncodingProperties = new AudioEncodingProperties();
            audioEncodingProperties.BitsPerSample = 32;
            audioEncodingProperties.ChannelCount = 2;
            audioEncodingProperties.SampleRate = 44100;
            audioEncodingProperties.Subtype= MediaEncodingSubtypes.Float;

            audioFrameInputNode = audioGraph.CreateFrameInputNode(audioEncodingProperties);
            audioFrameInputNode.QuantumStarted += FrameInputNode_QuantumStarted;

            audioFrameInputNode.AddOutgoingConnection(deviceOutputNode);
            audioGraph.Start();
        }

        private async Task GetFileStream()
        {
            IStorageFile file = null;
            FileOpenPicker filePicker = new FileOpenPicker();
            filePicker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
            filePicker.FileTypeFilter.Add(".pcm");
            filePicker.ViewMode = PickerViewMode.Thumbnail;
            file = await filePicker.PickSingleFileAsync();

            // File can be null if cancel is hit in the file picker
            if (file == null)
            {
                return;
            }
            var ras = await file.OpenReadAsync();
            fileStream = ras.AsStreamForRead();
        }
        private unsafe void FrameInputNode_QuantumStarted(AudioFrameInputNode sender, FrameInputNodeQuantumStartedEventArgs args)
        {
            var bufferSize = args.RequiredSamples * sizeof(float) * 2;
            AudioFrame audioFrame = new AudioFrame((uint)bufferSize);

            if (fileStream == null)
                return;
            using (var audioBuffer = audioFrame.LockBuffer(AudioBufferAccessMode.Write))
            {
                using (var bufferReference = audioBuffer.CreateReference())
                {
                    byte* dataInBytes;
                    uint capacityInBytes;
                    float* dataInFloat;

                    // Get the buffer from the AudioFrame
                    ((IMemoryBufferByteAccess)bufferReference).GetBuffer(out dataInBytes, out capacityInBytes);
                    dataInFloat = (float*)dataInBytes;

                    var managedBuffer = new byte[capacityInBytes];

                    var lastLength = fileStream.Length - fileStream.Position;
                    int readLength = (int)(lastLength < capacityInBytes ? lastLength : capacityInBytes);
                    if (readLength <= 0)
                    {
                        fileStream.Close();
                        fileStream = null;
                        return;
                    }
                    fileStream.Read(managedBuffer, 0, readLength);

                    for (int i = 0; i < readLength; i += 8)
                    {
                        dataInBytes[i + 4] = managedBuffer[i + 0];
                        dataInBytes[i + 5] = managedBuffer[i + 1];
                        dataInBytes[i + 6] = managedBuffer[i + 2];
                        dataInBytes[i + 7] = managedBuffer[i + 3];
                        dataInBytes[i + 0] = managedBuffer[i + 4];
                        dataInBytes[i + 1] = managedBuffer[i + 5];
                        dataInBytes[i + 2] = managedBuffer[i + 6];
                        dataInBytes[i + 3] = managedBuffer[i + 7];
                    }
                }
            }

            audioFrameInputNode.AddFrame(audioFrame);
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await GetFileStream();
            await Init();
        }
    }

    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }
}
