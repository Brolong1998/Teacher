# ClassRoom Control 1.1

局域网课堂任务系统：ASP.NET Core 服务端 + 教师 Web 管理台 + Windows WPF 学生端。

## 当前功能
- 教师端创建、发布课堂任务
- 学生端登记设备名称并每 10 秒发送心跳
- 学生端接收课堂任务并提交文字作业
- 教师端查看在线/离线状态及提交情况
- 默认 30 秒无心跳标记离线
- 学生端首次成功连接后保存教师服务器地址和设备名称；下次启动自动读取并尝试连接

> 本版本是课堂任务 MVP，不是防逃课/强制锁定软件。普通窗口或全屏窗口不能可靠阻止 Alt+Tab、任务管理器、关机或其他系统操作。正式课堂限制应由学校管理员通过 Windows Assigned Access、组策略或受管设备方案配置，并保留紧急恢复方式。

## 目录
- `src/ClassRoom.Server`：ASP.NET Core 服务端和教师后台
- `src/ClassRoom.Student`：Windows WPF 学生端
- `.github/workflows/build-windows.yml`：GitHub Actions Windows 编译工作流

## 环境
- .NET 11 SDK
- 教师主机与学生电脑需在同一局域网，防火墙允许 TCP 5080

## 1. 启动教师服务端
在教师主机（Windows 或 Mac）安装 .NET 11 SDK，在项目根目录运行：

```bash
cd src/ClassRoom.Server
dotnet restore
dotnet run --urls http://0.0.0.0:5080
```

浏览器打开 `http://localhost:5080`。学生电脑使用教师主机的局域网 IPv4 地址，例如 `http://192.168.1.100:5080`。

服务端默认教师密钥为 `change-this-teacher-key`。正式使用前必须修改。macOS/Linux:
```bash
Teacher__ApiKey="请替换为足够长的随机密钥" dotnet run --urls http://0.0.0.0:5080
```

Windows PowerShell:
```powershell
$env:Teacher__ApiKey = "请替换为足够长的随机密钥"
dotnet run --urls http://0.0.0.0:5080
```

## 2. 获取 Windows 学生端
1. 打开 GitHub 仓库的 Actions 页面。
2. 选择 **Build Windows Student EXE** 工作流，点击 **Run workflow**；提交学生端或服务端代码时也会自动构建。
3. 构建成功后打开该次运行，在 Artifacts 下载 `Student-win-x64`。
4. 解压后运行 `ClassRoom.Student.exe`（实际文件名以构建产物为准）。

工作流使用 .NET 11，在 Windows runner 上发布 win-x64、self-contained、single-file。目标是学生电脑不必另装 .NET Runtime；请以 Actions 发布目录中的实际产物为准。

## 3. 学生端连接
首次运行时，在“服务器地址”填入教师主机地址，例如 `http://192.168.1.100:5080`，设备名称可保留电脑名，然后点击“连接课堂”。连接成功后会保存设置到当前 Windows 用户的 `%AppData%\ClassRoomStudent\settings.json`，以后启动会自动尝试连接。若教师主机 IP 改变，可重新输入并连接。

## 4. 局域网部署
1. 在教师主机运行服务端。
2. 用 `ipconfig`（Windows）查教师主机 IPv4。
3. 由学校 IT 管理员允许专用网络 TCP 5080 入站。
4. 学生端填写教师主机地址。
5. 先用 2 台测试机验证，再扩展到全班。

## 安全与限制
- 不要把服务器直接暴露到互联网。
- 必须更改默认教师 API Key。
- 建议将教师管理台限制在教师网络/教师设备，并由学校管理员配置防火墙。
- 当前版本暂未加入 HTTPS、学生账户、设备注册审批、文件上传、审计日志和防篡改能力。
- 数据目前保存在服务端内存中，服务端重启后任务、设备状态和提交记录会丢失。
- “离线”表示服务端在超时内未收到心跳，可能是关机、断网、程序退出或崩溃；无法仅凭心跳判断具体原因。
