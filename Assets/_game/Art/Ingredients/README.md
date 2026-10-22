# Toàn bộ nguyên liệu và art mẫu

Có **21 sprite PNG 512 × 512**, nền trong suốt, hình phác họa màu **không có chữ**. [Xem toàn bộ art](IngredientsPreview.png). Tên và ID nằm trong bảng bên dưới, không vẽ lên sprite.

Tất cả dùng **một prefab**: [Ingredient.prefab](../../Prefabs/Ingredient.prefab). Prefab giữ [SushiIngredientArt.asset](SushiIngredientArt.asset), ánh xạ từ `PoolType` sang sprite. `FoodShushi.SetupFromData` gán lại hình theo type mỗi lần tái sử dụng, trên board, băng chuyền và lúc giao khách.

Kéo prefab `Ingredient` vào scene rồi đổi **Pool Type** trên `FoodShushi`: hình cập nhật ngay trong Scene/Prefab Mode, không cần Play. Prefab mặc định hiển thị cơm. Component tự tìm renderer và gán danh mục art mặc định nếu thiếu reference; khi bật object trong Play Mode cũng nạp hình theo type đã chọn.

Đổi art: chọn `SushiIngredientArt.asset` → `ingredients` → kéo sprite mới vào đúng `type`. Ảnh tự co vừa ô; không tạo thêm prefab. Cả 21 type đã gán sprite. Không dùng `rawSprite`/`cookedSprite` hay nhãn tên nguyên liệu. Nền ô chỉ hiện trên board; lên băng và giao khách dùng riêng hình nguyên liệu.

`Customer.prefab` cũng đã gán danh mục này để thẻ order dùng cùng icon. Nguyên liệu bay đến đúng icon cùng loại trên thẻ của khách đang ngồi; chữ chỉ còn ở tên món, số tiến độ và timer.

Catalog chung nằm ở [Resources/SushiArtCatalog.asset](../../Resources/SushiArtCatalog.asset): `ingredientImages` trỏ tới danh sách này; `dishImages` trỏ tới [danh sách 48 món ăn](../Dishes/SushiDishArt.asset). Catalog còn giữ ảnh hỏi chấm, băng, dây nối và khay. Các component tự đọc catalog khi thiếu library riêng. Material đã dùng shader URP 2D Unlit của project.

Nếu thay/import lại hình, menu **SushiKhai → Art → Rebuild Art References** sẽ bổ sung reference bị thiếu bằng sprite thực Unity đã import, hỗ trợ cả ảnh slice Multiple và giữ những ảnh bạn tự gán trong Inspector. Không nhập tay fileID của sprite.

Nếu `SushiIngredientArt` báo **Missing Script** hoặc prefab hiện `Ingredient Art = None`, công cụ sẽ phục hồi script của asset trước rồi gán lại library và sprite lên `Ingredient.prefab`. Giữ nguyên GUID và danh sách ảnh hiện có; không cần tạo data nguyên liệu bằng tay. Công cụ tự chạy sau khi Unity reload script; menu trên có thể dùng để chạy lại.

| ID | PoolType | Nguyên liệu | Sprite | Sử dụng |
| --- | --- | --- | --- | --- |
| 1 | `SushiRice` | Cơm | [SushiRice.png](SushiRice.png) | 48 công thức |
| 2 | `Shrimp` | Tôm | [Shrimp.png](Shrimp.png) | 48 công thức |
| 3 | `Egg` | Trứng cuộn | [Egg.png](Egg.png) | 48 công thức |
| 4 | `Sesame` | Hạt mè | [Sesame.png](Sesame.png) | Dữ liệu cũ |
| 5 | `Salmon` | Cá hồi | [Salmon.png](Salmon.png) | 48 công thức |
| 6 | `CrabStick` | Thanh cua | [CrabStick.png](CrabStick.png) | Dữ liệu cũ |
| 7 | `Cucumber` | Dưa leo | [Cucumber.png](Cucumber.png) | Dữ liệu cũ |
| 8 | `Octopus` | Bạch tuộc | [Octopus.png](Octopus.png) | 48 công thức |
| 9 | `FishRoe` | Trứng cá cam | [FishRoe.png](FishRoe.png) | 48 công thức |
| 10 | `Tuna` | Cá ngừ | [Tuna.png](Tuna.png) | 48 công thức |
| 11 | `GrilledEel` | Lươn nướng | [GrilledEel.png](GrilledEel.png) | Dữ liệu cũ |
| 12 | `Flounder` | Cá bơn | [Flounder.png](Flounder.png) | 48 công thức |
| 13 | `Mackerel` | Cá thu | [Mackerel.png](Mackerel.png) | 48 công thức |
| 14 | `Avocado` | Bơ | [Avocado.png](Avocado.png) | 48 công thức |
| 15 | `Mayonnaise` | Mayonnaise | [Mayonnaise.png](Mayonnaise.png) | 48 công thức |
| 16 | `BlackFishRoe` | Trứng cá đen | [BlackFishRoe.png](BlackFishRoe.png) | 48 công thức |
| 17 | `GreenFishRoe` | Trứng cá xanh | [GreenFishRoe.png](GreenFishRoe.png) | 48 công thức |
| 18 | `Seaweed` | Rong biển | [Seaweed.png](Seaweed.png) | 48 công thức |
| 19 | `TeaPot` | Ấm trà | [TeaPot.png](TeaPot.png) | 48 công thức |
| 20 | `EmptyCup` | Cốc rỗng | [EmptyCup.png](EmptyCup.png) | 48 công thức |
| 21 | `IceCube` | Đá | [IceCube.png](IceCube.png) | 48 công thức |

17 loại được dùng trong 48 công thức hiện tại. Bốn type Hạt mè, Thanh cua, Dưa leo và Lươn nướng được giữ ID cho dữ liệu cũ, cũng có art đầy đủ. `None` (0) không phải nguyên liệu.

Scene mẫu đã gán chung prefab này cho Board và PoolControler.
