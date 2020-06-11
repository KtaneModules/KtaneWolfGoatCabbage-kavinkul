using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;
using WolfGoatCabbage;
using KModkit;

public class WolfGoatCabbageScript : MonoBehaviour 
{
    public KMBombModule Module;
    public KMAudio Audio;
    public GameObject[] Images;
    public GameObject[] Buttons;
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
            Buttons[j].GetComponent<KMSelectable>().OnInteract += delegate
            {
                CycleButtonPress(j);
                return false;
            };
        }
        Buttons[2].GetComponent<KMSelectable>().OnInteract += delegate
        {
            AboardButtonPress();
            return false;
        };
        Buttons[3].GetComponent<KMSelectable>().OnInteract += delegate
        {
            RowButtonPress();
            return false;
        };
        Buttons[4].GetComponent<KMSelectable>().OnInteract += delegate
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
        StartCoroutine(ButtonAnimation(i));
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
                Debug.LogFormat("[Wolf, Goat, and Cabbage #{0}] Moving {1} to the other side of the river.", _moduleID, String.Join(", ", _onBoat.Select((s2, index) => index == _onBoat.Count - 1 && _onBoat.Count != 1 ? "and " + s2.ToLowerInvariant() : s2.ToLowerInvariant()).ToArray()));
            List<string> animalOnNewShore = _onStartingShore ? _startShore.ToList() : _finalShore.ToList();
            animalOnNewShore.AddRange(_onBoat);
            if (_onStartingShore)
            {
                Debug.LogFormat("[Wolf, Goat, and Cabbage #{0}] The starting side of the river now has {1}.", _moduleID, animalOnNewShore.Count == 0 ? "nothing" : String.Join(animalOnNewShore.Count == 2 ? " " : ", ", animalOnNewShore.Select((s2, index) => index == animalOnNewShore.Count - 1 && animalOnNewShore.Count != 1 ? "and " + s2.ToLowerInvariant() : s2.ToLowerInvariant()).ToArray()));
                Debug.LogFormat("[Wolf, Goat, and Cabbage #{0}] The finishing side of the river now has {1}.", _moduleID, _finalShore.Count == 0 ? "nothing" : String.Join(_finalShore.Count == 2 ? " " : ", ", _finalShore.Select((s2, index) => index == _finalShore.Count - 1 && _finalShore.Count != 1 ? "and " + s2.ToLowerInvariant() : s2.ToLowerInvariant()).ToArray()));
            }
            else
            {
                Debug.LogFormat("[Wolf, Goat, and Cabbage #{0}] The starting side of the river now has {1}.", _moduleID, _startShore.Count == 0 ? "nothing" : String.Join(_startShore.Count == 2 ? " " : ", ", _startShore.Select((s2, index) => index == _startShore.Count - 1 && _startShore.Count != 1 ? "and " + s2.ToLowerInvariant() : s2.ToLowerInvariant()).ToArray()));
                Debug.LogFormat("[Wolf, Goat, and Cabbage #{0}] The finishing side of the river now has {1}.", _moduleID, animalOnNewShore.Count == 0 ? "nothing" : String.Join(animalOnNewShore.Count == 2 ? " " : ", ", animalOnNewShore.Select((s2, index) => index == animalOnNewShore.Count - 1 && animalOnNewShore.Count != 1 ? "and " + s2.ToLowerInvariant() : s2.ToLowerInvariant()).ToArray()));
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
        _onStartingShore = true;
        Images[Array.IndexOf(_creaturesList, _animalOnScreen[_currentAnimal])].SetActive(false);
        _currentAnimal = 0;
        Images[Array.IndexOf(_creaturesList, _animalOnScreen[_currentAnimal])].SetActive(true);
    }
    private IEnumerator ButtonAnimation(int i)
    {
        _buttonAnimation[i] = true;
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        Buttons[i].GetComponent<KMSelectable>().Highlight.transform.localPosition -= new Vector3(0, 0, .05f);
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
        Buttons[i].GetComponent<KMSelectable>().Highlight.transform.localPosition += new Vector3(0, 0, .05f);
        _buttonAnimation[i] = false;
    }
    private void Generate()
    {
        int animalCount = Rnd.Range(6, 10);
        while (_startShore.Count != animalCount)
        {
            int animal = Rnd.Range(0, _creaturesList.Length);
            if (!_graph.CreateNode(_creaturesList[animal], _conflictList[_creaturesList[animal]])) continue;
            _startShore.Add(_creaturesList[animal]);
        }
        _animalOnScreen = _startShore.ToArray();
        Debug.LogFormat("[Wolf, Goat, and Cabbage #{0}] The creatures that you have are {1}.", _moduleID, String.Join(", ", _animalOnScreen.Select((x, index) => index == _animalOnScreen.Length - 1 ? "and " + x.ToLowerInvariant() : x.ToLowerInvariant()).ToArray()));
        _graph.GenerateConnectingEdge();
        Debug.LogFormat("[Wolf, Goat, and Cabbage #{0}] In this group, the conflicts for each creature are the following:", _moduleID);
        bool listExist = false;
        _animalOnScreen.ToList().ForEach(s =>
        {
            List<string> item = _animalOnScreen.Where(x => s != x && _graph.ContainEdge(s, x)).ToList();
            if (item.Count != 0)
            {
                Debug.LogFormat("[Wolf, Goat, and Cabbage #{0}] {1}: {2}", _moduleID, s, String.Join(item.Count == 2 ? " " : ", ", item.Select((s2, index) => index == item.Count - 1 && item.Count != 1 ? "and " + s2 : s2).ToArray()));
                listExist = true;
            }
        });
        if (!listExist)
            Debug.LogFormat("[Wolf, Goat, and Cabbage #{0}] Wait a minute. There is no conflict.", _moduleID);
        _graph.BoundSearchTree(animalCount);
        _boatSize = _graph.AlcuinNumber();
        Debug.LogFormat("[Wolf, Goat, and Cabbage #{0}] The Alcuin number for this group is {1}.", _moduleID, _boatSize);
        Images[Array.IndexOf(_creaturesList, _animalOnScreen[_currentAnimal])].SetActive(true);
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = "Use !{0} c/cycle to cycle for creatures. Use !{0} reset to reset the module. Use !{0} board/aboard <creature> to board the specified creatures. The module will press aboard button if <creatures> is empty. Use !{0} left/right <1-digit number> to press the left or right button that many times. Use !{0} row to press row button.";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.Trim().ToLowerInvariant();
        Match m = Regex.Match(command, @"^(?:(c|cycle)|(reset)|(a?board)((?: .+)*)|(left|right) (\d)|row)$");
        if (m.Success)
        {
            if (m.Groups[1].Success)
            {
                yield return null;
                for (int i = 0; i < _animalOnScreen.Length; i++)
                {
                    yield return new WaitUntil(() => !_buttonAnimation[1]);
                    Buttons[1].GetComponent<KMSelectable>().OnInteract();
                    yield return new WaitForSeconds(2);
                    yield return "trycancel";
                }
            }
            else if (m.Groups[2].Success)
            {
                yield return null;
                yield return new WaitUntil(() => !_buttonAnimation[4]);
                Buttons[4].GetComponent<KMSelectable>().OnInteract();
                yield return new WaitForSeconds(.1f);
            }
            else if (m.Groups[3].Success)
            {
                if (!m.Groups[4].Success)
                {
                    yield return null;
                    yield return new WaitUntil(() => !_buttonAnimation[2]);
                    Buttons[2].GetComponent<KMSelectable>().OnInteract();
                    yield return new WaitForSeconds(.1f);
                }
                else
                {
                    string[] creatures = m.Groups[4].Value.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (creatures.Any(c => !_animalOnScreen.Select(x => x.ToLowerInvariant()).ToArray().Contains(c)))
                    {
                        yield return "sendtochaterror At least one of the creature does not exist on the module. Please ensure that the creature is presented.";
                        yield break;
                    }
                    yield return null;
                    for (int i = 0; i < creatures.Length; i++)
                    {
                        while (_animalOnScreen[_currentAnimal].ToLowerInvariant() != creatures[i])
                        {
                            yield return new WaitUntil(() => !_buttonAnimation[1]);
                            Buttons[1].GetComponent<KMSelectable>().OnInteract();
                            yield return new WaitForSeconds(.1f);
                            yield return "trycancel";
                        }
                        yield return new WaitUntil(() => !_buttonAnimation[2]);
                        Buttons[2].GetComponent<KMSelectable>().OnInteract();
                        yield return new WaitForSeconds(.1f);
                        yield return "trycancel";
                    }
                }
            }
            else if (m.Groups[5].Success)
            {
                yield return null;
                int buttonIndex = m.Groups[5].Value == "left" ? 0 : 1;
                for (int i = 0; i < int.Parse(m.Groups[6].Value) % _animalOnScreen.Length; i++)
                {
                    yield return new WaitUntil(() => !_buttonAnimation[buttonIndex]);
                    Buttons[buttonIndex].GetComponent<KMSelectable>().OnInteract();
                    yield return new WaitForSeconds(.1f);
                    yield return "trycancel";
                }
            }
            else
            {
                yield return null;
                yield return new WaitUntil(() => !_buttonAnimation[3]);
                Buttons[3].GetComponent<KMSelectable>().OnInteract();
                yield return new WaitForSeconds(.1f);
            }
        }
        else
            yield return "sendtochaterror Valid commands are c/cycle, reset, board/aboard, left/right, and row. Use !{1} help to see the full command.";
        yield break;
    }
}
