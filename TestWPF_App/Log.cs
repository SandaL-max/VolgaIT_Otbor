using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Globalization;
using System.Windows;
using System.IO;

namespace VolgaIT_Otbor
{
    class Log
    {
        public static string sBasePath = System.AppDomain.CurrentDomain.BaseDirectory;
        public static string logFile = sBasePath + "logfile.txt";


        public static string getCurDateTime()
        {
            DateTime dtG = DateTime.Now;
            string curdate = dtG.ToString("G", DateTimeFormatInfo.InvariantInfo);
            return curdate;
        }

        public static void LogExceptionMSG(string class_name, string function_name, string fail_msg, bool alert)
        {
            string curdate = getCurDateTime();
            string errmsg = curdate + " | " + class_name + ":" + function_name + " | Exception: " + fail_msg;

            File.AppendAllText(logFile, errmsg + Environment.NewLine);

            if (alert)
            {
                MessageBox.Show(errmsg);
            }
            
        }


    }
}
