# Dynamic Island for Windows 11

Một viên "Dynamic Island" kiểu iPhone chạy trên Windows 11, dựng bằng WPF (.NET 8).

![icon](assets/icon.png)

## Tính năng
- Nổi cố định giữa, sát mép trên màn hình, luôn ở trên cùng (always on top), không chiếm chỗ trong taskbar
- **Đang phát nhạc**: tự đọc bài hát/nghệ sĩ/ảnh bìa từ bất kỳ app nào qua Windows Media Session (Spotify, YouTube, trình duyệt...), có hiệu ứng sóng nhạc (equalizer) 3 vạch màu
- **Thông báo/cuộc gọi**: đọc toast notification hệ thống (Viber, Zalo...) qua `UserNotificationListener`, tự phồng ra 5 giây rồi thu lại
- **Kéo thả tự do**: giữ chuột trái kéo lên/xuống, kể cả sang màn hình phụ — trục ngang luôn tự khóa về chính giữa màn hình đang chứa con trỏ chuột
- **Đổi màu nền**: chuột phải icon khay hệ thống → chọn 1 trong 6 màu (xám đậm, đen, navy, tím, đỏ đô, xanh rêu), lựa chọn được lưu tự động cho lần chạy sau
- Animation phồng/co theo kiểu lò xo (`BackEase`)
- Icon riêng cho app (không dùng icon mặc định của Windows), hiện ở taskbar/tray/file `.exe`
- Icon khay hệ thống để thoát nhanh + nút "Test thông báo" để xem trước hiệu ứng thông báo

## Chạy thử
Yêu cầu [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
dotnet run
```

- **Click trái** (không kéo) vào viên pill: chuyển qua lại Idle ⟷ Music để test animation nhanh
- **Giữ và kéo** viên pill: di chuyển tự do, tự canh giữa theo màn hình
- **Click phải**: mở menu (đổi màu nền, test thông báo, thoát)

## Build file .exe độc lập

```bash
dotnet publish -c Release
```

File `.exe` nằm trong `bin/Release/net8.0-windows10.0.19041.0/win-x64/publish/` — chạy độc lập, không cần cài .NET trên máy khác, không cần mở terminal.

Xem hướng dẫn chi tiết và các lưu ý (quyền đọc thông báo, tự khởi động cùng Windows, đổi màu nền...) trong [`HUONG-DAN.md`](HUONG-DAN.md).

## Công nghệ
- WPF (.NET 8, `net8.0-windows10.0.19041.0`)
- `GlobalSystemMediaTransportControlsSessionManager` (Windows Media Session API)
- `UserNotificationListener` (Windows Notification Listener API)
- `System.Windows.Forms.Screen` (phát hiện màn hình khi kéo thả đa màn hình)