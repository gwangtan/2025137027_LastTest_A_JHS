using UnityEngine;
public class Workbench : MonoBehaviour
{
    public AudioClip craftSound;
    public void PlayCraftEffect()
    {
        AudioSource asrc = GetComponent<AudioSource>();
        if (asrc && craftSound) asrc.PlayOneShot(craftSound);
    }
    void Awake() { gameObject.tag = "Workbench"; }
}