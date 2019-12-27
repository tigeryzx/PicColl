using System;
using System.Collections.Generic;
using System.Text;

namespace PicColl.DBContext.Model
{
    public class PicInfo
    {
        public int Id { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 图片源页面URL
        /// </summary>
        public string ImageSiteUrl { get; set; }

        /// <summary>
        /// 图片URL
        /// </summary>
        public string ImageUrl { get; set; }

        /// <summary>
        /// 图片本地路径
        /// </summary>
        public string ImageLocalPath { get; set; }

        /// <summary>
        /// 原图片名称
        /// </summary>
        public string SrcImageName { get; set; }

        /// <summary>
        /// 本地图片名称
        /// </summary>
        public string LocalImageName { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateDate { get; set; }

        /// <summary>
        /// 最后操作时间
        /// </summary>
        public DateTime? LastDate { get; set; }

        /// <summary>
        /// 是否重复
        /// </summary>
        public bool IsRepeat { get; set; }

        /// <summary>
        /// 重新下载次数
        /// </summary>
        public int? ReDownTime { get; set; }
    }
}
