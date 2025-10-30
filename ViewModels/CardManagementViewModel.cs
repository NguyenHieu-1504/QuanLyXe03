using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Linq;
using System.Diagnostics;
using QuanLyXe03.Models;
using QuanLyXe03.Repositories;
using Avalonia.Threading;


namespace QuanLyXe03.ViewModels
{
    public class CardManagementViewModel : ReactiveObject
    {
        private readonly CardRepository _repo;

        public ObservableCollection<CardModel> Cards { get; } = new();

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set => this.RaiseAndSetIfChanged(ref _searchText, value);
        }

        private DateTime? _fromDate;
        public DateTime? FromDate
        {
            get => _fromDate;
            set => this.RaiseAndSetIfChanged(ref _fromDate, value);
        }

        private DateTime? _toDate;
        public DateTime? ToDate
        {
            get => _toDate;
            set => this.RaiseAndSetIfChanged(ref _toDate, value);
        }

        private string _dateFilterType = "none"; // none, register, expire
        public string DateFilterType
        {
            get => _dateFilterType;
            set => this.RaiseAndSetIfChanged(ref _dateFilterType, value);
        }

        private int _selectedCount = 0;
        public int SelectedCount
        {
            get => _selectedCount;
            set => this.RaiseAndSetIfChanged(ref _selectedCount, value);
        }

        private int _totalCount = 0;
        public int TotalCount
        {
            get => _totalCount;
            set => this.RaiseAndSetIfChanged(ref _totalCount, value);
        }

        public ReactiveCommand<Unit, Unit> SearchCommand { get; }
        public ReactiveCommand<Unit, Unit> ResetCommand { get; }
        public ReactiveCommand<Unit, Unit> ExportExcelCommand { get; }
        public ReactiveCommand<Unit, Unit> ImportExcelCommand { get; }

        public CardManagementViewModel()
        {
            _repo = new CardRepository();

            //SearchCommand = ReactiveCommand.Create(LoadCards);
            SearchCommand = ReactiveCommand.Create(() =>
            {
                Debug.WriteLine("========================================");
                Debug.WriteLine("🔍 SearchCommand BẮT ĐẦU");
                Debug.WriteLine($"   SearchText = '{SearchText}'");
                Debug.WriteLine($"   Cards.Count TRƯỚC = {Cards.Count}");

                LoadCards();

                Debug.WriteLine($"   Cards.Count SAU = {Cards.Count}");
                Debug.WriteLine("🔍 SearchCommand KẾT THÚC");
                Debug.WriteLine("========================================");
            });
            ResetCommand = ReactiveCommand.Create(ResetFilters);
            ExportExcelCommand = ReactiveCommand.Create(ExportToExcel);
            ImportExcelCommand = ReactiveCommand.Create(ImportFromExcel);

           
        }


        private bool _isLoading = false;

        private void LoadCards()
        {
            if (_isLoading)
            {
                Debug.WriteLine("⚠️ Đang load rồi, bỏ qua...");
                return;
            }

            _isLoading = true;
            Debug.WriteLine("🔍 Đang tải danh sách thẻ...");

            try
            {
                // Load data trên background
                var data = _repo.GetCards(
                    searchText: SearchText,
                    fromDate: FromDate,
                    toDate: ToDate,
                    dateFilterType: DateFilterType,
                    maxRows: 100
                );

                Debug.WriteLine($"📦 Đã lấy {data.Count} dòng từ database");

                // Update UI trên UI thread
                Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        Debug.WriteLine($"🔄 Bắt đầu update UI. Cards.Count = {Cards.Count}");

                        Cards.Clear();
                        Debug.WriteLine($"🗑️ Đã clear. Cards.Count = {Cards.Count}");

                        foreach (var card in data)
                        {
                            Cards.Add(card);
                        }

                        TotalCount = Cards.Count;
                        Debug.WriteLine($"✅ Đã load {TotalCount} thẻ vào UI. Cards.Count = {Cards.Count}");
                    }
                    finally
                    {
                        _isLoading = false;
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Lỗi LoadCards: {ex.Message}");
                _isLoading = false;
            }
        }

        /// <summary>
        /// Refresh danh sách thẻ (gọi từ bên ngoài)
        /// </summary>
        public void RefreshCards()
        {
            Debug.WriteLine("🔄 RefreshCards được gọi");
            LoadCards();
        }




        private void ResetFilters()
        {
            SearchText = "";
            FromDate = null;
            ToDate = null;
            DateFilterType = "none";
            LoadCards();
        }

        private void ExportToExcel()
        {
            Debug.WriteLine("📊 Xuất Excel...");
            // TODO: Implement export
        }

        private void ImportFromExcel()
        {
            Debug.WriteLine("📥 Nhập Excel...");
            // TODO: Implement import
        }
    }
}