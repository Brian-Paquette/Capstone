using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Capstone
{
    public class Class
    {
        private int programID, ID, type;
        private string code, name, section, room;
        private DateTime day, start, end;


        public int ProgramID { get => programID; set => programID = value; }
        public int ID1 { get => ID; set => ID = value; }
        public int Type { get => type; set => type = value; }
        public string Code { get => code; set => code = value; }
        public string Name { get => name; set => name = value; }
        public string Section { get => section; set => section = value; }
        public string Room { get => room; set => room = value; }
        public DateTime Day { get => day; set => day = value; }
        public DateTime Start { get => start; set => start = value; }
        public DateTime End { get => end; set => end = value; }
    }
}