using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Clasa pentru a retine informatii despre actiunea butonului

[System.Serializable]
public class ButtonAct
{
    public Button button;
    public UnityEvent action;
    public int buttonID = 0;
}

public class Interact : MonoBehaviour
{
    // Variabile publice

    public bool autoResetAll = true;
    private bool isProcessing = false;
    public float fadeOutSpd = 3f;
    public float fadeInSpd = 2f;
    public float fadeLim = 0.01f;
    public float resetDelay = 2f;
    public List<ButtonAct> buttonActions = new List<ButtonAct>();

    // Variabile locale

    private Dictionary<Button, CanvasGroup> btnCanvasGroups = new Dictionary<Button, CanvasGroup>();
    private Dictionary<Button, float> alphaOrig = new Dictionary<Button, float>();

    void Start()
    {
        InitializeButtons();
    }

    // Initializarea tuturor butoanelor
    private void InitializeButtons()
    {
        foreach (var buttonAction in buttonActions)
        {
            if (buttonAction.button != null)
            {
                CanvasGroup canvasGroup = buttonAction.button.GetComponent<CanvasGroup>();

                if (canvasGroup == null)
                {
                    canvasGroup = buttonAction.button.gameObject.AddComponent<CanvasGroup>();
                }

                btnCanvasGroups[buttonAction.button] = canvasGroup;
                alphaOrig[buttonAction.button] = canvasGroup.alpha;

                buttonAction.button.onClick.RemoveAllListeners();
                buttonAction.button.onClick.AddListener(() => OnButtonClicked(buttonAction));
            }
        }
    }

    // La apasarea unui buton din lista
    private void OnButtonClicked(ButtonAct clickedButtonAction)
    {
        if (isProcessing)
            return;

        StartCoroutine(HandleButtonClick(clickedButtonAction));
    }

    private IEnumerator HandleButtonClick(ButtonAct clickedButtonAction)
    {
        isProcessing = true;
        clickedButtonAction.action?.Invoke();

        yield return StartCoroutine(FadeOutAllButtons());

        if (autoResetAll)
        {
            yield return new WaitForSeconds(resetDelay);
            FadeInAllButtons();
        }

        isProcessing = false;
    }

    // Efect Fade Out asupra tuturor butoanelor
    private IEnumerator FadeOutAllButtons()
    {
        List<Coroutine> fadeCoroutines = new List<Coroutine>();

        foreach (var buttonAction in buttonActions)
            if (buttonAction.button != null && btnCanvasGroups.ContainsKey(buttonAction.button))
            {
                fadeCoroutines.Add(StartCoroutine(FadeButton(buttonAction.button, 0f)));
            }

        foreach (var coroutine in fadeCoroutines)
        {
            yield return coroutine;
        }
    }

    // Efect Fade In asupra tuturor butoanelor
    public void FadeInAllButtons()
    {
        foreach (var buttonAction in buttonActions)
            if (buttonAction.button != null && alphaOrig.ContainsKey(buttonAction.button))
            {
                StartCoroutine(FadeButton(buttonAction.button, alphaOrig[buttonAction.button]));
            }

        isProcessing = false;
    }

    // Fade buton specific
    private IEnumerator FadeButton(Button button, float targetAlpha)
    {
        if (!btnCanvasGroups.ContainsKey(button))
            yield break;

        CanvasGroup canvasGroup = btnCanvasGroups[button];
        float currentAlpha = canvasGroup.alpha;
        float fadeSpeed = targetAlpha > currentAlpha ? fadeInSpd : fadeOutSpd;

        if (targetAlpha <= fadeLim)
        {
            button.interactable = false;
        }

        while (Mathf.Abs(currentAlpha - targetAlpha) > fadeLim)
        {
            currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * fadeSpeed);
            canvasGroup.alpha = currentAlpha;
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;

        if (targetAlpha > fadeLim)
        {
            button.interactable = true;
        }
    }

    public void ResetAllButtons()
    {
        FadeInAllButtons();
    }

    public void DisableAllButtons()
    {
        foreach (var buttonAction in buttonActions)
        {
            if (buttonAction.button != null && btnCanvasGroups.ContainsKey(buttonAction.button))
            {
                buttonAction.button.interactable = false;
                btnCanvasGroups[buttonAction.button].alpha = 0f;
            }
        }
    }

    void OnDestroy()
    {
        foreach (var buttonAction in buttonActions)
        {
            if (buttonAction.button != null)
            {
                buttonAction.button.onClick.RemoveAllListeners();
            }
        }
    }
}
