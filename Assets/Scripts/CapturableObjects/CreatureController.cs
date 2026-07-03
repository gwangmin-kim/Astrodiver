using UnityEngine;

public class CreatureController : MonoBehaviour, ICapturable
{
    [SerializeField] private CreatureCaptureData _data;

    public void Capture()
    {

    }
}

[System.Serializable]
public struct CreatureCaptureData
{

}
