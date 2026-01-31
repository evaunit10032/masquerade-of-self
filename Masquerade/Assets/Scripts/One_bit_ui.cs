using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class One_bit_ui : MonoBehaviour
{
    [SerializeField] GameObject lamp;

    [SerializeField] GameObject lampMaskHolder;

    [SerializeField] RectTransform lampMask;

    [SerializeField] GameObject litRender;

    [SerializeField] Transform lampMaskPos;

    [SerializeField] TextMeshProUGUI apCounter;

    [SerializeField] TextMeshProUGUI loadedAmmoCounter;

    [SerializeField] TextMeshProUGUI storedAmmoCounter;

    [SerializeField] TextMeshProUGUI hpCounter;

    [SerializeField] Slider lampOilGauge;

    [SerializeField] Slider dashGauge;

    [SerializeField] Slider bulletTimeGauge;

    [SerializeField] GameObject UIPanel;

    [SerializeField] GameObject deathPanel;

    [SerializeField] TextMeshProUGUI dialogueText;

    [SerializeField] GameObject interactable_popup;

    [SerializeField] GameObject radioPopup;

    BR_PlayerController playerController;

    public static One_bit_ui instance;

    OneBitInteractable saved_interactable;

    [SerializeField] TextMeshProUGUI bulletReadout;

    [SerializeField] GameObject intro_Screen;

    [SerializeField] GameObject end_screen;

    [SerializeField] GameObject credits_screen;

    [SerializeField] TextMeshProUGUI intro_text;

    [SerializeField] Image intro_image;

    [SerializeField] GameObject skip_button;

    [SerializeField] List<GameObject> weapon_icons = new List<GameObject>();

    [SerializeField] Animator blood_splat;

    [SerializeField] TextMeshProUGUI objective_text;

    List<int> bulletReadouts = new List<int>();

    float readoutLifetime;

    float lampOil;

    int lampState;

    bool dead;

    public void set_objective_text(string input_text)
    {
        objective_text.SetText(input_text);

    }

    public void set_intro_text(string input_text, Sprite input_image)
    {
        if (input_text != "")
        {
            intro_Screen.SetActive(true);
            intro_text.SetText(input_text);
            intro_image.sprite = input_image;
        }
        else
        {
            Debug.Log("Skipping");
            intro_Screen.SetActive(false);
            playerController.MakeNotDead();
        }
    }

    public void skip_intro()
    {
        GameManager.Instance.SkipIntro();
    }

    public void hide_skip_button()
    {
        skip_button.SetActive(false);
    }

    private void Awake()
    {
        instance = this;
        lampMaskPos = litRender.transform;
        UIEvents.RecievePlayerDeath += OpenDeathUI;
    }

    // Start is called before the first frame update
    void Start()
    {
        GameManager.Instance.ui = this;
        UIEvents.RecieveUpdateUI += UpdateUI;
        UIEvents.RecieveInteractableCall += RecieveInteractable;
        UIEvents.RecieveBulletReadout += RecieveBulletReadout;
        playerController = GameObject.Find("BRPlayerController").GetComponent<BR_PlayerController>();
        lamp = GameObject.Find("Lamp");
        lampOil = 3000f;
    }

    private void OnDestroy()
    {
        UIEvents.RecieveUpdateUI -= UpdateUI;
        UIEvents.RecievePlayerDeath -= OpenDeathUI;
        UIEvents.RecieveInteractableCall -= RecieveInteractable;
        UIEvents.RecieveBulletReadout -= RecieveBulletReadout;
    }

    // Update is called once per frame
    private void Update()
    {
        CheckReadoutLifetimes();
        if (saved_interactable != null)
        {
            Debug.Log("Popup");
            interactable_popup.SetActive(true);
            if (Input.GetKeyDown(KeyCode.E))
            {
                saved_interactable.Interact();
            }
        }
        else if (interactable_popup.activeInHierarchy)
        {
            interactable_popup.SetActive(false);
        }
        if (dead)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                dead = false;
                Time.timeScale = 1;

                deathPanel.SetActive(false);
                UIPanel.SetActive(true);
                playerController.Die();

            }
        }
        else
        {
            /*litRender.transform.SetParent(null, true);
            lampMaskHolder.transform.position = Camera.main.WorldToScreenPoint(lamp.transform.position);
            litRender.transform.SetParent(lampMask);*/
            //litRender.transform.position = lampMaskPos.transform.position;

            /*switch (lampState)
            {
                case 0:
                    lampOil -= 1 * Time.fixedDeltaTime;
                    break;
                case 1:
                    lampOil -= 3 * Time.fixedDeltaTime;
                    break;
            }*/
            lampOilGauge.value = playerController.get_lamp_oil().x / playerController.get_lamp_oil().y;
            if (playerController.get_lamp_oil().x <= 0)
            {
                litRender.SetActive(false);
            }
            else if (!litRender.activeInHierarchy)
            {
                litRender.SetActive(true);
            }

            bulletTimeGauge.value = playerController.GetBulletTimeStamina();
            dashGauge.value = playerController.GetDashStamina();
        }


    }

    public void player_hit()
    {
        blood_splat.SetTrigger("DMG");
    }

    public void UpdateUI(int health, int loadedAmmo, int storedAmmo, int armor)
    {
        loadedAmmoCounter.SetText(loadedAmmo.ToString() + "/");
        storedAmmoCounter.SetText(storedAmmo.ToString());
        int emptyNums = 3 - health.ToString().Length;
        string healthText = health.ToString();
        for (int i = 0; i < emptyNums; i++)
        {
            healthText = "0" + healthText;
        }
        hpCounter.SetText(healthText);
        int apNums = 3 - armor.ToString().Length;
        string apText = armor.ToString();
        for (int i = 0; i < apNums; i++)
        {
            apText = "0" + apText;
        }
        apCounter.SetText(apText);
        for (int i = 0; i < playerController.GetActiveWeapons().Count; i++)
        {
            weapon_icons[i].SetActive(playerController.GetActiveWeapons()[i]);
        }
    }

    public void IntroScreen(bool show)
    {
        if (show)
        {
            intro_Screen.SetActive(true);
        }
        else
        {
            intro_Screen.SetActive(false);
            playerController.MakeNotDead();
        }
    }

    public void start_end_screen()
    {
        playerController.MakeMeDead();
    }

    public void start_credits_screen()
    {

        credits_screen.SetActive(true);
    }


    public void AdjustLampSize(int newLampState)
    {
        lampState = newLampState;
        switch (newLampState)
        {
            case 0:
                lampMask.sizeDelta = new Vector2(6979.303f, 2000f);
                break;
            case 1:
                lampMask.sizeDelta = new Vector2(6979.303f, 4000f);
                break;
            case 2:
                lampMask.sizeDelta = new Vector2(0, 0);
                break;
        }

    }

    public void RecieveInteractable(OneBitInteractable interactable)
    {
        saved_interactable = interactable;
    }

    public void SetDialogueText(string text)
    {
        dialogueText.SetText(text);
        if (text != "" && text.ToCharArray()[0] != '*')
        {
            radioPopup.SetActive(true);
        }
        else
        {
            radioPopup.SetActive(false);
        }
    }

    private void RecieveBulletReadout(string newReadout)
    {
        /* if(bulletReadouts.Count < 0)
         {
             readoutLifetime = Time.fixedTime + 2f;
         }
         bulletReadout.SetText(bulletReadout.text + "\n" + newReadout);
         bulletReadouts.Add(newReadout.Length);*/

    }

    private void CheckReadoutLifetimes()
    {
        readoutLifetime += Time.fixedDeltaTime;
        if (Time.fixedTime >= readoutLifetime && bulletReadouts.Count > 0)
        {
            readoutLifetime = Time.fixedTime + 2f;
            bulletReadout.SetText(bulletReadout.text.Substring(bulletReadouts[0] - 1));
            bulletReadouts.Remove(0);
        }
    }

    private void OpenDeathUI()
    {
        Time.timeScale = 0;
        UIPanel.SetActive(false);
        deathPanel.SetActive(true);
        dead = true;
    }

}
