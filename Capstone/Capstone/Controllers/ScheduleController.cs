using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.OleDb;
using System.Threading.Tasks;
using System.IO;
using System.Data;
using Newtonsoft.Json;
using System.Diagnostics;
using Capstone.Classes.GeneratorClasses;

namespace Capstone.Controllers
{
    public class ScheduleController : Controller
    {
        // GET: ScheduleGenerator
        public ActionResult Index()
        {
            //If for ever any reason we return here ... just go back to the main page!
            return RedirectToAction("Index", "Home", null);
        }

        [HttpPost]
        public ActionResult ImportExcel(HttpPostedFileBase sheetFile)
        {
            if (sheetFile == null || sheetFile.ContentLength == 0)
            {
                TempData["UploadError"] = "You must upload a file.";
            }
            if (sheetFile.FileName.EndsWith(".xls") || sheetFile.FileName.EndsWith(".xlsx"))
            {
                TempData["UploadSuccess"] = "Upload successful!";
                // Do processing
                ProcessSchedule(sheetFile);
            }
            else
            {
                TempData["UploadError"] = "Please upload a valid file.";
            }
            return RedirectToAction("Index", "Home", null);
        }

        public void ProcessSchedule(HttpPostedFileBase sheetFile)
        {
            string path = Server.MapPath("~/Content/sheetStorage/" + sheetFile.FileName);
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
            sheetFile.SaveAs(path);

            // Connect to recently saved excel sheet
            OleDbConnection conn = null;
            if (sheetFile.FileName.EndsWith(".xls"))
                conn = new OleDbConnection(@"Provider=Microsoft.Jet.OLEDB.4.0; data source=" + path + ";Extended Properties=\"Excel 8.0;HDR=1;IMEX=1\";");
            if (sheetFile.FileName.EndsWith(".xlsx"))
                conn = new OleDbConnection(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + path + ";Extended Properties=\"Excel 12.0 Xml;HDR=1;IMEX=1\";");

            conn.Open();
            DataTable data = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
            string sheet = data.Rows[0]["Table_Name"].ToString();

            OleDbCommand sheetCommand = new OleDbCommand(@"SELECT * FROM [" + sheet + @"]", conn);
            OleDbDataAdapter sheetAdapter = new OleDbDataAdapter(sheetCommand);

            DataSet sheetData = new DataSet();
            sheetAdapter.Fill(sheetData);
            conn.Close();
            DataSet examSchedule = GenerateExamSchedule(sheetData);
            //Debug.WriteLine(JsonConvert.SerializeObject(examSchedule, Formatting.Indented));
        }

        public DataSet GenerateExamSchedule(DataSet classData)
        {
            // Data Ref Points
            // Indexing ensures that any sheet will function appropriately even if the headers aren't exact, so long as
            // they're in the right place. In future development, we could use a series of value comparisons to dynamically
            // assign indexes to allow a sheet to have varying column placement. Quality of life.
            int CODE = 0;
            int SECTION = 1;
            int NAME = 2;
            int FACULTY = 3;
            int ROOM = 4;
            int DAY = 5;
            int START = 6;
            int END = 7;
            int DUR = 8;

            // roomSlots logs available rooms in a series of dictionary layers to optimizate iteration patterns.
            // [Day][StartTime][Room, EndTime]
            Dictionary<string, Dictionary<string, Dictionary<string, string>>>
                rooms = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
            // classes logs every unique class by program code, and then by section.
            // [Code][Section][Name, FACULTY, Duration]
            Dictionary<string, Dictionary<string, Dictionary<string, string>>>
                classes = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
            // FACULTYs logs all available program FACULTYs which are thne used to assign proctors.
            // [Name, Available]
            Dictionary<string, DateTime> faculty = new Dictionary<string, DateTime>();

            foreach (DataRow row in classData.Tables[0].Rows)
            {
                // Establish Room Slot
                if (!rooms.ContainsKey(row[DAY].ToString()))
                    rooms[row[DAY].ToString()] = new Dictionary<string, Dictionary<string, string>>();
                if (!rooms[row[DAY].ToString()].ContainsKey(row[START].ToString()))
                    rooms[row[DAY].ToString()][row[START].ToString()] = new Dictionary<string, string>();
                rooms[row[DAY].ToString()][row[START].ToString()][row[ROOM].ToString()] = row[END].ToString();
                // Establish Classes
                if (!classes.ContainsKey(row[CODE].ToString()))
                    classes[row[CODE].ToString()] = new Dictionary<string, Dictionary<string, string>>();
                if (!classes[row[CODE].ToString()].ContainsKey(row[SECTION].ToString()))
                    classes[row[CODE].ToString()][row[SECTION].ToString()] = new Dictionary<string, string>()
                    {
                        { "NAME", row[NAME].ToString() },
                        { "FACULTY", row[FACULTY].ToString() },
                        { "DUR", row[DUR].ToString() }
                    };
                // Establish FACULTYs
                faculty[row[FACULTY].ToString()] = new DateTime();
            }
            int iteration = 0;
            List<Exam> exams = new List<Exam>();

            List<string> classKeys = new List<string>(classes.Keys);
            List<string> roomKeys = new List<string>(rooms.Keys);

            // This loop restricts exams from being placed in time slots that might need to accomodate larger section splits
            while(classKeys.Count > 0 && iteration <= 1)
            {
                foreach (string classSlot in classKeys)
                {
                    // All weekdays.
                    foreach (string roomSlot in roomKeys)
                    {
                        // All time entries within a week day. For example, 12:00. Contains all rooms and end times for rooms that start at 12.
                        List<string> timeKeys = new List<string>(rooms[roomSlot].Keys);
                        foreach (string timeSlot in timeKeys)
                        {
                            // This condition verifies whether or not this specific timeslot (E.G. 12:00) contains as many available rooms as there are
                            // sections in a class.
                            if (rooms[roomSlot][timeSlot].Count() == classes[classSlot].Count() 
                                || (iteration == 1 && rooms[roomSlot][timeSlot].Count() >= classes[classSlot].Count()))
                            {
                                // Next, we need to verify that this room slot is large enough to accomodate our exam duration
                                bool valid = true;
                                DateTime start = DateTime.Now;
                                DateTime end = DateTime.Now;
                                try
                                {
                                    start = Convert.ToDateTime(timeSlot);

                                    List<string> spefRoomKeys = new List<string>(rooms[roomSlot][timeSlot].Keys);
                                    foreach (string room in spefRoomKeys)
                                    {
                                        if (rooms[roomSlot][timeSlot][room] == null)
                                            continue;
                                        try
                                        {
                                            end = Convert.ToDateTime(rooms[roomSlot][timeSlot][room]);
                                            TimeSpan roomAvailability = end.Subtract(start);
                                            List<string> sectionKeys = new List<string>(classes[classSlot].Keys);
                                            foreach (string section in sectionKeys)
                                            {
                                                if (int.Parse(classes[classSlot][section]["DUR"].ToString()) > roomAvailability.Hours)
                                                    valid = false;
                                            }
                                        }
                                        catch (Exception e) { valid = false; } // TO DO if this catches, we should return a row value through an error message
                                    }
                                }
                                catch (Exception e) { valid = false; } // TO DO if this catches, we should return a row value through an error message

                                // If this room slot is valid, it means all of our sections can be accomodated here! Yipee!!
                                if (valid)
                                {
                                    List<string> sectionKeys = new List<string>(classes[classSlot].Keys);
                                    foreach (string section in sectionKeys)
                                    {
                                        List<string> spefRoomKeys = new List<string>(rooms[roomSlot][timeSlot].Keys);
                                        foreach (string room in spefRoomKeys)
                                        {
                                            Exam sectionExam = new Exam();
                                            sectionExam.Code = classSlot;
                                            sectionExam.Name = classes[classSlot][section]["NAME"];
                                            sectionExam.Section = section;
                                            sectionExam.Faculty = classes[classSlot][section]["FACULTY"];
                                            sectionExam.Proctor = null;
                                            sectionExam.Room = room;
                                            sectionExam.Time = string.Format("{0:hh:mm tt}", start) + "-" + string.Format("{0:hh:mm tt}", end);
                                            sectionExam.Duration = classes[classSlot][section]["DUR"] + " hour(s)";
                                            exams.Add(sectionExam);

                                            rooms[roomSlot][timeSlot].Remove(room);
                                            break;
                                        }
                                        classes[classSlot].Remove(section);
                                    }
                                }
                            }
                        }
                    }
                }
                iteration++; // iteration counter limits potential looping locking caused by inadequate room availibility. If we reach that, we should output an error
                classKeys = new List<string>(classes.Keys);
                roomKeys = new List<string>(rooms.Keys);
            }

            //// With all exams scheduled, we can now assign proctors!
            //List<string> facKeys = new List<string>(faculty.Keys);
            //for (int i = 0; i <= exams.Count(); i++)
            //{
            //    int timeSplit = exams[i].Time.IndexOf("-");
            //    DateTime start = Convert.ToDateTime(exams[i].Time.Substring(0, timeSplit));
            //    DateTime end = Convert.ToDateTime(exams[i].Time.Substring(timeSplit));
            //    DateTime facTimeSet = end.AddMinutes(30);
            //    foreach (string proctor in facKeys)
            //    {
            //        if (faculty[proctor] == null)
            //        {
            //            faculty[proctor] = facTimeSet;
            //            exams[i].Proctor = proctor;
            //        }
            //        if (faculty[proctor] < start)
            //        {
            //            faculty[proctor] = facTimeSet;
            //            exams[i].Proctor = proctor;
            //        }
            //    }
            //}
            Debug.WriteLine(JsonConvert.SerializeObject(exams, Formatting.Indented));
            return null;
        }

        public bool DownloadFileFromPath(string path)
        {
            FileInfo downloadFile = new FileInfo(path);
            if (downloadFile.Exists)
            {
                Response.Clear();
                Response.ClearHeaders();
                Response.ClearContent();
                Response.AddHeader("content-disposition", "attachment; filename=" + downloadFile.Name);
                Response.AddHeader("Content-Type", "application/Excel");
                Response.ContentType = "application/vnd.xls";
                Response.AddHeader("Content-Length", downloadFile.Length.ToString());
                Response.WriteFile(downloadFile.FullName);
                Response.End();
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}