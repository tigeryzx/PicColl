using AngleSharp;
using AngleSharp.Dom;
using PicColl.DBContext.Model;
using PicColl.PageAnalyze.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PicColl.PageAnalyze
{
    public abstract class PageAnalyzeBase
    {
        public PageAnalyzeBase(string url) : this(url, 1)
        {

        }

        public PageAnalyzeBase(string url, int startPageIndex) : this(url,startPageIndex, null)
        {

        }

        public PageAnalyzeBase(string url, int startPageIndex, int? endPageIndex)
        {
            this.StartPageIndex = startPageIndex;
            this.EndPageIndex = endPageIndex;

            // 加载浏览器配置
            var config = Configuration.Default.WithDefaultLoader();
            
            this._BaseBrowsingContext = BrowsingContext.New(config);

            // 加载初始页
            this.PagerInfo = new PageLinkInfo()
            {
                PageIndex = this.StartPageIndex,
                PageUrl = url
            };
        }

        private IBrowsingContext _BaseBrowsingContext;

        /// <summary>
        /// 最大页码
        /// </summary>
        public int? EndPageIndex { get; set; }

        /// <summary>
        /// 起始页码
        /// </summary>
        public int StartPageIndex { get; set; }

        /// <summary>
        /// 分页信息
        /// </summary>
        public PageLinkInfo PagerInfo { get; set; }

        /// <summary>
        /// 当前页内容
        /// </summary>
        protected PageContentInfo CurrentPageContext { get; set; }

        /// <summary>
        /// 获取页面上下文
        /// </summary>
        protected virtual IDocument GetPageContext(string url)
        {
            // 设置配置以支持文档加载
            var document = _BaseBrowsingContext.OpenAsync(url);
            return document.Result;
        }

        /// <summary>
        /// 获取当前页所有图片链接
        /// </summary>
        /// <returns></returns>
        public virtual List<PicInfo> GetCurrentPageImageLinks()
        {
            if(this.PagerInfo == null)
            {
                Console.WriteLine("未有要处理的页信息");
                return null;
            }

            if (this.PagerInfo.PageIndex > this.EndPageIndex)
            {
                Console.WriteLine("当前页已大于当前设定的最大限制{0}", this.EndPageIndex);
                return null;
            }

            if(this.PagerInfo.PageIndex < 0)
            {
                Console.WriteLine("当前页已少于0");
                return null;
            }

            if (string.IsNullOrEmpty(this.PagerInfo.PageUrl))
            {
                Console.WriteLine("要处理的URL为空");
                return null;
            }

            var pageContentInfo = new PageContentInfo();
            pageContentInfo.Url = this.PagerInfo.PageUrl;
            pageContentInfo.Content = HttpUitls.Get(this.PagerInfo.PageUrl);
            this.CurrentPageContext = pageContentInfo;

            return HandePageImageLink(pageContentInfo);
        }

        /// <summary>
        /// 处理页面图片链接
        /// </summary>
        /// <returns></returns>
        protected abstract List<PicInfo> HandePageImageLink(PageContentInfo pageContentInfo);

        public bool Next()
        {
            this.PagerInfo = HandeNext(this.CurrentPageContext);
            return this.PagerInfo != null;
        }

        public bool Prev()
        {
            this.PagerInfo = HandePrev(this.CurrentPageContext);
            return this.PagerInfo != null;
        }

        protected abstract PageLinkInfo HandeNext(PageContentInfo pageContentInfo);

        protected abstract PageLinkInfo HandePrev(PageContentInfo pageContentInfo);

    }
}
