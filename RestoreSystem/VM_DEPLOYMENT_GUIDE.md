# RestoreSystem VM 部署完整指南

## 📋 情況說明

您的虛擬機是**全新環境**，只有 `C:\RestoreSystem\Logs` 資料夾，沒有任何程式檔案。

---

## ✅ 完整部署流程

### **階段 1：在開發機上打包（您目前的電腦）**

1. **開啟 PowerShell 或 CMD**
   ```cmd
   cd D:\RestoreUI\RestoreSystem
   ```

2. **執行打包腳本**
   ```cmd
   .\Build-DeployPackage.bat
   ```

3. **等待完成**，應該會看到：
   ```
   ============================================
   部署包建立完成！
   ============================================
   輸出位置：D:\RestoreUI\RestoreSystem\Deploy
   ```

4. **檢查輸出檔案**
   - 前往 `D:\RestoreUI\RestoreSystem\Deploy`
   - 應該包含：
     ```
     Deploy/
     ├── Service/               ← 服務檔案
     ├── UI/                    ← UI 檔案
     ├── Install-VM.bat         ← VM 一鍵安裝腳本 ⭐
     ├── Check-DotNetRuntime.bat ← .NET Runtime 檢查
     ├── Create-Shortcut.bat    ← 建立桌面捷徑
     └── README.txt             ← 說明文件
     ```

---

### **階段 2：傳輸到虛擬機**

選擇以下任一方式：

#### **方式 A：共用資料夾**
1. 在 VMware/Hyper-V 設定共用資料夾
2. 將 `Deploy` 資料夾複製到共用位置

#### **方式 B：網路共用**
1. 在開發機建立網路共用
2. 從 VM 存取並複製

#### **方式 C：壓縮傳輸**
1. 將 `Deploy` 資料夾壓縮成 ZIP
2. 透過網路或 USB 傳輸到 VM
3. 在 VM 上解壓縮

---

### **階段 3：在虛擬機上安裝**

#### **步驟 1：檢查 .NET Runtime**

1. 在 VM 上，進入 `Deploy` 資料夾
2. **執行** `Check-DotNetRuntime.bat`
3. 如果未安裝，選擇：
   - **選項 1**：自動下載安裝（推薦）
   - **選項 2**：手動下載

#### **步驟 2：執行一鍵安裝**

1. **以系統管理員身分**執行 `Install-VM.bat`
   - 右鍵 → 以系統管理員身分執行

2. 腳本會自動：
   - ✅ 檢查 .NET Runtime
   - ✅ 建立 `C:\RestoreSystem` 目錄結構
   - ✅ 複製所有檔案
   - ✅ 建立預設設定檔（VM 模式已啟用）
   - ✅ 安裝並啟動服務

3. 看到以下訊息表示成功：
   ```
   ============================================
   安裝完成！
   ============================================
   服務狀態：RUNNING
   ```

#### **步驟 3：建立桌面捷徑（可選）**

```cmd
.\Create-Shortcut.bat
```

---

### **階段 4：測試運行**

1. **執行 UI**
   - 雙擊桌面捷徑
   - 或執行 `C:\RestoreSystem\UI\RestoreSystem.UI.exe`

2. **首次登入**
   - 輸入您想設定的密碼
   - 點擊 Login

3. **檢查 VM 偵測**
   - Shift + Click 進入 Settings
   - 查看「偵測結果」應該顯示：
     ```
     ✓ 偵測到虛擬機環境：VMware (或 Hyper-V)
     ```

4. **測試啟用保護**
   - 前往 Protection 面板
   - 點擊「Enable Protection」
   - **應該在 1-2 秒內完成** ✅
   - **不會出現逾時錯誤**

---

## 📁 安裝後的檔案結構

```
C:\RestoreSystem\
├── Service\
│   ├── RestoreSystem.Service.exe     ← 服務主程式
│   ├── RestoreSystem.Core.dll
│   ├── RestoreSystem.Shared.dll
│   └── ... (其他 DLL)
├── UI\
│   ├── RestoreSystem.UI.exe          ← UI 主程式
│   ├── RestoreSystem.Core.dll
│   └── ... (其他檔案)
├── Logs\
│   └── operation.log                 ← 運行日誌
└── config.json                       ← 設定檔
```

---

## 🔧 故障排除

### 問題 1：找不到 Deploy 資料夾

**解決**：確認 `Build-DeployPackage.bat` 成功執行完成

### 問題 2：VM 上無法安裝 .NET Runtime

**解決**：
1. 手動下載離線安裝包
2. 網址：https://dotnet.microsoft.com/download/dotnet/8.0
3. 選擇「Desktop Runtime」Windows x64 版本

### 問題 3：服務啟動失敗

**檢查**：
```cmd
sc query RestoreSystemService
```

**查看日誌**：
```cmd
type C:\RestoreSystem\Logs\operation.log
```

### 問題 4：UI 無法連線到服務

**確認服務運行**：
```powershell
Get-Service RestoreSystemService
```

**重新啟動服務**：
```cmd
sc stop RestoreSystemService
sc start RestoreSystemService
```

---

## 🎯 完成後的驗證

- [ ] Service 正在運行（`sc query RestoreSystemService`）
- [ ] UI 可以正常啟動
- [ ] 可以登入管理模式
- [ ] Settings 顯示 VM 偵測結果
- [ ] Enable Protection 在 2 秒內完成
- [ ] Dashboard 顯示「Protection: ENABLED」

---

## 📞 需要協助？

如果遇到問題，請提供：

1. **錯誤訊息截圖**
2. **服務狀態**：
   ```cmd
   sc query RestoreSystemService
   ```
3. **日誌內容**：
   ```cmd
   type C:\RestoreSystem\Logs\operation.log
   ```
4. **.NET 版本**：
   ```cmd
   dotnet --list-runtimes
   ```

---

## 🚀 快速命令參考

```cmd
# 建立部署包（開發機）
cd D:\RestoreUI\RestoreSystem
.\Build-DeployPackage.bat

# 檢查 .NET（VM）
.\Check-DotNetRuntime.bat

# 安裝（VM，管理員）
.\Install-VM.bat

# 建立捷徑（VM）
.\Create-Shortcut.bat

# 查看服務狀態
sc query RestoreSystemService

# 重啟服務
sc stop RestoreSystemService
sc start RestoreSystemService

# 執行 UI
C:\RestoreSystem\UI\RestoreSystem.UI.exe
```
