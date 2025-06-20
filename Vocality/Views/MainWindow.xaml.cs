using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Vocality.Audio;

namespace Vocality.Views
{
    public partial class MainWindow : Window
    {
        private WaveInEvent? waveIn;
        private WaveOutEvent? waveOut;
        private WaveFileWriter? writer;
        private BufferedWaveProvider? byteBuffer;
        private SoundTouchPitchProvider? activePitchProvider;

        private int selectedInputDevice = 0;
        private int selectedOutputDevice = 0;
        private float currentPitchShift = 0f;

        private readonly WaveFormat waveFormat = new(44100, 1);


        public MainWindow()
        {
            InitializeComponent();
            LoadAudioDevices();
        }

        /* ── Device list ─────────────────────────────────────────── */
        private void LoadAudioDevices()
        {
            InputDeviceSelector.Items.Clear();
            for (int i = 0; i < WaveInEvent.DeviceCount; i++)
                InputDeviceSelector.Items.Add($"{i}: {WaveInEvent.GetCapabilities(i).ProductName}");
            InputDeviceSelector.SelectedIndex = 0;

            OutputDeviceSelector.Items.Clear();
            for (int i = 0; i < WaveOut.DeviceCount; i++)
                OutputDeviceSelector.Items.Add($"{i}: {WaveOut.GetCapabilities(i).ProductName}");
            OutputDeviceSelector.SelectedIndex = 0;
        }

        private void InputDeviceSelector_SelectionChanged(object _, SelectionChangedEventArgs __) =>
            selectedInputDevice = InputDeviceSelector.SelectedIndex;

        private void OutputDeviceSelector_SelectionChanged(object _, SelectionChangedEventArgs __) =>
            selectedOutputDevice = OutputDeviceSelector.SelectedIndex;

        /* ── Audio callbacks ────────────────────────────────────── */
        private void WaveIn_DataAvailable(object? _, WaveInEventArgs e)
        {
            byteBuffer?.AddSamples(e.Buffer, 0, e.BytesRecorded);

            writer?.Write(e.Buffer, 0, e.BytesRecorded);
            DrawWaveform(e.Buffer, e.BytesRecorded);
        }

        /* ── Microphone control ─────────────────────────────────── */
        private void StartMicInput()
        {
            if (waveIn != null) return;

            waveIn = new WaveInEvent
            {
                DeviceNumber = selectedInputDevice,
                WaveFormat = waveFormat,
                BufferMilliseconds = 20
            };
            waveIn.DataAvailable += WaveIn_DataAvailable;
            waveIn.RecordingStopped += (_, _) => { waveIn?.Dispose(); waveIn = null; };
            waveIn.StartRecording();

            MicStatus.Text = "Mic active";
        }

        private void StopMicInput()
        {
            if (waveIn == null) return;

            StopMicLoopback();
            StopRecording();

            waveIn.StopRecording();
            MicStatus.Text = "Mic stopped";
            MicToggleButton.IsChecked = false;
        }

        /* ── Loop-back (with pitch) ─────────────────────────────── */
        private void StartMicLoopback()
        {
            if (waveOut != null) return;
            StartMicInput();

            byteBuffer = new BufferedWaveProvider(waveFormat) {
                DiscardOnBufferOverflow = true,
                BufferLength = waveFormat.AverageBytesPerSecond / 5
            };

            activePitchProvider = new SoundTouchPitchProvider(byteBuffer.ToSampleProvider())
            {
                PitchSemitones = currentPitchShift
            };

            waveOut = new WaveOutEvent 
            {
                DeviceNumber = selectedOutputDevice,
                DesiredLatency = 50
            };
            waveOut.Init(activePitchProvider);
            waveOut.Play();

            LoopbackStatus.Text = "Loop-back started";
        }

        private void StopMicLoopback()
        {
            waveOut?.Stop();
            waveOut?.Dispose();
            waveOut = null;
            byteBuffer = null;
            activePitchProvider = null;

            LoopbackStatus.Text = "Loop-back stopped";
            LoopbackToggleButton.IsChecked = false;
        }

        /* ── Recording ──────────────────────────────────────────── */
        private void StartRecording()
        {
            if (writer != null) return;
            StartMicInput();

            string fileName = $"recorded_{DateTime.Now:yyyyMMdd_HHmmss}.wav";
            writer = new WaveFileWriter(fileName, waveFormat);
            RecordingStatus.Text = $"Recording to {fileName}";
        }

        private void StopRecording()
        {
            writer?.Dispose();
            writer = null;
            RecordingStatus.Text = "Recording stopped";
            RecordToggleButton.IsChecked = false;
        }

        /* ── Waveform visualiser ───────────────────────────────── */
        private void DrawWaveform(byte[] buffer, int bytesRecorded)
        {
            Dispatcher.Invoke(() =>
            {
                WaveformCanvas.Children.Clear();
                int width = (int)WaveformCanvas.Width;
                int height = (int)WaveformCanvas.Height;

                int samples = bytesRecorded / 2;
                if (samples == 0) return;

                int step = Math.Max(samples / width, 1);

                for (int x = 0; x < width; x++)
                {
                    int idx = x * step * 2;
                    if (idx >= bytesRecorded) break;

                    short sample = BitConverter.ToInt16(buffer, idx);
                    double normalized = sample / 32768.0;
                    double y = height / 2 - normalized * height / 2;

                    WaveformCanvas.Children.Add(new Line
                    {
                        X1 = x,
                        X2 = x,
                        Y1 = height / 2,
                        Y2 = y,
                        StrokeThickness = 1,
                        Stroke = Brushes.Lime
                    });
                }
            });
        }

        /* ── UI handlers ───────────────────────────────────────── */
        private void MicToggleButton_Checked(object _, RoutedEventArgs __) => StartMicInput();
        private void MicToggleButton_Unchecked(object _, RoutedEventArgs __) => StopMicInput();

        private void LoopbackToggleButton_Checked(object _, RoutedEventArgs __) => StartMicLoopback();
        private void LoopbackToggleButton_Unchecked(object _, RoutedEventArgs __) => StopMicLoopback();

        private void RecordToggleButton_Checked(object _, RoutedEventArgs __) => StartRecording();
        private void RecordToggleButton_Unchecked(object _, RoutedEventArgs __) => StopRecording();

        private void ReloadAudioDevices(object _, RoutedEventArgs __) => LoadAudioDevices();

        private void PitchSlider_ValueChanged(object _, RoutedPropertyChangedEventArgs<double> e)
        {
            currentPitchShift = (float)e.NewValue;
            PitchValue.Text = currentPitchShift.ToString("0");

            if (activePitchProvider != null)
                activePitchProvider.PitchSemitones = currentPitchShift;
        }
    }
}
