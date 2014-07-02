using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ELearningCrawler
{
    class CookieAwareWebClient : WebClient
    {
		public CookieContainer CookieContainer { get; set; }
	
        public CookieAwareWebClient()
        {
            CookieContainer = new CookieContainer();
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);
            var httpRequest = request as HttpWebRequest;

            if (httpRequest != null)
                httpRequest.CookieContainer = CookieContainer;

            return request;
        }
    }
}
