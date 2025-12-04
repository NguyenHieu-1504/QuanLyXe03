using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Linq;
using System.Diagnostics;
using QuanLyXe03.Models;
using QuanLyXe03.Repositories;
using Avalonia.Threading;
using System.Reactive.Linq; 
using System.Collections.Generic; 

namespace QuanLyXe03.ViewModels
{
    public class CardManagementViewModel : ReactiveObject
    {
        private readonly CardRepository _repo;
        private readonly int _pageSize = 30; // Cấu hình kích thước trang

        public ObservableCollection<CardModel> Cards { get; } = new();

        // --- Filters (Giữ nguyên) ---
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

        // --- Properties Phân Trang (MỚI) ---
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

        // --- Properties đếm (Giữ nguyên) ---
        private int _selectedCount = 0;
        public int SelectedCount
        {
            get => _selectedCount;
            set => this.RaiseAndSetIfChanged(ref _selectedCount, value);
        }

        private int _totalCount = 0; // Đây là tổng số bản ghi TRONG DB
        public int TotalCount
        {
            get => _totalCount;
            set => this.RaiseAndSetIfChanged(ref _totalCount, value);
        }

        // --- Commands (Thêm 2) ---
        public ReactiveCommand<Unit, Unit> SearchCommand { get; }
        public ReactiveCommand<Unit, Unit> ResetCommand { get; }
        public ReactiveCommand<Unit, Unit> ExportExcelCommand { get; }
        public ReactiveCommand<Unit, Unit> ImportExcelCommand { get; }
        public ReactiveCommand<Unit, Unit> DeleteCardsCommand { get; }
        public ReactiveCommand<Unit, Unit> NextPageCommand { get; }  
        public ReactiveCommand<Unit, Unit> PreviousPageCommand { get; } 

        public event EventHandler<(int count, Action onConfirm)>? DeleteConfirmationRequested;

        public CardManagementViewModel()
        {
            Debug.WriteLine("🏗️ CardManagementViewModel Constructor START");
            _repo = new CardRepository();

            // --- Định nghĩa Commands ---
            SearchCommand = ReactiveCommand.Create(() =>
            {
                Debug.WriteLine("🔍 SearchCommand triggered");
                // Khi nhấn Tìm, reset về trang 1
                if (CurrentPage != 1) CurrentPage = 1;
                else LoadCards(); // Nếu đang ở trang 1, buộc tải lại
            });

            ResetCommand = ReactiveCommand.Create(ResetFilters);
            ExportExcelCommand = ReactiveCommand.Create(ExportToExcel);
            ImportExcelCommand = ReactiveCommand.Create(ImportFromExcel);
            DeleteCardsCommand = ReactiveCommand.Create(DeleteSelectedCards);

            // --- Biến trạng thái tải ---
            var isLoading = this.WhenAnyValue(x => x._isLoading);

            // --- Command: Trang kế tiếp ---
            var canGoNext = Observable.CombineLatest(
                this.WhenAnyValue(x => x.CurrentPage),
                this.WhenAnyValue(x => x.TotalPages),
                isLoading,
                (curr, total, loading) => curr < total && !loading
            );

            NextPageCommand = ReactiveCommand.Create(() =>
            {
                CurrentPage++;
            }, canGoNext);

            // --- Command: Trang trước ---
            var canGoPrev = Observable.CombineLatest(
                this.WhenAnyValue(x => x.CurrentPage),
                isLoading,
                (curr, loading) => curr > 1 && !loading
            );

            PreviousPageCommand = ReactiveCommand.Create(() =>
            {
                CurrentPage--;
            }, canGoPrev);

            Debug.WriteLine("✅ Commands initialized");

            // --- Logic Tự động tải (Triggers) ---

            // Trigger 1: Khi Filter thay đổi, reset về trang 1
            this.WhenAnyValue(x => x.SearchText, x => x.FromDate, x => x.ToDate, x => x.DateFilterType)
                .Throttle(TimeSpan.FromMilliseconds(500)) // Chờ 500ms
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    Debug.WriteLine("Filters changed, resetting to page 1");
                    if (CurrentPage != 1) CurrentPage = 1;
                    // Không cần gọi LoadCards(), vì việc set CurrentPage=1 sẽ kích hoạt Trigger 2
                });

            // Trigger 2: Khi Trang thay đổi (do Next, Prev, hoặc Filter reset)
            // Sẽ tự động gọi LoadCards()
            this.WhenAnyValue(x => x.CurrentPage)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    Debug.WriteLine($"Page changed to {CurrentPage}, loading data");
                    LoadCards();
                });

            // Tải dữ liệu lần đầu (khi CurrentPage = 1)
            LoadCards();

            Debug.WriteLine("🏗️ CardManagementViewModel Constructor END");
        }


        private bool _isLoading = false;

        private void LoadCards()
        {
            if (_isLoading) return;
            _isLoading = true;
            Debug.WriteLine($"🔍 Đang tải danh sách thẻ cho Trang {CurrentPage}...");

            try
            {
                // Load data từ DB (Sửa lại)
                var pageData = _repo.GetCards(
                    searchText: SearchText,
                    fromDate: FromDate,
                    toDate: ToDate,
                    dateFilterType: DateFilterType,
                    statusFilter: -1, 
                    pageNumber: CurrentPage,
                    pageSize: _pageSize
                );

                var data = pageData.Cards;
                var totalDbCount = pageData.TotalCount;

                Debug.WriteLine($"📦 Đã lấy {data.Count} dòng từ database (Tổng: {totalDbCount})");

                //  Update UI trên UI thread
                Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        Debug.WriteLine($"🔄 Bắt đầu update UI. Cards.Count = {Cards.Count}");
                        foreach (var card in Cards) card.PropertyChanged -= OnCardPropertyChanged;
                        Cards.Clear();
                        Debug.WriteLine($"🗑️ Đã clear. Cards.Count = {Cards.Count}");

                        // Tính toán STT cho trang hiện tại
                        int index = (_pageSize * (CurrentPage - 1)) + 1;
                        foreach (var card in data)
                        {
                            card.Index = index++;
                            card.PropertyChanged += OnCardPropertyChanged;
                            Cards.Add(card);
                        }

                        TotalCount = totalDbCount; // Tổng số bản ghi trong DB
                        TotalPages = (int)Math.Ceiling((double)TotalCount / _pageSize); // Tính tổng số trang
                        UpdateSelectedCount();

                        Debug.WriteLine($"✅ Đã load {Cards.Count} thẻ vào UI (Trang {CurrentPage}/{TotalPages})");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"❌ Lỗi update UI: {ex.Message}");
                        Debug.WriteLine($"   StackTrace: {ex.StackTrace}");
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
                Debug.WriteLine($"   StackTrace: {ex.StackTrace}");
                _isLoading = false;
            }
        }

        //  Event handler riêng để dễ unsubscribe
        private void OnCardPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CardModel.IsSelected))
            {
                UpdateSelectedCount();
            }
        }

        //  Update số lượng thẻ đã chọn
        private void UpdateSelectedCount()
        {
            try
            {
                SelectedCount = Cards.Count(c => c.IsSelected);
                Debug.WriteLine($"📊 Đã chọn {SelectedCount}/{Cards.Count} thẻ (trang này)");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Lỗi UpdateSelectedCount: {ex.Message}");
            }
        }

        //  Xóa các thẻ đã chọn
        private void DeleteSelectedCards()
        {
            var selectedCards = Cards.Where(c => c.IsSelected).ToList();

            if (selectedCards.Count == 0)
            {
                Debug.WriteLine("⚠️ Không có thẻ nào được chọn");
                return;
            }

            Debug.WriteLine($"🗑️ Chuẩn bị xóa {selectedCards.Count} thẻ...");

            // Raise event để View hiển thị confirmation dialog
            DeleteConfirmationRequested?.Invoke(this, (selectedCards.Count, () =>
            {
                ExecuteDelete(selectedCards);
            }
            ));
        }

        //  Thực hiện xóa sau khi confirm
        private void ExecuteDelete(System.Collections.Generic.List<CardModel> cardsToDelete)
        {
            Debug.WriteLine($"🗑️ Bắt đầu xóa {cardsToDelete.Count} thẻ...");

            int successCount = 0;
            int failCount = 0;

            foreach (var card in cardsToDelete)
            {
                bool success = _repo.DeleteCard(card.CardID);
                if (success)
                {
                    successCount++;
                    Debug.WriteLine($"✅ Đã xóa thẻ: {card.CardNumber}");
                }
                else
                {
                    failCount++;
                    Debug.WriteLine($"❌ Lỗi xóa thẻ: {card.CardNumber}");
                }
            }

            Debug.WriteLine($"✅ Hoàn thành: {successCount} thành công, {failCount} thất bại");

            // Refresh danh sách
            Dispatcher.UIThread.Post(() =>
            {
                LoadCards();
            });
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
            // Không cần gọi LoadCards()
            // Việc thay đổi các thuộc tính trên sẽ kích hoạt Trigger 1
            // Trigger 1 sẽ set CurrentPage = 1
            // Trigger 2 sẽ thấy CurrentPage thay đổi và gọi LoadCards()
            if (CurrentPage != 1) CurrentPage = 1;
            else LoadCards(); // Nếu đã ở trang 1, buộc tải lại
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