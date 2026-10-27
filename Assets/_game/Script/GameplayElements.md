# Sushi Pop gameplay data

## Giao diện ingame đặt sẵn trong scene

Từ cập nhật 27/10/2026, `GamePlay.unity` dùng ảnh trong `Assets/_game/Art/ingame`: nhóm **Ingame art** chứa nền, bàn và khung băng chuyền; nhóm **Ingame UI** chứa các instance `UICGamePlay`/`UICFloating` đặt sẵn. `SushiSceneUI` đăng ký chúng với `UIManager`, nên load/replay chỉ cập nhật dữ liệu và mở UI hiện có. `SushiRestaurantView` không dựng nền và `ConveyorManager` không tạo khung băng chuyền lúc chạy. Xem [hướng dẫn chỉnh scene và prefab](../../../Documentation/IngameScene.md).

Luật triển khai theo tài liệu người dùng cung cấp ngày 21/10/2026. Chưa đọc được Google Sheet để đối chiếu các thông số kinh tế.

## Order món và công thức

`SushiRecipeCatalog` có đủ 48 công thức, ID 1–48 trùng bảng thiết kế: 12 Nigiri, 8 Maki, 11 Gunkan, 15 Sashimi và 2 trà. Maki/Gunkan dùng rong biển + cơm + topping; đế và cách trình bày không phải nguyên liệu cần pick thêm.

Trong mỗi `CustomerOrderData`, dùng `dishes` gồm `{ dish, amount }`, để `orders` trống. Ví dụ `NigiriShrimp ×2` và `MakiShrimp ×1` cần 3 cơm + 3 tôm + 1 rong biển. Hệ thống tự sinh tổng nguyên liệu trên board, nhóm ×n và tính thời gian khách vội theo tổng này. `orders` nguyên liệu cũ vẫn được đọc khi không có dishes; validator không cho cấu hình cả hai.

Mỗi phần món có `SushiDishOrder` riêng. Ngay khi một nguyên liệu phù hợp đáp trên băng chuyền, hệ thống giữ chỗ nguyên liệu và một đĩa phục vụ của khách; nguyên liệu bay riêng lẻ lên đĩa và nằm lại ở đó trong lúc chờ các nguyên liệu còn thiếu. Khi đủ công thức, các nguyên liệu được thay bằng ảnh món hoàn chỉnh, rồi món bay thẳng tới thẻ order. `Customer.OnDishCompleted` chỉ phát khi món bay tới bảng order; `OnCompleted` chỉ phát khi mọi phần món đều hoàn thành. Nguyên liệu ghép dở không cộng tiến độ món.

`CustomerRecipeView` hiển thị **ảnh món ngay khi khách ngồi**, gộp các phần cùng công thức vào một icon với số đã nhận/tổng cần. Chạm vào order sẽ ẩn ảnh/tên/tiến độ món và mở công thức thành cột nguyên liệu có khoảng cách; mỗi khách chỉ mở một công thức tại một thời điểm. Khi nhận đủ, icon hiện dấu hoàn thành. Bảng order chọn art theo số nhóm công thức: 1 nhóm dùng `1x2`, 2–4 nhóm dùng `2x2`, và từ 5 nhóm trở lên dùng bảng lớn `2x3`; từ 2 nhóm trở lên được chia hai cột. Art bảng được gán trên prefab và vẫn có thể chỉnh trong Unity. Bộ art có đủ [48 hình món ăn](../Art/Dishes/README.md), nền trong suốt, không vẽ chữ lên hình. `FoodShushi` dùng hình nguyên liệu trên board/băng chuyền; `SushiPreparedDish` dùng ảnh thành phẩm khi đã ghép xong.

Các nguyên liệu thiếu đã được bổ sung vào cuối `PoolType` để giữ nguyên ID cũ. `FishRoe` (ID 9 cũ) là trứng cá cam. Có tổng cộng **21 type nguyên liệu**, gồm 17 loại trong công thức hiện tại và 4 loại giữ cho dữ liệu cũ; tất cả đã gán sprite phác họa màu **nền trong suốt, không có chữ**. Xem [danh sách đầy đủ ID/type/art](../Art/Ingredients/README.md) và [ảnh tổng hợp](../Art/Ingredients/IngredientsPreview.png).

## Một prefab nguyên liệu dùng chung

Các visual cố định đã được lưu trong prefab, không tạo lại bằng code khi `Setup`:

- `Prefabs/Ingredient.prefab`: `Ingredient image`, `Quantity` (TMP), `Board tile`, `Element` (TMP), `Cover`; tất cả được gán vào **Prefab visuals** của `FoodShushi`. Quantity/Element/Cover mặc định tắt, code bật theo số lượng/ẩn/băng. Chọn child hoặc bật tạm để chỉnh trong Prefab Mode. Font, màu và vị trí Quantity thông thường lấy từ prefab; vị trí khi đóng băng chỉnh ở các trường `Frozen ...`.
- `Prefabs/FoodQueue.prefab`: `Column conveyor` chứa Belt/Left rail/Right rail và 4 `Empty belt base`. Khay rỗng sau pick vẫn clone từ Board tile, trượt nhẹ + fade theo cấu hình hiện có.
- `Prefabs/Customer.prefab` chứa Canvas World Space, `Portrait viewport` cắt ảnh nhân vật toàn thân, Avatar (`Image` giữ tỉ lệ), vùng Orders với hai cột Layout Group, timer/badge/thanh countdown và template thẻ món (tắt). Portrait viewport có Canvas sorting order thấp hơn Orders để avatar luôn nằm dưới bảng order, kể cả khi nhiều khách chồng lên nhau. Chỉnh vị trí, kích thước và khoảng cách trực tiếp trên RectTransform/Layout Group trong prefab. Avatar chỉ nhận sprite, không bị code sửa scale/vị trí.
- `Prefabs/DishCard.prefab`: UI Image/TMP cho nền, tên món, ảnh món, dấu hoàn thành và hàng nguyên liệu. Khi mở công thức, `SushiDishCardView` chuyển hàng nguyên liệu thành cột, ẩn tên/ảnh món và tăng chiều cao thẻ; `CustomerRecipeView` nới bảng Orders để giữ khoảng cách giữa các thẻ. Ở trạng thái thường, binding chỉ gán dữ liệu/trạng thái và không đổi layout đã author trong prefab. Image bật Preserve Aspect để ảnh vừa khung do prefab quy định.
- `Prefabs/OrderIngredient.prefab`: Image, Progress TMP và Delivery slot; được lồng sẵn trong DishCard. Khi mở công thức, chỉ hiện ảnh nguyên liệu với alpha đầy đủ và kích thước lớn hơn; Progress chỉ dùng cho order thường. Các slot dư tắt theo công thức. Điểm nhận nguyên liệu và vùng chọn Magnet đọc đúng UI thực tế, không dùng tọa độ tính riêng trong code. Runtime nhân bản template theo số phần món; chỉnh style chung ở prefab con hoặc override trong CustomerVisuals.
- `Prefabs/Dia.prefab`: ảnh nằm trong child `Ceramic plate`, root giữ scale 1 cho nguyên liệu đáp đúng kích thước. `OrderView.prefab` cũ đã có sẵn icon và dấu hoàn thành nên giữ cấu trúc đó.

Menu **SushiKhai → Prefabs → Add Missing Visuals** chỉ dành cho bổ sung reference/visual thiếu trong Editor; không tự chạy khi import hoặc Play, không dựng lại visual đã được gán. Sprite panel và material viền được lưu thành asset trong Resources để reference trong prefab không bị mất khi đóng/mở Unity. Công cụ cập nhật art chỉ đổi sprite của `foodRenderer`, không ghi ảnh nguyên liệu lên nền/cover.

`BoardShishiManager.ingredientPrefab` trỏ tới `Assets/_game/Prefabs/Ingredient.prefab`. Board đăng ký mọi type nguyên liệu vào cùng prefab qua `SimplePool.RegisterPrefab`; pool lưu theo instance ID của prefab nên cả 21 type cùng dùng một pool. Scene mẫu cũng chỉ preload prefab này trong `PoolControler.otherPools`.

Prefab chứa `foodRenderer` và tham chiếu `SushiIngredientArtLibrary`. Asset sẵn có là `Assets/_game/Art/Ingredients/SushiIngredientArt.asset`, chứa 21 cặp `{ type, sprite }`. `FoodShushi.SetupFromData` luôn gán lại sprite theo `typeFood` và co vừa ô, kể cả khi object vừa được tái sử dụng cho một loại khác. Không cần `rawSprite`/`cookedSprite`, nhãn tên hay prefab riêng theo nguyên liệu. Nền ô chỉ hiển thị trên board; băng chuyền và khách dùng riêng sprite nguyên liệu.

Để thay art, chọn asset danh mục và kéo sprite mới vào đúng `type`. Các PNG mẫu có kích thước 512 × 512. Cả **Sprite (Single)** và sprite con của ảnh **Multiple** đều dùng được. `Customer.prefab.ingredientArt` đã gán cùng danh mục cho icon trên thẻ order.

## Hai danh sách ảnh và hình element

Chọn `Assets/_game/Resources/SushiArtCatalog.asset` trong Inspector. Catalog dùng chung có hai danh sách:

- `ingredientImages` → `Art/Ingredients/SushiIngredientArt.asset`: 21 cặp **PoolType → Sprite**.
- `dishImages` → `Art/Dishes/SushiDishArt.asset`: 48 cặp **SushiDishId → Sprite**.

Bốn field `hiddenTile`, `frozenTile`, `linkedTile`, `rigidTray` đã gán hình trong `Art/Elements`: nắp gỗ dấu hỏi, băng trong suốt, dây rơm và khay gỗ. Số lớp băng vẫn là nhãn cập nhật theo gameplay. Dây xoay theo hai ô, khay co theo 2–3 ngăn. Xem [hướng dẫn hình element](../Art/Elements/README.md).

[Ảnh chụp màn 46 trong Unity URP](../Art/GameplayArtPreview.png) có đủ hình nguyên liệu, món order và bốn hình element.

`FoodShushi`, `Customer` và `CustomerRecipeView` tự lấy catalog trong Resources khi chưa gán library riêng. Ảnh order và nguyên liệu dùng `Resources/SushiSpriteMaterial.mat`, shader **URP/2D/Sprite-Unlit-Default**, để hiển thị trong renderer 2D của project.

`Editor/SushiArtSetup.cs` tự bổ sung reference bị thiếu sau khi import/recompile, lấy **Sprite thực do Unity import** thay vì đoán fileID. Điều này xử lý cả PNG được slice thành Multiple. Ảnh đã tự chọn trong danh sách vẫn được giữ. Có thể chạy lại qua menu **SushiKhai → Art → Rebuild Art References**; kết quả ghi `Temp/SushiArtBindingReport.txt`. Khi thay art, chỉ kéo sprite mới vào đúng entry; không cần sửa code hoặc tạo prefab nguyên liệu khác.

Nếu danh mục ảnh báo **Missing Script**, công cụ sẽ import lại script đúng tên lớp, phục hồi `m_Script`/`m_EditorClassIdentifier` của asset rồi mới gán ảnh và prefab. Giữ nguyên GUID danh mục và các entry đã chỉnh; không xóa rồi tạo lại dữ liệu đang có. Nếu script không biên dịch/import được, Console sẽ báo rõ đường dẫn thay vì bỏ qua việc sửa. Asset thư viện đã có đầy đủ 21 nguyên liệu và 48 món; file `.meta` của các lớp art dùng MonoImporter.

`Ingredient.prefab` mặc định chọn `SushiRice` và gán trực tiếp sprite cơm trên `foodRenderer`. Đổi `Pool Type` của `FoodShushi` trong Inspector sẽ cập nhật hình ngay ở Scene/Prefab Mode, không cần nạp level. `OnValidate` hẹn đổi sprite qua Editor delayCall; khi thêm component hoặc thiếu reference, tự tìm renderer con và danh mục art mặc định. `OnEnable` cập nhật hình cho object đặt trực tiếp trong scene ở Play Mode; `SetupFromData` tiếp tục xử lý các lần lấy từ pool.

Đã kiểm tra cả 21 type bằng SerializedObject trong Unity Edit Mode: đổi `Pool Type` cập nhật đúng sprite ngay, tự gán lại renderer/library khi trống, không gọi `SetupFromData` hay vào Play Mode.

## Bố cục khách và vị trí nhận nguyên liệu

### Ghép món ở đĩa phục vụ và giao thẳng order

Nguyên liệu được pick vẫn giữ từng slot riêng trên đĩa trắng. Khi cả lượt pick đã đáp xuống và đủ công thức, hệ thống ưu tiên các món còn thiếu của khách đang ngồi (khách đến trước được ưu tiên), rồi mới đến hàng chờ. Hai hoặc ba nguyên liệu hội tụ thành **một món trên đĩa màu**, dùng lại một slot nguyên liệu; các slot còn lại được giải phóng. Không ghép một phần công thức và không tạo quá số phần order yêu cầu.

`PreparedDish.prefab` chứa đĩa màu và ảnh món. Khi đủ nguyên liệu, hệ thống giữ trước một đĩa tròn trống của khách đang ngồi; từng nguyên liệu bay từ băng chuyền tới đĩa đó theo thứ tự. Sau khi đáp đủ, ảnh món hoàn chỉnh hiện trên đĩa rồi nhảy thẳng lên bảng order. **Chỉ lúc đáp xuống bảng order mới cộng số phần và kiểm tra hoàn thành khách.** Popup dừng cả ghép món lẫn chuyến bay; replay dọn món và mọi reservation trước khi reset khách.

`Customer.prefab` có `Serving slots` với bốn vị trí bàn, phần avatar và bảng order đặt cạnh nhau. Các phần cùng công thức được gộp thành một icon trên `DishCard.prefab`, hiển thị `0/2`, `1/2`, rồi dấu tick khi đủ. Đồng hồ thời gian chỉ hiển thị với khách rush, chuyển màu xanh/cam/đỏ theo thời gian còn lại. `ServingCounter.prefab` và text số slot được đặt sẵn trong `GamePlay.unity`; các thành phần header nằm trong `UICGamePlay`/`UICFloating`.

Magnet chọn một phần còn thiếu trên icon order đã gộp: hút đủ nguyên liệu tới đĩa tròn của khách, ghép thành món rồi bay vào order. Món đang ghép/đang giao không bị nhân đôi. Thiếu nguyên liệu thì không thay đổi board hoặc trừ booster.

Menu **SushiKhai → Prefabs → Author Plated Dish Delivery** tạo lại bố cục mặc định và gán reference. Đây là công cụ Editor có chủ đích; khi chơi chỉ instantiate prefab, bind dữ liệu và chạy animation. Các khoảng thời gian nguyên liệu bay/ghép, dừng ngắn trên đĩa phục vụ và bay lên order nằm trong `ConveyorManager`.

Kiểm tra Play Mode và chạy 20 màn thắng trước lần đổi luồng ghép món; xem [kết quả lần chạy trước](../../../Documentation/PlatedServingRegressionResults.txt). Luồng mới đổi sang cho nguyên liệu bay lần lượt lên đĩa tròn của khách rồi món hoàn chỉnh bay thẳng tới order; regression đã cập nhật nhưng cần chạy lại bằng Unity Editor để xác nhận hình ảnh/thời điểm trên thiết bị.

Scene có hai ghế cân hai bên và hàng chờ phía sau. Khách chờ thu nhỏ bằng `CustomerManager.waitingScale = 0.6`; khi vào ghế sẽ phóng về kích thước chuẩn, hiện order và bắt đầu timer nếu là khách vội. Các icon món xếp tối đa hai cột trong bảng cạnh avatar. Khách chờ không hiện bảng order hoặc vị trí đặt đĩa trên bàn.

`ConveyorManager` tự tính điểm giao trên đường băng gần **ghế tương ứng**, thông qua `CustomerManager.GetSeat`; không cần gán một danh sách Delivery Points riêng. Điều chỉnh marker ghế trong scene để đổi bố cục rồi nạp lại level. Món thành phẩm bay theo vòng cung đến bàn rồi bảng order; đích được đọc lại mỗi bước tween nên vẫn theo đúng vị trí khi khách dịch chuyển. Luồng order nguyên liệu cũ được giữ cho dữ liệu legacy, còn bộ campaign dùng món thành phẩm.

`Dia.prefab` đã được gán lại sprite đĩa còn thiếu bằng `Art/ConveyorPlate.png`; đường băng được vẽ theo spline dưới các đĩa. Trong scene mẫu, tâm băng chuyền ở y = 0.05 và BoardManager ở y = -3.12 để hạ đồng thời hai khu vực gameplay; dòng gợi ý nằm dưới nút booster để không che hàng cuối.

## Chuyển động nguyên liệu và khay

Các khay được xếp gần nhau theo chiều ngang với khe nhỏ để không chồng mép; chỉ mép ngoài cùng của toàn board có hai thanh viền dọc (Left rail ở cột đầu, Right rail ở cột cuối). Hai thanh viền đặt tại x = -0.83 và x = 0.83, có scale x = 0.8 để cách khay. `FoodQueue.prefab` có child `Lower rows shade` với `SpriteRenderer` author sẵn; màu và alpha chỉnh trực tiếp trên prefab. Runtime chỉ đổi vị trí/kích thước để phủ các hàng 2–4, hàng đầu không bị phủ. Mỗi cột có bốn hàng đế, kể cả hàng rỗng. Khi pick hết ô thường, ô ×n, cặp nối hoặc khay, nguyên liệu nhảy tới đĩa đã giữ chỗ và để lại một view khay rỗng. Sau khi nguyên liệu cuối cất cánh và chờ thêm `BoardShishiManager.trayHoldDuration` (mặc định 0,04 giây), khay rỗng **trượt nhẹ lên đồng thời giảm alpha về 0, rồi ẩn**. Khay giữ nguyên scale, dùng ease InOutSine cùng nhịp với các hàng còn lại dồn lên trong `FoodQueue.moveDuration` (0,28 giây). Khung và các ngăn của khay nhóm cùng di chuyển và mờ dần. Trong Inspector của `FoodQueue`, `emptyTraySlideDistance` (0,24) điều chỉnh quãng trượt; `emptyTraySlideDuration` (0,28 giây) điều chỉnh thời gian trượt và fade. Chỉ tạo hiệu ứng khi lấy hết ô; lấy một phần số lượng vẫn giữ đế tại chỗ theo luật booster.

Trong lúc băng cột đang dồn, `FoodQueue.IsPickupPending` khóa tap/Cây gắp/Magnet vào cột đó; các cột khác vẫn lấy bình thường. Kiểm tra kẹt băng/thắng đợi chuyển động xong; Swap Row kết thúc nhịp dồn hiện tại rồi đổi hàng ngay. Băng cột kề nhận sát thương theo vị trí hàng thực tế đang hiển thị. Ô ẩn bắt đầu lật khi đến đầu cột. Cây gắp chỉ dồn những hàng phía sau ô bị lấy; Magnet lấy một phần số lượng giữ nguyên đế, và vẫn giữ ngăn khay rỗng theo luật khay. Chơi lại/dọn cột hủy tween dồn hàng; tắt cột giữa animation đưa các hàng về đúng vị trí.

`ConveyorManager` có `pickupStagger` (0,055 giây), `trayPickupStagger` (0,085 giây), `takeoffDuration` (0,065 giây), `landingDuration` (0,1 giây), `beltFoodScale` (0,78). Mỗi nguyên liệu chờ tại vị trí world của board, lấy đà nhẹ, bay theo vòng cung với ease InOutSine, nghiêng/giãn nhẹ rồi nảy mềm khi đáp xuống. Đích đĩa/order được đọc lại mỗi frame; chỉ gắn vào đích và xác nhận tới nơi sau khi đáp xong. Cả nhóm vẫn giữ đủ slot ngay khi pick, kể cả nguyên liệu chưa cất cánh. Đồ ăn đang bay có sorting order 30 để không bị nền ô che.

[Xem animation từ scene URP thực](../../../Documentation/SushiMotionPreview.gif). Đã quay lấy khay, dồn cột và giao đủ món cho khách vội 18 giây, sau đó chạy lại 27 nhóm gameplay và 5 màn mẫu.

`SushiRecipesDemo.asset` minh họa đủ 5 nhóm món và một khách vội; `SushiElementsDemo.asset` minh họa 5 element bằng các món Nigiri. Level mặc định đã chuyển sang order Nigiri tôm/trứng thay cho order nguyên liệu.

## Bộ 20 màn chơi chính

`Assets/_game/LevelData/_AllLevelData.asset` dùng 20 asset `Campaign/Level01.asset` đến `Level20.asset`, theo đúng thứ tự chơi. Board được lưu sẵn nên chơi lại giữ nguyên bố cục. Độ khó tăng từ 2 lên 12 khách, 3–5 cột, thêm món ba nguyên liệu và phối hợp element; các màn cuối có 8 slot. Ô ẩn xuất hiện từ màn 5, băng từ 7, cặp nối từ 9, khay nhóm từ 11. Bộ này chưa thêm khách có timer.

Xem [thiết kế và lời giải 20 màn](../../../Documentation/Campaign20.md). Menu **SushiKhai → Levels → Build 20 Level Campaign** tạo lại bộ mặc định; **Sushi → Validate 20 Level Campaign** kiểm tra dữ liệu và lời giải. `SushiCampaignRegression.RunBatch` còn chơi từng màn bằng thao tác lấy thường qua board, băng chuyền và khách thật, không dùng booster.

Nút **Chơi lại** trong Setting gọi `GameManager.ReplayCurrentLevel`: mở `UICTransis`, đợi che kín và chạy thanh tiến trình rồi mới tải lại đúng màn đang chơi. Khi hiện màn xong mới mở HUD và nhận input; bấm lặp trong lúc chuyển màn không tạo thêm lần tải. Tiến trình, xu và booster được giữ nguyên.

Đã kiểm tra ngày 26/10/2026 với Unity 6000.3.15f1 trong project riêng: **20/20 màn thắng bằng tap thường, không booster/revive**; Settings chạy transition trước reload, dọn nguyên liệu đang bay, chặn bấm lặp và giữ đúng màn đang chơi ngay cả khi tiến trình lưu trỏ sang màn khác. Xem [kết quả kiểm thử](../../../Documentation/CampaignRegressionResults.txt). Lượt chạy có lỗi indexing `UnityEditor.Search` của Editor; bộ kiểm tra loại riêng lỗi này và vẫn kiểm tra lỗi gameplay.

## Bộ 5 màn chơi thử riêng

5 asset trong `Assets/_game/LevelData/Samples` được giữ để thử riêng các mốc level 10/20/30/40/46, không nằm trong danh sách chơi chính. Bộ này có đủ 5 element, 17 loại nguyên liệu, khách vội chờ đến lượt, khách vội đồng thời và thời gian ghi đè 18/60/75/90 giây. Xem [README bộ mẫu](../LevelData/Samples/README.md) để biết từng trường hợp và cách đổi thời gian.

`LevelManager.showLevelSelector` bật nút Màn trước/Chơi lại/Màn sau; scene mẫu đã bật. Có thể tắt khi dùng cho tiến trình chơi chính thức. Asset level cũ vẫn giữ nguyên trong thư mục cha.

## Cấu hình ô ×n

- `FoodData.amount`: 1–3 nguyên liệu cùng loại trong một ô. Khi tap, toàn bộ số lượng chiếm từng slot riêng, gồm các slot đang giữ cho nguyên liệu đang bay. Thiếu slot thì giữ nguyên board.
- `SushiLevelData.maxIngredientsPerTile`: sinh ô ×n từ tổng orders; giá trị 1 giữ cách chơi cũ. Level mẫu đã đặt 2 để kiểm tra ×2.
- `boardColumns`: cấu hình board trực tiếp; phần tử `foods[0]` là hàng 1. Không shuffle board nhập trực tiếp. Để trống để sinh từ orders. Tổng theo từng loại, tính cả amount, phải khớp orders.
- `modifiers`: cấu hình bổ sung cho ô trong board sinh tự động hoặc board nhập trực tiếp; column/row bắt đầu từ 0. Một vị trí chỉ có một modifier.
- Vùng hiển thị và vùng băng chịu sát thương chỉ có 4 hàng đầu. Collider tự cập nhật theo số hàng hiện tại.

## Element

| ID tài liệu | Dữ liệu |
| --- | --- |
| hidden_tile | `hidden = true` hoặc `foodState = HoiCham` (giá trị 1 cũ). Lật 0.25 giây khi lên đầu cột. |
| rush_customer | `CustomerOrderData.rushCustomer = true`; `rushSeconds = 0` dùng 20 + 5 × tổng order, chạy khi đã ngồi. |
| frozen_tile | `iceLayers = 1..5` hoặc `foodState = Ice` kèm số lớp. Tap cột kề làm các hàng tương ứng di chuyển mới trừ lớp. |
| linked_tile | Hai ô ở cột kề cùng `linkId > 0`. `Lock`/`lockId` cũ được đọc như linked tile. |
| rigid_tray | 2–3 ô liên tiếp trong cùng cột có cùng `trayId > 0`, tổng không quá 6 nguyên liệu. |

Ô ẩn có thể dùng cùng iceLayers từ level 40. Ô nối/khay không kết hợp element khác. Validator kiểm tra mốc mở, số lớp băng, cân bằng nguyên liệu và đường lên đầu cột của các cặp nối. Giá trị 5 lớp dành cho level 39 và 49, tối đa hai ô.

`SushiElementsDemo.asset` chứa đủ 5 element với ba loại nguyên liệu, sáu cột và bốn khách. Gán asset này vào danh sách `LevelManager.levels` khi cần thử riêng.

## Input, view và booster

`FoodInputController` xử lý một click/touch duy nhất và bỏ qua click trên UI. Tap thường lấy hàng 1. Cây gắp vào chế độ chọn một ô trong 4 hàng đang thấy để đưa toàn bộ số lượng của ô lên vòng xoay; không lấy ô băng, ô đang chuyển động/lật hoặc ô ngoài vùng thấy. Ô nối được tháo liên kết ở cả hai đầu; lấy từ khay để lại ngăn rỗng. Thiếu slot thì giữ nguyên board và tiếp tục chọn. Nút Hủy thoát chế độ chọn mà không đổi dữ liệu.

Magnet vào chế độ **chọn một món trong order của khách đang ngồi**; thẻ món được tô sáng và board không nhận lựa chọn Magnet. Booster lấy đủ nguyên liệu cho một phần món đã chọn, ưu tiên vòng xoay (kể cả nguyên liệu đang bay tới đĩa khách), rồi board theo ưu tiên thường/ẩn → nối/khay → băng. Nguyên liệu ghép trên đĩa tròn của khách, không chiếm thêm slot vòng xoay, rồi món bay lên order mới hoàn thành. Phần đã nhận/đang được giao không bị lấy lại. Thiếu nguyên liệu thì thao tác bị từ chối, giữ nguyên board/vòng xoay/order và không trừ lượt booster. Tải lại màn hủy cả các chuyến bay Magnet và lựa chọn booster.

Swap Row thực hiện ngay khi bấm, không có bước chọn ô: hàng 1 xuống hàng 4, hàng 2–4 đẩy lên một bậc; cột ngắn xoay trong số hàng hiện có. Khay nhóm vẫn đi theo khối, mở rộng cửa sổ khi khay vắt hàng 4–5. Giữ nguyên đối tượng nên link và lớp băng đi theo ô. Không booster nào gây sát thương băng cột kề.

Prefab chung có nhãn ×n và dùng ảnh minh họa cho nắp ?, lớp băng, dây nối và khay. Gameplay dùng `UIManager` cùng các prefab `UICGamePlay`, `UICFloating`, Win/Lose và Settings của dự án. Không bật HUD dựng bằng code `SushiGameplayHud`.

## Timer, popup và revive

Timer dùng thời gian unscaled; vẫn chạy khi chọn booster, chỉ dừng khi popup mở. `UICanvas.Open`/`OnDisable` tự đăng ký popup với LevelManager; UI riêng gọi `SetPopupOpen(true/false)` cân bằng. Khách thường và khách vội nhận theo thứ tự khách đến.

Hết giờ: popup +30 giây cho đúng khách đó. Kẹt vì mọi đầu cột bị băng: popup phá băng ở hàng 1. Revive lần đầu 300 xu, sau đó 600. `SushiReviveProvider` là điểm tích hợp ví xu/quảng cáo: implement `TrySpendCoins` và `ShowRewardedAd`, rồi gán vào LevelManager. Chỉ nhận revive khi thanh toán/reward thành công; khi chưa gán provider, các nút thanh toán không hoạt động. Nút từ chối chuyển sang thua/chơi lại.

## Kiểm tra

`Tests/Editor/SushiGameplayRegression.cs` có 32 nhóm kiểm tra trên Unity Play Mode, dùng pool và component thật. Bao gồm toàn bộ 21 sprite với prefab chung, 48 ảnh món hiện trước khi hoàn thành, bốn ảnh element, phục hồi kích thước icon khi gán lại ảnh, ×n/thiếu slot, click liên tiếp, ô nối, băng, đế băng chuyền/khay rỗng, Cây gắp chọn ô, Magnet chọn đúng món từ cả board/vòng xoay, nguyên liệu đang giao/thiếu nguyên liệu, Swap Row tức thì, chọn/hủy booster, timer/popup, giá revive, thứ tự khách, reload khi đang bay, khách chờ và giao đến đúng ghế/ô ngay cả khi đích di chuyển.

Dùng project test riêng để không ảnh hưởng scene đang mở. Với source và dependencies của game trong project đó, chạy Unity `-batchmode -nographics -projectPath <project-test> -executeMethod SushiGameplayRegression.RunBatch -logFile <log>`; không thêm `-quit` vì test chuyển sang Play Mode. Kết quả ghi `SushiRegressionResults.txt`, exit 0 khi pass, 1 khi fail.

Lượt cập nhật băng chuyền cột và booster ngày 22/10/2026 đã qua **32 nhóm gameplay** cùng **441 assertions cho 48 công thức** trong Unity 6000.3.15f1. Có kiểm tra Magnet khi vòng xoay đầy, chuyển hướng nguyên liệu đang bay, phần món dùng chung nguyên liệu, thiếu nguyên liệu giữ nguyên dữ liệu, chọn/hủy booster, Swap Row trong lúc cột đang dồn và reload khi Magnet đang giao.

Bộ kiểm tra thuần dữ liệu `SushiRecipeChecks.cs` đối chiếu đủ 48 công thức, mở rộng đơn nhiều phần, giữ tiến độ món độc lập, kiểm tra ID cũ và dữ liệu sai. Có thể chạy trực tiếp với assembly game đã biên dịch, không cần Unity Play Mode, hoặc chọn menu **SushiKhai → Tests → Check 48 Recipes** trong Unity.

Đã kiểm tra biên dịch runtime/editor và **441 assertions cho 48 công thức**. Ngày 22/10/2026, Unity 6000.3.15f1 chạy batch Play Mode trong project kiểm tra riêng đã qua **27 nhóm kiểm tra gameplay**, bao gồm hai danh sách art, ảnh element, đổi art khi tái sử dụng object, ẩn order ở hàng chờ, đúng điểm giao của ghế thứ hai, đúng icon nguyên liệu và đích dịch chuyển trong lúc bay. Cả **5 level mẫu** đã hoàn thành với board/băng trống và mọi khách hoàn tất đơn; khách vội đều kịp giờ theo đường lấy đã kiểm tra. Đã thử riêng chọn booster vẫn chạy timer, popup dừng timer, hết giờ mở đúng popup revive, từ chối thì thua, chơi lại reset 18 giây. Xem [kết quả từng mẫu](../LevelData/Samples/README.md).

Các kiểm tra batch dùng component/pool, sprite và dữ liệu level thật, mô phỏng thời gian và tween. Đã render và xem SampleScene ở 720 × 1280 trong bản sao dùng **đúng QualitySettings, URP asset và renderer 2D của project, không ghi đè material trong công cụ chụp**: màn 10 hiện nguyên liệu/ảnh món; màn 46 hiện cả hỏi chấm, băng, dây nối và khay. Đã thử xóa reference nguyên liệu, món có sprite con Multiple và băng: công cụ tự gán lại đúng, giữ ảnh tùy chọn của người dùng, báo 0 hình thiếu. Chưa kiểm tra thao tác trên thiết bị thật. Art phác họa có ảnh tổng hợp để xem trước; luồng ví xu/quảng cáo cần implementation của dự án.
