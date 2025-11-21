using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace NeuroReachVR.UI
{
    /// <summary>
    /// Base class for menu management - eliminates duplicate SetActive() calls
    /// Provides smooth transitions with UIAnimator
    /// </summary>
    public class MenuManager : MonoBehaviour
    {
        [Header("Menu Configuration")]
        [SerializeField] protected bool useAnimations = true;
        [SerializeField] protected bool enableMenuHistory = false;

        protected Dictionary<string, GameObject> menus = new Dictionary<string, GameObject>();
        protected Stack<string> menuHistory = new Stack<string>();
        protected Coroutine activeTransition;
        protected string currentMenuName;

        protected virtual void Awake()
        {
            InitializeMenus();
        }

        /// <summary>
        /// Override this to register menus in the dictionary
        /// Example: RegisterMenu("main", mainMenuGameObject);
        /// </summary>
        protected virtual void InitializeMenus() { }

        /// <summary>
        /// Register a menu with a unique name
        /// </summary>
        protected void RegisterMenu(string menuName, GameObject menuObject)
        {
            if (menuObject == null)
            {
                Debug.LogWarning($"[MenuManager] Attempted to register null menu: {menuName}");
                return;
            }

            if (menus.ContainsKey(menuName))
                menus[menuName] = menuObject;
            else
                menus.Add(menuName, menuObject);

            // Ensure menu has CanvasGroup for animations
            if (menuObject.GetComponent<CanvasGroup>() == null)
                menuObject.AddComponent<CanvasGroup>();
        }

        /// <summary>
        /// Show a menu by name (hides all others)
        /// </summary>
        public void ShowMenu(string menuName, bool animated = true)
        {
            if (!menus.ContainsKey(menuName))
            {
                Debug.LogWarning($"[MenuManager] Menu not found: {menuName}");
                return;
            }

            if (activeTransition != null)
                StopCoroutine(activeTransition);

            if (enableMenuHistory && !string.IsNullOrEmpty(currentMenuName))
                menuHistory.Push(currentMenuName);

            currentMenuName = menuName;
            activeTransition = StartCoroutine(ShowMenuCoroutine(menuName, animated && useAnimations));
        }

        /// <summary>
        /// Hide a specific menu
        /// </summary>
        public void HideMenu(string menuName, bool animated = true)
        {
            if (!menus.ContainsKey(menuName))
            {
                Debug.LogWarning($"[MenuManager] Menu not found: {menuName}");
                return;
            }

            if (activeTransition != null)
                StopCoroutine(activeTransition);

            activeTransition = StartCoroutine(HideMenuCoroutine(menuName, animated && useAnimations));
        }

        /// <summary>
        /// Hide all menus
        /// </summary>
        public void HideAllMenus(bool animated = true)
        {
            if (activeTransition != null)
                StopCoroutine(activeTransition);

            activeTransition = StartCoroutine(HideAllMenusCoroutine(animated && useAnimations));
        }

        /// <summary>
        /// Go back to previous menu in history
        /// </summary>
        public void ShowPreviousMenu()
        {
            if (!enableMenuHistory)
            {
                Debug.LogWarning("[MenuManager] Menu history is disabled");
                return;
            }

            if (menuHistory.Count == 0)
            {
                Debug.LogWarning("[MenuManager] No previous menu in history");
                return;
            }

            string previousMenu = menuHistory.Pop();
            ShowMenu(previousMenu);
        }

        /// <summary>
        /// Clear menu history
        /// </summary>
        public void ClearHistory()
        {
            menuHistory.Clear();
        }

        // Coroutines
        private IEnumerator ShowMenuCoroutine(string menuName, bool animated)
        {
            // Hide all menus first
            yield return HideAllMenusCoroutine(animated);

            GameObject menuToShow = menus[menuName];

            if (animated)
            {
                CanvasGroup canvasGroup = menuToShow.GetComponent<CanvasGroup>();
                canvasGroup.alpha = 0f;
                menuToShow.SetActive(true);
                yield return UIAnimator.FadeIn(canvasGroup);
            }
            else
            {
                menuToShow.SetActive(true);
            }
        }

        private IEnumerator HideMenuCoroutine(string menuName, bool animated)
        {
            GameObject menuToHide = menus[menuName];

            if (!menuToHide.activeSelf)
                yield break;

            if (animated)
            {
                CanvasGroup canvasGroup = menuToHide.GetComponent<CanvasGroup>();
                yield return UIAnimator.FadeOut(canvasGroup);
            }

            menuToHide.SetActive(false);
        }

        private IEnumerator HideAllMenusCoroutine(bool animated)
        {
            List<Coroutine> hideCoroutines = new List<Coroutine>();

            foreach (var kvp in menus)
            {
                if (kvp.Value.activeSelf)
                {
                    if (animated)
                    {
                        CanvasGroup canvasGroup = kvp.Value.GetComponent<CanvasGroup>();
                        hideCoroutines.Add(StartCoroutine(UIAnimator.FadeOut(canvasGroup)));
                    }
                    else
                    {
                        kvp.Value.SetActive(false);
                    }
                }
            }

            // Wait for all fade animations to complete
            if (animated)
            {
                foreach (var coroutine in hideCoroutines)
                    yield return coroutine;

                foreach (var kvp in menus)
                    kvp.Value.SetActive(false);
            }
        }

        /// <summary>
        /// Check if a menu is currently visible
        /// </summary>
        public bool IsMenuVisible(string menuName)
        {
            return menus.ContainsKey(menuName) && menus[menuName].activeSelf;
        }

        /// <summary>
        /// Get the currently active menu name
        /// </summary>
        public string GetCurrentMenuName()
        {
            return currentMenuName;
        }
    }
}
