using UnityEngine;

// Bàn phục vụ chung: số ô cố định (mặc định 4) dùng chung cho mọi khách đang ngồi.
// Món của khách nào cũng lấy ô trống bất kỳ; hết ô thì món sau phải đợi.
// Ô trống hiện vòng nét đứt; khi đĩa món đã hiện trên ô thì vòng nét đứt tắt.
public sealed class SushiServingCounter : MonoBehaviour
{
    public static SushiServingCounter Ins { get; private set; }

    [Tooltip("Các vị trí đặt đĩa trên bàn, theo thứ tự trái sang phải.")]
    [SerializeField] private Transform[] slots = new Transform[0];
    [Tooltip("Vòng nét đứt của từng ô, cùng thứ tự với slots.")]
    [SerializeField] private SpriteRenderer[] emptyArts = new SpriteRenderer[0];

    private Customer[] owners = new Customer[0];

    public int Count => slots != null ? slots.Length : 0;
    public Transform Slot(int index) => index >= 0 && index < Count ? slots[index] : null;

    private void Awake()
    {
        Ins = this;
        EnsureOwners();
    }

    private void OnDestroy()
    {
        if (Ins == this) Ins = null;
    }

    private void EnsureOwners()
    {
        if (owners.Length != Count) owners = new Customer[Count];
    }

    // Ô trống đầu tiên (trái sang phải) thoả bộ lọc, -1 nếu không có.
    public int FindFree(System.Predicate<int> allowed = null)
    {
        EnsureOwners();
        for (int i = 0; i < Count; i++)
            if (slots[i] != null && owners[i] == null && (allowed == null || allowed(i))) return i;
        return -1;
    }

    public void Claim(int index, Customer customer)
    {
        EnsureOwners();
        if (index >= 0 && index < owners.Length) owners[index] = customer;
    }

    public void Release(int index, Customer customer)
    {
        EnsureOwners();
        if (index >= 0 && index < owners.Length && owners[index] == customer) owners[index] = null;
    }

    // Trả mọi ô của một khách (khách rời đi / dựng lại khách).
    public void ReleaseAll(Customer customer)
    {
        EnsureOwners();
        for (int i = 0; i < owners.Length; i++)
            if (owners[i] == customer) owners[i] = null;
    }

    public void ClearAll()
    {
        EnsureOwners();
        for (int i = 0; i < owners.Length; i++) owners[i] = null;
    }

    private void LateUpdate()
    {
        for (int i = 0; i < Count && i < emptyArts.Length; i++)
        {
            if (emptyArts[i] == null || slots[i] == null) continue;
            var dish = slots[i].GetComponentInChildren<SushiPreparedDish>();
            emptyArts[i].enabled = dish == null || !dish.IsPlateShown;
        }
    }
}
