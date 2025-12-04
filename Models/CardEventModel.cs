using System;

namespace QuanLyXe03.Models
{
    // KHÔNG KẾ THỪA ReactiveObject NỮA
    public class CardEventModel
    {
        // Thuộc tính Index (không cần setter private)
        public int Index { get; set; }
        public Guid Id { get; set; }
        public string CardNumber { get; set; } = "";
        public string PlateIn { get; set; } = "";

        // CÁC THUỘC TÍNH GỐC
        public DateTime? DatetimeIn { get; set; }
        public DateTime? DateTimeOut { get; set; } // Dạng nullable

        public string CustomerName { get; set; } = "";
        public decimal Moneys { get; set; }

        // --- THUỘC TÍNH MỚI: DÙNG ĐỂ BINDING LÊN UI ---
        // Giúp loại bỏ StringFormat trong XAML, tránh lỗi với giá trị null
        public string DisplayDatetimeIn => DatetimeIn.HasValue
            ? DatetimeIn.Value.ToString("dd/MM/yyyy HH:mm:ss")
            : "";
        public string DisplayDateTimeOut => DateTimeOut.HasValue
            ? DateTimeOut.Value.ToString("dd/MM/yyyy HH:mm:ss")
            : "";
        public string DisplayMoneys => Moneys.ToString("N0") + " VNĐ";
    }
}