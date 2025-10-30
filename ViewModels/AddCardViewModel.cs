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

        // Properties cho form
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

        // ✅ ĐỔI THÀNH DateTimeOffset?
        private DateTimeOffset? _dateRegister = DateTimeOffset.Now;
        public DateTimeOffset? DateRegister
        {
            get => _dateRegister;
            set => this.RaiseAndSetIfChanged(ref _dateRegister, value);
        }

        // ✅ ĐỔI THÀNH DateTimeOffset?
        private DateTimeOffset? _expireDate = DateTimeOffset.Now.AddYears(1);
        public DateTimeOffset? ExpireDate
        {
            get => _expireDate;
            set => this.RaiseAndSetIfChanged(ref _expireDate, value);
        }

        private int _selectedStatus = 0;
        public int SelectedStatus
        {
            get => _selectedStatus;
            set => this.RaiseAndSetIfChanged(ref _selectedStatus, value);
        }

        // Danh sách nhóm thẻ
        public ObservableCollection<CardGroupModel> CardGroups { get; } = new();

        // Commands
        public ReactiveCommand<Unit, Unit> SaveCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }

        // Event để đóng window
        public event EventHandler<bool>? CloseRequested;

        public AddCardViewModel()
        {
            _repo = new CardRepository();

            // Load danh sách nhóm thẻ
            LoadCardGroups();

            // Commands
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

                // Chọn nhóm đầu tiên
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

        private void SaveCard()
        {
            Debug.WriteLine("💾 Đang lưu thẻ mới...");

            // Validation
            if (string.IsNullOrWhiteSpace(CardNo))
            {
                Debug.WriteLine("⚠️ CardNo không được để trống");
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

            try
            {
                var newCard = new CardModel
                {
                    CardNo = CardNo.Trim(),
                    CardNumber = CardNumber.Trim(),
                    CustomerID = "", // Có thể để trống hoặc thêm field nhập
                    CardGroupID = SelectedCardGroup.CardGroupID.ToString(),
                    DateRegister = DateRegister?.DateTime ?? DateTime.Now,
                    ExpireDate = ExpireDate?.DateTime ?? DateTime.Now.AddYears(1),
                    ImportDate = DateTime.Now,
                    Status = SelectedStatus,
                    Description = ""
                };

                bool success = _repo.InsertCard(newCard);

                if (success)
                {
                    Debug.WriteLine($"✅ Đã thêm thẻ thành công: {CardNumber}");
                    CloseRequested?.Invoke(this, true); // true = success
                }
                else
                {
                    Debug.WriteLine("❌ Thêm thẻ thất bại");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Lỗi SaveCard: {ex.Message}");
            }
        }
    }
}