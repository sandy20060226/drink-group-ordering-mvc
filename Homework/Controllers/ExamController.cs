using Exam.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;

namespace Exam.Controllers // ⚠️ 記得改成你的專案名稱
{
    public class ExamController : Controller
    {

        // =================================================================
        // 🔑 項目一：全域資料庫連線字串
        // =================================================================
        private readonly string _connectionString;

        public ExamController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("尚未設定資料庫連線。");
        }




        // =================================================================
        // 🔒 項目二：登入驗證處理 Action (接收 htmlexam.html 表單)
        // =================================================================
        [HttpPost]
        public ActionResult Login(IFormCollection form)
        {
            string user = form["txtUser"];
            string pwd = form["txtPwd"];

            // 預設登入帳密：sandy / 0226
            if (user == "sandy" && pwd == "0226")
            {
                HttpContext.Session.SetString("IsLogin", "Yes");
                return RedirectToAction("Index");
            }
            else
            {
                return Redirect("/htmlexam.html?error=1");
            }
        }




        // =================================================================
        // 📥 項目三：主功能列表 Action (核心：安全驗證 + 三條件複合查詢 + 分頁)
        // =================================================================
        // 💡 密技：要多查一個下拉選單，就在方法參數最後面直接多加一個接收變數（例如：string employeeSelection = ""）
        public ActionResult Index(int page = 1, string searchString = "", string shipViaSelection = "", string employeeSelection = "")
        {

            // -------------------------------------------------------------
            // 🔒 關卡 A：安全檢查鎖頭 (防偷看機制)
            // -------------------------------------------------------------
            if (HttpContext.Session.GetString("IsLogin") != "Yes")
            {
                return Redirect("/htmlexam.html?error=auth");
            }


            // -------------------------------------------------------------
            // 📐 關卡 B：分頁參數初始化
            // -------------------------------------------------------------
            int pageSize = 5;
            int skipRows = (page - 1) * pageSize;
            List<ExamModel> orderList = new List<ExamModel>();
            int totalRows = 0;


            // -------------------------------------------------------------
            // 🔍 關卡 C：動態 SQL 語句拼接 (基礎外殼)
            // -------------------------------------------------------------
            string countSql = "SELECT COUNT(*) FROM TableFinalExams1131619 WHERE 1=1";
            string pageSql = "SELECT * FROM TableFinalExams1131619 WHERE 1=1";


            // 🌟 條件一：文字框查詢【送貨地址】
            if (!string.IsNullOrEmpty(searchString))
            {
                countSql += " AND 送貨地址 LIKE @Search";
                pageSql += " AND 送貨地址 LIKE @Search";
            }


            // 🌟 條件二：下拉選單一【送貨方式】
            if (!string.IsNullOrEmpty(shipViaSelection))
            {
                countSql += " AND 送貨方式 = @ShipVia";
                pageSql += " AND 送貨方式 = @ShipVia";
            }


            // 🌟 條件三：下拉選單二【員工編號】(這是示範多條件時額外追加的！)
            // 💡 考試注意：你想加幾個條件，就按照這個格式一直複製往下貼，完全不會打架！
            if (!string.IsNullOrEmpty(employeeSelection))
            {
                countSql += " AND 員工編號 = @EmpId";
                pageSql += " AND 員工編號 = @EmpId";
            }


            // 串接分頁排序語法 (這行一定要乖乖待在所有 AND 條件的最尾端)
            pageSql += " ORDER BY 訂單號碼 OFFSET @Skip ROWS FETCH NEXT @PageSize ROWS ONLY";


            // -------------------------------------------------------------
            // 🛢️ 關卡 D：正式連線資料庫讀取資料
            // -------------------------------------------------------------
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                try
                {
                    conn.Open();

                    // 1️⃣ 執行第一個語法：計算「同時符合上面所有條件」的總筆數
                    using (SqlCommand countCmd = new SqlCommand(countSql, conn))
                    {
                        if (!string.IsNullOrEmpty(searchString))
                        {
                            countCmd.Parameters.AddWithValue("@Search", "%" + searchString + "%");
                        }
                        if (!string.IsNullOrEmpty(shipViaSelection))
                        {
                            countCmd.Parameters.AddWithValue("@ShipVia", shipViaSelection);
                        }
                        if (!string.IsNullOrEmpty(employeeSelection))
                        {
                            // 額外追加的參數綁定
                            countCmd.Parameters.AddWithValue("@EmpId", employeeSelection);
                        }

                        totalRows = Convert.ToInt32(countCmd.ExecuteScalar());
                    }

                    // 2️⃣ 執行第二個語法：撈出當頁分頁資料
                    using (SqlCommand cmd = new SqlCommand(pageSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Skip", skipRows);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);

                        if (!string.IsNullOrEmpty(searchString))
                        {
                            cmd.Parameters.AddWithValue("@Search", "%" + searchString + "%");
                        }
                        if (!string.IsNullOrEmpty(shipViaSelection))
                        {
                            cmd.Parameters.AddWithValue("@ShipVia", shipViaSelection);
                        }
                        if (!string.IsNullOrEmpty(employeeSelection))
                        {
                            // 額外追加的參數綁定
                            cmd.Parameters.AddWithValue("@EmpId", employeeSelection);
                        }

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ExamModel order = new ExamModel();

                                order.OrderId = Convert.ToInt32(reader["訂單號碼"]);
                                order.CustomerId = reader["客戶編號"].ToString();
                                order.EmployeeId = Convert.ToInt32(reader["員工編號"]);
                                order.ShipVia = Convert.ToInt32(reader["送貨方式"]);
                                order.ShipName = reader["收貨人"].ToString();
                                order.ShipAddress = reader["送貨地址"].ToString();
                                order.ShipCity = reader["送貨城市"].ToString();
                                order.ShipRegion = reader["送貨行政區"].ToString();
                                order.ShipPostalCode = reader["送貨郵遞區號"].ToString();

                                orderList.Add(order);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    return Content("資料庫讀取失敗：" + ex.Message);
                }
            }


            // -------------------------------------------------------------
            // 🧮 關卡 E：計算總頁數與狀態傳遞回前端
            // -------------------------------------------------------------
            int totalPages = (int)Math.Ceiling((double)totalRows / pageSize);
            if (totalPages == 0) totalPages = 1;

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.SearchString = searchString;
            ViewBag.ShipViaSelection = shipViaSelection;

            // 🌟 記得要把新條件的狀態也用 ViewBag 傳回前端，這樣下拉選單才不會換頁就跑掉
            ViewBag.EmployeeSelection = employeeSelection;




            // =================================================================
            // 📊 項目四：圖表區數據撈取 (GROUP BY 機制)
            // =================================================================
            List<string> chartLabels = new List<string>();
            List<int> chartCounts = new List<int>();

            string chartSql = "SELECT 送貨方式, COUNT(*) AS 總數 FROM TableFinalExams1131619 GROUP BY 送貨方式";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                try
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(chartSql, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                chartLabels.Add(reader["送貨方式"].ToString() + "號方式");
                                chartCounts.Add(Convert.ToInt32(reader["總數"]));
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    chartLabels.Add("載入失敗");
                    chartCounts.Add(0);
                }
            }

            ViewBag.ChartLabels = chartLabels;
            ViewBag.ChartCounts = chartCounts;


            // -------------------------------------------------------------
            // 🚀 最終站：回傳資料模型給網頁畫面
            // -------------------------------------------------------------
            return View(orderList);
        }
    }
}