using Capstone.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.Linq;
using System.Web;

namespace Capstone.Classes
{
    public class DBManager
    {
        public OleDbConnection GetConnection()
        {
            OleDbConnection connection = new OleDbConnection(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=|DataDirectory|/CapstoneDatabase.accdb");
            return connection;
        }
        public List<History> GetHistoryList()
        {
            List<History> historyList = new List<History>();
            OleDbConnection connection = GetConnection();
            connection.Open();
            OleDbDataReader reader = null;
            OleDbCommand command = new OleDbCommand("SELECT * FROM  History ORDER BY genDate", connection);
            reader = command.ExecuteReader();
            while (reader.Read())
            {
                History h = new History();

                h.FileName = reader["FileName"].ToString();
                h.ExamFileName = reader["ExamFileName"].ToString();
                h.CalendarURL = reader["CalendarURL"].ToString();
                string test = reader["GenDate"].ToString();
                h.GenDate = DateTime.Parse(reader["GenDate"].ToString());
                h.User = reader["User"].ToString();

                historyList.Add(h);
            }
            connection.Close();
            return historyList;
        }
        public void NewHistoryEntry(string file, string exam, string calendarURL, string user)
        {
            OleDbConnection connection = GetConnection();
            OleDbCommand command = new OleDbCommand("INSERT INTO History([FileName], [ExamFileName], [CalendarURL], [GenDate], [User]) VALUES (@file,@exam,@calURL,@genDate,@user)", connection);
            command.Parameters.AddWithValue("@file", file);
            command.Parameters.AddWithValue("@exam", exam);
            command.Parameters.AddWithValue("@calURL", calendarURL);
            command.Parameters.AddWithValue("@genDate", DateTime.Now.ToString());
            command.Parameters.AddWithValue("@user", user);
            try
            {
                connection.Open();
                command.ExecuteNonQuery();
            }
            catch(Exception e){ }
            connection.Close();
        }
    }
}