using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DrawingManager : MonoBehaviour
{
    private Animator anim;

    private void Start()
    {
        anim=GetComponent<Animator>();
    }

    public void OpenDrawing()
    {
        anim.enabled = true;
        anim.Play("OpenDrawing", 0, 0f);
        
    }

    public void CloseDrawing()
    {
        anim.Play("CloseDrawing", 0, 0f);
        StartCoroutine(OnCloseDrawing());
    }

    IEnumerator OnCloseDrawing()
    {
        yield return AnimationHandler.WaitForStateEnd(anim, "CloseDrawing");
        PlayerMovement.canInteract = true;
    }
}
