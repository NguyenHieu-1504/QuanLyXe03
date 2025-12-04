using Avalonia.Threading;
using QuanLyXe03.Models;
using QuanLyXe03.Repositories;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace QuanLyXe03.ViewModels
{
    public class CardEventManagementViewModel : ReactiveObject
    {
        private readonly CardEventRepository _repo;
        private readonly int _pageSize = 15; // Giới hạn 15 bản ghi/trang

        // --- Trạng thái Loading ---
        private readonly ObservableAsPropertyHelper<bool> _isLoading;
        public bool IsLoading => _isLoading.Value;

        // --- Dữ liệu ---
        private List<CardEventModel> _cardEvents = new();
        public List<CardEventModel> CardEvents
        {
            get => _cardEvents;
            protected set => this.RaiseAndSetIfChanged(ref _cardEvents, value);
        }

        // --- Filters ---
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

        // --- Thuộc tính Phân trang ---
        private int _totalCount = 0;
        public int TotalCount
        {
            get => _totalCount;
            set => this.RaiseAndSetIfChanged(ref _totalCount, value);
        }

        private int _currentPage = 1;
        public int CurrentPage
        {
            get => _currentPage;
            set => this.RaiseAndSetIfChanged(ref _currentPage, value);
        }

        private int _totalPages = 0;
        public int TotalPages
        {
            get => _totalPages;
            set => this.RaiseAndSetIfChanged(ref _totalPages, value);
        }

        // --- COMMANDS ---
        public ReactiveCommand<Unit, Unit> LoadCardEventsCommand { get; }
        public ReactiveCommand<Unit, Unit> ResetFiltersCommand { get; }
        public ReactiveCommand<Unit, Unit> ExportToExcelCommand { get; }
        public ReactiveCommand<Unit, Unit> NextPageCommand { get; }
        public ReactiveCommand<Unit, Unit> PreviousPageCommand { get; }

        // --- CONSTRUCTOR ---
        public CardEventManagementViewModel()
        {
            _repo = new CardEventRepository();

            // 1. Command Tải Dữ Liệu
            LoadCardEventsCommand = ReactiveCommand.CreateFromTask(LoadCardEventsAsync);
            _isLoading = LoadCardEventsCommand.IsExecuting.ToProperty(this, x => x.IsLoading);

            // 2. Commands Phân Trang
            var canGoNext = this.WhenAnyValue(
                x => x.CurrentPage, x => x.TotalPages, x => x.IsLoading,
                (curr, total, loading) => curr < total && !loading);

            NextPageCommand = ReactiveCommand.Create(() => { CurrentPage++; }, canGoNext);

            var canGoPrev = this.WhenAnyValue(
                x => x.CurrentPage, x => x.IsLoading,
                (curr, loading) => curr > 1 && !loading);

            PreviousPageCommand = ReactiveCommand.Create(() => { CurrentPage--; }, canGoPrev);

            // 3. Command Reset
            ResetFiltersCommand = ReactiveCommand.Create(ResetFilters,
                LoadCardEventsCommand.IsExecuting.Select(isExecuting => !isExecuting));

            ExportToExcelCommand = ReactiveCommand.Create(ExportToExcel);

            // 4. LOGIC KÍCH HOẠT (TRIGGER)

            // Trigger 1: Khi Filters (SearchText, FromDate, ToDate) thay đổi
            // Sẽ RESET về trang 1.
            this.WhenAnyValue(x => x.SearchText, x => x.FromDate, x => x.ToDate)
                .Throttle(TimeSpan.FromMilliseconds(500), RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    if (CurrentPage != 1)
                    {
                        CurrentPage = 1; // Việc set CurrentPage=1 sẽ tự động kích hoạt Trigger 2
                    }
                    else
                    {
                        // Nếu đang ở trang 1, phải tự gọi Load
                        LoadCardEventsCommand.Execute().Subscribe();
                    }
                });

            // Trigger 2: Khi CurrentPage thay đổi (do Next, Prev, hoặc Reset)
            // Sẽ TẢI DỮ LIỆU cho trang đó.
            this.WhenAnyValue(x => x.CurrentPage)
                .Select(_ => Unit.Default)
                .InvokeCommand(LoadCardEventsCommand);

            // Không cần tải lần đầu, vì CurrentPage=1 sẽ tự động kích hoạt Trigger 2
        }

        // --- LOGIC ---

        private async Task LoadCardEventsAsync()
        {
            Debug.WriteLine($"🔄 Bắt đầu LoadCardEventsAsync cho Trang {CurrentPage}...");
            try
            {
                var searchText = SearchText;
                var fromDate = FromDate;
                var toDate = ToDate;
                var pageNumber = CurrentPage;

                // 1. Lấy dữ liệu phân trang từ Repository
                var pageData = await Task.Run(() => _repo.GetCardEvents(searchText, fromDate, toDate, pageNumber, _pageSize));

                // 2. Tính toán Index cho STT
                int startIndex = (pageNumber - 1) * _pageSize + 1;
                foreach (var item in pageData.Events)
                {
                    item.Index = startIndex++;
                }

                // 3. Gán kết quả về UI (phải quay về UI Thread)
                CardEvents = pageData.Events;
                TotalCount = pageData.TotalCount;
                TotalPages = (int)Math.Ceiling((double)TotalCount / _pageSize);

                Debug.WriteLine($"✅ LoadCardEventsAsync thành công: {pageData.Events.Count} bản ghi (Tổng: {TotalCount})");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Lỗi LoadCardEventsAsync: {ex.Message}");
                CardEvents = new List<CardEventModel>();
                TotalCount = 0;
                TotalPages = 0;
            }
        }

        private void ResetFilters()
        {
            SearchText = "";
            FromDate = null;
            ToDate = null;
            // Việc set 3 thuộc tính này sẽ tự động kích hoạt Trigger 1 (Reset về trang 1)
        }

        private void ExportToExcel()
        {
            Debug.WriteLine("📊 Xuất Excel... (chưa làm)");
        }

        public void RefreshCardEvents() => LoadCardEventsCommand.Execute().Subscribe();
    }
}