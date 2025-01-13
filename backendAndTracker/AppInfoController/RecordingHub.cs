using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace AppInfoController
{
    public class RecordingHub : Hub
    {
        public static ConcurrentDictionary<string, MyUserType> MyUsers = new ConcurrentDictionary<string, MyUserType>();
        public static ConcurrentDictionary<string, MyUserType> AdminUsers = new ConcurrentDictionary<string, MyUserType>();
        public static ConcurrentDictionary<MyUserType, MyUserType> ConnectedStreamings = new ConcurrentDictionary<MyUserType, MyUserType>();

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
                var admin = AdminUsers.FirstOrDefault(_ => _.Value.ConnectionId == Context.ConnectionId);
                if(ConnectedStreamings.Any(_ => _.Value.ConnectionId == user.ConnectionId))
                {
                    return;
                }
                //if (ConnectedStreamings.Any(_ => _.Key.ConnectionId == admin.Value.ConnectionId))
                //{
                //    return;
                //}
                ConnectedStreamings.TryAdd(admin.Value, user);
                await Clients.Client(user.ConnectionId).SendAsync("StartRecording", Context.ConnectionId);
            }
        }

        public async Task SendBytes(ReordingData data)
        {
            await Clients.Clients(data.AdminConnectionId).SendAsync("SendBytes", data);
        }

        public async Task StopRecording(string id)
        {
            _ = MyUsers.TryGetValue(id, out MyUserType user);
            if (user != null)
            {
                var admin = AdminUsers.FirstOrDefault(_ => _.Value.ConnectionId == Context.ConnectionId);
                MyUserType garbage;
                ConnectedStreamings.TryRemove(admin.Value, out garbage);
                await Clients.Client(user.ConnectionId).SendAsync("StopRecording", Context.ConnectionId);
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
