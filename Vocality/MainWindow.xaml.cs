using NAudio.Wave;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Vocality;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{

    private WaveInEvent waveIn;
    private WaveOutEvent waveOut;
    private BufferedWaveProvider bufferedWaveProvider;
    private WaveFileWriter writer;
    private string outputFile = "recorded.wav";
    public MainWindow()
    {
        InitializeComponent();
    }

    /*
     Microphone functions
    */

    // Start Microphone
    private void MicToggleButton_Checked(object sender, RoutedEventArgs e)
    {
        // Handle the event when the microphone toggle button is checked
        MicToggleButton.Content = "Stop Microphone";
        StartMicInput();
        //TODO: Start microphone and process accordingly
    }

    private void StartMicInput()
    {
        // Logic to start microphone input
        // TODO: Start mic input
    }

    // Stop Microphone
    private void MicToggleButton_Unchecked(object sender, RoutedEventArgs e)
    {
        // Handle the event when the microphone toggle button is unchecked
        MicToggleButton.Content = "Start Microphone";
        StopMicInput();
        //TODO: Stop microphone
    }

    private void StopMicInput()
    {
        // Logic to stop microphone input
        //TODO: Stop mic input
    }

    /*
     Recording functions
    */

    // Start Recording
    private void RecordToggleButton_Checked(object sender, RoutedEventArgs e)
    {
        // Handle the event when the record toggle button is checked
        RecordToggleButton.Content = "Stop Recording";
        StartRecording();
    }

    private void StartRecording()
    {
        // Logic to start recording
        RecordingStatus.Text = "Recording started";
    }

    // Stop Recording
    private void RecordToggleButton_Unchecked(object sender, RoutedEventArgs e)
    {
        // Handle the event when the record toggle button is unchecked
        RecordToggleButton.Content = "Start Recording";
        StopRecording();
    }

    private void StopRecording()
    {
        // Logic to stop recording
        RecordingStatus.Text = "Recording stopped";
    }

    /*
     Loopback Functions
    */

    // Start Loopback
    private void LoopbackToggleButton_Checked(object sender, RoutedEventArgs e)
    {
        // Handle the event when the loopback toggle button is checked
        LoopbackToggleButton.Content = "Stop Loopback";
        StartMicLoopback();
    }
    private void StartMicLoopback()
    {
        // Logic to start microphone loopback
        waveIn = new WaveInEvent
        {
            DeviceNumber = 0,
            WaveFormat = new WaveFormat(44100, 1)
        };

        bufferedWaveProvider = new BufferedWaveProvider(waveIn.WaveFormat)
        {
            DiscardOnBufferOverflow = true
        };

        waveOut = new WaveOutEvent();
        waveOut.Init(bufferedWaveProvider);

        waveIn.DataAvailable += (s, a) =>
        {
            bufferedWaveProvider.AddSamples(a.Buffer, 0, a.BytesRecorded);
            writer?.Write(a.Buffer, 0, a.BytesRecorded);
        };

        waveIn.StartRecording();
        waveOut.Play();
        LoopbackStatus.Text = "Loopback started";
    }

    // Stop Loopback
    private void LoopbackToggleButton_Unchecked(object sender, RoutedEventArgs e)
    {
        // Handle the event when the loopback toggle button is unchecked
        LoopbackToggleButton.Content = "Start Loopback";
        StopMicLoopback();
    }

    private void StopMicLoopback()
    {
        // Logic to stop microphone loopback
        LoopbackStatus.Text = "Loopback stopped";
    }
}