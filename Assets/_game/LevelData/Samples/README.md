# 5 level mẫu Sushi Pop

Mở `Assets/Scenes/SampleScene.unity` và nhấn Play. Scene đã gán đúng 5 asset theo thứ tự dưới đây và bắt đầu từ mẫu 1. Dùng **Màn trước / Chơi lại / Màn sau** để thử từng trường hợp, kể cả khi popup hết giờ đang mở. Thắng màn vẫn có nút sang màn tiếp.

Số level giữ mốc 10/20/30/40/46 để dữ liệu tuân theo luật mở element. Số thứ tự bộ mẫu trên HUD là 1/5 đến 5/5.

| Mẫu | Asset | Nội dung | Khách vội | Slot băng |
| --- | --- | --- | --- | --- |
| 1 · level 10 | `Sample01_Hidden.asset` | ×1–×3, đúng 3 ô ẩn ở hàng 2 | Không | 12 |
| 2 · level 20 | `Sample02_QueuedRush.asset` | Maki, Gunkan, Nigiri, trà/đá, ô ẩn | Khách thứ 3: **45 giây**, dùng công thức mặc định, chờ đến ghế mới chạy | 12 |
| 3 · level 30 | `Sample03_FrozenRush.asset` | Băng 2 lớp ở đầu cột, băng 1 lớp hàng 2; ô ẩn | Khách đầu: **60 giây**, ghi đè bằng `rushSeconds` | 14 |
| 4 · level 40 | `Sample04_LinkedRush.asset` | Một cặp nối lệch hàng; ô ẩn + băng 2 lớp | Hai khách đầu: **75 và 90 giây** | 14 |
| 5 · level 46 | `Sample05_AllElements.asset` | Đủ 5 element, khay 2 và 3 ngăn, khay vắt hàng 4–5 | Khách đầu **18 giây**; khách thứ 4 **60 giây** khi đến lượt | 12 |

Tổng nguyên liệu của 5 mẫu lần lượt là **16, 15, 19, 19, 21**. Tổng từng loại khớp chính xác các món khách order. Cả bộ có đủ **17 loại nguyên liệu** được dùng trong 48 công thức; không đưa nguyên liệu thừa vào board.

## Thử nhanh

- **Ô ẩn:** lấy đầu cột ở mẫu 1 để ô dấu `?` lên hàng 1 và tự lật. Có cả lượng ×2/×3 để thử giữ đủ slot.
- **Khách đang chờ:** mẫu 2 có khách thứ 3 vội; đồng hồ không chạy trong hàng đợi. Hoàn thành một khách trước để đồng hồ bắt đầu.
- **Băng:** mẫu 3, lấy cột 1 hai lần sẽ phá băng 2 lớp ở đầu cột 2. Ô ×2 chỉ trừ một lớp mỗi lần lấy.
- **Ô nối:** mẫu 4, cột 2 đang nối với ô ở hàng 2 cột 3. Lấy đầu cột 3 trước rồi lấy cặp. Thiếu slot cho tổng lượng thì cả hai giữ nguyên.
- **Thắng khách 18 giây:** mẫu 5, chạm khay cơm/tôm ở đầu cột 1 để cung cấp ngay món của khách đầu. Đồng hồ hết thì mở popup revive; chọn **Thua level** để thử thua/chơi lại.
- **Swap Row và khay:** mẫu 5 có khay 3 ngăn ở đầu cột 4 và khay 2 ngăn ở hàng 4–5 cột 6. Swap Row phải giữ cả khay, kể cả ngăn thứ 5 chưa thấy lúc bắt đầu.
- **Booster:** Cây gắp không chọn băng/nối/khay. Magnet có thể lấy một phần ×n, ưu tiên ô thường, rồi nối/khay, cuối cùng băng.

## Chỉnh thời gian

Chọn asset level → `customers` → khách cần sửa:

- `rushCustomer = true` để bật khách vội.
- `rushSeconds > 0` là số giây cụ thể, ví dụ 18/60/75/90.
- `rushSeconds = 0` dùng **20 + 5 × tổng nguyên liệu của cả đơn**, tính cả số phần món. Khách vội của mẫu 2 có 5 nguyên liệu nên là 45 giây.

Bấm **Chơi lại** sau khi đổi dữ liệu để nạp lại timer. Trong game, timer bắt đầu khi ngồi; chọn booster vẫn chạy. Mở popup thì dừng. Hết giờ dùng luồng revive/thua đã có. Nút thanh toán revive chỉ hoạt động khi gán `SushiReviveProvider` cho ví xu/SDK quảng cáo.

## Art nguyên liệu

Đã gán **21 sprite phác họa màu không chữ, nền trong suốt**, gồm đủ 17 loại trong 48 công thức và 4 loại dữ liệu cũ. Xem [art tổng hợp](../../Art/Ingredients/IngredientsPreview.png) và [danh sách ID/type/art](../../Art/Ingredients/README.md). Số ×n và nắp ẩn/băng vẫn hiển thị theo trạng thái.

Toàn bộ dùng chung `Assets/_game/Prefabs/Ingredient.prefab`. Muốn thay art, sửa sprite tại đúng `type` trong `Assets/_game/Art/Ingredients/SushiIngredientArt.asset`. Hình được gán lại theo type mỗi lần lấy object từ pool. `Customer.prefab` cũng đã gán danh mục này; thẻ order hiện icon và số tiến độ. Nguyên liệu nhảy đến đúng icon của khách, thu nhỏ dần khi tới nơi. Đã gán đủ 48 ảnh món qua `SushiArtCatalog.dishImages`; ảnh món hiện từ lúc khách ngồi, đủ nguyên liệu thì thẻ xanh và có dấu hoàn thành.

Hai ghế nằm cân hai bên phía trên băng. Ba khách chờ nhỏ hơn ở hàng phía sau; order chỉ hiện khi đã tới ghế. Điểm giao tự lấy từ vị trí ghế, tránh nhầm sang vị trí khách chờ.

Đĩa đã có sprite `Art/ConveyorPlate.png`, đường băng hiện theo spline; dòng gợi ý nằm dưới các nút booster. Đã render kiểm tra bố cục mẫu 5 ở 720 × 1280 khi chờ, đang nhảy và nhận nguyên liệu.

## Kiểm tra dữ liệu

Đã chạy `SushiLevelData.ValidateData()` cho cả 5 mẫu, đối chiếu đủ 17 nguyên liệu và tìm được đường lấy hợp lệ xét sức chứa băng, lớp băng, cặp nối, khay và thứ tự phục vụ hai ghế.

Ngày 22/10/2026 đã chạy tiếp bằng **Unity 6000.3.15f1 batch Play Mode**, dùng component game thật, prefab Ingredient chung, đủ 21 sprite và các asset level này. Thời gian dưới đây là thời gian mô phỏng, không phải yêu cầu người chơi bấm hết màn trong số giây đó:

| Mẫu | Tổng nguyên liệu | Thời gian mô phỏng hoàn thành | Khách vội đã ngồi từ đầu |
| --- | --- | --- | --- |
| 1 | 16 | 32,2 giây | Không |
| 2 | 15 | 31,0 giây | Khách vội vào sau hoàn thành kịp giờ |
| 3 | 19 | 26,0 giây | Còn 46,3 giây khi xong đơn |
| 4 | 19 | 24,5 giây | Hai khách còn 62,0 và 76,2 giây |
| 5 | 21 | 27,9 giây | Khách 18 giây còn 8,5 giây khi xong đơn |

Cả 5 mẫu đều thắng với board/băng trống và mọi đơn hoàn thành. Đã thử riêng mẫu 5: chọn Magnet vẫn trừ giờ; mở popup dừng giờ; hết 18 giây mở revive cho đúng khách; từ chối thì thua; Chơi lại xóa popup và khởi động lại 18 giây. Cùng lượt kiểm tra đã qua 27 nhóm gameplay và 441 assertions công thức, gồm giao đúng ghế/loại nguyên liệu khi đích di chuyển. Fixture kiểm tra mô phỏng tween/thời gian trong scene tối giản.

Các asset cũ `SushiLevel`, `SushiRecipesDemo`, `SushiElementsDemo` vẫn được giữ trong thư mục cha; scene mẫu hiện dùng bộ 5 màn này.
