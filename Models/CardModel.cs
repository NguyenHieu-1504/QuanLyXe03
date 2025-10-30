using System;

namespace QuanLyXe03.Models
{
    public class CardModel
    {
        public Guid CardID { get; set; }
        public string CardNo { get; set; } = "";
        public string CardNumber { get; set; } = "";
        public string CustomerID { get; set; } = "";
        public string CardGroupID { get; set; } = "";
        public DateTime? ImportDate { get; set; }
        public DateTime? ExpireDate { get; set; }
        public string Plate1 { get; set; } = "";
        public string VehicleName1 { get; set; } = "";
        public string Plate2 { get; set; } = "";
        public string VehicleName2 { get; set; } = "";
        public string Plate3 { get; set; } = "";
        public string VehicleName3 { get; set; } = "";
        public bool IsLock { get; set; }
        public bool IsDelete { get; set; }
        public string Description { get; set; } = "";
        public DateTime? DateRegister { get; set; }
        public string AccessLevelID { get; set; } = "";
        public int Status { get; set; }

        // Properties bổ sung để hiển thị
        public string CustomerName { get; set; } = "";
        public string CustomerGroupName { get; set; } = "";
        public string Address { get; set; } = "";
        public string ApartmentNumber { get; set; } = "";
        public string CardGroupName { get; set; } = "";
        public string StatusText => Status switch
        {
            0 => "Chưa kích hoạt",
            1 => "Đang hoạt động",
            2 => "Hết hạn",
            3 => "Đã khóa",
            _ => "Không xác định"
        };
    }
}