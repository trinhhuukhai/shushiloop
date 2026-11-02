# Ảnh món ăn

Catalog có **64 món** đang sử dụng: 8 Nigiri, 8 Maki, 11 Gunkan, 15 Sashimi hai loại, 20 Sashimi ba loại và 2 món trà. `SushiDishArt.asset` có 68 liên kết: 64 ảnh nguồn cho các món đang dùng và 4 liên kết ảnh cũ lưu lại cho ID đã bỏ.

Ảnh nguồn nằm trong `Assets/_game/Art/nguyenLieu_monAn/asset-20261102T044738Z-1-001/asset`. Dropdown trong Inspector và Level Editor dùng tên file nguồn. Xem [bảng tên, ID, công thức và đường dẫn ảnh](../../../../Documentation/RecipeAssets20261102.md).

ID hợp lệ là **1–8 và 13–68**. ID 9–12 đã bỏ khỏi enum và catalog công thức, không được tái sử dụng. Bộ nguồn không có 4 ảnh món tương ứng; ảnh, liên kết trong thư viện ảnh và định nghĩa sprite atlas cũ vẫn được giữ nguyên làm data lưu lại, không tạo thêm món hay order. Các món Gunkan trứng cá tiếp tục được giữ.

Ảnh order catalog đã bao gồm hình thành phẩm. Sashimi dùng đĩa trang trí riêng theo hai hoặc ba nguyên liệu; dữ liệu đĩa/lớp nguyên liệu nằm trong `SushiPlatedIngredientArt.asset`. Xem [hướng dẫn công thức](../../../../Documentation/RecipeDesign.md).
