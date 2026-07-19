using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LevelCollections;

/// <summary>
/// ListViewItem subclass for collection/level list entries.
/// Binds a string name to a TextMeshProUGUI label and manages MenuButton.isOn highlight.
/// </summary>
public class CollectionListItem : ListViewItem
{
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

        // DIAGNOSTIC: force visible color to confirm Image renders
        var img = GetComponent<Image>();
        if (img != null)
        {
            img.color = new Color(1f, 0f, 0f, 0.5f);
            Debug.Log($"[CollectionListItem] Bind #{index}: img.color=red50 sprite={(img.sprite?img.sprite.name:"NULL")} enabled={img.enabled}");
        }
    }

    /// <summary>Toggle the selected highlight via MenuButton.isOn + explicit color.</summary>
    public void SetActive(bool active)
    {
        var mb = GetComponent<MenuButton>();
        if (mb != null)
        {
            mb.isOn = active;
            if (mb.targetGraphic != null)
            {
                mb.targetGraphic.color = active
                    ? new Color(0f, 0f, 0f, 0.9f)
                    : new Color(1f, 1f, 1f, 0f);
            }
        }
    }

    private void HandleClick()
    {
        var lv = GetComponentInParent<ListView>();
        if (lv != null)
            lv.OnSelect(this);
    }
}
