using Microsoft.EntityFrameworkCore;
using PicColl.DBContext;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;
using PicColl.DBContext.Common;
using PicColl.DBContext.Model;
using PicColl.PageAnalyze;
using PicColl.PageAnalyze.Pipe;
using PicColl.PageAnalyze.Model;
using AutoMapper;
using System.Threading;

namespace PicColl
{
    public class PicDownloadMachine
    {
        public PicDBContext _DB;

        private List<IPicPipe> _PicPipeList = new List<IPicPipe>();

        private IMapper _Mapper;

        public List<IPicPipe> PicPipeList
        {
            get
            {
                return _PicPipeList;
            }
        }

        private ImageDownloadConfig _ImageDownloadConfig = new ImageDownloadConfig();

        public ImageDownloadConfig ImageDownloadConfig
        {
            get
            {
                return _ImageDownloadConfig;
            }
        }

        public PicDownloadMachine()
        {
            this.Init();
        }

        public void Init()
        {
            _DB = new PicDBContext();
            // 自动迁移数据库
            _DB.Database.Migrate();

            var config = new MapperConfiguration(cfg => 
            {
                cfg.CreateMap<PicDto, PicInfo>();
                cfg.CreateMap<PicInfo, PicDto>();
            });

            this._Mapper = config.CreateMapper();

            // 默认配置
            _ImageDownloadConfig.WaitNumber = 4;
            _ImageDownloadConfig.WaitSecond = 1;
        }

        /// <summary>
        /// 清除失败图片
        /// </summary>
        public void ClearFailedPic()
        {
            Console.WriteLine("要查找重新尝试下载几次以上的无效图片信息?(默认0)");
            var reDownTime = Console.ReadLine();
            if (string.IsNullOrEmpty(reDownTime))
                reDownTime = "0";
            var nReDownTime = Convert.ToInt32(reDownTime);
            var failedPicList = _DB.PicInfo.Where(x => x.IsSuccess == false && x.ReDownTime >= nReDownTime);
            var failedItemCount = failedPicList.Count();
            Console.WriteLine(string.Format("找到{0}个符合条件的无效图片信息，是否删除[Y/N]？", failedItemCount));
            var isDelete = Console.ReadLine().ToUpper() == "Y";
            if (isDelete)
            {
                foreach (var picInfo in failedPicList)
                {
                    var imageLocalPath = picInfo.ImageLocalPath;
                    if (File.Exists(imageLocalPath))
                        File.Delete(imageLocalPath);
                    _DB.PicInfo.Remove(picInfo);
                    Console.WriteLine("Deleted: {0}", imageLocalPath);
                }
                _DB.SaveChanges();
                Console.WriteLine("Total Delete {0} Item", failedItemCount);
            }
        }

        /// <summary>
        /// 分析重复图片
        /// </summary>
        public void AnalyzeRepeat()
        {
            Console.WriteLine("扫描全库，若数据量大则需要较长时间是否继续？选择N则直接进行重复图片处理。[Y/N]");

            var isHande = Console.ReadLine().ToUpper() == "Y";
            if (isHande)
            {
                Console.WriteLine("加载数据中，请稍候……");

                var allPicInfoList = _DB.PicInfo
                    .Where(x => x.IsRepeat != true)
                    .Select(x => new SearchParis()
                    {
                        Id = x.Id,
                        Value = x.ImageUrl,
                        Tag = x.LocalImageName
                    })
                    .ToHashSet();

                foreach (var picinfo in allPicInfoList)
                {
                    Console.WriteLine("Process {0}", picinfo.Tag);

                    var repeatPicList = allPicInfoList
                        .Where(x => x.Value == picinfo.Value && x.Id != picinfo.Id && x.IsHande != true)
                        .ToList();

                    if (repeatPicList != null && repeatPicList.Count() > 0)
                    {
                        repeatPicList.ForEach(x =>
                        {
                            x.IsHande = true;
                            var dbPicInfo = _DB.PicInfo.Single(y => y.Id == x.Id);
                            dbPicInfo.IsRepeat = true;
                            Console.WriteLine(string.Format("★ Mark Repeat {0}", x.Tag));
                        });
                        _DB.SaveChanges();
                    }

                }
            }

            var repeatItem = _DB.PicInfo.Count(x => x.IsRepeat == true);
            if (repeatItem <= 0)
            {
                Console.WriteLine(string.Format("No Repeat Item."));
                return;
            }

            Console.WriteLine(string.Format("Find {0} Repeat Item.", repeatItem));
            Console.WriteLine("Delete is [Y/N]?");
            var isDelete = Console.ReadLine().ToUpper() == "Y";
            if (isDelete)
            {
                var allRepeatItem = _DB.PicInfo.Where(x => x.IsRepeat == true);
                foreach (var picInfo in allRepeatItem)
                {
                    var imageLocalPath = picInfo.ImageLocalPath;
                    if (File.Exists(imageLocalPath))
                        File.Delete(imageLocalPath);
                    _DB.PicInfo.Remove(picInfo);
                    Console.WriteLine("Deleted: {0}", imageLocalPath);
                }
                _DB.SaveChanges();
                Console.WriteLine("Total Delete {0} Item", repeatItem);
            }

        }

        /// <summary>
        /// 重新下载失败图片
        /// </summary>
        public void ReplacePicFormDb()
        {
            Console.WriteLine("请输无效文件保存路径：");
            var faildSavePath = Console.ReadLine();
            //var faildSavePath = @"C:\Users\Administrator\Desktop\f";

            // 只重试少于一次失败的
            var faildPicList = _DB.PicInfo.Where(x => x.IsSuccess == false
            && (x.ReDownTime < 1 || !x.ReDownTime.HasValue))
            .ToList();
            int successCount = 0;
            int faildCount = 0;
            var memoryCount = 0;

            var total = faildPicList.Count;

            foreach (var picInfo in faildPicList)
            {
                var currentIndex = faildPicList.IndexOf(picInfo);
                Console.WriteLine("proc:{0}", picInfo.ImageUrl);

                picInfo.LastDate = DateTime.Now;
                var picDto = this._Mapper.Map<PicDto>(picInfo);
                if (this.Download(picDto, picInfo.ImageLocalPath, currentIndex))
                {
                    Console.WriteLine($"★success [{currentIndex + 1 }/{total}]:{0}", picInfo.ImageLocalPath);
                    successCount++;
                }
                else
                {
                    var savefaildPath = Path.Combine(faildSavePath, picInfo.LocalImageName);
                    Console.WriteLine("●filed:{0}", savefaildPath);
                    if (File.Exists(picInfo.ImageLocalPath))
                    {
                        if (!picInfo.ReDownTime.HasValue)
                            picInfo.ReDownTime = 1;
                        else
                            picInfo.ReDownTime = picInfo.ReDownTime + 1;

                        File.Move(picInfo.ImageLocalPath, savefaildPath);
                        picInfo.ImageLocalPath = savefaildPath;
                        faildCount++;
                    }
                }
                memoryCount++;
                if (memoryCount == 100)
                {
                    _DB.SaveChanges();
                    memoryCount = 0;
                    Console.WriteLine("Save......");
                }
            }

            _DB.SaveChanges();
            Console.WriteLine("Last Save......");

            Console.WriteLine("Total Success {0} Faild {1}", successCount, faildCount);
        }



        private PageAnalyzeBase SelectedAnalyze(int startPage, int? endPage)
        {
            PageAnalyzeBase selected = null;

            do
            {
                Console.WriteLine("请选择分析器:");
                Console.WriteLine("1:Page_sesehezi");
                Console.WriteLine("2:Page_mzitu");

                var id = Console.ReadLine();

                if (id == "1")
                {
                    var rootUrl = "http://www.sesehezi.com/api/v1.1/?page=" + startPage;
                    selected = new Page_sesehezi(rootUrl, startPage, endPage);
                }
                else if (id == "2")
                {
                    var rootUrl = "https://www.mzitu.com/all/";
                    selected = new Page_mzitu(rootUrl, startPage, endPage);
                };
            } while (selected == null);
            return selected;
        }

        public void PaTu(string savePath)
        {
            Console.WriteLine("请输入起始页：");
            var startPage = Console.ReadLine();
            Console.WriteLine("请输入结束页(可选)：");
            var endPage = Console.ReadLine();

            int nStartPage = Convert.ToInt32(startPage);
            int? nEndPage = null;
            if (!string.IsNullOrEmpty(endPage))
                nEndPage = Convert.ToInt32(endPage);


            PageAnalyzeBase pageAnalyze = SelectedAnalyze(nStartPage, nEndPage);

            bool isHasRepeat = false;

            do
            {
                if (isHasRepeat)
                    break;

                var currentPageIndex = pageAnalyze.PagerInfo.PageIndex;
                var handelUrl = pageAnalyze.PagerInfo.PageUrl;

                Console.WriteLine(string.Format("●Start Hander Page {0} !", currentPageIndex));
                Console.WriteLine(string.Format("●Url:{0}", handelUrl));
                List<PicDto> imgLinkUrls = pageAnalyze.GetCurrentPageImageLinks();
                if (imgLinkUrls == null)
                    break;

                var total = imgLinkUrls.Count;

                foreach (var picDto in imgLinkUrls)
                {
                    var currentIndex = imgLinkUrls.IndexOf(picDto);
                    if (_DB.PicInfo.Count(x => x.ImageUrl == picDto.ImageUrl) > 0)
                    {
                        Console.WriteLine(string.Format("STOP:Check exists image {0} in DB", picDto.ImageUrl));
                        isHasRepeat = true;
                        break;
                    }

                    this.Download(picDto, savePath, currentIndex);
                    var picInfo = this._Mapper.Map<PicInfo>(picDto);
                    _DB.PicInfo.Add(picInfo);

                    Console.WriteLine($"save[{currentIndex + 1}/{total}]:" + picDto.ImageLocalPath);
                }
                Console.WriteLine("Save {0} {1} Page In Database", currentPageIndex, handelUrl);
                _DB.SaveChanges();
                Console.WriteLine(string.Format("★ Hander Complete Page {0} !", currentPageIndex));
            }
            while (pageAnalyze.Next());
        }

        private string GetDirName(int pageIndex)
        {
            return (Math.Ceiling(Convert.ToDouble(pageIndex) / 100d) * 100).ToString();
        }

        string[] spChar = new string[] { "\\", "/", ":", "?", "\"", "<", ">", "|", ",", "!", " ", "'", "”", "“", "！", "~" };
        string rpChar = "_";

        private bool Download(PicDto picInfo, string savePath,int downloadIndex)
        {
            downloadIndex++;

            if (this.ImageDownloadConfig.WaitNumber > 0)
            {
                if (downloadIndex % this.ImageDownloadConfig.WaitNumber == 0)
                    Thread.Sleep(this.ImageDownloadConfig.WaitSecond * 1000);
            }

            var filename = Path.GetFileName(picInfo.ImageUrl);
            var dirName = string.Empty;
            if (!string.IsNullOrEmpty(picInfo.Title))
            {
                dirName = picInfo.Title;
                foreach (var c in spChar)
                    dirName = dirName.Replace(c, rpChar);
            }

            var localFullPath = picInfo.ImageLocalPath;
            if (string.IsNullOrEmpty(localFullPath))
                localFullPath = Path.Combine(savePath, dirName, filename);

            var dirPath = Path.GetDirectoryName(localFullPath);
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            picInfo.ImageLocalPath = localFullPath;
            picInfo.LocalImageName = filename;

            var isSuccess = DownLoadHelper.HttpDownload(picInfo.ImageUrl, picInfo.ImageLocalPath);

            picInfo.IsSuccess = isSuccess;
            picInfo.ReDownTime = 0;
            return isSuccess;
        }


    }
}
