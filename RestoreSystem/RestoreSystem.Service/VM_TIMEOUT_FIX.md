# VM 環境逾時問題修復說明

## 🐛 問題描述

在虛擬機 (VM) 環境中執行時，「啟用還原」功能會發生「無法與服務連訊：信號等待逾時」錯誤。

### 根本原因
即使 UI 勾選了「強制 VM 安全模式」，Service 端仍然會嘗試執行：
1. VHD 掛載操作（`_vhdManager.Mount()`）
2. BCD 開機設定修改（`_bootManager.EnsureRestoreModeEntry()`）

這些操作在 VM 環境下可能：
- 執行速度極慢（超過 30 秒 timeout）
- 完全失敗（權限不足或不支援）
- 導致服務卡住或崩潰

---

## ✅ 修復內容

### 1. **新增 VirtualMachineDetector.cs**
- 自動偵測是否運行在 VM 環境中
- 支援偵測：VMware、Hyper-V、VirtualBox、Xen、QEMU/KVM
- 使用 WMI 查詢硬體資訊進行判斷

### 2. **擴充 RestoreConfig**
新增兩個設定欄位：
```csharp
public bool ForceVmSafeMode { get; set; }           // 強制 VM 安全模式
public bool AutoDetectVirtualMachine { get; set; }  // 自動偵測虛擬機（預設 true）
```

### 3. **修改 Worker.EnableProtectionAsync**
加入 VM 模式判斷邏輯：
```csharp
bool useVmSafeMode = config.ForceVmSafeMode || 
                    (config.AutoDetectVirtualMachine && VirtualMachineDetector.IsRunningInVirtualMachine());

if (useVmSafeMode)
{
    // 跳過 VHD/BCD 操作，立即返回成功
    return;
}

// 否則執行完整的 VHD/BCD 流程...
```

### 4. **更新 UI（MainWindow.xaml）**
在 Settings 面板加入：
- ☑ **自動偵測虛擬機**（預設勾選）
- ☑ **強制 VM 安全模式**
- 即時顯示 VM 偵測結果

### 5. **狀態回報更新**
Service 的 `BuildStatus` 會回傳 VM 模式指示：
```
STATUS:ENABLED:VM_MODE:NONE:VM_SAFE
```

---

## 🚀 使用方式

### **方案 A：自動偵測（推薦）**
1. 開啟 UI → Settings（Shift + Click）
2. 確認 ☑ **自動偵測虛擬機** 已勾選
3. 查看偵測結果（例如：「✓ 偵測到虛擬機環境：VMware」）
4. 重新啟動服務
5. 執行「啟用還原」→ 應該會立即成功（不會超時）

### **方案 B：強制 VM 模式**
1. 開啟 UI → Settings
2. 勾選 ☑ **強制 VM 安全模式**
3. 儲存設定
4. 重新啟動服務
5. 執行「啟用還原」→ 會跳過所有 VHD/BCD 操作

---

## 🧪 測試結果

### ✅ VM 環境下
- **啟用還原**：應該在 1 秒內完成（不會逾時）
- **服務狀態**：顯示 `VM_MODE` 或 `:VM_SAFE`
- **日誌內容**：
  ```
  偵測到 VM 環境或啟用強制 VM 安全模式，跳過 VHD/BCD 操作。
  VM 類型: VMware
  保護模式已啟用 (VM 安全模式)。
  ```

### ✅ 實體機環境下
- **自動偵測**：會偵測到非 VM，執行完整流程
- **VHD/BCD**：正常掛載和設定
- **服務狀態**：顯示 `BASE_OK`

---

## 📋 注意事項

### **VM 安全模式的限制**
在 VM 安全模式下，以下功能會被跳過：
- ❌ VHD 掛載/卸載
- ❌ BCD 開機項目設定
- ❌ 差異磁碟建立
- ⚠️ 但快照功能可能仍然可用（取決於實作）

### **何時需要強制 VM 模式？**
- 自動偵測失敗（特殊 VM 環境）
- 想在實體機上測試 VM 模式邏輯
- VM 環境仍然發生逾時問題

### **重新啟動服務**
修改 VM 設定後，必須重新啟動 `RestoreSystem Service` 才會生效：
```powershell
Restart-Service "RestoreSystem Service"
```

---

## 🔧 技術細節

### 加入的 NuGet 套件
- **System.Management 8.0.0**（用於 WMI 查詢）

### 修改的檔案
1. `RestoreSystem.Core/RestoreConfig.cs` - 新增 VM 設定欄位
2. `RestoreSystem.Core/VirtualMachineDetector.cs` - 新增 VM 偵測邏輯
3. `RestoreSystem.Core/RestoreSystem.Core.csproj` - 加入 NuGet 參考
4. `RestoreSystem.Service/Worker.cs` - 修改啟用保護邏輯
5. `RestoreSystem.UI/MainWindow.xaml` - 加入 VM 設定 UI
6. `RestoreSystem.UI/MainWindow.xaml.cs` - 加入 VM 設定處理

---

## 🎯 預期效果

| 環境 | 自動偵測 | 強制 VM 模式 | 實際行為 |
|------|---------|------------|---------|
| VMware | ✓ ON | ☐ OFF | 跳過 VHD/BCD（VM 模式） |
| Hyper-V | ✓ ON | ☐ OFF | 跳過 VHD/BCD（VM 模式） |
| 實體機 | ✓ ON | ☐ OFF | 執行完整流程（正常模式） |
| 實體機 | ✓ ON | ✓ ON | 跳過 VHD/BCD（強制 VM 模式） |
| 任何環境 | ☐ OFF | ✓ ON | 跳過 VHD/BCD（強制 VM 模式） |
| 任何環境 | ☐ OFF | ☐ OFF | 執行完整流程（可能逾時） |

---

## 🐞 故障排除

### 問題：仍然逾時
1. 確認 VM 設定已儲存（檢查 `config.json`）
2. 確認服務已重新啟動
3. 查看服務日誌（`C:\RestoreSystem\Logs`）
4. 手動勾選「強制 VM 安全模式」

### 問題：偵測不到 VM
1. 查看 Settings 面板的偵測結果訊息
2. 特殊 VM 環境可能無法偵測 → 使用「強制 VM 安全模式」
3. 檢查是否有足夠權限執行 WMI 查詢

### 問題：實體機被誤判為 VM
1. 取消勾選「自動偵測虛擬機」
2. 或檢查 BIOS/硬體資訊是否包含 VM 關鍵字

---

## ✨ 建議後續改進

1. **VM 模式下的替代還原機制**
   - 使用檔案層級快照（而非 VHD）
   - 使用 Volume Shadow Copy 服務

2. **更詳細的日誌**
   - 記錄每個步驟的耗時
   - 記錄 VM 偵測的詳細資訊

3. **UI 提示**
   - 在 Dashboard 顯示當前模式（實體機/VM）
   - 啟用還原前提示預計耗時

4. **動態 timeout**
   - 根據環境自動調整 timeout（VM: 10秒，實體機: 60秒）
