# UI trong game

Chạy từ `Assets/_game/Scenes/Loading.unity`. Scene này nạp dữ liệu rồi chuyển sang `GamePlay`; mở riêng `GamePlay` khi chưa có các manager cũng sẽ quay qua `Loading`.

Các màn hình dùng `UIManager` / `UICanvas` và prefab trong `Assets/_game/Resources/UI`:

- `UICFloating`: ví xu và nút tạm dừng; giữ artwork và hiệu ứng có sẵn.
- `UICGamePlay`: level, số chỗ trên băng chuyền, tốc độ, số lượng booster, chọn/hủy booster.
- `UICVictory`: thưởng xu và chuyển màn; màn cuối cho chơi lại.
- `UICLose`: hồi sinh bằng xu, từ chối hồi sinh và chơi lại.
- `UICSetting`: tiếp tục, chơi lại hoặc dừng màn.

Chỉnh ảnh, font, màu, vị trí và kích thước ngay trong prefab. Các callback của nút được lưu trong `Button.onClick`; script chỉ cập nhật dữ liệu và trạng thái. `SushiProjectUISetup` là công cụ tạo/bind prefab trong Editor, không chạy trong game.

`DataManager.allLevelData` và `LevelManager.allLevelData` cùng tham chiếu `Assets/_game/LevelData/_AllLevelData.asset`. Thứ tự `listAllLevel` quyết định thứ tự chơi; `GameDataFragment.dataPlayer.cur_level` lưu chỉ số màn bắt đầu từ 0. Thưởng thắng chỉnh ở `LevelManager.victoryReward`.

HUD không pause game. Popup kế thừa cơ chế pause của `UICanvas`; đóng popup cuối cùng mới tiếp tục. Booster chỉ bị trừ khi tác vụ thành công, chọn rồi hủy không mất booster. Hồi sinh dùng ví của `GameDataFragment`, giá lần đầu 300 xu rồi 600 xu.

Chưa có SDK quảng cáo được nối vào dự án nên nút hồi sinh quảng cáo được ẩn. Có thể thay provider trên `LevelManager` bằng một `SushiReviveProvider` hỗ trợ SDK thật.
