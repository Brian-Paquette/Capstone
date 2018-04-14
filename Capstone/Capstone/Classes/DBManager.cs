using Capstone.Models;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
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
            OleDbCommand command = new OleDbCommand("SELECT * FROM  History ORDER BY GenDate", connection);
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
    }
}