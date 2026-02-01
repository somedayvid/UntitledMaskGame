using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TutorialScript : MonoBehaviour
{
    [SerializeField] Sprite[] tutorialSlides;
    [SerializeField] GameObject canvas;
    [SerializeField] UnityEngine.UI.Image slideImage;
    [SerializeField] GameObject nextButton;
    [SerializeField] GameObject prevButton;

    private int currentSlideIndex = 0;

    //Start is called before the first frame update
    void Start()
    {
        currentSlideIndex = 0;
        slideImage.sprite = tutorialSlides[currentSlideIndex];
        nextButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(NextButton);
        prevButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(PreviousButton);


        RefreshUI();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void NextButton()
    {

        if (currentSlideIndex == tutorialSlides.Length - 1)
        {
            GameManager.instance.StartGame();
            return;
        }

        currentSlideIndex++;
        currentSlideIndex = Mathf.Clamp(currentSlideIndex, 0, tutorialSlides.Length - 1);
        RefreshUI();

    }

    void PreviousButton()
    {
        if (currentSlideIndex == 0)
        {
            GameManager.instance.MainMenu();
            return;
        }

        currentSlideIndex--;
        currentSlideIndex = Mathf.Clamp(currentSlideIndex, 0, tutorialSlides.Length - 1);
        RefreshUI();

    }

    private void RefreshUI()
    {
        // Update slide
        slideImage.sprite = tutorialSlides[currentSlideIndex];

        // Update button text
        nextButton.GetComponentInChildren<TextMeshProUGUI>().text =
            currentSlideIndex == tutorialSlides.Length - 1 ? "Start Game" : "Next";

        prevButton.GetComponentInChildren<TextMeshProUGUI>().text =
            currentSlideIndex == 0 ? "Main Menu" : "Previous";
    }


    

}
