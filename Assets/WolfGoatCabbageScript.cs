using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;
using WolfGoatCabbage;

public class WolfGoatCabbageScript : MonoBehaviour 
{
    public KMBombModule Module;
    public KMAudio Audio;
    public GameObject[] Images;
    public KMSelectable[] Buttons;
    public GameObject[] LightObject;
    public Light LightSource;

    private string[] _creaturesList = new string[] { "Cat", "Wolf", "Rabbit", "Berry", "Fish", "Dog", "Duck", "Goat", "Fox", "Grass", "Rice", "Mouse", "Bear", "Cabbage", "Chicken", "Goose", "Corn", "Carrot", "Horse", "Earthworm", "Kiwi", "Seeds" };
    private Dictionary<string, string[]> _conflictList = new Dictionary<string, string[]>()
    {
        { "Cat", new string[] { "Fish", "Mouse", "Goose", "Bear", "Wolf" } },
        { "Wolf", new string[] { "Fox", "Goose", "Bear", "Cat", "Goat", "Chicken", "Dog" } },
        { "Rabbit", new string[] { "Grass", "Fox", "Carrot", "Goose", "Cabbage", "Dog" } },
        { "Berry", new string[] { "Fox", "Mouse", "Duck", "Earthworm" } },
        { "Fish", new string[] { "Cat", "Bear", "Fox", "Dog", "Earthworm" } },
        { "Dog", new string[] { "Wolf", "Mouse", "Fish", "Rabbit", "Chicken", "Goose", "Bear" } },
        { "Duck", new string[] { "Grass", "Cabbage", "Goose", "Rice", "Berry", "Seeds", "Corn" } },
        { "Goat", new string[] { "Grass", "Cabbage", "Goose", "Wolf", "Corn", "Carrot" } },
        { "Fox", new string[] { "Wolf", "Rabbit", "Chicken", "Berry", "Fish", "Mouse", "Goose" } },
        { "Grass", new string[] { "Duck", "Goose", "Rabbit", "Goat", "Horse", "Earthworm" } },
        { "Rice", new string[] { "Chicken", "Duck", "Mouse", "Goose" } },
        { "Mouse", new string[] { "Cat", "Berry", "Cabbage", "Corn", "Dog", "Rice", "Fox", "Goose" } },
        { "Bear", new string[] { "Fish", "Goose", "Wolf", "Dog", "Cat", "Horse" } },
        { "Cabbage", new string[] { "Goat", "Duck", "Goose", "Mouse", "Kiwi", "Earthworm", "Rabbit" } },
        { "Chicken", new string[] { "Fox", "Rice", "Goose", "Seeds", "Earthworm", "Corn", "Wolf", "Dog" } },
        { "Goose", new string[] { "Grass", "Cabbage", "Duck", "Chicken", "Bear", "Horse", "Rabbit", "Wolf", "Fox", "Cat", "Goat", "Dog", "Kiwi", "Mouse", "Earthworm", "Rice" } },
        { "Corn", new string[] { "Mouse", "Chicken", "Goat", "Duck" } },
        { "Carrot", new string[] { "Rabbit", "Horse", "Earthworm", "Goat" } },
        { "Horse", new string[] { "Grass", "Carrot", "Goose", "Bear" } },
        { "Earthworm", new string[] { "Kiwi", "Carrot", "Grass", "Cabbage", "Chicken", "Goose", "Seeds", "Fish", "Berry" } },
        { "Kiwi", new string[] { "Earthworm", "Cabbage", "Seeds", "Goose" } },
        { "Seeds", new string[] { "Chicken", "Kiwi", "Duck", "Earthworm" } }
    };
    private VertexCoverGraph _graph = new VertexCoverGraph();
    private int _boatSize;
    private List<string> _startShore = new List<string>();
    private List<string> _finalShore = new List<string>();
    private List<string> _onBoat = new List<string>();
    private bool _onStartingShore = true;
    private string[] _animalOnScreen;
    private int _currentAnimal = 0;
    private bool[] _buttonAnimation = new bool[5] { false, false, false, false, false };
    private bool _TwitchPlaysExecuteOnInvalid = false;
    private Dictionary<string, string> _TwitchPlaysAnimalAliases = new Dictionary<string, string>()
    {
        { "worm", "earthworm" }
    };
    List<State> _solution = new List<State>();

    static int moduleIdCounter = 1;
    private int _moduleID;
    private bool _moduleSolved = false;

    private void Start() 
    {
        _moduleID = moduleIdCounter++;
        float scalar = transform.lossyScale.x;
        LightSource.range *= scalar;
        Generate();
        for (int i = 0; i < 2; i++)
        {
            int j = i;
            Buttons[j].OnInteract += delegate
            {
                CycleButtonPress(j);
                return false;
            };
        }
        Buttons[2].OnInteract += delegate
        {
            AboardButtonPress();
            return false;
        };
        Buttons[3].OnInteract += delegate
        {
            RowButtonPress();
            return false;
        };
        Buttons[4].OnInteract += delegate
        {
            ResetButtonPress();
            return false;
        };
    }

    private void CycleButtonPress(int i)
    {
        if (_buttonAnimation[i]) return;
        StartCoroutine(ButtonAnimation(i));
        if (_moduleSolved) return;
        Images[Array.IndexOf(_creaturesList, _animalOnScreen[_currentAnimal])].SetActive(false);
        _currentAnimal = i == 0 ? (_currentAnimal + _animalOnScreen.Length - 1) % _animalOnScreen.Length : (_currentAnimal + 1) % _animalOnScreen.Length ;
        Images[Array.IndexOf(_creaturesList, _animalOnScreen[_currentAnimal])].SetActive(true);
        if (_onBoat.Contains(_animalOnScreen[_currentAnimal]))
        {
            LightObject[1].SetActive(false);
            LightObject[0].SetActive(true);
        }
        else
        {
            LightObject[0].SetActive(false);
            LightObject[1].SetActive(true);
        }
    }

    private void AboardButtonPress()
    {
        if (_buttonAnimation[2]) return;
        StartCoroutine(ButtonAnimation(2));
        if (_moduleSolved) return;
        if (_onBoat.Contains(_animalOnScreen[_currentAnimal]) || (_onBoat.Count < _boatSize && ((_onStartingShore && _startShore.Contains(_animalOnScreen[_currentAnimal])) || (!_onStartingShore && _finalShore.Contains(_animalOnScreen[_currentAnimal])))))
        {
            List<string> list = _onStartingShore ? _startShore : _finalShore;
            if (_onBoat.Contains(_animalOnScreen[_currentAnimal]))
            {
                list.Add(_animalOnScreen[_currentAnimal]);
                _onBoat.Remove(_animalOnScreen[_currentAnimal]);
                Debug.LogFormat("[Wolf, Goat, and Cabbage #{0}] Moving the {1} off the boat.", _moduleID, _animalOnScreen[_currentAnimal].ToLowerInvariant());
                LightObject[0].SetActive(false);
                LightObject[1].SetActive(true);
            }
            else
            {
                list.Remove(_animalOnScreen[_currentAnimal]);
                Debug.LogFormat("[Wolf, Goat, and Cabbage #{0}] Boarding the {1} on to the boat.", _moduleID, _animalOnScreen[_currentAnimal].ToLowerInvariant());
                _onBoat.Add(_animalOnScreen[_currentAnimal]);
                LightObject[1].SetActive(false);
                LightObject[0].SetActive(true);
            }
        }
        else
        {
            Debug.LogFormat("[Wolf, Goat, and Cabbage #{0}] Unable to move the {1} on to the boat. Initiating a strike.", _moduleID, _animalOnScreen[_currentAnimal].ToLowerInvariant());
            Module.HandleStrike();
        }
    }

    private void RowButtonPress()
    {
        if (_buttonAnimation[3]) return;
        StartCoroutine(ButtonAnimation(3));
        if (_moduleSolved) return;
        List<string> list = _onStartingShore ? _startShore : _finalShore;
        if (list.Any(s => list.Where(x => s != x).Any(s2 => _graph.ContainEdge(s, s2))))
        {
            Debug.LogFormat("[Wolf, Goat, and Cabbage #{0}] Creatures with conflicts are still on the same shore. Initiating a strike.", _moduleID);
            Module.HandleStrike();
        }
        else
        {
            _onStartingShore = !_onStartingShore;
            if (_onBoat.Count == 0)
                Debug.LogFormat("[Wolf, Goat, and Cabbage #{0}] Moving to the other side of the river by yourself.", _moduleID);
            else
                Debug.LogFormat("[Wolf, Goat, and Cabbage #{0}] Moving {1} to the other side of the river.", _moduleID, string.Join(_onBoat.Count == 2 ? " " : ", ", _onBoat.Select((s2, index) => index == _onBoat.Count - 1 && _onBoat.Count != 1 ? "and " + s2.ToLowerInvariant() : s2.ToLowerInvariant()).ToArray()));
            List<string> animalOnNewShore = _onStartingShore ? _startShore.ToList() : _finalShore.ToList();
            animalOnNewShore.AddRange(_onBoat);
            if (_onStartingShore)
            {
                Debug.LogFormat("[Wolf, Goat, and Cabbage #{0}] The starting side of the river now has {1}.", _moduleID, animalOnNewShore.Count == 0 ? "nothing" : animalOnNewShore.Select(animal => animal.ToLowerInvariant()).JoinWithCommasOrAnd());
                Debug.LogFormat("[Wolf, Goat, and Cabbage #{0}] The finishing side of the river now has {1}.", _moduleID, _finalShore.Count == 0 ? "nothing" : _finalShore.Select(animal => animal.ToLowerInvariant()).JoinWithCommasOrAnd());
            }
            else
            {
                Debug.LogFormat("[Wolf, Goat, and Cabbage #{0}] The starting side of the river now has {1}.", _moduleID, _startShore.Count == 0 ? "nothing" : _startShore.Select(animal => animal.ToLowerInvariant()).JoinWithCommasOrAnd());
                Debug.LogFormat("[Wolf, Goat, and Cabbage #{0}] The finishing side of the river now has {1}.", _moduleID, animalOnNewShore.Count == 0 ? "nothing" : animalOnNewShore.Select(animal => animal.ToLowerInvariant()).JoinWithCommasOrAnd());
            }
            if (!_onStartingShore && _startShore.Count == 0)
            {
                Images[Array.IndexOf(_creaturesList, _animalOnScreen[_currentAnimal])].SetActive(false);
                LightObject[0].SetActive(false);
                LightObject[1].SetActive(true);
                Debug.LogFormat("[Wolf, Goat, and Cabbage #{0}] No more creature left on the starting side of the river. Solving the module.", _moduleID);
                _moduleSolved = true;
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
                Module.HandlePass();
            }
        }
    }

    private void ResetButtonPress()
    {
        if (_buttonAnimation[4]) return;
        StartCoroutine(ButtonAnimation(4));
        if (_moduleSolved) return;
        Debug.LogFormat("[Wolf, Goat, and Cabbage #{0}] Reset button was pressed. Resetting the module.", _moduleID);
        _onBoat.Clear();
        _finalShore.Clear();
        _startShore = _animalOnScreen.ToList();
        LightObject[0].SetActive(false);
        LightObject[1].SetActive(true);
        _onStartingShore = true;
        Images[Array.IndexOf(_creaturesList, _animalOnScreen[_currentAnimal])].SetActive(false);
        _currentAnimal = 0;
        Images[Array.IndexOf(_creaturesList, _animalOnScreen[_currentAnimal])].SetActive(true);
    }

    private IEnumerator ButtonAnimation(int i)
    {
        _buttonAnimation[i] = true;
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        Buttons[i].Highlight.transform.localPosition -= new Vector3(0, 0, .05f);
        for (int count = 0; count < 6; count++)
        {
            Buttons[i].transform.localPosition -= new Vector3(0, .00067f, 0);
            yield return new WaitForSeconds(.005f);
        }
        for (int count = 0; count < 6; count++)
        {
            Buttons[i].transform.localPosition += new Vector3(0, .00067f, 0);
            yield return new WaitForSeconds(.005f);
        }
        Buttons[i].Highlight.transform.localPosition += new Vector3(0, 0, .05f);
        _buttonAnimation[i] = false;
    }

    private void Generate()
    {
        //Commented lines is an example of large boat graph.
        int animalCount = Rnd.Range(6, 10);
        //int animalCount = 6;
        //int kk = 0;
        //int[] allAnimals = new[] { 20, 7, 13, 6, 15, 12 };
        while (_startShore.Count != animalCount)
        {
            int animal = Rnd.Range(0, _creaturesList.Length);
            //int animal = allAnimals[kk];
            //kk++;
            if (!_graph.CreateNode(_creaturesList[animal], _conflictList[_creaturesList[animal]])) continue;
            _startShore.Add(_creaturesList[animal]);
        }
        _animalOnScreen = _startShore.ToArray();
        Debug.LogFormat("[Wolf, Goat, and Cabbage #{0}] The creatures that you have are {1}.", _moduleID, string.Join(", ", _animalOnScreen.Select((x, index) => index == _animalOnScreen.Length - 1 ? "and " + x.ToLowerInvariant() : x.ToLowerInvariant()).ToArray()));
        _graph.GenerateConnectingEdge();
        Debug.LogFormat("[Wolf, Goat, and Cabbage #{0}] In this group, the conflicts for each creature are the following:", _moduleID);
        bool listExist = false;
        _animalOnScreen.ToList().ForEach(s =>
        {
            List<string> item = _animalOnScreen.Where(x => s != x && _graph.ContainEdge(s, x)).ToList();
            if (item.Count != 0)
            {
                Debug.LogFormat("[Wolf, Goat, and Cabbage #{0}] {1}: {2}", _moduleID, s, string.Join(item.Count == 2 ? " " : ", ", item.Select((s2, index) => index == item.Count - 1 && item.Count != 1 ? "and " + s2 : s2).ToArray()));
                listExist = true;
            }
        });
        if (!listExist)
            Debug.LogFormat("[Wolf, Goat, and Cabbage #{0}] Wait a minute. There is no conflict.", _moduleID);
        GenerateSVG();
        _boatSize = _graph.BoundSearchTree(animalCount);
        try
        {
            _solution = BFS();
        }
        catch
        {
            Debug.LogFormat("<Wolf, Goat, and Cabbage #{0}> No solution for small boat. Adding 1 more space to the boat.", _moduleID);
            _boatSize++;
            _solution = BFS();
        }
        Debug.LogFormat("[Wolf, Goat, and Cabbage #{0}] The Alcuin number for this group is {1}.", _moduleID, _boatSize);
        Debug.LogFormat("[Wolf, Goat, and Cabbage #{0}] One possible solution starting from the initial state:", _moduleID);
        foreach (State step in _solution)
        {
            if (step.Movement[0] == "")
                Debug.LogFormat("[Wolf, Goat, and Cabbage #{0}] Move across the river by yourself alone.", _moduleID);
            else
                Debug.LogFormat("[Wolf, Goat, and Cabbage #{0}] Move {1} across the river.", _moduleID, step.Movement.Select(animal => animal.ToLowerInvariant()).JoinWithCommasOrAnd());
        }
        Images[Array.IndexOf(_creaturesList, _animalOnScreen[_currentAnimal])].SetActive(true);
    }

    private void GenerateSVG()
    {
        string svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 104 104\" fill=\"none\" width=\"75%\">" +
                         "<style>" +
                             ".node {{" +
                                 "fill: black;" +
                                 "font-size: 6px;"+
                                 "text-anchor: middle;" +
                                 "text-align: center;"+
                                 "dominant-baseline: central;"+
                             "}}"+
                                 ".vertex {{"+
                                 "fill: white;"+
                                 "stroke: #000000;"+          
                                 "stroke-width: 1;"+
                             "}}"+
                         "</style>"+
                         "{1}" + 
                     "</svg>";
        string group = "<g transform=\"translate(52, 52)\">{0}</g>";
        string node = "<g transform= \"translate(0, -43) rotate({0}, 0, 43) rotate(-{1}, 0, 0)\"><circle class=\"vertex\" cx=\"0\" cy=\"0\" r=\"8\"/><text class=\"node\">{2}</text></g>";
        float graphSize = _graph._vertices.Count;
        string circles = string.Join("", _graph._vertices.Select((vertex, index) => string.Format(node, index * 360f / graphSize, index * 360f / graphSize, vertex.Name.Substring(0, 3))).ToArray());
        string line = "<line x1=\"{0}\" y1=\"{1}\" x2=\"{2}\" y2=\"{3}\" stroke=\"black\" />";
        string linesGroup = string.Join("", _graph._edges.Select(e => string.Format(line, 52f - 43f * Mathf.Sin(-2f * _graph._vertices.IndexOf(e.Index1) * Mathf.PI / graphSize), 52f -43f * Mathf.Cos(-2f * _graph._vertices.IndexOf(e.Index1) * Mathf.PI / graphSize), 52f - 43f * Mathf.Sin(-2f * _graph._vertices.IndexOf(e.Index2) * Mathf.PI / graphSize), 52f - 43f * Mathf.Cos(-2f * _graph._vertices.IndexOf(e.Index2) * Mathf.PI / graphSize))).ToArray());
        group = string.Format(group,circles);
        Debug.LogFormat("[Wolf, Goat, and Cabbage #{0}]=svg[The following is the graph of the conflicts between creatures:]" + svg, _moduleID, linesGroup + group);
    }

    private List<State> BFS()
    {
        Queue<State> queue = new Queue<State>();
        HashSet<State> visited = new HashSet<State>();
        queue.Enqueue(new State(_graph, _startShore.ToArray(), _finalShore.ToArray(), _boatSize, !_onStartingShore));
        while (queue.Count != 0)
        {
            State currentState = queue.Dequeue();
            visited.Add(currentState);
            if (currentState.IsGoal())
            {
                List<State> solutions = new List<State>();
                State currentLookupState = currentState;
                while (currentLookupState.PreviousState != null)
                {
                    solutions.Add(currentLookupState);
                    currentLookupState = currentLookupState.PreviousState;
                }
                queue.Clear();
                visited.Clear();
                solutions.Reverse();
                return solutions;
            }

            List<State> nextStates = currentState.GetSuccessors();
            foreach (State state in nextStates)
            {
                //if (!queue.Any(s => s.Equals(state)) && !visited.Any(s => s.Equals(state)))
                if (!queue.Contains(state) && !visited.Contains(state))
                    queue.Enqueue(state);
            }
        }
        throw new Exception("Algorithm fails to find a solution. Please report the bug to the current maintainer.");
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = "Use !{0} c(ycle) to cycle for creatures. Use !{0} reset to reset the module. Use !{0} b(oard)/ab(oard) <creature> to board the specified creatures. The module will press aboard button if <creatures> is empty. Use !{0} l(eft)/r(ight) <1-digit number> to press the left or right button that many times. Default to 1 press if number is not specified. Use !{0} row to press row button.\nCommands can be chained by seperating them with spaces or the following characters: ( | , ; ) These characters must be used to chain any command after b(oard)/ab(oard). Using multiple of these characters in succession will result in an invalid command.\nCommands will be discarded by default upon encountering an invalid command. Use !{0} a(lways)e(xecute) <on|off> to allow execution up until when the invalid command is encountered. This command cannot be chained. Not specifying on or off will toggle the setting.";
#pragma warning restore 414

    sealed class WGCTwitchPlaysCommand
    {
        public int Index;
        public float Delay;
        public int Times;

        public WGCTwitchPlaysCommand(int index, float delay, int times = 1)
        {
            Index = index;
            Delay = delay;
            Times = times;
        }
    }

    private IEnumerator DoInteraction(WGCTwitchPlaysCommand nextTPCommand)
    {
        for (int i = 0; i < nextTPCommand.Times; i++)
        {
            yield return new WaitUntil(() => !_buttonAnimation[nextTPCommand.Index]);
            Buttons[nextTPCommand.Index].OnInteract();
            yield return new WaitForSeconds(nextTPCommand.Delay);
        }
    }

    private IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.Trim().ToLowerInvariant();
        if (Regex.IsMatch(command, @"{|}"))
        {
            yield return "sendtochaterror Commands cannot contain braces.";
            yield break;
        }
        Match m = Regex.Match(command, @"^a(?:lways)?e(?:xecute)?(?:\s+(on|off))?$");
        if (m.Success)
        {
            yield return null;
            if (m.Groups[1].Success)
                _TwitchPlaysExecuteOnInvalid = m.Groups[1].Value == "on";
            else
                _TwitchPlaysExecuteOnInvalid = !_TwitchPlaysExecuteOnInvalid;
            if (_TwitchPlaysExecuteOnInvalid)
                yield return "sendtochat Commands will now always be executed whether there is an invalid command in the string or not.";
            else
                yield return "sendtochat Commands will now be prevented from executing if there is any invalid command in the string.";
            yield break;
        }
        List<WGCTwitchPlaysCommand> buttonsToPress = new List<WGCTwitchPlaysCommand>();
        m = Regex.Match(command, @"(?:^|(?!^)(?:\s*[\|;,]|\s+)\s*)((c(?:ycle)?\b)|(reset)\b|(a?b(?:oard)?)\b((?: [^\|;,]+)+)?|(l(?:eft)?|r(?:ight)?)\b( \d)?|(row)\b|(.+))$?");
        uint nthCommand = 1;
        bool commandError = false;
        bool animalError = false;
        int animalIndex = _currentAnimal;
        string commandName = "";
        while (m.Success)
        {
            commandName = m.Groups[1].Value.Trim();
            if (m.Groups[2].Success)
            {
                for (int i = 0; i < _animalOnScreen.Length; i++)
                    buttonsToPress.Add(new WGCTwitchPlaysCommand(1, 2f));
            }
            else if (m.Groups[3].Success)
                buttonsToPress.Add(new WGCTwitchPlaysCommand(4, .1f));
            else if (m.Groups[4].Success)
            {
                if (!m.Groups[5].Success)
                    buttonsToPress.Add(new WGCTwitchPlaysCommand(2, .1f));
                else
                {
                    string[] creatures = m.Groups[5].Value.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(rawName =>
                    {
                        string parsedName;
                        if (!_TwitchPlaysAnimalAliases.TryGetValue(rawName, out parsedName))
                            parsedName = rawName;
                        return parsedName;
                    }).ToArray();
                    if (creatures.Any(c => !_animalOnScreen.Select(x => x.ToLowerInvariant()).ToArray().Contains(c)))
                    {
                        commandError = true;
                        animalError = true;
                        break;
                    }
                    for (int i = 0; i < creatures.Length; i++)
                    {
                        int goalIndex = Array.FindIndex(_animalOnScreen, animal => animal.Equals(creatures[i], StringComparison.InvariantCultureIgnoreCase));
                        int count1 = (goalIndex - animalIndex + _animalOnScreen.Length) % _animalOnScreen.Length;
                        int count2 = (animalIndex - goalIndex + _animalOnScreen.Length) % _animalOnScreen.Length;
                        int indexToPress = count1 < count2 ? 1 : 0;
                        int pressCounts = indexToPress == 1 ? count1 : count2;
                        buttonsToPress.Add(new WGCTwitchPlaysCommand(indexToPress, .1f, pressCounts));
                        buttonsToPress.Add(new WGCTwitchPlaysCommand(2, .1f));
                        animalIndex = goalIndex;
                    }
                }
            }
            else if (m.Groups[6].Success)
            {
                int times = int.TryParse(m.Groups[7].Value.Trim(), out times) ? times : 1;
                int buttonIndex = m.Groups[6].Value[0] == 'l' ? 0 : 1;
                buttonsToPress.Add(new WGCTwitchPlaysCommand(buttonIndex, .1f, times % _animalOnScreen.Length));
            }
            else if (m.Groups[8].Success)
                buttonsToPress.Add(new WGCTwitchPlaysCommand(3, .1f));
            else if (m.Groups[9].Success)
            {
                commandError = true;
                break;
            }
            m = m.NextMatch();
            nthCommand++;
        }
        if (!commandError || _TwitchPlaysExecuteOnInvalid)
        {
            yield return null;
            foreach (WGCTwitchPlaysCommand cmd in buttonsToPress)
                yield return DoInteraction(cmd);
        }
        if (commandError)
        {
            if (Regex.IsMatch(commandName, @"\ba(?:lways)?e(?:xecute)?(?:\s+(on|off))?\b"))
                yield return "sendtochaterror AlwaysExecute cannot be chained with other commands.";
            else
                yield return string.Format("sendtochaterror The {0} command ({1}) is invalid. {2}. {3}.", Ordinals(nthCommand), commandName, _TwitchPlaysExecuteOnInvalid ? "Stop processing commands" : "Aborting every command", animalError ? "At least one of the creatures does not exist on the module. Please ensure that the creature is presented. The valid creatures are Cat, Wolf, Rabbit, Berry, Fish, Dog, Duck, Goat, Fox, Grass, Rice, Mouse, Bear, Cabbage, Chicken, Goose, Corn, Carrot, Horse, Earthworm, Kiwi, and Seeds" : "Valid commands are c(ycle), reset, b(oard)/ab(oard), l(eft)/r(ight), row, and a(lways)e(xecute). Use !{1} help to see the full commands");
        }
    }

    private string Ordinals(uint number)
    {
        switch (number % 100)
        {
            case 11:
            case 12:
            case 13:
                return number + "th";
        }
        switch (number % 10)
        {
            case 1:
                return number + "st";
            case 2:
                return number + "nd";
            case 3:
                return number + "rd";
            default:
                return number + "th";
        }
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        yield return ProcessTwitchCommand("reset");

        List<string> commands = ParseBFSResults();

        yield return ProcessTwitchCommand(string.Join(" | ", commands.ToArray()));
    }

    private List<string> ParseBFSResults()
    {
        List<string> commands = new List<string>();

        int lastIndex = 0;
        foreach (State step in _solution)
        {
            List<string> moveOrder = new List<string>();
            if (step.Movement[0] != "")
            {
                int onBoatToRemoveCount = 0;
                IEnumerable<string> allAnimalsToMove;
                int previousBoatCount = 0;
                if (step.PreviousState.Movement != null)
                {
                    previousBoatCount = step.PreviousState.Movement.Length;
                    var animalsToMoveOffTheBoat = step.PreviousState.Movement.Where(animal => !step.Movement.Contains(animal));
                    onBoatToRemoveCount = animalsToMoveOffTheBoat.Count();
                    allAnimalsToMove = animalsToMoveOffTheBoat.Concat(step.Movement.Where(animal => !step.PreviousState.Movement.Contains(animal)));
                }
                else
                    allAnimalsToMove = step.Movement;
                allAnimalsToMove = allAnimalsToMove.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                moveOrder = Dijkstra(allAnimalsToMove.ToArray(), onBoatToRemoveCount, previousBoatCount, lastIndex);
            }
            else
                moveOrder = Dijkstra(step.PreviousState.Movement, step.PreviousState.Movement.Length, step.PreviousState.Movement.Length, lastIndex);
            lastIndex = Array.IndexOf(_animalOnScreen, moveOrder.Last());
            commands.Add(string.Format("b {0}", string.Join(" ", moveOrder.ToArray())));
            commands.Add("row");
        }

        return commands;
    }

    private List<string> Dijkstra(string[] animalToMove, int onBoatToRemoveCount, int onBoatCount, int lastIndex)
    {
        int[] allIndices = animalToMove.Select(animal => Array.IndexOf(_animalOnScreen, animal)).ToArray();
        MovementState startingState = new MovementState(lastIndex, allIndices, allIndices.Skip(onBoatToRemoveCount).ToArray(), Enumerable.Range(0, animalToMove.Length).Select(x => false).ToArray(), onBoatCount, _boatSize, _animalOnScreen.Length);
        List<MovementState> visited = new List<MovementState>();
        PriorityQueue<MovementState> pQueue = new PriorityQueue<MovementState>();
        pQueue.Enqueue(startingState);
        while (pQueue.Count != 0)
        {
            MovementState currentState = pQueue.Dequeue();
            visited.Add(currentState);
            if (currentState.IsGoal())
            {
                List<string> result = new List<string>();
                MovementState currentLookupState = currentState;
                while (currentLookupState.PreviousState != null)
                {
                    result.Add(_animalOnScreen[currentLookupState.CurrentIndex]);
                    currentLookupState = currentLookupState.PreviousState;
                }
                visited.Clear();
                pQueue.Clear();
                result.Reverse();
                return result;
            }
            foreach (MovementState state in currentState.GetSuccessors())
            {
                if (!visited.Contains(state) && !pQueue.Contains(state))
                    pQueue.Enqueue(state);
            }
        }
        throw new Exception("Couldn't find the short animal movement order.");
    }
}
