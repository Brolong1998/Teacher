# ClassRoom Control 1.0

局域网课堂任务系统第一版：ASP.NET Core 服务端 + 教师 Web 管理台 + Windows WPF 学生端。

## 当前版本功能
- 教师端创建、发布课堂任务
- 学生端登记设备名称并定时发送心跳
- 学生端轮询当前任务并提交文字作业
- 教师端查看在线/离线状态及提交情况
- 默认 30 秒无心跳标记离线
- 服务端和教师端可先在 Mac 上开发、运行

> 本版本是可运行的 MVP，不是防逃课/强制锁定软件。普通窗口或全屏窗口不能可靠阻止 Alt+Tab、任务管理器、关机或其他系统操作。正式课堂限制应由学校管理员通过 Windows Assigned Access、组策略或受管设备方案配置，并保留紧急恢复方式。

## 目录
- `src/ClassRoom.Server`：ASP.NET Core 服务端和教师后台
- `src/ClassRoom.Student`：Windows WPF 学生端
- `.github/workflows/build-windows.yml`：GitHub Actions Windows 编译工作流

## 环境
- .NET 11 SDK
- Mac 可运行服务端；学生端为 Windows WPF 应用
- 学生电脑和教师主控电脑需在同一 LAN，且防火墙允许 TCP 5080

## 1. 在 Mac 启动服务端
```bash
cd src/ClassRoom.Server
dotnet restore
dotnet run --urls http://0.0.0.0:5080
```
浏览器打开 `http://localhost:5080`。

服务端默认教师密钥为 `change-this-teacher-key`。正式使用前必须修改：
```bash
Teacher__ApiKey="换成足够长的随机密钥" dotnet run --urls http://0.0.0.0:5080
```
Windows PowerShell:
```powershell
$env:Teacher__ApiKey = "换成足够长的随机密钥"
dotnet run --urls http://0.0.0.0:5080
```

## 2. 学生端连接服务端
在 Windows 上安装 .NET 11 Desktop Runtime（如果使用框架依赖部署）。
打开学生端后，填入服务端地址，例如 `http://192.168.1.100:5080`，再填设备名称，如 `PC-01`。
教师端和学生端必须使用相同的服务器地址。

## 3. Mac 上交叉编译 Windows 学生端（可选）
需要安装 .NET 11 SDK，并启用 Windows targeting：
```bash
cd src/ClassRoom.Student
dotnet restore -p:EnableWindowsTargeting=true
dotnet publish -c Release -r win-x64 --self-contained true \
  -p:EnableWindowsTargeting=true \
  -p:PublishSingleFile=true \
  -o ../../artifacts/student-win-x64
```
如果本机 SDK/Windows Desktop targeting pack 下载失败，请使用仓库内 GitHub Actions 工作流构建。

## 4. GitHub Actions 生成 Windows 发布文件
将整个项目上传到 GitHub，进入 Actions，运行 `Build Windows` 工作流。完成后下载 `classroom-control-windows` artifact。
当前工作流会发布学生端 Windows x64 文件。教师端运行在浏览器中，服务器可部署为 Windows 服务/控制台程序；后续版本再制作教师端安装器。

## 5. 学校局域网部署
1. 在教师主控 Windows 电脑运行服务端。
2. 用 `ipconfig` 查主控电脑 IPv4，例如 `192.168.1.100`。
3. 在教师电脑防火墙中允许专用网络 TCP 5080 入站（由学校 IT 管理员操作）。
4. 学生端输入 `http://192.168.1.100:5080`。
5. 用 2 台测试机验证后，再扩展到全班。

## 安全注意
- 不要把服务器直接暴露到互联网。
- 更改默认教师 API Key。
- 建议将教师管理台限制在教师网络/教师设备，并由学校管理员配置防火墙。
- 此 MVP 暂未加入 HTTPS、用户账户、设备注册审批、文件上传、审计日志和防篡改能力。
- “离线”代表服务端在超时内未收到心跳，可能是关机、断网、程序退出或崩溃；无法仅凭心跳判断具体原因。
