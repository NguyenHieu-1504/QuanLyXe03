using Microsoft.Data.SqlClient;
using QuanLyXe03.Models;
using QuanLyXe03.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace QuanLyXe03.Repositories
{
    public class CardEventRepository
    {
        private readonly string _connectionString;

        public CardEventRepository()
        {

            _connectionString = SettingsManager.GetConnectionString("ParkingEventDb");
            Debug.WriteLine($"🔗 CardEventRepository: Using connection string from config");
        }

        public List<CardEventModel> GetAll()
        {
            var list = new List<CardEventModel>();

            try
            {
                Debug.WriteLine(" Đang kết nối database...");

                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    Debug.WriteLine(" Kết nối database thành công!");

                    var cmd = new SqlCommand(@"
                SELECT TOP 200 Id, CardNumber, PlateIn, DatetimeIn, DateTimeOut, CustomerName, Moneys 
                FROM tblCardEvent 
                ORDER BY DatetimeIn DESC", conn);

                    var reader = cmd.ExecuteReader();

                    int count = 0;
                    while (reader.Read())
                    {
                        var item = new CardEventModel
                        {
                            Id = reader.GetGuid(0),
                            CardNumber = reader["CardNumber"]?.ToString() ?? "",
                            PlateIn = reader["PlateIn"]?.ToString() ?? "",
                            DatetimeIn = reader["DatetimeIn"] as DateTime?,
                            DateTimeOut = reader["DateTimeOut"] as DateTime?,
                            CustomerName = reader["CustomerName"]?.ToString() ?? "",
                            Moneys = reader["Moneys"] != DBNull.Value ? Convert.ToDecimal(reader["Moneys"]) : 0
                        };
                        list.Add(item);
                        count++;
                    }

                    Debug.WriteLine($" Đã đọc {count} bản ghi từ database");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Lỗi kết nối database: {ex.Message}");
            }

            return list;
        }

        /// <summary>
        /// Lấy danh sách sự kiện ra vào thẻ CÓ PHÂN TRANG
        /// </summary>
        public CardEventPageModel GetCardEvents(string searchText = "", DateTime? fromDate = null, DateTime? toDate = null, int pageNumber = 1, int pageSize = 15)
        {
            var result = new CardEventPageModel();
            var list = new List<CardEventModel>();
            int totalCount = 0;

            // Tính toán Offset cho SQL
            int offset = (pageNumber - 1) * pageSize;
            Debug.WriteLine($"📂 Repository: GetCardEvents (Page {pageNumber}) - Connecting DB...");

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    Debug.WriteLine("📂 Repository: Connected!");

                    // --- TRUY VẤN 1: LẤY TỔNG SỐ BẢN GHI (ĐỂ TÍNH SỐ TRANG) ---
                    var countQuery = @"SELECT COUNT(*) FROM tblCardEvent WHERE 1=1 ";

                    // Thêm điều kiện lọc cho query COUNT
                    if (!string.IsNullOrWhiteSpace(searchText))
                    {
                        countQuery += " AND (CardNumber LIKE @SearchText OR PlateIn LIKE @SearchText OR CustomerName LIKE @SearchText) ";
                    }
                    if (fromDate.HasValue)
                    {
                        countQuery += " AND DatetimeIn >= @FromDate ";
                    }
                    if (toDate.HasValue)
                    {
                        countQuery += " AND DatetimeIn < @ToDate ";
                    }

                    var countCmd = new SqlCommand(countQuery, conn);
                    // Thêm Parameters cho query COUNT
                    if (!string.IsNullOrWhiteSpace(searchText)) countCmd.Parameters.AddWithValue("@SearchText", $"%{searchText.Trim()}%");
                    if (fromDate.HasValue) countCmd.Parameters.AddWithValue("@FromDate", fromDate.Value.Date);
                    if (toDate.HasValue) countCmd.Parameters.AddWithValue("@ToDate", toDate.Value.Date.AddDays(1));

                    // Thực thi
                    totalCount = (int)countCmd.ExecuteScalar();


                    // --- TRUY VẤN 2: LẤY DỮ LIỆU CỦA TRANG HIỆN TẠI (15 BẢN GHI) ---
                    var query = @"
                        SELECT Id, CardNumber, PlateIn, DatetimeIn, DateTimeOut, CustomerName, Moneys 
                        FROM tblCardEvent 
                        WHERE 1=1 ";

                    // Thêm điều kiện lọc cho query DATA
                    if (!string.IsNullOrWhiteSpace(searchText))
                    {
                        query += " AND (CardNumber LIKE @SearchText OR PlateIn LIKE @SearchText OR CustomerName LIKE @SearchText) ";
                    }
                    if (fromDate.HasValue)
                    {
                        query += " AND DatetimeIn >= @FromDate ";
                    }
                    if (toDate.HasValue)
                    {
                        query += " AND DatetimeIn < @ToDate ";
                    }

                    // Thêm logic Sắp xếp và Phân trang
                    query += @" ORDER BY DatetimeIn DESC
                               OFFSET @Offset ROWS 
                               FETCH NEXT @PageSize ROWS ONLY";

                    var cmd = new SqlCommand(query, conn);

                    // Thêm Parameters cho query DATA
                    if (!string.IsNullOrWhiteSpace(searchText)) cmd.Parameters.AddWithValue("@SearchText", $"%{searchText.Trim()}%");
                    if (fromDate.HasValue) cmd.Parameters.AddWithValue("@FromDate", fromDate.Value.Date);
                    if (toDate.HasValue) cmd.Parameters.AddWithValue("@ToDate", toDate.Value.Date.AddDays(1));

                    cmd.Parameters.AddWithValue("@Offset", offset);
                
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);

                    var reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        var item = new CardEventModel
                        {
                            Id = reader.GetGuid(0),
                            CardNumber = reader["CardNumber"]?.ToString() ?? "",
                            PlateIn = reader["PlateIn"]?.ToString() ?? "",
                            DatetimeIn = reader["DatetimeIn"] as DateTime?,
                            DateTimeOut = reader["DateTimeOut"] as DateTime?,
                            CustomerName = reader["CustomerName"]?.ToString() ?? "",
                            Moneys = reader["Moneys"] != DBNull.Value ? Convert.ToDecimal(reader["Moneys"]) : 0
                        };
                        list.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Lỗi GetCardEvents (Pagination): {ex.Message}");
            }

            result.Events = list;
            result.TotalCount = totalCount;
            return result;
        }


        /// <summary>
        /// Thêm bản ghi xe vào (Ghi vào)
        /// </summary>
        public Guid? InsertCardEventIn(string plateIn, DateTime datetimeIn, string cardNumber)
        {
            try
            {
                var newId = Guid.NewGuid();

                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    //  SỬ DỤNG TRANSACTION VỚI ISOLATION LEVEL THẤP
                    using (var transaction = conn.BeginTransaction(System.Data.IsolationLevel.ReadCommitted))
                    {
                                        var query = @"
                    INSERT INTO tblCardEvent
                    (Id, CardNumber, PlateIn, DatetimeIn, DateTimeOut, CustomerName, Moneys)
                    VALUES (@Id, @CardNumber, @PlateIn, @DatetimeIn, NULL, '', 0)";

                        var cmd = new SqlCommand(query, conn, transaction);
                        cmd.Parameters.AddWithValue("@Id", newId);
                        cmd.Parameters.AddWithValue("@CardNumber", cardNumber ?? "");
                        cmd.Parameters.AddWithValue("@PlateIn", plateIn);
                        cmd.Parameters.AddWithValue("@DatetimeIn", datetimeIn);

                        int result = cmd.ExecuteNonQuery();
                        transaction.Commit();  // ← Commit ngay

                        return result > 0 ? newId : null;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Lỗi InsertCardEventIn: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Tìm xe theo biển số (chưa ra)
        /// </summary>
        public CardEventModel? FindCardEventByPlate(string plateIn)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    var query = @"
                SELECT TOP 1 Id, CardNumber, PlateIn, DatetimeIn, DateTimeOut, CustomerName, Moneys
                FROM tblCardEvent
                WHERE PlateIn = @PlateIn
                AND DateTimeOut IS NULL
                ORDER BY DatetimeIn DESC";

                    var cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@PlateIn", plateIn);

                    var reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        var cardEvent = new CardEventModel
                        {
                            Id = reader.GetGuid(0),
                            CardNumber = reader["CardNumber"]?.ToString() ?? "",
                            PlateIn = reader["PlateIn"]?.ToString() ?? "",
                            DatetimeIn = reader["DatetimeIn"] as DateTime?,
                            DateTimeOut = reader["DateTimeOut"] as DateTime?,
                            CustomerName = reader["CustomerName"]?.ToString() ?? "",
                            Moneys = reader["Moneys"] != DBNull.Value ? Convert.ToDecimal(reader["Moneys"]) : 0
                        };

                        Debug.WriteLine($" Tìm thấy xe: {plateIn} vào lúc {cardEvent.DatetimeIn:HH:mm:ss}");
                        return cardEvent;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Lỗi FindCardEventByPlate: {ex.Message}");
            }

            Debug.WriteLine($"⚠️ Không tìm thấy xe với biển số: {plateIn}");
            return null;
        }

        /// <summary>
        /// Update xe ra (Ghi ra)
        /// </summary>
        public bool UpdateCardEventOut(Guid id, DateTime datetimeOut, decimal moneys)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    var query = @"
                UPDATE tblCardEvent
                SET DateTimeOut = @DateTimeOut,
                    Moneys = @Moneys
                WHERE Id = @Id";

                    var cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@DateTimeOut", datetimeOut);
                    cmd.Parameters.AddWithValue("@Moneys", moneys);

                    int result = cmd.ExecuteNonQuery();

                    if (result > 0)
                    {
                        Debug.WriteLine($"✅ Đã cập nhật xe ra: ID={id}, Tiền={moneys:N0} VNĐ");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Lỗi UpdateCardEventOut: {ex.Message}");
            }

            return false;
        }
    }
    }

