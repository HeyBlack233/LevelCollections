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
        // NOTE: ListView.Bind calls us BEFORE SetActive(true), so Awake
        // hasn't run yet — resolve the label directly here.
        var label = GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
            label.text = data as string ?? "";

        // Hook Button.onClick to guarantee mouse clicks work even when
        // the game hasn't switched its input device to Mouse mode.
        var btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(HandleClick);
        }
    }

    /// <summary>Toggle the selected highlight via MenuButton.isOn.</summary>
    public void SetActive(bool active)
    {
        var mb = GetComponent<MenuButton>();
        if (mb != null) mb.isOn = active;
    }

    private void HandleClick()
    {
        var lv = GetComponentInParent<ListView>();
        if (lv != null)
            lv.OnSelect(this);
    }
}
