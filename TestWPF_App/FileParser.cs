using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace VolgaIT_Otbor
{
    class FileParser
    {
        public static string sBasePath = System.AppDomain.CurrentDomain.BaseDirectory;
        public static string ProjectsPath = sBasePath + "content\\";
        //public List<string> lWords = new List<string>();
        List<Tuple<int, string>> lWordsStats = new List<Tuple<int, string>>();

        public int Parse(ref DBManagement db, int bufsize, string projectid, string folder, string fileid, string filename)
        {
            try
            {
                string FilePath = ProjectsPath + folder + "\\" + filename;
                int words_count = 0;
                if (File.Exists(FilePath))
                {
                    int count = 0;
                    string sql = "";
                    string word = "";
                    db.Words_Prepare(fileid); // чистим базу от предыдущих сканирований данного файла

                    Int32 BufferSize = bufsize; //128;
                    using (var fileStream = File.OpenRead(FilePath))
                    using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
                    {
                        String line, nohtml;
                        string[] words;
                        string[] separators = { " ", ",", ".", "!", "?", "\"", ";", ":", "[", "]", "(", ")", "\n", "\r", "\t" };
                        while ((line = streamReader.ReadLine()) != null)
                        {
                            // удалить html теги, текст переводим в нижний регистр, чтобы избежать дублирования слов.
                            nohtml = RemoveHTMLTagsCompiled(line).ToLower();

                            // получить слова
                            words = nohtml.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                            words_count += words.Length;

                            // считаем повторяющиеся слова
                            lWordsStats = words.GroupBy(x => x)
                                              .Where(x => x.Count() > 0)
                                              .Select(x => Tuple.Create( x.Count(), x.Key )).ToList();

                            // обновляем статистику в БД | lWordsStats.Item1 - количество | lWordsStats.Item2 - слово
                            sql = "";
                            foreach (var item in lWordsStats)
                            {
                                //db.Word_Add(projectid, fileid, item.Item2, item.Item1);
                                word = item.Item2.Replace("'", "''");
                                count = item.Item1;
                                sql += "INSERT INTO words (id, projectid, fileid, word, count) " +
                                                    "VALUES ((SELECT id FROM words WHERE projectid = " + projectid + " AND fileid = " + fileid + " AND word = '" + word + "'), " + projectid + ", " + fileid + ", '" + word + "', " + Convert.ToString(count) + ")" +
                                                    "ON CONFLICT (id) DO " +
                                                    "UPDATE SET count = count + " + Convert.ToString(count) + ";";
                            }
                            db.SQLite_RunCommand(sql);
                        }
                    }
                    return words_count;
                }
                else
                {                    
                    Log.LogExceptionMSG("FileParser", "Parse", "Project: '" + folder + "' | File: '" + FilePath + "' not found and removed from DB.", false);
                    return -1;
                }
            }
            catch (Exception fail)
            {
                Log.LogExceptionMSG("FileParser", "Parse", fail.Message, true);
                return -1;
            }
        }

        static Regex htmlRegex = new Regex("<.*?>|&.*?;", RegexOptions.Compiled);

        public static string RemoveHTMLTagsCompiled(string html)
        {
            html = html.Replace("&lsquo;", "'");
            html = html.Replace("&rsquo;", "'");
            html = html.Replace("&amp;", "&");
            //html = html.Replace("&quot;", "\"");
            html = html.Replace("&mdash;", "-");
            
            return htmlRegex.Replace(html, " "); //string.Empty
        }




    }
}
