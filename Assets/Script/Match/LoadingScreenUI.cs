using UnityEngine;
using TMPro;

namespace Match
{
    public class LoadingScreenUI : MonoBehaviour
    {
        public static LoadingScreenUI Instance { get; private set; }

        public Animator transitionAnimator;
        public TMP_Text loadingText;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void SetLoadingText(string text)
        {
            if (loadingText) loadingText.text = text;
        }

        public void FinishLoadingAndReveal()
        {
            // Hide the text
            if (loadingText) loadingText.gameObject.SetActive(false);
            
            // Trigger the transition
            if (transitionAnimator)
            {
                transitionAnimator.SetBool("OutIn", true);
                transitionAnimator.SetTrigger("isLoad");
            }
        }
    }
}
