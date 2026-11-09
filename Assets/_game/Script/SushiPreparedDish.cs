using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

// One authored dish assembled on a customer serving plate, then flown to its order.
// Nguyên liệu lên đĩa giữ nguyên ảnh gốc (bát cơm, miếng cá...). Mỗi lần ghép, các nguyên liệu
// nảy lên một chút, đổi hình ở đỉnh rồi rơi xuống:
//  - Đủ 2 nguyên liệu nhưng món chưa xong (maki, gunkan, sashimi 3...) => món ghép dở bằng các lớp ảnh trên đĩa.
//  - Đủ công thức => ảnh món hoàn chỉnh.
public sealed class SushiPreparedDish : MonoBehaviour
{
    [SerializeField] private SpriteRenderer plate;
    [SerializeField] private SpriteRenderer dishImage;
    [SerializeField] private Vector2 imageSize = new Vector2(0.86f, 0.7f);
    [SerializeField] private SushiPlatedIngredientArtLibrary ingredientArt;
    [SerializeField] private Transform ingredientRoot;

    [Header("Nguyên liệu gốc trước khi ghép")]
    [Tooltip("Cạnh lớn nhất của phần nền (bát cơm, cốc) khi nằm trên đĩa.")]
    [SerializeField, Min(0.05f)] private float rawFoundationSize = 0.8f;
    [Tooltip("Cạnh lớn nhất của nguyên liệu phủ lên nền.")]
    [SerializeField, Min(0.05f)] private float rawToppingSize = 0.66f;
    [Tooltip("Vị trí phần nền trên đĩa (0,0 = chính giữa đĩa).")]
    [SerializeField] private Vector2 rawFoundationPosition = Vector2.zero;
    [Tooltip("X: khoảng cách dàn ngang khi món không có cơm (trên cơm thì luôn nằm giữa); Y: độ nâng so với nền.")]
    [SerializeField] private Vector2 rawToppingSpacing = new Vector2(0.2f, 0.04f);

    [Serializable]
    public sealed class RawIngredientOverride
    {
        public PoolType type = PoolType.SushiRice;
        [Tooltip("Cạnh lớn nhất của ảnh nguyên liệu trên đĩa (giữ tỉ lệ). 0 = dùng size chung.")]
        public float size;
        [Tooltip("Cộng thêm vào vị trí mặc định trên đĩa.")]
        public Vector2 offset;
    }
    [Tooltip("Chỉnh riêng size/vị trí cho từng nguyên liệu khi nằm trên đĩa (vd. cơm, rong biển).")]
    [SerializeField] private List<RawIngredientOverride> rawIngredientOverrides = new List<RawIngredientOverride>();

    [Header("Đĩa xuất hiện (mờ dần hiện ra khi nguyên liệu đầu tiên - bát cơm - bay tới)")]
    [Tooltip("Thời gian đĩa mờ dần hiện ra dưới nguyên liệu.")]
    [SerializeField, Min(0.02f)] private float plateFadeDuration = 0.1f;
    [Tooltip("Tỉ lệ đĩa lúc bắt đầu hiện, phóng lên 1 khi hiện xong.")]
    [SerializeField, Range(0.5f, 1f)] private float plateStartScale = 0.92f;

    [Header("Hiệu ứng ghép món (nảy lên - đổi hình - rơi xuống)")]
    [Tooltip("Độ cao nảy lên khi các nguyên liệu ghép lại.")]
    [SerializeField, Min(0f)] private float hopHeight = 0.38f;
    [Tooltip("Thời gian nảy lên tới đỉnh.")]
    [SerializeField, Min(0.02f)] private float hopUpDuration = 0.17f;
    [Tooltip("Thời gian rơi xuống đĩa.")]
    [SerializeField, Min(0.02f)] private float hopDownDuration = 0.19f;
    [Tooltip("Thời gian nhún khi rơi chạm đĩa.")]
    [SerializeField, Min(0.02f)] private float landSquashDuration = 0.14f;
    [SerializeField] private Vector2 finishedDishOffset = new Vector2(0f, 0.06f);
    [SerializeField] private Color sparkleColor = new Color(1f, 0.93f, 0.55f, 1f);

    private const int RawLayerOffset = 10;
    private readonly List<FoodShushi> assemblyIngredients = new List<FoodShushi>();
    private readonly HashSet<FoodShushi> combinedIngredients = new HashSet<FoodShushi>();
    private readonly Dictionary<PoolType, Transform> ingredientTargets = new Dictionary<PoolType, Transform>();
    private bool isFlying;
    private bool combinedLayout;
    private bool transformed;
    private bool hasCachedPlate;
    private Sprite defaultPlateSprite;
    private Vector3 defaultPlatePosition;
    private Vector3 defaultPlateScale;
    private Vector3 baseScale = Vector3.one;
    private Vector3 ingredientRootPosition;
    private float contentHop;
    private bool plateShown;
    private Sequence mergeSequence;
    private Tween plateTween;
    private static Sprite glowSprite;

    public IReadOnlyList<FoodShushi> AssemblyIngredients => assemblyIngredients;
    public Transform IngredientRoot => ingredientRoot;
    public Customer Owner { get; private set; }
    public SushiDishOrder Order { get; private set; }
    public bool IsReady { get; private set; }
    // Món đã biến thành ảnh món hoàn chỉnh (hoặc lớp ghép đầy đủ khi không có ảnh món).
    public bool IsTransformed => transformed;
    // Các nguyên liệu đã ghép thành món dở (ví dụ cơm + rong biển của maki).
    public bool IsPartiallyCombined => combinedLayout && !transformed;
    public float TransformDuration => hopUpDuration + hopDownDuration + landSquashDuration;
    // Nguyên liệu đã chuyển sang lớp ảnh ghép trên đĩa.
    public bool IsCombined(FoodShushi food) => combinedIngredients.Contains(food);

    // Gắn khách và đơn món cho đĩa, tạo các điểm đặt nguyên liệu theo công thức.
    public void Bind(Customer customer, SushiDishOrder order)
    {
        Owner = customer; Order = order; IsReady = false;
        isFlying = false;
        combinedLayout = false;
        transformed = false;
        baseScale = transform.localScale;
        if (ingredientArt == null && SushiArtCatalog.Default != null)
            ingredientArt = SushiArtCatalog.Default.platedIngredientImages;
        RefreshPlateArtwork();
        if (ingredientRoot == null)
        {
            ingredientRoot = new GameObject("Ingredients").transform;
            ingredientRoot.SetParent(transform, false);
        }
        ingredientRootPosition = ingredientRoot.localPosition;
        ingredientRoot.gameObject.SetActive(true);
        for (int i = 0; i < order.Ingredients.Count; i++)
        {
            PoolType type = order.Ingredients[i].typeFood;
            var target = new GameObject(type.ToString()).transform;
            target.SetParent(ingredientRoot, false);
            ingredientTargets.Add(type, target);
        }
        foreach (var target in ingredientTargets) UpdateIngredientTarget(target.Key, target.Value);
        dishImage.enabled = false;
        HidePlate();
    }

    // Bắt đầu ghép: đĩa chờ nguyên liệu đầu tiên cất cánh mới hiện ra.
    public void BeginAssembly()
    {
        dishImage.enabled = false;
        if (!plateShown) HidePlate();
    }

    public bool IsPlateShown => plateShown;

    // Ẩn đĩa, chờ nguyên liệu đầu tiên bay tới mới hiện.
    private void HidePlate()
    {
        plateTween?.Kill();
        plateShown = false;
        plate.enabled = false;
        SetPlateAlpha(1f);
    }

    // Đĩa mờ dần hiện ra dưới nguyên liệu và phóng nhẹ lên kích thước thật (giống video).
    public void RevealPlate(float delay = 0f)
    {
        if (plateShown) return;
        plateShown = true;
        plateTween?.Kill();
        Transform plateTransform = plate.transform;
        Vector3 fullScale = plateTransform.localScale;
        plate.enabled = true;
        SetPlateAlpha(0f);
        plateTransform.localScale = fullScale * plateStartScale;
        plateTween = DOTween.Sequence().SetLink(gameObject)
            .AppendInterval(Mathf.Max(0f, delay))
            .Append(DOVirtual.Float(0f, 1f, plateFadeDuration, t =>
            {
                if (plate == null) return;
                SetPlateAlpha(t);
                plateTransform.localScale = fullScale * Mathf.Lerp(plateStartScale, 1f, t);
            }).SetEase(Ease.OutQuad))
            .OnKill(() =>
            {
                if (plate == null) return;
                SetPlateAlpha(1f);
                plateTransform.localScale = fullScale;
            });
    }

    // Đặt độ trong suốt của đĩa.
    private void SetPlateAlpha(float alpha)
    {
        Color color = plate.color;
        color.a = alpha;
        plate.color = color;
    }

    // Đổi đơn món của đĩa, bổ sung hoặc bỏ điểm nguyên liệu và cập nhật hình ảnh.
    internal void ReassignOrder(SushiDishOrder order)
    {
        Order = order;
        var obsolete = new List<PoolType>(ingredientTargets.Keys);
        foreach (OrderItem ingredient in order.Ingredients)
        {
            PoolType type = ingredient.typeFood;
            obsolete.Remove(type);
            if (!ingredientTargets.ContainsKey(type))
            {
                var target = new GameObject(type.ToString()).transform;
                target.SetParent(ingredientRoot, false);
                ingredientTargets.Add(type, target);
            }
        }
        foreach (PoolType type in obsolete)
        {
            Destroy(ingredientTargets[type].gameObject);
            ingredientTargets.Remove(type);
        }
        RefreshArtwork(ingredientArt);
    }

    // Lấy điểm đặt dành cho loại nguyên liệu trên đĩa.
    public Transform IngredientTarget(PoolType type) => ingredientTargets[type];

    // Đặt vị trí điểm nguyên liệu: bố cục ảnh gốc trước khi ghép, bố cục lớp ghép từ lần ghép đầu tiên.
    private void UpdateIngredientTarget(PoolType type, Transform target)
    {
        if (!combinedLayout)
        {
            Vector2 raw = RawPosition(type, out _) + (FindRawOverride(type)?.offset ?? Vector2.zero);
            target.localPosition = new Vector3(raw.x, raw.y, -0.1f);
            target.localRotation = Quaternion.identity;
            return;
        }
        var artwork = ingredientArt != null ? ingredientArt.Find(type, Order.Recipe) : null;
        Vector2 position = ingredientArt != null ? ingredientArt.GetPosition(type, Order.Recipe) : Vector2.zero;
        target.localPosition = new Vector3(position.x, position.y, -0.1f);
        target.localRotation = Quaternion.Euler(0, 0, artwork != null ? artwork.rotation : 0);
    }

    // Bố cục nguyên liệu gốc: nền ở giữa đĩa, các nguyên liệu khác nằm trên nền và dàn ngang.
    private Vector2 RawPosition(PoolType type, out int layer)
    {
        PoolType foundation = Order.FoundationType;
        if (foundation != PoolType.None && type == foundation)
        {
            layer = 0;
            return rawFoundationPosition;
        }
        int count = 0, index = 0;
        foreach (OrderItem item in Order.Ingredients)
        {
            if (foundation != PoolType.None && item.typeFood == foundation) continue;
            if (item.typeFood == type) index = count;
            count++;
        }
        layer = 1 + index;
        // Trên bát cơm: nguyên liệu phủ nằm chính giữa như topping; món không cơm (sashimi, trà) thì dàn ngang.
        float x = foundation == PoolType.SushiRice ? 0f : (index - (count - 1) * 0.5f) * rawToppingSpacing.x;
        float y = foundation != PoolType.None ? rawFoundationPosition.y + rawToppingSpacing.y : 0f;
        return new Vector2(x, y);
    }

    // Cập nhật đĩa, điểm đặt và hình nguyên liệu khi thư viện đang dùng thay đổi.
    public void RefreshArtwork(SushiPlatedIngredientArtLibrary changedArt)
    {
        if (Order == null || ingredientArt != changedArt) return;
        RefreshPlateArtwork();
        foreach (var target in ingredientTargets) UpdateIngredientTarget(target.Key, target.Value);
        foreach (FoodShushi food in assemblyIngredients)
            if (food != null) ApplyIngredientArtwork(food, isFlying);
    }

    // Chọn hình đĩa phù hợp công thức hoặc phục hồi đĩa mặc định.
    private void RefreshPlateArtwork()
    {
        if (!hasCachedPlate)
        {
            defaultPlateSprite = plate.sprite;
            defaultPlatePosition = plate.transform.localPosition;
            defaultPlateScale = plate.transform.localScale;
            hasCachedPlate = true;
        }
        var artwork = ingredientArt != null ? ingredientArt.GetPlate(Order.Recipe) : null;
        if (artwork == null)
        {
            plate.sprite = defaultPlateSprite;
            plate.transform.localPosition = defaultPlatePosition;
            plate.transform.localScale = defaultPlateScale;
            return;
        }
        plate.sprite = artwork.sprite;
        plate.transform.localPosition = new Vector3(artwork.position.x, artwork.position.y, defaultPlatePosition.z);
        plate.transform.localScale = new Vector3(artwork.size.x / Mathf.Max(0.01f, artwork.sprite.bounds.size.x),
            artwork.size.y / Mathf.Max(0.01f, artwork.sprite.bounds.size.y), defaultPlateScale.z);
    }

    // Gán sprite, kích thước và lớp vẽ cho nguyên liệu tùy trạng thái nằm trên đĩa hay đang bay.
    // Nguyên liệu chưa ghép dùng ảnh gốc giữ đúng tỉ lệ; đã ghép thì dùng lớp ảnh theo công thức.
    public void ApplyIngredientArtwork(FoodShushi food, bool flying)
    {
        int baseOrder = flying ? 31 : 18;
        if (!combinedIngredients.Contains(food))
        {
            Sprite raw = food.IngredientSprite;
            RawPosition(food.Type, out int layer);
            bool isFoundation = Order.FoundationType != PoolType.None && food.Type == Order.FoundationType;
            RawIngredientOverride custom = FindRawOverride(food.Type);
            float maxSize = custom != null && custom.size > 0f ? custom.size : isFoundation ? rawFoundationSize : rawToppingSize;
            // Nguyên liệu gốc mới đáp luôn nằm trên các lớp đã ghép.
            int rawOrder = baseOrder + layer + (combinedLayout ? RawLayerOffset : 0);
            food.SetPlatedArtwork(raw, FitInside(raw, Vector2.one * maxSize), rawOrder);
            return;
        }
        var artwork = ingredientArt != null ? ingredientArt.Find(food.Type, Order.Recipe) : null;
        Sprite sprite = artwork != null ? artwork.sprite : food.IngredientSprite;
        Vector2 size = artwork != null ? artwork.size : imageSize;
        int order = baseOrder + (ingredientArt != null ? ingredientArt.GetSortingOrder(food.Type, Order.Recipe) : 0);
        food.SetPlatedArtwork(sprite, size, order);
    }

    // Tìm thông số chỉnh riêng của nguyên liệu khi nằm trên đĩa.
    private RawIngredientOverride FindRawOverride(PoolType type)
    {
        foreach (RawIngredientOverride entry in rawIngredientOverrides)
            if (entry != null && entry.type == type) return entry;
        return null;
    }

#if UNITY_EDITOR
    // Sửa thông số trong Inspector lúc Play thì đĩa đang có cập nhật ngay.
    private void OnValidate()
    {
        if (!Application.isPlaying || Order == null) return;
        foreach (var target in ingredientTargets) UpdateIngredientTarget(target.Key, target.Value);
        foreach (FoodShushi food in assemblyIngredients)
            if (food != null) ApplyIngredientArtwork(food, isFlying);
    }
#endif

    // Kích thước giữ tỉ lệ sprite và nằm gọn trong khung.
    private static Vector2 FitInside(Sprite sprite, Vector2 frame)
    {
        if (sprite == null) return frame;
        Vector2 bounds = sprite.bounds.size;
        float scale = Mathf.Min(frame.x / Mathf.Max(0.01f, bounds.x), frame.y / Mathf.Max(0.01f, bounds.y));
        return bounds * scale;
    }

    // Gắn nguyên liệu đã đáp xuống đúng điểm trên đĩa và thêm vào danh sách quản lý.
    public void AddAssemblyIngredient(FoodShushi food)
    {
        if (food == null) return;
        food.transform.SetParent(IngredientTarget(food.Type), false);
        food.transform.localPosition = Vector3.zero;
        food.transform.localRotation = Quaternion.identity;
        food.transform.localScale = Vector3.one;
        ApplyIngredientArtwork(food, false);
        assemblyIngredients.Add(food);
        // Dự phòng: nguyên liệu đã đáp mà đĩa chưa hiện.
        RevealPlate();
        if (!transformed) PlayLandingBounce(food.transform);
    }

    // Nhún nhẹ khi nguyên liệu đáp lên đĩa giống lúc đặt đồ ăn xuống.
    private void PlayLandingBounce(Transform food)
    {
        food.DOKill();
        food.localScale = new Vector3(1.12f, 0.88f, 1f);
        food.DOScale(Vector3.one, 0.18f).SetEase(Ease.OutBack).SetLink(food.gameObject);
    }

    // Tách và trả toàn bộ nguyên liệu đã ghép trên đĩa về pool.
    public void ClearAssemblyIngredients()
    {
        foreach (FoodShushi food in assemblyIngredients)
        {
            if (food == null) continue;
            food.transform.DOKill();
            food.transform.SetParent(null, true);
            SimplePool.Despawn(food);
        }
        assemblyIngredients.Clear();
        combinedIngredients.Clear();
    }

    // Món chưa đủ: khi đã có từ 2 nguyên liệu trên đĩa thì ghép chúng thành món dở.
    // Trả về thời lượng hiệu ứng, 0 nếu chưa cần ghép (vd. mới chỉ có bát cơm).
    public float CombineLandedIngredients()
    {
        if (transformed || assemblyIngredients.Count < 2) return 0f;
        bool hasRaw = false;
        foreach (FoodShushi food in assemblyIngredients)
            if (food != null && !combinedIngredients.Contains(food)) { hasRaw = true; break; }
        if (!hasRaw) return 0f;
        var landed = new List<FoodShushi>(assemblyIngredients);
        return PlayMerge(() => CombineIngredients(landed));
    }

    // Đánh dấu đĩa đã ghép xong, chạy hiệu ứng biến thành món và trả về thời lượng hiệu ứng.
    public float FinishAssembly()
    {
        IsReady = true;
        dishImage.enabled = false;
        RevealPlate();
        return PlayMerge(SwapToFinishedDish);
    }

    // Nảy nguyên liệu lên, đổi hình ở đỉnh (kèm lóe sáng), rơi xuống rồi nhún đĩa.
    private float PlayMerge(Action swap)
    {
        // Lần ghép trước chưa xong thì bỏ; lần ghép mới tự ghép lại toàn bộ nguyên liệu đang có.
        mergeSequence?.Kill();
        ResetMergePose();
        Vector3 squash = new Vector3(baseScale.x * 1.1f, baseScale.y * 0.9f, baseScale.z);
        mergeSequence = DOTween.Sequence().SetLink(gameObject)
            .Append(DOVirtual.Float(0f, hopHeight, hopUpDuration, SetContentHop).SetEase(Ease.OutQuad))
            .AppendCallback(() =>
            {
                swap();
                PlayFlash();
            })
            .Append(DOVirtual.Float(hopHeight, 0f, hopDownDuration, SetContentHop).SetEase(Ease.InQuad))
            .Append(transform.DOScale(squash, landSquashDuration * 0.4f).SetEase(Ease.OutQuad))
            .Append(transform.DOScale(baseScale, landSquashDuration * 0.6f).SetEase(Ease.OutBack))
            .OnKill(() => { if (this != null) ResetMergePose(); });
        return TransformDuration;
    }

    // Nâng nhóm nguyên liệu và ảnh món theo độ cao nảy, đĩa đứng yên.
    private void SetContentHop(float height)
    {
        contentHop = height;
        if (ingredientRoot != null) ingredientRoot.localPosition = ingredientRootPosition + Vector3.up * height;
        if (dishImage != null)
        {
            Vector3 position = dishImage.transform.localPosition;
            dishImage.transform.localPosition = new Vector3(finishedDishOffset.x, finishedDishOffset.y + height, position.z);
        }
    }

    // Đưa nguyên liệu, ảnh món và đĩa về tư thế nghỉ.
    private void ResetMergePose()
    {
        if (contentHop != 0f || (ingredientRoot != null && ingredientRoot.localPosition != ingredientRootPosition))
            SetContentHop(0f);
        transform.localScale = baseScale;
    }

    // Chuyển các nguyên liệu đã đáp sang lớp ảnh ghép theo công thức và đổi bố cục điểm đặt.
    private void CombineIngredients(IEnumerable<FoodShushi> foods)
    {
        combinedLayout = true;
        foreach (var target in ingredientTargets) UpdateIngredientTarget(target.Key, target.Value);
        foreach (FoodShushi food in foods)
        {
            if (food == null) continue;
            combinedIngredients.Add(food);
            food.transform.DOKill();
            food.transform.localScale = Vector3.one;
        }
        foreach (FoodShushi food in assemblyIngredients)
            if (food != null) ApplyIngredientArtwork(food, isFlying);
    }

    // Đổi nguyên liệu thành món: dùng ảnh món hoàn chỉnh nếu có, nếu không ghép đủ các lớp theo công thức.
    private void SwapToFinishedDish()
    {
        transformed = true;
        CombineIngredients(assemblyIngredients);
        Sprite finished = FinishedSprite();
        if (finished == null)
        {
            dishImage.enabled = false;
            ingredientRoot.gameObject.SetActive(true);
            return;
        }
        dishImage.sprite = finished;
        Vector2 size = FitInside(finished, imageSize);
        dishImage.transform.localPosition = new Vector3(finishedDishOffset.x, finishedDishOffset.y + contentHop,
            dishImage.transform.localPosition.z);
        dishImage.transform.localRotation = Quaternion.identity;
        dishImage.transform.localScale = new Vector3(size.x / Mathf.Max(0.01f, finished.bounds.size.x),
            size.y / Mathf.Max(0.01f, finished.bounds.size.y), 1f);
        dishImage.sortingOrder = isFlying ? 31 : 18;
        dishImage.enabled = true;
        ingredientRoot.gameObject.SetActive(false);
    }

    // Ảnh món hoàn chỉnh; món dùng đĩa trang trí riêng (sashimi) giữ bố cục lớp đã căn chỉnh.
    private Sprite FinishedSprite()
    {
        if (Order == null) return null;
        if (ingredientArt != null && ingredientArt.GetPlate(Order.Recipe) != null) return null;
        SushiDishArtLibrary dishes = SushiArtCatalog.Default != null ? SushiArtCatalog.Default.dishImages : null;
        return dishes != null ? dishes.GetSprite(Order.Recipe) : null;
    }

    // Lóe sáng trắng và các tia lấp lánh bung ra tại tâm món (đang ở đỉnh cú nảy).
    private void PlayFlash()
    {
        int order = (isFlying ? 31 : 18) + 20;
        Vector3 center = new Vector3(finishedDishOffset.x, finishedDishOffset.y + contentHop, -0.3f);

        var flash = NewEffect("Merge flash", GlowSprite, order, center);
        flash.color = new Color(1f, 1f, 1f, 0.95f);
        flash.transform.localScale = Vector3.one * 0.35f;
        DOTween.Sequence().SetLink(flash.gameObject)
            .Join(flash.transform.DOScale(1.25f, 0.32f).SetEase(Ease.OutQuad))
            .Join(flash.DOFade(0f, 0.32f).SetEase(Ease.InQuad))
            .OnComplete(() => Destroy(flash.gameObject));

        const int sparkles = 6;
        for (int i = 0; i < sparkles; i++)
        {
            float angle = (i + 0.5f) * Mathf.PI * 2f / sparkles + UnityEngine.Random.Range(-0.25f, 0.25f);
            Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle) * 0.75f, 0f);
            var star = NewEffect("Sparkle", SushiWorldVisual.WhiteSprite, order + 1, center);
            star.color = sparkleColor;
            star.transform.localRotation = Quaternion.Euler(0, 0, 45f);
            star.transform.localScale = Vector3.one * 0.02f;
            float distance = UnityEngine.Random.Range(0.42f, 0.58f);
            float size = UnityEngine.Random.Range(0.07f, 0.11f);
            DOTween.Sequence().SetLink(star.gameObject)
                .Join(star.transform.DOLocalMove(center + direction * distance, 0.4f).SetEase(Ease.OutCubic))
                .Join(star.transform.DOLocalRotate(new Vector3(0, 0, 225f), 0.4f, RotateMode.FastBeyond360))
                .Join(star.transform.DOScale(size, 0.14f).SetEase(Ease.OutBack))
                .Insert(0.18f, star.transform.DOScale(0f, 0.22f).SetEase(Ease.InQuad))
                .OnComplete(() => Destroy(star.gameObject));
        }
    }

    // Tạo một SpriteRenderer hiệu ứng con của đĩa.
    private SpriteRenderer NewEffect(string name, Sprite sprite, int order, Vector3 localPosition)
    {
        var effect = new GameObject(name).AddComponent<SpriteRenderer>();
        effect.transform.SetParent(transform, false);
        effect.transform.localPosition = localPosition;
        effect.sprite = sprite;
        effect.sharedMaterial = SushiWorldVisual.SpriteMaterial;
        effect.sortingOrder = order;
        return effect;
    }

    // Sprite tròn mờ dần từ tâm dùng cho vệt lóe sáng.
    private static Sprite GlowSprite
    {
        get
        {
            if (glowSprite != null) return glowSprite;
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Dish merge glow", wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), Vector2.one * (size / 2f)) / (size / 2f);
                    float a = Mathf.Clamp01(1f - d);
                    pixels[y * size + x] = new Color(1, 1, 1, a * a);
                }
            texture.SetPixels(pixels);
            texture.Apply();
            glowSprite = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
            return glowSprite;
        }
    }

    // Đưa đĩa cùng nguyên liệu lên lớp vẽ phía trước khi bay giao món.
    public void SetFlying()
    {
        isFlying = true;
        plate.sortingOrder = 30;
        if (dishImage.enabled) dishImage.sortingOrder = 31;
        foreach (FoodShushi food in assemblyIngredients) ApplyIngredientArtwork(food, true);
    }

    // Giữ hình đĩa khi công thức có đĩa riêng, nếu không thì ẩn đĩa lúc rời bàn.
    public void LeaveTable()
    {
        // Món rời bàn trước khi hiệu ứng xong (vd. nam châm) vẫn phải hiện ảnh món hoàn chỉnh.
        if (mergeSequence != null && mergeSequence.IsActive()) mergeSequence.Complete(true);
        if (IsReady && !transformed) SwapToFinishedDish();
        ResetMergePose();
        if (plateTween != null && plateTween.IsActive()) plateTween.Complete(true);
        // The decorated sashimi plate is part of the dish delivered to its order.
        plate.enabled = ingredientArt != null && ingredientArt.GetPlate(Order.Recipe) != null;
    }

    // Thu hồi nguyên liệu còn gắn trên đĩa khi đối tượng món bị hủy.
    private void OnDestroy()
    {
        mergeSequence?.Kill();
        plateTween?.Kill();
        ClearAssemblyIngredients();
    }
}
