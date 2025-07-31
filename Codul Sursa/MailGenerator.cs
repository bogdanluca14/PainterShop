using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MailGenerator : MonoBehaviour
{
    // Variabile globale

    public Image displayImage;
    public Text phraseText;
    public Text emailText;
    public List<Sprite> images = new List<Sprite>();

    // Variabile locale

    private int crt = 0;

    private string[] phrases =
    {
        "Salut! Ai putea sa imi faci un portret frumos? Uite o poza cu mine:",
        "Buna ziua! M-ar ajuta mult un portret desenat dupa aceasta fotografie:",
        "Hey! Poti sa creezi un desen artistic bazat pe imaginea asta cu mine?",
        "Salutare! Imi doresc un portret personalizat dupa poza de mai jos:",
        "Buna! Ai timp sa faci un desen frumos dupa aceasta imagine cu mine?",
        "Hei acolo! Poti transforma fotografia asta intr-un portret desenat manual?",
        "Salut prietene! Ma ajuti cu un portret artistic bazat pe poza asta?",
        "Buna seara! Vreau un desen personalizat dupa aceasta fotografie cu mine:",
        "Hey! Imi poti face un portret deosebit folosind imaginea de mai jos?",
        "Salutari! Doresc sa transform poza asta intr-un portret desenat frumos:",
        "Buna dimineata! Poti crea un desen artistic din aceasta fotografie cu mine?",
        "Hei! Ma ajuti sa obtin un portret manual dupa imaginea asta personala?",
        "Salut dragul meu! Imi faci un desen frumos bazat pe poza de aici?",
        "Buna ziua domnule! Vreau un portret personalizat dupa aceasta imagine cu mine:",
        "Hey acolo! Poti transforma fotografia asta intr-un portret desenat special?",
        "Salutare frumoasa! Imi doresc un desen artistic dupa poza asta cu mine:",
        "Buna! Ma ajuti cu un portret manual bazat pe aceasta fotografie personala?",
        "Hei prietene! Poti face un desen deosebit din imaginea de mai jos?",
        "Salut! Vreau sa transform poza asta intr-un portret desenat cu talent:",
        "Buna seara! Imi poti crea un portret frumos dupa aceasta imagine cu mine?",
    };

    private string[] mails =
    {
        "maria.popescu@gmail.com",
        "ion.ionescu@yahoo.ro",
        "george.popa@yahoo.ro",
        "alex.dumitru@gmail.com",
        "ana.georgescu@outlook.com",
        "elena.radu@yahoo.com",
        "mihai.stan@gmail.ro",
        "cristina.marin@hotmail.com",
        "diana.tudor@gmail.com",
        "adrian.stoica@outlook.ro",
        "larisa.nicu@yahoo.com",
        "bogdan.matei@gmail.com",
        "andreea.dobre@hotmail.ro",
        "catalin.ungur@yahoo.ro",
        "roxana.Filip@gmail.com",
        "daniel.cretu@outlook.com",
        "simona.barbu@yahoo.com",
        "razvan.luca@gmail.ro",
        "bianca.neagu@hotmail.com",
        "florin.badea@yahoo.ro",
    };

    // Procesarea si updatarea informatiilor
    public void ProcessInfo(Sprite image, string phrase, string email)
    {
        if (displayImage != null)
            displayImage.sprite = image;

        if (phraseText != null)
            phraseText.text = phrase;

        if (emailText != null)
            emailText.text = email;
    }

    // Afisarea informatiilor curente
    private void DisplayCurrentContent()
    {
        if (images.Count == 0)
            return;

        int imageIndex = crt % images.Count;
        int phraseIndex = crt % phrases.Length;
        int emailIndex = crt % mails.Length;

        ProcessInfo(images[imageIndex], phrases[phraseIndex], mails[emailIndex]);
    }

    // Informatiile urmatoare
    public void NextInfo()
    {
        if (images.Count == 0)
            return;

        crt = (crt + 1) % Mathf.Max(images.Count, phrases.Length);
        DisplayCurrentContent();
    }

    // Informatiile anterioare
    public void PreviousInfo()
    {
        if (images.Count == 0)
            return;

        crt--;
        if (crt < 0)
            crt = Mathf.Max(images.Count, phrases.Length) - 1;

        DisplayCurrentContent();
    }

    public void AutoCycleImages(float interval = 3f)
    {
        InvokeRepeating(nameof(NextInfo), interval, interval);
    }

    public void StopAutoCycle()
    {
        CancelInvoke(nameof(NextInfo));
    }
}
