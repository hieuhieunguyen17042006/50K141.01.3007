# Hệ thống quản lý bán hàng nội bộ T-HOME

Mã ứng dụng nằm tại **[3. Source Code/THomeApp](<3. Source Code/THomeApp>)**, dùng ASP.NET Core MVC trên .NET 10, EF Core và SQL Server. Các thư mục tài liệu mục 1 và 2 được giữ nguyên theo repo nhóm. Phạm vi ứng dụng không có phân hệ kho.

## Cấu trúc

| Thư mục | Nội dung |
|---|---|
| `1. THU THẬP YÊU CẦU/` | Tài liệu phỏng vấn của nhóm |
| `2. PHÂN TÍCH VÀ THIẾT KẾ HỆ THỐNG/` | Tài liệu phân tích, thiết kế và Figma |
| `3. Source Code/THomeApp/` | Controllers, Models, Data, Migrations, Services, Views và tài nguyên web |

## Cấu hình riêng trước khi chạy

Cần .NET SDK 10 và SQL Server. Tại thư mục gốc repo, tạo file cấu hình riêng từ mẫu:

```powershell
Copy-Item '.\3. Source Code\THomeApp\appsettings.Local.example.json' '.\3. Source Code\THomeApp\appsettings.Local.json'
```

Điền bằng trình soạn thảo:

- `ConnectionStrings:DefaultConnection`: chuỗi kết nối SQL Server của máy bạn. Tài khoản chạy ứng dụng cần quyền áp dụng migration trên database được chọn.
- `SeedAccounts:OwnerPassword`, `AccountantPassword`, `SalesPassword`, `LockedPassword`: mật khẩu riêng cho bốn tài khoản mẫu khi khởi tạo database trống. Không để trống nếu cần tạo database mới.
- `Payment`: thông tin ngân hàng của nhóm nếu muốn dùng QR chuyển khoản. Khi chưa cấu hình, ứng dụng hiển thị **QR tra cứu nội bộ**.

`appsettings.Local.json` đã bị Git bỏ qua. Có thể thay bằng biến môi trường, chẳng hạn `ConnectionStrings__DefaultConnection` hoặc `SeedAccounts__OwnerPassword`; giá trị môi trường và dòng lệnh được ưu tiên. Không đặt mật khẩu, token hay kết nối riêng vào `appsettings.json`, file mẫu hoặc mã nguồn.

Ứng dụng tự áp dụng migration và chỉ seed tài khoản nếu bảng nhân viên đang trống. Database đã có dữ liệu giữ nguyên tài khoản và mật khẩu hiện tại. Mã tài khoản mẫu: `admin` (Chủ cửa hàng), `ketoan` (Kế toán), `nhanvien` (Bán hàng), `khoa01` (bị khóa để kiểm tra). Mật khẩu được cấp riêng qua cấu hình, không lưu trong repo hoặc tự điền trên trang đăng nhập.

## Build và chạy

```powershell
dotnet restore '.\3. Source Code\THomeApp'
dotnet build '.\3. Source Code\THomeApp' --no-restore
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project '.\3. Source Code\THomeApp' --no-launch-profile --urls http://localhost:5000
```

Mở **http://localhost:5000/Auth/Login**. Nếu bản THomeApp cũ đang chạy, dừng tiến trình đó trước khi build để tránh khóa tệp thực thi. Các thư viện CSS, JavaScript, phông chữ và ảnh cần cho web đã nằm trong `wwwroot`; không cần Node.js để build hoặc chạy ứng dụng .NET.

## Chức năng hiện có

- Đăng nhập và phân quyền Chủ cửa hàng / Kế toán / Bán hàng; khóa tài khoản thu hồi phiên đang dùng.
- Đơn tạm tính → QR → xác nhận thu → xem trước → phát hành hóa đơn khi thu đủ; tiền cọc thuộc số đã thu.
- Chi phí phát sinh, trả/đổi hàng, cấn nợ, hoàn tiền và ghi đè có liên kết chứng từ, lịch sử, kiểm tra quyền và chống gửi trùng.
- Khách hàng, sản phẩm và lịch sử giá, chính sách chiết khấu, nhân viên, yêu cầu mở khóa và nhật ký hoạt động.
- Báo cáo doanh thu, thống kê bán chạy, Excel/PDF, công nợ và đối soát ca.

Xác nhận thu/hoàn là thao tác riêng. Lưu phiếu hoặc mở/in QR không tự ghi nhận giao dịch tiền. Giá và chiết khấu trên hóa đơn đã lưu được giữ theo chứng từ. Các phiếu điều chỉnh mới có bản trước/sau để đối chiếu.

Zalo/Email và xác nhận giao dịch ngân hàng tự động cần cấu hình/tích hợp dịch vụ thật; hiện có thể in hoặc lưu PDF để gửi thủ công. Form đăng nhập cũ có token không phù hợp phiên hiện tại sẽ bị chặn và chuyển về GET an toàn; bảo vệ antiforgery vẫn được duy trì.

## Phạm vi commit

Repo chứa mã ứng dụng, migrations, cấu hình công khai/mẫu và tài nguyên web. Thư mục build, cấu hình riêng, database local, khóa/chứng thư, log, ảnh kiểm thử, file tạm và các script phân tích Word chỉ dùng trong workspace được Git bỏ qua. Không commit dữ liệu database đang dùng hoặc thông tin đăng nhập thật.
