using Microsoft.AspNetCore.SignalR.Client;
using NAudio.Wave;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Security.Policy;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.ComponentModel;

namespace MicrophoneRecorder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static string _url = "https://ac.saralesuvidha.com/";
        private BufferedWaveProvider bwp;
        WaveOut wo;
        HubConnection hubConnection;
        private int sampleRate;
        private int channelCount;
        private Model SelectedUser;
        private bool isListening;
        private static string _userInner = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
        public MainWindow()
        {
            ItemList = new ObservableCollection<Model>();
            InitializeComponent();
            var users = GetUsers();
            
            foreach(var user in users)
            {
                var model = new Model();
                model.User = user.User;
                model.DisplayName = user.User + " " + user.Name + " " + user.MobileNo + " " + user.City;
                ItemList.Add(model);
            }
            hubConnection = new HubConnectionBuilder().WithUrl($"{_url}recordinghub").WithAutomaticReconnect().Build();
            hubConnection.Reconnected += (msg) => hubConnection.InvokeAsync("RegisterAdmin", _userInner);
            hubConnection.StartAsync().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                }
                else
                {
                    try
                    {
                        hubConnection.InvokeAsync("RegisterAdmin", _userInner);
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }).Wait();
            hubConnection.On<ReordingData>("SendBytes", (message) =>
            {
                if(isListening)
                {
                    if (wo == null)
                    {
                        wo = new WaveOut();
                        bwp = new BufferedWaveProvider(WaveFormat.CreateIeeeFloatWaveFormat(message.SampleRate, message.ChannelCount));
                        bwp.DiscardOnBufferOverflow = true;
                        wo.Init(bwp);
                        wo.Play();
                    }
                    bwp.AddSamples(message.Buffer, 0, message.BytesRecorded);
                }                
            });
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            wo = null;
            var model = cb.SelectedItem as Model;
            if (model != null)
            {
                Button1.IsEnabled = false;
                cb.IsEnabled = false;
                SelectedUser = model;
                isListening = true;
                hubConnection.InvokeAsync("StartRecording", model.User);
            }
        }

        public ObservableCollection<Model> ItemList { get; set; }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var model = cb.SelectedItem as Model;
            if (model != null)
            {
                isListening = false;
                cb.IsEnabled = true;
                SelectedUser = null;
                hubConnection.InvokeAsync("StopRecording", model.User);
            }
            Button1.IsEnabled = true;
        }

        private static IEnumerable<LastHitByUser> GetUsers()
        {
            var users = new List<LastHitByUser>();
            try
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                };
                var client = new HttpClient(handler);

                // Set the base address to simplify maintenance & requests
                client.BaseAddress = new Uri(_url);

                // Post to the endpoint

                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                //GET Method

                var response = client.GetAsync($"/appinfo/GetLastHitByUserDetails").Result;
                if (response.IsSuccessStatusCode)
                {
                    users = response.Content.ReadAsAsync<List<LastHitByUser>>().Result;
                }
            }
            catch (Exception ex)
            {
            }
            return users;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (SelectedUser != null)
            {
                cb.IsEnabled = true;
                hubConnection.InvokeAsync("StopRecording", SelectedUser.User);
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            hubConnection.InvokeAsync("StopAll", "");
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            hubConnection.InvokeAsync("ForceStopAll", "");
        }
    }

    public class ReordingData
    {
        public byte[] Buffer { get; set; }
        public int BytesRecorded { get; set; }
        public int SampleRate { get; set; }
        public int ChannelCount { get; set; }
    }

    public class LastHitByUser
    {
        public long Id { get; set; }

        public string User { get; set; }

        public string Date { get; set; }

        public string Name { get; set; }

        public string MobileNo { get; set; }

        public string City { get; set; }

        public string Address { get; set; }

        public bool Inactive { get; set; }

        public string AllowedUserId { get; set; }

        public string AppVersion { get; set; }

        public string Summary { get; set; }
    }

    public class Model
    {
        public string User { get; set; }
        public string DisplayName { get; set; }
    }
}
