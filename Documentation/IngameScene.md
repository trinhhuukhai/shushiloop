# Giao diện ingame đặt sẵn

Ảnh dùng trực tiếp từ `Assets/_game/Art/ingame`. Các ảnh `UI_ingame*.png` là ảnh tham khảo tổng thể; gameplay dùng từng ảnh thành phần để số level, xu, tiến độ và order vẫn cập nhật theo dữ liệu.

Mở `Assets/_game/Scenes/GamePlay.unity`:

- **Ingame art**: Background, Restaurant backdrop, Serving counter, Conveyor table, Conveyor frame. Thay `Sprite`, vị trí, scale và sorting order ngay trên đối tượng trong scene. Các đối tượng này tồn tại trước khi Play và không bị tạo lại khi replay.
- **Ingame UI**: hai instance prefab `UICGamePlay` và `UICFloating`, có thể chỉnh header, xu, nút Settings, thanh khách và booster trong scene hoặc mở prefab gốc. `SushiSceneUI` đăng ký hai instance này với `UIManager`; hệ thống mở/đóng UI dùng lại chúng.
- **BoardManager**: vị trí là tâm hàng đầu, `First Row Offset = 0` trong GamePlay. `Column Width` và prefab `FoodQueue` điều khiển khoảng cách các cột. Số cột/nguyên liệu vẫn theo level.
- **CustomerManager**: sprite nhân vật lấy từ `char2`, `char1`, `char3`. Trong `Customer.prefab`, `Portrait viewport` cắt phần thân dưới ảnh toàn thân; bảng order, thanh thời gian và slot đĩa đều có sẵn.

Các prefab đã gán ảnh mới: `Ingredient`, `FoodQueue`, `Dia`, `PreparedDish`, `ServingCounter`, `Customer`, `DishCard`, `UICGamePlay`, `UICFloating`. Khay dùng `tray`/`tray_selecct`; số lượng dùng ảnh `x2`/`x3`; bàn dùng `serve_table`, `serve_plate`, `serve_empty_slot`; khung order và tick dùng ảnh riêng. Thanh thời gian chọn ảnh xanh/cam/đỏ theo dữ liệu.

`LevelManager` điều khiển dữ liệu và mở UI qua `UIManager`. `SushiRestaurantView` không còn dựng nền; `ConveyorManager` không còn tạo khung băng chuyền hoặc LineRenderer trang trí. Các đối tượng gameplay thay đổi theo level (khách, cột, nguyên liệu, đĩa, hiệu ứng) vẫn được tạo theo dữ liệu như trước. Bấm món để xem công thức được giữ nguyên.

Menu **SushiKhai → Art → Apply Ingame Art To Scene And Prefabs** là công cụ Editor để áp lại bố cục mặc định từ bộ ảnh này. Công cụ không tự chạy khi import hay Play; không cần chạy lại sau mỗi lần sửa scene. Khi đã chỉnh bố cục riêng, tránh chạy lại menu vì nó áp lại các kích thước/vị trí mặc định.

Kiểm tra bằng `SushiIngameSceneChecks.RunBatch` trong project kiểm tra riêng: scene dùng sprite ingame, UIManager dùng đúng instance HUD trong scene, không sinh HUD/nền trùng, ảnh trang trí không chặn pick, xem/đóng công thức và Settings → Transis → replay. Công cụ chụp ở 720×1280, 1080×2400 và 1536×2048. Chưa kiểm tra trên thiết bị thật.
