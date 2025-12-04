using System.Collections.Generic;

namespace QuanLyXe03.Models
{
    /// <summary>
    /// Model chứa dữ liệu phân trang cho CardEvent
    /// </summary>
    public class CardEventPageModel
    {
        /// <summary>
        /// Danh sách các bản ghi của trang hiện tại
        /// </summary>
        public List<CardEventModel> Events { get; set; } = new List<CardEventModel>();
        /// <summary>
        /// Tổng số bản ghi (không phân trang)
        /// </summary>
        public int TotalCount { get; set; }
    }
}