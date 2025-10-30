using System;

namespace QuanLyXe03.Models
{
    public class CardEventModel
    {
        public Guid Id { get; set; }
        public string CardNumber { get; set; } = "";
        public string PlateIn { get; set; } = "";
        public DateTime? DatetimeIn { get; set; }
        public DateTime? DateTimeOut { get; set; }
        public string CustomerName { get; set; } = "";
        public decimal Moneys { get; set; }
    }
}
