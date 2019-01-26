using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Collections.Generic;

[CustomEditor (typeof( PanelManager ))]
[CanEditMultipleObjects]
public class PanelManagerEditor: Editor
{

    private Dictionary<string, SerializedProperty> _properties = new Dictionary<string, SerializedProperty>();
    private List<Property> _timingProperties = new List<Property>();

    private class Property
    {
        public string name;
        public string text;

        public Property(string n, string t)
        {
            name = n;
            text = t;
        }
    }


    private static bool _showGeneral;
    private static bool _showCallKeys;

    private PanelManager panelManager;

    #region Properties

    //props for general section:
    private readonly Property CANVAS = new Property("Canvas", "Canvas");
    private readonly Property PANELS = new Property("panels", "Panels");
    private readonly Property CURSORTEXTURECHANGE = new Property("cursorChangeOnHover", "Cursor texture change");
    private readonly Property CURSORTEXTURE = new Property("cursorOverPanelTexture", "cursor texture");
    private readonly Property HIDEALLONSTART = new Property("hideAllOnStart", "Hide all on start");
    private readonly Property PAUSEONSHOWALL = new Property("pauseGameOnShowAll", "Pause on show all");



    //props for call keys section:
    private readonly Property SETKEYCALLS = new Property("setKeyCalls", "Set key calls");
    private readonly Property PANELKEYS = new Property("showPanelIndividuallyKeys", "Individual calls");
    private readonly Property SHOWALLKEY = new Property("showAllPanelsKey", "show all");



    #endregion



    private bool hasCreatedIndividualPanelKeyHolders = false;


    private void OnEnable()
    {
        panelManager = (PanelManager)target; //need this for button
        _properties.Clear();
        SerializedProperty property = serializedObject.GetIterator();

        while (property.NextVisible(true))
        {
            _properties[property.name] = property.Copy();
        }

    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        _timingProperties.Clear();

        GUIStyle boldStyle = new GUIStyle();
        boldStyle.fontStyle = FontStyle.Bold;

        EditorGUILayout.Separator();

        _showGeneral = EditorGUILayout.Foldout(_showGeneral, "General");

        if (_showGeneral)
        {
            DisplayRegularField(CANVAS);

            EditorGUILayout.Separator();

            DisplayRegularField(CURSORTEXTURECHANGE);

            if (_properties[CURSORTEXTURECHANGE.name].hasMultipleDifferentValues || _properties[CURSORTEXTURECHANGE.name].boolValue == true)
            {
                DisplayRegularField(CURSORTEXTURE);
            }

            DisplayRegularField(HIDEALLONSTART);
            DisplayRegularField(PAUSEONSHOWALL);

            DisplayRegularField(PANELS);
            EditorGUILayout.Separator();
            // kad deserijalizira, onda je show button isti kao i hide button (na neku foru...) , pa za sad nek ostane zakomentiran ovaj dio editora dok to ne skuzim
            //ShowDisplayButtonsForPanels();


            EditorGUILayout.Separator();
            EditorGUILayout.Separator();

        }

        _showCallKeys = EditorGUILayout.Foldout(_showCallKeys, "Call keys");

        if (_showCallKeys)
        {
            DisplayRegularField(SHOWALLKEY);
            DisplayRegularField(SETKEYCALLS);

            if (_properties[SETKEYCALLS.name].hasMultipleDifferentValues || _properties[SETKEYCALLS.name].boolValue == true)
            {
                DisplayRegularField(PANELKEYS);
            }

            EditorGUILayout.Separator();
        }


        serializedObject.ApplyModifiedProperties();

        CheckConsistency();
    }

    /// <summary>
    /// shows hide /show panel buttons slots in inspector, for panels that have that option set.
    /// </summary>
    private void ShowDisplayButtonsForPanels()
    {
        for (int i = 0; i < panelManager.panels.Length; i++)
        {
            if (panelManager.panels[i].callOnButtonPress)
            {
                string showButtonLabel = panelManager.panels[i].panel.name + " show panel button";
                string hideButtonLabel = panelManager.panels[i].panel.name + " hide panel button";

                panelManager.panels[i].showPanelButton = (PanelButtonHandler) EditorGUILayout.ObjectField(showButtonLabel, panelManager.panels[i].showPanelButton, typeof(PanelButtonHandler), allowSceneObjects: true);
                panelManager.panels[i].hidePanelButton = (PanelButtonHandler) EditorGUILayout.ObjectField(hideButtonLabel, panelManager.panels[i].hidePanelButton, typeof(PanelButtonHandler), allowSceneObjects: true);
            }
        }
    }



    private void DisplayRegularField(Property property)
    {
        EditorGUILayout.PropertyField(_properties[property.name], new GUIContent(property.text), true);
    }


    /// <summary>
    /// check if panel manager is consistent 
    /// </summary>
    private void CheckConsistency()
    {

        if (panelManager.setKeyCalls && (!hasCreatedIndividualPanelKeyHolders || panelManager.showPanelIndividuallyKeys.Count != panelManager.panels.Length))
        {
            if (panelManager.showPanelIndividuallyKeys.Count < panelManager.panels.Length)
                FillKeyList(panelManager);
            else
                RemoveKeyExcess(panelManager);
        }
        else if (!panelManager.setKeyCalls && (hasCreatedIndividualPanelKeyHolders || panelManager.showPanelIndividuallyKeys.Count > 0))
            ClearKeyList(panelManager);

        CheckForConstantlyVisible(panelManager);

        CanvasScaler canvasScalerOfAssignedCanvas = panelManager.Canvas.gameObject.GetComponent<CanvasScaler>();
        if (CheckWheterAppropriateObjectHasBeenAssignedAsCanvas(panelManager))
        {
            if (panelManager.Canvas != null && canvasScalerOfAssignedCanvas == null)
                AssignCanvasScalerComponentToCanvas(panelManager);
            else if (canvasScalerOfAssignedCanvas != null && canvasScalerOfAssignedCanvas.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
                SetCanvasScalerScaleModeToScaleWithScreenSize(canvasScalerOfAssignedCanvas);
        }
    }




    /// <summary>
    /// thanks two this 2 belowed defined methods, appear and disappear effects are set to "no animation" if constantlyVisible is checked for that panel
    /// </summary>
    private void CheckForConstantlyVisible(PanelManager panelManager)
    {
        for (int i = 0; i < panelManager.panels.Length; i++)
        {
            if (panelManager.panels[i].constantlyVisible)
            {
                SetTransitionEffectToNone(panelManager.panels[i]);
            }
        }
    }

    private void SetTransitionEffectToNone(PanelManager.CallablePanel panel)
    {
        panel.appearTransition = PanelManager.AppearTransition.noAnimation;
        panel.disappearTransition = PanelManager.DisappearTransition.noAnimation;
    }



    #region ------------------------Other methods----------------------------------

    /// <summary>
    /// first clears the list, prepares it, and then fills it
    /// </summary>
    private void FillKeyList(PanelManager panelManager)
    {

        for (int i = 0; i < panelManager.panels.Length; i++)
        {
            panelManager.showPanelIndividuallyKeys.Add(new KeyCode());
        }
        hasCreatedIndividualPanelKeyHolders = true;
    }


    private void ClearKeyList(PanelManager panelManager)
    {
        panelManager.showPanelIndividuallyKeys.Clear();
        hasCreatedIndividualPanelKeyHolders = false;
    }

    private void RemoveKeyExcess(PanelManager panelManager)
    {
        int currentSizeOfList = panelManager.showPanelIndividuallyKeys.Count;

        //removing the last from the list:
        for (int i = 0; i < Mathf.Abs(currentSizeOfList - panelManager.panels.Length); i++)
        {
            panelManager.showPanelIndividuallyKeys.Remove(panelManager.showPanelIndividuallyKeys[panelManager.showPanelIndividuallyKeys.Count - 1]);
        }
    }

    private void AssignCanvasScalerComponentToCanvas(PanelManager panelManager)
    {
        panelManager.Canvas.AddComponent<CanvasScaler>();
        Debug.Log("<i> CanvasScaler compnent has been added to your Canvas game object ! Why, you may ask? Because we need that for proper displaying of tooltips ! :)  </i>");
    }


    private void SetCanvasScalerScaleModeToScaleWithScreenSize(CanvasScaler canvasScalerOfAssignedCanvas)
    {
        canvasScalerOfAssignedCanvas.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        Debug.Log("<i> UI scale mode of CanvasScaler compnent of Canvas game object that you assigned on panel manager game object has been set to ScaleWithScreenSize . This specific scale mode is needed for proper showing of tooltip panel in our game. :) </i>");
    }

    /// <summary>
    /// checks whether the asigned canvas game object is really a canvas. It is really a canvas if it has Canvas component assigned to it.
    /// </summary>
    private bool CheckWheterAppropriateObjectHasBeenAssignedAsCanvas(PanelManager panelManager)
    {
        if (panelManager.Canvas.GetComponent<Canvas>() == null)
        {
            panelManager.Canvas = null;
            throw new System.ArgumentException("object that you attached on Canvas slot on PanelManager game object under 'Canvas' has no component Canvas attached. In another words, that's not Canvas bro, so it has been removed from there. Maybe you really wanted to add Canvas to that slot but accidently assigned wrong object instead?  Anyway, I need canvas here on Canvas slot on panel manager game object. :) ");
        }
        return true;
    }

    #endregion




}
