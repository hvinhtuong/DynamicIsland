# Dynamic Island cho Windows 11 (WPF)

## 1. Cài công cụ cần thiết (chỉ làm 1 lần)
1. Tải và cài **.NET 8 SDK**: https://dotnet.microsoft.com/download/dotnet/8.0
   (chọn bản "SDK" cho Windows x64, không phải "Runtime")
2. Kiểm tra đã cài xong bằng cách mở **PowerShell** hoặc **Terminal**, gõ:

dotnet --version

   Nếu hiện ra số phiên bản (vd `8.0.xxx`) là thành công.

## 2. Chạy thử (chế độ debug, có sẵn console log)
Mở PowerShell tại thư mục chứa các file (`DynamicIsland.csproj`, `MainWindow.xaml`...), gõ:

dotnet run

Viên Dynamic Island màu xám đậm sẽ hiện ở giữa, sát mép trên màn hình.

- **Click trái** (không kéo) vào viên pill: chuyển qua lại Idle ⟷ Music để test animation nhanh, không cần mở nhạc thật.
- **Giữ chuột trái và kéo**: di chuyển viên pill lên/xuống tự do, kể cả sang màn hình phụ — trục ngang tự động khóa về chính giữa màn hình đang chứa con trỏ chuột.
- **Click phải**: mở menu gồm 3 mục — **Màu nền** (chọn màu), **Test thông báo** (xem trước hiệu ứng), **Thoát** (đóng app).
- Ứng dụng cũng có icon nhỏ trong khay hệ thống (system tray, góc dưới phải màn hình) chứa cùng menu trên.

## 3. Đóng gói thành file .exe chạy độc lập
Khi đã ưng ý, đóng gói thành 1 file `.exe` duy nhất, không cần máy đích cài .NET:

dotnet publish -c Release

File `.exe` sẽ nằm trong:

bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\DynamicIsland.exe

Copy file này sang máy Windows 11 khác là chạy được luôn, không cần mở terminal.

## 4. Cho ứng dụng tự khởi động cùng Windows (tùy chọn)
Nhấn `Win + R`, gõ `shell:startup`, Enter → mở thư mục Startup.
Tạo shortcut của file `DynamicIsland.exe` rồi thả vào thư mục đó.

## 5. Đổi màu nền
Chuột phải vào icon khay hệ thống (hoặc vào viên pill) → **Màu nền** → chọn 1 trong 6 màu có sẵn (xám đậm, đen tuyệt đối, navy, tím, đỏ đô, xanh rêu). Màu được lưu lại tự động vào `%AppData%\DynamicIsland\settings.txt`, lần sau mở app vẫn giữ nguyên lựa chọn.

Muốn thêm màu khác: mở `MainWindow.xaml.cs`, tìm mảng `BackgroundColorOptions` ở đầu file, thêm dòng `("Tên màu", "#FFxxxxxx")`.

## 6. Kéo thả đa màn hình
Giữ chuột trái vào viên pill và kéo tự do theo chiều dọc. Trong lúc kéo:
- Trục dọc đi theo tay bạn, bị chặn lại ở mép trên/dưới của màn hình đang chứa con trỏ.
- Trục ngang **luôn tự động canh giữa** màn hình đang chứa con trỏ chuột — kéo qua màn hình phụ (dù đặt phía trên, dưới, hay khác kích thước) đều tự nằm giữa, không bị lệch.

Nếu bạn dùng nhiều màn hình với **tỷ lệ scale (DPI) khác nhau**, việc canh giữa có thể hơi lệch nhẹ — đây là giới hạn của việc quy đổi pixel giữa các màn hình khác DPI.

## 7. Tùy chỉnh thêm cho người muốn sửa code
- Đổi kích thước lúc thu gọn/mở rộng: sửa các hằng số `_collapsedWidth`, `_musicExpandedWidth`... ở đầu `MainWindow.xaml.cs`.
- Đổi bố cục hiển thị nhạc/thông báo: sửa `ExpandedMusicContent` / `ExpandedNotificationContent` trong `MainWindow.xaml`.
- Muốn tự tay kích hoạt trạng thái nào đó bằng code: gọi hàm `SetState(IslandState.Music)` hoặc `SetState(IslandState.Notification)` trong `MainWindow.xaml.cs`.

## 8. Lưu ý về hiệu năng
Bản build Release của WPF thường dùng khoảng 40-70MB RAM lúc idle, CPU gần như 0% khi không có animation chạy — nhẹ hơn nhiều so với đóng gói bằng Electron.

## 9. Tính năng thông báo / cuộc gọi (Viber, Zalo...)
Lần đầu chạy app, Windows có thể hiện hộp thoại xin quyền đọc thông báo hệ thống — bấm **Có / Yes**.

- Nếu Viber (hoặc app khác) gửi toast notification khi có cuộc gọi/tin nhắn đến, island sẽ tự phồng ra hiện tên + nội dung, sau 5 giây tự thu lại.
- Nếu **không thấy** hộp thoại xin quyền hiện ra, hoặc thông báo không tự động hiện: dùng chuột phải vào **icon khay hệ thống** (góc dưới phải màn hình) → chọn **"Test thông báo"** để xem trước hiệu ứng bằng dữ liệu giả.
- Đây là giới hạn của Windows với app chưa đóng gói (MSIX) — không phải lỗi code. Nếu bạn muốn quyền này hoạt động ổn định 100%, nói mình biết để hướng dẫn đóng gói MSIX.