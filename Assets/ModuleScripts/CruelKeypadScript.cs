﻿using System.Collections.Generic;
using UnityEngine;
using KModkit;
using rnd = UnityEngine.Random;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections;

public class CruelKeypadScript : MonoBehaviour
{
    public KMAudio Audio;
    public KMBombModule Module;
    public KMBombInfo Info;

    public KMSelectable[] Buttons;
    public SpriteRenderer[] SpriteRenderers;
    public Sprite[] Sprites;
    public TextMesh StageText;
    public MeshRenderer StripRenderer;
    public Material[] StripColors;
    public Material BlackMat;
    public GameObject[] Lights;
    public GameObject[] ButtonObjects;
    public GameObject[] ButtonHighlites;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool _isSolved = false;

    private int stage = 1;

    private enum CruelKeypadsColor
    {
        Red = 0,
        Green = 1,
        Blue = 2,
        Yellow = 3,
        Magenta = 4,
        White = 5
    }
    private CruelKeypadsColor stripColor;
   
    private VennDiagram DiagramOutput;

    private static readonly List<char> Symbols = new List<char>() { 'ㄹ', 'ㅁ', 'ㅂ', 'ㄱ', 'ㄲ', 'ㄷ', 'ㅈ', 'ㅉ', 'ㅟ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅃ', 'ㅅ', 'ㅆ', 'ㅇ', 'ㅢ', 'ㄴ', 'ㄸ' };

    private static readonly List<char> OrderA = new List<char>() { 'ㅃ', 'ㄲ', 'ㄴ', 'ㄷ', 'ㄸ', 'ㄹ', 'ㅁ', 'ㅂ', 'ㄱ', 'ㅅ', 'ㅆ', 'ㅇ', 'ㅈ', 'ㅉ', 'ㅟ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅢ' };
    private static readonly List<char> OrderB = new List<char>() { 'ㅇ', 'ㅈ', 'ㅉ', 'ㅟ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅢ', 'ㄱ', 'ㄹ', 'ㅁ', 'ㅂ', 'ㅃ', 'ㅅ', 'ㅆ', 'ㄲ', 'ㄴ', 'ㄷ', 'ㄸ' };
    private static readonly List<char> OrderC = new List<char>() { 'ㄹ', 'ㅁ', 'ㅂ', 'ㄱ', 'ㄲ', 'ㄷ', 'ㅈ', 'ㅉ', 'ㅟ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅃ', 'ㅅ', 'ㅆ', 'ㅇ', 'ㅢ', 'ㄴ', 'ㄸ' };
    private static readonly List<char> OrderD = new List<char>() { 'ㄱ', 'ㅢ', 'ㄲ', 'ㅍ', 'ㄴ', 'ㅌ', 'ㄷ', 'ㅋ', 'ㄸ', 'ㅟ', 'ㄹ', 'ㅉ', 'ㅁ', 'ㅈ', 'ㅂ', 'ㅇ', 'ㅃ', 'ㅆ', 'ㅅ' };
    private static readonly List<char> OrderE = new List<char>() { 'ㄷ', 'ㄸ', 'ㄹ', 'ㅁ', 'ㅂ', 'ㅃ', 'ㅢ', 'ㅟ', 'ㅋ', 'ㄱ', 'ㄲ', 'ㄴ', 'ㅅ', 'ㅆ', 'ㅈ', 'ㅍ', 'ㅉ', 'ㅇ', 'ㅌ' };
    private static readonly List<char> OrderF = new List<char>() { 'ㅋ', 'ㄸ', 'ㄷ', 'ㅅ', 'ㅍ', 'ㅌ', 'ㅁ', 'ㄴ', 'ㅃ', 'ㅉ', 'ㄲ', 'ㅆ', 'ㅢ', 'ㅈ', 'ㅟ', 'ㅂ', 'ㄹ', 'ㄱ', 'ㅇ' };

    private IList<char> pickedSymbols = new List<char>();
    private IList<char> sortedSymbols = new List<char>();

    private List<int> circles = new List<int>();

    private List<char> PressedButtons = new List<char>();

    private IList<char> Stage1Symbols = new List<char>();
    private IList<char> Stage2Symbols = new List<char>();

    private readonly IList<IVennDiagram> Diagrams = new List<IVennDiagram>
    {
        new VennDiagramA(),
        new VennDiagramB(),
        new VennDiagramC(),
        new VennDiagramD(),
        new VennDiagramE(),
        new VennDiagramF()
    };

    private readonly bool[] buttonPressed = new bool[] { false, false, false, false, };

    private CruelKeypadsColor[] StageColor = new CruelKeypadsColor[2];

    private static readonly Regex TPRegex = new Regex("^press ([1-4])([1-4])([1-4])([1-4])$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    //logging
    private string VennDiagramLogging;
    private readonly bool[] SpecialRuleLogging = new bool[] { false, false, false, false, false, false};
    private int SpecialRuleIndexLogging;

    // Use this for initialization
    void Start()
    {
        _moduleId = ++_moduleIdCounter;
        Module.OnActivate += Activate;
    }

    void Activate()
    {
        Initialize(true);
        
        for (int i = 0; i < 4; ++i)
        {
            var index = i;
            Buttons[index].OnInteract += delegate
            {
                if (buttonPressed[index])
                {
                    return false;
                }
                if (_isSolved)
                {
                    return false;
                }
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Buttons[index].transform);
                Buttons[index].AddInteractionPunch();
                HandlePress(index);
                return false;
            };
        }
    }

    void GenerateSymbols()
    {
        for (int i = 0; i < 4; ++i)
        {
            var symbol = Symbols.PickRandom();
            while (pickedSymbols.Contains(symbol))
            {
                symbol = Symbols.PickRandom();
            }
            pickedSymbols.Add(symbol);
        }
    }

    private void HandlePress(int index)
    {
        buttonPressed[index] = true;
        Debug.LogFormat("[Cruel Keypads #{0}] Pressed button: {1} label {2}.", _moduleId, index + 1, pickedSymbols[index].ToString());
        Lights[index].SetActive(false);
        ButtonObjects[index].transform.localPosition = new Vector3(0, -0.01f, 0);
        ButtonHighlites[index].SetActive(false);
        PressedButtons.Add(pickedSymbols[index]);
        if (PressedButtons.ToArray().Length == 4)
        {
            Debug.LogFormat("[Cruel Keypads #{0}] You entered: {1} Expected: {2}.", _moduleId, string.Join(", ", PressedButtons.Select(x => x.ToString()).ToArray()), string.Join(", ", sortedSymbols.Select(x => x.ToString()).ToArray()));
            for (var i = 0; i < Lights.Length; ++i)
            {
                Lights[i].SetActive(true);
                ButtonObjects[i].transform.localPosition = new Vector3(0, 0, 0);
            }
            if(string.Join("", PressedButtons.Select(x => x.ToString()).ToArray()) == string.Join("", sortedSymbols.Select(x => x.ToString()).ToArray()))
            {
                stage++;
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, Buttons[index].transform);
                if(stage == 4)
                {
                    Debug.LogFormat("[Cruel Keypads #{0}] All 3 stages passed. Module solved.", _moduleId);
                    Module.HandlePass();
                    _isSolved = true;
                    StageText.text = "";
                    StripRenderer.material = BlackMat; 
                    for (int i = 0; i < 4; ++i)
                    {
                        SpriteRenderers[i].gameObject.SetActive(false);
                    }
                }
                else
                {
                    Debug.LogFormat("[Cruel Keypads #{0}] Stage: {1} passed.", _moduleId, stage - 1);
                    Initialize(false);
                }
            }
            else
            {
                Module.HandleStrike();
                Debug.LogFormat("[Cruel Keypads #{0}] STRIKE! Module resets.", _moduleId);
                Initialize(true);
            }
        }
        
    }

    private void Initialize(bool reset)
    {
        if (reset)
        {
            stage = 1;
            StageColor = new CruelKeypadsColor[2];
            Stage1Symbols.Clear();
            Stage2Symbols.Clear();
        }
        for(var i = 0; i < buttonPressed.Length; ++i)
        {
            buttonPressed[i] = false;
            ButtonHighlites[i].SetActive(true);
            ButtonObjects[i].transform.localPosition = new Vector3(0, 0, 0);
        }
        circles = new List<int>();
        sortedSymbols = new List<char>();
        pickedSymbols = new List<char>();
        stripColor = GetColor(rnd.Range(0, 6));
        PressedButtons = new List<char>();
        GenerateSymbols();
        var symbolArray = pickedSymbols.ToArray();
        if(stage == 1)
        {
            Stage1Symbols = pickedSymbols;
        }
        if (stage == 2)
        {
            Stage2Symbols = pickedSymbols;
        }
        for (int i = 0; i < 4; ++i)
        {
            SpriteRenderers[i].sprite = GetSprite(symbolArray[i]);
        }
        if (stage < 3)
        {
            StageColor[stage - 1] = stripColor;
        }
        GenerateAnswer();
        StageText.text = stage.ToString();
        StripRenderer.material = StripColors[(int)stripColor];
        Debug.LogFormat("[Cruel Keypads #{0}] ------------ Stage: {1} ------------.", _moduleId, stage);
        Debug.LogFormat("[Cruel Keypads #{0}] The selected symbols are: {1}.", _moduleId, string.Join(", ", pickedSymbols.Select(x => x.ToString()).ToArray()));
        Debug.LogFormat("[Cruel Keypads #{0}] The strip color is: {1}.", _moduleId, stripColor.ToString());
        Debug.LogFormat("[Cruel Keypads #{0}] The correct table is: {1}.", _moduleId, VennDiagramLogging);
        Debug.LogFormat("[Cruel Keypads #{0}] The special rule is {1}.", _moduleId, SpecialRuleLogging[SpecialRuleIndexLogging].ToString());
        Debug.LogFormat("[Cruel Keypads #{0}] The correct order is: {1}.", _moduleId, string.Join(", ", sortedSymbols.Select(x => x.ToString()).ToArray()));
    }

    private CruelKeypadsColor GetColor(int color)
    {
        switch (color)
        {
            case 0:
                return CruelKeypadsColor.Red;
            case 1:
                return CruelKeypadsColor.Green;
            case 2:
                return CruelKeypadsColor.Blue;
            case 3:
                return CruelKeypadsColor.Yellow;
            case 4:
                return CruelKeypadsColor.Magenta;
            default:
                return CruelKeypadsColor.White;
        }
    }

    private void GenerateAnswer()
    {
        var allowedSymbols = new List<char> { 'ㅇ', 'ㅈ', 'ㅉ', 'ㅟ', 'ㅋ' };
        var colorChars = stripColor.ToString().ToLowerInvariant().ToCharArray();
        if(stripColor == CruelKeypadsColor.Blue || stripColor == CruelKeypadsColor.Red || stripColor == CruelKeypadsColor.Green)
        {
            circles.Add(1);
        }
        if(stage == 1 || stage == 3)
        {
            circles.Add(2);
        }
        if (pickedSymbols.Any(x => allowedSymbols.Contains(x)))
        {
            circles.Add(3);
        }
        foreach(var color in colorChars)
        {
            if (Info.GetSerialNumber().ToLowerInvariant().Contains(color.ToString()))
            {
                circles.Add(4);
                break;
            }
        }
        var solution = Diagrams.Single(x => x.IsMatch(circles));
        sortedSymbols = pickedSymbols;
        List<char> order;
        switch (solution.Type)
        {
            case VennDiagram.A:
                VennDiagramLogging = "Table A";
                order = OrderA;
                break;
            case VennDiagram.B:
                VennDiagramLogging = "Table B";
                order = OrderB;
                break;
            case VennDiagram.C:
                VennDiagramLogging = "Table C";
                order = OrderC;
                break;
            case VennDiagram.D:
                VennDiagramLogging = "Table D";
                order = OrderD;
                break;
            case VennDiagram.E:
                VennDiagramLogging = "Table E";
                order = OrderE;
                break;
            default:
                VennDiagramLogging = "Table F";
                order = OrderF;
                break;
        }
        sortedSymbols = sortedSymbols.OrderBy(x => order.IndexOf(x)).ToList();
        GetSpecialRule(solution.Type);
    }

    private void GetSpecialRule(VennDiagram diagram)
    {
        var concreteList = (List<char>)sortedSymbols;
        switch (diagram)
        {
            case VennDiagram.A:
                SpecialRuleLogging[0] = true;
                SpecialRuleIndexLogging = 0;
                sortedSymbols = Swap(Swap(sortedSymbols, 0, 3), 1, 2);
                break;
            case VennDiagram.B:
                SpecialRuleIndexLogging = 1;
                if (stage == 2 || stage == 3)
                {
                    SpecialRuleLogging[1] = true;
                    sortedSymbols = Swap(sortedSymbols, 0, 3);
                }
                break;
            case VennDiagram.C:
                SpecialRuleIndexLogging = 2;
                
                if (Info.IsPortPresent(Port.PS2) && Info.GetOnIndicators().Any())
                {
                    SpecialRuleLogging[2] = true;
                    concreteList.Reverse();
                }
                else
                {
                    sortedSymbols = Swap(sortedSymbols, 0, 1);
                }
                sortedSymbols = concreteList;
                break;
            case VennDiagram.D:
                SpecialRuleIndexLogging = 3;
                if (Stage1Symbols.Contains('ㅟ') || Stage2Symbols.Contains('ㅟ') || sortedSymbols.Contains('ㅟ'))
                {
                    SpecialRuleLogging[3] = true;
                    concreteList.Reverse();
                }
                sortedSymbols = concreteList;
                break;
            case VennDiagram.E:
                SpecialRuleIndexLogging = 4;
                HandleCaseE();      
                break;
            default:
                SpecialRuleIndexLogging = 5;
                HandleCaseF();
                break;
        }
    }

    private void HandleCaseF()
    {
        bool found = false;
        char[] serialNumberChars = Info.GetSerialNumber().ToLowerInvariant().ToCharArray();
        foreach (var serialNumberChar in serialNumberChars)
        {
            foreach (var port in Info.GetPorts())
            {
                if (port.ToLowerInvariant().Contains(serialNumberChar.ToString()))
                {
                    SpecialRuleLogging[5] = true;
                    sortedSymbols = Swap(sortedSymbols, 2, 3);
                    found = true;
                    break;
                }
            }
            if (found)
            {
                break;
            }
        }
        if (!found)
        {
                sortedSymbols = Swap(sortedSymbols, 0, 1);
        }
    }

    private Sprite GetSprite(char character)
    {
        return Sprites[Symbols.IndexOf(character)];
    }

    private void HandleCaseE()
    {
        var concreteList = (List<char>)sortedSymbols;

        if (stage == 1)
        {
            return;
        }
        else if (stage == 2)
        {
            if (StageColor[0] == CruelKeypadsColor.Yellow || StageColor[0] == CruelKeypadsColor.Blue)
            {
                SpecialRuleLogging[4] = true;
                concreteList.Reverse();
            }
        }
        else
        {
            if (StageColor[1] == CruelKeypadsColor.Yellow || StageColor[1] == CruelKeypadsColor.Blue || StageColor[0] == CruelKeypadsColor.Yellow || StageColor[0] == CruelKeypadsColor.Blue)
            {
                SpecialRuleLogging[4] = true;
                concreteList.Reverse();
            }
        }
        sortedSymbols = concreteList;
    }

    private static IList<char> Swap(IList<char> list, int index1, int index2)
    {
        var value1 = list[index1];
        list[index1] = list[index2];
        list[index2] = value1;
        return list;
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = "!{0} press 1234. Reading order.";
#pragma warning restore 414

    public IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant().Trim();
        Match M = TPRegex.Match(command);
        if (M.Success)
        {
            yield return null;
            for(var i = 0; i < 4; ++i)
            {
                Buttons[int.Parse(M.Groups[i + 1].Value) - 1].OnInteract();
                yield return new WaitForSeconds(.1f);
            }
        }
    }

    public IEnumerator TwitchHandleForcedSolve()
    {
        Debug.LogFormat("[Cruel Keypads #{0}] Force solve requested by twitch plays.", _moduleId);
        for(var i = 0; i < PressedButtons.Count;++i)
        {
            if (PressedButtons[i] != sortedSymbols[i])
            {
                Initialize(true);
            }
        }
        while(!_isSolved)
        {
            var symbols = pickedSymbols;
            var correctOrder = sortedSymbols;

            for (int i = buttonPressed.Count(x => x); i < 4; i++)
            {
                var correctButton = symbols.IndexOf(correctOrder[i]);
                Buttons[correctButton].OnInteract();
                yield return new WaitForSeconds(.1f);
            }
        }
    }
}