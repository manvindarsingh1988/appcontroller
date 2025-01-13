using Microsoft.AspNetCore.SignalR.Client;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Threading;

namespace AppController
{
    public class WasapiCaptureHelper
    {
        private MMDevice selectedDevice;
        private int sampleRate;
        private int channelCount;
        private WasapiCapture capture;
        private string currentFileName;
        private int shareModeIndex;
        private HubConnection hubConnection;
        public volatile bool isEnable = false;
        public volatile string adminConnectionId = string.Empty;

        public WasapiCaptureHelper(HubConnection hubConnection)
        {
            this.hubConnection = hubConnection;
            var enumerator = new MMDeviceEnumerator();
            selectedDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
            GetDefaultRecordingFormat(selectedDevice);
        }

        private void GetDefaultRecordingFormat(MMDevice value)
        {
            using (var c = new WasapiCapture(value))
            {
                sampleRate = c.WaveFormat.SampleRate;
                channelCount = c.WaveFormat.Channels;
            }
        }

        public void HandleRecording()
        {
            Thread th = new Thread(() =>
            {
                isEnable = true;
                Record();
                while (isEnable)
                {
                }
                Stop();
            });
            th.Start();
        }

        public void Record()
        {
            try
            {
                capture = new WasapiCapture(selectedDevice);
                capture.ShareMode = AudioClientShareMode.Shared;
                capture.WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount);
                capture.StartRecording();
                capture.RecordingStopped += OnRecordingStopped;
                capture.DataAvailable += CaptureOnDataAvailable;
            }
            catch (Exception e)
            {
            }
        }

        public void Stop()
        {
            capture?.StopRecording();
            WriteException(new Exception("Stop"));
        }

        void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            capture?.Dispose();
            capture = null;
            WriteException(new Exception("OnRecordingStopped"));
        }

        private void CaptureOnDataAvailable(object sender, WaveInEventArgs waveInEventArgs)
        {
            if(isEnable)
            {
                hubConnection.InvokeAsync<ReordingData>("SendBytes", new ReordingData { Buffer = waveInEventArgs.Buffer, BytesRecorded = waveInEventArgs.BytesRecorded, SampleRate = sampleRate, ChannelCount = channelCount, AdminConnectionId = adminConnectionId });                
            }
        }

        private static void WriteException(Exception ex)
        {
            var st = string.Empty;
            if (System.IO.File.Exists("test1.txt"))
            {
                st = System.IO.File.ReadAllText("test1.txt");
            }
            System.IO.File.WriteAllText("test1.txt", st + Environment.NewLine + ex.Message);
        }
    }

    public class ReordingData
    {
        public byte[] Buffer { get; set; }
        public int BytesRecorded { get; set; }
        public int SampleRate { get; set; }
        public int ChannelCount { get; set; }
        public string AdminConnectionId { get; set; }
    }
}
