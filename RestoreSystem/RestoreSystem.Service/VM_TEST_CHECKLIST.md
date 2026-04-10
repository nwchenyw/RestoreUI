# VM 逾時修復測試清單

## 📋 測試前準備

- [ ] 確認已重新編譯整個方案
- [ ] 停止現有的 RestoreSystem Service
- [ ] 備份 `C:\RestoreSystem\config.json`（如果存在）

---

## ✅ 測試步驟（VM 環境）

### 測試 1：自動偵測 VM 模式

1. [ ] 啟動 RestoreUI
2. [ ] 登入管理模式
3. [ ] Shift + Click 進入 Settings
4. [ ] 確認 ☑ **自動偵測虛擬機** 已勾選
5. [ ] 查看偵測結果，應顯示：
   - ✓ 「偵測到虛擬機環境：[VM類型]」（綠色）
6. [ ] 重新啟動服務（或重新安裝）
7. [ ] 前往 Protection 面板
8. [ ] 點擊「Enable Protection」
9. [ ] **預期結果**：
   - ✅ 應該在 1-2 秒內完成
   - ✅ 顯示「已啟用保護模式」
   - ✅ 不會出現逾時錯誤

### 測試 2：強制 VM 安全模式

1. [ ] 進入 Settings
2. [ ] 勾選 ☑ **強制 VM 安全模式**
3. [ ] 點擊儲存提示後的「確定」
4. [ ] 重新啟動服務
5. [ ] 執行「Enable Protection」
6. [ ] **預期結果**：同測試 1

### 測試 3：查看服務日誌

1. [ ] 開啟 `C:\RestoreSystem\Logs\operation.log`
2. [ ] 查找以下訊息：
   ```
   偵測到 VM 環境或啟用強制 VM 安全模式，跳過 VHD/BCD 操作。
   VM 類型: [VMware/Hyper-V/...]
   保護模式已啟用 (VM 安全模式)。
   ```
3. [ ] **預期結果**：應該有上述日誌，且沒有 VHD/BCD 相關錯誤

### 測試 4：檢查服務狀態

1. [ ] 前往 Dashboard
2. [ ] 查看狀態顯示
3. [ ] **預期結果**：
   - Protection: ENABLED
   - Base Disk: VM_MODE 或 BASE_OK
   - 綠色狀態指示燈

---

## ✅ 測試步驟（實體機環境）

### 測試 5：自動偵測實體機

1. [ ] 在實體機上執行 RestoreUI
2. [ ] 進入 Settings
3. [ ] 查看 VM 偵測結果
4. [ ] **預期結果**：
   - ⚠ 「未偵測到虛擬機環境（實體機或偵測失敗）」（橙色）
5. [ ] 執行「Enable Protection」
6. [ ] **預期結果**：
   - 會執行完整的 VHD/BCD 流程（可能耗時 10-30 秒）
   - 最終成功啟用

### 測試 6：強制 VM 模式（在實體機上）

1. [ ] 勾選 ☑ **強制 VM 安全模式**
2. [ ] 儲存並重啟服務
3. [ ] 執行「Enable Protection」
4. [ ] **預期結果**：
   - 即使在實體機上，也會跳過 VHD/BCD 操作
   - 快速完成（1-2 秒）

---

## 🐞 錯誤診斷

### 如果仍然逾時：

1. [ ] 檢查 `config.json` 是否包含：
   ```json
   {
     "AutoDetectVirtualMachine": true,
     "ForceVmSafeMode": true
   }
   ```

2. [ ] 確認服務確實已重新啟動：
   ```powershell
   Get-Service "RestoreSystem Service" | Select-Object Status, StartType
   ```

3. [ ] 查看 Windows 事件檢視器：
   - 應用程式與服務日誌 → RestoreSystem

4. [ ] 手動測試 VM 偵測：
   - 在專案中建立簡單的測試程式
   - 呼叫 `VirtualMachineDetector.IsRunningInVirtualMachine()`

### 如果 UI 沒有顯示 VM 設定：

1. [ ] 確認已重新編譯 RestoreSystem.UI
2. [ ] 確認 MainWindow.xaml 包含新的 CheckBox 控制項
3. [ ] 確認沒有 XAML 編譯錯誤

---

## 📊 成功標準

- ✅ VM 環境下「啟用還原」在 5 秒內完成
- ✅ 不再出現「信號等待逾時」錯誤
- ✅ 服務日誌顯示 VM 模式訊息
- ✅ Dashboard 正確顯示保護狀態
- ✅ 實體機環境仍可正常執行完整流程

---

## 📝 測試記錄

| 日期 | 環境 | 測試項目 | 結果 | 備註 |
|------|------|---------|------|------|
| 2026/04/11 | VMware | 測試 1 | ⏳ | 待測試 |
| | | 測試 2 | ⏳ | 待測試 |
| | 實體機 | 測試 5 | ⏳ | 待測試 |

---

## 🔄 回滾計畫

如果修復失敗，需要回滾：

1. [ ] 還原 `config.json` 備份
2. [ ] 重新部署舊版本的 RestoreSystem Service
3. [ ] 回報問題並提供日誌檔案
