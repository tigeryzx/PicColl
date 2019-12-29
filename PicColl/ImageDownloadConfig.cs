using System;
using System.Collections.Generic;
using System.Text;

namespace PicColl
{
    public class ImageDownloadConfig
    {
        /// <summary>
        /// 每下载多少个文件停顿一次
        /// </summary>
        public int WaitNumber { get; set; }

        /// <summary>
        /// 每次停顿秒数
        /// </summary>
        public int WaitSecond { get; set; }
    }
}
