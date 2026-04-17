# RestoreSystem 服務安裝與測試指南

## ⚠️ 您遇到的問題

**錯誤訊息**：「無法與服務連訊：信號等待逾時」

**根本原因**：RestoreSystem Service **未安裝或未運行**，所以 UI 無法透過 Named Pipe 連線到服務。

---

## ✅ 解決方案

### **方案 A：快速測試（推薦用於開發）**

使用主控台模式直接運行 Service，不需要安裝為 Windows 服務。

#### 步驟：
1. 開啟 PowerShell 或命令提示字元（**一般權限即可**）
2. 切換到專案目錄：
   ```cmd
   cd D:\RestoreUI\RestoreSystem
   ```
3. 執行測試腳本：
   ```cmd
   .\RestoreSystem.Service\Run-Debug.bat
   ```
4. 看到「Named Pipe 啟動」訊息後，保持此視窗開啟
5. 開啟另一個視窗，執行 RestoreSystem.UI
6. 現在應該可以正常連線了！

**優點**：
- ✅ 不需要管理員權限
- ✅ 可以看到即時日誌輸出
- ✅ 容易除錯
- ✅ Ctrl+C 即可停止

**缺點**：
- ❌ 關閉視窗服務就停止
- ❌ 需要手動啟動

---

### **方案 B：安裝為 Windows 服務（正式環境）**

將 Service 安裝為 Windows 服務，開機自動啟動。

#### 步驟：
1. **以系統管理員身分**開啟命令提示字元
   - 右鍵「命令提示字元」→ 以系統管理員身分執行
   
2. 切換到專案目錄：
   ```cmd
   cd D:\RestoreUI\RestoreSystem
   ```

3. 執行安裝腳本：
   ```cmd
   .\RestoreSystem.Service\Install-Service.bat
   ```

4. 等待安裝完成，應該會看到：
   ```
   建立服務成功！
   啟動服務成功！
   服務狀態：RUNNING
   ```

5. 執行 RestoreSystem.UI 測試連線

**優點**：
- ✅ 開機自動啟動
- ✅ 背景運行
- ✅ 穩定可靠

**缺點**：
- ❌ 需要管理員權限
- ❌ 查看日誌較不方便

---

### **方案 C：手動使用 sc 命令**

```powershell
# 1. 編譯專案
dotnet build D:\RestoreUI\RestoreSystem\RestoreSystem.Service\RestoreSystem.Service.csproj -c Release

# 2. 建立服務
sc create RestoreSystemService binPath= "D:\RestoreUI\RestoreSystem\RestoreSystem.Service\bin\Release\net8.0\RestoreSystem.Service.exe" start= auto

# 3. 啟動服務
sc start RestoreSystemService

# 4. 查看狀態
sc query RestoreSystemService
```

---

## 🔧 故障排除

### 問題：仍然逾時

1. **確認服務正在運行**：
   ```powershell
   Get-Service RestoreSystemService
   # 或
   sc query RestoreSystemService
   ```

2. **檢查 Named Pipe 是否啟動**：
   - 如果使用 Run-Debug.bat，應該會看到：
     ```
     Named Pipe 啟動：\\.\pipe\RestoreSystemPipe
     ```

3. **檢查防火牆**：
   - Named Pipe 通常不受防火牆影響，但請確認

4. **查看日誌**：
   - 檢查 `C:\RestoreSystem\Logs\operation.log`（如果有）
   - 使用 Run-Debug.bat 可直接看到輸出

### 問題：編譯失敗

1. **確認 .NET 8 SDK 已安裝**：
   ```powershell
   dotnet --version
   ```
   應該顯示 8.x.x

2. **清理並重新建置**：
   ```powershell
   dotnet clean
   dotnet build
   ```

### 問題：VM 設定未生效

1. **確認 config.json 存在**：
   ```powershell
   Get-Content C:\RestoreSystem\config.json
   ```

2. **確認包含 VM 設定**：
   ```json
   {
     "ForceVmSafeMode": true,
     "AutoDetectVirtualMachine": true
   }
   ```

3. **重新啟動服務**：
   ```powershell
   sc stop RestoreSystemService
   sc start RestoreSystemService
   ```

---

## 📋 測試清單

完成以下步驟確認一切正常：

- [ ] Service 正在運行（使用 `sc query` 或 Run-Debug.bat）
- [ ] config.json 存在於 `C:\RestoreSystem\`
- [ ] config.json 包含 VM 設定
- [ ] 啟動 RestoreSystem.UI
- [ ] 登入管理模式
- [ ] 前往 Settings → 確認 VM 偵測結果
- [ ] 前往 Protection → 點擊「Enable Protection」
- [ ] **應該在 1-2 秒內完成，不會逾時** ✅

---

## 🎯 下一步

安裝並啟動服務後：

1. **測試基本功能**
   - 啟用/停用保護
   - 建立快照
   - 查看服務狀態

2. **驗證 VM 模式**
   - 確認在 VM 環境下不會逾時
   - 檢查日誌是否顯示「VM 安全模式」

3. **部署到生產環境**
   - 使用 Install-Service.bat 安裝
   - 設定為自動啟動
   - 測試重開機後自動運行

---

## 📞 需要協助？

如果仍然無法解決，請提供：
1. `sc query RestoreSystemService` 的輸出
2. Run-Debug.bat 的主控台輸出
3. `C:\RestoreSystem\config.json` 的內容
4. UI 的完整錯誤訊息
