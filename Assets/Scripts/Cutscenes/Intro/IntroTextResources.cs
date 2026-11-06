namespace Astrodiver.Cutscenes.Intro
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Localization;

    [CreateAssetMenu(menuName = "Astrodiver/Intro/Text References")]
    public sealed class IntroTextResources : ScriptableObject
    {
        public LocalizedString programmingRunning = new();
        public LocalizedString programmingComplete = new();
        public LocalizedString continuePrompt = new();
        public LocalizedString loadingPrompt = new();
        public LocalizedString hubLoadError = new();

        public IEnumerable<LocalizedString> References
        {
            get
            {
                yield return programmingRunning;
                yield return programmingComplete;
                yield return continuePrompt;
                yield return loadingPrompt;
                yield return hubLoadError;
            }
        }
    }
}
