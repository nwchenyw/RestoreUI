# 🎉 部署包已準備完成！

## ✅ 已建立的部署包

位置：`D:\RestoreUI\RestoreSystem\Deploy\`

## 📦 包含的檔案

```
Deploy/
├── Service/                    (44 個檔案)
│   ├── RestoreSystem.Service.exe ← 服務主程式
│   └── ... (相依 DLL)
├── UI/                         (12 個檔案)
│   ├── RestoreSystem.UI.exe    ← UI 主程式
│   └── ... (相依檔案)
├── Install-VM.bat              ⭐ VM 一鍵安裝腳本
├── Check-DotNetRuntime.bat     .NET Runtime 檢查工具
├── Create-Shortcut.bat         建立桌面捷徑
├── Install-Service.bat         服務安裝（手動用）
├── Uninstall-Service.bat       服務卸載
├── Run-Debug.bat               偵錯模式
└── README.txt                  說明文件
```

---

## 🚀 VM 部署步驟（3 步驟）

### **步驟 1：傳輸到 VM**

將整個 `Deploy` 資料夾複製到虛擬機（建議位置：`C:\Deploy` 或桌面）

**傳輸方式**：
- VMware 共用資料夾
- 網路共用（\\\\主機\\共用）
- USB 隨身碟
- 壓縮成 ZIP 並透過網路傳輸

---

### **步驟 2：安裝 .NET Runtime（如未安裝）**

在 VM 的 `Deploy` 資料夾中，執行：
```cmd
Check-DotNetRuntime.bat
```

選擇「1」自動下載安裝，或「2」手動下載。

---

### **步驟 3：一鍵安裝**

**以系統管理員身分**執行：
```cmd
Install-VM.bat
```

等待完成，應該會看到：
```
============================================
Installation Complete!
============================================
Service status: RUNNING
```

---

## ✅ 安裝完成後

1. **執行 UI**
   ```
   C:\RestoreSystem\UI\RestoreSystem.UI.exe
   ```
   或執行 `Create-Shortcut.bat` 建立桌面捷徑

2. **設定密碼並登入**

3. **測試啟用保護**
   - 前往 Protection → Enable Protection
   - **應該在 1-2 秒內完成** ✅
   - 不會出現逾時錯誤

4. **檢查 VM 偵測**
   - Settings → 查看「VM 偵測結果」
   - 應顯示：「✓ 偵測到虛擬機環境：VMware」

---

## 🎯 驗證清單

- [ ] Deploy 資料夾已複製到 VM
- [ ] .NET 8 Runtime 已安裝
- [ ] Install-VM.bat 執行成功
- [ ] 服務狀態為 RUNNING
- [ ] UI 可以啟動
- [ ] 可以登入管理模式
- [ ] Enable Protection 在 2 秒內完成
- [ ] VM 模式已正確偵測

---

## 🔧 如果遇到問題

### 問題：服務無法啟動

**檢查**：
```cmd
sc query RestoreSystemService
type C:\RestoreSystem\Logs\operation.log
```

### 問題：UI 無法連線到服務

**確認**：
1. 服務是否正在運行
2. 防火牆是否阻擋
3. config.json 是否正確

### 問題：仍然逾時

**檢查 config.json**：
```json
{
  "ForceVmSafeMode": true,
  "AutoDetectVirtualMachine": true
}
```

確認這兩個設定都是 `true`。

---

## 📞 需要協助？

提供以下資訊：
1. `sc query RestoreSystemService` 的輸出
2. `C:\RestoreSystem\Logs\operation.log` 的內容
3. UI 的完整錯誤訊息截圖

---

**現在可以將 Deploy 資料夾傳輸到 VM 並執行安裝了！** 🎊
