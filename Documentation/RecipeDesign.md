# Công thức 2 và 3 nguyên liệu

[Ảnh các đĩa được render trong Unity](RecipePlatesPreview.png). Mỗi hàng, từ trái sang phải: 2 nguyên liệu có cơm, 2 nguyên liệu không cơm, rồi hai ví dụ 3 nguyên liệu có cơm.

Trong **SushiKhai → Level Editor**, chọn khách/món rồi chọn **Công thức**. Inspector của `SushiLevelData` cũng có các lựa chọn này:

| Kiểu | Thành phần | Quy tắc ghép |
| --- | --- | --- |
| 2 nguyên liệu — có cơm | Cơm + một nguyên liệu | Cơm lên đĩa trước |
| 2 nguyên liệu — không cơm | Hai loại nguyên liệu khác nhau | Nhận loại nào trước cũng được |
| 3 nguyên liệu — cơm đầu tiên | Cơm + hai loại nguyên liệu khác nhau | Cơm lên đĩa trước; sau đó nhận hai loại còn lại theo thứ tự bất kỳ |
| 3 nguyên liệu — không cơm | Ba loại nguyên liệu khác nhau | Nhận loại nào trước cũng được |

Cơm được thêm tự động cho kiểu có cơm. Có thể chọn mọi loại thực phẩm từ Tôm đến Rong biển, gồm mè, thanh cua, dưa leo, lươn và các loại trứng cá; không bắt buộc rong biển cho món 3 nguyên liệu. Trà/cốc/đá tiếp tục dùng món nước trong **Món có sẵn**. Các nguyên liệu trong cùng món phải khác loại.

Với món **3 nguyên liệu có cơm**, chọn riêng từng order `Hình cơm`: **3 cục** hoặc **1 cục**. Mặc định là 3 cục để giữ nguyên cách hiển thị của các level và món cũ. Tùy chọn này áp dụng cho cả món catalog ba thành phần và công thức tự ghép; món 2 nguyên liệu tiếp tục dùng hình cơm riêng của loại đó.

`DishOrderData.GetRecipe()` là điểm đọc công thức chung cho gameplay, sinh board, tính độ khó và tutorial. Mỗi `amount` là một phần món. Đảo thứ tự các nguyên liệu vẫn là cùng công thức và được gộp tiến độ trên cùng thẻ order. Các tổ hợp khác nhau có thẻ riêng.

Các level cũ mặc định `recipeKind = Catalog`, giữ nguyên ID của 48 món. Maki/Gunkan hiện liệt kê cơm đầu tiên. Dùng `SushiCustomRecipesDemo.asset` trong `Assets/_game/LevelData` để xem các kiểu công thức; asset mẫu chưa được thêm vào campaign.

## Ảnh trên đĩa và order

- Chỉnh thông số trong `SushiPlatedIngredientArt.asset` ở entry đúng **Recipe Kind + Type**. Món lên đĩa lấy trực tiếp `Sprite`, `Position`, `Size`, `Rotation` và `Sorting Order` của entry đó. `Size X/Y` áp dụng độc lập chiều rộng/cao theo đơn vị local, không co theo cạnh nhỏ hơn; `Position` không bị cộng độ lệch tự động. Khi sửa asset trong Play Mode, món đã nằm trên đĩa và ảnh order ghép lớp cập nhật ngay. `recipeOverrides` được ưu tiên hơn `familyOverrides` rồi `ingredients`; Sorting Order cộng mức nền 18 khi trên đĩa hoặc 31 khi bay. Muốn tách hai topping, đặt Position khác nhau trong data.
- `SushiPlatedIngredientArt.asset → recipeOverrides` chọn ảnh theo **recipeKind + type**, ưu tiên trước `familyOverrides` và `ingredients`. Đã gán bộ 3 nguyên liệu cho 18 thực phẩm: nền cơm `monan/com`, topping nhỏ và tách vị trí. Món 2 nguyên liệu có cơm tiếp tục dùng nền/top nigiri; món không cơm có bộ ảnh rời cho 17 topping, đặt cạnh nhau.
- `SushiDishArt.asset → customRecipes` cho phép gán **ảnh order hoàn chỉnh** theo kiểu công thức và các nguyên liệu tương ứng. Cơm được hiểu ngầm ở kiểu có cơm. Kiểu 3 nguyên liệu không cơm có ba trường nguyên liệu; các kiểu công thức có khóa riêng, không lấy nhầm ảnh của nhau.
- Nếu chưa gán ảnh hoàn chỉnh, tổ hợp trùng món có sẵn dùng ảnh catalog; tổ hợp mới ghép các lớp ảnh nguyên liệu ngay trên thẻ order, cùng ảnh/vị trí/thứ tự lớp với đĩa phục vụ. Mở chi tiết công thức vẫn hiển thị icon nguyên liệu gốc.
- Ảnh nguyên liệu trên board/băng chuyền vẫn nằm trong `SushiIngredientArt.asset`; ảnh theo kiểu công thức được áp dụng khi nguyên liệu bay lên đĩa.

Kiểm tra công thức: **SushiKhai → Tests → Check Recipes**. Bộ kiểm tra bao phủ 48 món cũ và mọi tổ hợp tùy chỉnh 2/3 nguyên liệu có cơm hoặc không cơm, thứ tự ghép, số phần, khóa gộp món và nguyên liệu không hợp lệ.
