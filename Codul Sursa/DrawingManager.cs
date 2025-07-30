using System.Collections;
using UnityEngine;

public class DrawingManager : MonoBehaviour
{
    // Variabile locale

    private Animator anim;

    // Initializare
    private void Start()
    {
        anim = GetComponent<Animator>();
    }

    // Mergem catre panza
    public void OpenDrawing()
    {
        anim.enabled = true;
        anim.Play("OpenDrawing", 0, 0f);
    }

    // Plecam dinspre panza
    public void CloseDrawing()
    {
        anim.Play("CloseDrawing", 0, 0f);
        StartCoroutine(OnCloseDrawing());
    }

    IEnumerator OnCloseDrawing()
    {
        yield return AnimationHandler.WaitForStateEnd(anim, "CloseDrawing");
    }
}
