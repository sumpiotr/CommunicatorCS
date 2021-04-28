using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerAppThatCanConnectWithJavaClient
{
    abstract class DataBase
    {
        protected SQLiteConnection _connection;

        public DataBase(string sqliteFile) 
        {
            bool fileExist = File.Exists(sqliteFile);
            _connection = new SQLiteConnection($"DataSource={sqliteFile}; Version=3;");
            _connection.Open();
            if (!fileExist) CreateDataBase();
        }

        protected virtual void CreateDataBase() 
        {
         
        }

        protected SQLiteCommand PrepareCommand(string baseStatement, params object[] fields)
        {
            SQLiteCommand command = new SQLiteCommand(_connection);
            command.CommandText = baseStatement;
            int counter = 0;
            foreach (object o in fields)
            {
                baseStatement = baseStatement.Replace($"@{counter}", o.ToString());
                command.Parameters.Add(new SQLiteParameter($"@{counter++}", o));
            }
            Console.WriteLine($"Database: Execute SQL Statement: {baseStatement}");
            return command;
        }
        protected SQLiteDataReader ExecuteQuery(string baseStatement, params object[] fields)
        {
            SQLiteCommand command = PrepareCommand(baseStatement, fields);
            SQLiteDataReader reader = command.ExecuteReader();
            command.Dispose();
            return reader;
        }

        protected void ExecuteStatement(string baseStatement, params object[] fields)
        {
            SQLiteCommand command = PrepareCommand(baseStatement, fields);
            command.ExecuteNonQuery();
            command.Dispose();
        }



    }
}
