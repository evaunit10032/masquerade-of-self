using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public BR_PlayerController playerController;

    [SerializeField] AudioSource ambientAudio;

    [SerializeField] AudioSource combatAudio;

    [SerializeField] AudioSource speechAudio;

    [SerializeField] AudioClip introSpeech;

    [SerializeField] AudioClip endSpeech;

    [SerializeField] List<AudioClip> introAudio;

    [SerializeField] List<string> introText;

    [SerializeField] List<Sprite> introImages;

    [SerializeField] List<AudioClip> endingAudio;

    [SerializeField] List<string> endingText;

    [SerializeField] List<Sprite> endingImages;

    [SerializeField] List<string> objectives = new List<string>();

    public int questStep;

    public One_bit_ui ui;

    // Start is called before the first frame update
    void Start()
    {

        questStep = -1;
        Instance = this;
        //if(PlayIntro() == null)
        //{
        StartCoroutine(PlayIntro());
        //}

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void quest_progress()
    {
        questStep++;
        ui.set_objective_text(objectives[questStep]);
    }

    public void EndGame()
    {
        //playerController.MakeMeDead();
        StartCoroutine(EndGameEnum());
    }

    private IEnumerator EndGameEnum()
    {
        combatAudio.Stop();
        combatAudio.volume = 0f;
        ambientAudio.volume = 0.4f;
        ambientAudio.Play();
        ui.hide_skip_button();
        ui.start_end_screen();
        Cursor.lockState = CursorLockMode.Confined;
        for (int i = 0; i < endingAudio.Count; i++)
        {
            speechAudio.PlayOneShot(endingAudio[i]);
            ui.set_intro_text(endingText[i], endingImages[i]);
            yield return new WaitForSecondsRealtime(endingAudio[i].length - 0.1f);
        }
        //ui.set_intro_text("", null);
        ui.start_credits_screen();
        yield return new WaitForSeconds(60f);
        Application.Quit();

    }

    private IEnumerator PlayIntro()
    {

        ambientAudio.volume = 0f;
        for (int i = 0; i < introAudio.Count; i++)
        {
            speechAudio.PlayOneShot(introAudio[i]);
            ui.set_intro_text(introText[i], introImages[i]);
            yield return new WaitForSecondsRealtime(introAudio[i].length - 0.1f);
        }
        ui.set_intro_text("", null);
        ambientAudio.volume = 0.4f;
    }

    public void SwitchMusic(bool inCombat)
    {
        if (inCombat)
        {
            ambientAudio.Stop();
            combatAudio.volume = 0.2f;
            ambientAudio.volume = 0f;

            combatAudio.Play();
        }
        else
        {
            combatAudio.Stop();
            combatAudio.volume = 0f;
            ambientAudio.volume = 0.4f;
            ambientAudio.Play();
        }
    }

    public void SkipIntro()
    {
        Debug.Log("skipping");
        //StopCoroutine(PlayIntro());
        StopAllCoroutines();
        speechAudio.Stop();
        ui.set_intro_text("", null);
        ambientAudio.volume = 0.4f;
    }

    public void PlayerHit()
    {
        ui.player_hit();
    }


    public void play_dialouge(List<AudioClip> audio, List<string> text)
    {
        StartCoroutine(Dialogue(audio, text));
    }

    private IEnumerator Dialogue(List<AudioClip> audio, List<string> text)
    {
        ui.set_objective_text("");
        for (int i = 0; i < audio.Count; i++)
        {
            speechAudio.PlayOneShot(audio[i]);
            ui.SetDialogueText(text[i]);
            yield return new WaitForSecondsRealtime(audio[i].length);
        }
        quest_progress();
        ui.SetDialogueText("");
    }
}
