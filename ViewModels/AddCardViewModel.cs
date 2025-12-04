using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Diagnostics;
using QuanLyXe03.Models;
using QuanLyXe03.Repositories;
using System.Linq;

namespace QuanLyXe03.ViewModels
{
    public class AddCardViewModel : ReactiveObject
    {
        private readonly CardRepository _repo;

        // ==================== THÔNG TIN THẺ ====================
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

        private CardGroupModel? _selectedCardGroup;
        public CardGroupModel? SelectedCardGroup
        {
            get => _selectedCardGroup;
            set => this.RaiseAndSetIfChanged(ref _selectedCardGroup, value);
        }

        private DateTimeOffset? _dateRegister = DateTimeOffset.Now;
        public DateTimeOffset? DateRegister
        {
            get => _dateRegister;
            set => this.RaiseAndSetIfChanged(ref _dateRegister, value);
        }

        private DateTimeOffset? _expireDate = DateTimeOffset.Now.AddYears(1);
        public DateTimeOffset? ExpireDate
        {
            get => _expireDate;
            set => this.RaiseAndSetIfChanged(ref _expireDate, value);
        }

        private int _selectedStatus = 1; // Mặc định: Đang hoạt động
        public int SelectedStatus
        {
            get => _selectedStatus;
            set => this.RaiseAndSetIfChanged(ref _selectedStatus, value);
        }

        private string _description = "";
        public string Description
        {
            get => _description;
            set => this.RaiseAndSetIfChanged(ref _description, value);
        }

        // ==================== THÔNG TIN XE ====================
        private string _plate1 = "";
        public string Plate1
        {
            get => _plate1;
            set => this.RaiseAndSetIfChanged(ref _plate1, value);
        }

        private string _plate2 = "";
        public string Plate2
        {
            get => _plate2;
            set => this.RaiseAndSetIfChanged(ref _plate2, value);
        }

        // ==================== THÔNG TIN KHÁCH HÀNG ====================
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

        private CustomerGroupModel? _selectedCustomerGroup;
        public CustomerGroupModel? SelectedCustomerGroup
        {
            get => _selectedCustomerGroup;
            set => this.RaiseAndSetIfChanged(ref _selectedCustomerGroup, value);
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

        // ==================== DANH SÁCH ====================
        public ObservableCollection<CardGroupModel> CardGroups { get; } = new();
        public ObservableCollection<CustomerGroupModel> CustomerGroups { get; } = new();

        // ==================== COMMANDS ====================
        public ReactiveCommand<Unit, Unit> SaveCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }

        public event EventHandler<bool>? CloseRequested;

        public AddCardViewModel()
        {
            _repo = new CardRepository();

            LoadCardGroups();
            LoadCustomerGroups();

            SaveCommand = ReactiveCommand.Create(SaveCard);
            CancelCommand = ReactiveCommand.Create(() => CloseRequested?.Invoke(this, false));
        }

        private void LoadCardGroups()
        {
            try
            {
                var groups = _repo.GetCardGroups();
                CardGroups.Clear();
                foreach (var group in groups)
                {
                    CardGroups.Add(group);
                }

                if (CardGroups.Count > 0)
                {
                    SelectedCardGroup = CardGroups[0];
                }

                Debug.WriteLine($"✅ Đã load {CardGroups.Count} nhóm thẻ");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Lỗi LoadCardGroups: {ex.Message}");
            }
        }

        private void LoadCustomerGroups()
        {
            try
            {
                // Giả sử bạn có hàm GetCustomerGroups() trong Repository
                var groups = _repo.GetCustomerGroups();
                CustomerGroups.Clear();
                foreach (var group in groups)
                {
                    CustomerGroups.Add(group);
                }

                if (CustomerGroups.Count > 0)
                {
                    SelectedCustomerGroup = CustomerGroups[0];
                }

                Debug.WriteLine($"✅ Đã load {CustomerGroups.Count} nhóm khách hàng");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Lỗi LoadCustomerGroups: {ex.Message}");
            }
        }

        private void SaveCard()
        {
            Debug.WriteLine("💾 Đang lưu thẻ mới...");

            // ========== VALIDATION ==========
            if (string.IsNullOrWhiteSpace(CardNo))
            {
                Debug.WriteLine("⚠️ CardNo không được để trống");
                // TODO: Hiển thị thông báo lỗi cho user
                return;
            }

            if (string.IsNullOrWhiteSpace(CardNumber))
            {
                Debug.WriteLine("⚠️ CardNumber không được để trống");
                return;
            }

            if (SelectedCardGroup == null)
            {
                Debug.WriteLine("⚠️ Chưa chọn nhóm thẻ");
                return;
            }

            // Kiểm tra ít nhất phải có 1 thông tin: Biển số HOẶC Tên khách hàng
            if (string.IsNullOrWhiteSpace(Plate1) && string.IsNullOrWhiteSpace(CustomerName))
            {
                Debug.WriteLine("⚠️ Phải nhập ít nhất Biển số xe hoặc Tên khách hàng");
                return;
            }

            try
            {
                var newCard = new CardModel
                {
                    // Thông tin thẻ
                    CardNo = CardNo.Trim(),
                    CardNumber = CardNumber.Trim(),
                    CardGroupID = SelectedCardGroup.CardGroupID.ToString(),
                    DateRegister = DateRegister?.DateTime ?? DateTime.Now,
                    ExpireDate = ExpireDate?.DateTime ?? DateTime.Now.AddYears(1),
                    ImportDate = DateTime.Now,
                    Status = SelectedStatus,
                    Description = Description.Trim(),

                    // Thông tin xe
                    Plate1 = Plate1.Trim(),
                    Plate2 = Plate2.Trim(),

                    // Thông tin khách hàng
                    CustomerID = CustomerID.Trim(),
                    CustomerName = CustomerName.Trim(),
                    CustomerGroupID = SelectedCustomerGroup?.CustomerGroupID.ToString() ?? "",
                    Address = Address.Trim(),
                    ApartmentNumber = ApartmentNumber.Trim(),
                    Phone = Phone.Trim(),
                    Email = Email.Trim()
                };

                bool success = _repo.InsertCard(newCard);

                if (success)
                {
                    Debug.WriteLine($"✅ Đã thêm thẻ thành công: {CardNumber}");
                    CloseRequested?.Invoke(this, true);
                }
                else
                {
                    Debug.WriteLine("❌ Thêm thẻ thất bại");
                    // TODO: Hiển thị thông báo lỗi
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Lỗi SaveCard: {ex.Message}");
                Debug.WriteLine($"   StackTrace: {ex.StackTrace}");
            }
        }
    }
}