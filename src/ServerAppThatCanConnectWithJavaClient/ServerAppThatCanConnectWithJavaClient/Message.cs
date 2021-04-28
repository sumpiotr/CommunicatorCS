using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerAppThatCanConnectWithJavaClient
{
    class Message
    {
        public string Content;
        public string AuthorUsername;
        public DateTime ReciveDate;
        public int Id;
        public int AvatarId;

        public Message(string content, string authorUsername, DateTime reciveDate, int id, int avatarId) 
        {
            Content = content;
            AuthorUsername = authorUsername;
            ReciveDate = reciveDate;
            Id = id;
            AvatarId = avatarId;
        }
    }
}
