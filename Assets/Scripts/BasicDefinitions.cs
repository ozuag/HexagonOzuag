using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HexaFall.Basics
{
    public enum SortingLayers
    {
        Bottom,
        Mid,
        Top,
        TopMost
    }

    public enum HexaType
    {
        NotDefined = -666,

        ColorHexagon = 0,
        StarredHexagon,
        BombHexagon,

        HexaParticles = 666
    }

    public enum HexagonEdge
    {
        NotDefined = -666,

        TopRight = 0, // (0 - 60]
        Top = 1, // (60 - 120]
        TopLeft = 2, // (120 - 180]
        BottomLeft = 3, // (180 - 240] 
        Bottom = 4, // (240 - 300] 
        BottomRight = 5, // (300 - 360] 

    }
   
    public class HexaGroup
    {
        public List<Transform> hexagons;
        public Vector2 pivotWPosition;

        public HexaGroup()
        {
            this.hexagons = new List<Transform>();
            this.pivotWPosition = Vector3.zero;
        }

        public void AddHexagon(Transform _transform) => this.hexagons?.Add(_transform);

        public void SetGroup(List<Transform> _hexagons)
        {
            if (this.hexagons == null)
            {
                this.hexagons = new List<Transform>();
            }
            else
            {
                this.SetGroupSorting(SortingLayers.Mid);
                this.hexagons.Clear();
            }

            this.pivotWPosition = Vector2.zero;

            for (int i = 0; i < _hexagons.Count; i++)
                this.pivotWPosition += new Vector2(_hexagons[i].position.x, _hexagons[i].position.y);

            this.pivotWPosition /= _hexagons.Count;

            // giriş değeri olark bu hexagonları ccw yönünde sırala, swap'da bu bilgiyi kullanacağız
            float[] _angles = new float[_hexagons.Count];

            for (int i = 0; i < _hexagons.Count; i++)
            {
                _angles[i] = Vector2.SignedAngle(Vector2.right, new Vector2(_hexagons[i].position.x, _hexagons[i].position.y) - this.pivotWPosition);

                if (_angles[i] < 0)
                    _angles[i] += 360f;
            }

            // saat yönü tersi düzende transformları buraya ekle
            for (int i = 0; i < _hexagons.Count; i++)
            {
                int _minIndex = HexaFunctions.IndexOfMinVal(_angles);

                if (_minIndex < 0)
                    continue;

                this.hexagons.Add(_hexagons[_minIndex]);

                _angles[_minIndex] = int.MaxValue;
            }


            this.SetGroupSorting(SortingLayers.Top);

            _angles = null;
        }

        public void RotateTripleSet(float _angle)
        {
            for (int i = 0; i < this.hexagons.Count; i++)
                this.hexagons[i].RotateAround(this.pivotWPosition, this.hexagons[i].forward, _angle);

        }

        public void SwapParents(bool _ccwOrder)
        {
            int _nHexagons = this.hexagons.Count;

            List<Transform> _initialParents = new List<Transform>();
            for (int i = 0; i < _nHexagons; i++)
                _initialParents.Add(this.hexagons[i].parent);

            int _nextIndexStep = _ccwOrder ? 1 : -1;

            for (int i = 0; i < _nHexagons; i++)
            {
                int _nextIndex = i + _nextIndexStep;

                if (_nextIndex < 0)
                    _nextIndex += _nHexagons;
                else if (_nextIndex >= _nHexagons)
                    _nextIndex -= _nHexagons;

                this.hexagons[i].SetParent(_initialParents[_nextIndex]);

            }

            _initialParents.Clear();



        }

        public void SetGroupEdgeColliders(bool _enabled)
        {
            for (int i = 0; i < this.hexagons.Count; i++)
                this.hexagons[i].GetComponent<HexagonBasics>()?.SetEdgeCollidersEnableState(_enabled); ;
        }

        public int CheckGroupState()
        {
            int _totalPoint = 0;

            for (int i = 0; i < this.hexagons.Count; i++)
            {

                HexagonBasics _hex = this.hexagons[i].GetComponent<HexagonBasics>();
                if (_hex == null)
                    continue;

                int _point = _hex.GroupPoint();

                _totalPoint += _point;

            }

            return _totalPoint;
        }

        private void SetGroupSorting(SortingLayers _sortingLayer)
        {
            if (this.hexagons == null)
                return;

            foreach (Transform _tr in this.hexagons)
            {
                SortingGroup _sg = _tr.GetComponent<SortingGroup>();

                if (_sg != null)
                    _sg.sortingLayerName = _sortingLayer.ToString();

            }

        }

        public void Ungroup()
        {
            if (this.hexagons != null)
            {
                this.SetGroupSorting(SortingLayers.Mid);

                this.hexagons.Clear();

            }

            this.pivotWPosition = Vector2.zero;


        }

        ~HexaGroup()
        {
            if (this.hexagons != null)
            {
                this.Ungroup();
                this.hexagons.Clear();
            }

        }

    }

    public class LinkedObject
    {
        public HexagonEdge edge;
        public int uniqeId;
        public HexagonBasics hexagon;

        public LinkedObject(HexagonEdge _edge = HexagonEdge.NotDefined, HexagonBasics _hexagon = null, int _uniqeId = int.MinValue)
        {
            this.edge = _edge;
            this.hexagon = _hexagon;
            this.uniqeId = _uniqeId;
        }
    }

    public static class HexaFunctions
    {
        public static Action DestroyWantedList; // Arananlar listesindekileri yok et
        public static Action KillAllHexagons; // bütün hexagonları yok et
        public static Action HexagonMoved;

        public static Action<string> GameOver;

        public const int HexaKillPoint = 5;
        public const int LevelUpdatePoint = 1000;

        public static int IndexOfMinVal(float[] _values)
        {

            int _index = -1;

            float _tempVal = int.MaxValue;

            for (int i = 0; i < _values.Length; i++)
            {
                if (_tempVal > _values[i])
                {
                    _tempVal = _values[i];
                    _index = i;
                }

            }

            return _index;
        }

        private static List<Color> HexaColors = new List<Color>();

        public static void SetHexaColor(int _nColors = 5)
        {
            float _s = 1f;
            float _v = 1f;

            float _hStep = 1f / _nColors;

            HexaColors.Clear();

            for (float _h = 0f; _h < 1.0f; _h += _hStep)
                HexaColors.Add(Color.HSVToRGB(_h, _s, _v));
        }

        public static Color GetHexaColor(int _index)
        {
            _index = Mathf.Clamp(_index, 0, HexaColors.Count - 1);
            return HexaColors[_index];
        }

        public static int GetRandomColorIndex => UnityEngine.Random.Range(0, HexaColors.Count);

        // seçili renk havuzunu rastgele 2 farklı kümeye böler
        public static void RandomColorGroups(out List<int> _group1, out List<int> _group2)
        {
            _group1 = new List<int>();
            _group2 = new List<int>();

            for (int i = 0; i < HexaColors.Count; i++)
                _group1.Add(i);

            int _nGroup2 = HexaColors.Count / 2;

            for (int i = 0; i < _nGroup2; i++)
            {
                int _index = UnityEngine.Random.Range(0, _group1.Count);
                _group2.Add(_group1[_index]);

                _group1.RemoveAt(_index);
            }


        }

    }

    // oyun verileri kaydedilirken kullanılır
    [Serializable]
    public class HexagonData
    {
        public int type;
        public int colorId;
        public int parameter1; // rn: bomba sayaç

        public HexagonData(int _type = int.MinValue, int _color = int.MinValue, int _parameter1 = int.MinValue)
        {
            this.type = _type;
            this.colorId = _color;
            this.parameter1 = _parameter1;
        }
    }

    [Serializable]
    public class GameData
    {
        public int score;
        public int moveCount;

        public int nVerticalHexagons;
        public int nHorizontalHexagons;

        public int nDefinedColors;

        public List<HexagonData> hexagons;
    }

}