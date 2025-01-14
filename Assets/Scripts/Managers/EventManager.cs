using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
public class EventManager : MonoBehaviour, IEventService
{
    [SerializeField] private CanvasGroup _choiceButtonsCanvas;
    [SerializeField] private Button _buttonA, _buttonB;
    [SerializeField] private CanvasGroup _wheelCanvas;
    [SerializeField] private DrawUI _drawUI;
    [SerializeField] private TextInputUI _textInputUI;
    [SerializeField] private RouletteUI _rouletteUI;

    [SerializeField] private Image _rouletteChoiceSymbol;
    
    private IDialogueService _dialogueService;

    private Dictionary<Button, Action>_buttonActions;
    
    private int _currentResult;
    private int _currentSpin;

    [SerializeField] private GameObject _wheel;


    private bool hasDrawn;
    private bool hasSlogan;

    private void Start()
    {
        _dialogueService = GameManager.Instance.Get<IDialogueService>();

        _buttonActions = new()
        {
            { _buttonA, null },
            { _buttonB, null }
        };
        
        HideChoiceButtons();

        hasDrawn = hasSlogan = false;
    }

    public void StartEvent(IGameEvent e, IBuilding building)
    {
        GameManager.Instance.CurrentGameState = GameManager.GameState.OnEvent;
        print("evento");
        switch (e)
        {
            case ChoiceEvent choiceEvent:
                _rouletteChoiceSymbol.enabled = false;
                _dialogueService.SendDialogue(choiceEvent.StartDialogue, false, ShowChoiceButtons);
                SetChoiceToButton(_buttonA, choiceEvent.ChoiceTextA, choiceEvent.EndDialogueA, 
                    () => ResolveOutcomes(choiceEvent.OutcomesA, building));
                SetChoiceToButton(_buttonB, choiceEvent.ChoiceTextB, choiceEvent.EndDialogueB, 
                    () => ResolveOutcomes(choiceEvent.OutcomesB, building));
                
                break;
            
            case PassiveEvent passiveEvent:
                
                _dialogueService.SendDialogue(passiveEvent.StartDialogue, true, () => ResolveOutcomes(passiveEvent.Outcomes, building));
                
                break;

            case RouletteEvent rouletteEvent:
                _rouletteChoiceSymbol.enabled = true;

                _dialogueService.SendDialogue(rouletteEvent.StartDialogue, false, ShowChoiceButtons);
                SetRouletteToButton(_buttonA, rouletteEvent, building);
                SetChoiceToButton(_buttonB, rouletteEvent.ChoiceTextRefuse, rouletteEvent.EndDialogueRefuse, 
                    () => ResolveOutcomes(rouletteEvent.OutcomesRefuse, building));
              
                break;
            
            case DrawEvent drawEvent: //joder como me pone la programacion asincrona joder que bonito y poco mantenible
                
                _dialogueService.SendDialogue(drawEvent.StartDialogue, true, () =>
                {
                    _drawUI.Display(() =>
                    {
                        if (!hasDrawn)
                        {
                            hasDrawn = true;
                            if (hasSlogan)
                            {
                                _dialogueService.SendDialogue(GameManager.Instance.GameInfo.TutorialDialogue, true, () =>
                                {
                                    GameManager.Instance.Get<IEventSpawnService>().StartSpawn();
                                });
                            }
                        }
                        GameManager.Instance.CurrentGameState = GameManager.GameState.OnPlay;

                    });
                });
                
                break;
            
            case ChooseNameEvent chooseNameEvent:
                
                _dialogueService.SendDialogue(chooseNameEvent.StartDialogue, true, () =>
                {
                    _textInputUI.Display(chooseNameEvent.RequestPhrase, chooseNameEvent.CharLimit, (chosenName) =>
                    {
                        GameManager.Instance.GameInfo.OrgName = chosenName;
                        GameManager.Instance.CurrentGameState = GameManager.GameState.OnPlay;
                        GameManager.Instance.Get<IEventSpawnService>().SpawnSetUpEvents();
                        GameManager.Instance.Get<IHUDService>().ShowHUD();
                        GameManager.Instance.Get<IPeopleService>()
                            .AddPeople(GameManager.Instance.GameInfo.InitialPeople);
                    });
                });
                
                break;
            
            case ChooseSloganEvent chooseSloganEvent:
                
                _dialogueService.SendDialogue(chooseSloganEvent.StartDialogue, true, () =>
                {
                    _textInputUI.Display(chooseSloganEvent.RequestPhrase, chooseSloganEvent.CharLimit, (chosenSlogan) =>
                    {
                        if (!hasSlogan)
                        {
                            hasSlogan = true;
                            if (hasDrawn)
                            {
                                _dialogueService.SendDialogue(GameManager.Instance.GameInfo.TutorialDialogue, true, () =>
                                {
                                    
                                    GameManager.Instance.Get<IEventSpawnService>().StartSpawn();
                                });
                            }
                        }
                        GameManager.Instance.GameInfo.OrgSlogan = chosenSlogan;
                        GameManager.Instance.CurrentGameState = GameManager.GameState.OnPlay;
                    });
                });
                
                break;
        }
    }

    private void SetChoiceToButton(Button button, string choice, Dialogue dialogue, Action onDialogueFinish)
    {
        button.GetComponentInChildren<TextMeshProUGUI>().text = choice;
        _buttonActions[button] = () =>
        {
            _dialogueService.SendDialogue(dialogue, true, onDialogueFinish);
        };
    }

    private void SetRouletteToButton(Button button, RouletteEvent rEvent, IBuilding building)
    {
        button.GetComponentInChildren<TextMeshProUGUI>().text = rEvent.ChoiceTextAccept;
        _buttonActions[button] = () =>
        {
            _dialogueService.Hide();
            _rouletteUI.Display(rEvent, (result) => OnRouletteResolved(rEvent, result, building));
        };
    }

    private void OnRouletteResolved(RouletteEvent rouletteEvent, RouletteUI.Result result, IBuilding building)
    {
        (Dialogue dialogue, Outcomes outcomes) = result switch
        {
            RouletteUI.Result.Crit => (rouletteEvent.EndDialogueCrit, rouletteEvent.OutcomesCrit),
            RouletteUI.Result.Success => (rouletteEvent.EndDialogueWin, rouletteEvent.OutcomesWin),
            RouletteUI.Result.Fail => (rouletteEvent.EndDialogueLose, rouletteEvent.OutcomesLose),
            _ => (null, null)
        };
        _dialogueService.SendDialogue(dialogue, true, () => ResolveOutcomes(outcomes, building));
    }

    public void AButtonClick()
    {
        AudioManager.Instance.PlayClick1();
        HideChoiceButtons();
        _buttonActions[_buttonA]();
    }

    public void BButtonClick()
    {
        AudioManager.Instance.PlayClick1();
        HideChoiceButtons();
        _buttonActions[_buttonB]();
    }
    
    private void ShowChoiceButtons()
    {
        _choiceButtonsCanvas.alpha = 1;
        _choiceButtonsCanvas.blocksRaycasts = true;
    }
    
    private void HideChoiceButtons()
    {
        _choiceButtonsCanvas.alpha = 0;
        _choiceButtonsCanvas.blocksRaycasts = false;
    }
    
    private void ResolveOutcomes(Outcomes outcomes, IBuilding building)
    {
        HideChoiceButtons();

        if (outcomes.IsWallSuccess)
        {
            GameManager.Instance.TearDownWall();
            return;
        }
        
        var outcomeList = outcomes.Get();
        if (outcomeList is null || !outcomeList.Any())
        {
            GameManager.Instance.CurrentGameState = GameManager.GameState.OnPlay;
            return;
        }
        
        string text = "";
        foreach(var outcome in outcomeList)
        {
            //outcome.Execute(building);
            if (outcome.DisplayText != "") text += $"{outcome.DisplayText}\n";
        }
        if(text != "") _dialogueService.SendInfoText(text, () => {
            foreach (var outcome in outcomeList)
            {
                outcome.Execute(building);
            }
            GameManager.Instance.CurrentGameState = GameManager.GameState.OnPlay;
            });
        else GameManager.Instance.CurrentGameState = GameManager.GameState.OnPlay;
    }
    
}
