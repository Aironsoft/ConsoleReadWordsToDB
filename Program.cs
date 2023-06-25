using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ude;

namespace ConsoleReadWordsToDB
{
    internal class Program
    {
        static void Main(string[] args)
        {
            using (WordsDBContainer db = new WordsDBContainer())
            {
                if(!db.Database.Exists())
                {
                    Console.WriteLine("Database {0} not founded...", db.Database.Connection.Database);
                    Console.WriteLine("Trying to create database...");
                    db.Database.Create();
                    Console.WriteLine("Database {0} was created!", db.Database.Connection.Database);
                }
                else
                {
                    Console.WriteLine("Database {0} already exists.", db.Database.Connection.Database);
                }

                bool doneReadingWork = false;
                while(!doneReadingWork)
                {
                    doneReadingWork = startReadingWork(db);
                }

                Console.WriteLine("Closing the program...");
            }
        }


        static bool startReadingWork(WordsDBContainer db)
        {
            Console.WriteLine("\nPlease, enter address of the text file for reading (for example, d://words.txt) or 'exit' to end the work:");
            bool successfulRead = false;
            while (!successfulRead)
            {
                string txtFileAddress = Console.ReadLine(); // адрес файла
                txtFileAddress.Trim();

                if(txtFileAddress.ToLower() == "exit") // проверка на команду выхода
                {
                    return true;
                }

                if (File.Exists(txtFileAddress))
                {
                    string encoding = detectEncoding(txtFileAddress);
                    if (encoding != "UTF-8")
                    {
                        Console.WriteLine($"Please, change the file encoding ({encoding}) to UTF-8 and try again:");
                        continue;
                    }

                    successfulRead = true;

                    readingProcess(db, txtFileAddress);

                    // получение слов из БД
                    var words = db.Words;
                    foreach (Word w in words)
                        Console.WriteLine("{0}.{1} - {2}", w.Id, w.Value, w.Count);
                }
                else
                {
                    Console.WriteLine("File not found. Please, enter address again:");
                }
            }

            return false;
        }


        public static void readingProcess(WordsDBContainer db, string path)
        {
            using (StreamReader sr = new StreamReader(path, true))
            {
                Console.WriteLine("Start reading...");

                string s;
                s = sr.ReadToEnd().Replace("\r", "").Replace("\t", "").Replace("\n", " ");
                var stringWords = s.Split(' ') // опредеяем подходящие слова и количество каждого из них
                        .Where(x => x.Length >= 3 && x.Length <= 20)
                        .GroupBy(x => x)
                        .Where(x => x.Count() >= 4)
                        .Select(x => new Tuple<string, int>(x.Key, x.Count()));

                // добавление подходящих слов в БД
                foreach (var word in stringWords)
                {
                    Word w = db.Words.FirstOrDefault(x => x.Value == word.Item1);
                    if (w != null)
                    {
                        w.Count += word.Item2;
                    }
                    else
                    {
                        db.Words.Add(new Word { Value = word.Item1, Count = word.Item2 });
                    }
                }
                db.SaveChanges();

                Console.WriteLine("Words reading is done!");
            }
        }

        public static string detectEncoding(string path)
        {
            using (FileStream fs = File.OpenRead(path))
            {
                Ude.CharsetDetector cdet = new Ude.CharsetDetector();
                cdet.Feed(fs);
                cdet.DataEnd();
                return cdet.Charset;
            }
        }
    }
}
