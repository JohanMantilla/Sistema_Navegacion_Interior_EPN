using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public AudioSource audioSource;
    public int conditional = 5;
    private bool hasPlayed = false;

    void Start()
    {


    }

    // Update is called once per frame
    void Update()
    {

    }

    public void x(int ValueToCompare) {
        if (ValueToCompare == conditional && hasPlayed)
        {
            PlayAudio();
            hasPlayed = true;
        }
        else if (ValueToCompare != null) {
            hasPlayed = false;
        }

    }


    public void PlayAudio(){
        audioSource.Play();
    }

    private void Stop() { 
        audioSource.Stop();
    }



}
