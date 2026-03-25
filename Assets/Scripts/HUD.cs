using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class HUD : MonoBehaviour
{
    private static readonly byte[] HotbarBlockTypes =
    {
        Chunk.Air,
        Chunk.Stone,
        Chunk.Regolith,
        Chunk.Ice,
        Chunk.Metal
    };

    private static Sprite whiteSprite;
    private static Font builtInFont;

    [SerializeField] private Inventory inventory;
    [SerializeField] private RectTransform canvasRoot;

    private RectTransform[] slotRects;
    private Image[] slotBorders;
    private Image[] slotBackgrounds;
    private Text[] slotCountTexts;

    public void SetInventory(Inventory targetInventory)
    {
        inventory = targetInventory;
    }

    private void Start()
    {
        EnsureUiBuilt();
        TryFindInventory();
        RefreshHotbar();
    }

    private void Update()
    {
        EnsureUiBuilt();

        if (inventory == null)
        {
            TryFindInventory();
        }

        RefreshHotbar();
    }

    private void EnsureUiBuilt()
    {
        if (slotRects != null &&
            slotBorders != null &&
            slotBackgrounds != null &&
            slotCountTexts != null &&
            slotRects.Length == HotbarBlockTypes.Length &&
            slotBorders.Length == HotbarBlockTypes.Length &&
            slotBackgrounds.Length == HotbarBlockTypes.Length &&
            slotCountTexts.Length == HotbarBlockTypes.Length &&
            slotRects[0] != null &&
            slotBorders[0] != null &&
            slotBackgrounds[0] != null &&
            slotCountTexts[0] != null)
        {
            return;
        }

        ClearExistingUi();
        CreateCanvas();
    }

    private void CreateCanvas()
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        canvasRoot = canvasObject.GetComponent<RectTransform>();

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        CreateCrosshair(canvasObject.transform);
        CreateHotbar(canvasObject.transform);
    }

    private void ClearExistingUi()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if (Application.isPlaying)
            {
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }

        canvasRoot = null;
        slotRects = null;
        slotBorders = null;
        slotBackgrounds = null;
        slotCountTexts = null;
    }

    private void CreateCrosshair(Transform parent)
    {
        GameObject crosshairObject = new GameObject("Crosshair", typeof(RectTransform), typeof(Image));
        crosshairObject.transform.SetParent(parent, false);

        RectTransform rectTransform = crosshairObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(6f, 6f);

        Image image = crosshairObject.GetComponent<Image>();
        image.sprite = GetWhiteSprite();
        image.color = Color.white;
        image.raycastTarget = false;
    }

    private void CreateHotbar(Transform parent)
    {
        GameObject hotbarObject = new GameObject("Hotbar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        hotbarObject.transform.SetParent(parent, false);

        RectTransform hotbarRect = hotbarObject.GetComponent<RectTransform>();
        hotbarRect.anchorMin = new Vector2(0.5f, 0f);
        hotbarRect.anchorMax = new Vector2(0.5f, 0f);
        hotbarRect.pivot = new Vector2(0.5f, 0f);
        hotbarRect.anchoredPosition = new Vector2(0f, 36f);
        hotbarRect.sizeDelta = new Vector2(282f, 50f);

        HorizontalLayoutGroup layoutGroup = hotbarObject.GetComponent<HorizontalLayoutGroup>();
        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.spacing = 8f;

        int slotCount = HotbarBlockTypes.Length;
        slotRects = new RectTransform[slotCount];
        slotBorders = new Image[slotCount];
        slotBackgrounds = new Image[slotCount];
        slotCountTexts = new Text[slotCount];

        for (int i = 0; i < slotCount; i++)
        {
            CreateSlot(hotbarObject.transform, i);
        }
    }

    private void CreateSlot(Transform parent, int slotIndex)
    {
        GameObject slotObject = new GameObject("Slot_" + (slotIndex + 1), typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        slotObject.transform.SetParent(parent, false);

        RectTransform slotRect = slotObject.GetComponent<RectTransform>();
        slotRect.sizeDelta = new Vector2(50f, 50f);

        LayoutElement layoutElement = slotObject.GetComponent<LayoutElement>();
        layoutElement.preferredWidth = 50f;
        layoutElement.preferredHeight = 50f;

        Image borderImage = slotObject.GetComponent<Image>();
        borderImage.sprite = GetWhiteSprite();
        borderImage.type = Image.Type.Sliced;
        borderImage.raycastTarget = false;

        GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObject.transform.SetParent(slotObject.transform, false);

        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2f, 2f);
        fillRect.offsetMax = new Vector2(-2f, -2f);

        Image fillImage = fillObject.GetComponent<Image>();
        fillImage.sprite = GetWhiteSprite();
        fillImage.raycastTarget = false;

        GameObject countObject = new GameObject("Count", typeof(RectTransform), typeof(Text));
        countObject.transform.SetParent(fillObject.transform, false);

        RectTransform countRect = countObject.GetComponent<RectTransform>();
        countRect.anchorMin = Vector2.zero;
        countRect.anchorMax = Vector2.one;
        countRect.offsetMin = Vector2.zero;
        countRect.offsetMax = Vector2.zero;

        Text countText = countObject.GetComponent<Text>();
        countText.font = GetBuiltInFont();
        countText.fontSize = 18;
        countText.alignment = TextAnchor.MiddleCenter;
        countText.color = Color.white;
        countText.raycastTarget = false;

        slotRects[slotIndex] = slotRect;
        slotBorders[slotIndex] = borderImage;
        slotBackgrounds[slotIndex] = fillImage;
        slotCountTexts[slotIndex] = countText;
    }

    private void RefreshHotbar()
    {
        if (slotRects == null || slotBorders == null || slotBackgrounds == null || slotCountTexts == null)
        {
            return;
        }

        int selectedIndex = inventory != null ? inventory.SelectedIndex : 0;

        for (int i = 0; i < HotbarBlockTypes.Length; i++)
        {
            if (slotRects[i] == null || slotBorders[i] == null || slotBackgrounds[i] == null || slotCountTexts[i] == null)
            {
                return;
            }

            bool isSelected = i == selectedIndex;
            slotRects[i].localScale = isSelected ? Vector3.one * 1.08f : Vector3.one;
            slotBorders[i].color = isSelected
                ? new Color(1f, 1f, 1f, 1f)
                : new Color(1f, 1f, 1f, 0.35f);
            slotBackgrounds[i].color = isSelected
                ? new Color(1f, 1f, 1f, 0.2f)
                : new Color(1f, 1f, 1f, 0.08f);

            int count = inventory != null ? inventory.GetHotbarCount(i) : 0;
            slotCountTexts[i].text = HotbarBlockTypes[i] == Chunk.Air ? "-" : count.ToString();
        }
    }

    private void TryFindInventory()
    {
        if (inventory == null)
        {
            inventory = FindObjectOfType<Inventory>();
        }
    }

    private static Sprite GetWhiteSprite()
    {
        if (whiteSprite == null)
        {
            whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        }

        return whiteSprite;
    }

    private static Font GetBuiltInFont()
    {
        if (builtInFont == null)
        {
            builtInFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        return builtInFont;
    }
}
