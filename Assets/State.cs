using System;
using System.Collections.Generic;
using System.Linq;

namespace WolfGoatCabbage
{

    public sealed class State : IEquatable<State>
    {
        public VertexCoverGraph Graph { get; private set; }
        public HashSet<string> OnStartingShore { get; private set; }
        public HashSet<string> OnFinishingShore { get; private set; }
        public int AlcuinNumber { get; private set; }
        public bool IsFinishShore { get; private set; }
        public string[] Movement { get; private set; }

        public State PreviousState { get; private set; }

        public State(VertexCoverGraph graph, HashSet<string> onStartingShore, HashSet<string> onFinishingShore, int alcuinNumber, bool isFinishShore, State previousState = null, string[] movement = null)
        {
            Graph = graph;
            OnStartingShore = new HashSet<string>(onStartingShore);
            OnFinishingShore = new HashSet<string>(onFinishingShore);
            AlcuinNumber = alcuinNumber;
            IsFinishShore = isFinishShore;
            PreviousState = previousState;
            Movement = movement;
        }
        public State(VertexCoverGraph graph, string[] onStartingShore, string[] onFinishingShore, int alcuinNumber, bool isFinishShore) :
            this(graph, new HashSet<string>(onStartingShore), new HashSet<string>(onFinishingShore), alcuinNumber, isFinishShore)
        {
        }

        public List<State> GetSuccessors()
        {
            List<State> list = new List<State>();
            if (!IsFinishShore)
            {
                if (!OnStartingShore.Any(s => OnStartingShore.Where(x => s != x).Any(s2 => Graph.ContainEdge(s, s2))))
                    list.Add(new State(Graph, OnStartingShore, OnFinishingShore, AlcuinNumber, !IsFinishShore, this, new[] { "" }));
                for (int i = 0; i < AlcuinNumber; i++)
                {
                    var someCombinations = OnStartingShore.Combinations(i + 1);
                    foreach (var item in someCombinations)
                    {
                        HashSet<string> set1 = new HashSet<string>(OnStartingShore);
                        set1.ExceptWith(item);
                        if (set1.Any(s => set1.Where(x => s != x).Any(s2 => Graph.ContainEdge(s, s2))))
                            continue;

                        HashSet<string> set2 = new HashSet<string>(OnFinishingShore);
                        set2.UnionWith(item);
                        list.Add(new State(Graph, set1, set2, AlcuinNumber, !IsFinishShore, this, item.ToArray()));
                    }
                }
            }
            else
            {
                if (!OnFinishingShore.Any(s => OnFinishingShore.Where(x => s != x).Any(s2 => Graph.ContainEdge(s, s2))))
                    list.Add(new State(Graph, OnStartingShore, OnFinishingShore, AlcuinNumber, !IsFinishShore, this, new[] { "" }));
                for (int i = 0; i < AlcuinNumber; i++)
                {
                    var someCombinations = OnFinishingShore.Combinations(i + 1);
                    foreach (var item in someCombinations)
                    {
                        HashSet<string> set1 = new HashSet<string>(OnFinishingShore);
                        set1.ExceptWith(item);
                        if (set1.Any(s => set1.Where(x => s != x).Any(s2 => Graph.ContainEdge(s, s2))))
                            continue;

                        HashSet<string> set2 = new HashSet<string>(OnStartingShore);
                        set2.UnionWith(item);
                        list.Add(new State(Graph, set2, set1, AlcuinNumber, !IsFinishShore, this, item.ToArray()));
                    }
                }
            }
            return list;
        }

        public bool IsGoal()
        {
            return IsFinishShore && OnStartingShore.Count == 0;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !GetType().Equals(obj.GetType()))
                return false;

            return Equals((State)obj);
        }

        public bool Equals(State otherState)
        {
            return Graph == otherState.Graph &&
                OnStartingShore.SetEquals(otherState.OnStartingShore) &&
                OnFinishingShore.SetEquals(otherState.OnFinishingShore) &&
                AlcuinNumber == otherState.AlcuinNumber &&
                IsFinishShore == otherState.IsFinishShore;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 31;
                hash = hash * 47 + Graph.GetHashCode();
                foreach (string animal in OnStartingShore)
                    hash = hash * 11 + animal.GetHashCode();
                foreach (string animal in OnFinishingShore)
                    hash = hash * 37 + animal.GetHashCode();
                hash = hash * 59 + AlcuinNumber;
                hash = hash * 13 + IsFinishShore.GetHashCode();
                return hash;
            }
        }
    }

    public sealed class MovementState : IEquatable<MovementState>, IComparable<MovementState>
    {
        public MovementState PreviousState { get; private set; }

        public int CurrentIndex { get; private set; }

        private int[] _allIndices;
        private int[] _indexNotOnBoat;
        private int _totalSteps;
        private bool[] _isSelected;
        private int _currentSpaceTaken;
        private int _alcuinNumber;
        private int _totalAnimals;

        public MovementState(int currentIndex, int[] allIndices, int[] indexNotOnBoat, bool[] isSelected, int currentSpaceTaken, int alcuinNumber, int totalAnimals, MovementState previousState = null, int totalSteps = 0)
        {
            CurrentIndex = currentIndex;
            _allIndices = allIndices;
            _indexNotOnBoat = indexNotOnBoat;
            _isSelected = isSelected;
            _currentSpaceTaken = currentSpaceTaken;
            _alcuinNumber = alcuinNumber;
            _totalAnimals = totalAnimals;
            if (_allIndices.Length != _isSelected.Length)
                throw new Exception("_allIndices and _isSelected must have the same length.");
            PreviousState = previousState;
            _totalSteps = totalSteps;
        }
        
        public List<MovementState> GetSuccessors()
        {
            List<MovementState> list = new List<MovementState>();
            for (int i = 0; i < _allIndices.Length; i++)
            {
                if (_isSelected[i])
                    continue;
                int nextIndex = _allIndices[i];
                int newSpaceTaken = _indexNotOnBoat.Contains(nextIndex) ? _currentSpaceTaken + 1 : _currentSpaceTaken - 1;
                if (_currentSpaceTaken > _alcuinNumber)
                    continue;
                int stepCounts = Math.Min((nextIndex - CurrentIndex + _totalAnimals) % _totalAnimals, (CurrentIndex - nextIndex + _totalAnimals) % _totalAnimals);
                bool[] newIsSelected = _isSelected.ToArray();
                newIsSelected[i] = true;
                list.Add(new MovementState(nextIndex, _allIndices, _indexNotOnBoat, newIsSelected, newSpaceTaken, _alcuinNumber, _totalAnimals, this, _totalSteps + stepCounts));
            }
            return list;
        }

        public bool IsGoal()
        {
            return _isSelected.All(x => x);
        }

        public int CompareTo(MovementState otherState)
        {
            if (otherState == null)
                return 1;

            return _totalSteps.CompareTo(otherState._totalSteps);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !GetType().Equals(obj.GetType()))
                return false;

            return Equals((MovementState) obj);
        }

        public bool Equals(MovementState otherState)
        {
            return CurrentIndex == otherState.CurrentIndex &&
                   _allIndices.SequenceEqual(otherState._allIndices) &&
                   _indexNotOnBoat.SequenceEqual(otherState._indexNotOnBoat) &&
                   _isSelected.SequenceEqual(otherState._isSelected) &&
                   _currentSpaceTaken == otherState._currentSpaceTaken &&
                   _alcuinNumber == otherState._alcuinNumber &&
                   _totalAnimals == otherState._totalAnimals &&
                   _totalSteps == otherState._totalSteps;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 31;
                hash = hash * 47 + CurrentIndex;
                foreach (int index in _allIndices)
                    hash = hash * 11 + index;
                foreach (int index in _indexNotOnBoat)
                    hash = hash * 17 + index;
                foreach (bool selected in _isSelected)
                    hash = hash * 13 + selected.GetHashCode();
                hash = hash * 19 + _currentSpaceTaken;
                hash = hash * 23 + _alcuinNumber;
                hash = hash * 67 + _totalAnimals;
                hash = hash * 73 + _totalSteps;
                return hash;
            }
        }
    }
}