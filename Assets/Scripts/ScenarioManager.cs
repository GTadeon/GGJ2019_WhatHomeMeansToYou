using UnityEngine;
using UnityEngine.UI;

public class ScenarioManager : MonoBehaviour
{
    private static int _nextDialogToDisplayIndex;

    [Tooltip("$=pause from displaying another one !")]
    public string[] DialogsOrdered;

    public int DialogPanelId;

    public Text DialogTxt;

    private static ScenarioManager _scenarioManager;

    private void Awake()
    {
        _nextDialogToDisplayIndex = 0;
        _scenarioManager = GetComponent<ScenarioManager>();
    }

    //called from inspector on ok button click!
    public void DisplayNext()
    {
        DisplayNextDialogProcedure();
    }

    public static void DisplayNextDialogProcedure()
    {
        if (_scenarioManager.DialogsOrdered.Length == _nextDialogToDisplayIndex)
        {
            PanelManager.HidePanelWithId(_scenarioManager.DialogPanelId);
            return;
        }
        if (_scenarioManager.DialogsOrdered[_nextDialogToDisplayIndex] != GGJGameMananager.DialogStopDelimiter)
        {
            _scenarioManager.DialogTxt.text = _scenarioManager.DialogsOrdered[_nextDialogToDisplayIndex];
            PanelManager.ShowPanelWithId(_scenarioManager.DialogPanelId);
        }
        else
        {
            PanelManager.HidePanelWithId(_scenarioManager.DialogPanelId);
        }
        _nextDialogToDisplayIndex++;
    }

}
