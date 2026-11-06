# UI trong game

Chạy từ `Assets/_game/Scenes/Loading.unity`. Loading nạp dữ liệu và chuyển sang scene `GamePlay`, sau đó mở `UICMainMenu`; các manager đã tồn tại nhưng chưa tạo level, nguyên liệu, khách hay chạy timer. Nút **Level** mở `UICTransis`, dựng level dưới lớp chuyển cảnh rồi mới cho tương tác. Tween và tiến độ dùng unscaled time để hoạt động khi game đang pause. Nút Home trong Cài đặt dọn phiên chơi và quay về menu; Replay vẫn chơi lại màn hiện tại.

UI sử dụng `UIManager` / `UICanvas` và prefab trong `Assets/_game/Resources/UI`. Mọi Image, TMP, Button và callback được lưu trong prefab để chỉnh trực tiếp bằng Inspector, không dựng UI lúc chạy:

- `UICMainMenu`: nền nhà hàng, avatar/hồ sơ, xu, tim, sao, cài đặt, Level, Trang trí và thanh tab.
- `UICShop`: màn Shop, hiển thị booster đang có; chưa có giao dịch.
- `UICLeaderBoard`: màn Xếp hạng, hiển thị số màn đã hoàn thành; chưa có bảng xếp hạng online.
- `UICDecor`: bảng “Nâng cấp công trình” trượt từ dưới Home lên, có nút X, thanh tiến độ và danh sách thẻ 3 cột cuộn dọc. Xây lần lượt 12 hạng mục cố định của `kaiten_counter`, tổng 50 sao. Mỗi màn thắng lần đầu cho 1 sao; số sao trên Home là số sao còn lại sau khi xây. Không có chọn kiểu trang trí.
- `UICFloating` và `UICGamePlay`: ví xu/cài đặt và HUD ingame.
- `UICVictory`, `UICLose`, `UICSetting`: kết quả, hồi sinh và tạm dừng.

Menu giữ nguyên header và thanh điều hướng khi đổi tab. Nội dung trượt trái/phải theo thứ tự Shop–Home–Xếp hạng; nền chọn trượt ngang, icon được nâng và phóng nhẹ. Tween chạy theo thời gian thực, kết thúc chuyển cảnh đang chạy trước khi nhận lựa chọn mới để bấm liên tục vẫn về đúng tab. Chỉnh tốc độ tại `CanvasMainMenu.tabDuration` trong prefab.

Art menu nằm riêng trong `Assets/_game/Art/UI/MainMenu`, không dùng atlas nguyên liệu/khay/băng chuyền. `AtlasLayout.json` lưu rect sprite; `README.md` trong đó lưu prompt tạo art. Các nút và chữ là UI thật, không ghép vào ảnh nền.

Menu Editor **SushiKhai → UI → Build Main Menu and Tabs** import art và dựng/bind menu khi cần. Đây là thao tác authoring có chủ đích; các prefab đã lưu là nguồn dùng lúc runtime. `SushiProjectUISetup` phụ trách HUD ingame.

Dữ liệu level lấy chung từ `_AllLevelData.asset`; nút Level dùng `GameDataFragment.dataPlayer.cur_level` (index bắt đầu 0). Xu lấy từ ví; sao lấy từ các màn đã thắng, trừ tổng sao đã xây. Chơi lại một màn không sinh thêm sao.

Home có danh sách nhà hàng, mặc định gồm **Quán sushi** và **Vườn anh đào**. Vuốt ngang trực tiếp trên công trình hoặc bấm mũi tên để chuyển; hai đầu danh sách không quay vòng. Nhà hàng thứ hai mở sau khi **vượt level 30** (đang chơi level 30 vẫn khóa). Nhà hàng khóa có công trình mờ, biển đóng cửa và bảng điều kiện; chưa thể chơi hay nâng cấp từ trang đó. Vị trí đang xem được lưu, việc xem nhà hàng khác không đổi level đang chơi.

`Assets/_game/Resources/Decoration/Restaurants.asset` chứa danh sách, ID ổn định, tên, biển hiệu, màu và `unlockAfterLevel` (mốc level toàn chiến dịch, bắt đầu từ 1). Mốc 0 mở sẵn. Đổi tên/mốc trực tiếp trong Inspector; sau khi thêm nhà hàng hoặc đổi màu, chạy **Build Restaurant Decoration** để dựng các lớp hình vào prefab. Giữ ID đầu `kaiten_counter` để tương thích save cũ; ID các nhà hàng phải khác nhau. Các nhà hàng dùng chung cấu hình 12 hạng mục và ví sao.

Bấm Trang trí vẫn giữ tab Home và nhà hàng đang chọn hoạt động; `SushiHomeRestaurantLayout` dời công trình lên trên bảng nâng cấp, ẩn tạm nút chơi, mũi tên và các nút quà. Bấm thẻ nâng cấp trừ sao và chạy hiệu ứng trên đúng công trình ở Home, không tạo bản xem trước thứ hai hay chuyển trang/cảnh. Trong lúc mở bảng, thao tác không thể chuyển sang công trình khác. Đóng bằng X, Back hoặc tab Home sẽ phục hồi bố cục; chuyển Shop/Xếp hạng cũng đóng bảng. Thưởng hoàn thiện hiển thị ngay trong bảng, không mở hộp thoại che nhà hàng.

`SushiDecorationSetup` vẽ cửa hàng sushi, nền đường và các biểu tượng hạng mục rồi lưu vào prefab; có thể thay hình từng lớp trong Inspector mà giữ logic. Dựng lại qua **SushiKhai → UI → Build Restaurant Decoration** (giữ nguyên cấu hình chi phí hiện có). Dựng lại toàn bộ menu cũng gọi bước này. `SushiUIFitContent.referenceSize` cho phép bảng tự co vừa điện thoại và tablet.

`Assets/_game/Resources/Decoration/KaitenCounter.asset` chứa thứ tự, ID, tên Việt/Anh, iconKey, chi phí và thưởng hoàn thành (1.000 xu **mỗi nhà hàng**, nhận một lần). Nhà hàng đầu giữ `DataPlayer.restaurantDecorationCount` / `restaurantCompletionRewardClaimed` để đọc đúng save cũ; các nhà hàng sau lưu số hạng mục, sao đã chi và cờ thưởng riêng theo ID trong `DataPlayer.restaurants`. `selectedRestaurantId` lưu trang đang xem. Sao còn lại trừ tổng chi của tất cả nhà hàng, kể cả nhà hàng không đang xem. Sau mỗi lần xây, tiến độ và thưởng cuối được lưu cùng một lần trước hiệu ứng. Save cũ giữ nguyên tiến độ nhà hàng đầu. Hoàn thành hiển thị nhãn “Nhà hàng hoàn thiện” trên Home; trang trí không thay đổi gameplay hay booster.

Kiểm tra tự động chạy trong bản sao có productName `Sushi UI Verification`:
- `SushiMainMenuCapture.RunBatch`: chụp UI thật trên ba tỷ lệ màn hình, kiểm tra chuyển tab và bấm liên tục.
- `SushiProjectUIRegression.RunBatch`: Loading → GamePlay chưa có level → Home → UICTransis → Level, chặn bấm Play hai lần, gameplay/booster/hồi sinh, Home từ popup và vào chơi lại.
- `SushiDecorationRegression.RunBatch`: dựng hai prefab, kiểm tra dữ liệu và 13 mốc hiển thị, sau đó chạy luồng nâng cấp trực tiếp trên Home trong bản sao `SushiVerification`.
- `SushiDecorationRegression.RunFlowBatch`: chỉ chạy Play Mode; kiểm tra dùng chung nhà hàng, không chuyển tab/cảnh, thẻ khóa, nhấn lặp, đóng giữa hiệu ứng, thiếu sao, cuộn tới hạng mục tiếp theo, 12 nâng cấp, thưởng 1.000 xu, đóng/mở nhanh và chuyển tab. Chụp Home/bảng nâng cấp ở 0/12, 6/12, 12/12 trên ba tỷ lệ màn hình (bật graphics).
- `SushiRestaurantRegression.RunBatch`: kiểm tra mốc 29/30, save cũ, hai tiến độ riêng, ví sao chung, thưởng từng nhà hàng; chạy vuốt/mũi tên, giới hạn danh sách, khóa thao tác, mở khóa trực tiếp, lưu vị trí và nâng cấp nhà hàng thứ hai. Chụp trạng thái khóa/mở trên ba tỷ lệ màn hình, kết quả tại `Documentation/RestaurantRegressionResults.txt`.
