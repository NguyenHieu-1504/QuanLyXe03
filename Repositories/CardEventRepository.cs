using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Data.SqlClient;

using QuanLyXe03.Models;

namespace QuanLyXe03.Repositories
{
    public class CardEventRepository
    {
        private readonly string _connectionString;

        public CardEventRepository()
        {
            
            _connectionString = "Server=LAPTOP-KI4FNUQ7;Database=MPARKINGEVENTTM;User Id=sa;Password=123;TrustServerCertificate=True;";
        }

        public List<CardEventModel> GetAll()
        {
            var list = new List<CardEventModel>();

            try
            {
                Debug.WriteLine("🔌 Đang kết nối database...");

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

                    Debug.WriteLine($"📦 Đã đọc {count} bản ghi từ database");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Lỗi kết nối database: {ex.Message}");
            }

            return list;
        }


            /// <summary>
            /// Thêm bản ghi xe vào (Ghi vào)
            /// </summary>
public Guid? InsertCardEventIn(string plateIn, DateTime datetimeIn)
        {
            try
            {
                var newId = Guid.NewGuid();

                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    var query = @"
                INSERT INTO tblCardEvent (
                    Id, CardNumber, PlateIn, DatetimeIn, DateTimeOut, CustomerName, Moneys
                )
                VALUES (
                    @Id, '', @PlateIn, @DatetimeIn, NULL, '', 0
                )";

                    var cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Id", newId);
                    cmd.Parameters.AddWithValue("@PlateIn", plateIn);
                    cmd.Parameters.AddWithValue("@DatetimeIn", datetimeIn);

                    int result = cmd.ExecuteNonQuery();

                    if (result > 0)
                    {
                        Debug.WriteLine($"✅ Đã thêm xe vào: {plateIn} lúc {datetimeIn:HH:mm:ss}");
                        return newId;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Lỗi InsertCardEventIn: {ex.Message}");
            }

            return null;
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

                        Debug.WriteLine($"✅ Tìm thấy xe: {plateIn} vào lúc {cardEvent.DatetimeIn:HH:mm:ss}");
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

