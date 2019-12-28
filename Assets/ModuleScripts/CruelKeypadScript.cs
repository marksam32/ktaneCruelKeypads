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
    public TextMesh[] ButtonTexts;
    public TextMesh StageText;
    public MeshRenderer StripRenderer;
    public Material[] StripColors;
    public Material BlackMat;
    public GameObject[] Lights;
    public GameObject[] ButtonObjects;
    public GameObject[] ButtonHighlites;

    private static int _moduleIdCounter = 1;
    private int _moduleId = 0;
    private bool _isSolved = false;

    private int stage = 1;

    private enum Colors
    {
        Red = 0,
        Green = 1,
        Blue = 2,
        Yellow = 3,
        Magenta = 4,
        White = 5
    }
    private Colors stripColor;
   
    private VennDiagram DiagramOutput;

    private List<char> Symbols = new List<char>() { 'ㄹ', 'ㅁ', 'ㅂ', 'ㄱ', 'ㄲ', 'ㄷ', 'ㅈ', 'ㅉ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅃ', 'ㅅ', 'ㅆ', 'ㅇ', 'ㅎ', 'ㄴ', 'ㄸ' };

    private List<char> OrderA = new List<char>() { 'ㅃ', 'ㄲ', 'ㄴ', 'ㄷ', 'ㄸ', 'ㄹ', 'ㅁ', 'ㅂ', 'ㄱ', 'ㅅ', 'ㅆ', 'ㅇ', 'ㅈ', 'ㅉ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ' };
    private List<char> OrderB = new List<char>() { 'ㅇ', 'ㅈ', 'ㅉ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ', 'ㄱ', 'ㄹ', 'ㅁ', 'ㅂ', 'ㅃ', 'ㅅ', 'ㅆ', 'ㄲ', 'ㄴ', 'ㄷ', 'ㄸ' };
    private List<char> OrderC = new List<char>() { 'ㄹ', 'ㅁ', 'ㅂ', 'ㄱ', 'ㄲ', 'ㄷ', 'ㅈ', 'ㅉ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅃ', 'ㅅ', 'ㅆ', 'ㅇ', 'ㅎ', 'ㄴ', 'ㄸ' };
    private List<char> OrderD = new List<char>() { 'ㄱ', 'ㅎ', 'ㄲ', 'ㅍ', 'ㄴ', 'ㅌ', 'ㄷ', 'ㅋ', 'ㄸ', 'ㅊ', 'ㄹ', 'ㅉ', 'ㅁ', 'ㅈ', 'ㅂ', 'ㅇ', 'ㅃ', 'ㅆ', 'ㅅ' };
    private List<char> OrderE = new List<char>() { 'ㄷ', 'ㄸ', 'ㄹ', 'ㅁ', 'ㅂ', 'ㅃ', 'ㅎ', 'ㅊ', 'ㅋ', 'ㄱ', 'ㄲ', 'ㄴ', 'ㅅ', 'ㅆ', 'ㅈ', 'ㅍ', 'ㅉ', 'ㅇ', 'ㅌ' };
    private List<char> OrderF = new List<char>() { 'ㅋ', 'ㄸ', 'ㄷ', 'ㅅ', 'ㅍ', 'ㅌ', 'ㅁ', 'ㄴ', 'ㅃ', 'ㅉ', 'ㄲ', 'ㅆ', 'ㅎ', 'ㅈ', 'ㅊ', 'ㅂ', 'ㄹ', 'ㄱ', 'ㅇ' };

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

    private bool[] buttonPressed = new bool[] { false, false, false, false, };

    private Colors[] StageColor = new Colors[2];

    private static readonly Regex TPRegex = new Regex("^press ([1-4])([1-4])([1-4])([1-4])$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

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
        char symbol;
        for (int i = 0; i < 4; ++i)
        {
            symbol = Symbols.PickRandom();
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
        Debug.LogFormat("[Cruel Keyads #{0}] Pressed button: {1} label {2}.", _moduleId, index + 1, ButtonTexts[index].text.ToString());
        Lights[index].SetActive(false);
        ButtonObjects[index].transform.localPosition = new Vector3(0, -0.01f, 0);
        ButtonHighlites[index].SetActive(false);
        PressedButtons.Add(char.Parse(ButtonTexts[index].text));
        if (PressedButtons.ToArray().Length == 4)
        {
            Debug.LogFormat("[Cruel Keyads #{0}] You entered: {1} Expected: {2}.", _moduleId, string.Join(", ", PressedButtons.Select(x => x.ToString()).ToArray()), string.Join(", ", sortedSymbols.Select(x => x.ToString()).ToArray()));
            for (var i = 0; i < Lights.Length; ++i)
            {
                Lights[i].SetActive(true);
                ButtonObjects[i].transform.localPosition = new Vector3(0, 0, 0);
            }
            if(string.Join(", ", PressedButtons.Select(x => x.ToString()).ToArray()) == string.Join(", ", sortedSymbols.Select(x => x.ToString()).ToArray()))
            {
                stage++;
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, Buttons[index].transform);
                if(stage == 4)
                {
                    Debug.LogFormat("[Cruel Keyads #{0}] All 3 stages passed. Module solved.s", _moduleId);
                    Module.HandlePass();
                    StageText.text = "";
                    StripRenderer.material = BlackMat; 
                    for (int i = 0; i < 4; ++i)
                    {
                        ButtonTexts[i].text = "";
                    }
                }
                else
                {
                    Debug.LogFormat("[Cruel Keyads #{0}] Stage: {1} passed.", _moduleId, stage - 1);
                    Initialize(false);
                }
            }
            else
            {
                Module.HandleStrike();
                Debug.LogFormat("[Cruel Keyads #{0}] STRIKE! Module resets.", _moduleId);
                Initialize(true);
            }
        }
        
    }

    private void Initialize(bool reset)
    {
        if (reset)
        {
            stage = 1;
            StageColor = new Colors[2];
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
        var SymbolArray = pickedSymbols.ToArray();
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
            ButtonTexts[i].text = SymbolArray[i].ToString();
        }
        if (stage < 3)
        {
            StageColor[stage - 1] = stripColor;
        }
        GenerateAnswer();
        StageText.text = stage.ToString();
        StripRenderer.material = StripColors[(int)stripColor];
        Debug.LogFormat("[Cruel Keyads #{0}] ------------ Stage: {1} ------------.", _moduleId, stage);
        Debug.LogFormat("[Cruel Keyads #{0}] The selected symbols are: {1}.", _moduleId, string.Join(", ", pickedSymbols.Select(x => x.ToString()).ToArray()));
        Debug.LogFormat("[Cruel Keyads #{0}] The strip color is: {1}.", _moduleId, stripColor.ToString());
        Debug.LogFormat("[Cruel Keyads #{0}] The the correct order is: {1}.", _moduleId, string.Join(", ", sortedSymbols.Select(x => x.ToString()).ToArray()));
    }

    private Colors GetColor(int color)
    {
        switch (color)
        {
            case 0:
                return Colors.Red;
            case 1:
                return Colors.Green;
            case 2:
                return Colors.Blue;
            case 3:
                return Colors.Yellow;
            case 4:
                return Colors.Magenta;
            default:
                return Colors.White;
        }
    }

    private void GenerateAnswer()
    {
        List<char> allowedSymbols = new List<char> { 'ㅇ', 'ㅈ', 'ㅉ', 'ㅊ', 'ㅋ' };
        char[] colorChars = stripColor.ToString().ToLowerInvariant().ToCharArray();
        if(stripColor == Colors.Blue || stripColor == Colors.Red || stripColor == Colors.Green)
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
        var solution = Diagrams.Where(x => x.IsMatch(circles)).SingleOrDefault();
        sortedSymbols = pickedSymbols;
        List<char> order = null;
        switch (solution.Type)
        {
            case VennDiagram.A:
                order = OrderA;
                break;
            case VennDiagram.B:
                order = OrderB;
                break;
            case VennDiagram.C:
                order = OrderC;
                break;
            case VennDiagram.D:
                order = OrderD;
                break;
            case VennDiagram.E:
                order = OrderE;
                break;
            default:
                order = OrderF;
                break;
        }
        sortedSymbols = sortedSymbols.OrderBy(x => order.IndexOf(x)).ToList();
        GetSpecialRule(solution.Type);
    }

    private void GetSpecialRule(VennDiagram diagram)
    {
        switch (diagram)
        {
            case VennDiagram.A:
                sortedSymbols = Swap(Swap(sortedSymbols, 0, 3), 1, 2);
                break;
            case VennDiagram.B:
                if(stage == 2 || stage == 3)
                {
                    sortedSymbols = Swap(sortedSymbols, 0, 3);
                }
                break;
            case VennDiagram.C:
                if(Info.GetPortCount(Port.PS2) > 0 && Info.GetOnIndicators().Count() > 0)
                {
                    sortedSymbols.Reverse();
                }
                else
                {
                    sortedSymbols = Swap(sortedSymbols, 0, 1);
                }
                break;
            case VennDiagram.D:
                if(Stage1Symbols.Contains('ㅊ') || Stage2Symbols.Contains('ㅊ') || sortedSymbols.Contains('ㅊ'))
                {
                    sortedSymbols.Reverse();
                }
                break;
            case VennDiagram.E:
                if (stage == 1)
                {
                    break;
                }
                else if (stage == 2)
                {
                    if (StageColor[0] == Colors.Yellow || StageColor[0] == Colors.Blue)
                    {
                        sortedSymbols.Reverse();
                        break;
                    }
                }
                else
                {
                    if (StageColor[1] == Colors.Yellow || StageColor[1] == Colors.Blue)
                    {
                        sortedSymbols.Reverse();
                        break;
                    }
                }              
                break;
            default:
                char[] serialNumberChars = Info.GetSerialNumber().ToLowerInvariant().ToCharArray();
                foreach(var serialNumberChar in serialNumberChars)
                {
                    if (Info.GetPorts().Contains(serialNumberChar.ToString()))
                    {
                        sortedSymbols = Swap(sortedSymbols, 2, 3);
                        break;
                    }
                    else
                    {
                        sortedSymbols = Swap(sortedSymbols, 0, 1);
                        break;
                    }
                }
                break;
        }
    }   

    private static IList<char> Swap(IList<char> list, int index1, int index2)
    {
        var value1 = list[index1];
        list[index1] = list[index2];
        list[index2] = value1;
        return list;
    }

    // Update is called once per frame
    void Update()
    {

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
        yield break;
    }
}