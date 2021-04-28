using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace ServerAppThatCanConnectWithJavaClient
{
    class Program
    {
        static Dictionary<string, List<CSocket>> OnlineClientList = new Dictionary<string, List<CSocket>>();
        static CommunicatorDataBase dataBase;
        static char[] AllowedSigns = new char[62];

        static void Main(string[] args)
        {
            FillAllowedSignsArray();

 
            int portNumber = 1232;
            dataBase = new CommunicatorDataBase("data.db");
            TcpListener server = new TcpListener(IPAddress.Any, portNumber);
            Thread handleConnection = new Thread(() => { HandleConnection(server); });
            handleConnection.Start();
            while (true)
            {
                string command = Console.ReadLine();
                //command processing
            }
        }

        public static void HandleConnection(TcpListener server)
        {
            server.Start();
            Console.WriteLine("Server is working...");
            while (true)
            {
                CSocket client = new CSocket(server.AcceptSocket());
                Thread clientThread = new Thread(() => { ClientThread(client); });
                clientThread.Start();
            }
        }

        public static void ClientThread(CSocket client)
        {
        START_CLIENT_THREAD:
            string username;
            //Log in or Register
            while (true)
            {
                ServerEvents eventType = (ServerEvents)client.ReceiveInt32();
                if (eventType == ServerEvents.Login)
                {
                    username = client.ReceiveString();
                    string password = client.ReceiveString();
                    if (Login(username, password))
                    {
                        client.SendInt32((int)ServerEvents.Login);
                        break;
                    }
                    else
                    {
                        client.SendInt32((int)ServerEvents.Error);
                        client.SendString("Wrong username or password!");
                    }
                }
                else if (eventType == ServerEvents.Register)
                {
                    username = client.ReceiveString();
                    string password = client.ReceiveString();
                    if (Register(username, password))
                    {
                        client.SendInt32((int)ServerEvents.Register);
                        AddUser(username);
                        break;
                    }
                    else
                    {
                        client.SendInt32((int)ServerEvents.Error);
                        client.SendString($"Username {username} is already used!");
                    }
                }
                else
                {
                    CloseConnection(client, null);
                    return;
                }
            }


            if (OnlineClientList.ContainsKey(username))
            {
                OnlineClientList[username].Add(client);
            }
            else
            {
                OnlineClientList.Add(username, new List<CSocket>());
                OnlineClientList[username].Add(client);
            }



            client.SendString(username);
            client.SendInt32(dataBase.GetAvatarID(username));


            //Client events 
            while (true)
            {
                ServerEvents eventType = (ServerEvents)client.ReceiveInt32();
                switch (eventType)
                {
                    case ServerEvents.End:
                        {
                            CloseConnection(client, username);
                            return;
                        }
                    case ServerEvents.Logout:
                        {
                            OnlineClientList[username].RemoveAt(OnlineClientList[username].IndexOf(client));
                            if (OnlineClientList[username].Count == 0) OnlineClientList.Remove(username);
                            Console.WriteLine($"Client {username} disconnected");
                            client.SendInt32((int)ServerEvents.Logout);
                            goto START_CLIENT_THREAD;
                        }
                    case ServerEvents.SendMessage:
                        {
                            string targetUsername = client.ReceiveString();
                            string message = client.ReceiveString();
                            char typeSign = targetUsername[targetUsername.Length - 1];
                            targetUsername = targetUsername.Substring(0, targetUsername.Length - 1);
                            SendMessage(username, targetUsername, message, typeSign == '&');
                            break;
                        }
                    case ServerEvents.GetConversation:
                        {
                            string targetUsername = client.ReceiveString();
                            char typeSign = targetUsername[targetUsername.Length - 1];
                            targetUsername = targetUsername.Substring(0, targetUsername.Length - 1);
                            List<Message> conversation;
                            conversation = dataBase.GetConversation(username, targetUsername, typeSign == '&');
                            client.SendInt32((int)ServerEvents.GetConversation);
                            client.SendInt32(conversation.Count);
                            foreach (Message message in conversation)
                            {
                                client.SendString(message.Content);
                                client.SendString(message.AuthorUsername);
                                client.SendInt32(message.Id);
                                client.SendInt32(message.AvatarId);
                                client.SendInt32(message.ReciveDate.Year);
                                client.SendInt32(message.ReciveDate.Month);
                                client.SendInt32(message.ReciveDate.Day);
                                client.SendInt32(message.ReciveDate.Hour);
                                client.SendInt32(message.ReciveDate.Minute);
                            }
                            break;
                        }
                    case ServerEvents.GetUsers:
                        {
                            List<string> users = dataBase.GetUsersList(username);
                            client.SendInt32((int)ServerEvents.GetUsers);
                            client.SendInt32(users.Count);
                            foreach (string name in users)
                            {
                                client.SendString(name + '*');
                            }
                            break;
                        }
                    case ServerEvents.AddGroup:
                        {
                            string groupName = client.ReceiveString();
                            int length = client.ReceiveInt32();
                            List<string> users = new List<string>();
                            for (int i = 0; i < length; i++)
                            {
                                users.Add(client.ReceiveString());
                            }
                            if (dataBase.GetGroupId(groupName) >= 0)
                            {
                                client.SendInt32((int)ServerEvents.Error);
                                client.SendString($"Groupname {groupName} is already used");
                                break;
                            }
                            users.Add(username);
                            dataBase.AddGroup(groupName, users);
                            client.SendInt32((int)ServerEvents.AddGroup);
                            AddGroup(users, groupName);
                            break;
                        }
                    case ServerEvents.AddGroupMemeber: 
                        {
                            string groupName = client.ReceiveString();
                            groupName = groupName.Substring(0, groupName.Length - 1);
                            int length = client.ReceiveInt32();
                            List<string> users = new List<string>();
                            for (int i = 0; i < length; i++)
                            {
                                users.Add(client.ReceiveString());
                            }
                            List<string> groupUsers = dataBase.GetGroupUsers(groupName);
                            
                            foreach(string member in groupUsers) 
                            {
                                if (!OnlineClientList.ContainsKey(member)) continue;
                                foreach(CSocket socket in OnlineClientList[member]) 
                                {
                                    foreach (string name in users)
                                    {
                                        socket.SendInt32((int)ServerEvents.AddGroupMemeber);
                                        socket.SendString(groupName + "&");
                                        socket.SendString(name);
                                    }
                                }
                            }
                            dataBase.AddGroupMemebers(users, groupName);
                            AddGroup(users, groupName);
                            break;
                        }
                    case ServerEvents.GetUsersAndGroups:
                        {
                            List<(string, int)> usersAndGroupsList = dataBase.GetUsersAndGroupsList(username);
                            client.SendInt32((int)ServerEvents.GetUsersAndGroups);
                            client.SendInt32(usersAndGroupsList.Count);
                            foreach ((string name, int avatarID) data in usersAndGroupsList)
                            {
                                client.SendString(data.name);
                                client.SendInt32(data.avatarID);
                            }
                            break;
                        }
                    case ServerEvents.GetAddGroupUsers:
                        {
                            string groupName = client.ReceiveString();
                            List<string> users = dataBase.GetUsersList(username);
                            client.SendInt32((int)ServerEvents.GetUsers);
                            List<string> groupUsers = dataBase.GetGroupUsers(groupName.Substring(0, groupName.Length - 1));
                            groupUsers.Remove(username);
                            foreach (string user in groupUsers)
                            {
                                users.Remove(user);
                            }
                            client.SendInt32(users.Count);
                            foreach (string name in users)
                            {
                                client.SendString(name + '*');
                            }
                            break;
                        }
                    case ServerEvents.GetGroupMembers: 
                        {
                            string groupName = client.ReceiveString();
                            client.SendInt32((int)ServerEvents.GetGroupMembers);
                            List<string> groupUsers = dataBase.GetGroupUsers(groupName.Substring(0, groupName.Length - 1));
                            client.SendInt32(groupUsers.Count);
                            foreach (string name in groupUsers)
                            {
                                client.SendString(name + '*');
                            }
                            break;
                        }
                    case ServerEvents.ChangeLogin:
                        {
                            string newUsername = client.ReceiveString();
                            if (dataBase.ChangeUsername(username, newUsername) == null)
                            {
                                client.SendInt32((int)ServerEvents.Error);
                                client.SendString($"Username {newUsername} is already use");
                                break;
                            }
                            List<CSocket> tmp = OnlineClientList[username];
                            OnlineClientList.Remove(username);
                            OnlineClientList.Add(newUsername, tmp);
                            foreach (var list in OnlineClientList)
                            {
                                foreach (CSocket socket in list.Value)
                                {
                                    socket.SendInt32((int)ServerEvents.ChangeLogin);
                                    socket.SendString(username + "*");
                                    socket.SendString(newUsername + "*");
                                    socket.SendInt32(dataBase.GetAvatarID(newUsername));
                                }
                            }
                            username = newUsername;
                            break;
                        }
                    case ServerEvents.ChangeIcon:
                        {
                            int iconIndex = client.ReceiveInt32();
                            dataBase.ChangeUserIcon(iconIndex, username);
                            foreach (var list in OnlineClientList)
                            {
                                foreach (CSocket socket in list.Value)
                                {
                                    socket.SendInt32((int)ServerEvents.ChangeIcon);
                                    socket.SendString(username + "*");
                                    socket.SendInt32(iconIndex);
                                }
                            }
                            break;
                        }
                    case ServerEvents.ChangePassword:
                        {
                            string newPassword = client.ReceiveString();
                            User me = dataBase.GetUser(username);
                            string saltPassword = newPassword + me.Salt;
                            saltPassword = CalcSum(saltPassword);
                            dataBase.ChangePassword(saltPassword, username);
                            break;
                        }
                    case ServerEvents.ChangeGroupName: 
                        {
                            string oldName = client.ReceiveString();
                            oldName = oldName.Substring(0, oldName.Length - 1);
                            string newGroupName = client.ReceiveString();
                            if(dataBase.GetGroupId(newGroupName) >= 0) 
                            {
                                client.SendInt32((int)ServerEvents.Error);
                                client.SendString($"Groupname {newGroupName} is already used");
                                break;
                            }
                            dataBase.ChangeGroupName(oldName, newGroupName);
                            List<string> users = dataBase.GetGroupUsers(newGroupName);
                            foreach(string name in users) 
                            {
                                if (!OnlineClientList.ContainsKey(name)) continue;
                                foreach(CSocket socket in OnlineClientList[name])
                                {
                                    socket.SendInt32((int)ServerEvents.ChangeLogin);
                                    socket.SendString(oldName + "&");
                                    socket.SendString(newGroupName + "&");
                                    socket.SendInt32(0);
                                }
                            }
                            break;
                        }
                    case ServerEvents.RemoveMessage:
                        {
                            string targetUsername = client.ReceiveString();
                            char typeSign = targetUsername[targetUsername.Length - 1];
                            targetUsername = targetUsername.Substring(0, targetUsername.Length - 1);
                            int messageID = client.ReceiveInt32();
                            RemoveMessage(targetUsername, username, messageID, typeSign == '&');
                            break;
                        }
                    case ServerEvents.RemoveGroupMember: 
                        {
                            string groupName = client.ReceiveString();
                            groupName = groupName.Substring(0, groupName.Length - 1);
                            int length = client.ReceiveInt32();
                            List<string> users = new List<string>();
                            for (int i = 0; i < length; i++)
                            {
                                users.Add(client.ReceiveString());
                            }
                            RemoveGroup(users, groupName);
                            dataBase.RemoveGroupMemebers(users, groupName);
                            break;
                        }
                }
            }

        }



        #region Server functions

        public static void SendMessage(string authorUsername, string targetUsername, string message, bool isGroup)
        {
            int id = dataBase.AddMessage(authorUsername, targetUsername, message, isGroup);

            if (isGroup)
            {
                List<string> users = dataBase.GetGroupUsers(targetUsername);
                foreach (string username in users)
                {
                    if (!OnlineClientList.ContainsKey(username)) continue;
                    foreach (CSocket socket in OnlineClientList[username])
                    {
                        socket.SendInt32((int)ServerEvents.ReceiveMessage);
                        char typeSign = '&';
                        socket.SendString(targetUsername + typeSign);
                        socket.SendString(message);
                        socket.SendInt32(id);
                        socket.SendInt32(dataBase.GetAvatarID(authorUsername));
                        socket.SendBoolean(true);
                        socket.SendString(authorUsername + "*");
                    }
                }
            }
            else
            {

                foreach (CSocket socket in OnlineClientList[authorUsername])
                {
                    socket.SendInt32((int)ServerEvents.ReceiveMessage);
                    char typeSign = '*';
                    socket.SendString(targetUsername + typeSign);
                    socket.SendString(message);
                    socket.SendInt32(id);
                    socket.SendInt32(0);
                    socket.SendBoolean(true);
                    socket.SendString(authorUsername + "*");
                }

                if (!OnlineClientList.ContainsKey(targetUsername)) return;
                foreach (CSocket socket in OnlineClientList[targetUsername])
                {
                    socket.SendInt32((int)ServerEvents.ReceiveMessage);
                    char typeSign = '*';
                    socket.SendString(authorUsername + typeSign);
                    socket.SendString(message);
                    socket.SendInt32(id);
                    socket.SendInt32(dataBase.GetAvatarID(authorUsername));
                    socket.SendBoolean(false);
                }
            }


        }

        public static void RemoveMessage(string targetUsername, string authorUsername, int id, bool isGroup)
        {
            dataBase.RemoveMessage(id);
            if (isGroup)
            {
                List<string> users = dataBase.GetGroupUsers(targetUsername);
                foreach (string username in users)
                {
                    if (!OnlineClientList.ContainsKey(username) || username == authorUsername) continue;
                    foreach (CSocket socket in OnlineClientList[username])
                    {
                        socket.SendInt32((int)ServerEvents.RemoveMessage);
                        char typeSign = '&';
                        socket.SendString(targetUsername + typeSign);
                        socket.SendInt32(id);
                    }
                }
            }
            else
            {
                if (!OnlineClientList.ContainsKey(targetUsername)) return;
                foreach (CSocket socket in OnlineClientList[targetUsername])
                {
                    socket.SendInt32((int)ServerEvents.RemoveMessage);
                    char typeSign = '*';
                    socket.SendString(authorUsername + typeSign);
                    socket.SendInt32(id);
                }
            }
        }



        public static void AddUser(string username)
        {
            foreach (var data in OnlineClientList)
            {
                if (data.Key == username) continue;
                foreach (CSocket client in data.Value)
                {
                    client.SendInt32((int)ServerEvents.AddUser);
                    client.SendString(username + "*");
                }
            }
        }

        public static void AddGroup(List<string> users, string groupName)
        {
            foreach (string username in users)
            {
                if (OnlineClientList.ContainsKey(username))
                {
                    foreach (CSocket client in OnlineClientList[username])
                    {
                        client.SendInt32((int)ServerEvents.AddUser);
                        client.SendString(groupName + "&");
                    }
                }
            }
        }

        public static void RemoveGroup(List<string> users, string groupName) 
        {

            List<string> groupMembers = dataBase.GetGroupUsers(groupName);

            foreach(string username in groupMembers)
            {
                if (!OnlineClientList.ContainsKey(username)) continue;
                if (users.Contains(username)) 
                {
                    foreach (CSocket socket in OnlineClientList[username])
                    {
                        socket.SendInt32((int)ServerEvents.RemoveGroupMember);
                        socket.SendString(groupName + "&");
                        socket.SendString(username);
                    }
                }
                else 
                {
                    foreach (CSocket socket in OnlineClientList[username])
                    {
                        foreach(string name in users) 
                        {
                            socket.SendInt32((int)ServerEvents.RemoveGroupMember);
                            socket.SendString(groupName + "&");
                            socket.SendString(name);
                        } 
                    }
                }
               
            }

        }

        public static void CloseConnection(CSocket socket, string username)
        {
            socket.SendInt32((int)ServerEvents.End);
            socket.Close();
            if (username != null)
            {
                OnlineClientList[username].RemoveAt(OnlineClientList[username].IndexOf(socket));
                if (OnlineClientList[username].Count == 0) OnlineClientList.Remove(username);
                Console.WriteLine($"Client {username} disconnected");
            }

        }

        #endregion

        #region Login and register

        public static bool Register(string username, string password)
        {
            if (dataBase.GetUser(username) != null) return false;
            string salt = GenerateRandomSalt(20);
            string saltPassword = password + salt;
            saltPassword = CalcSum(saltPassword);
            User newUser = new User(username, saltPassword, salt, 0);
            dataBase.AddUser(newUser);
            return true;
        }

        public static bool Login(string username, string password)
        {
            User user = dataBase.GetUser(username);
            if (user == null) return false;
            password += user.Salt;
            return user.Password == CalcSum(password);

        }

        public static string GenerateRandomSalt(int size)
        {
            string salt = "";
            Random r = new Random();
            for (int i = 0; i < size; i++)
            {
                salt += AllowedSigns[r.Next(0, AllowedSigns.Length - 1)];
            }
            return salt;
        }

        public static string CalcSum(string str)
        {
            using (SHA256 instance = SHA256.Create())
            {
                byte[] hash = instance.ComputeHash(Encoding.UTF8.GetBytes(str));
                return BitConverter.ToString(hash).Replace("-", "");
            }
        }

        public static void FillAllowedSignsArray()
        {
            int tmp = 'a';
            for (int i = 0; i < 26; i++)
            {
                AllowedSigns[i] = (char)tmp++;
            }
            tmp = 'A';
            for (int i = 26; i < 52; i++)
            {
                AllowedSigns[i] = (char)tmp++;
            }
            tmp = '0';
            for (int i = 52; i < 62; i++)
            {
                AllowedSigns[i] = (char)tmp++;
            }
        }

        #endregion

    }

    public enum ServerEvents
    {
        End,
        Error,
        Register,
        Login,
        Logout,
        SendMessage,
        ReceiveMessage,
        GetConversation,
        AddUser,
        GetUsers,
        AddGroup,
        AddGroupMemeber,
        GetUsersAndGroups,
        GetAddGroupUsers,
        GetGroupMembers,
        ChangeLogin,
        ChangePassword,
        ChangeIcon,
        ChangeGroupName,
        RemoveMessage,
        RemoveGroupMember,
    }

}
