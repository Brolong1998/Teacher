using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Threading;

namespace ClassRoom.Student;

public partial class MainWindow : Window
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(5) };
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(10) };
    private string? _taskId;
    private bool _busy;

    public MainWindow()
    {
        InitializeComponent();
        DeviceBox.Text = Environment.MachineName;
        _timer.Tick += async (_, _) => await CheckInAsync();
    }

    private async void Connect_Click(object sender, RoutedEventArgs e)
    {
        if (!Uri.TryCreate(ServerBox.Text.TrimEnd('/') + "/", UriKind.Absolute, out var uri) ||
            (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            MessageBox.Show("请输入有效服务器地址，例如 http://192.168.1.100:5080");
            return;
        }

        _http.BaseAddress = uri;
        ConnectButton.IsEnabled = false;
        try
        {
            await CheckInAsync();
            _timer.Start();
            StatusText.Text = "已连接教师主机，正在接收课堂任务";
            SubmitButton.IsEnabled = true;
        }
        catch (Exception ex)
        {
            StatusText.Text = "连接失败：" + ex.Message;
            ConnectButton.IsEnabled = true;
        }
    }

    private async Task CheckInAsync()
    {
        if (_busy || _http.BaseAddress is null) return;
        _busy = true;
        try
        {
            await _http.PostAsJsonAsync("api/student/heartbeat",
                new { deviceId = DeviceBox.Text.Trim(), displayName = DeviceBox.Text.Trim() });
            var task = await _http.GetFromJsonAsync<TaskDto>("api/student/task");
            if (task is not null && task.Id != _taskId)
            {
                _taskId = task.Id;
                TaskTitle.Text = task.Title;
                TaskInstructions.Text = task.Instructions;
                SubmissionBox.Clear();
                StatusText.Text = "收到新任务：" + task.Title;
            }
            else if (task is null)
            {
                _taskId = null;
                TaskTitle.Text = "等待教师发布任务";
                TaskInstructions.Text = "目前没有已发布的课堂任务。";
            }
            else StatusText.Text = "连接正常 · " + DateTime.Now.ToString("HH:mm:ss");
        }
        catch (Exception ex)
        {
            StatusText.Text = "连接中断：" + ex.Message;
        }
        finally { _busy = false; }
    }

    private async void Submit_Click(object sender, RoutedEventArgs e)
    {
        if (_http.BaseAddress is null || string.IsNullOrWhiteSpace(_taskId))
        {
            MessageBox.Show("目前没有可提交的任务。");
            return;
        }
        try
        {
            var response = await _http.PostAsJsonAsync("api/student/submission",
                new { deviceId = DeviceBox.Text.Trim(), taskId = _taskId, content = SubmissionBox.Text });
            if (response.IsSuccessStatusCode)
                MessageBox.Show("提交成功！", "课堂任务", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show("提交失败：" + (int)response.StatusCode);
        }
        catch (Exception ex) { MessageBox.Show("提交失败：" + ex.Message); }
    }

    private sealed record TaskDto(string Id, string Title, string Instructions, DateTimeOffset CreatedUtc, bool Published);
}
