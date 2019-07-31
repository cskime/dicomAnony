using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using Dicom;
using Excel = Microsoft.Office.Interop.Excel;

namespace DicomAnony
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            AnonymizedDicom anonimized = new AnonymizedDicom();
            anonimized.Anonimize();
        }
    }

    public class AnonymizedDicom
    {
        //private string filepath = "/Users/cskim/OneDrive - SNU/Medical";
        private string filepath = "/Users/cskim/Desktop/mcstest/dicomtest/dataset";
        private List<FileInfo> fInfos = new List<FileInfo>();

        public AnonymizedDicom()
        {
            string[] files = Directory.GetFiles(filepath, "*.dcm", SearchOption.AllDirectories);
            fInfos = files.Select(file => new FileInfo(file)).ToList();
        }

        public void Anonimize()
        {
            // Excel
            Excel.Application app = new Excel.Application();
            Excel.Workbook wb = app.Workbooks.Add();
            Excel.Worksheet ws = (Excel.Worksheet)wb.Worksheets.Item["Sheet1"];

            // Worksheet Header
            string[] header = { "Anony", "ID", "Name" };
            header = header.Select((head, index) =>
            {
                ws.Cells[1, index + 1] = head;
                return head;
            }).ToArray();


            bool isOverlap = false;  // 파일 덮어쓸지 따로 저장할지 결정하는 flag
            fInfos = fInfos.Select((info, index) =>
            {
                DicomFile dicom = DicomFile.Open(info.FullName);
                string ID = dicom.GetDicomValue(DicomTag.PatientID);
                string name = dicom.GetDicomValue(DicomTag.PatientName);
                string birth = dicom.GetDicomValue(DicomTag.PatientBirthDate);
                //string anony = hashing(ID, name);         
                string anony = CreateAnonymousCode(birth);

                ws.Cells[index + 2, 1] = anony;
                ws.Cells[index + 2, 2] = ID;
                ws.Cells[index + 2, 3] = name.DecodeKR();

                // 이름, ID 익명화
                dicom.Dataset.AddOrUpdate(DicomTag.PatientID, $"AN_ID_{anony}");
                dicom.Dataset.AddOrUpdate(DicomTag.PatientName, $"AN_NM_{anony}");

                // 플래그를 사용하여 덮어쓰거나 새로운 경로에 저장하거나 선택
                if (isOverlap)
                {
                    dicom.Save(info.FullName);
                }
                else
                {
                    string newpath = filepath + "/anonymous";
                    if (!Directory.Exists(newpath))
                        newpath = Directory.CreateDirectory(filepath + "/anonymous").FullName;
                    newpath += $"/AN_{anony}.dcm";
                    dicom.Save(newpath);
                }

                return info;
            }).ToList();
            // Excel column을 값 길이에 맞게 정렬
            ws.Range[ws.Cells[1, 1], ws.Cells[header.Length, fInfos.Count + 1]].EntireColumn.AutoFit();

            // Excel 파일을 저장
            try
            {
                wb.SaveAs("/Users/cskim/Desktop/Anonym.xlsx");
            }
            catch
            {
                wb.Close();
            }
            wb.Close();
        }

        // {birthday}{current time}으로 익명화
        private string CreateAnonymousCode(string arg)
        {
            return $"{arg}{DateTime.Now.ToString("hhmmss")}";
        }

        // ID와 Name을 이용한 HASH값으로 익명화
        private string hashing(string id, string name)
        {
            byte[] result;

            byte[] msg_buffer = new ASCIIEncoding().GetBytes(id);
            byte[] key_buffer = new ASCIIEncoding().GetBytes(name);

            HMACSHA1 h = new HMACSHA1(key_buffer);

            result = h.ComputeHash(msg_buffer);

            return Convert.ToBase64String(result);
        }
    }

    public static class MyExtensions
    {
        // 한글 디코딩
        public static string DecodeKR(this string name)
        {
            Encoding iso = Encoding.GetEncoding("ISO-8859-1");
            Decoder euckr = Encoding.GetEncoding(51949).GetDecoder();
            byte[] isoByte = iso.GetBytes(name);
            char[] decodename;
            int charCount = euckr.GetCharCount(isoByte, 0, isoByte.Length);
            decodename = new char[charCount];
            int charDecodedCount = euckr.GetChars(isoByte, 0, isoByte.Length, decodename, 0);
            return new string(decodename);
        }

        // Dicom Dataset에서 TAG에 해당하는 값 가져옴
        public static string GetDicomValue(this DicomFile dicom, DicomTag tag)
        {
            try
            {
                return dicom.Dataset.GetValue<string>(tag, 0);
            }
            catch
            {
                // 태그에 해당하는 값이 없을 떄 빈 string 반환
                return string.Empty;
            }
        }
    }
}
