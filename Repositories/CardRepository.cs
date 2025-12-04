using Microsoft.Data.SqlClient;
using QuanLyXe03.Models;
using QuanLyXe03.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace QuanLyXe03.Repositories
{
    public class CardRepository
    {
        private readonly string _connectionString;

        // Simple in-memory cache to avoid repeated DB hits for the same card
        // Key: cardNumber, Value: tuple(CardModel, expireUtc)
        private readonly ConcurrentDictionary<string, (CardModel card, DateTime expireUtc)> _cache
            = new(StringComparer.OrdinalIgnoreCase);
        private readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(5);

        public CardRepository()
        {
            _connectionString = SettingsManager.GetConnectionString("ParkingCardDb");
            Debug.WriteLine($"🔗 CardRepository: Using connection string from config");
        }

        /// <summary>
        ///   Tìm thẻ theo CardNumber (dùng cho quẹt thẻ)
        ///   Uses in-memory cache to speed up repeated accesses.
        /// </summary>
        public CardModel? FindCardByNumber(string cardNumber)
        {
            if (string.IsNullOrWhiteSpace(cardNumber))
                return null;

            // Check cache first
            if (_cache.TryGetValue(cardNumber, out var entry))
            {
                if (DateTime.UtcNow < entry.expireUtc)
                {
                    Debug.WriteLine($"(CACHE) Tìm thấy thẻ (cache): {cardNumber}");
                    return entry.card;
                }
                else
                {
                    // expired
                    _cache.TryRemove(cardNumber, out _);
                }
            }

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // chỉ JOIN CardGroup để lấy tên nhóm thẻ
                    var query = @"
                SELECT TOP 1
                    c.CardID, 
                    c.CardNo, 
                    c.CardNumber, 
                    c.CustomerID, 
                    c.CardGroupID,
                    c.ImportDate, 
                    c.ExpireDate, 
                    c.DateRegister,
                    c.Plate1, 
                    c.Plate2, 
                    c.Plate3,
                    c.VehicleName1, 
                    c.VehicleName2, 
                    c.VehicleName3,
                    c.IsLock, 
                    c.IsDelete, 
                    c.Description, 
                    c.AccessLevelID, 
                    c.Status,
                    cg.CardGroupName
                FROM tblCard c
                LEFT JOIN tblCardGroup cg ON CONVERT(varchar(36), cg.CardGroupID) = c.CardGroupID
                WHERE c.CardNumber = @CardNumber 
                  AND c.IsDelete = 0
                  AND c.Plate1 IS NOT NULL
                ORDER BY c.DateRegister DESC";

                    var cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@CardNumber", cardNumber);

                    var reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        var card = new CardModel
                        {
                            CardID = reader.GetGuid(0),
                            CardNo = reader["CardNo"]?.ToString() ?? "",
                            CardNumber = reader["CardNumber"]?.ToString() ?? "",
                            CustomerID = reader["CustomerID"]?.ToString() ?? "",
                            CardGroupID = reader["CardGroupID"]?.ToString() ?? "",

                            ImportDate = reader["ImportDate"] as DateTime?,
                            ExpireDate = reader["ExpireDate"] as DateTime?,
                            DateRegister = reader["DateRegister"] as DateTime?,

                            Plate1 = reader["Plate1"]?.ToString() ?? "",
                            Plate2 = reader["Plate2"]?.ToString() ?? "",
                            Plate3 = reader["Plate3"]?.ToString() ?? "",
                            VehicleName1 = reader["VehicleName1"]?.ToString() ?? "",
                            VehicleName2 = reader["VehicleName2"]?.ToString() ?? "",
                            VehicleName3 = reader["VehicleName3"]?.ToString() ?? "",

                            IsLock = reader.GetBoolean(reader.GetOrdinal("IsLock")),
                            IsDelete = reader.GetBoolean(reader.GetOrdinal("IsDelete")),
                            Description = reader["Description"]?.ToString() ?? "",
                            AccessLevelID = reader["AccessLevelID"]?.ToString() ?? "",
                            Status = reader["Status"] != DBNull.Value ? Convert.ToInt32(reader["Status"]) : 0,

                            //  CHỈ LẤY TÊN NHÓM THẺ
                            CardGroupName = reader["CardGroupName"]?.ToString() ?? "",

                            //  BỎ QUA thông tin khách hàng
                            CustomerName = "",
                            CustomerGroupName = "",
                            Address = "",
                            ApartmentNumber = "",
                            Phone = "",
                            Email = ""
                        };

                        Debug.WriteLine($"✅ Tìm thấy thẻ: {cardNumber}");
                        Debug.WriteLine($"   Biển số: {card.Plate1}");
                        Debug.WriteLine($"   Nhóm thẻ: {card.CardGroupName}");
                        Debug.WriteLine($"   Hết hạn: {card.ExpireDate?.ToString("dd/MM/yyyy") ?? "N/A"}");

                        // Put into cache
                        _cache[cardNumber] = (card, DateTime.UtcNow.Add(_cacheTtl));

                        return card;
                    }

                    Debug.WriteLine($"⚠️ Không tìm thấy thẻ: {cardNumber}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Lỗi FindCardByNumber: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Invalidate cache for a card (use when card is updated in DB)
        /// </summary>
        public void InvalidateCardCache(string cardNumber)
        {
            if (string.IsNullOrWhiteSpace(cardNumber)) return;
            _cache.TryRemove(cardNumber, out _);
        }
        /// <summary>
        ///  MỚI: Kiểm tra thẻ có hợp lệ không
        /// </summary>
        public (bool isValid, string errorMessage) ValidateCard(CardModel card)
        {
            // Kiểm tra khóa
            if (card.IsLock)
            {
                return (false, "Thẻ đã bị khóa");
            }

            // Kiểm tra hết hạn
            if (card.ExpireDate.HasValue && card.ExpireDate.Value.Date < DateTime.Now.Date)
            {
                return (false, $"Thẻ đã hết hạn ({card.ExpireDate.Value:dd/MM/yyyy})");
            }

            // Kiểm tra trạng thái
            if (card.Status == 3) // Đã hủy
            {
                return (false, "Thẻ đã bị hủy");
            }

            if (card.Status == 0) // Chưa kích hoạt
            {
                return (false, "Thẻ chưa được kích hoạt");
            }

            return (true, "");
        }

        /// <summary>
        /// Lấy danh sách thẻ CÓ PHÂN TRANG với JOIN đầy đủ thông tin
        /// </summary>
        public CardPageModel GetCards(
        string searchText = "",
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string customerGroup = "",
        int statusFilter = -1,
        string dateFilterType = "none",
        int pageNumber = 1,
        int pageSize = 30)
        {
            var result = new CardPageModel();
            var list = new List<CardModel>();
            int totalCount = 0;

            // SQL 2008: Tính toán dòng bắt đầu và kết thúc cho ROW_NUMBER
            int startRow = ((pageNumber - 1) * pageSize) + 1;
            int endRow = pageNumber * pageSize;

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    var baseWhere = " WHERE c.IsDelete = 0";

                    if (!string.IsNullOrWhiteSpace(searchText))
                    {
                        baseWhere += @" AND (
                    c.CardNo LIKE @SearchText OR 
                    c.CardNumber LIKE @SearchText OR 
                    c.CustomerID LIKE @SearchText OR 
                    c.Plate1 LIKE @SearchText OR 
                    cust.CustomerName LIKE @SearchText
                    )";
                    }

                    if (fromDate.HasValue && toDate.HasValue)
                    {
                        if (dateFilterType == "register")
                        {
                            baseWhere += " AND c.DateRegister BETWEEN @FromDate AND @ToDate";
                        }
                        else if (dateFilterType == "expire")
                        {
                            baseWhere += " AND c.ExpireDate BETWEEN @FromDate AND @ToDate";
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(customerGroup))
                    {
                        baseWhere += " AND cust.CustomerGroupID = @CustomerGroup";
                    }

                    if (statusFilter >= 0)
                    {
                        baseWhere += " AND c.Status = @StatusFilter";
                    }

                    // 1. COUNT QUERY
                    var countQuery = @"
                        SELECT COUNT(*) 
                        FROM tblCard c
                        LEFT JOIN tblCustomer cust ON CONVERT(varchar(36), cust.CustomerID) = c.CustomerID
                        " + baseWhere;

                    var countCmd = new SqlCommand(countQuery, conn);
                    if (!string.IsNullOrWhiteSpace(searchText))
                        countCmd.Parameters.AddWithValue("@SearchText", $"%{searchText}%");
                    if (fromDate.HasValue && toDate.HasValue)
                    {
                        countCmd.Parameters.AddWithValue("@FromDate", fromDate.Value.Date);
                        countCmd.Parameters.AddWithValue("@ToDate", toDate.Value.Date.AddDays(1).AddSeconds(-1));
                    }
                    if (!string.IsNullOrWhiteSpace(customerGroup))
                        countCmd.Parameters.AddWithValue("@CustomerGroup", customerGroup);
                    if (statusFilter >= 0)
                        countCmd.Parameters.AddWithValue("@StatusFilter", statusFilter);

                    totalCount = (int)countCmd.ExecuteScalar();

                    // 2. DATA QUERY
                    var dataQuery = @"
                        SELECT * FROM (
                            SELECT 
                                ROW_NUMBER() OVER (ORDER BY c.DateRegister DESC) AS RowNum,
                                c.CardID, c.CardNo, c.CardNumber, c.CustomerID, c.CardGroupID,
                                c.ImportDate, c.ExpireDate, c.Plate1, c.VehicleName1, 
                                c.Plate2, c.VehicleName2, c.Plate3, c.VehicleName3,
                                c.IsLock, c.IsDelete, c.Description, c.DateRegister, 
                                c.AccessLevelID, c.Status,
                                cust.CustomerName, cust.Address, cust.CustomerGroupID,
                                cg.CardGroupName,
                                custg.CustomerGroupName
                            FROM tblCard c
                            LEFT JOIN tblCustomer cust ON CONVERT(varchar(36), cust.CustomerID) = c.CustomerID
                            LEFT JOIN tblCardGroup cg ON CONVERT(varchar(36), cg.CardGroupID) = c.CardGroupID
                            LEFT JOIN tblCustomerGroup custg ON CONVERT(varchar(36), custg.CustomerGroupID) = cust.CustomerGroupID
                            " + baseWhere + @"
                        ) AS RowConstrainedResult
                        WHERE RowNum >= @StartRow AND RowNum <= @EndRow
                        ORDER BY RowNum";

                    var dataCmd = new SqlCommand(dataQuery, conn);
                    if (!string.IsNullOrWhiteSpace(searchText))
                        dataCmd.Parameters.AddWithValue("@SearchText", $"%{searchText}%");
                    if (fromDate.HasValue && toDate.HasValue)
                    {
                        dataCmd.Parameters.AddWithValue("@FromDate", fromDate.Value.Date);
                        dataCmd.Parameters.AddWithValue("@ToDate", toDate.Value.Date.AddDays(1).AddSeconds(-1));
                    }
                    if (!string.IsNullOrWhiteSpace(customerGroup))
                        dataCmd.Parameters.AddWithValue("@CustomerGroup", customerGroup);
                    if (statusFilter >= 0)
                        dataCmd.Parameters.AddWithValue("@StatusFilter", statusFilter);

                    dataCmd.Parameters.AddWithValue("@StartRow", startRow);
                    dataCmd.Parameters.AddWithValue("@EndRow", endRow);

                    var reader = dataCmd.ExecuteReader();

                    int index = startRow;
                    while (reader.Read())
                    {
                        var card = new CardModel
                        {
                            Index = index++,
                            CardID = reader.GetGuid(1),
                            CardNo = reader["CardNo"]?.ToString() ?? "",
                            CardNumber = reader["CardNumber"]?.ToString() ?? "",
                            CustomerID = reader["CustomerID"]?.ToString() ?? "",
                            CardGroupID = reader["CardGroupID"]?.ToString() ?? "",
                            ImportDate = reader["ImportDate"] as DateTime?,
                            ExpireDate = reader["ExpireDate"] as DateTime?,
                            Plate1 = reader["Plate1"]?.ToString() ?? "",
                            VehicleName1 = reader["VehicleName1"]?.ToString() ?? "",
                            Plate2 = reader["Plate2"]?.ToString() ?? "",
                            VehicleName2 = reader["VehicleName2"]?.ToString() ?? "",
                            Plate3 = reader["Plate3"]?.ToString() ?? "",
                            VehicleName3 = reader["VehicleName3"]?.ToString() ?? "",
                            IsLock = reader.GetBoolean(reader.GetOrdinal("IsLock")),
                            IsDelete = reader.GetBoolean(reader.GetOrdinal("IsDelete")),
                            Description = reader["Description"]?.ToString() ?? "",
                            DateRegister = reader["DateRegister"] as DateTime?,
                            AccessLevelID = reader["AccessLevelID"]?.ToString() ?? "",
                            Status = reader["Status"] != DBNull.Value ? Convert.ToInt32(reader["Status"]) : 0,
                            // Thông tin khách hàng
                            CustomerName = reader["CustomerName"]?.ToString() ?? "",
                            Address = reader["Address"]?.ToString() ?? "",
                            ApartmentNumber = "",
                            Phone = "",
                            Email = "",
                            // Tên nhóm
                            CardGroupName = reader["CardGroupName"]?.ToString() ?? "",
                            CustomerGroupName = reader["CustomerGroupName"]?.ToString() ?? ""
                        };

                        list.Add(card);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Lỗi GetCards: {ex.Message}");
            }

            result.Cards = list;
            result.TotalCount = totalCount;
            return result;
        }

        /// <summary>
        /// Thêm thẻ mới (có transaction)
        /// </summary>
        public bool InsertCard(CardModel card)
        {
            var newCardID = Guid.NewGuid();
            var newCustomerID = string.IsNullOrWhiteSpace(card.CustomerID)
                ? Guid.NewGuid().ToString()
                : card.CustomerID;

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using var transaction = conn.BeginTransaction();

                    try
                    {
                        // 1. Thêm/cập nhật khách hàng
                        var custQuery = @"
                            IF EXISTS (SELECT 1 FROM tblCustomer WHERE CONVERT(varchar(36), CustomerID) = @CustomerID)
                            BEGIN
                                UPDATE tblCustomer SET
                                    CustomerName = @CustomerName,
                                    CustomerGroupID = ISNULL(@CustomerGroupID, CustomerGroupID),
                                    Address = @Address,
                                    CompartmentId = @ApartmentNumber,
                                    Mobile = @Phone,
                                    Email = @Email,
                                    DateUpdate = GETDATE(),
                                    Inactive = @Inactive
                                WHERE CONVERT(varchar(36), CustomerID) = @CustomerID
                            END
                            ELSE
                            BEGIN
                                INSERT INTO tblCustomer (
                                    CustomerID, CustomerName, CustomerGroupID,
                                    Address, CompartmentId, Mobile, Email,
                                    DateUpdate, Inactive 
                                ) VALUES (
                                    CONVERT(uniqueidentifier, @CustomerID), @CustomerName, @CustomerGroupID,
                                    @Address, @ApartmentNumber, @Phone, @Email,
                                    GETDATE(), @Inactive 
                                )
                            END";

                        var custCmd = new SqlCommand(custQuery, conn, transaction);
                        custCmd.Parameters.AddWithValue("@CustomerID", newCustomerID);
                        custCmd.Parameters.AddWithValue("@CustomerName", card.CustomerName ?? "");
                        custCmd.Parameters.AddWithValue("@CustomerGroupID",
                            string.IsNullOrWhiteSpace(card.CustomerGroupID)
                                ? (object)DBNull.Value
                                : card.CustomerGroupID);
                        custCmd.Parameters.AddWithValue("@Address", card.Address ?? "");
                        custCmd.Parameters.AddWithValue("@ApartmentNumber", card.ApartmentNumber ?? "");
                        custCmd.Parameters.AddWithValue("@Phone", card.Phone ?? "");
                        custCmd.Parameters.AddWithValue("@Email", card.Email ?? "");
                        custCmd.Parameters.AddWithValue("@Inactive", 0);

                        custCmd.ExecuteNonQuery();
                        Debug.WriteLine($"✅ Đã thêm/cập nhật khách hàng: {newCustomerID}");

                        // 2. Thêm thẻ
                        var cardQuery = @"
                            INSERT INTO tblCard (
                                CardID, CardNo, CardNumber, CustomerID, CardGroupID,
                                ImportDate, ExpireDate, DateRegister, Status, 
                                Plate1, Plate2, VehicleName1, VehicleName2,
                                IsLock, IsDelete, Description, AccessLevelID,
                                AccessExpireDate, DateActive, DVT, CardType, DateRemove, DateUpdate
                            )
                            VALUES (
                                @CardID, @CardNo, @CardNumber, @CustomerID, @CardGroupID,
                                @ImportDate, @ExpireDate, @DateRegister, @Status,
                                @Plate1, @Plate2, @VehicleName1, @VehicleName2,
                                @IsLock, @IsDelete, @Description, @AccessLevelID,
                                '2099-12-31', '2000-12-31', 0, 0, GETDATE(), GETDATE()
                            )";

                        var cardCmd = new SqlCommand(cardQuery, conn, transaction);
                        cardCmd.Parameters.AddWithValue("@CardID", newCardID);
                        cardCmd.Parameters.AddWithValue("@CardNo", card.CardNo ?? "");
                        cardCmd.Parameters.AddWithValue("@CardNumber", card.CardNumber ?? "");
                        cardCmd.Parameters.AddWithValue("@CustomerID", newCustomerID);
                        cardCmd.Parameters.AddWithValue("@CardGroupID", card.CardGroupID ?? (object)DBNull.Value);
                        cardCmd.Parameters.AddWithValue("@ImportDate", card.ImportDate ?? DateTime.Now);
                        cardCmd.Parameters.AddWithValue("@ExpireDate", card.ExpireDate ?? DateTime.Now.AddYears(1));
                        cardCmd.Parameters.AddWithValue("@DateRegister", card.DateRegister ?? DateTime.Now);
                        cardCmd.Parameters.AddWithValue("@Status", card.Status);
                        cardCmd.Parameters.AddWithValue("@Plate1", card.Plate1 ?? "");
                        cardCmd.Parameters.AddWithValue("@Plate2", card.Plate2 ?? "");
                        cardCmd.Parameters.AddWithValue("@VehicleName1", card.VehicleName1 ?? "");
                        cardCmd.Parameters.AddWithValue("@VehicleName2", card.VehicleName2 ?? "");
                        cardCmd.Parameters.AddWithValue("@IsLock", card.IsLock ? 1 : 0);
                        cardCmd.Parameters.AddWithValue("@IsDelete", 0);
                        cardCmd.Parameters.AddWithValue("@Description", card.Description ?? "");
                        cardCmd.Parameters.AddWithValue("@AccessLevelID", card.AccessLevelID ?? "");

                        int result = cardCmd.ExecuteNonQuery();

                        // Commit transaction
                        transaction.Commit();

                        Debug.WriteLine($"✅ Đã thêm thẻ: {card.CardNumber} (ID: {newCardID})");
                        return result > 0;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Lỗi InsertCard: {ex.Message}");
                Debug.WriteLine($"   StackTrace: {ex.StackTrace}");
                return false;
            }
        }

        public bool LockCard(Guid cardId)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand("UPDATE tblCard SET IsLock = 1 WHERE CardID = @CardID", conn);
                    cmd.Parameters.AddWithValue("@CardID", cardId);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Lỗi LockCard: {ex.Message}");
                return false;
            }
        }

        public bool UnlockCard(Guid cardId)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand("UPDATE tblCard SET IsLock = 0 WHERE CardID = @CardID", conn);
                    cmd.Parameters.AddWithValue("@CardID", cardId);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Lỗi UnlockCard: {ex.Message}");
                return false;
            }
        }

        public bool DeleteCard(Guid cardId)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand("UPDATE tblCard SET IsDelete = 1, DateDelete = GETDATE() WHERE CardID = @CardID", conn);
                    cmd.Parameters.AddWithValue("@CardID", cardId);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Lỗi DeleteCard: {ex.Message}");
                return false;
            }
        }

        public bool CancelCard(Guid cardId)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand(@"
                        UPDATE tblCard 
                        SET Status = 3, DateCancel = GETDATE() 
                        WHERE CardID = @CardID", conn);
                    cmd.Parameters.AddWithValue("@CardID", cardId);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Lỗi CancelCard: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Lấy danh sách nhóm thẻ (tblCardGroup)
        /// </summary>
        public List<CardGroupModel> GetCardGroups()
        {
            var list = new List<CardGroupModel>();

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    var query = @"
                        SELECT CardGroupID, CardGroupName, Description 
                        FROM tblCardGroup 
                        ORDER BY CardGroupName";  // Xóa WHERE IsDelete = 0

                    var cmd = new SqlCommand(query, conn);
                    var reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        list.Add(new CardGroupModel
                        {
                            CardGroupID = reader.GetGuid(0),
                            CardGroupName = reader["CardGroupName"]?.ToString() ?? "",
                            Description = reader["Description"]?.ToString() ?? ""
                        });
                    }
                }

                Debug.WriteLine($"✅ Đã tải {list.Count} nhóm thẻ.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Lỗi GetCardGroups: {ex.Message}");
            }

            return list;
        }

        /// <summary>
        /// Lấy danh sách nhóm khách hàng (tblCustomerGroup)
        /// </summary>
        public List<CustomerGroupModel> GetCustomerGroups()
        {
            var list = new List<CustomerGroupModel>();

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    var query = @"
                        SELECT CustomerGroupID, CustomerGroupName, Description 
                        FROM tblCustomerGroup 
                        ORDER BY CustomerGroupName";  // Xóa WHERE IsDelete = 0

                    var cmd = new SqlCommand(query, conn);
                    var reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        list.Add(new CustomerGroupModel
                        {
                            CustomerGroupID = reader.GetGuid(0),
                            CustomerGroupName = reader["CustomerGroupName"]?.ToString() ?? "",
                            Description = reader["Description"]?.ToString() ?? ""
                        });
                    }
                }

                Debug.WriteLine($"✅ Đã tải {list.Count} nhóm khách hàng.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Lỗi GetCustomerGroups: {ex.Message}");
            }

            return list;
        }
    }
}