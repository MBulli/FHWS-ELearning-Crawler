using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ELearningCrawler
{
    class Crawler
    {
        private static readonly Regex IllegalPathCharactersRegEx;
        private static readonly Regex FolderContentURLRegEx;

        CookieContainer cookies;
        int courseCount;
        int currentCourses;

        public bool AlwaysOverwrite { get; set; }
        public bool ShouldDownloadAll { get; set; }
        public string DestinationFolder { get; set; }

        public void DownloadAll()
        {
            ParseCourses();
        }

        static Crawler()
        {
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            IllegalPathCharactersRegEx = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)), RegexOptions.Compiled);

            string url = Regex.Escape(@"https://elearning.fhws.de/pluginfile.php/xXx/mod_folder/content/");
            url = url.Replace("xXx", @"\d+");
            FolderContentURLRegEx = new Regex(url, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        public async Task LoginToELeraning(string user, string password)
        {
            cookies = new CookieContainer();

            HttpWebRequest req = CreateHttpWebRequest("POST", "https://elearning.fhws.de/login/index.php");
            req.ContentType = "application/x-www-form-urlencoded";

            using (StreamWriter sw = new StreamWriter(req.GetRequestStream()))
            {
                sw.Write(string.Format("username={0}&password={1}", user, password));
            }

            using (HttpWebResponse response = (HttpWebResponse)(await req.GetResponseAsync()))
            {
                if (response.StatusCode != HttpStatusCode.OK)
                    throw new WebException(response.StatusDescription);

                using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                {
                    HtmlDocument doc = new HtmlDocument();
                    doc.Load(sr);

                    var errNode = doc.DocumentNode.SelectSingleNode("//span[@class='error']");

                    if (errNode != null)
                    {
                        string msg = errNode.InnerText;
                        throw new WebException(msg);
                    }
                }

                HttpWebRequest req2 = CreateHttpWebRequest("GET", response.ResponseUri);

                using (HttpWebResponse response2 = (HttpWebResponse)(await req2.GetResponseAsync()))
                {
                    if (response2.StatusCode != HttpStatusCode.OK)
                        throw new WebException(response.StatusDescription);    

                }
            }
        }

        private static void ConsoleWriteLine(ConsoleColor color, string line, params object[] arg)
        {
            ConsoleColor c = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(line, arg);
            Console.ForegroundColor = c;
        }

        private WebClient CreateWebclient()
        {
            CookieAwareWebClient wc = new CookieAwareWebClient();
            wc.CookieContainer = cookies;
            wc.Encoding = Encoding.UTF8;

            return wc;
        }

        private HttpWebRequest CreateHttpWebRequest(string method, string url)
        {
            return CreateHttpWebRequest(method, new Uri(url));
        }

        private HttpWebRequest CreateHttpWebRequest(string method, Uri uri)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.CreateHttp(uri);
            req.Method = string.IsNullOrEmpty(method) ? "GET" : method;
            req.CookieContainer = cookies;
            
            return req;
        }

        private async Task<HtmlDocument> HtmlDocumentFromUrl(string url)
        {
            using (WebClient client = CreateWebclient())
            {
                string htmlString = await client.DownloadStringTaskAsync(url);
                HtmlDocument doc = new HtmlDocument();

                using (StringReader sr = new StringReader(htmlString))
                {
                    doc.Load(sr);

                    return doc;
                }
            }
        }

        private async void ParseCourses()
        {
            HtmlDocument doc = await HtmlDocumentFromUrl("https://elearning.fhws.de/my/");

            HtmlNodeCollection courses = null;

            if (this.ShouldDownloadAll)
            {
                courses = doc.DocumentNode.SelectNodes("//div[@class='coc-course' or @class='coc-course coc-hidden']/div/div/h3/a");
            }
            else
            {
                courses = doc.DocumentNode.SelectNodes("//div[@class='coc-course']/div/div/h3/a");
            }

            if (courses == null || courses.Count == 0)
                return;

            courseCount = courses.Count;

            foreach (HtmlNode node in courses)
            {
                Task t = new Task(async () =>
                {
                    if (node.Attributes["title"] == null || string.IsNullOrEmpty(node.Attributes["title"].Value))
                        return;
                    if (node.Attributes["href"] == null || string.IsNullOrEmpty(node.Attributes["href"].Value))
                        return;

                    string courseName = node.Attributes["title"].Value;
                    string courseLink = node.Attributes["href"].Value;

                    Console.WriteLine("Found course '{0}' follow link: {1}", courseName, node.Attributes["href"].Value);

                    HtmlDocument course = await HtmlDocumentFromUrl(courseLink);

                    string destFolder = courseName;
                    if (!string.IsNullOrEmpty(this.DestinationFolder))
                        Path.Combine(this.DestinationFolder, courseName);

                    Directory.CreateDirectory(destFolder);

                    await ParseCourse(course, destFolder);

                    ConsoleWriteLine(ConsoleColor.Green, "Finished {0}/{1} courses.", ++currentCourses, courseCount);
                });
                t.Start();
            }
        }

        private async Task ParseCourse(HtmlDocument course, string dest)
        {
            var sections = course.DocumentNode.SelectNodes("//li[starts-with(@id, 'section-')]").Where(n => n.Id != "section-0");

            if (sections == null || sections.Count() == 0)
                return;

            foreach (var sec in sections)
            {
                var materials = sec.SelectNodes("div[@class='content']/ul/li/div/div/a");

                if (materials == null || materials.Count == 0)
                    continue;

                foreach (var mat in materials)
                {
                    string downloadLink = mat.Attributes["href"].Value;
                    downloadLink += "&redirect=1";

                    string title = mat.SelectSingleNode("span[@class='instancename']").FirstChild.InnerText;

                    if (downloadLink.StartsWith("https://elearning.fhws.de/mod/folder/"))
                    {
                        // if folder
                        string folderName = Path.Combine(dest, EliminateInvalidCharactersFromFilename(title));
                        Directory.CreateDirectory(folderName);

                        HtmlDocument folderDoc = await HtmlDocumentFromUrl(downloadLink);

                        await DownloadMaterialFolder(folderDoc, folderName);
                    } 
                    else if (downloadLink.StartsWith("https://elearning.fhws.de/mod/resource/"))
                    {
                        // default file download
                        await DownloadMaterial(downloadLink, dest);
                    }
                    else
                    {
                        // skip links etc.
                        continue;
                    }
                }
            }
        }

        private async Task DownloadMaterialFolder(HtmlDocument folderDoc, string dest)
        {
            var links = folderDoc.DocumentNode.SelectNodes("//div[@class='filemanager']//span[@class='fp-filename-icon']/a");

            foreach (HtmlNode link  in links)
            {
                if (link.Attributes["href"] == null || string.IsNullOrEmpty(link.Attributes["href"].Value))
                    continue;

                string downloadLink = link.Attributes["href"].Value;

                if (!FolderContentURLRegEx.IsMatch(downloadLink))
                    continue;

                await DownloadMaterial(downloadLink, dest);
            }
        }

        private async Task DownloadMaterial(string downloadLink, string dest)
        {
            HttpWebRequest req = CreateHttpWebRequest("GET", downloadLink);

            using (WebResponse response = await req.GetResponseAsync())
            {
                string fileName = Path.GetFileName(response.ResponseUri.GetComponents(UriComponents.Path, UriFormat.Unescaped));
                fileName = EliminateInvalidCharactersFromFilename(fileName);
                fileName = Path.Combine(dest, fileName);

                if (this.AlwaysOverwrite || !File.Exists(fileName))
                {
                    Console.WriteLine("Download material '{0}' ({1})", fileName, GetBytesReadable(response.ContentLength));

                    using (Stream source = response.GetResponseStream())
                    using (MemoryStream mem = new MemoryStream())
                    {
                        await source.CopyToAsync(mem);
                        File.WriteAllBytes(fileName, mem.ToArray());
                    }
                }
                else
                {
                    ConsoleWriteLine(ConsoleColor.DarkGray, "Skipped file: {0}", Path.GetFileName(fileName));
                }
            }
        }

        private static string EliminateInvalidCharactersFromFilename(string path)
        {
            return IllegalPathCharactersRegEx.Replace(path, "");
        }

        public static string GetBytesReadable(long i)
        {
            // see: http://www.somacon.com/p576.php
            string sign = (i < 0 ? "-" : "");
            double readable = (i < 0 ? -i : i);
            string suffix;

            if (i >= 0x40000000) // Gigabyte
            {
                suffix = "GB";
                readable = (double)(i >> 20);
            }
            else if (i >= 0x100000) // Megabyte
            {
                suffix = "MB";
                readable = (double)(i >> 10);
            }
            else if (i >= 0x400) // Kilobyte
            {
                suffix = "KB";
                readable = (double)i;
            }
            else
            {
                return i.ToString(sign + "0 B"); // Byte
            }
            readable /= 1024;

            return sign + readable.ToString("0.### ") + suffix;
        }
    }
}
