# Công thức theo bộ asset 02/11/2026

Nguồn: `Assets/_game/Art/nguyenLieu_monAn/asset-20261102T044738Z-1-001/asset`. Tên món bám theo tên PNG tiếng Việt, bỏ dấu gạch dưới và chuẩn hóa `maiki`/`miaki` thành Maki. `nigiri_muc` hiển thị Nigiri mực; nguyên liệu tương ứng trong game vẫn là `PoolType.Octopus` như lớp `nigiri_top_octopus`.

Dropdown trong Inspector của mọi asset/prefab và Level Editor dùng đúng tên file qua `InspectorName`: `nigiri_ca_ngu`, `tuna`, `rice`… Sashimi dùng `sashimi_x2/32` hoặc `sashimi_x3/49` để phân biệt thư mục của các PNG đánh số. Mã số enum giữ nguyên; tên tiếng Việt trong bảng dưới đây dùng khi hiển thị món trong game.

Catalog art duy nhất là `Assets/_game/Resources/SushiArtCatalog.asset`. Đã xóa bản trùng không có tham chiếu trong `DataSO`, cùng hai trường `orderCard`/`conveyor` không được gameplay đọc; các hình này đã được gán trực tiếp trên scene/prefab. `boardTile`, `plate` và `customerAvatars` vẫn có nơi sử dụng. Các entry công thức/nguyên liệu cũ còn được campaign, level mẫu hoặc món tự ghép sử dụng tiếp tục được giữ.

Catalog chỉ gồm 64 món có trong bộ ảnh nguồn, dùng ID 1–8 và 13–68. ID 9–12 đã bỏ và không được tái sử dụng; ID các món còn lại giữ nguyên. ID 49–68 đúng số trên ảnh Sashimi ba loại. Các order cũ đã đổi sang Nigiri hợp lệ và cân lại nguyên liệu trên board; công thức tự ghép hai nguyên liệu có cơm loại trừ mayonnaise và các loại trứng cá. Các món Gunkan trứng cá tiếp tục dùng nguyên liệu tương ứng.

Tên file tiếng Việt của cá bơn/cá thu trong các món thành phẩm và lớp Sashimi bị đảo so với tên tiếng Anh `hirame`/`mackerel` ở nguyên liệu/top Nigiri. Bảng và các lớp ghép dưới đây ưu tiên tên file tiếng Việt đúng theo nguồn, không đổi ID nguyên liệu. Màu `roe_vang` của món dùng nguyên liệu `roe_orange` trong cùng bộ.

`SushiDishArt.asset` giữ 68 liên kết ảnh: 64 món đang dùng được gán đúng ảnh trong thư mục nguồn; 4 liên kết ID 9–12 thuộc các món đã bỏ, không có ảnh tương ứng trong bộ mới nên chỉ giữ lại data cũ. Không thêm lại 4 món vào enum hoặc order. PNG, metadata và định nghĩa atlas cũ vẫn được giữ.

`SushiIngredientArt.asset` dùng 17 ảnh trong bộ nguồn, kể cả ấm/tách trà và đá. Chỉ 4 nguyên liệu không có trong bộ (mè, thanh cua, dưa leo, lươn) dùng ảnh bên ngoài. `SushiPlatedIngredientArt.asset` dùng cùng bộ nguồn cho đủ 168 lớp theo từng món và 50 lớp dùng chung trong `ingredients`, `familyOverrides`, `recipeOverrides`; các vị trí, kích thước và thứ tự lớp đã chỉnh được giữ nguyên. Công cụ Rebuild Art References ưu tiên bộ nguồn khi gặp ảnh atlas cũ; Apply Refreshed Sprite Art không được ghi đè món/nguyên liệu đã có ảnh trong bộ này.

Trong `SushiPlatedIngredientArt.asset`, `sashimiTwoIngredientPlate` dùng `sashimi_x2/dia_setup.png`, còn `sashimiThreeIngredientPlate` dùng `sashimi_x3/dia_setup.png`. Hai đĩa trang trí được chọn theo số nguyên liệu cho cả món catalog lẫn tự ghép, hiện từ lúc ghép nguyên liệu và đi cùng món tới order. Ảnh order catalog đã có sẵn đĩa nên không chồng thêm; order tự ghép dùng đúng đĩa phía sau các lớp nguyên liệu. Các món ngoài Sashimi giữ đĩa đã gán trong prefab.

| ID | Tên món | Công thức (mỗi nguyên liệu ×1) | PNG trong thư mục nguồn |
| --- | --- | --- | --- |
| 1 | Nigiri cá ngừ | Cơm + Cá ngừ | `nigiri/nigiri_img/nigiri_ca_ngu.png` |
| 2 | Nigiri cá hồi | Cơm + Cá hồi | `nigiri/nigiri_img/nigiri_ca_hoi.png` |
| 3 | Nigiri cá bơn | Cơm + Cá bơn | `nigiri/nigiri_img/nigiri_ca_bon.png` |
| 4 | Nigiri cá thu | Cơm + Cá thu | `nigiri/nigiri_img/nigiri_ca_thu.png` |
| 5 | Nigiri tôm | Cơm + Tôm | `nigiri/nigiri_img/nigiri_tom.png` |
| 6 | Nigiri mực | Cơm + Bạch tuộc | `nigiri/nigiri_img/nigiri_muc.png` |
| 7 | Nigiri bơ | Cơm + Bơ | `nigiri/nigiri_img/nigiri_bo.png` |
| 8 | Nigiri trứng | Cơm + Trứng | `nigiri/nigiri_img/nigiri_trung.png` |
| 13 | Maki cá ngừ | Cơm + Rong biển + Cá ngừ | `maki/maiki_ca_ngu.png` |
| 14 | Maki cá hồi | Cơm + Rong biển + Cá hồi | `maki/maiki_ca_hoi.png` |
| 15 | Maki cá bơn | Cơm + Rong biển + Cá bơn | `maki/maiki_ca_bon.png` |
| 16 | Maki cá thu | Cơm + Rong biển + Cá thu | `maki/maiki_ca_thu.png` |
| 17 | Maki tôm | Cơm + Rong biển + Tôm | `maki/maiki_tom.png` |
| 18 | Maki bạch tuộc | Cơm + Rong biển + Bạch tuộc | `maki/maiki_bach_tuoc.png` |
| 19 | Maki bơ | Cơm + Rong biển + Bơ | `maki/miaki_bo.png` |
| 20 | Maki trứng | Cơm + Rong biển + Trứng | `maki/maiki_trung.png` |
| 21 | Gunkan cá ngừ | Cơm + Rong biển + Cá ngừ | `gunkan/gunkan_ca_ngu.png` |
| 22 | Gunkan cá hồi | Cơm + Rong biển + Cá hồi | `gunkan/gunkan_ca_hoi.png` |
| 23 | Gunkan cá bơn | Cơm + Rong biển + Cá bơn | `gunkan/gunkan_ca_bon.png` |
| 24 | Gunkan cá thu | Cơm + Rong biển + Cá thu | `gunkan/gunkan_ca_thu.png` |
| 25 | Gunkan tôm | Cơm + Rong biển + Tôm | `gunkan/gunkan_tom.png` |
| 26 | Gunkan bạch tuộc | Cơm + Rong biển + Bạch tuộc | `gunkan/gunkan_bach_tuoc.png` |
| 27 | Gunkan bơ | Cơm + Rong biển + Bơ | `gunkan/gunkan_bo.png` |
| 28 | Gunkan trứng | Cơm + Rong biển + Trứng | `gunkan/gunkan_trung.png` |
| 29 | Gunkan trứng cá vàng | Cơm + Rong biển + Trứng cá vàng | `gunkan/gunkan_roe_vang.png` |
| 30 | Gunkan trứng cá đen | Cơm + Rong biển + Trứng cá đen | `gunkan/gunkan_roe_den.png` |
| 31 | Gunkan trứng cá xanh | Cơm + Rong biển + Trứng cá xanh | `gunkan/gunkan_roe_xanh.png` |
| 32 | Sashimi cá ngừ + cá hồi | Cá ngừ + Cá hồi | `sashimi_x2/sashimi_x2_img/32.png` |
| 33 | Sashimi cá ngừ + cá bơn | Cá ngừ + Cá bơn | `sashimi_x2/sashimi_x2_img/33.png` |
| 34 | Sashimi cá ngừ + cá thu | Cá ngừ + Cá thu | `sashimi_x2/sashimi_x2_img/34.png` |
| 35 | Sashimi cá ngừ + tôm | Cá ngừ + Tôm | `sashimi_x2/sashimi_x2_img/35.png` |
| 36 | Sashimi cá ngừ + bạch tuộc | Cá ngừ + Bạch tuộc | `sashimi_x2/sashimi_x2_img/36.png` |
| 37 | Sashimi cá hồi + cá bơn | Cá hồi + Cá bơn | `sashimi_x2/sashimi_x2_img/37.png` |
| 38 | Sashimi cá hồi + cá thu | Cá hồi + Cá thu | `sashimi_x2/sashimi_x2_img/38.png` |
| 39 | Sashimi cá hồi + tôm | Cá hồi + Tôm | `sashimi_x2/sashimi_x2_img/39.png` |
| 40 | Sashimi cá hồi + bạch tuộc | Cá hồi + Bạch tuộc | `sashimi_x2/sashimi_x2_img/40.png` |
| 41 | Sashimi cá bơn + cá thu | Cá bơn + Cá thu | `sashimi_x2/sashimi_x2_img/41.png` |
| 42 | Sashimi cá bơn + tôm | Cá bơn + Tôm | `sashimi_x2/sashimi_x2_img/42.png` |
| 43 | Sashimi cá bơn + bạch tuộc | Cá bơn + Bạch tuộc | `sashimi_x2/sashimi_x2_img/43.png` |
| 44 | Sashimi cá thu + tôm | Cá thu + Tôm | `sashimi_x2/sashimi_x2_img/44.png` |
| 45 | Sashimi cá thu + bạch tuộc | Cá thu + Bạch tuộc | `sashimi_x2/sashimi_x2_img/45.png` |
| 46 | Sashimi tôm + bạch tuộc | Tôm + Bạch tuộc | `sashimi_x2/sashimi_x2_img/46.png` |
| 47 | Trà nóng | Ấm trà + Tách trà | `tea_pot/tra_nong.png` |
| 48 | Trà đá | Ấm trà + Tách trà + Đá | `tea_pot/tra_da.png` |
| 49 | Sashimi cá ngừ + cá hồi + cá thu | Cá ngừ + Cá hồi + Cá thu | `sashimi_x3/sashimi_x3_img/49.png` |
| 50 | Sashimi cá ngừ + cá hồi + tôm | Cá ngừ + Cá hồi + Tôm | `sashimi_x3/sashimi_x3_img/50.png` |
| 51 | Sashimi cá ngừ + cá thu + tôm | Cá ngừ + Cá thu + Tôm | `sashimi_x3/sashimi_x3_img/51.png` |
| 52 | Sashimi cá hồi + cá thu + tôm | Cá hồi + Cá thu + Tôm | `sashimi_x3/sashimi_x3_img/52.png` |
| 53 | Sashimi cá ngừ + cá hồi + cá bơn | Cá ngừ + Cá hồi + Cá bơn | `sashimi_x3/sashimi_x3_img/53.png` |
| 54 | Sashimi cá ngừ + cá bơn + cá thu | Cá ngừ + Cá bơn + Cá thu | `sashimi_x3/sashimi_x3_img/54.png` |
| 55 | Sashimi cá ngừ + cá bơn + tôm | Cá ngừ + Cá bơn + Tôm | `sashimi_x3/sashimi_x3_img/55.png` |
| 56 | Sashimi cá hồi + cá bơn + cá thu | Cá hồi + Cá bơn + Cá thu | `sashimi_x3/sashimi_x3_img/56.png` |
| 57 | Sashimi cá hồi + cá bơn + tôm | Cá hồi + Cá bơn + Tôm | `sashimi_x3/sashimi_x3_img/57.png` |
| 58 | Sashimi cá bơn + cá thu + tôm | Cá bơn + Cá thu + Tôm | `sashimi_x3/sashimi_x3_img/58.png` |
| 59 | Sashimi cá ngừ + cá hồi + bạch tuộc | Cá ngừ + Cá hồi + Bạch tuộc | `sashimi_x3/sashimi_x3_img/59.png` |
| 60 | Sashimi cá ngừ + cá bơn + bạch tuộc | Cá ngừ + Cá bơn + Bạch tuộc | `sashimi_x3/sashimi_x3_img/60.png` |
| 61 | Sashimi cá ngừ + cá thu + bạch tuộc | Cá ngừ + Cá thu + Bạch tuộc | `sashimi_x3/sashimi_x3_img/61.png` |
| 62 | Sashimi cá ngừ + tôm + bạch tuộc | Cá ngừ + Tôm + Bạch tuộc | `sashimi_x3/sashimi_x3_img/62.png` |
| 63 | Sashimi cá hồi + cá bơn + bạch tuộc | Cá hồi + Cá bơn + Bạch tuộc | `sashimi_x3/sashimi_x3_img/63.png` |
| 64 | Sashimi cá hồi + cá thu + bạch tuộc | Cá hồi + Cá thu + Bạch tuộc | `sashimi_x3/sashimi_x3_img/64.png` |
| 65 | Sashimi cá hồi + tôm + bạch tuộc | Cá hồi + Tôm + Bạch tuộc | `sashimi_x3/sashimi_x3_img/65.png` |
| 66 | Sashimi cá bơn + cá thu + bạch tuộc | Cá bơn + Cá thu + Bạch tuộc | `sashimi_x3/sashimi_x3_img/66.png` |
| 67 | Sashimi cá bơn + tôm + bạch tuộc | Cá bơn + Tôm + Bạch tuộc | `sashimi_x3/sashimi_x3_img/67.png` |
| 68 | Sashimi cá thu + tôm + bạch tuộc | Cá thu + Tôm + Bạch tuộc | `sashimi_x3/sashimi_x3_img/68.png` |

## Level kiểm tra tất cả món

`Assets/_game/LevelData/SushiAllDishesDemo.asset` chứa đủ 64 món trong catalog, mỗi món một phần: 16 khách × 4 món, tổng 168 nguyên liệu trên 5 cột. Không có khách vội hoặc chướng ngại; lấy nguyên liệu theo hàng từ trái sang phải sẽ theo đúng thứ tự order. Dữ liệu dùng `number = 51`, băng chuyền 14 slot và không xáo trộn board. Có đủ cả Sashimi 2/3 loại để kiểm tra hai đĩa trang trí.

Để chơi thử:

1. Mở `Assets/_game/Scenes/Loading.unity`.
2. Gán `Assets/_game/LevelData/_AllDishesDemoLevelData.asset` vào trường **All Level Data** của component `DataManager`.
3. Bật **Is Play Testing Level** trên `GameManager`, rồi nhấn Play. Danh sách test chỉ có level này nên dùng index 0.
4. Khi thử xong, gán lại `Assets/_game/LevelData/_AllLevelData.asset` và tắt **Is Play Testing Level** để dùng campaign.

Đã kiểm tra bằng `SushiLevelData.ValidateData()` và `CreateBoardData()`: đủ 64 ID không trùng, số nguyên liệu khớp từng loại, board đi theo thứ tự khách và scene GamePlay có đủ vị trí hàng chờ cho 16 khách. Chưa chạy Play Mode cho level này.
