# Dynamic Island cho Windows 11 (WPF)

## 1. Cài công cụ cần thiết (chỉ làm 1 lần)
1. Tải và cài **.NET 8 SDK**: https://dotnet.microsoft.com/download/dotnet/8.0
   (chọn bản "SDK" cho Windows x64, không phải "Runtime")
2. Kiểm tra đã cài xong bằng cách mở **PowerShell** hoặc **Terminal**, gõ:
   ```
   dotnet --version
   ```
   Nếu hiện ra số phiên bản (vd `8.0.xxx`) là thành công.

## 2. Chạy thử (chế độ debug, có sẵn console log)
Mở PowerShell tại thư mục chứa các file (`DynamicIsland.csproj`, `MainWindow.xaml`...), gõ:
```
dotnet run
```
Viên Dynamic Island màu đen sẽ hiện ở giữa, sát mép trên màn hình.
- **Click trái** vào viên pill: phồng to ra như đang hiển thị nhạc.
- **Click trái** lần nữa: thu nhỏ lại.
- **Click phải**: thoát ứng dụng.
- Ứng dụng cũng có icon nhỏ trong khay hệ thống (system tray, góc dưới phải màn hình) để thoát.

## 3. Đóng gói thành file .exe chạy độc lập
Khi đã ưng ý, đóng gói thành 1 file `.exe` duy nhất, không cần máy đích cài .NET:
```
dotnet publish -c Release
```
File `.exe` sẽ nằm trong:
```
bin\Release\net8.0-windows\win-x64\publish\DynamicIsland.exe
```
Copy file này sang máy Windows 11 khác là chạy được luôn.

## 4. Cho ứng dụng tự khởi động cùng Windows (tùy chọn)
Nhấn `Win + R`, gõ `shell:startup`, Enter → mở thư mục Startup.
Tạo shortcut của file `DynamicIsland.exe` rồi thả vào thư mục đó.

## 5. Tùy chỉnh thêm
- Đổi màu, kích thước: sửa trong `MainWindow.xaml` (`Background`, `Width`, `Height`, `CornerRadius`).
- Đổi nội dung hiển thị lúc mở rộng (tên bài hát, icon...): sửa phần `ExpandedContent` trong `MainWindow.xaml`.
- Muốn tự động phồng ra khi có sự kiện thật (vd: có cuộc gọi, có nhạc đang phát...): gọi hàm `ToggleIsland()` trong `MainWindow.xaml.cs` từ nơi bạn detect sự kiện đó (ví dụ hook vào Windows Media Session API để biết khi nào có nhạc phát).

## Đổi màu nền
Chuột phải vào icon khay hệ thống → **Màu nền** → chọn 1 trong 6 màu có sẵn (xám đậm, đen tuyệt đối, navy, tím, đỏ đô, xanh rêu). Màu được lưu lại tự động, lần sau mở app vẫn giữ nguyên lựa chọn.

## Lưu ý về hiệu năng
Bản build Release của WPF thường dùng khoảng 40-70MB RAM lúc idle, CPU gần như 0% khi không có animation chạy — nhẹ hơn nhiều so với đóng gói bằng Electron.

## Tính năng thông báo / cuộc gọi (Viber, Zalo...)
Lần đầu chạy app, Windows có thể hiện hộp thoại xin quyền đọc thông báo hệ thống — bấm **Có / Yes**.

- Nếu Viber (hoặc app khác) gửi toast notification khi có cuộc gọi/tin nhắn đến, island sẽ tự phồng ra hiện tên + nội dung, sau 5 giây tự thu lại.
- Nếu **không thấy** hộp thoại xin quyền hiện ra, hoặc thông báo không tự động hiện: dùng chuột phải vào **icon khay hệ thống** (góc dưới phải màn hình) → chọn **"Test thông báo"** để xem trước hiệu ứng bằng dữ liệu giả.
- Đây là giới hạn của Windows với app chưa đóng gói (MSIX) — không phải lỗi code. Nếu bạn muốn quyền này hoạt động ổn định 100%, nói mình biết để hướng dẫn đóng gói MSIX.
