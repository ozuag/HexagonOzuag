using System.Collections.Generic;
using HexaFall.Basics;
using UnityEngine;

public class ColorHexagon : HexagonBasics, IColorHexagon
{
    public override HexaType HexagonType => HexaType.ColorHexagon;

    public int ColorId { get; private set; } = int.MinValue;

    public void SetColor(int _colorId)
    {
        this.ColorId = _colorId;

        if (this.spriteRenderer != null)
            this.spriteRenderer.color = HexaFunctions.GetHexaColor(_colorId);
    }

    // bu hexagnonun mevcut konumunda üçlü olma durumu ve eğer üçlü ise patlatıldığında kaç puan gelir
    public int TripletState()
    {
        bool _isTriplet = false; // en az üçlü ise  puan hesaplanacak
        int _totalPoint = 0; // puan

        // bu hexagon ile elde edilebilecek toplam puanı hesaplayacak, komşuların türlerini tut
        Dictionary<HexaType, int> _pointCounter = new Dictionary<HexaType, int>();

        // bütün edge colliderlara git bağlı olduklarının rengine bak
        for (int i = 0; i < this.edgeColliders.Count; i++)
        {
            // kenar aktif değilse atla
            if (this.edgeColliders[i].ActiveEdge == false)
                continue;

            // i. kenarından bağlı olan altıgen
            LinkedObject _firstLinkedObject = this.edgeColliders[i].LinkedHexagon;

            // bağlı olduğu altıgen ile renk uyumu yoksa dikkate alma
            int _firstColorId = (int)_firstLinkedObject.hexagon?.GetHexagonData()?.colorId;

            if (_firstColorId  == this.ColorId)
            {

                LinkedObject _secondLinkedObject = _firstLinkedObject.hexagon.GetEdgeNeighbor((int) _firstLinkedObject.edge, true, true);

                if (_secondLinkedObject != null)
                {
                    // üçlü oluşturuldu, yok edilecekler listesine eklemeye başla

                    if (this.AddOnWantedList()) // KENDİSİNİ EKLE
                    {
                        // listeye eklenebildi, ödülü al
                        if (_pointCounter.ContainsKey(this.HexagonType) == false)
                            _pointCounter.Add(this.HexagonType, 1);
                        else
                            _pointCounter[this.HexagonType]++;
                    }

                    if (_firstLinkedObject.hexagon.AddOnWantedList()) // KOMŞUSUNU EKLE
                    {
                        HexaType _linkedType = _firstLinkedObject.hexagon.HexagonType;

                        if (_pointCounter.ContainsKey(_linkedType) == false)
                            _pointCounter.Add(_linkedType, 1);
                        else
                            _pointCounter[_linkedType]++;
                    }

                    if (_secondLinkedObject.hexagon.AddOnWantedList()) // KOMŞUSUNUN KOMŞUSUNU EKLE
                    {
                        HexaType _linkedType = _secondLinkedObject.hexagon.HexagonType;

                        if (_pointCounter.ContainsKey(_linkedType) == false)
                            _pointCounter.Add(_linkedType, 1);
                        else
                            _pointCounter[_linkedType]++;
                    }

                    _isTriplet = true;
                }

            }
        }


        //  En az üç tane hexa aynı renkte grup olduysa, hepsinin puanını birlikte burada hesapla
        if (_isTriplet)
        {
            // puanı hesapla
            int _nHexagons = 0; // listeye giren hexagonların sayısı, bomba ve yıldızlı olanları da say
            int _multiplier = 1; // yıldızlı/bomba başına *2

            foreach (KeyValuePair<HexaType, int> _keyPair in _pointCounter)
            {
                if (_keyPair.Key != HexaType.NotDefined)
                    _nHexagons += _keyPair.Value;

                if ((_keyPair.Key == HexaType.BombHexagon) | (_keyPair.Key == HexaType.StarredHexagon))
                    _multiplier *= ((int)Mathf.Pow(2, _keyPair.Value));
            }

            _totalPoint = _nHexagons * _multiplier * HexaFunctions.HexaKillPoint;
        }


        _pointCounter.Clear();

        return _totalPoint;
    }

    protected override void Reset()
    {
        base.Reset();

        this.ColorId = int.MinValue;

    }

    public override HexagonData GetHexagonData()
    {
        return new HexagonData((int)this.HexagonType, this.ColorId);
    }

    public override void SetHexagonData(HexagonData _data)
    {
        this.SetColor(_data.colorId);
    }

    protected override void BreakHexagon()
    {
        GameObject _particles = HexaPooling.Instance.PullObject(HexaType.HexaParticles);
        if (_particles != null)
        {
            //_particles.transform.SetParent(this.transform.parent);
            _particles.transform.position = this.transform.position;

            _particles.GetComponent<HexaParticles>()?.SetColor(HexaFunctions.GetHexaColor(this.ColorId));

            _particles.SetActive(true);
        }
    }

}
