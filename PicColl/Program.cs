using AngleSharp;
using PicColl.DBContext;
using PicColl.DBContext.Model;
using PicColl.PageAnalyze;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PicColl.DBContext.Common;
using System.Threading;

namespace PicColl
{
    class Program
    {
        static PicDBContext _DB;

        static void Download(List<PicInfo> list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                var item = list[i];
                var filename = Path.GetFileName(item.ImageUrl);
                DownLoadHelper.HttpDownload(item.ImageUrl, "E:\\test\\" + filename);
                if (i % 2 == 0)
                    Thread.Sleep(1000);
            }
        }

        static void Main(string[] args)
        {
         
            //Page_mzitu page_Mzitu = new Page_mzitu("https://www.mzitu.com/all/", 1);
            //var test = page_Mzitu.GetCurrentPageImageLinks();
            //Download(test);
            //page_Mzitu.Next();
            //test = page_Mzitu.GetCurrentPageImageLinks();
            //Download(test);


            Console.ReadKey();
            //_DB = new PicDBContext();
            //// 自动迁移数据库
            //_DB.Database.Migrate();

            //Func<string> NeedRootPath = new Func<string>(() =>
            //{
            //    Console.WriteLine("请输保存路径：");
            //    return Console.ReadLine();
            //    //return @"C:\Users\Administrator\Desktop\sese\";
            //});

            //while (true)
            //{
            //    string tipInfo = "请输入以下数字以运行不同模式：\r\n【1】爬图 " +
            //        "\r\n【2】重新下载失败的图 \r\n【3】分析图库到数据库 \r\n【4】分析&处理重复图片 \r\n【5】清除失败图片";

            //    Console.WriteLine(tipInfo);
            //    var type = Console.ReadLine();
            //    if (type == "1")
            //    {
            //        PaTu(NeedRootPath());
            //    }
            //    else if (type == "2")
            //        ReplacePicFormDb();
            //    else if (type == "3")
            //    {
            //        ImportPicInfo(NeedRootPath());
            //    }
            //    else if (type == "4")
            //        AnalyzeRepeat();
            //    else if (type == "5")
            //        ClearFailedPic();

            //    Console.WriteLine("Done..................");
            //    Console.ReadKey();
            //}
        }

        static void ClearFailedPic()
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

        static void AnalyzeRepeat()
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
                foreach(var picInfo in allRepeatItem)
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

        static string GetPicSrcUrl(string filename)
        {
            var extDir = filename.Substring(filename.IndexOf("_") + 1, 2);
            var urlFilename = filename.Substring(filename.IndexOf("_") + 1, filename.Length - filename.IndexOf("_") - 1);
            var url = string.Format("http://t1.b0b1.com/720/{0}/{1}", extDir, urlFilename);
            return url;
        }

        static void ImportPicInfo(string savePath)
        {
            var oldRecordCount = _DB.PicInfo.Count();

            if (oldRecordCount > 0)
            {
                Console.WriteLine(string.Format("当前数据库中有{0}条数据,处理前是否清空数据表[Y/N]？", oldRecordCount));
                var isClearTable = Console.ReadLine().ToUpper() == "Y";

                if (isClearTable)
                {
                    _DB.PicInfo.RemoveRange(_DB.PicInfo.Where(x => x.Id == x.Id));
                    _DB.SaveChanges();
                }
            }

            var filePaths = Directory.GetFiles(savePath, "*.*", SearchOption.AllDirectories);
            var memoryCount = 0;
            foreach(var filepath in filePaths)
            {
                var fileInfo = new FileInfo(filepath);
                var localFileName = Path.GetFileName(filepath);

                PicInfo picInfo = new PicInfo();
                picInfo.CreateDate = DateTime.Now;
                picInfo.ImageLocalPath = filepath;
                picInfo.LocalImageName = localFileName;
                picInfo.ImageUrl = GetPicSrcUrl(localFileName);
                picInfo.SrcImageName = Path.GetFileName(picInfo.ImageUrl);
                picInfo.IsSuccess = (fileInfo.Length != 0);
                picInfo.ReDownTime = 0;

                _DB.PicInfo.Add(picInfo);
                memoryCount++;
                if (memoryCount == 1000)
                {
                    _DB.SaveChanges();
                    memoryCount = 0;
                    Console.WriteLine("Save......");
                }
                Console.WriteLine(string.Format("Append:{0}", picInfo.ImageLocalPath));

            }

            // 最后再保存一次
            _DB.SaveChanges();
            Console.WriteLine("Last Save......");

            Console.WriteLine(string.Format("Save Total Record {0},Repeat Item {1}", 
                _DB.PicInfo.Count(), 
                _DB.PicInfo.Count(x => x.IsRepeat == true)));
        }

        static void ReplacePicFormDb()
        {
            Console.WriteLine("请输无效文件保存路径：");
            var faildSavePath = Console.ReadLine();
            //var faildSavePath = @"C:\Users\Administrator\Desktop\f";

            // 只重试少于一次失败的
            var faildPicList = _DB.PicInfo.Where(x => x.IsSuccess == false 
            && (x.ReDownTime < 1 || !x.ReDownTime.HasValue));
            int successCount = 0;
            int faildCount = 0;
            var memoryCount = 0;

            foreach (var picInfo in faildPicList)
            {
                Console.WriteLine("proc:{0}", picInfo.ImageUrl);

                picInfo.LastDate = DateTime.Now;
                if (DownLoadHelper.HttpDownload(picInfo.ImageUrl, picInfo.ImageLocalPath))
                {
                    picInfo.IsSuccess = true;
                    Console.WriteLine("★success:{0}", picInfo.ImageLocalPath);
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

        static PageAnalyzeBase SelectedAnalyze(int startPage, int? endPage)
        {
            PageAnalyzeBase selected = null;

            do
            {
                Console.WriteLine("请选择分析器:");
                Console.WriteLine("1:Page_sesehezi");
                Console.WriteLine("2:Page_sesehezi");

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

        static void PaTu(string savePath)
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
                List<PicInfo> imgLinkUrls = pageAnalyze.GetCurrentPageImageLinks();
                if (imgLinkUrls == null)
                    break;

                foreach (var imginfo in imgLinkUrls)
                {
                    if(_DB.PicInfo.Count(x => x.ImageUrl == imginfo.ImageUrl) > 0)
                    {
                        Console.WriteLine(string.Format("STOP:Check exists image {0} in DB", imginfo.ImageUrl));
                        isHasRepeat = true;
                        break;
                    }
                    //Task.Run(() =>
                    //{
                    var filename = currentPageIndex + "_" + imginfo.SrcImageName;
                    var dirName = GetDirName(currentPageIndex);
                    var localFullPath = Path.Combine(savePath, dirName, filename);
                    var isSuccess = DownLoadHelper.HttpDownload(imginfo.ImageUrl, localFullPath);

                    imginfo.ImageLocalPath = localFullPath;
                    imginfo.LocalImageName = filename;
                    imginfo.IsSuccess = isSuccess;
                    imginfo.ReDownTime = 0;

                    _DB.PicInfo.Add(imginfo);

                    Console.WriteLine("save:" + localFullPath);
                    //});
                }
                Console.WriteLine("Save {0} {1} Page In Database", currentPageIndex, handelUrl);
                _DB.SaveChanges();
                Console.WriteLine(string.Format("★ Hander Complete Page {0} !", currentPageIndex));
            }
            while (pageAnalyze.Next());
        }

        static string GetDirName(int pageIndex)
        {
            return (Math.Ceiling(Convert.ToDouble(pageIndex) / 100d) * 100).ToString();
        }
    }
}
