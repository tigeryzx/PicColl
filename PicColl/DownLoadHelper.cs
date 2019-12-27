using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace PicColl
{
    public class DownLoadHelper
    {
        /// <summary>
        /// http下载文件
        /// </summary>
        /// <param name="url">下载文件地址</param>
        /// <param name="path">文件存放地址，包含文件名</param>
        /// <returns></returns>
        public static bool HttpDownload(string url, string path)
        {
            var dirPath = Path.GetDirectoryName(path);
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);

            FileStream fs = null;
            try
            {
                fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                // 设置参数
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;

                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                request.Method = "GET";
                request.Headers["accept"] = "image/webp,image/apng,image/*,*/*;q=0.8";
                request.Headers["accept-encoding"] = "gzip, deflate, br";
                request.Headers["Accept-Language"] = "zh-CN,zh;q=0.8";
                //request.Headers["Host"] = "i5.meizitu.net";
                request.Headers["Pragma"] = "no-cache";
                request.Headers["referer"] = "https://www.mzitu.com/";
                request.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/55.0.2883.87 Safari/537.36";

                //发送请求并获取相应回应数据
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    fs.Close();
                    return false;
                }
                    
                //直到request.GetResponse()程序才开始向目标网页发送Post请求
                Stream responseStream = response.GetResponseStream();
                //创建本地文件写入流
                //Stream stream = new FileStream(tempFile, FileMode.Create);
                byte[] bArr = new byte[1024];
                int size = responseStream.Read(bArr, 0, (int)bArr.Length);
                while (size > 0)
                {
                    //stream.Write(bArr, 0, size);
                    fs.Write(bArr, 0, size);
                    size = responseStream.Read(bArr, 0, (int)bArr.Length);
                }
                fs.Close();
                responseStream.Close();
                return true;
            }
            catch (Exception ex)
            {
                fs.Close();
                return false;
            }
        }
    }
}
