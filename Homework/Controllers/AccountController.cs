using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient; // 🟢 引入 SQL 套件，消滅紅色毛毛蟲！
using System;

namespace Homework.Controllers
{
    public class AccountController : Controller
    {
        // 1. 學校資料庫連線字串
        private readonly string _connectionString;

        public AccountController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("尚未設定資料庫連線。");
        }

        // 2. 顯示登入網頁畫面 (GET: /Account/Login)
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // 3. 處理登入按鈕送出的資料 (POST: /Account/Login)
        [HttpPost]
        public IActionResult Login(string username, string account, string password)
        {
            // 💡 超強防呆：自動偵測前端傳來的是 username 還是 account，誰有值就用誰！
            string loginAccount = !string.IsNullOrEmpty(username) ? username : account;

            if (string.IsNullOrEmpty(loginAccount) || string.IsNullOrEmpty(password))
            {
                ViewBag.Message = "帳號與密碼皆為必填！";
                return View();
            }

            // -------------------------------------------------------------
            // 🔍 帳密驗證邏輯（請確認你登入時輸入的是 BubbleTea / 1234）
            // -------------------------------------------------------------
            if (loginAccount == "Bubble Tea" && password == "2222")
            {
                // 成功登入，寫入 Session 紀錄
                HttpContext.Session.SetString("UserAccount", loginAccount);
                HttpContext.Session.SetString("UserName", "老師專用測試帳號");

                // ========================================================
                // 🎯【全新功能：登入時大清空】
                // 📢 改用 TRUNCATE 直接全表清空，繞過欄位型態比對的錯誤！
                // 📢 請務必確認 TableOrders1131619 的學號與你的資料庫完全一致！
                // ========================================================
                string clearMyOrdersQuery = "TRUNCATE TABLE TableOrders1131619";
                string clearMyVotesQuery = "TRUNCATE TABLE TableVotes1131619";

                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    try
                    {
                        conn.Open();

                        // 1. 直接清空點餐表
                        using (SqlCommand cmd = new SqlCommand(clearMyOrdersQuery, conn))
                        {
                            cmd.ExecuteNonQuery();
                        }

                        // 2. 直接清空投票表
                        using (SqlCommand cmd = new SqlCommand(clearMyVotesQuery, conn))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch (Exception ex)
                    {
                        // 🎯 如果資料庫卡住，直接把錯誤寫在畫面上方便除錯
                        ViewBag.Message = "資料庫清理失敗，原因：" + ex.Message;
                        return View();
                    }
                }
                // ========================================================

                // 兩張表清空成功，完美跳轉到點餐大廳
                return RedirectToAction("Index", "GroupBuy");
            }

            // 密碼或帳號不對
            ViewBag.Message = "帳號或密碼錯誤！";
            return View();
        }

        // 4. 安全登出功能 (GET: /Account/Logout)
        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // 清除所有的 Session 紀錄
            return RedirectToAction("Login");
        }
    }
}