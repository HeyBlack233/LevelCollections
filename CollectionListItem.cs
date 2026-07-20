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

    private void Start()
    {
        var label = GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
        {
            label.enableAutoSizing = false;
            label.fontSize = 30;
        }
    }

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
    /// </summary>
    public new void OnSelect(BaseEventData eventData)
    {
        var lv = GetComponentInParent<ListView>();
        if (lv != null)
            lv.OnSelect(this);
    }

    /// <summary>Prepend or remove the "> " focus indicator on the label.</summary>
    public void SetFocusPrefix(bool focused)
    {
        var label = GetComponentInChildren<TextMeshProUGUI>();
        if (label == null) return;
        var text = label.text;
        if (focused && !text.StartsWith("> "))
            label.text = "> " + text;
        else if (!focused && text.StartsWith("> "))
            label.text = text.Substring(2);
    }

    /// <summary>Set the label text colour (e.g. red for broken collections).</summary>
    public void SetLabelColor(Color color)
    {
        var label = GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
            label.color = color;
    }
}
