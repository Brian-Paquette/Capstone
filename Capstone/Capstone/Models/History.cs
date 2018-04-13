using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Capstone.Models
{
    public class History
    {
        string fileName;
        string examFileName;
        string calendarURL;
        DateTime genDate;
        string user;

        public int Id { get => Id; set => Id = value; }
        public string FileName { get => fileName; set => fileName = value; }
        public string ExamFileName { get => examFileName; set => examFileName = value; }
        public string CalendarURL { get => calendarURL; set => calendarURL = value; }
        public DateTime GenDate { get => genDate; set => genDate = value; }
        public string User { get => user; set => user = value; }

        public History()
        {

        }
    }
    
}