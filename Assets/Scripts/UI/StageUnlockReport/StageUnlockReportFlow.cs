using System;
using UnityEngine;

/// <summary>
/// Controls the short stage-unlock report sequence independently from the
/// whiteboard tutorial. Its page is a pre-authored prefab; this class only
/// determines when that page is displayed.
/// </summary>
public sealed class StageUnlockReportFlow : IDisposable
{
    private readonly UpgradeTreeUI _upgradeTree;
    private readonly TutorialDocumentView _document;
    private readonly StageUnlockReportPageEntry[] _reportPages;
    private bool _subscribed;

    public StageUnlockReportFlow(
        UpgradeTreeUI upgradeTree,
        TutorialDocumentView document,
        StageUnlockReportPageEntry[] reportPages)
    {
        _upgradeTree = upgradeTree;
        _document = document;
        _reportPages = reportPages ?? Array.Empty<StageUnlockReportPageEntry>();
    }

    public bool IsActive { get; private set; }

    public void Enable()
    {
        if (_subscribed)
        {
            return;
        }

        if (_upgradeTree != null)
        {
            _upgradeTree.PurchaseAttempted += HandlePurchaseAttempted;
        }

        if (_document != null)
        {
            _document.Closed += HandleDocumentClosed;
        }

        _subscribed = true;
    }

    public void Dispose()
    {
        if (!_subscribed)
        {
            return;
        }

        if (_upgradeTree != null)
        {
            _upgradeTree.PurchaseAttempted -= HandlePurchaseAttempted;
        }

        if (_document != null)
        {
            _document.Closed -= HandleDocumentClosed;
        }

        _subscribed = false;
    }

    private void HandlePurchaseAttempted(
        UpgradeNodeUI node,
        UpgradePurchaseResult result)
    {
        if (IsActive || !result.Succeeded ||
            !TryGetReportPage(result.NodeId, out GameObject reportPagePrefab))
        {
            return;
        }

        if (_upgradeTree == null || _document == null)
        {
            Debug.LogError(
                "Stage unlock report flow is missing a required reference.");
            return;
        }

        IsActive = true;
        _upgradeTree.gameObject.SetActive(false);
        if (!_document.OpenWithTemporaryPage(reportPagePrefab))
        {
            Finish();
        }
    }

    private void HandleDocumentClosed()
    {
        if (IsActive)
        {
            Finish();
        }
    }

    private void Finish()
    {
        IsActive = false;
        if (_upgradeTree != null)
        {
            _upgradeTree.gameObject.SetActive(true);
        }
    }

    private bool TryGetReportPage(
        string upgradeId,
        out GameObject reportPagePrefab)
    {
        for (int i = 0; i < _reportPages.Length; i++)
        {
            StageUnlockReportPageEntry entry = _reportPages[i];
            if (entry != null && entry.Matches(upgradeId) &&
                entry.PagePrefab != null)
            {
                reportPagePrefab = entry.PagePrefab;
                return true;
            }
        }

        reportPagePrefab = null;
        return false;
    }
}

[Serializable]
public sealed class StageUnlockReportPageEntry
{
    [SerializeField] private string _upgradeId;
    [SerializeField] private GameObject _pagePrefab;

    public GameObject PagePrefab => _pagePrefab;

    public bool Matches(string upgradeId)
    {
        return string.Equals(_upgradeId, upgradeId, StringComparison.Ordinal);
    }
}
