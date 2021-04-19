using System.Collections.Generic;
using UnityEngine;
using HexaFall.Basics;


public class HexaPooling : MonoBehaviour
{
    
    [SerializeField] private int poolMaxCapacity = 666;
    public int SetPoolCapacity { set { this.poolMaxCapacity = value; } }

    // bir birim üretilebilecekse onun master prefabını buraya ekle, üretilemeyecekse buradan sil 
    private Dictionary<HexaType, GameObject> masterObjects = new Dictionary<HexaType, GameObject>();

    // oyun içinde üretilen tüm birimler burada toplansın
    private Dictionary<HexaType, Queue<GameObject>> hexfallUnits = new Dictionary<HexaType, Queue<GameObject>>();

    public static HexaPooling Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }
    

    private void OnDestroy()
    {
        foreach(KeyValuePair<HexaType, Queue<GameObject>> _pair in this.hexfallUnits)
        {
            if (_pair.Value != null)
                _pair.Value.Clear();
        }

        this.hexfallUnits.Clear();
        this.masterObjects.Clear();
    }

    public GameObject PullObject(HexaType _unit = HexaType.NotDefined)
    {

        if(_unit == HexaType.NotDefined)
        {
            // tanımsız geldiyse normal ya da yıldızlı getir, %13 ihtimalle yıldızlı gelir
            if (Random.Range(0f, 1f) < 0.13f)
                _unit = HexaType.StarredHexagon;
            else
                _unit = HexaType.Hexagon;
        }

        // eğer istenen nensenin master'ı yoksa önce master'ı oluştur
        if (this.masterObjects.ContainsKey(_unit) == false)
            this.CreateMasterObject(_unit);

        if (this.masterObjects[_unit] == null)
            return null;

        // bu birim için havuz yoksa, havuz oluştur
        if (this.hexfallUnits.ContainsKey(_unit) == false)
            this.hexfallUnits.Add(_unit, new Queue<GameObject>());

        // ilgili havuzda bu birimden yoksa/kalmadıysa masterdan kopyala
        if (this.hexfallUnits[_unit].Count == 0)
        {
            if (!this.CreateNewUnit(_unit))
                return null;
        }

        return this.hexfallUnits[_unit].Dequeue();
    }

    public void PushObject(HexaType _unit, GameObject _object)
    {
        _object.SetActive(false);

        // bu birim için havuz yoksa, havuz oluştur
        if (this.hexfallUnits.ContainsKey(_unit) == false)
            this.hexfallUnits.Add(_unit, new Queue<GameObject>());

        // eğer havuzun boyutu çok fazla ilse artık havuza alma, bu nesneyi sil
        if(this.hexfallUnits[_unit].Count < this.poolMaxCapacity)
        {
            _object.transform.SetParent(this.transform);
            _object.transform.localPosition = Vector3.zero;
            _object.transform.localEulerAngles = Vector3.zero;

            this.hexfallUnits[_unit].Enqueue(_object);
        }
        else
        {
            GameObject.Destroy(_object);
        }


    }

    private bool CreateNewUnit(HexaType _unit)
    {
        GameObject _obj = GameObject.Instantiate(this.masterObjects[_unit]);

        if (_obj == null)
            return false;

        _obj.name += _unit.ToString() + "_ID(" + _obj.GetInstanceID().ToString()+ ")";

        _obj.SetActive(false);

        _obj.transform.SetParent(this.transform);

        this.hexfallUnits[_unit].Enqueue(_obj);

        return true;

    }

    private void CreateMasterObject(HexaType _unit)
    {

        string _masterPath = "Prefabs" + System.IO.Path.DirectorySeparatorChar +
                                    _unit.ToString();

        this.masterObjects.Add( _unit, Resources.Load<GameObject>(_masterPath)  );

    }

}
