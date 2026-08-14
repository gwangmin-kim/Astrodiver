using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class UpgradeTooltipUI : MonoBehaviour
{
    [Header("Content")]
    [SerializeField] private TextMeshProUGUI _displayNameText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private TextMeshProUGUI _valueText;
    [SerializeField] private GameObject _costGroup;
    [SerializeField] private TextMeshProUGUI _costStatusText;
    [SerializeField] private UpgradeTooltipCostEntryUI[] _costEntries;

    [Header("Placement")]
    [SerializeField] private UpgradeTooltipPositioner _positioner;

    private readonly UpgradeTooltipDataBuilder _dataBuilder = new();

    public void Show(
        UpgradeNodeUI node,
        int currentLevel,
        GameRuntimeData runtimeData)
    {
        if (node == null || node.Definition == null)
        {
            Hide();
            return;
        }

        UpgradeTooltipViewModel model = _dataBuilder.Build(
            node.Definition,
            currentLevel,
            runtimeData);
        ApplyContent(model);

        gameObject.SetActive(true);
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
        _positioner.SetTarget((RectTransform)node.transform);
    }

    public void Hide()
    {
        _positioner.ClearTarget();
        gameObject.SetActive(false);
    }

    private void ApplyContent(UpgradeTooltipViewModel model)
    {
        _displayNameText.text = model.DisplayName;
        _descriptionText.text = model.Description;
        _descriptionText.gameObject.SetActive(
            !string.IsNullOrWhiteSpace(model.Description));
        _valueText.text = string.Join("\n", model.EffectLines);

        int visibleCostCount = Mathf.Min(model.Costs.Count, _costEntries.Length);
        for (int i = 0; i < _costEntries.Length; i++)
        {
            bool visible = i < visibleCostCount;
            _costEntries[i].gameObject.SetActive(visible);
            if (visible)
            {
                UpgradeResourceCost cost = model.Costs[i];
                _costEntries[i].SetResource(cost.Resource, cost.Amount);
            }
        }

        bool showCosts = !model.IsMaxLevel && model.Costs.Count > 0;
        _costGroup.SetActive(showCosts);
        _costStatusText.text = model.IsMaxLevel ? "최대!" : "무료";
        _costStatusText.gameObject.SetActive(!showCosts);
        if (model.Costs.Count > _costEntries.Length)
        {
            Debug.LogWarning(
                $"Upgrade tooltip needs {model.Costs.Count} cost entries, " +
                $"but only {_costEntries.Length} are configured in the prefab.",
                this);
        }
    }
}
