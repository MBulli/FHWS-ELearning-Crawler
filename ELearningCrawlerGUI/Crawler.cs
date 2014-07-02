using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ELearningCrawlerGUI
{
    class Crawler
    {
        private CookieContainer cookies;

        public async Task<bool> LoginToELearning(string user, string password)
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
                HttpWebRequest req2 = CreateHttpWebRequest("GET", response.ResponseUri);

                using (HttpWebResponse response2 = (HttpWebResponse)(await req2.GetResponseAsync()))
                {
                    // nothing to do
                }
            }

            return true; // TODO: Login fail
        }

        [Obsolete]
        public async Task<CourseList> FetchCourses()
        {
            HtmlDocument doc = await HtmlDocumentFromUrl("https://elearning.fhws.de/my/");

            var courses = doc.DocumentNode.SelectNodes("//div[@class='box coursebox']/h3/a");

            if (courses == null || courses.Count == 0)
                return null;

            CourseList result = new CourseList();

            foreach (HtmlNode node in courses)
            {
                if (node.Attributes["title"] == null || string.IsNullOrEmpty(node.Attributes["title"].Value))
                    continue;
                if (node.Attributes["href"] == null || string.IsNullOrEmpty(node.Attributes["href"].Value))
                    continue;

                string courseName = node.Attributes["title"].Value;
                string courseLink = node.Attributes["href"].Value;

                Course course = new Course(courseName, courseLink);
                result.Add(course);
            }

            return result;
        }

        public async Task FetchCourses(ICollection<Course> result)
        {
            if (result == null)
                throw new ArgumentNullException("result");

            HtmlDocument doc = await HtmlDocumentFromUrl("https://elearning.fhws.de/my/");

            var courses = doc.DocumentNode.SelectNodes("//div[@class='box coursebox']/h3/a");

            if (courses == null || courses.Count == 0)
                throw new Exception("Konnte Kurse nich laden. (Falscher Login?)");

            foreach (HtmlNode node in courses)
            {
                if (node.Attributes["title"] == null || string.IsNullOrEmpty(node.Attributes["title"].Value))
                    continue;
                if (node.Attributes["href"] == null || string.IsNullOrEmpty(node.Attributes["href"].Value))
                    continue;

                string courseName = node.Attributes["title"].Value;
                string courseLink = node.Attributes["href"].Value;

                Course course = new Course(courseName, courseLink);
                result.Add(course);
            }
        }

        public async Task<CourseMaterialList> FetchMaterials(Course course)
        {
            if (course == null)
                throw new ArgumentNullException("course");

            HtmlDocument courseHtml = await HtmlDocumentFromUrl(course.Link);

            // Diskussionsforum überspringen (section-0)
            var sections = courseHtml.DocumentNode.SelectNodes("//li[starts-with(@id, 'section-')]").Where(n => n.Id != "section-0");

            if (sections == null || sections.Count() == 0)
                return null;

            CourseMaterialList result = new CourseMaterialList();

            foreach (var sec in sections)
            {
                var materials = sec.SelectNodes("div[@class='content']/ul/li/div/div/a");

                if (materials == null || materials.Count == 0)
                    continue;

                foreach (var mat in materials)
                {
                    string downloadLink = mat.Attributes["href"].Value;
                    downloadLink += "&redirect=1";

                    // skip folder links etc.
                    if (!downloadLink.StartsWith("https://elearning.fhws.de/mod/resource/"))
                        continue;
                    if (mat.SelectSingleNode("span[@class='instancename']") == null)
                        continue;

                    string title = mat.SelectSingleNode("span[@class='instancename']").FirstChild.InnerText;

                    CourseMaterial courseMaterial = new CourseMaterial(course, title, downloadLink);
                    result.Add(courseMaterial);
                }
            }

            return result;
        }

        public async Task DownloadMaterials(IEnumerable<Course> courses)
        {
            foreach (var course in courses)
            {
                foreach (var mat in course.Materials)
                {
                    HttpWebRequest req = CreateHttpWebRequest("GET", mat.DownloadLink);

                    using (WebResponse response = await req.GetResponseAsync())
                    {
                        string fileName = Path.GetFileName(response.ResponseUri.GetComponents(UriComponents.Path, UriFormat.Unescaped));

                        using (Stream source = response.GetResponseStream())
                        using (MemoryStream mem = new MemoryStream())
                        {
                            await source.CopyToAsync(mem);

                            string dest = string.Empty; // TODO
                            File.WriteAllBytes(Path.Combine(dest, fileName), mem.ToArray());
                        }
                    }   
                }
            }
        }

        #region Web helper
        private WebClient CreateWebclient()
        {
            CookieAwareWebClient wc = new CookieAwareWebClient();
            wc.CookieContainer = cookies;
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
        #endregion
    }
}
