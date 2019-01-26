using System.Collections;
using UnityEngine;

public class GGJGameMananager : MonoBehaviour
{
    public const float WaitBeforeDisplayingFirstDialog=1.5f;
    public const string DialogStopDelimiter = "$";


    public static bool CanControlPlayer = true;
    public static bool CanShowDialog = false;
    private GGJGameMananager _GGJGameMananager;


    private void Awake()
    {
        this._GGJGameMananager = GetComponent<GGJGameMananager>();
        StartCoroutine(StarWaitBeforeDisplayingFirstDialogTime());
    }

    public IEnumerator StarWaitBeforeDisplayingFirstDialogTime()
    {
        CanShowDialog = false;
        yield return new WaitForSeconds(WaitBeforeDisplayingFirstDialog);
        CanShowDialog = true;
        DisplayNextMessage();
    }


    public static void DisplayNextMessage()
    {
        ScenarioManager.DisplayNextDialogProcedure();
    }


    public static void DisablePlayerControls()
    {
        CanControlPlayer = false;
    }

    public static void EnablePlayerControls()
    {
        CanControlPlayer = true;
    }

}
