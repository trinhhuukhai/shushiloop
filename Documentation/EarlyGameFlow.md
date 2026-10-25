# Luồng các level đầu

- Level 1–4: Loading mở scene GamePlay và chuẩn bị level trực tiếp; chỉ mở UICGamePlay + UICFloating sau khi cover Loading đóng.
- Thắng level 1–4: Next mở UICTransis, giữ màn chơi tạm dừng, tải level kế tiếp, đóng transition rồi mới mở HUD.
- Home mở từ level 5: Settings có Home; Next sau khi thắng level 5 trở lên quay về MainMenu.
- Khi khởi động với tiến độ đã tới level 5, scene GamePlay được chuẩn bị nhưng chưa tạo board; Play ở MainMenu mới tải level qua UICTransis.
- Loading ban đầu hiển thị tối thiểu 1,65 giây. Chỉnh Minimum Startup Loading Duration trên GameManager trong scene Loading.

# Tutorial tạm tắt

Enable Tutorial trên SushiLevelOneTutorial (LevelManager trong scene GamePlay) mặc định tắt. Level 1 cho chọn nguyên liệu bình thường, không hiện bàn tay, không khóa input và không dừng timer. Việc bỏ qua không ghi cờ hoàn tất tutorial, để có thể bổ sung sau.

Prefab UICTutorial và các trường Target Column / Target Ingredient / Target Customer Index / Target Dish Index được giữ lại. Nếu bật lại sau này, kiểm tra lại đích theo dữ liệu level 1. Các chỉ số bắt đầu từ 0.

# Text slot băng chuyền

Scene GamePlay > ConveyorManager > Slot count là TextMeshPro trong world, hiển thị số đang chiếm / sức chứa (ví dụ 8/12), không có ảnh nền. Chỉnh Font Asset, Font Size, màu và material trực tiếp tại đây; runtime chỉ cập nhật nội dung, vị trí giữa băng chuyền và bật/tắt khi vào/ra level. Replay giữ nguyên object và thiết lập chữ.

Băng chuyền, khay và nguyên liệu vẫn là sprite trong world; các nút bấm thuộc UI Canvas.

# Kiểm thử

Chỉ chạy các entrypoint batch dưới đây trong bản sao riêng với PlayerSettings.productName = "Sushi UI Verification"; bộ test tự seed save cho bản sao.

- SushiEarlyGameRegression.RunBatch: HUD ẩn suốt Loading, level 1 không tutorial, input/timer, bộ đếm, replay, Settings, thắng 1→2 và 4→5, thắng 5→Home.
- SushiEarlyGameRegression.RunUnlockedBatch: startup đã mở Home, scene chưa tạo board, Play qua transition, thắng về Home.
- SushiCampaignRegression.RunBatch: kiểm tra 20 asset rồi chơi tới thắng bằng thao tác lấy nguyên liệu thông thường, không booster/revive.
- SushiGameplayRegression.RunBatch: cơ chế nguyên liệu, giới hạn mốc mở khóa, băng, link, tray và giao món.

Bảng thiết kế các màn ở Campaign20.md. Ảnh tutorial cũ chỉ là tham khảo cho tính năng đang tắt.
