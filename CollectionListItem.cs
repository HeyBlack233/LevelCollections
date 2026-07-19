using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LevelCollections;

/// <summary>
/// ListViewItem subclass for collection/level list entries.
/// Binds a string name to a TextMeshProUGUI label and manages selection highlight.
/// </summary>
public class CollectionListItem : ListViewItem
{
    private static readonly Color SelColor = new Color(0f, 0f, 0f, 0.9f);
    private static readonly Color NormalColor = new Color(1f, 1f, 1f, 0f);

    public override void Bind(int index, object data)
    {
        base.Bind(index, data);
        var label = GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
            label.text = data as string ?? "";

        var btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(HandleClick);
        }
    }

    /// <summary>Toggle the selected highlight.</summary>
    public void SetActive(bool active)
    {
        var mb = GetComponent<MenuButton>();
        if (mb == null || mb.targetGraphic == null)
        {
            Debug.Log($"[SetActive] BAIL: mb={(mb!=null)} tg={(mb?.targetGraphic!=null)}");
            return;
        }

        var c = active ? SelColor : NormalColor;
        var oldColor = mb.targetGraphic.color;
        mb.isOn = active;
        // Set m_Color so future Graphic rebuilds use the correct colour
        mb.targetGraphic.color = c;
        // Also force the CanvasRenderer immediately, before crossfade tweens kick in
        mb.targetGraphic.CrossFadeColor(c, 0f, true, true);
        Debug.Log($"[SetActive] active={active} isOn={mb.isOn} oldColor={oldColor} newColor={mb.targetGraphic.color} graphicType={mb.targetGraphic.GetType().Name} enabled={mb.targetGraphic.enabled} tex={((mb.targetGraphic as RawImage)?.texture?.name ?? "N/A")}");
    }

    private void HandleClick()
    {
        var lv = GetComponentInParent<ListView>();
        if (lv != null)
            lv.OnSelect(this);
    }
}
