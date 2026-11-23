using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using static Citrus.Interop.NativeMethods;

namespace Citrus.Ui;

/// <summary>
/// 작업을 맡자마자 화면 오른쪽 아래에 "Citrus 작업 중" 을 띄우고, 모든 작업이 끝났다고 확인되면(ShowDone)
/// 결과를 잠깐 보여 준 뒤 닫는다. 포커스를 뺏지 않고 클릭은 아래 창으로 통과한다.
/// </summary>
public partial class BusyWindow : Window
{
    static readonly Brush Working = new SolidColorBrush(Color.FromRgb(0xFF, 0xA9, 0x4D));
    static readonly Brush Warning = new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x6B));

    // 작업 표시줄 등 다른 맨 위 창에 가려지지 않게 1초마다 다시 맨 위로
    readonly DispatcherTimer _keepOnTop = new() { Interval = TimeSpan.FromSeconds(1) };
    readonly DispatcherTimer _closeLater = new() { Interval = TimeSpan.FromSeconds(3) };

    public BusyWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLongPtr(hwnd, GWL_EXSTYLE, GetWindowLongPtr(hwnd, GWL_EXSTYLE) | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
        };
        SizeChanged += (_, _) => PlaceBottomRight();
        _keepOnTop.Tick += (_, _) =>
            SetWindowPos(new WindowInteropHelper(this).Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        _closeLater.Tick += (_, _) =>
        {
            _closeLater.Stop();
            _keepOnTop.Stop();
            Hide();
        };
    }

    /// <param name="percent">0~100, 모르면 음수 (막대가 계속 움직임)</param>
    public void ShowStatus(string detail, int percent)
    {
        _closeLater.Stop(); // 결과를 보여 주는 사이 새 작업이 오면 닫지 않는다
        Heading.Text = "Citrus 작업 중";
        Heading.Foreground = Bar.Foreground = Working;
        Detail.Text = detail;
        Bar.IsIndeterminate = percent < 0;
        if (percent >= 0)
            Bar.Value = percent;
        Appear();
    }

    /// <summary>모든 작업이 끝났을 때만 부른다. 결과를 3초(실패는 읽을 수 있게 6초) 보여 준 뒤 닫는다.</summary>
    public void ShowDone(string title, string detail, bool warning)
    {
        Heading.Text = title;
        Heading.Foreground = Bar.Foreground = warning ? Warning : Working;
        Detail.Text = detail;
        _closeLater.Interval = TimeSpan.FromSeconds(warning ? 6 : 3);
        Bar.IsIndeterminate = false;
        Bar.Value = 100;
        Appear();
        _closeLater.Stop();
        _closeLater.Start();
    }

    void Appear()
    {
        if (IsVisible)
            return;
        Show();
        PlaceBottomRight();
        _keepOnTop.Start();
    }

    // 주 모니터의 작업 영역(작업 표시줄 제외) 오른쪽 아래
    void PlaceBottomRight()
    {
        var area = SystemParameters.WorkArea;
        Left = area.Right - ActualWidth;
        Top = area.Bottom - ActualHeight;
    }
}
