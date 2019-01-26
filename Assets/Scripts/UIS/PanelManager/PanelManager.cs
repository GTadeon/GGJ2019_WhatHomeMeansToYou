using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// this script is attached to panel manager GO. It allows you to adjust settings related to inventory/item toolbar/crafting panels such
/// as ways in which they appear/disappear, hot keys used for bringing them up /closing them, etc.
/// </summary>
public class PanelManager : MonoBehaviour {

    [Tooltip("place your canvas game object here")]
    public GameObject Canvas;
    public static Vector2 CanvasReferenceResolution;

    private float canvasWidth;
    private float canvasHeight;


    public enum AppearTransition { noAnimation, fadeIn, flyInFromLeft, flyInFromTop, flyInFromRight, flyInFromBottom }
    public enum DisappearTransition { noAnimation, fadeOut, flyAwayLeft, flyAwayTop, flyAwayRight, flyAwayBottom }

    public enum TransitionSide { left, right, top, bottom}

    private static bool isTransitionInProcess;


    [Tooltip("check this if you want to set call keys for each assigned panel independently")]
    public bool setKeyCalls;
    [Tooltip("set for each panel show/hide key")]
    public List<KeyCode> showPanelIndividuallyKeys = new List<KeyCode>();

    [Tooltip("show/hide all panels on this key press")]
    public KeyCode showAllPanelsKey;

    [Tooltip("hide all panels when game starts (checked) or leave them visible (unchecked) ?")]
    public bool hideAllOnStart;
    [Tooltip("pause game when key for calling all panels is pressed ? ")]
    public bool pauseGameOnShowAll;
    private static bool gamePaused;


    private bool areAllPanelsVisible; //if false, then all are not visible, except the ones who have constantlyVisible checked.  :) 
    [Tooltip("should cursor texture change when hovering over panels?")]
    public bool cursorChangeOnHover;
    public Texture2D cursorOverPanelTexture; //if assigned, then you get different cursor when you call all panels.


    [Serializable]
    public class CallablePanel
    {
        [Tooltip("Read only value. Panel id is used for different internal checks.")]
        public int PanelId;

        [Tooltip("UI element that you want to display/hide. It must have Canvas Group component attached if you want to apply fade in /fade out effects on it, else it's fine without it.")]
        public GameObject panel;
        [Tooltip("Should this element be constantly visible?")]
        public bool constantlyVisible;
        [Tooltip("check this if you want this panel to render above everything everytime it should be shown. ")]
        public bool renderAboveEverything;
        [Tooltip("Ignore display calls, done by key pressed for displaying all panels, for this panel?")]
        public bool notInfluencedByShowAllPanelsKeyAtAll;
        [Tooltip("Only hide this item when key for displaying all panels is pressed ? ")]
        public bool influencedByShowAllPanelsKeyOnlyOnDisappear;
        [Tooltip("Should game be paused when this element is displayed?")]
        public bool pauseGameOnShow;
        [Tooltip("effect when displaying this element?")]
        public AppearTransition appearTransition;
        [Tooltip("effect when hiding this element?")]
        public DisappearTransition disappearTransition;

        [Tooltip("check this if you want to hide/show panel by clicking on a button. You can assign buttons by scrolling all the way below the last panel in this array.")]
        //[SerializeField]
        public bool callOnButtonPress;
        [Tooltip("please drag n drop button after which click a panel gets displayed. Your button must have PanelButtonHandler script attahed.")]
        //[HideInInspector]
        //[SerializeField]
        public PanelButtonHandler showPanelButton;
        [Tooltip("please drag n drop button after which click a panel gets hidden. Your button must have PanelButtonHandler script attahed.")]
        //[HideInInspector]
        //[SerializeField]
        public PanelButtonHandler hidePanelButton;
        [Tooltip("check this if you want that only one show/display button is visible at the time. E.G. useful when you want to hide hide panel button when show panel button is clicked or vice versa. If you check this, you can't assign the same button for showing as the one used for hiding a panel, since checking this implies that you have two different buttons in the scene.")]
        public bool oneButtonVisible;

        [Tooltip("should this panel get hidden after time t (seconds) ?")]
        public bool shouldHideAfterTime;
        public float hideAfterTime;

        //to be set on awake:
        [HideInInspector]
        public Vector2 originalPos;
        [HideInInspector]
        public CanvasGroup canvasGroup;
        [HideInInspector]
        public RectTransform rectTransform;
    }
    [Tooltip("place here UI elements that you want to manage (show/hide on screen)")]
    public CallablePanel[] panels;
    public static CallablePanel [] _panels; //Why another one? Cuz I need to access it from other scripts and I don't want thousands of references in various scripts since the whole asset loses on its flexibility then .. :)

    public static PanelManager panelManager; //same thing man , I needed it because I can't call this class coroutines in static methods otherwise :)


    //EVENTS FOR BROADCASTING
    //--------------EVENTS--------------
    #region --------EVENTS-------------
    public event PanelDisplayedHandler NewPanelDisplayedManager;

    public class PanelDisplayedArgs : EventArgs
    {
        public CallablePanel PanelThatGotDisplayed { get; set; }
    }

    public delegate void PanelDisplayedHandler(PanelDisplayedArgs args);
    #endregion

    void Awake()
    {
        _panels = panels;
        try
        {
            CanvasReferenceResolution = Canvas.gameObject.GetComponent<CanvasScaler>().referenceResolution;
        }
        catch
        {
            throw new System.ArgumentException("please assign canvas game object that has canvas scaler component attached with UI scale mode of type 'scale with screen size' to canvas slot. Without that we can't show tooltip panel properly :/. Just hit the stats button on game window , read your current resolution and type that in in canvas scaler component under 'reference resolution' on your canvas game object that you plan to assign to PanelManager canvas slot. ");
        }
        panelManager = GetComponent<PanelManager>();
        gamePaused = false;

        canvasWidth = Canvas.GetComponent<RectTransform>().sizeDelta.x;
        canvasHeight = Canvas.GetComponent<RectTransform>().sizeDelta.y;
        isTransitionInProcess = false;
        SetNeededPanelPrivateFields();
        AdjustAlphaValues();
        DeactivateHideButtons();
    }

    void Start()
    {
        if (hideAllOnStart)
            HideAllPanels();
        else
            ShowAllPanels();

    }

    void Update()
    {
        if (!isTransitionInProcess)
        {
            if (setKeyCalls)
            {
                CheckForIndividualPanelCallKey();
            }
            if (Input.GetKeyDown(showAllPanelsKey) && !areAllPanelsVisible)
            {
                ShowAllPanels();
                if (pauseGameOnShowAll && !gamePaused)
                    StartCoroutine(pauseWhenTransitionEnds());
            }
            else if (Input.GetKeyDown(showAllPanelsKey) && areAllPanelsVisible)
            {
                HideAllPanels();
                if (pauseGameOnShowAll && gamePaused)
                    UnPauseGame();
            }

        }
    }

    private void SetNeededPanelPrivateFields()
    {
        SetCanvasFields();
    }

    private void SetCanvasFields()
    {
        for (int i = 0; i < panels.Length; i++)
        {
            panels[i].originalPos = panels[i].panel.GetComponent<RectTransform>().anchoredPosition; //original position
            panels[i].rectTransform = panels[i].panel.GetComponent<RectTransform>(); //Rect transform
            panels[i].canvasGroup = panels[i].panel.GetComponent<CanvasGroup>(); // canvas group
        }
    }

    private void ChangeCursorTexture(Texture2D cursorTexture)
    {
        if (cursorChangeOnHover)
            Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.Auto);
    }

    /// <summary>
    /// Sets cursor texture to default one.
    /// </summary>
    private void ChangeCursorTexture()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    /// <summary>
    /// loops trhough panels and sets alpha to 1 if 
    /// panel's transition is set to fade out, or 
    /// 0 if set to fade in. Should be set on awake
    /// </summary>
    private void AdjustAlphaValues()
    {
        foreach (var panel in panels)
        {
            if (panel.appearTransition == AppearTransition.fadeIn)
            {
                panel.canvasGroup.alpha = 0f;
            }
            else if (panel.disappearTransition == DisappearTransition.fadeOut)
            {
                panel.canvasGroup.alpha = 1f;
            }
        }
    }





    //show all panels from panels array having in mind their transitional animations:
    private void ShowAllPanels()
    {
        ChangeCursorTexture(cursorOverPanelTexture);
        for (int i = 0; i < panels.Length; i++)
        {
            //we are showing only those that are influenced by show all panels key. 
            if (!panels[i].notInfluencedByShowAllPanelsKeyAtAll && !panels[i].influencedByShowAllPanelsKeyOnlyOnDisappear)
                AppearTransitionEffectINIT(panels[i]);
        }
        areAllPanelsVisible = true;
    }

    //hides all panels from the panels array, except the ones that should be visible all the time
    private void HideAllPanels()
    {
        ChangeCursorTexture();
        for (int i = 0; i < panels.Length; i++)
        {
            if (!panels[i].constantlyVisible && !panels[i].notInfluencedByShowAllPanelsKeyAtAll)
                DisappearTransitionEffectINIT(panels[i]);
        }
        areAllPanelsVisible = false;

    }

    private IEnumerator pauseWhenTransitionEnds()
    {
        while (isTransitionInProcess)
        {
            yield return null;
        }
        PauseGame();
    }


    private static void PauseGame()
    {
        Time.timeScale = 0;
        gamePaused = true;
    }

    private static void UnPauseGame()
    {
        Time.timeScale = 1;
        gamePaused = false;
    }

  

    private void CheckForIndividualPanelCallKey()
    {
        for (int i = 0; i < showPanelIndividuallyKeys.Count; i++)
        {
            if (Input.GetKeyDown(showPanelIndividuallyKeys[i]))
            {
                if (!panels[i].panel.activeInHierarchy)
                {
                    AppearTransitionEffectINIT(panels[i]);
                    if (panels[i].pauseGameOnShow)
                        StartCoroutine(pauseWhenTransitionEnds());

                }
                else if (!panels[i].constantlyVisible)
                {
                    DisappearTransitionEffectINIT(panels[i]);
                    if (panels[i].pauseGameOnShow)
                        UnPauseGame();
                }   
            }
        }
    }

    public IEnumerator HideAfterTimeT(CallablePanel panel, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        DisappearTransitionEffectINIT(panel);
    }

    //methods for effects of appearing/disappearing:
    public static void AppearTransitionEffectINIT(CallablePanel panel)
    {
        panel.panel.SetActive(true);
        if (panel.renderAboveEverything)
            panel.panel.transform.SetAsLastSibling();
        switch (panel.appearTransition)
        {
            case AppearTransition.noAnimation:
                panel.panel.SetActive(true);
                break;
            case AppearTransition.fadeIn:
                panel.canvasGroup.alpha = 0f;
                panelManager.StartCoroutine(panelManager.FadeIn(panel, 1f));
                break;
            case AppearTransition.flyInFromLeft:
                panelManager.StartCoroutine(panelManager.FlyInFromLeft(panel, 1f));
                break;
            case AppearTransition.flyInFromTop:
                panelManager.StartCoroutine(panelManager.FlyInFromTop(panel, 1f));
                break;
            case AppearTransition.flyInFromRight:
                panelManager.StartCoroutine(panelManager.FlyInFromRight(panel, 1f));
                break;
            case AppearTransition.flyInFromBottom:
                panelManager.StartCoroutine(panelManager.FlyInFromBottom(panel, 1f));
                break;
            default:
                break;
        }
        if (panel.shouldHideAfterTime)
            panelManager.StartCoroutine(panelManager.HideAfterTimeT(panel, panel.hideAfterTime));

        //trigger panel displayed event
        if (panelManager.NewPanelDisplayedManager != null)
        {
            PanelDisplayedArgs panelDisplayedArgs = new PanelDisplayedArgs() { PanelThatGotDisplayed = panel };
            panelManager.NewPanelDisplayedManager(panelDisplayedArgs);
        }
    }

    public static void DisappearTransitionEffectINIT(CallablePanel panel)
    {
        switch (panel.disappearTransition)
        {
            case DisappearTransition.noAnimation:
                panel.panel.SetActive(false);
                break;
            case DisappearTransition.fadeOut:
                panel.canvasGroup.alpha = 1f;
                panelManager.StartCoroutine(panelManager.FadeOut(panel, 1f));
                break;
            case DisappearTransition.flyAwayLeft:
                panelManager.StartCoroutine(panelManager.FlyAwayLeft(panel, 1f));
                break;
            case DisappearTransition.flyAwayTop:
                panelManager.StartCoroutine(panelManager.FlyAwayTop(panel, 1f));
                break;
            case DisappearTransition.flyAwayRight:
                panelManager.StartCoroutine(panelManager.FlyAwayRight(panel, 1f));
                break;
            case DisappearTransition.flyAwayBottom:
                panelManager.StartCoroutine(panelManager.FlyAwayBottom(panel, 1f));
                break;
            default:
                break;
        }
    }


    /// <summary>
    /// based on the passed button clicked, looks for it in array (based on gameobject.id that has PanelButtonHandler script attached).
    /// Then, based on the settings made, hides or shows panel that it corresponds to.
    /// </summary>
    public static void HandleButtonClick(GameObject buttonClicked)
    {
        //first, get the panel that it corresponds to:
        int buttonClickedId = buttonClicked.GetInstanceID();


        List<CallablePanel> panels =  GetPanelsByDisplayButton(buttonClicked);

        for (int i = 0; i < panels.Count; i++)
        {

            int panelShowButtonId = panels[i].showPanelButton.gameObject.GetInstanceID();
            int panelHideButtonId = panels[i].hidePanelButton.gameObject.GetInstanceID();
            bool areShowHideTheSameButton = AreShowHideButtonsTheSame(panels[i]);

            //checking if show and hide buttons are virtually the same
            if (areShowHideTheSameButton)
            {
                //if the same button is used for hiding and showing, then check if that panel is active, if it is deactivate it. If it's deactivated, activate it.
                if (panels[i].panel.activeInHierarchy)
                    DisappearTransitionEffectINIT(panels[i]);
                else
                    AppearTransitionEffectINIT(panels[i]);
            }
            else
            {
                //if button used for displaying is not the same as the one used for hiding a panel then check whether is used for hiding or showing.
                //if it's for showing, show a panel, if it's for hiding, hide a panel.
                if (panelShowButtonId == buttonClickedId)
                {
                    if (!isTransitionInProcess)
                    {
                        AppearTransitionEffectINIT(panels[i]);
                        if (panels[i].oneButtonVisible)
                            AntagonistButtonDeactivation(panels[i], buttonClicked);
                    }

                }
                else if (panelHideButtonId == buttonClickedId)
                {
                    if (!isTransitionInProcess)
                    {
                        DisappearTransitionEffectINIT(panels[i]);
                        if (panels[i].oneButtonVisible)
                            AntagonistButtonDeactivation(panels[i], buttonClicked);
                    }

                }
            }
        }

    }

    public static void HandleButtonClick(int[] idsOfPanelsThatShouldBeVisibleAndOthersHiddenOnClick)
    {
        foreach (var panel in _panels)
        {
            var shouldBeVisible = idsOfPanelsThatShouldBeVisibleAndOthersHiddenOnClick.Any(id=>id==panel.PanelId);
            if (shouldBeVisible)
            {
                AppearTransitionEffectINIT(panel);
            }
            else
            {
                DisappearTransitionEffectINIT(panel);
            }
        }
    }

    public static void HandleButtonClick(int idOfPanelsThatShouldBeVisibleAndOthersHiddenOnClick)
    {
        foreach (var panel in _panels)
        {
            var shouldBeVisible = panel.PanelId==idOfPanelsThatShouldBeVisibleAndOthersHiddenOnClick;
            if (shouldBeVisible)
            {
                AppearTransitionEffectINIT(panel);
            }
            else
            {
                DisappearTransitionEffectINIT(panel);
            }
        }
    }


    /// <summary>
    /// if call panel on button press is implemented as feature, for iterated panel,
    /// searches the one used for hiding panels and deactivates it.
    /// It's a good idea to call this e.g. on awake :)
    /// </summary>
    private static void DeactivateHideButtons()
    {
        for (int i = 0; i < _panels.Length; i++)
        {
            if (_panels[i].callOnButtonPress && _panels[i].oneButtonVisible)
            {
                bool sameButtonForBoth = AreShowHideButtonsTheSame(_panels[i]);
                if (!sameButtonForBoth)
                {
                    _panels[i].hidePanelButton.gameObject.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// pass panel and its button (show or hide) that you want to deactivate in antagonist manner.
    /// Meaning, if you pass show button, then it will deactivate show button, but activate hide button.
    /// If you pass hide button, then it will deactivate hide button, but  activate show button.
    /// </summary>
    private static void AntagonistButtonDeactivation(CallablePanel panel, GameObject button)
    {
        if (panel.showPanelButton.gameObject.GetInstanceID() == button.GetInstanceID())
        {
            button.SetActive(false);
            panel.hidePanelButton.gameObject.SetActive(true);
        }
        else if (panel.hidePanelButton.gameObject.GetInstanceID() == button.GetInstanceID())
        {
            button.SetActive(false);
            panel.showPanelButton.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// checks whether or not are show/display buttons the same for passed panel
    /// </summary>
    private static bool AreShowHideButtonsTheSame(CallablePanel panel)
    {
        if (panel.showPanelButton.GetInstanceID() == panel.hidePanelButton.GetInstanceID())
            return true;
        else
            return false;
    }


    /// <summary>
    /// gets CallablePanel based on the passed button for which it checks in array for the instance that has it set as 
    /// showpanelbutton or hidepanelbutton. 
    /// </summary>
    public static List<CallablePanel> GetPanelsByDisplayButton(GameObject button)
    {
        List<CallablePanel> panels = new List<CallablePanel>();
        int buttonId = button.GetInstanceID();

        for (int i = 0; i < _panels.Length; i++)
        {
            if (_panels[i].callOnButtonPress)
            {
                if ((_panels[i].showPanelButton.gameObject.GetInstanceID() == buttonId) || (_panels[i].hidePanelButton.gameObject.GetInstanceID() == buttonId))
                {
                    panels.Add(_panels[i]);
                }
            }
        }
        return panels;
    }


    public static void ShowPanelWithId(int panelId)
    {
        var panel = _panels.Where(x => x.PanelId == panelId).FirstOrDefault();
        AppearTransitionEffectINIT(panel);
    }

    public static void HidePanelWithId(int panelId)
    {
        var panel = _panels.Where(x => x.PanelId == panelId).FirstOrDefault();
        DisappearTransitionEffectINIT(panel);
    }

    ///
    ///****** panels' transition methods (appear / disappear effects and animations ) ******
    ///

    #region transition effect methods 

    private IEnumerator FadeIn(CallablePanel panel, float fadeTime)
    {
        isTransitionInProcess = true;
        panel.rectTransform.anchoredPosition = panel.originalPos;
        float elapsedTime = 0;

        while (elapsedTime < fadeTime)
        {
            float A = panel.canvasGroup.alpha;
            float B = 1f;

            panel.canvasGroup.alpha = Mathf.Lerp(A, B, elapsedTime / fadeTime); 
            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();

        }
        isTransitionInProcess = false;
    }

    private IEnumerator FadeOut(CallablePanel panel, float fadeTime)
    {
        isTransitionInProcess = true;
        GameObject panelGO = panel.panel;
        float elapsedTime = 0;

        while (elapsedTime < fadeTime)
        {
            float A = panel.canvasGroup.alpha;
            float B = 0f;

            panel.canvasGroup.alpha = Mathf.Lerp(A, B, elapsedTime / fadeTime);
            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();

        }
        isTransitionInProcess = false;
        panelGO.SetActive(false);
    }


    //based on the anchors : left | center | right 
    //0=left anchor, 0.5=when anchored center, 1=right. Returns where it should be when transitioning from left
    private Vector2 GetPositionByAnchorForLeftorRight(CallablePanel panel, RectTransform panelRectTransform, TransitionSide leftOrRight)
    {

        Vector2 positionToGoTo;

        if (panelRectTransform.anchorMax.x == 0)
            positionToGoTo= new Vector2(-panelRectTransform.sizeDelta.x, panel.originalPos.y);
        else if (panelRectTransform.anchorMax.x == 0.5)
            positionToGoTo= new Vector2((-panelRectTransform.sizeDelta.x - canvasWidth) / 2f, panel.originalPos.y);
        else
        {
            //else if    ... == 1f :
            positionToGoTo= new Vector2(-panelRectTransform.sizeDelta.x - canvasWidth / 2f - panelRectTransform.sizeDelta.x / 2f, panel.originalPos.y);
        }

        switch (leftOrRight)
        {
            case TransitionSide.left:
                return positionToGoTo;
            case TransitionSide.right:
                return new Vector2(positionToGoTo.x*-1, positionToGoTo.y); //just to turn it other way round
            default:
                return positionToGoTo;
        }

    }
    //0=bottom anchor, 0.5=when anchored center, 1=top. Returns where it should be when transitioning from top

    private Vector2 GetPositionByAnchorForToporBottom(CallablePanel panel, RectTransform panelRectTransform, TransitionSide topOrBottom)
    {
        Vector2 positionToGoTo;


        if (panelRectTransform.anchorMax.y == 0)
            positionToGoTo= new Vector2(panel.originalPos.x, canvasHeight + (panelRectTransform.sizeDelta.y/2) );
        else if (panelRectTransform.anchorMax.y == 0.5)
            positionToGoTo= new Vector2(panel.originalPos.x, (canvasHeight/2) + panelRectTransform.sizeDelta.y/2);
        else
        {
            //else if    ... == 1f :
            positionToGoTo= new Vector2(panel.originalPos.x, panelRectTransform.sizeDelta.y);
        }

        switch (topOrBottom)
        {
            case TransitionSide.top:
                return positionToGoTo;
            case TransitionSide.bottom:
                return new Vector2(positionToGoTo.x, positionToGoTo.y*-1); //just to turn it other way round
            default:
                return positionToGoTo;
        }
    }


  
    private IEnumerator FlyInFromLeft(CallablePanel panel, float flyInTime)
    {
        isTransitionInProcess = true;
        GameObject panelGO = panel.panel;

        panel.canvasGroup.alpha = 1f;

        Vector2 A = GetPositionByAnchorForLeftorRight(panel, panel.rectTransform, TransitionSide.left);
        Vector2 B = panel.originalPos;

        float elapsedTime = 0;
        while (elapsedTime < flyInTime)
        {
            panel.rectTransform.anchoredPosition = Vector2.Lerp(A, B, elapsedTime / flyInTime);
            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();

        }
        isTransitionInProcess = false;
    }

    private IEnumerator FlyAwayLeft(CallablePanel panel, float flyAwayTime)
    {
        isTransitionInProcess = true;
        GameObject panelGO = panel.panel;

        Vector2 A = panel.originalPos ;
        Vector2 B = GetPositionByAnchorForLeftorRight(panel, panel.rectTransform, TransitionSide.left);

        float elapsedTime = 0;
        while (elapsedTime < flyAwayTime)
        {
            panel.rectTransform.anchoredPosition = Vector2.Lerp(A, B, elapsedTime / flyAwayTime);
            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();

        }
        isTransitionInProcess = false;
        panel.canvasGroup.alpha = 0f;
        panelGO.SetActive(false);

    }

    private IEnumerator FlyInFromTop(CallablePanel panel, float flyInTime)
    {
        isTransitionInProcess = true;
        panel.canvasGroup.alpha = 1f;

        Vector2 A = GetPositionByAnchorForToporBottom(panel, panel.rectTransform, TransitionSide.top);
        Vector2 B = panel.originalPos;

        float elapsedTime = 0;
        while (elapsedTime < flyInTime)
        {
            elapsedTime += Time.deltaTime;
            panel.rectTransform.anchoredPosition = Vector2.Lerp(A, B, elapsedTime / flyInTime);
            yield return new WaitForEndOfFrame();
        }
        isTransitionInProcess = false;
    }

    private IEnumerator FlyAwayTop(CallablePanel panel, float flyAwayTime)
    {
        isTransitionInProcess = true;
        GameObject panelGO = panel.panel;
        panel.canvasGroup.alpha = 1f;

        Vector2 A = panel.originalPos ;
        Vector2 B = GetPositionByAnchorForToporBottom(panel, panel.rectTransform, TransitionSide.top);

        float elapsedTime = 0;
        while (elapsedTime < flyAwayTime)
        {
            panel.rectTransform.anchoredPosition = Vector2.Lerp(A, B, elapsedTime / flyAwayTime);
            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();

        }
        isTransitionInProcess = false;
        panel.canvasGroup.alpha = 0f;
        panelGO.SetActive(false);
    }

    private IEnumerator FlyInFromRight(CallablePanel panel, float flyInTime)
    {
        isTransitionInProcess = true;
        GameObject panelGO = panel.panel;
        panel.canvasGroup.alpha = 1f;

        Vector2 A = GetPositionByAnchorForLeftorRight(panel, panel.rectTransform, TransitionSide.right); 
        Vector2 B = panel.originalPos;

        float elapsedTime = 0;
        while (elapsedTime < flyInTime)
        {
            panel.rectTransform.anchoredPosition = Vector2.Lerp(A, B, elapsedTime / flyInTime);
            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();

        }
        isTransitionInProcess = false;
    }

    private IEnumerator FlyAwayRight(CallablePanel panel, float flyAwayTime)
    {
        isTransitionInProcess = true;
        GameObject panelGO = panel.panel;

        Vector2 A = panel.originalPos;
        Vector2 B = GetPositionByAnchorForLeftorRight(panel, panel.rectTransform, TransitionSide.right);

        float elapsedTime = 0;
        while (elapsedTime < flyAwayTime)
        {
            panel.rectTransform.anchoredPosition = Vector2.Lerp(A, B, elapsedTime / flyAwayTime);
            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();

        }
        isTransitionInProcess = false;
        panel.canvasGroup.alpha = 0f;
        panelGO.SetActive(false);
    }

    private IEnumerator FlyInFromBottom(CallablePanel panel, float flyInTime)
    {
        isTransitionInProcess = true;
        panel.canvasGroup.alpha = 1f;

        Vector2 A = GetPositionByAnchorForToporBottom(panel, panel.rectTransform, TransitionSide.bottom);
        Vector2 B = panel.originalPos;

        float elapsedTime = 0;
        while (elapsedTime < flyInTime)
        {
            panel.rectTransform.anchoredPosition = Vector2.Lerp(A, B, elapsedTime / flyInTime);
            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();

        }
        isTransitionInProcess = false;
    }

    private IEnumerator FlyAwayBottom(CallablePanel panel, float flyAwayTime)
    {
        isTransitionInProcess = true;
        GameObject panelGO = panel.panel;
        panel.canvasGroup.alpha = 1f;

        Vector2 A = panel.originalPos;
        Vector2 B = GetPositionByAnchorForToporBottom(panel, panel.rectTransform, TransitionSide.bottom);

        float elapsedTime = 0;
        while (elapsedTime < flyAwayTime)
        {
            panel.rectTransform.anchoredPosition = Vector2.Lerp(A, B, elapsedTime / flyAwayTime);
            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();

        }
        isTransitionInProcess = false;
        panel.canvasGroup.alpha = 0f;
        panelGO.SetActive(false);
    }
    #endregion


}
