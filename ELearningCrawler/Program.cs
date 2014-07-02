using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommandLine;

namespace ELearningCrawler
{
    class Program
    {
        static void Main(string[] args)
        {
            Options options = new Options();
            if (Parser.Default.ParseArguments(args, options))
            {
                if (string.IsNullOrEmpty(options.Password))
                {
                    Console.Write("Bitte Passwort für {0} eingeben: ", options.Username);
                    options.Password = ReadPassword();
                    Console.WriteLine();
                }


                Start(options);
            }
            else
            {
                CommandLine.Text.HelpText txt = CommandLine.Text.HelpText.AutoBuild(options);
                Console.WriteLine(txt.ToString());
            }

            Console.ReadLine();
        }

        static async void Start(Options options)
        {
            Crawler c = new Crawler();

            c.DestinationFolder = options.Destination;
            c.AlwaysOverwrite = options.AlwaysOverwrite;
            c.ShouldDownloadAll = options.DownloadAll;

            try
            {
                await c.LoginToELeraning(options.Username, options.Password);
                c.DownloadAll();
            }
            catch (System.Net.WebException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Fail! {0} ({1})", ex.Message, ex.Status);
            }
        }

        static string ReadPassword()
        {
            StringBuilder pwd = new StringBuilder();
            while (true)
            {
                ConsoleKeyInfo i = Console.ReadKey(true);
                if (i.Key == ConsoleKey.Enter)
                    return pwd.ToString();
                else
                    pwd.Append(i.KeyChar);
            }
        }
    }

    class Options
    {
        [Option('u', "user", Required = true, HelpText = "Anmeldename, in der Regel die K-Nummer.")]
        public string Username { get; set; }
        [Option('p', "password", Required = false, HelpText = "Dein Passwort. Achtung: Passwort wird mit HTTPS übertragen jedoch unverschlüsselt verarbeitet. Es wird jedoch so kurz wie möglich im Speicher gehalten.")]
        public string Password { get; set; }
        [Option('d', "destination", HelpText = "Ziel-Ordner für den Download")]
        public string Destination { get; set; }
        [Option('y', "y", DefaultValue = false, HelpText = "Vorhandene Dateien immer überschreiben.")]
        public bool AlwaysOverwrite { get; set; }
        [Option('a', "all", DefaultValue = false, HelpText = "Lädt alle Kurse in dennen du bist, auch versteckte. Normalerweise werden nur Kurse geladen die auf e-learning sichtbar sind.")]
        public bool DownloadAll { get; set; }
    }
}
