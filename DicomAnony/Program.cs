using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Dicom;
using Excel = Microsoft.Office.Interop.Excel;

namespace DICOM
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            //string path = "/Users/cskim/OneDrive - SNU/Medical";
            string basepath = "/Users/cskim/Documents/dataset";
            string[] files = Directory.GetFiles(basepath, "*.dcm", SearchOption.AllDirectories);
            List<DicomFile> dicoms = files.Select(file => {
                FileInfo info = new FileInfo(file);
                DicomFile dicom = DicomFile.Open(info.FullName);

                string ID = dicom.Dataset.GetValue<string>(DicomTag.PatientID, 0);
                string name = dicom.Dataset.GetValue<string>(DicomTag.PatientName, 0);
                string age = dicom.Dataset.GetValue<string>(DicomTag.PatientAge, 0);
                string gender = dicom.Dataset.GetValue<string>(DicomTag.PatientSex, 0);
                string birth = dicom.Dataset.GetValue<string>(DicomTag.PatientBirthDate, 0);
                string study = dicom.Dataset.GetValue<string>(DicomTag.StudyDate, 0);

                Console.WriteLine($"{ID}, {name}, {age}, {gender}, {birth}, {study}, {info.FullName}");

                return dicom;
            }).ToList();

            //// Excel
            //Excel.Application app = new Excel.Application();
            //Excel.Workbook wb = app.Workbooks.Add();
            //Excel.Worksheet ws = (Excel.Worksheet)wb.Worksheets.Item["Sheet1"];

            //// Worksheet Header
            //string[] header = { "ID", "Name", "Age", "Gender", "BirthDate", "StudyDate", "Path" };
            //for (int col = 1; col < header.Length; col++)
            //{
            //    ws.Cells[1, col] = header[col];
            //}

            //// Read File. Get Dicom Object
            //int row = 2;
            //List<DicomFile> dicoms = files.Select(file => {
            //    FileInfo info = new FileInfo(file);
            //    DicomFile dicom = DicomFile.Open(info.FullName);

            //    string ID = dicom.Dataset.GetValue<string>(DicomTag.PatientID, 0);
            //    string name = dicom.Dataset.GetValue<string>(DicomTag.PatientName, 0);
            //    string age = dicom.Dataset.GetValue<string>(DicomTag.PatientAge, 0);
            //    string gender = dicom.Dataset.GetValue<string>(DicomTag.PatientSex, 0);
            //    string birth = dicom.Dataset.GetValue<string>(DicomTag.PatientBirthDate, 0);
            //    string study = dicom.Dataset.GetValue<string>(DicomTag.StudyDate, 0);

            //    ws.Cells[row, 1] = ID;
            //    ws.Cells[row, 2] = name;
            //    ws.Cells[row, 3] = age;
            //    ws.Cells[row, 4] = gender;
            //    ws.Cells[row, 5] = birth;
            //    ws.Cells[row, 6] = study;
            //    ws.Cells[row, 7] = info.FullName;
            //    row++;

            //    // Anonymous ID, Name
            //    dicom.Dataset.AddOrUpdate(DicomTag.PatientID, $"AN_ID_{birth}");
            //    dicom.Dataset.AddOrUpdate(DicomTag.PatientName, $"AN_Nm_{birth}");

            //    return dicom;
            //}).ToList();

            //wb.SaveAs("Anonym");
            //wb.Close();
        }
    }
}
