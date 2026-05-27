using UnityEngine;

public class CameraController
    : MonoBehaviour
{
    private Transform targetPlayer;

    [Header("Follow")]
    public float followSpeed = 10f;

    void LateUpdate()
    {
        if (targetPlayer == null)
        {
            FindLocalPlayer();
            return;
        }

        Vector3 targetPosition =
            targetPlayer.position;

        targetPosition.z = -10f;

        transform.position =
            Vector3.Lerp(
                transform.position,
                targetPosition,
                followSpeed
                * Time.deltaTime
            );
    }

    void FindLocalPlayer()
    {
        PlayerController[] players =
            FindObjectsByType<PlayerController>(
                FindObjectsSortMode.None
            );

        foreach (
            PlayerController player
            in players
        )
        {
            if (
                player.Object != null
                &&
                player.Object
                    .HasInputAuthority
            )
            {
                targetPlayer =
                    player.transform;

                break;
            }
        }
    }
}