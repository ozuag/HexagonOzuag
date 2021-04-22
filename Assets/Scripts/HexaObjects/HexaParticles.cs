using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class HexaParticles : MonoBehaviour
{

    public void SetColor(Color _color)
    {
        ParticleSystem _particle = this.GetComponent<ParticleSystem>();

        if (_particle == null)
            return;

        var _main = _particle.main;
        _main.startColor = _color;


    }


    public void OnParticleSystemStopped() => HexaPooling.Instance.PushObject(HexaFall.Basics.HexaType.HexaParticles, this.gameObject);

    
}
