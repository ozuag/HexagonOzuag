using UnityEngine;
using HexaFall.Basics;

[RequireComponent(typeof(Collider2D))]
public class HexaEdgeCollider : MonoBehaviour, IEdgeCollider
{
    public HexagonEdge Edge => (HexagonEdge) this.transform.GetSiblingIndex();
    public LinkedObject LinkedHexagon {get;} = new LinkedObject();
    public void SetColliderEnabledState(bool _state) => this.GetComponent<Collider2D>().enabled = _state;
    public bool ActiveEdge { get; private set;} = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        this.CheckCollision(collision);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {

        // ilgilenmediği bir collider ise geç
        if (collision.gameObject.tag != "HexaEdge")
            return;

        // mevcut bağlı edge ise bağlantıyı kaldır
        if ( collision.gameObject.GetInstanceID() == this.LinkedHexagon.uniqeId)
        {
            this.ActiveEdge = false;

            this.LinkedHexagon.edge = HexagonEdge.NotDefined;
            this.LinkedHexagon.uniqeId = int.MinValue;
            this.LinkedHexagon.hexagon = null;
        }

    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        this.CheckCollision(collision);
    }

    private void CheckCollision(Collider2D collision)
    {
        // zaten aktif ise yeni bir etkileşime izin verme
        if (this.ActiveEdge)
            return;

        IEdgeCollider _iedge = collision.GetComponent<IEdgeCollider>();

        // ilgilenmediği bir collider ise geç
        if (_iedge == null)
            return;

        this.LinkedHexagon.edge = _iedge.Edge;
        this.LinkedHexagon.uniqeId = collision.gameObject.GetInstanceID();
        this.LinkedHexagon.hexagon = collision.gameObject.GetComponentInParent<HexagonBasics>();

        this.ActiveEdge = true;
    }

    
   
    
}
