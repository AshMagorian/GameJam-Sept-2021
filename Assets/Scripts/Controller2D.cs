using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Controller2D : MonoBehaviour
{
    public LayerMask collisionMask;

    const float skinWidth = 0.015f;
    public int horizontalRayCount = 4;
    public int verticalRayCount = 4;

    float horizontalRaySpacing;
    float verticalRaySpacing;

    public BoxCollider2D collider;
    RaycastOrigins raycastOrigins;
    public CollisionInfo collisions;

    private void Awake()
    {
        collider = GetComponent<BoxCollider2D>();
    }

    void Start()
    {
        CalculateRaySpacing();
        collisions.faceDir = 1;
    }

    public void Move(Vector2 deltaMovement)
    {
        UpdateRaycastOrigins();
        collisions.Reset();

        //Collisions happen here
        int oldFaceDir = collisions.faceDir;
        if (deltaMovement.x != 0)
        {
            collisions.faceDir = (int)Mathf.Sign(deltaMovement.x);
        }

        HorizontalCollisions(ref deltaMovement);
        if (deltaMovement.y != 0)
        {
            VerticalCollisions(ref deltaMovement);
        }

        transform.Translate(deltaMovement);

        if (oldFaceDir != collisions.faceDir) 
        {
            // Flip the character if there is a change in direction
            Flip();
        }
    }

    void HorizontalCollisions(ref Vector2 deltaMovement)
    {
        // directionX = -1 if moving left, 1 if moving right
        float directionX = collisions.faceDir;
        float rayLength = Mathf.Abs(deltaMovement.x) + skinWidth;
        // Sets the ray length to a value even if the x velocity = 0
        if (Mathf.Abs(deltaMovement.x) < skinWidth)
        {
            rayLength = 2 * skinWidth;
        }

        for (int i = 0; i < horizontalRayCount; i++)
        {
            // If moving left, the ray origin is the bottom left. If moving right the origin is bottom right
            Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
            // Cycle through all of the horizontal rays
            rayOrigin += Vector2.up * (horizontalRaySpacing * i);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            if (hit)
            {
                deltaMovement.x = (hit.distance - skinWidth) * directionX;
                rayLength = hit.distance;

                collisions.left = directionX == -1;
                collisions.right = directionX == 1;
            }

            Debug.DrawRay(rayOrigin, Vector2.right * directionX * rayLength, Color.red);
        }
    }

    void VerticalCollisions(ref Vector2 deltaMovement)
    {
        // directionY = -1 if moving downwards, 1 if moving upwards
        float directionY = Mathf.Sign(deltaMovement.y);
        float rayLength = Mathf.Abs(deltaMovement.y) + skinWidth;

        for (int i = 0; i < verticalRayCount; i++)
        {
            // If moving downwards, the ray origin is the bottom left. If moving upwards the origin is top left
            Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
            // Cycle through all of the vertical rays, also checking for where the ray origin will be after the velocity is applied
            rayOrigin += Vector2.right * (verticalRaySpacing * i + deltaMovement.x);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, collisionMask);

            if (hit)
            {
                deltaMovement.y = (hit.distance - skinWidth )* directionY;
                rayLength = hit.distance;

                collisions.below = directionY == -1;
                collisions.above = directionY == 1;
            }

            Debug.DrawRay(rayOrigin, Vector2.up * directionY * rayLength, Color.red);
        }
    }

    void UpdateRaycastOrigins()
    {
        Bounds bounds = collider.bounds;
        bounds.Expand(skinWidth * -2);

        raycastOrigins.bottomLeft = new Vector2(bounds.min.x, bounds.min.y);
        raycastOrigins.bottomRight = new Vector2(bounds.max.x, bounds.min.y);
        raycastOrigins.topLeft = new Vector2(bounds.min.x, bounds.max.y);
        raycastOrigins.topRight = new Vector2(bounds.max.x, bounds.max.y);
    }

    void CalculateRaySpacing()
    {
        Bounds bounds = collider.bounds;
        bounds.Expand(skinWidth * -2);

        // Make sure that there are at least 2 rays of each
        horizontalRayCount = Mathf.Clamp(horizontalRayCount, 2, int.MaxValue);
        verticalRayCount = Mathf.Clamp(verticalRayCount, 2, int.MaxValue);

        horizontalRaySpacing = bounds.size.y / (horizontalRayCount - 1);
        verticalRaySpacing = bounds.size.x / (verticalRayCount - 1);
    }

    void Flip()
    {
        // Multiply the player's x local scale by -1.
        Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;
    }

    struct RaycastOrigins
    {
        public Vector2 topLeft, topRight;
        public Vector2 bottomLeft, bottomRight;
    }

    public struct CollisionInfo
    {
        public bool above, below, left, right;
        public int faceDir; // 1 if facing right, -1 if facing left
        public void Reset()
        {
            above = below = false;
            left = right = false;
        }
    }
}
