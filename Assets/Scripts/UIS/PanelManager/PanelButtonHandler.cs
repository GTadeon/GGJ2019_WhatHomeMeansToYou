using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// this script is used in combination with PanelManager.cs script. Attach this to button
/// after which click a panel gets displayed / hidden. Display /hide settings for each button are done
/// in PanelManager GO, in inspector, in which you can adjust them for each assigned panel.
/// </summary>
[Serializable]
public class PanelButtonHandler : MonoBehaviour, IPointerClickHandler
{
    //called when this button is clicked.
    public bool LeaveOnlyPanelWithTheseIdsVisibleAndHideRest;

    public int [] IdsOfPanelsThatShouldBeVisibleAndOthersHiddenOnClick;

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("kkk");
        if (!LeaveOnlyPanelWithTheseIdsVisibleAndHideRest)
        {
            PanelManager.HandleButtonClick(this.gameObject);
        }
        else
        {
            PanelManager.HandleButtonClick(IdsOfPanelsThatShouldBeVisibleAndOthersHiddenOnClick);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
    }

    public void OnPointerUp(PointerEventData eventData)
    {
    }

}
