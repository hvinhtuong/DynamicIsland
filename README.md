# Dynamic Island for Windows 11

Một viên "Dynamic Island" kiểu iPhone chạy trên Windows 11, dựng bằng WPF (.NET 8).

![icon](assets/icon.png)

## Tính năng
- Nổi cố định giữa, sát mép trên màn hình, luôn ở trên cùng (always on top)
- **Đang phát nhạc**: tự đọc bài hát/nghệ sĩ/ảnh bìa từ bất kỳ app nào qua Windows Media Session (Spotify, YouTube, trình duyệt...), có hiệu ứng sóng nhạc (equalizer) 3 vạch màu
- **Thông báo/cuộc gọi**: đọc toast notification hệ thống (Viber, Zalo...) qua `UserNotificationListener`, tự phồng ra 5 giây rồi thu lại
- Animation phồng/co theo kiểu lò xo (`BackEase`), không chiếm chỗ trong taskbar
- Icon khay hệ thống để thoát nhanh + nút "Test thông báo" để xem trước hiệu ứng

## Chạy thử
Yêu cầu [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
dotnet run
```

## Build file .exe độc lập

```bash
dotnet publish -c Release
```

File `.exe` nằm trong `bin/Release/net8.0-windows10.0.19041.0/win-x64/publish/`.

Xem hướng dẫn chi tiết và các lưu ý (quyền đọc thông báo, tự khởi động cùng Windows...) trong [`HUONG-DAN.md`](HUONG-DAN.md).

## Công nghệ
- WPF (.NET 8, `net8.0-windows10.0.19041.0`)
- `GlobalSystemMediaTransportControlsSessionManager` (Windows Media Session API)
- `UserNotificationListener` (Windows Notification Listener API)
