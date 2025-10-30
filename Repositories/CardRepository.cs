using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using QuanLyXe03.Models;
using System.Diagnostics;

namespace QuanLyXe03.Repositories
{
    public class CardRepository
    {
        private readonly string _connectionString;

        public CardRepository()
        {
            _connectionString = "Server=LAPTOP-KI4FNUQ7;Database=MPARKINGKH;User Id=sa;Password=123;TrustServerCertificate=True;";
        }

        /// <summary>
        /// Lấy danh sách thẻ với filter
        /// </summary>
        public List<CardModel> GetCards(
    string searchText = "",
    DateTime? fromDate = null,
    DateTime? toDate = null,
    string customerGroup = "",
    int statusFilter = -1,
    string dateFilterType = "none",
    int maxRows = 100)  // ✅ Mặc định chỉ load 100 dòng
        {
            var list = new List<CardModel>();

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // ✅ THÊM TOP để giới hạn
                    var query = $@"
                SELECT TOP ({maxRows})
                    CardID, CardNo, CardNumber, CustomerID, CardGroupID,
                    ImportDate, ExpireDate, Plate1, VehicleName1, 
                    Plate2, VehicleName2, Plate3, VehicleName3,
                    IsLock, IsDelete, Description, DateRegister, 
                    AccessLevelID, Status
                FROM tblCard 
                WHERE IsDelete = 0";

                    // Filter theo từ khóa tìm kiếm
                    if (!string.IsNullOrWhiteSpace(searchText))
                    {
                        query += @" AND (
                    CardNo LIKE @SearchText OR 
                    CardNumber LIKE @SearchText OR 
                    CustomerID LIKE @SearchText OR
                    Plate1 LIKE @SearchText OR
                    Plate2 LIKE @SearchText OR
                    Plate3 LIKE @SearchText
                )";
                    }

                    // Filter theo ngày
                    if (dateFilterType == "register" && fromDate.HasValue && toDate.HasValue)
                    {
                        query += " AND DateRegister BETWEEN @FromDate AND @ToDate";
                    }
                    else if (dateFilterType == "expire" && fromDate.HasValue && toDate.HasValue)
                    {
                        query += " AND ExpireDate BETWEEN @FromDate AND @ToDate";
                    }

                    // Filter theo trạng thái
                    if (statusFilter >= 0)
                    {
                        query += " AND Status = @Status";
                    }

                    query += " ORDER BY DateRegister DESC";

                    var cmd = new SqlCommand(query, conn);

                    if (!string.IsNullOrWhiteSpace(searchText))
                    {
                        cmd.Parameters.AddWithValue("@SearchText", $"%{searchText}%");
                    }

                    if (dateFilterType != "none" && fromDate.HasValue && toDate.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@FromDate", fromDate.Value);
                        cmd.Parameters.AddWithValue("@ToDate", toDate.Value);
                    }

                    if (statusFilter >= 0)
                    {
                        cmd.Parameters.AddWithValue("@Status", statusFilter);
                    }

                    var reader = cmd.ExecuteReader();

                    while (reader.Read())
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
                            Status = reader["Status"] != DBNull.Value ? Convert.ToInt32(reader["Status"]) : 0
                        };

                        list.Add(card);
                    }
                }

                Debug.WriteLine($"✅ CardRepository: Đã lấy {list.Count} thẻ");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Lỗi CardRepository.GetCards: {ex.Message}");
            }

            return list;
        }

        /// <summary>
        /// Lấy danh sách nhóm thẻ
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
                SELECT CardGroupID, CardGroupCode, CardGroupName, Description, CardType, Inactive
                FROM tblCardGroup
                WHERE Inactive = 0
                ORDER BY CardGroupName";

                    var cmd = new SqlCommand(query, conn);
                    var reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        var group = new CardGroupModel
                        {
                            CardGroupID = reader.GetGuid(0),
                            CardGroupCode = reader["CardGroupCode"]?.ToString() ?? "",
                            CardGroupName = reader["CardGroupName"]?.ToString() ?? "",
                            Description = reader["Description"]?.ToString() ?? "",
                            CardType = reader["CardType"] != DBNull.Value ? Convert.ToInt32(reader["CardType"]) : 0,
                            Inactive = reader.GetBoolean(reader.GetOrdinal("Inactive"))
                        };
                        list.Add(group);
                    }
                }

                Debug.WriteLine($"✅ Đã lấy {list.Count} nhóm thẻ");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Lỗi GetCardGroups: {ex.Message}");
            }

            return list;
        }

        /// <summary>
        /// Thêm thẻ mới
        /// </summary>
        public bool InsertCard(CardModel card)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    var query = @"
                INSERT INTO tblCard (
                    CardID, CardNo, CardNumber, CustomerID, CardGroupID,
                    ImportDate, ExpireDate, DateRegister, Status, 
                    IsLock, IsDelete, Description, AccessLevelID,
                    AccessExpireDate, DateActive, DVT, CardType, DateRemove, DateUpdate
                )
                VALUES (
                    @CardID, @CardNo, @CardNumber, @CustomerID, @CardGroupID,
                    @ImportDate, @ExpireDate, @DateRegister, @Status,
                    0, 0, @Description, '',
                    '2099-12-31', '2000-12-31', 0, 0, GETDATE(), GETDATE()
                )";

                    var cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@CardID", Guid.NewGuid());
                    cmd.Parameters.AddWithValue("@CardNo", card.CardNo);
                    cmd.Parameters.AddWithValue("@CardNumber", card.CardNumber);
                    cmd.Parameters.AddWithValue("@CustomerID", card.CustomerID);
                    cmd.Parameters.AddWithValue("@CardGroupID", card.CardGroupID);
                    cmd.Parameters.AddWithValue("@ImportDate", card.ImportDate ?? DateTime.Now);
                    cmd.Parameters.AddWithValue("@ExpireDate", card.ExpireDate ?? DateTime.Now.AddYears(1));
                    cmd.Parameters.AddWithValue("@DateRegister", card.DateRegister ?? DateTime.Now);
                    cmd.Parameters.AddWithValue("@Status", card.Status);
                    cmd.Parameters.AddWithValue("@Description", card.Description ?? "");

                    int result = cmd.ExecuteNonQuery();
                    Debug.WriteLine($"✅ Đã thêm thẻ: {card.CardNumber}");
                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Lỗi InsertCard: {ex.Message}");
                return false;
            }
        }




        /// <summary>
        /// Khóa thẻ
        /// </summary>
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

        /// <summary>
        /// Mở thẻ
        /// </summary>
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

        /// <summary>
        /// Xóa thẻ (soft delete)
        /// </summary>
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

        /// <summary>
        /// Hủy thẻ (vĩnh viễn - cập nhật trạng thái)
        /// </summary>
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
    }
}