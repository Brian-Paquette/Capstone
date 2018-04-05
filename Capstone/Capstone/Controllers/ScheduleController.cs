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
            Debug.WriteLine(JsonConvert.SerializeObject(sheetData, Formatting.Indented));
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