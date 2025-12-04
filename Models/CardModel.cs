using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace QuanLyXe03.Models
{
    /// <summary>
    /// Model đầy đủ cho thẻ xe
    /// </summary>
    public class CardModel : ReactiveObject
    {
        // THÔNG TIN THẺ 
        private Guid _cardID;
        public Guid CardID
        {
            get => _cardID;
            set => this.RaiseAndSetIfChanged(ref _cardID, value);
        }

        private string _cardNo = "";
        public string CardNo
        {
            get => _cardNo;
            set => this.RaiseAndSetIfChanged(ref _cardNo, value);
        }

        private string _cardNumber = "";
        public string CardNumber
        {
            get => _cardNumber;
            set => this.RaiseAndSetIfChanged(ref _cardNumber, value);
        }

        private string _cardGroupID = "";
        public string CardGroupID
        {
            get => _cardGroupID;
            set => this.RaiseAndSetIfChanged(ref _cardGroupID, value);
        }

        private string _cardGroupName = "";
        public string CardGroupName
        {
            get => _cardGroupName;
            set => this.RaiseAndSetIfChanged(ref _cardGroupName, value);
        }

        private DateTime? _dateRegister;
        public DateTime? DateRegister
        {
            get => _dateRegister;
            set => this.RaiseAndSetIfChanged(ref _dateRegister, value);
        }

        private DateTime? _expireDate;
        public DateTime? ExpireDate
        {
            get => _expireDate;
            set => this.RaiseAndSetIfChanged(ref _expireDate, value);
        }

        private DateTime? _importDate;
        public DateTime? ImportDate
        {
            get => _importDate;
            set => this.RaiseAndSetIfChanged(ref _importDate, value);
        }

        private int _status;
        public int Status
        {
            get => _status;
            set => this.RaiseAndSetIfChanged(ref _status, value);
        }

        // Hiển thị trạng thái dạng text
        public string StatusText => Status switch
        {
            0 => "Chưa kích hoạt",
            1 => "Đang hoạt động",
            2 => "Hết hạn",
            3 => "Đã khóa",
            _ => "Không xác định"
        };

        private bool _isLock;
        public bool IsLock
        {
            get => _isLock;
            set => this.RaiseAndSetIfChanged(ref _isLock, value);
        }

        private bool _isDelete;
        public bool IsDelete
        {
            get => _isDelete;
            set => this.RaiseAndSetIfChanged(ref _isDelete, value);
        }

        private string _description = "";
        public string Description
        {
            get => _description;
            set => this.RaiseAndSetIfChanged(ref _description, value);
        }

        private string _accessLevelID = "";
        public string AccessLevelID
        {
            get => _accessLevelID;
            set => this.RaiseAndSetIfChanged(ref _accessLevelID, value);
        }

        //  THÔNG TIN XE 
        private string _plate1 = "";
        public string Plate1
        {
            get => _plate1;
            set => this.RaiseAndSetIfChanged(ref _plate1, value);
        }

        private string _vehicleName1 = "";
        public string VehicleName1
        {
            get => _vehicleName1;
            set => this.RaiseAndSetIfChanged(ref _vehicleName1, value);
        }

        private string _plate2 = "";
        public string Plate2
        {
            get => _plate2;
            set => this.RaiseAndSetIfChanged(ref _plate2, value);
        }

        private string _vehicleName2 = "";
        public string VehicleName2
        {
            get => _vehicleName2;
            set => this.RaiseAndSetIfChanged(ref _vehicleName2, value);
        }

        private string _plate3 = "";
        public string Plate3
        {
            get => _plate3;
            set => this.RaiseAndSetIfChanged(ref _plate3, value);
        }

        private string _vehicleName3 = "";
        public string VehicleName3
        {
            get => _vehicleName3;
            set => this.RaiseAndSetIfChanged(ref _vehicleName3, value);
        }

        //THÔNG TIN KHÁCH HÀNG 
        private string _customerID = "";
        public string CustomerID
        {
            get => _customerID;
            set => this.RaiseAndSetIfChanged(ref _customerID, value);
        }

        private string _customerName = "";
        public string CustomerName
        {
            get => _customerName;
            set => this.RaiseAndSetIfChanged(ref _customerName, value);
        }

        private string _customerGroupID = "";
        public string CustomerGroupID
        {
            get => _customerGroupID;
            set => this.RaiseAndSetIfChanged(ref _customerGroupID, value);
        }

        private string _customerGroupName = "";
        public string CustomerGroupName
        {
            get => _customerGroupName;
            set => this.RaiseAndSetIfChanged(ref _customerGroupName, value);
        }

        private string _address = "";
        public string Address
        {
            get => _address;
            set => this.RaiseAndSetIfChanged(ref _address, value);
        }

        private string _apartmentNumber = "";
        public string ApartmentNumber
        {
            get => _apartmentNumber;
            set => this.RaiseAndSetIfChanged(ref _apartmentNumber, value);
        }

        private string _phone = "";
        public string Phone
        {
            get => _phone;
            set => this.RaiseAndSetIfChanged(ref _phone, value);
        }

        private string _email = "";
        public string Email
        {
            get => _email;
            set => this.RaiseAndSetIfChanged(ref _email, value);
        }

        // UI HELPERS 

        // Checkbox trong DataGrid
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => this.RaiseAndSetIfChanged(ref _isSelected, value);
        }

        // STT trong DataGrid
        private int _index;
        public int Index
        {
            get => _index;
            set => this.RaiseAndSetIfChanged(ref _index, value);
        }

        // Hiển thị ngày đăng ký
        public string DisplayDateRegister => DateRegister?.ToString("dd/MM/yyyy") ?? "---";

        // Hiển thị ngày hết hạn
        public string DisplayExpireDate => ExpireDate?.ToString("dd/MM/yyyy") ?? "---";

        // Hiển thị ngày import
        public string DisplayImportDate => ImportDate?.ToString("dd/MM/yyyy") ?? "---";
    }

}