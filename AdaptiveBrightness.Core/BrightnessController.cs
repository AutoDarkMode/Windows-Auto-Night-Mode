using System.Globalization;
using System.IO.Ports;
using System.Diagnostics;

namespace AdaptiveBrightness.Core;

public sealed class BrightnessController : IDisposable
{
    private AppSettings _settings;
    private readonly MonitorController _monitors = new();
    private readonly SynchronizationContext? _uiContext;
    private readonly Timer _reconnectTimer;
    private readonly object _gate = new();
    private int _connecting;
    private SerialPort? _port;
    private Mutex? _controllerMutex;
    private DateTime _lastWriteUtc = DateTime.MinValue;
    private DateTime _lastRampUtc = DateTime.UtcNow;
    private double? _filteredLux;
    private double? _rampedBrightness;
    private bool _disposed;
    private bool _started;
    private bool _ownsControllerMutex;

    public string Status { get; private set; } = "等待 Pico";
    public event EventHandler? StatusChanged;

    public BrightnessController(AppSettings settings)
    {
        _settings = settings;
        _uiContext = SynchronizationContext.Current;
        _reconnectTimer = new Timer(_ => EnsureConnected(), null, Timeout.Infinite, Timeout.Infinite);
    }

    public void Start()
    {
        lock (_gate)
        {
            if (_started || _disposed) return;
            _started = true;
            if (!_settings.BrightnessAutomationEnabled)
            {
                SetStatus("自动亮度调节已关闭");
                return;
            }
            try
            {
                var legacyProcesses = Process.GetProcessesByName("AdaptiveBrightness.Windows");
                try
                {
                    if (legacyProcesses.Any(process => process.Id != Environment.ProcessId))
                    {
                        SetStatus("旧版托盘控制器仍在运行；为避免争用 Pico CDC，当前亮度控制已暂停");
                        return;
                    }
                }
                finally { foreach (var process in legacyProcesses) process.Dispose(); }
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception) { }

            try
            {
                _controllerMutex = new Mutex(initiallyOwned: true, name: @"Local\AdaptiveBrightness.CdcController", out var createdNew);
                if (!createdNew)
                {
                    _controllerMutex.Dispose();
                    _controllerMutex = null;
                    SetStatus("另一个 Adaptive Brightness 控制器正在运行；当前亮度控制已暂停");
                    return;
                }
                _ownsControllerMutex = true;
            }
            catch (Exception ex)
            {
                _controllerMutex?.Dispose();
                _controllerMutex = null;
                SetStatus($"无法取得 CDC 控制权，亮度控制已暂停：{ex.Message}");
                return;
            }

            _reconnectTimer.Change(3000, 3000);
        }
        EnsureConnected();
    }

    /// <summary>Apply curve/rate changes without reopening CDC or resetting ramp progress.</summary>
    public bool TryUpdateSettings(AppSettings settings)
    {
        if (!AppSettingsValidation.TryValidate(settings, out var error)) throw new ArgumentException(error, nameof(settings));
        lock (_gate)
        {
            if (_disposed || settings.BrightnessAutomationEnabled != _settings.BrightnessAutomationEnabled
                || settings.SerialPort != _settings.SerialPort || settings.BaudRate != _settings.BaudRate
                || settings.DryRun != _settings.DryRun) return false;
            _settings = settings;
            return true;
        }
    }

    private void EnsureConnected()
    {
        if (Interlocked.Exchange(ref _connecting, 1) != 0) return;
        SerialPort? retiredPort = null;
        try
        {
            lock (_gate)
            {
                if (_disposed || _port?.IsOpen == true) return;
                retiredPort = _port;
                _port = null;
            }
            // SerialPort.Dispose may wait for DataReceived. Never hold _gate while closing.
            ClosePort(retiredPort);
            retiredPort = null;
            var serialPort = _settings.ResolveSerialPort();
            lock (_gate)
            {
                if (_disposed) return;
                if (string.IsNullOrWhiteSpace(serialPort)) { SetStatus("等待 Pico USB CDC 传感器"); return; }
                var candidate = new SerialPort(serialPort, _settings.BaudRate) { NewLine = "\r\n", ReadTimeout = 1000 };
                candidate.DataReceived += OnSerialData;
                _port = candidate;
                try
                {
                    // Serialize opening with shutdown: no port may open after Dispose completes.
                    candidate.Open();
                    SetStatus($"CDC 已连接 {serialPort}（{(_settings.DryRun ? "预览" : "运行中")}）");
                }
                catch (Exception ex)
                {
                    _port = null;
                    retiredPort = candidate;
                    SetStatus($"等待 {serialPort}：{ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            lock (_gate) { if (!_disposed) SetStatus($"CDC 连接失败：{ex.Message}"); }
        }
        finally
        {
            ClosePort(retiredPort);
            Volatile.Write(ref _connecting, 0);
        }
    }

    private void OnSerialData(object sender, SerialDataReceivedEventArgs e)
    {
        if (sender is not SerialPort source) return;
        lock (_gate) { if (_disposed || !ReferenceEquals(source, _port)) return; }
        try
        {
            var line = source.ReadLine().Trim();
            if (!line.StartsWith("LUX:", StringComparison.Ordinal)) return;
            if (!double.TryParse(line[4..], NumberStyles.Float, CultureInfo.InvariantCulture, out var lux) || !double.IsFinite(lux) || lux < 0) return;
            lock (_gate)
            {
                if (_disposed || !ReferenceEquals(source, _port)) return;
                HandleLux(lux);
            }
        }
        catch (TimeoutException) { }
        catch (Exception ex)
        {
            lock (_gate)
            {
                if (_disposed || !ReferenceEquals(source, _port)) return;
                _port = null;
                SetStatus($"CDC 读取失败，正在重连：{ex.Message}");
            }
            // Close on another thread; closing within DataReceived can wait for itself.
            ThreadPool.QueueUserWorkItem(_ => ClosePort(source));
        }
    }

    private void HandleLux(double rawLux)
    {
        _filteredLux = _filteredLux is null ? rawLux : _filteredLux.Value * 0.8 + rawLux * 0.2;
        var desired = BrightnessCurve.Interpolate(_filteredLux.Value, _settings.Curve);
        var now = DateTime.UtcNow;
        if (_rampedBrightness is null)
        {
            _rampedBrightness = _settings.DryRun ? desired : _monitors.GetCurrentBrightnessPercent() ?? desired;
            _lastRampUtc = now;
        }

        var elapsedSeconds = Math.Clamp((now - _lastRampUtc).TotalSeconds, 0, 1);
        _lastRampUtc = now;
        var maximumChange = Math.Max(0, _settings.BrightnessRampPercentPerSecond) * elapsedSeconds;
        _rampedBrightness += Math.Clamp(desired - _rampedBrightness.Value, -maximumChange, maximumChange);
        var target = _rampedBrightness.Value;

        if ((now - _lastWriteUtc).TotalMilliseconds < Math.Max(0, _settings.MinimumWriteIntervalMilliseconds)) return;
        if (_monitors.LastTarget is { } last && Math.Abs(target - last) < _settings.ChangeDeadbandPercent) return;

        _lastWriteUtc = now;
        if (_settings.DryRun) { _monitors.LastTarget = target; SetStatus($"{_filteredLux:F0} lux → {target:F1}%（目标 {desired:F1}%，预览）"); return; }
        var result = _monitors.SetBrightness(target);
        SetStatus($"{_filteredLux:F0} lux → {target:F1}%（目标 {desired:F1}%，{result}）");
    }

    private void SetStatus(string text)
    {
        void Update(object? _)
        {
            if (_disposed) return;
            Status = text;
            StatusChanged?.Invoke(this, EventArgs.Empty);
        }
        if (_uiContext is null) Update(null);
        else _uiContext.Post(Update, null);
    }

    public void Dispose()
    {
        SerialPort? retiredPort;
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            _reconnectTimer.Dispose();
            retiredPort = _port;
            _port = null;
            _monitors.Dispose();
            if (_ownsControllerMutex)
            {
                try { _controllerMutex?.ReleaseMutex(); }
                catch (ApplicationException) { }
            }
            _controllerMutex?.Dispose();
        }
        ClosePort(retiredPort);
    }

    private void ClosePort(SerialPort? port)
    {
        if (port is null) return;
        port.DataReceived -= OnSerialData;
        try { port.Dispose(); }
        catch (Exception ex) when (ex is IOException or InvalidOperationException) { }
    }
}
