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
}