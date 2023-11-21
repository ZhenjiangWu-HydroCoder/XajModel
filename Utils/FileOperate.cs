using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public class FileOperate
    {
        /// <summary>
        /// 打开文件流格式的文件(csv/txt)
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static DataTable OpenFile(string path)
        {
            DataTable dt = new DataTable();
            FileStream fs;
            StreamReader sR;
            try
            {
                fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
                sR = new StreamReader(fs, Encoding.UTF8);
            }
            catch
            {
                Console.WriteLine(string.Format("[{0}] 文件打开失败，请关闭已打开的文件！", path));
                return null;
            }
            try
            {
                string firstLine = sR.ReadLine();
                string[] colName = firstLine.Split(new char[] { ' ', ',', '\t' });
                foreach (var i in colName)
                {
                    DataColumn col = dt.Columns.Add(i.ToString(), typeof(string));
                }
                string nextLine;
                while ((nextLine = sR.ReadLine()) != null)
                {
                    string[] every_row = nextLine.Split(new char[] { ',', ' ', '\t' }); ;
                    DataRow dr = dt.NewRow();
                    for (int i = 0; i < dt.Columns.Count; i++)
                    {
                        if (every_row[i] == "")
                        {
                            every_row[i] = "0";
                        }
                        dr[i] = every_row[i];
                    }
                    dt.Rows.Add(dr);
                }
                sR.Close();
                return dt;
            }
            catch
            {
                return null;
            }
        }
    }
}
