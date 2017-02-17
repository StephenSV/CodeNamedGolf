using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class PlayerShooting2 : MonoBehaviour
{
    public int damagePerShot = 20;                  // The damage inflicted by each bullet.
    public float timeBetweenBullets = 0.15f;        // The time between each shot.
    public float range = 100f;                      // The distance the gun can fire.

    //------------------------------
    public int clips = 0; // how many clips you have
    public int bulletsPerClip = 0; // how many bullets per clip
    public int bullets = 0; // start with a brand new clip in the gun
    public float reloadTime = 0.5f; // reload time in seconds
    public AudioSource reloadSound; // Reference to the reload audio source.
    public AudioSource clickSound; // optional "no bullets" click sound
    public Text bulletText;
    public Text clipText;
    //------------------------------
    public UnityEngine.UI.Slider ammoBar;


    float timer;                                    // A timer to determine when to fire.
    Ray shootRay;                                   // A ray from the gun end forwards.
    RaycastHit shootHit;                            // A raycast hit to get information about what was hit.
    int shootableMask;                              // A layer mask so the raycast only hits things on the shootable layer.
    ParticleSystem gunParticles;                    // Reference to the particle system.
    LineRenderer gunLine;                           // Reference to the line renderer.
    AudioSource gunAudio;                           // Reference to the shoot audio source.
    Light gunLight;                                 // Reference to the light component.
    float effectsDisplayTime = 0.2f;                // The proportion of the timeBetweenBullets that the effects will display for.



    void Awake()
    {
        // Create a layer mask for the Shootable layer.
        shootableMask = LayerMask.GetMask("Shootable");

        // Set up the references.
        gunParticles = GetComponent<ParticleSystem>();
        gunLine = GetComponent<LineRenderer>();
        gunAudio = GetComponent<AudioSource>();
        gunLight = GetComponent<Light>();

        ammoBar.value = bullets / bulletsPerClip;

    }

    void Start()
    {

    }

    void Update()
    {

        // Add the time since Update was last called to the timer.
        timer += Time.deltaTime;

        //If the Fire1 button is being press and it's time to fire...
        if ((GvrController.ClickButton) && timer >= timeBetweenBullets)

            if (bullets > 0)
            { // and you have bullets...
                Fire(); // shoot
                bullets -= 1;
            }
            else // but if gun empty... 
                 if (clips > 0)
            { // and still have ammo clips...
                StartCoroutine(Reload()); // start reload routine
            }
            else // no bullets, no clips:
                 if (clickSound)
            { // play a click sound, if desired
                clickSound.Play();
            }

        // diplay number of bulets left 
        if (bulletText != null)
        {
            bulletText.text = "AMMO: " + bullets.ToString();
        }

        ammoBar.value = (float)bullets / (float)bulletsPerClip;

        // If the timer has exceeded the proportion of timeBetweenBullets that the effects should be displayed for...
        if (timer >= timeBetweenBullets * effectsDisplayTime)
        {
            // ... disable the effects.
            DisableEffects();
        }
    }

    public void DisableEffects()
    {
        // Disable the line renderer and the light.
        gunLine.enabled = false;
        gunLight.enabled = false;
    }

    void Fire()
    {
        // Reset the timer.
        timer = 0f;

        // Play the gun shot audioclip.
        gunAudio.Play();

        // Enable the light.
        gunLight.enabled = true;

        // Stop the particles from playing if they were, then start the particles.
        gunParticles.Stop();
        gunParticles.Play();

        // Enable the line renderer and set it's first position to be the end of the gun.
        gunLine.enabled = true;
        gunLine.SetPosition(0, transform.position);

        // Set the shootRay so that it starts at the end of the gun and points forward from the barrel.
        shootRay.origin = transform.position;
        shootRay.direction = transform.forward;

        //------------------------------
        //------------------------------

        // Perform the raycast against gameobjects on the shootable layer and if it hits something...
        if (Physics.Raycast(shootRay, out shootHit, range, shootableMask))
        {
            // Try and find an EnemyHealth script on the gameobject hit.
            EnemyHealth2 enemyHealth2 = shootHit.collider.GetComponent<EnemyHealth2>();
//---->            AiHealth AiHealth = shootHit.collider.GetComponent<AiHealth>();
            Debug.Log("hit something...");

            // If the EnemyHealth component exist...
//---->           if (AiHealth != null)
            {
                // ... the enemy should take damage.
                enemyHealth2.TakeDamage(damagePerShot, shootHit.point);
//---->                AiHealth.TakeDamage(damagePerShot, shootHit.point);
                Debug.Log("Enemy Damaged");
            }


            // Set the second position of the line renderer to the point the raycast hit.
            gunLine.SetPosition(1, shootHit.point);
        }
        // If the raycast didn't hit anything on the shootable layer...
        else
        {
            // ... set the second position of the line renderer to the fullest extent of the gun's range.
            gunLine.SetPosition(1, shootRay.origin + shootRay.direction * range);
        }
    }
   

    




    private bool reloading = false; // is true while reloading, false otherwise

    IEnumerator Reload()
    {
        // abort other Reload calls if already reloading:
        if (reloading) yield return null;

        reloading = true; // signals that is currently reloading
        clips -= 1; // got one clip, decrement clip count:
        Debug.Log("clip -1");
        reloadSound.Play(); // play the reload sound
        yield return new WaitForSeconds(reloadTime); // wait the reload time
        bullets = bulletsPerClip; // now the bullets are available
        reloading = false; // reloading finished

        //display clip size in text
        if (clipText != null)
        {
            clipText.text = "CLIP: " + clips.ToString();
        }
    }
}
