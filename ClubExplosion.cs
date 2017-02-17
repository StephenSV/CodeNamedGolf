using UnityEngine;
using System.Collections;

public class ClubExplosion : MonoBehaviour
{
    public float radius = 1.0F;
    public float upForce = 0.02F;
    public float power = 10.0F;

    public MovePlayer movePlayer;
    public AudioClip golftee;

    void Start()
    {
        GetComponent<AudioSource>();
    }

    void OnCollisionEnter(Collision col)
    {
        if (GvrController.ClickButton)
            if (col.gameObject.tag == "GolfBall")

                GetComponent<AudioSource>().PlayOneShot(golftee, 0.8F);

        {
            Vector3 explosionPos = transform.position;
            Collider[] colliders = Physics.OverlapSphere(explosionPos, radius);

            foreach (Collider hit in colliders)
            {
                Rigidbody rb = hit.GetComponent<Rigidbody>();
                Debug.DrawRay(transform.position, hit.transform.position - transform.position, Color.red, 10.0F);

                if (rb != null)
                {
                    rb.AddExplosionForce(power, explosionPos, radius, upForce);
                    Debug.Log(GetComponent<Rigidbody>().velocity);
                }
            }
        }
    }
}