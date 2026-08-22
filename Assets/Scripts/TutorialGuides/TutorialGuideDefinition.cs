using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "TutorialGuideDefinition",
    menuName = "Astrodiver/Tutorials/Tutorial Guide Definition")]
public sealed class TutorialGuideDefinition : GameDefinition
{
    [SerializeField, TextArea] private string _text;
    [SerializeField] private bool _requireNoCompletedEvents;
    [SerializeField] private GameProgressEventId[] _requiredCompletedEvents =
        Array.Empty<GameProgressEventId>();
    [SerializeField] private GameProgressEventId[] _forbiddenCompletedEvents =
        Array.Empty<GameProgressEventId>();
    [SerializeField] private GameProgressEventId _completionEvent;

    public string Text => _text;
    public bool RequireNoCompletedEvents => _requireNoCompletedEvents;
    public IReadOnlyList<GameProgressEventId> RequiredCompletedEvents =>
        _requiredCompletedEvents ?? Array.Empty<GameProgressEventId>();
    public IReadOnlyList<GameProgressEventId> ForbiddenCompletedEvents =>
        _forbiddenCompletedEvents ?? Array.Empty<GameProgressEventId>();
    public GameProgressEventId CompletionEvent => _completionEvent;

    public bool IsVisibleFor(GameDataManager gameData)
    {
        if (gameData == null || !gameData.IsInitialized ||
            _completionEvent == GameProgressEventId.None ||
            gameData.IsEventCompleted(_completionEvent))
        {
            return false;
        }

        if (_requireNoCompletedEvents &&
            gameData.SaveData.completedEvents.Count > 0)
        {
            return false;
        }

        for (int index = 0; index < RequiredCompletedEvents.Count; index++)
        {
            if (!gameData.IsEventCompleted(RequiredCompletedEvents[index]))
            {
                return false;
            }
        }

        for (int index = 0; index < ForbiddenCompletedEvents.Count; index++)
        {
            if (gameData.IsEventCompleted(ForbiddenCompletedEvents[index]))
            {
                return false;
            }
        }

        return true;
    }
}
