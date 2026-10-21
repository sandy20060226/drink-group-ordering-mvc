using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace Homework.Controllers
{
    public class GroupBuyController : Controller
    {
        // 🔒 學校正式資料庫連線字串
        private readonly string _connectionString;

        public GroupBuyController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("尚未設定資料庫連線。");
        }

        [HttpGet]
        public IActionResult Index(int page = 1, bool showOnlyMe = false)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserAccount")))
            {
                return RedirectToAction("Login", "Account");
            }

            if (page < 1) page = 1;

            string myName = HttpContext.Session.GetString("UserName") ?? "";
            ViewBag.UserAccount = HttpContext.Session.GetString("UserAccount");
            ViewBag.UserName = myName;
            ViewBag.TodayShop = "珍煮丹 (今日限定團)";

            List<Dictionary<string, string>> allOrders = new List<Dictionary<string, string>>();
            List<Dictionary<string, string>> filteredOrders = new List<Dictionary<string, string>>();
            int totalPrice = 0;

            // 1. 讀取今日點餐清單
            string orderQuery = "SELECT id, item_name, price, buyer_name FROM TableOrders1131619 ORDER BY id DESC";

            // 2. 讀取明日店家投票統計結果
            string voteQuery = "SELECT shop_name, COUNT(*) as vote_count FROM TableVotes1131619 GROUP BY shop_name";

            Dictionary<string, int> voteResults = new Dictionary<string, int>
            {
                { "可不可熟成紅茶", 0 },
                { "迷客夏 Milksha", 0 },
                { "50嵐", 0 },
                { "麻古茶坊", 0 }
            };

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                try
                {
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand(orderQuery, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var order = new Dictionary<string, string>
                            {
                                { "Id", reader["id"].ToString() ?? "" },
                                { "ItemName", reader["item_name"].ToString() ?? "" },
                                { "Price", reader["price"].ToString() ?? "0" },
                                { "BuyerName", reader["buyer_name"].ToString() ?? "" }
                            };
                            allOrders.Add(order);
                            totalPrice += Convert.ToInt32(reader["price"]);

                            if (showOnlyMe)
                            {
                                if (reader["buyer_name"].ToString() == myName)
                                {
                                    filteredOrders.Add(order);
                                }
                            }
                            else
                            {
                                filteredOrders.Add(order);
                            }
                        }
                    }

                    using (SqlCommand cmd = new SqlCommand(voteQuery, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string shop = reader["shop_name"].ToString() ?? "";
                            int count = Convert.ToInt32(reader["vote_count"]);
                            if (voteResults.ContainsKey(shop))
                            {
                                voteResults[shop] = count;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.ErrorMessage = "資料庫讀取失敗：" + ex.Message;
                }
            }

            int pageSize = 5;
            int totalOrdersCount = filteredOrders.Count;
            int totalPages = (int)Math.Ceiling((double)totalOrdersCount / pageSize);
            if (totalPages < 1) totalPages = 1;

            if (page > totalPages) page = totalPages;

            int skipCount = (page - 1) * pageSize;
            List<Dictionary<string, string>> pagedOrders = new List<Dictionary<string, string>>();

            for (int i = skipCount; i < skipCount + pageSize && i < totalOrdersCount; i++)
            {
                pagedOrders.Add(filteredOrders[i]);
            }

            ViewBag.Orders = pagedOrders;
            ViewBag.TotalPrice = totalPrice;
            ViewBag.VoteResults = voteResults;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.ShowOnlyMe = showOnlyMe;

            int targetPrice = 500;
            double progress = ((double)totalPrice / targetPrice) * 100;
            ViewBag.Progress = progress > 100 ? 100 : Math.Round(progress, 1);
            ViewBag.IsFreeDelivery = totalPrice >= targetPrice;

            return View();
        }

        [HttpPost]
        public IActionResult CreateOrder(string itemName, int price)
        {
            string? buyerName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(buyerName)) return RedirectToAction("Login", "Account");

            if (price <= 0) price = 65;

            string query = "INSERT INTO TableOrders1131619 (item_name, price, buyer_name) VALUES (@item, @price, @buyer)";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@item", itemName ?? "預設飲料");
                    cmd.Parameters.AddWithValue("@price", price);
                    cmd.Parameters.AddWithValue("@buyer", buyerName);
                    try { conn.Open(); cmd.ExecuteNonQuery(); } catch (Exception ex) { return Content(ex.Message); }
                }
            }
            return RedirectToAction("Index", new { page = 1, showOnlyMe = false });
        }

        [HttpPost]
        public IActionResult UpdateOrder(int id, string itemName, int price, int currentPage, bool showOnlyMe)
        {
            if (price <= 0) price = 65;

            string query = "UPDATE TableOrders1131619 SET item_name = @item, price = @price WHERE id = @id";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@item", itemName ?? "預設飲料");
                    cmd.Parameters.AddWithValue("@price", price);
                    cmd.Parameters.AddWithValue("@id", id);
                    try
                    {
                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        return Content("修改失敗：" + ex.Message);
                    }
                }
            }
            return RedirectToAction("Index", new { page = currentPage, showOnlyMe = showOnlyMe });
        }

        [HttpPost]
        public IActionResult CastVote(string shopName)
        {
            // 驗證登入狀態
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserAccount")))
            {
                return RedirectToAction("Login", "Account");
            }

            // 💡 做法二：只寫入店家名稱、票數帶 1、時間帶目前時間，完全不碰人名
            string query = "INSERT INTO TableVotes1131619 (shop_name, vote_count, end_time) VALUES (@shop, 1, GETDATE())";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@shop", shopName);
                    try
                    {
                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        return Content("投票失敗，錯誤訊息：" + ex.Message);
                    }
                }
            }
            return RedirectToAction("Index");
        }
        [HttpPost]
        public IActionResult DeleteOrder(int id, int currentPage, bool showOnlyMe)
        {
            string query = "DELETE FROM TableOrders1131619 WHERE id = @id";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    try { conn.Open(); cmd.ExecuteNonQuery(); } catch { }
                }
            }
            return RedirectToAction("Index", new { page = currentPage, showOnlyMe = showOnlyMe });
        }
    }
}