# UI 當機問題修復說明

## 🐛 問題描述

UI 顯示「沒有回應」並凍結，特別是在以下情況：
- 啟動時嘗試連線到服務
- 點擊「Enable Protection」等按鈕
- RefreshStatus 查詢服務狀態時

## 🔍 根本原因

### 主要問題：UI 主執行緒阻塞

```csharp
// ❌ 錯誤：在 UI 主執行緒中同步呼叫
private void RefreshStatus()
{
    var status = PipeClient.SendAuthenticated(_authToken, RestoreCommand.Status);
    // 如果服務未回應，UI 會凍結 4-30 秒
}
```

當服務未運行或回應緩慢時：
1. `PipeClient.Connect` 阻塞等待 timeout
2. UI 主執行緒被鎖住
3. 視窗無法重繪，顯示「沒有回應」
4. 使用者體驗極差

---

## ✅ 修復方案

### 1. 改用非同步呼叫

```csharp
// ✅ 正確：在背景執行緒執行
private async Task RefreshStatusAsync()
{
    var status = await Task.Run(() => 
        PipeClient.SendAuthenticated(_authToken, RestoreCommand.Status, timeoutMs: 5000)
    );
    // UI 主執行緒不會被阻塞
}
```

### 2. 縮短 Status 查詢的 timeout

- **一般命令**：30 秒（因為可能需要執行 VHD/BCD 操作）
- **Status 查詢**：5 秒（只是查詢狀態，不應該太久）

### 3. 移除煩人的錯誤彈窗

```csharp
// 之前：每次查詢失敗都彈窗
MessageBox.Show("無法取得服務狀態：" + ex.Message, "警告", ...);

// 現在：只記錄到 Debug，不打擾使用者
System.Diagnostics.Debug.WriteLine($"服務狀態查詢失敗: {ex.Message}");
```

---

## 📝 修改的方法

| 原方法 | 新方法 | 變更 |
|--------|--------|------|
| `RefreshStatus()` | `RefreshStatusAsync()` | 非同步 + 5秒 timeout |
| `RefreshSnapshots()` | `RefreshSnapshotsAsync()` | 非同步 + 5秒 timeout |
| `ExecuteRawCommand()` | `ExecuteRawCommand()` (async void) | 非同步執行 |

---

## 🎯 預期效果

### 修復前 ❌
- UI 啟動時凍結 4-30 秒
- 點擊按鈕後介面卡死
- 強制結束才能關閉視窗

### 修復後 ✅
- UI 立即回應
- 即使服務未運行，介面仍可正常操作
- 背景靜默查詢，不打擾使用者
- 更好的使用者體驗

---

## 🧪 測試方式

### 測試 1：服務未運行時

1. 確認 RestoreSystemService 未運行
2. 啟動 UI
3. **預期結果**：
   - UI 立即顯示，不會凍結
   - Dashboard 顯示「SERVICE_OFFLINE」
   - 可以正常操作其他功能

### 測試 2：服務回應緩慢時

1. 啟動服務但讓它卡在某個操作
2. 點擊「Enable Protection」
3. **預期結果**：
   - UI 仍然可以拖曳/最小化
   - 顯示處理中的提示
   - 最多等待 30 秒後顯示逾時

### 測試 3：正常運作時

1. 服務正常運行
2. 測試所有功能
3. **預期結果**：
   - 所有操作流暢
   - 狀態即時更新
   - 無凍結或延遲

---

## 💡 最佳實踐

### WPF UI 執行緒規則

```csharp
// ❌ 不要在 UI 執行緒中執行長時間操作
private void Button_Click(object sender, RoutedEventArgs e)
{
    Thread.Sleep(5000); // 會凍結 UI
}

// ✅ 使用 async/await
private async void Button_Click(object sender, RoutedEventArgs e)
{
    await Task.Delay(5000); // UI 不會凍結
}

// ✅ 使用 Task.Run 執行背景工作
private async void Button_Click(object sender, RoutedEventArgs e)
{
    var result = await Task.Run(() => LongRunningOperation());
}
```

---

## 🔧 進一步改進（未來可選）

### 1. 加入 Loading 指示器

```csharp
private async void BtnEnable_Click(object sender, RoutedEventArgs e)
{
    LoadingOverlay.Visibility = Visibility.Visible; // 顯示 Loading
    try
    {
        await ExecuteAdminCommandAsync(...);
    }
    finally
    {
        LoadingOverlay.Visibility = Visibility.Collapsed; // 隱藏 Loading
    }
}
```

### 2. 使用 CancellationToken

```csharp
private CancellationTokenSource _cts;

private async void BtnEnable_Click(object sender, RoutedEventArgs e)
{
    _cts = new CancellationTokenSource();
    try
    {
        await ExecuteAdminCommandAsync(..., _cts.Token);
    }
    catch (OperationCanceledException)
    {
        MessageBox.Show("操作已取消");
    }
}

private void BtnCancel_Click(object sender, RoutedEventArgs e)
{
    _cts?.Cancel(); // 取消進行中的操作
}
```

### 3. 重試機制

```csharp
private async Task<string> SendWithRetryAsync(string command, int maxRetries = 3)
{
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            return await Task.Run(() => PipeClient.Send(command));
        }
        catch (TimeoutException) when (i < maxRetries - 1)
        {
            await Task.Delay(1000 * (i + 1)); // 指數退避
        }
    }
    throw new TimeoutException("重試失敗");
}
```

---

## 📌 總結

**核心原則**：
- ✅ 永遠不要在 UI 主執行緒中執行長時間操作
- ✅ 使用 `async/await` 處理 I/O 操作
- ✅ 使用 `Task.Run` 執行 CPU 密集操作
- ✅ 適當設定 timeout，避免無限等待
- ✅ 友善的錯誤處理，不要過度打擾使用者

**已修復**：
- ✅ UI 不再凍結
- ✅ 更短的 timeout 用於狀態查詢
- ✅ 背景執行所有網路/服務通訊
- ✅ 移除煩人的錯誤彈窗

**使用者體驗**：
- 😊 介面永遠回應迅速
- 😊 即使服務離線也能正常操作
- 😊 更流暢的互動體驗
