using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Capstone.Classes.GeneratorClasses
{
    public class Exam
    {
        private string code, name, section, faculty, proctor, room, time, dur;

        public string Code { get => code; set => code = value; }
        public string Name { get => name; set => name = value; }
        public string Section { get => section; set => section = value; }
        public string Faculty { get => faculty; set => faculty = value; }
        public string Proctor { get => proctor; set => proctor = value; }
        public string Room { get => room; set => room = value; }
        public string Time { get => time; set => time = value; }
        public string Duration { get => dur; set => dur = value; }
    }
}