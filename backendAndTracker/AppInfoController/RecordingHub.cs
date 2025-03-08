using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace AppInfoController
{
    public class RecordingHub : Hub
    {
        public static ConcurrentDictionary<string, MyUserType> MyUsers = new ConcurrentDictionary<string, MyUserType>();
        public static ConcurrentDictionary<string, MyUserType> AdminUsers = new ConcurrentDictionary<string, MyUserType>();
        public static ConcurrentDictionary<MyUserType, MyUserType> ConnectedStreamings = new ConcurrentDictionary<MyUserType, MyUserType>();
        public static ConcurrentBag<string> availableURLs = new ConcurrentBag<string>();
        public static ConcurrentDictionary<MyUserType, string> usedURLs = new ConcurrentDictionary<MyUserType, string>();

        public RecordingHub()
        {
            if(!availableURLs.Any() && !usedURLs.Any())
            {
                //availableURLs.Add("https://friendly-mendeleev.180-179-213-167.plesk.page/recordinghub");
                availableURLs.Add("http://180.179.213.167/plesk-site-preview/appcontroller2.in/recordinghub");
                availableURLs.Add("http://180.179.213.167/plesk-site-preview/appcontroller1.co.in/recordinghub");
                //availableURLs.Add("http://localhost:5122/recordinghub");
            }
        }
        //public override Task OnConnectedAsync()
        //{
        //    MyUsers.TryAdd(Context.ConnectionId, new MyUserType() { ConnectionId = Context.ConnectionId });
        //    return base.OnConnectedAsync();
        //}

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            MyUserType garbage;

            MyUsers.TryRemove(Context.ConnectionId, out garbage);

            if (AdminUsers.TryRemove(Context.ConnectionId, out MyUserType garbage1))
            {
                Clients.All.SendAsync("StopRecording", Context.ConnectionId);
            }

            return base.OnDisconnectedAsync(exception);
        }

        public async Task RegisterUser(string id)
        {
            _ = MyUsers.TryGetValue(id, out MyUserType user);
            if (user != null)
            {
                user.ConnectionId = Context.ConnectionId;
            }
            else
            {
                MyUsers.TryAdd(id, new MyUserType() { ConnectionId = Context.ConnectionId, Id = id });
            }                
            await Task.CompletedTask;
        }

        public async Task RegisterAdmin(string id)
        {
            _ = AdminUsers.TryGetValue(id, out MyUserType user);
            if (user != null)
            {
                //try
                //{
                //    var connectedUser = ConnectedStreamings.FirstOrDefault(_ => _.Key.ConnectionId == user.ConnectionId);
                //    if (connectedUser.Value != null)
                //    {
                //        await Clients.All.SendAsync("StopRecording", connectedUser.Key.ConnectionId);
                //        MyUserType garbage;
                //        ConnectedStreamings.TryRemove(connectedUser.Value, out garbage);
                //    }
                //}
                //catch (Exception ex)
                //{
                //}
                user.ConnectionId = Context.ConnectionId;
            }
            else
            {
                AdminUsers.TryAdd(id, new MyUserType() { ConnectionId = Context.ConnectionId, Id = id });
            }
            await Task.CompletedTask;           
        }

        public async Task StartRecording(string id)
        {
            _ = MyUsers.TryGetValue(id, out MyUserType user);
            if(user != null)
            {
                if (availableURLs.Any())
                {
                    availableURLs.TryTake(out var url);
                    
                    var admin = AdminUsers.FirstOrDefault(_ => _.Value.ConnectionId == Context.ConnectionId);
                    usedURLs.TryAdd(admin.Value, url);
                    if (ConnectedStreamings.Any(_ => _.Value.ConnectionId == user.ConnectionId))
                    {
                        return;
                    }
                    ConnectedStreamings.TryAdd(admin.Value, user);
                    await Clients.Client(Context.ConnectionId).SendAsync("GetURL", url);
                    await Clients.Client(user.ConnectionId).SendAsync("StartRecording", url);
                }
            }
        }

        public async Task SendBytes(ReordingData data)
        {
            await Clients.All.SendAsync("SendBytes", data);
        }

        public async Task StopRecording(string id)
        {
            _ = MyUsers.TryGetValue(id, out MyUserType user);
            if (user != null)
            {
                var admin = AdminUsers.FirstOrDefault(_ => _.Value.ConnectionId == Context.ConnectionId);
                MyUserType garbage;
                ConnectedStreamings.TryRemove(admin.Value, out garbage);
                if(usedURLs.TryRemove(admin.Value, out string url))
                {
                    availableURLs.Add(url);
                }

                await Clients.Client(user.ConnectionId).SendAsync("StopRecording", null);
            }
        }

        public async Task StopAll(string id)
        {
            await Clients.All.SendAsync("StopRecording", Context.ConnectionId);
        }

        public async Task ForceStopAll(string id)
        {
            await Clients.All.SendAsync("StopRecording", null);
        }
    }

    public class MyUserType
    {
        public string Id { get; set; }
        public string ConnectionId { get; set; }
        // Can have whatever you want here
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
