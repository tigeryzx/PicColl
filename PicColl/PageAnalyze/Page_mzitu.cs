using System;
using System.Collections.Generic;
using System.Text;
using PicColl.DBContext.Model;
using PicColl.PageAnalyze.Model;
using System.Linq;
using System.IO;

namespace PicColl.PageAnalyze
{
    public class Page_mzitu : PageAnalyzeBase
    {
        private List<LinkMenu> allMenu;

        public Page_mzitu(string url, int startPageIndex, int? endPageIndex)
            : base(url, startPageIndex, endPageIndex)
        {
            this.HandlerMainPage(url, startPageIndex);
        }

        protected void HandlerMainPage(string mainUrl,int startPageIndex)
        {
            var allPicListLink = this.GetPageContext(mainUrl).QuerySelectorAll("p.url a");

            this.allMenu = allPicListLink.Select(x => new LinkMenu()
            {
                Name = x.TextContent,
                Url = x.GetAttribute("href")
            }).ToList();

            this.EndPageIndex = allMenu.Count;
            var currentMenu = this.allMenu[startPageIndex - 1];
            this.PagerInfo.PageUrl = currentMenu.Url;
            this.PagerInfo.PageIndex = startPageIndex;
        }

        protected override PageLinkInfo HandeNext(PageContentInfo pageContentInfo)
        {
            this.PagerInfo.PageIndex += 1;
            var menu = allMenu[this.PagerInfo.PageIndex -1];
            
            return new PageLinkInfo()
            {
                PageUrl = menu.Url,
                PageIndex = this.PagerInfo.PageIndex
            };
        }

        protected override List<PicInfo> HandePageImageLink(PageContentInfo pageContentInfo)
        {
            var imagePage = this.GetPageContext(pageContentInfo.Url);
            var totalPageEl = imagePage.QuerySelector("span[class=dots]").NextElementSibling;
            var totalImageIndex = 0;
            if (totalPageEl != null)
                totalImageIndex = Convert.ToInt32(totalPageEl.TextContent);

            var mainImageEl = imagePage.QuerySelector(".main-image img");
            var mainImageUrl = mainImageEl.GetAttribute("src");

            List<PicInfo> picInfos = new List<PicInfo>();

            for (var i = 1; i <= totalImageIndex; i++)
            {
                var imgUrl = mainImageUrl.Replace("01.", i.ToString("00") + ".");

                picInfos.Add(new PicInfo()
                {
                    ImageUrl = imgUrl,
                    CreateDate = DateTime.Now,
                    ImageSiteUrl = pageContentInfo.Url,
                    SrcImageName = Path.GetFileName(imgUrl)
                });
            }

            return picInfos;
        }

        protected override PageLinkInfo HandePrev(PageContentInfo pageContentInfo)
        {
            this.PagerInfo.PageIndex -= 1;
            var menu = allMenu[this.PagerInfo.PageIndex -1];

            return new PageLinkInfo()
            {
                PageUrl = menu.Url,
                PageIndex = this.PagerInfo.PageIndex
            };
        }

        public class LinkMenu
        {
            public string Name { get; set; }

            public string Url { get; set; }
        }
    }
}
