using Microsoft.AspNetCore.Mvc;

namespace Exam.Models
{
    public class ExamModel
    {
        // 🌟 訂單號碼是主鍵 (int)，通常在資料庫設定自動遞增
        public int OrderId { get; set; }
        public string CustomerId { get; set; }
        public int EmployeeId { get; set; }
        public int ShipVia { get; set; }
        public string ShipName { get; set; }
        public string ShipAddress { get; set; }
        public string ShipCity { get; set; }
        public string ShipRegion { get; set; }
        public string ShipPostalCode { get; set; }
    }
}