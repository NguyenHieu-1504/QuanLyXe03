using QuanLyXe03.Models;
using System.Collections.Generic;

namespace QuanLyXe03.Models
{
    /// <summary>
    /// Model chứa dữ liệu phân trang cho Card
    /// </summary>
    public class CardPageModel
    {
        /// <summary>
        /// Danh sách các thẻ của trang hiện tại
        /// </summary>
        public List<CardModel> Cards { get; set; } = new List<CardModel>();

        /// <summary>
        /// Tổng số thẻ (không phân trang)
        /// </summary>
        public int TotalCount { get; set; }
    }
}