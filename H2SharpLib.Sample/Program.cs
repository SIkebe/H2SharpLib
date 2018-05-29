using System;
using System.Data.H2;

namespace H2SharpLib.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var connection = new H2Connection("connectionString", "username", "password"))
            {
                connection.Open();

                H2Command command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM ACCOUNT";

                H2DataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Console.WriteLine($"USER_NAME: {reader["USER_NAME"]}  MAIL_ADDRESS: {reader["MAIL_ADDRESS"]}");
                }
            }
        }
    }
}