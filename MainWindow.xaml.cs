using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using WpfRectangle = System.Windows.Shapes.Rectangle;
using Windows.Media.Control;
using Windows.Storage.Streams;
using Windows.UI.Notifications.Management;
using Forms = System.Windows.Forms;

namespace DynamicIsland
{
    public enum IslandState { Idle, Music, Notification }

    public partial class MainWindow : Window
    {
        private IslandState _currentState = IslandState.Idle;
        private IslandState _stateBeforeNotification = IslandState.Idle;

        private readonly double _collapsedWidth = 110;
        private readonly double _collapsedHeight = 32;
        private readonly double _musicExpandedWidth = 280;
        private readonly double _musicExpandedHeight = 54;
        private readonly double _notifExpandedWidth = 290;
        private readonly double _notifExpandedHeight = 54;

        private Forms.NotifyIcon? _trayIcon;

        private GlobalSystemMediaTransportControlsSessionManager? _mediaManager;
        private GlobalSystemMediaTransportControlsSession? _currentSession;
        private bool _isMusicPlaying = false;

        private UserNotificationListener? _notifListener;
        private System.Windows.Threading.DispatcherTimer? _notifAutoHideTimer;

        // ---- Trạng thái kéo thả ----
        private bool _mouseDownForDrag = false;
        private bool _isDragging = false;
        private System.Drawing.Point _dragStartCursorPx;
        private double _dragStartTopPx;
        private double _dragOffsetYPx; // khoảng cách từ điểm bấm chuột tới mép trên cửa sổ, để kéo không bị giật

        // ---- Màu nền tùy chỉnh ----
        private static readonly (string Name, string Hex)[] BackgroundColorOptions = new[]
        {
            ("Xám đậm (mặc định)", "#FF1C1C1E"),
            ("Đen tuyệt đối",      "#FF000000"),
            ("Xanh navy đậm",      "#FF0D1B2A"),
            ("Tím đậm",            "#FF2E1A47"),
            ("Đỏ đô",              "#FF3B0A0A"),
            ("Xanh rêu đậm",       "#FF0F2A1D"),
        };

        private static readonly string SettingsFilePath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DynamicIsland", "settings.txt");

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyBackgroundColor(LoadSavedColorHex());
            PositionAtTopCenter();
            SetupTrayIcon();
            await InitMediaListenerAsync();
            await InitNotificationListenerAsync();
        }

        private void PositionAtTopCenter()
        {
            var screen = Forms.Screen.PrimaryScreen ?? Forms.Screen.AllScreens[0];
            var dpi = VisualTreeHelper.GetDpi(this);
            double screenLeft = screen.Bounds.Left / dpi.DpiScaleX;
            double screenWidth = screen.Bounds.Width / dpi.DpiScaleX;
            double screenTop = screen.Bounds.Top / dpi.DpiScaleY;

            this.Left = screenLeft + (screenWidth - this.ActualWidth) / 2;
            this.Top = screenTop + 8;
        }

        // Luôn canh giữa theo chiều ngang dựa trên MÀN HÌNH HIỆN TẠI (nơi cửa sổ đang đứng),
        // không phải luôn lấy màn hình chính - để hỗ trợ kéo qua nhiều màn hình
        private void CenterHorizontallyOnCurrentScreen()
        {
            var dpi = VisualTreeHelper.GetDpi(this);
            var centerPx = new System.Drawing.Point(
                (int)((this.Left + this.ActualWidth / 2) * dpi.DpiScaleX),
                (int)((this.Top + this.ActualHeight / 2) * dpi.DpiScaleY));

            var screen = Forms.Screen.FromPoint(centerPx);
            double screenLeft = screen.Bounds.Left / dpi.DpiScaleX;
            double screenWidth = screen.Bounds.Width / dpi.DpiScaleX;

            this.Left = screenLeft + (screenWidth - this.ActualWidth) / 2;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Kích thước đổi (do animation phồng/co) -> canh lại giữa theo màn hình hiện tại
            CenterHorizontallyOnCurrentScreen();
        }

        // ================== KÉO THẢ ==================

        private void IslandBorder_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _mouseDownForDrag = true;
            _isDragging = false;

            var dpi = VisualTreeHelper.GetDpi(this);
            _dragStartCursorPx = Forms.Cursor.Position;
            _dragStartTopPx = this.Top * dpi.DpiScaleY;
            _dragOffsetYPx = _dragStartCursorPx.Y - _dragStartTopPx;

            IslandBorder.CaptureMouse();
        }

        private void IslandBorder_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_mouseDownForDrag) return;

            var cursorNow = Forms.Cursor.Position;
            int deltaX = cursorNow.X - _dragStartCursorPx.X;
            int deltaY = cursorNow.Y - _dragStartCursorPx.Y;

            // Chỉ tính là "đang kéo" khi di chuyển đủ xa, để phân biệt với click bình thường
            if (!_isDragging && (Math.Abs(deltaX) > 5 || Math.Abs(deltaY) > 5))
                _isDragging = true;

            if (!_isDragging) return;

            var dpi = VisualTreeHelper.GetDpi(this);

            // Xác định màn hình đang chứa con trỏ chuột -> đó là màn hình "đích"
            var screen = Forms.Screen.FromPoint(cursorNow);
            double screenLeftPx = screen.Bounds.Left;
            double screenTopPx = screen.Bounds.Top;
            double screenWidthPx = screen.Bounds.Width;
            double screenHeightPx = screen.Bounds.Height;

            double windowWidthPx = this.ActualWidth * dpi.DpiScaleX;
            double windowHeightPx = this.ActualHeight * dpi.DpiScaleY;

            // Trục ngang: LUÔN khóa về chính giữa màn hình đang chứa con trỏ
            double targetLeftPx = screenLeftPx + (screenWidthPx - windowWidthPx) / 2;

            // Trục dọc: đi theo tay kéo tự do, chỉ chặn không cho vượt ra ngoài màn hình
            double targetTopPx = cursorNow.Y - _dragOffsetYPx;
            double minTopPx = screenTopPx;
            double maxTopPx = screenTopPx + screenHeightPx - windowHeightPx;
            if (targetTopPx < minTopPx) targetTopPx = minTopPx;
            if (targetTopPx > maxTopPx) targetTopPx = maxTopPx;

            this.Left = targetLeftPx / dpi.DpiScaleX;
            this.Top = targetTopPx / dpi.DpiScaleY;
        }

        private void IslandBorder_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            IslandBorder.ReleaseMouseCapture();
            bool wasDragging = _isDragging;
            _mouseDownForDrag = false;
            _isDragging = false;

            if (!wasDragging)
            {
                // Không kéo -> coi như click bình thường: chuyển qua lại Idle/Music để test
                if (_currentState == IslandState.Music)
                    SetState(IslandState.Idle);
                else
                    SetState(IslandState.Music);
            }
            else
            {
                // Kéo xong -> canh lại chính giữa lần cuối cho chắc chắn (đề phòng sai số làm tròn)
                CenterHorizontallyOnCurrentScreen();
            }
        }

        private void IslandBorder_RightClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Forms.Application.Exit();
            this.Close();
        }

        // ================== ĐIỀU KHIỂN TRẠNG THÁI + ANIMATION ==================

        private void SetState(IslandState newState)
        {
            _currentState = newState;

            double targetWidth = _collapsedWidth;
            double targetHeight = _collapsedHeight;

            CollapsedContent.Visibility = Visibility.Collapsed;
            ExpandedMusicContent.Visibility = Visibility.Collapsed;
            ExpandedNotificationContent.Visibility = Visibility.Collapsed;

            switch (newState)
            {
                case IslandState.Idle:
                    targetWidth = _collapsedWidth;
                    targetHeight = _collapsedHeight;
                    CollapsedContent.Visibility = Visibility.Visible;
                    SetEqualizerRunning(_isMusicPlaying, mini: true);
                    break;

                case IslandState.Music:
                    targetWidth = _musicExpandedWidth;
                    targetHeight = _musicExpandedHeight;
                    ExpandedMusicContent.Visibility = Visibility.Visible;
                    SetEqualizerRunning(_isMusicPlaying, mini: false);
                    break;

                case IslandState.Notification:
                    targetWidth = _notifExpandedWidth;
                    targetHeight = _notifExpandedHeight;
                    ExpandedNotificationContent.Visibility = Visibility.Visible;
                    break;
            }

            AnimateSize(targetWidth, targetHeight);
        }

        // Animation "nảy" nhẹ giống lò xo iOS thay vì trượt đều đều
        private void AnimateSize(double targetWidth, double targetHeight)
        {
            var ease = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.35 };

            var widthAnim = new DoubleAnimation(this.Width, targetWidth, TimeSpan.FromMilliseconds(420))
            {
                EasingFunction = ease
            };
            var heightAnim = new DoubleAnimation(this.Height, targetHeight, TimeSpan.FromMilliseconds(420))
            {
                EasingFunction = ease
            };

            this.BeginAnimation(WidthProperty, widthAnim);
            this.BeginAnimation(HeightProperty, heightAnim);
        }

        // Bật/tắt animation sóng nhạc. mini=true dùng cho lúc thu gọn (3 vạch nhỏ),
        // mini=false dùng cho lúc mở rộng (3 vạch to, màu xanh lá giống Spotify)
        private void SetEqualizerRunning(bool running, bool mini)
        {
            WpfRectangle b1 = mini ? MiniBar1 : BigBar1;
            WpfRectangle b2 = mini ? MiniBar2 : BigBar2;
            WpfRectangle b3 = mini ? MiniBar3 : BigBar3;

            var res = this.Resources;
            if (running)
            {
                ((Storyboard)res["BarAnim1"]).Begin(b1, true);
                ((Storyboard)res["BarAnim2"]).Begin(b2, true);
                ((Storyboard)res["BarAnim3"]).Begin(b3, true);
            }
            else
            {
                b1.BeginAnimation(WpfRectangle.HeightProperty, null);
                b2.BeginAnimation(WpfRectangle.HeightProperty, null);
                b3.BeginAnimation(WpfRectangle.HeightProperty, null);
                b1.Height = 4; b2.Height = 4; b3.Height = 4;
            }
        }

        // ================== NHẠC (Spotify / bất kỳ app media nào) ==================

        private async System.Threading.Tasks.Task InitMediaListenerAsync()
        {
            try
            {
                _mediaManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
                _mediaManager.CurrentSessionChanged += (s, e) => Dispatcher.Invoke(SubscribeToCurrentSession);
                SubscribeToCurrentSession();
            }
            catch { /* Một số máy không hỗ trợ API này */ }
        }

        private void SubscribeToCurrentSession()
        {
            if (_currentSession != null)
            {
                _currentSession.MediaPropertiesChanged -= CurrentSession_MediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged -= CurrentSession_PlaybackInfoChanged;
            }

            _currentSession = _mediaManager?.GetCurrentSession();

            if (_currentSession != null)
            {
                _currentSession.MediaPropertiesChanged += CurrentSession_MediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged += CurrentSession_PlaybackInfoChanged;
                _ = UpdateMediaInfoAsync();
            }
            else
            {
                EvaluatePlaybackState();
            }
        }

        private async void CurrentSession_MediaPropertiesChanged(
            GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
            => await UpdateMediaInfoAsync();

        private void CurrentSession_PlaybackInfoChanged(
            GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
        {
            Dispatcher.Invoke(EvaluatePlaybackState);
        }

        // Điểm quyết định DUY NHẤT cho việc phồng/co theo trạng thái phát nhạc.
        // Luôn đọc trạng thái mới nhất trực tiếp từ session ngay tại thời điểm gọi,
        // tránh tình trạng 2 nơi khác nhau đọc dữ liệu cũ (stale) rồi đá nhau.
        private void EvaluatePlaybackState()
        {
            if (_currentSession == null)
            {
                _isMusicPlaying = false;
            }
            else
            {
                var status = _currentSession.GetPlaybackInfo().PlaybackStatus;
                _isMusicPlaying = status == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
            }

            if (_currentState == IslandState.Notification) return; // đang hiện thông báo thì để yên, không tranh chấp

            if (_isMusicPlaying && _currentState != IslandState.Music) SetState(IslandState.Music);
            else if (!_isMusicPlaying && _currentState == IslandState.Music) SetState(IslandState.Idle);
            else SetEqualizerRunning(_isMusicPlaying, mini: _currentState == IslandState.Idle);
        }

        private async System.Threading.Tasks.Task UpdateMediaInfoAsync()
        {
            if (_currentSession == null) return;
            var props = await _currentSession.TryGetMediaPropertiesAsync();

            Dispatcher.Invoke(() =>
            {
                TitleText.Text = string.IsNullOrWhiteSpace(props.Title) ? "Không rõ tên bài hát" : props.Title;
                ArtistText.Text = props.Artist ?? "";
            });

            if (props.Thumbnail != null)
            {
                var bmp = await LoadBitmapFromStreamRefAsync(props.Thumbnail);
                if (bmp != null) Dispatcher.Invoke(() => AlbumArtImage.Source = bmp);
            }

            Dispatcher.Invoke(EvaluatePlaybackState);
        }

        // ================== THÔNG BÁO / CUỘC GỌI (Viber, Zalo, mail...) ==================
        // Dùng UserNotificationListener - API hệ thống của Windows để đọc toast notification
        // từ mọi app khác. LẦN ĐẦU CHẠY, Windows sẽ hiện hộp thoại xin quyền - bạn cần bấm "Có/Yes".
        // Nếu app không được đóng gói MSIX, một số máy Windows có thể chặn quyền này.

        private async System.Threading.Tasks.Task InitNotificationListenerAsync()
        {
            try
            {
                _notifListener = UserNotificationListener.Current;
                var access = await _notifListener.RequestAccessAsync();
                if (access == UserNotificationListenerAccessStatus.Allowed)
                {
                    _notifListener.NotificationChanged += NotifListener_NotificationChanged;
                }
            }
            catch
            {
                // API không khả dụng - vẫn có thể test bằng menu "Test thông báo" ở khay hệ thống
            }
        }

        private async void NotifListener_NotificationChanged(
            UserNotificationListener sender, Windows.UI.Notifications.UserNotificationChangedEventArgs args)
        {
            if (args.ChangeKind != Windows.UI.Notifications.UserNotificationChangedKind.Added) return;

            try
            {
                var notif = sender.GetNotification(args.UserNotificationId);
                if (notif == null) return;

                string appName = notif.AppInfo.DisplayInfo.DisplayName;
                string title = appName;
                string body = "";

                var binding = notif.Notification?.Visual?.Bindings?.Count > 0
                    ? notif.Notification.Visual.Bindings[0]
                    : null;

                if (binding != null)
                {
                    var texts = binding.GetTextElements();
                    if (texts.Count > 0) title = texts[0].Text;
                    if (texts.Count > 1) body = texts[1].Text;
                }

                BitmapImage? icon = null;
                try
                {
                    var logoRef = notif.AppInfo.DisplayInfo.GetLogo(new Windows.Foundation.Size(48, 48));
                    icon = await LoadBitmapFromStreamRefAsync(logoRef);
                }
                catch { }

                Dispatcher.Invoke(() => ShowNotification(title, body, icon));
            }
            catch { }
        }

        // Hiện thông báo lên island, tự thu lại sau vài giây rồi quay về trạng thái trước đó
        private void ShowNotification(string title, string body, BitmapImage? icon)
        {
            if (_currentState != IslandState.Notification)
                _stateBeforeNotification = _currentState;

            NotifTitleText.Text = title;
            NotifBodyText.Text = string.IsNullOrWhiteSpace(body) ? " " : body;
            if (icon != null) NotifIconImage.Source = icon;

            SetState(IslandState.Notification);

            _notifAutoHideTimer?.Stop();
            _notifAutoHideTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _notifAutoHideTimer.Tick += (s, e) =>
            {
                _notifAutoHideTimer!.Stop();
                SetState(IslandState.Idle);
                EvaluatePlaybackState();
            };
            _notifAutoHideTimer.Start();
        }

        // ================== TIỆN ÍCH DÙNG CHUNG ==================

        private static async System.Threading.Tasks.Task<BitmapImage?> LoadBitmapFromStreamRefAsync(
            Windows.Storage.Streams.IRandomAccessStreamReference streamRef)
        {
            try
            {
                using var stream = await streamRef.OpenReadAsync();
                var reader = new DataReader(stream);
                uint size = (uint)stream.Size;
                await reader.LoadAsync(size);
                byte[] bytes = new byte[size];
                reader.ReadBytes(bytes);
                reader.Dispose();

                using var ms = new System.IO.MemoryStream(bytes);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        // Icon khay hệ thống: thoát app + nút test thông báo (hữu ích khi quyền
        // UserNotificationListener chưa xin được, vẫn xem trước được hiệu ứng)
        private void SetupTrayIcon()
        {
            System.Drawing.Icon appIcon;
            try
            {
                // Lấy icon từ resource đã nhúng sẵn trong assembly (khai báo trong .csproj),
                // cách này hoạt động ổn định kể cả khi build thành file .exe single-file
                var uri = new Uri("pack://application:,,,/assets/icon.ico");
                var streamInfo = System.Windows.Application.GetResourceStream(uri);
                appIcon = streamInfo != null
                    ? new System.Drawing.Icon(streamInfo.Stream)
                    : System.Drawing.SystemIcons.Application;
            }
            catch
            {
                appIcon = System.Drawing.SystemIcons.Application;
            }

            _trayIcon = new Forms.NotifyIcon
            {
                Icon = appIcon,
                Visible = true,
                Text = "Dynamic Island"
            };

            var menu = new Forms.ContextMenuStrip();

            var colorMenu = new Forms.ToolStripMenuItem("Màu nền");
            string currentHex = LoadSavedColorHex();
            foreach (var (name, hex) in BackgroundColorOptions)
            {
                var item = new Forms.ToolStripMenuItem(name)
                {
                    Checked = string.Equals(hex, currentHex, StringComparison.OrdinalIgnoreCase)
                };
                item.Click += (s, e) =>
                {
                    Dispatcher.Invoke(() => ApplyBackgroundColor(hex));
                    SaveColorHex(hex);
                    foreach (Forms.ToolStripMenuItem other in colorMenu.DropDownItems)
                        other.Checked = other == item;
                };
                colorMenu.DropDownItems.Add(item);
            }
            menu.Items.Add(colorMenu);
            menu.Items.Add(new Forms.ToolStripSeparator());

            menu.Items.Add("Test thông báo", null, (s, e) =>
            {
                Dispatcher.Invoke(() => ShowNotification("Viber", "Bạn có cuộc gọi đến: Minh Anh", null));
            });
            menu.Items.Add("Thoát", null, (s, e) =>
            {
                _trayIcon!.Visible = false;
                Forms.Application.Exit();
                this.Close();
            });
            _trayIcon.ContextMenuStrip = menu;
        }

        // Đổi màu nền viên pill ngay lập tức
        private void ApplyBackgroundColor(string hex)
        {
            try
            {
                var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex);
                var brush = new SolidColorBrush(color);
                brush.Freeze();
                IslandBorder.Background = brush;
            }
            catch { /* mã màu không hợp lệ thì bỏ qua, giữ màu hiện tại */ }
        }

        private static string LoadSavedColorHex()
        {
            try
            {
                if (System.IO.File.Exists(SettingsFilePath))
                {
                    var saved = System.IO.File.ReadAllText(SettingsFilePath).Trim();
                    if (!string.IsNullOrWhiteSpace(saved)) return saved;
                }
            }
            catch { }
            return BackgroundColorOptions[0].Hex; // mặc định: xám đậm
        }

        private static void SaveColorHex(string hex)
        {
            try
            {
                var dir = System.IO.Path.GetDirectoryName(SettingsFilePath)!;
                System.IO.Directory.CreateDirectory(dir);
                System.IO.File.WriteAllText(SettingsFilePath, hex);
            }
            catch { /* không lưu được thì thôi, không ảnh hưởng chức năng chính */ }
        }

        protected override void OnClosed(EventArgs e)
        {
            _trayIcon?.Dispose();
            base.OnClosed(e);
        }
    }
}
