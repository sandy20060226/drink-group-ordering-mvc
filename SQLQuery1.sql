@model Homework.Controllers.OrderItem[]
@{
    Layout = null;
}

<!DOCTYPE html>
<html lang="zh-TW">
<head>
    <meta charset="UTF-8">
    <title>揪Drink - 點餐大廳</title>
    <style>
        body { font-family: Arial, sans-serif; background: #f0f4f8; padding: 20px; }
        .container { max-width: 1000px; margin: 0 auto; display: flex; gap: 20px; }
        .card { background: white; padding: 25px; border-radius: 8px; box-shadow: 0 4px 6px rgba(0,0,0,0.05); flex: 1; }
        .form-card { max-width: 350px; }
        h2, h3 { color: #1e3c72; margin-top: 0; }
        select, input[type="number"] { width: 100%; padding: 8px; margin: 5px 0; border: 1px solid #cbd5e0; border-radius: 4px; box-sizing: border-box; }
        .btn { width: 100%; padding: 10px; background: #1e3c72; color: white; border: none; border-radius: 4px; font-size: 16px; cursor: pointer; font-weight: bold; margin-top: 10px; }
        .btn:hover { background: #2a5298; }
        table { width: 100%; border-collapse: collapse; margin-top: 15px; }
        th, td { padding: 12px; text-align: left; border-bottom: 1px solid #e2e8f0; }
        th { background-color: #f7fafc; color: #4a5568; }
        .btn-danger { background: #e53e3e; color: white; padding: 5px 10px; border-radius: 4px; text-decoration: none; font-size: 14px; }
        .btn-success { background: #38a169; color: white; padding: 5px 10px; border-radius: 4px; border: none; cursor: pointer; font-size: 14px; }
    </style>
</head>
<body>

<div class="container">
    
    <div class="card form-card">
        <h3>點餐！</h3>
        <form action="/GroupBuy/CreateOrder" method="post">
            <label>選擇飲料：</label>
            <select id="drinkSelect" name="DrinkName" onchange="updatePrice()" required>
                <option value="" data-price="0">-- 請選擇飲料 --</option>
                <option value="珍珠奶茶" data-price="65">珍珠奶茶 ($65)</option>
                <option value="大麥紅茶" data-price="40">大麥紅茶 ($40)</option>
                <option value="四季春茶" data-price="45">四季春茶 ($45)</option>
                <option value="黑糖珍珠鮮奶" data-price="70">黑糖珍珠鮮奶 ($70)</option>
            </select>

            <div style="margin-top: 15px; margin-bottom: 15px;">
                <label style="font-weight: bold;">金額(可以自訂喔！)：</label>
                <input type="number" id="priceInput" name="Price" placeholder="金額將自動帶入" required />
            </div>

            <button type="submit" class="btn">確認點餐！🚀</button>
        </form>
    </div>

    <div class="card">
        <h3>📋 當前點餐清單</h3>
        <table>
            <thead>
                <tr>
                    <th>品項</th>
                    <th>點餐人</th>
                    <th>金額</th>
                    <th>操作</th>
                </tr>
            </thead>
            <tbody>
                @foreach (var item in Model)
                {
                    <tr>
                        <form action="/GroupBuy/UpdateOrder" method="post">
                            <input type="hidden" name="Id" value="@item.Id" />
                            
                            <td>
                                <select name="UpdateDrinkName" style="width: auto;">
                                    <option value="珍珠奶茶" @(item.DrinkName == "珍珠奶茶" ? "selected" : "")>珍珠奶茶</option>
                                    <option value="大麥紅茶" @(item.DrinkName == "大麥紅茶" ? "selected" : "")>大麥紅茶</option>
                                    <option value="四季春茶" @(item.DrinkName == "四季春茶" ? "selected" : "")>四季春茶</option>
                                    <option value="黑糖珍珠鮮奶" @(item.DrinkName == "黑糖珍珠鮮奶" ? "selected" : "")>黑糖珍珠鮮奶</option>
                                </select>
                            </td>
                            <td>@item.BuyerName</td>
                            <td>
                                <input type="number" name="UpdatePrice" value="@item.Price" style="width: 70px; margin: 0;" />
                            </td>
                            <td>
                                <button type="submit" class="btn-success">儲存</button>
                                <a href="#" class="btn-danger">取消</a>
                            </td>
                        </form>
                    </tr>
                }
            </tbody>
        </table>
    </div>

</div>

<script>
function updatePrice() {
    var drinkSelect = document.getElementById("drinkSelect");
    var priceInput = document.getElementById("priceInput");
    
    var selectedOption = drinkSelect.options[drinkSelect.selectedIndex];
    var price = selectedOption.getAttribute("data-price");
    
    // 如果大於 0 就自動帶入價錢
    if(price > 0) {
        priceInput.value = price;
    } else {
        priceInput.value = "";
    }
}
</script>

</body>
</html>