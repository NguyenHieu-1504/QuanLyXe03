using Avalonia.Threading;
using QuanLyXe03.Models;
using QuanLyXe03.Repositories;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;

namespace QuanLyXe03.ViewModels
{
    public class CardEventManagementViewModel : ReactiveObject
    {
        private readonly CardEventRepository _repo;

        public ObservableCollection<CardEventModel> CardEvents { get; } = new();

        private List<CardEventModel> _allData = new();

        // Search properties
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

        private int _totalCount = 0;
        public int TotalCount
        {
            get => _totalCount;
            set => this.RaiseAndSetIfChanged(ref _totalCount, value);
        }

        public ReactiveCommand<Unit, Unit> SearchCommand { get; }
        public ReactiveCommand<Unit, Unit> ResetCommand { get; }
        public ReactiveCommand<Unit, Unit> ExportExcelCommand { get; }

        public CardEventManagementViewModel()
        {
            _repo = new CardEventRepository();

            SearchCommand = ReactiveCommand.Create(LoadCardEvents);
            ResetCommand = ReactiveCommand.Create(ResetFilters);
            ExportExcelCommand = ReactiveCommand.Create(ExportToExcel);

            // Load dữ liệu ban đầu
            LoadCardEvents();
        }

        private void LoadCardEvents()
        {
            Debug.WriteLine("🔍 Đang tải lịch sử xe ra vào...");

            try
            {
                // Sử dụng GetAll có sẵn trong CardEventRepository
                var data = _repo.GetAll();

                // Filter theo search text nếu có
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchLower = SearchText.ToLower().Trim();
                    data = data.Where(x =>
                        (x.CardNumber?.ToLower().Contains(searchLower) ?? false) ||
                        (x.PlateIn?.ToLower().Contains(searchLower) ?? false) ||
                        (x.CustomerName?.ToLower().Contains(searchLower) ?? false)
                    ).ToList();
                }

                // Filter theo ngày nếu có
                if (FromDate.HasValue && ToDate.HasValue)
                {
                    data = data.Where(x =>
                        x.DatetimeIn.HasValue &&
                        x.DatetimeIn.Value.Date >= FromDate.Value.Date &&
                        x.DatetimeIn.Value.Date <= ToDate.Value.Date
                    ).ToList();
                }

                Debug.WriteLine($"📦 Đã lấy {data.Count} bản ghi");

                // Update UI
                Dispatcher.UIThread.Post(() =>
                {
                    CardEvents.Clear();

                    foreach (var item in data)
                    {
                        CardEvents.Add(item);
                    }

                    TotalCount = CardEvents.Count;
                    Debug.WriteLine($"✅ Đã load {TotalCount} bản ghi vào UI");
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Lỗi LoadCardEvents: {ex.Message}");
            }
        }

        private void ResetFilters()
        {
            SearchText = "";
            FromDate = null;
            ToDate = null;
            LoadCardEvents();
        }

        private void ExportToExcel()
        {
            Debug.WriteLine("📊 Xuất Excel...");
            // TODO: Implement
        }

        public void RefreshCardEvents()
        {
            Debug.WriteLine("🔄 RefreshCardEvents được gọi");
            LoadCardEvents();
        }
    }
}