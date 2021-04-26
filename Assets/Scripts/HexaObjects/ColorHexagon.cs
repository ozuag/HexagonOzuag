using System.Collections.Generic;
using HexaFall.Basics;
using UnityEngine;

public class ColorHexagon : BasicHexagon, IColorHexagon
{
    protected int colorId = int.MinValue; // renk id, renk karşılaştırmalarda kullanılır

    public void SetColor(int _colorId)
    {
        this.colorId = _colorId;

        if (this.spriteRenderer != null)
            this.spriteRenderer.color = HexaGridSystem.Instance.GetColor(_colorId);
    }

    public int GetColorId()
    {
        return this.colorId;
    }

    // bu hexagnonun mevcut konumumda üçlü olma durumu ve eğer üçlü ise patlatıldığında kaç puan gelir
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

            if (_firstColorId  == this.colorId)
            {

                LinkedObject _secondLinkedObject = _firstLinkedObject.hexagon.GetEdgeNeighbor((int) _firstLinkedObject.edge, true, true);

                if (_secondLinkedObject != null)
                {
                    // üçlü oluşturuldu, yok edilecekler listesine eklemeye başla

                    if (this.AddOnWantedList()) // KENDİSİNİ EKLE
                    {
                        // listeye eklenebildi, ödülü al
                        if (_pointCounter.ContainsKey(this.hexaType) == false)
                            _pointCounter.Add(this.hexaType, 1);
                        else
                            _pointCounter[this.hexaType]++;
                    }

                    if (_firstLinkedObject.hexagon.AddOnWantedList()) // KOMŞUSUNU EKLE
                    {
                        HexaType _linkedType = _firstLinkedObject.hexagon.GetHexaType;

                        if (_pointCounter.ContainsKey(_linkedType) == false)
                            _pointCounter.Add(_linkedType, 1);
                        else
                            _pointCounter[_linkedType]++;
                    }

                    if (_secondLinkedObject.hexagon.AddOnWantedList()) // KOMŞUSUNUN KOMŞUSUNU EKLE
                    {
                        HexaType _linkedType = _secondLinkedObject.hexagon.GetHexaType;

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

        this.colorId = int.MinValue;

    }

    public override HexagonData GetHexagonData()
    {
        return new HexagonData((int)this.hexaType, this.colorId);
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

            _particles.GetComponent<HexaParticles>()?.SetColor(HexaGridSystem.Instance.GetColor(this.colorId));

            _particles.SetActive(true);
        }
    }

}
