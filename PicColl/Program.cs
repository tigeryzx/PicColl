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
        static void Download(List<PicInfo> list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                var item = list[i];
                var filename = Path.GetFileName(item.ImageUrl);
                DownLoadHelper.HttpDownload(item.ImageUrl, "E:\\test\\" + filename);
                if (i % 4 == 0)
                    Thread.Sleep(1000);
            }
        }

        static void Main(string[] args)
        {

            //Page_mzitu page_Mzitu = new Page_mzitu("https://www.mzitu.com/all/", 1, null);
            //var test = page_Mzitu.GetCurrentPageImageLinks();
            //Download(test);
            //page_Mzitu.Next();
            //test = page_Mzitu.GetCurrentPageImageLinks();
            //Download(test);
            //Console.ReadKey();

            PicDownloadMachine machine = new PicDownloadMachine();

            while (true)
            {
                string tipInfo = "请输入以下数字以运行不同模式：\r\n【1】爬图 " +
                    "\r\n【2】重新下载失败的图 \r\n【3】分析&处理重复图片 \r\n【4】清除失败图片";

                Console.WriteLine(tipInfo);
                var type = Console.ReadLine();
                switch (type)
                {
                    case "1":
                        Console.WriteLine("请输保存路径：");
                        var savePath = Console.ReadLine();
                        machine.PaTu(savePath);
                        break;
                    case "2":
                        machine.ReplacePicFormDb();
                        break;
                    case "3":
                        machine.AnalyzeRepeat();
                        break;
                    case "4":
                        machine.ClearFailedPic();
                        break;
                }
                Console.WriteLine("Done..................");
                Console.ReadKey();
            }
        }


    }
}
