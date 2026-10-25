# UI trong game

Chạy từ `Assets/_game/Scenes/Loading.unity`. Loading nạp dữ liệu và chuyển sang scene `GamePlay`, sau đó mở `UICMainMenu`; các manager đã tồn tại nhưng chưa tạo level, nguyên liệu, khách hay chạy timer. Nút **Level** mở `UICTransis`, dựng level dưới lớp chuyển cảnh rồi mới cho tương tác. Tween và tiến độ dùng unscaled time để hoạt động khi game đang pause. Nút Home trong Cài đặt dọn phiên chơi và quay về menu; Replay vẫn chơi lại màn hiện tại.

UI sử dụng `UIManager` / `UICanvas` và prefab trong `Assets/_game/Resources/UI`. Mọi Image, TMP, Button và callback được lưu trong prefab để chỉnh trực tiếp bằng Inspector, không dựng UI lúc chạy:

- `UICMainMenu`: nền nhà hàng, avatar/hồ sơ, xu, tim, sao, cài đặt, Level, Trang trí và thanh tab.
- `UICShop`: màn Shop, hiển thị booster đang có; chưa có giao dịch.
- `UICLeaderBoard`: màn Xếp hạng, hiển thị số màn đã hoàn thành; chưa có bảng xếp hạng online.
- `UICDecor`: khung Trang trí để phát triển tiếp. Tiến độ trang trí 0/12 và tim 5/Full là trạng thái trình bày ban đầu; không thay đổi luật chơi hoặc tự trừ tài nguyên.
- `UICFloating` và `UICGamePlay`: ví xu/cài đặt và HUD ingame.
- `UICVictory`, `UICLose`, `UICSetting`: kết quả, hồi sinh và tạm dừng.

Menu giữ nguyên header và thanh điều hướng khi đổi tab. Nội dung trượt trái/phải theo thứ tự Shop–Home–Xếp hạng; nền chọn trượt ngang, icon được nâng và phóng nhẹ. Tween chạy theo thời gian thực, kết thúc chuyển cảnh đang chạy trước khi nhận lựa chọn mới để bấm liên tục vẫn về đúng tab. Chỉnh tốc độ tại `CanvasMainMenu.tabDuration` trong prefab.

Art menu nằm riêng trong `Assets/_game/Art/UI/MainMenu`, không dùng atlas nguyên liệu/khay/băng chuyền. `AtlasLayout.json` lưu rect sprite; `README.md` trong đó lưu prompt tạo art. Các nút và chữ là UI thật, không ghép vào ảnh nền.

Menu Editor **SushiKhai → UI → Build Main Menu and Tabs** import art và dựng/bind menu khi cần. Đây là thao tác authoring có chủ đích; các prefab đã lưu là nguồn dùng lúc runtime. `SushiProjectUISetup` phụ trách HUD ingame.

Dữ liệu level lấy chung từ `_AllLevelData.asset`; nút Level dùng `GameDataFragment.dataPlayer.cur_level` (index bắt đầu 0). Xu và sao trên Home lấy từ tiền và số màn đã hoàn thành trong save.

Kiểm tra tự động chạy trong bản sao có productName `Sushi UI Verification`:
- `SushiMainMenuCapture.RunBatch`: chụp UI thật trên ba tỷ lệ màn hình, kiểm tra chuyển tab và bấm liên tục.
- `SushiProjectUIRegression.RunBatch`: Loading → GamePlay chưa có level → Home → UICTransis → Level, chặn bấm Play hai lần, gameplay/booster/hồi sinh, Home từ popup và vào chơi lại.
