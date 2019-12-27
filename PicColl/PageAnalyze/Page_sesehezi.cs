using AngleSharp;
using AngleSharp.Dom;
using PicColl.PageAnalyze.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using PicColl.DBContext.Model;

namespace PicColl.PageAnalyze
{
    public class Page_sesehezi : PageAnalyzeBase
    {
        public Page_sesehezi(string url, int startPageIndex, int? endPageIndex)
            : base(url, startPageIndex, endPageIndex)
        {

        }

        protected PageLinkInfo GetPageLinkInfo(PageContentInfo pageContentInfo, string className)
        {
            var url = pageContentInfo.Url;
            var splitIndex = url.LastIndexOf("=") + 1;
            var pageIndex = url.Substring(splitIndex, url.Length - splitIndex);
            var numPageIndex = Convert.ToInt32(pageIndex);

            var totalPage = GetPageJsonData(pageContentInfo)["data"]["pages"];

            if (className == ".next")
                numPageIndex++;
            else if (className == ".prev")
                numPageIndex--;

            if (numPageIndex > Convert.ToInt32(totalPage) || numPageIndex <= 0)
                return null;

            return new PageLinkInfo()
            {
                PageUrl = string.Format("http://www.sesehezi.com/api/v1.1/?page={0}", numPageIndex),
                PageIndex = numPageIndex
            };
        }

        protected override PageLinkInfo HandeNext(PageContentInfo pageContentInfo)
        {
            return GetPageLinkInfo(pageContentInfo, ".next");
        }

        protected override PageLinkInfo HandePrev(PageContentInfo pageContentInfo)
        {
            return GetPageLinkInfo(pageContentInfo, ".prev");
        }

        private JObject GetPageJsonData(PageContentInfo pageContentInfo)
        {
            JObject json = (JObject)JsonConvert.DeserializeObject(pageContentInfo.Content);
            return json;
        }

        protected override List<PicInfo> HandePageImageLink(PageContentInfo pageContentInfo)
        {
            var imgItem = (JArray)GetPageJsonData(pageContentInfo)["data"]["items"];

            var contentLinkUrls = new List<string>();
            foreach (var img in imgItem)
            {
                var shorturl = img["shorturl"].ToString().Replace(@"u/",string.Empty);
                var fullContentLink = string.Format("http://www.sesehezi.com/photo/{0}", shorturl);
                contentLinkUrls.Add(fullContentLink);
            }

            Console.WriteLine(string.Format("Find {0} Urls", contentLinkUrls.Count));

            var imgLinkUrls = new List<PicInfo>();
            foreach (var contentLink in contentLinkUrls)
            {
                Console.WriteLine(string.Format("open:{0}", contentLink));
                var contentPageDoc = GetPageContext(contentLink);

                if (contentPageDoc == null)
                    continue;

                var mainImagePlan = contentPageDoc.QuerySelector(".main-image");
                if (mainImagePlan == null)
                    continue;
                var bigPicTags = mainImagePlan.QuerySelectorAll("img");
                foreach (var bigPicTag in bigPicTags)
                {
                    var imgUrl = bigPicTag.GetAttribute("src");
                    Console.WriteLine(string.Format("img:{0}", imgUrl));
                    imgLinkUrls.Add(new PicInfo()
                    {
                        ImageUrl = imgUrl,
                        CreateDate = DateTime.Now,
                        ImageSiteUrl = contentLink,
                        SrcImageName = Path.GetFileName(imgUrl)
                    });
                }
                Console.WriteLine("End.");

            }
            return imgLinkUrls;
        }

    }
}
