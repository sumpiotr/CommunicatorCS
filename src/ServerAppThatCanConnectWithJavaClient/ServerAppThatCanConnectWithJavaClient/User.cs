using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerAppThatCanConnectWithJavaClient
{
    public class User
    {
        public int Id;
        public string Nickname;
        public string Password;
        public string Salt;

        public User(string nickname, string password, string salt, int id) 
        {
            Nickname = nickname;
            Password = password;
            Salt = salt;
            Id = id;
        }

    }
}
