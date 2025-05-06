# 🍽️ RecipeWeb - ASP.NET Core MVC

**RecipeWeb** là một website hỗ trợ tìm kiếm và quản lý công thức nấu ăn, được phát triển bằng nền tảng **ASP.NET Core MVC**. Ứng dụng cho phép người dùng tìm kiếm công thức theo nguyên liệu, đăng và chỉnh sửa công thức cá nhân, đánh giá và bình luận công thức, cũng như lưu các công thức yêu thích. Hệ thống còn tích hợp chức năng quản trị giúp quản lý người dùng và kiểm duyệt nội dung.

---

## 🚀 Tính năng chính

### 👨‍🍳 Người dùng
- Đăng ký, đăng nhập và chỉnh sửa thông tin cá nhân.
- Tìm kiếm công thức theo tên hoặc nguyên liệu.
- Đăng công thức nấu ăn mới.
- Sửa, xóa công thức cá nhân.
- Đánh giá và bình luận công thức.
- Lưu công thức yêu thích.

### 🛡️ Quản trị viên
- Duyệt và kiểm duyệt các công thức do người dùng đăng.
- Quản lý thông tin và trạng thái người dùng.
- Thống kê số liệu về công thức, lượt xem, lượt yêu thích,...

---

## 🛠️ Công nghệ sử dụng

- **Backend:** ASP.NET Core MVC
- **Frontend:** HTML, CSS, Bootstrap, JavaScript
- **Database:** SQL Server
- **Thiết kế giao diện:** Figma (mockup)
- **Quản lý mã nguồn:** GitHub

---

## 🧪 Hướng dẫn chạy ứng dụng

1. Clone repository:
   ```
  git clone https://github.com/hanhptm123/RecipeWeb-ASP.NETCoreMVC.git  
   ```

2. Mở bằng Visual Studio hoặc VS Code.

3. Thiết lập chuỗi kết nối với SQL Server trong `appsettings.json`.

4. Chạy lệnh để cập nhật database nếu có dùng EF Core:
   ```
   dotnet ef database update
   ```

5. Chạy ứng dụng:
   ```
   dotnet run
   ```

---

## 🧑‍💻 Nhóm phát triển

- **Phạm Thị Mỹ Hạnh** – Đăng nhập, Đăng ký, Quản lý người dùng, bình luận đánh giá công thức
- **Đặng Thị Ngọc Tiên** – Đăng, sửa, xem công thức và lưu công thức yêu thích, thống kê lượt yêu thích
- **Phương Hữu Gia Lộc** – Thống kê số liệu, duyệt công thức, tìm kiếm công thức  

---

## 📃 Giấy phép

Dự án được xây dựng phục vụ mục đích học tập. Không sử dụng cho mục đích thương mại nếu chưa có sự cho phép của nhóm phát triển.
