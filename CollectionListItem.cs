using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LevelCollections;

/// <summary>
/// ListViewItem subclass for collection/level list entries.
/// Binds a string name to a TextMeshProUGUI label and manages selection highlight.
/// </summary>
public class CollectionListItem : ListViewItem, ISelectHandler
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
        if (mb == null || mb.targetGraphic == null) return;

        var c = active ? SelColor : NormalColor;
        mb.isOn = active;
        mb.targetGraphic.color = c;
        mb.targetGraphic.CrossFadeColor(c, 0f, true, true);
    }

    private void HandleClick()
    {
        var lv = GetComponentInParent<ListView>();
        if (lv != null)
            lv.OnSelect(this);
    }

    /// <summary>
    /// Re-implements ISelectHandler to always route through ListView.OnSelect.
    /// The base ListViewItem.OnSelect skips when device==Mouse to avoid
    /// double-firing with OnPointerEnter.  But when keyboard and mouse are used
    /// simultaneously, a mouse move in the same frame switches device to Mouse
    /// and the keyboard-driven OnSelect is suppressed — SetActive(true) is
    /// never called.  This override unconditionally calls ListView.OnSelect,
    /// closing the gap.  Duplicate calls in the same frame are harmless:
    /// OnColSelect checks _prevColItem==ci and SelectCollection checks _selCol==i.
    /// </summary>
    public new void OnSelect(BaseEventData eventData)
    {
        var lv = GetComponentInParent<ListView>();
        if (lv != null)
            lv.OnSelect(this);
    }
}
