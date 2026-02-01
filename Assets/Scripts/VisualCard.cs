using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisualCard : MonoBehaviour
{
    private Vector3 nextPos;

    public float smoothSpeed = 0.125f;
    private Vector3 velocity = Vector3.zero;

    private void Update()
    {
        if (transform.position == nextPos)
        {
            return;
        }

        CardSmoothFollow(nextPos);
    }
    public void SetNewPosition(Vector3 newPos)
    {
        nextPos = newPos;
    }

    private void CardSmoothFollow(Vector3 newPos)
    {
        Vector3 desiredPositon = newPos;
        Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, desiredPositon, ref velocity, smoothSpeed);
        transform.position = smoothedPosition;
    }
}
