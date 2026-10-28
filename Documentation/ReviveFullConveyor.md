# Revive băng chuyền đầy

- Khi không còn nước đi và băng chuyền không thể tự giao món, hết thời gian chờ kẹt hiện có sẽ mở popup revive `FullConveyor`. Áp dụng cả khi còn slot nhưng không đủ chỗ cho ô ×n, cặp nối hoặc cả khay (ví dụ còn 2 slot nhưng nhóm cần 3–4 slot).
- Board còn nước đi hợp lệ thì không mở revive. Nếu còn slot nhưng tất cả đầu cột bị băng chặn, vẫn dùng revive phá băng hiện có.
- Chỉ nhận revive nếu có ít nhất 5 nguyên liệu đã đáp xuống đĩa và chưa giữ chỗ cho khách. Nguyên liệu đang bay, món đã giữ chỗ và túi cũ không được gom lại.
- Gom đúng 5 nguyên liệu vào một túi trên một trong các đĩa vừa dọn: trả lại chính xác 4 slot, giữ nguyên toàn bộ nguyên liệu.
- Túi đi cùng đĩa trên băng chuyền. Khi khách đang ngồi cần nguyên liệu, nguyên liệu bay từ miệng túi thẳng lên đĩa món trên bàn khách; vẫn theo thứ tự nền món trước topping. Túi không phải đợi tới điểm giao trên vòng xoay.
- Hết nguyên liệu thì túi ẩn và trả lại slot. Có thể có nhiều túi; mỗi túi chiếm 1 slot. Magnet cũng lấy được nguyên liệu trong túi sau khi gom xong.
- Dùng popup, giá xu và provider quảng cáo revive hiện có. Không thay đổi timer khách vội. Chơi lại/dọn màn hủy tween và trả nguyên liệu trong túi về pool.

Prefab: `Assets/_game/Prefabs/ReviveBag.prefab`. Art: `Assets/_game/Art/ReviveBag.png` (PNG có alpha). `ConveyorManager.reviveBagPrefab` tham chiếu prefab trong scene GamePlay; chỉnh hình, miệng túi và số lượng ngay trong prefab.

## Art

Generated with the built-in `image_gen` tool using the imagegen skill. The output was copied unchanged into the project. Final prompt:

> Use case: stylized-concept. Asset type: one isolated transparent PNG sprite for a polished casual mobile sushi conveyor game in Unity. Primary request: draw ONE small plump Japanese cloth ingredient pouch to sit on an existing round white ceramic plate. Subject: a rounded warm golden-beige fabric pouch with a gathered open neck and a short deep teal drawstring bow, a simple small dark green leaf motif on the front, generous soft folds, broad smooth highlights and warm brown contour shading. Show a slight view down from above at about 30 degrees, front facing, matching appetizing 2.5D hand-painted mobile sushi food icons. Readable silhouette at 80 pixels. Pouch is filled but no individual ingredients visible. Composition: centered, square 1024 by 1024, entire pouch visible, fills about 80 percent of frame, small transparent padding. True transparent background and transparent outside the silhouette, only a subtle contact shadow beneath the pouch. No plate, no table, no UI, no text, no numbers, no coins, no extra objects, no watermark. Final game-ready clean antialiased cutout, not a photo.

## Kiểm tra

`SushiGameplayRegression.RunReviveBagBatch` chạy các ca tập trung trong project Unity riêng: revive/trừ xu, giữ chỗ khi đang bay, giao đúng món từ túi, Magnet, revive nhiều lần và replay giữa animation.

Đã chạy thành công 8 nhóm kiểm tra trong Unity 6000.3.15f1, gồm cả chấp nhận/từ chối revive, callback thưởng quảng cáo giả lập và revive khi còn slot nhưng không đủ chỗ cho ô ×n, cặp nối hoặc khay. Không thay đổi hệ thống ẩn/hiện BoosterGroup.
