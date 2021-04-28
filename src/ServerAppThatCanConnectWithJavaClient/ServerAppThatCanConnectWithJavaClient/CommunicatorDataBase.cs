using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerAppThatCanConnectWithJavaClient
{
    class CommunicatorDataBase : DataBase
    {
        public CommunicatorDataBase(string sqliteFile) : base(sqliteFile) { }

        override protected void CreateDataBase() 
        {
            ExecuteStatement("Create table users(ID integer primary key autoIncrement, username text unique, salt text, password text, avatarID integer default 0)");
            ExecuteStatement("Create table conversations(ID integer primary key autoIncrement, isGroup bit, senderID integer, targetID integer, content text, recvDate date)");
            ExecuteStatement("Create table groups(ID integer primary key autoIncrement, groupName text unique)");
            ExecuteStatement("Create table usersGroups(ID integer primary key autoIncrement, groupID integer, userID integer)");
        }

      
        public User GetUser(string userName)
        {
            SQLiteDataReader userData = ExecuteQuery("Select id, salt, password from users where username = @0", userName);
            if (!userData.Read()) return null;
            string salt = userData.GetString(1);
            string password = userData.GetString(2);
            int id = userData.GetInt32(0);
            return new User(userName, password, salt, id);
        }

        public string ChangeUsername(string username, string newUsername) 
        {
            if (GetUser(newUsername) != null) return null;
            ExecuteStatement("Update users set username = @0 where username = @1", newUsername, username);
            return newUsername;
        }

        public void ChangePassword(string newPassword, string username) 
        {
            ExecuteStatement("Update users set password = @0 where username = @1", newPassword, username);
        }

        public void ChangeUserIcon(int newAvatarID, string username) 
        {
            ExecuteStatement("Update users set avatarID = @0 where username = @1", newAvatarID, username);
        }

        public string GetUsername(int id) 
        {
            SQLiteDataReader userData = ExecuteQuery("Select username from users where ID = @0", id);
            if (!userData.Read()) return null;
            return userData.GetString(0);
        }

        public int GetAvatarID(string username) 
        {
            SQLiteDataReader data = ExecuteQuery("Select avatarID from users where username = @0", username);
            if (!data.Read()) return 0;
            return data.GetInt32(0);
        }

        public int GetAvatarID(int id)
        {
            SQLiteDataReader data = ExecuteQuery("Select avatarID from users where ID = @0", id);
            if (!data.Read()) return 0;
            return data.GetInt32(0);
        }




        public void AddUser(User user)
        {
            ExecuteStatement("Insert into users (username, salt, password) values (@0, @1, @2)", user.Nickname, user.Salt, user.Password);
        }

        public int AddMessage(string senderUsername, string targetUsername, string message, bool isGroup = false)
        {
            User sender = GetUser(senderUsername);
            int targetID = isGroup ? GetGroupId(targetUsername) : GetUser(targetUsername).Id;
            ExecuteStatement("Insert into conversations (isGroup, senderID, targetID, content, recvDate) values (@0, @1, @2, @3, @4)", isGroup, sender.Id, targetID, message, DateTime.Now);
            SQLiteDataReader messageData = ExecuteQuery("Select MAX(id) from conversations where senderID = @0 and targetID = @1 and isGroup = @2", sender.Id, targetID, isGroup);
            if (!messageData.Read()) return -1;
            else return messageData.GetInt32(0);
        }

        public void AddGroup(string groupName, List<string> users) 
        {
            if (users.Count == 0) return;
            if (GetGroupId(groupName) >= 0) return;
            ExecuteStatement("Insert into groups (groupName) values (@0)", groupName);
            int groupID = GetGroupId(groupName);
            foreach(string username in users) 
            {
                User user = GetUser(username);
                ExecuteStatement("Insert into usersGroups (groupID, userID) values (@0, @1)", groupID, user.Id);
            }
        }

        public void AddGroupMemebers(List <String> users, string groupName)
        {
            int groupId = GetGroupId(groupName);
            foreach(string username in users) 
            {
                ExecuteStatement("Insert into usersGroups (groupId, userID) values (@0, @1)", groupId, GetUser(username).Id);
            }
        }

        public void RemoveGroupMemebers(List<String> users, string groupName)
        {
            int groupId = GetGroupId(groupName);
            foreach (string username in users)
            {
                ExecuteStatement("Delete from usersGroups where groupId = @0 and  userID = @1", groupId, GetUser(username).Id);
            }
            List<string> members = GetGroupUsers(groupName);
            if (members.Count == 0) RemoveGroup(groupName);
        }

        public void RemoveGroup(string groupName) 
        {
            int id = GetGroupId(groupName);
            ExecuteStatement("Delete from conversations where isGroup = 1 and targetID = @0", id);
            ExecuteStatement("Delete from usersGroups where groupId = @0", id);
            ExecuteStatement("Delete from groups where ID = @0", id);
        }

        public int GetGroupId(string groupName) 
        {
            SQLiteDataReader groupData = ExecuteQuery("Select id from groups where groupName = @0", groupName);
            if (!groupData.Read()) return -100;
            return groupData.GetInt32(0);
        }

        public void RemoveMessage(int id) 
        {
            ExecuteStatement("Delete from conversations where ID = @0", id);
        }

        public string GetGroupName(int groupID)
        {
            SQLiteDataReader groupData = ExecuteQuery("Select groupName from groups where ID = @0", groupID);
            if (!groupData.Read()) return null;
            return groupData.GetString(0);
        }

        public List<string> GetGroupUsers(string groupName) 
        {
            List<string> users = new List<string>();
            SQLiteDataReader usersData = ExecuteQuery("Select username from users, usersGroups where users.ID = userID and groupID = @0 group by username", GetGroupId(groupName));
            while (usersData.Read()) 
            {
                users.Add(usersData.GetString(0));
            }
            return users;
        }

        public void ChangeGroupName(string oldName, string newName) 
        {
            if (GetGroupId(newName) >= 0) return;
            ExecuteStatement("Update groups set groupName = @0 where groupName = @1", newName, oldName);
        }

        public List<Message> GetConversation(string senderUsername, string targetUsername, bool isGroup) 
        {
            User sender = GetUser(senderUsername);
            SQLiteDataReader conversationData;
            if (!isGroup) 
            {
                User target = GetUser(targetUsername);
                conversationData = ExecuteQuery("Select content, senderID, recvDate, ID from conversations where ((senderID = @0 and targetID=@1) or (senderID=@1 and targetID=@0)) and isGroup = 0 order by recvDate asc", sender.Id, target.Id);
            }
            else 
            {
                conversationData = ExecuteQuery("Select content, senderID, recvDate, ID from conversations where targetID=@0 and isGroup = 1 order by recvDate asc", GetGroupId(targetUsername));
            }
          
            List<Message> conversation = new List<Message>();
            while (conversationData.Read())
            {
                int id = conversationData.GetInt32(1);
                conversation.Add(new Message(conversationData.GetString(0), GetUsername(id), conversationData.GetDateTime(2), conversationData.GetInt32(3), GetAvatarID(id)));
            }

            return conversation;
        }

        /*public List<String> GetUsersList(string username) 
        {
            User user = GetUser(username);
            SQLiteDataReader usersData = ExecuteQuery("Select username from users where ID != @0", user.Id);
            List<String> usersList = new List<string>();
            while (usersData.Read()) 
            {
                usersList.Add(usersData.GetString(0));
            }
            return usersList;
        }*/

        public List<String> GetUsersList(string username)
        {
            User user = GetUser(username);
            SQLiteDataReader usersData = ExecuteQuery("Select senderID, targetID from conversations where (senderID = @0 or targetID = @0) and isGroup = 0 order by recvDate desc", user.Id);
            List<string> usersList = new List<string>();
            while (usersData.Read())
            {
                int senderID = usersData.GetInt32(0);
                int targetID = usersData.GetInt32(1);
                int correct = user.Id == senderID ? targetID : senderID;
                string name = GetUsername(correct);
                if (usersList.Contains(name)) continue;
                usersList.Add(name);
            }
            usersList.Add(username);
            int i = 0;
            string command = "Select username from users where username not in (";
            foreach (string name in usersList)
            {
                command += $"@{i++}, ";
            }
            command = command.Substring(0, command.Length - 2);
            command += ")";
            usersData = ExecuteQuery(command, usersList.Cast<Object>().ToArray());
            while (usersData.Read())
            {
                usersList.Add(usersData.GetString(0));
            }
            usersList.Remove(username);
            return usersList;
        }

        public List<String> GetGroupsList(string username) 
        {
            User user = GetUser(username);
            SQLiteDataReader groupsData = ExecuteQuery("Select groupName from  groups ,usersGroups where userID = @0 and groups.ID = groupID group by groupName", user.Id);
            List<String> groupList = new List<string>();
            while (groupsData.Read())
            {
                groupList.Add(groupsData.GetString(0));
            }
            return groupList;
        }

        public List<(string, int)> GetUsersAndGroupsList(string username) 
        {
            User user = GetUser(username);
            SQLiteDataReader usersData = ExecuteQuery("Select senderID, targetID, isGroup from conversations where senderID = @0 or targetID = @0 order by recvDate desc", user.Id);
            List<string> usersAndGroupsList = new List<string>();
            List<int> avatarsID = new List<int>();
            List<char> typeSigns = new List<char>();
            while (usersData.Read()) 
            {
                int senderID = usersData.GetInt32(0);
                int targetID = usersData.GetInt32(1);
                int correct = user.Id == senderID ? targetID : senderID;
                bool isGroup = usersData.GetBoolean(2);
                string name = isGroup ? GetGroupName(correct) : GetUsername(correct);
                if (usersAndGroupsList.Contains(name) || (senderID != targetID && targetID == user.Id && isGroup)) continue;
                typeSigns.Add(isGroup ? '&' : '*');
                usersAndGroupsList.Add(name);
                avatarsID.Add(isGroup ? 0 : GetAvatarID(name));
            }
            usersAndGroupsList.Add(username);

            List<string> tmpList = new List<string>();
            string command = "Select username from users where username not in (";
            {
                int i = 0;
                foreach (string name in usersAndGroupsList)
                {
                    command += $"@{i++}, ";
                }
            }
            
            command = command.Substring(0, command.Length - 2);
            command += ")";
            usersData = ExecuteQuery(command, usersAndGroupsList.Cast<Object>().ToArray());
            while (usersData.Read()) 
            {
                string name = usersData.GetString(0);
                tmpList.Add(name + "*");
                avatarsID.Add(GetAvatarID(name));

            }
            usersAndGroupsList.Remove(username);

            //command = "Select groupName from groups where groupName not in (";
            command = "Select groupName from groups, usersGroups where userID = @0 and groups.ID = groupID and groupName not in (";
            {
                int i = 1;
                foreach (string name in usersAndGroupsList)
                {
                    command += $"@{i++}, ";
                }
                if (i == 1) command += "..";
            }
            command = command.Substring(0, command.Length - 2);
            command += ") group by groupName";
            object[] array = new object[usersAndGroupsList.Count + 1];
            array[0] = user.Id;
            Array.Copy(usersAndGroupsList.Cast<object>().ToArray(), 0, array, 1, array.Length-1);
            usersData = ExecuteQuery(command, array);
            while (usersData.Read())
            {
                tmpList.Add(usersData.GetString(0) + "&");
                avatarsID.Add(0);
            }

            for (int i = 0; i < typeSigns.Count; i++) 
            {
                usersAndGroupsList[i] += typeSigns[i];
            }

            foreach(string name in tmpList) 
            {
                usersAndGroupsList.Add(name);
            }
            List<(string, int)> finalData = new List<(string, int)>();
            for(int i = 0; i < avatarsID.Count; i++) 
            {
                finalData.Add((usersAndGroupsList[i], avatarsID[i]));
            }
            return finalData;
        }


    }
}
