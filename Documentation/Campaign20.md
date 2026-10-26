# Bộ 20 level

Board tối đa 5 cột; dữ liệu được lưu cố định, replay giữ nguyên bố cục. Không thêm khách có timer.
Tutorial tạm tắt. Mốc xuất hiện: dấu ? ở 5, băng ở 7, link ở 9, tray ở 11.
Các màn giới thiệu cơ chế giữ khối lượng gần màn trước; các màn sau tăng số khách, món 3 nguyên liệu và số nhóm đặc biệt, rồi giảm slot còn 8.

| LV | Tên | Khách | Món | Nguyên liệu | Cột | Slot | ? | Ô băng (lớp tối đa) | Cặp link | Khay |
|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | Mẻ sushi đầu tiên | 2 | 2 | 4 | 3 | 8 | 0 | 0 (0) | 0 | 0 |
| 2 | Thêm một vị khách | 3 | 3 | 6 | 3 | 8 | 0 | 0 (0) | 0 | 0 |
| 3 | Nhớ món khách gọi | 4 | 4 | 8 | 3 | 8 | 0 | 0 (0) | 0 | 0 |
| 4 | Phục vụ hai món | 4 | 6 | 12 | 4 | 8 | 0 | 0 (0) | 0 | 0 |
| 5 | Điều bí mật | 4 | 8 | 16 | 4 | 9 | 2 | 0 (0) | 0 | 0 |
| 6 | Cuộn maki đầu tiên | 5 | 10 | 22 | 4 | 9 | 3 | 0 (0) | 0 | 0 |
| 7 | Làm tan băng | 5 | 10 | 22 | 4 | 10 | 3 | 1 (1) | 0 | 0 |
| 8 | Nhịp bếp đều tay | 6 | 12 | 28 | 4 | 10 | 4 | 2 (1) | 0 | 0 |
| 9 | Cặp nguyên liệu | 6 | 12 | 28 | 5 | 10 | 4 | 2 (2) | 1 | 0 |
| 10 | Chọn đúng thứ tự | 7 | 14 | 34 | 5 | 10 | 5 | 3 (2) | 1 | 0 |
| 11 | Khay phục vụ | 7 | 14 | 34 | 5 | 10 | 5 | 3 (2) | 1 | 1 |
| 12 | Chuẩn bị chỗ trống | 8 | 16 | 40 | 5 | 10 | 6 | 4 (2) | 1 | 1 |
| 13 | Hai cặp kết nối | 8 | 16 | 40 | 5 | 10 | 6 | 4 (2) | 2 | 1 |
| 14 | Bếp bắt đầu đông | 9 | 18 | 46 | 5 | 9 | 7 | 5 (3) | 2 | 2 |
| 15 | Khay ba ngăn | 9 | 18 | 46 | 5 | 9 | 7 | 5 (3) | 2 | 2 |
| 16 | Phối hợp cả bếp | 10 | 20 | 52 | 5 | 9 | 8 | 6 (3) | 2 | 2 |
| 17 | Chừa chỗ trên băng chuyền | 10 | 20 | 52 | 5 | 8 | 8 | 6 (3) | 2 | 2 |
| 18 | Giờ cao điểm | 11 | 22 | 58 | 5 | 8 | 9 | 7 (4) | 3 | 3 |
| 19 | Bếp trưởng khéo tay | 11 | 22 | 58 | 5 | 8 | 10 | 7 (4) | 3 | 3 |
| 20 | Thử thách nhà hàng | 12 | 24 | 64 | 5 | 8 | 10 | 8 (4) | 3 | 3 |

## Chỉnh sửa

Các asset nằm ở Assets/_game/LevelData/Campaign/Level01.asset … Level20.asset; _AllLevelData là thứ tự chơi chính.
Có thể chỉnh orders/board trong Inspector rồi chạy Sushi > Validate 20 Level Campaign để kiểm tra. Build 20 Level Campaign tái tạo bộ mặc định.
Độ khó thực tế cần tiếp tục cân bằng bằng playtest người chơi; kiểm tra tự động xác nhận có lời giải, không thay thế đánh giá cảm giác chơi.

## Lời giải kiểm chứng

Chỉ số cột bên dưới bắt đầu từ 1; đợi món giao xong trước lượt tiếp theo. Đây là một lời giải, không nhất thiết ít lượt nhất.

- Level 1 (seed 1000): 2, 1, 3, 1. 4 taps; peak reserved 1/8; searched 3 states.
- Level 2 (seed 2000): 1, 2, 3, 1, 2, 3. 6 taps; peak reserved 1/8; searched 6 states.
- Level 3 (seed 3000): 1, 2, 3, 1, 2, 3, 1, 2. 8 taps; peak reserved 1/8; searched 8 states.
- Level 4 (seed 4000): 1, 2, 2, 3, 2, 1, 3, 4, 4, 1. 10 taps; peak reserved 2/8; searched 10 states.
- Level 5 (seed 5000): 1, 4, 2, 3, 4, 1, 2, 3, 1, 4, 2, 3. 12 taps; peak reserved 2/9; searched 12 states.
- Level 6 (seed 6000): 3, 3, 1, 2, 1, 4, 2, 4, 4, 1, 2, 3, 1, 2, 3, 4, 1. 17 taps; peak reserved 2/9; searched 17 states.
- Level 7 (seed 7000): 1, 4, 2, 3, 3, 1, 2, 4, 1, 1, 2, 3, 4, 3, 2, 4, 1. 17 taps; peak reserved 2/10; searched 17 states.
- Level 8 (seed 8000): 3, 4, 2, 1, 3, 1, 2, 4, 1, 2, 3, 3, 4, 4, 1, 2, 2, 3, 4, 1, 1, 2. 22 taps; peak reserved 2/10; searched 22 states.
- Level 9 (seed 9000): 1, 2, 3, 3, 1, 4, 4, 5, 5, 2, 3, 4, 1, 2, 5, 1, 3, 1, 4, 5, 2. 21 taps; peak reserved 2/10; searched 21 states.
- Level 10 (seed 10000): 4, 1, 2, 3, 2, 5, 5, 4, 1, 3, 3, 2, 3, 1, 4, 2, 2, 5, 1, 4, 5, 4, 1, 3, 1. 25 taps; peak reserved 3/10; searched 25 states.
- Level 11 (seed 11000): 3, 2, 1, 1, 4, 5, 3, 2, 4, 4, 3, 4, 5, 2, 2, 1, 5, 5, 1, 2, 3, 4, 5, 1. 24 taps; peak reserved 4/10; searched 24 states.
- Level 12 (seed 12000): 1, 2, 4, 4, 2, 1, 3, 5, 3, 5, 4, 5, 2, 2, 1, 3, 1, 3, 4, 4, 2, 3, 1, 1, 5, 2, 5, 3, 1, 2. 30 taps; peak reserved 3/10; searched 30 states.
- Level 13 (seed 13001): 1, 2, 2, 3, 4, 5, 4, 1, 4, 3, 1, 4, 3, 5, 1, 3, 3, 2, 5, 2, 2, 1, 4, 5, 1, 4, 5, 1. 28 taps; peak reserved 4/10; searched 28 states.
- Level 14 (seed 14000): 1, 2, 2, 3, 4, 2, 1, 5, 3, 5, 4, 3, 3, 1, 4, 3, 4, 1, 1, 2, 5, 2, 2, 2, 1, 5, 5, 1, 3. 29 taps; peak reserved 3/9; searched 29 states.
- Level 15 (seed 15000): 4, 3, 2, 2, 5, 1, 3, 2, 1, 1, 1, 4, 4, 5, 3, 3, 1, 2, 3, 1, 4, 4, 5, 5, 1, 5, 5, 3, 3, 5. 30 taps; peak reserved 5/9; searched 30 states.
- Level 16 (seed 16000): 1, 5, 3, 4, 2, 3, 3, 1, 5, 1, 4, 1, 2, 2, 5, 5, 2, 1, 2, 4, 3, 3, 4, 2, 2, 5, 1, 4, 2, 4, 5, 5, 1, 1. 34 taps; peak reserved 4/9; searched 34 states.
- Level 17 (seed 17001): 3, 5, 1, 1, 2, 1, 2, 5, 4, 4, 5, 5, 3, 1, 3, 2, 4, 2, 4, 1, 3, 5, 3, 4, 1, 1, 5, 5, 3, 2, 5, 4, 4, 1, 4. 35 taps; peak reserved 5/8; searched 35 states.
- Level 18 (seed 18003): 3, 1, 2, 4, 1, 5, 4, 4, 3, 3, 5, 2, 2, 1, 1, 5, 2, 3, 4, 3, 3, 2, 4, 2, 5, 5, 5, 3, 4, 4, 5, 1, 1, 1, 2, 4. 36 taps; peak reserved 5/8; searched 36 states.
- Level 19 (seed 19000): 1, 5, 2, 4, 3, 2, 2, 3, 3, 4, 3, 1, 1, 1, 5, 4, 5, 3, 4, 2, 4, 1, 4, 1, 2, 4, 5, 5, 2, 2, 1, 1, 4, 3, 1, 2. 36 taps; peak reserved 4/8; searched 36 states.
- Level 20 (seed 20000): 4, 1, 1, 2, 3, 5, 5, 4, 2, 1, 3, 3, 2, 3, 5, 1, 3, 4, 4, 5, 5, 2, 5, 1, 1, 2, 2, 3, 4, 1, 5, 4, 5, 2, 1, 1, 4, 4, 2, 1. 40 taps; peak reserved 6/8; searched 40 states.
