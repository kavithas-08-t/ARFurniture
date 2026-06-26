using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class FurniturePlacementManager : MonoBehaviour
{
    public GameObject SpawnableFurniture;
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;

    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    private GameObject selectedObject;

    private float minScale = 0.2f;
    private float maxScale = 2f;

    void Update()
    {
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);

        // 🚫 Ignore UI touch
        if (EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            return;

        // =====================
        // 🔹 SELECT OR SPAWN
        // =====================
        if (touch.phase == TouchPhase.Began)
        {
            Ray ray = Camera.main.ScreenPointToRay(touch.position);
            RaycastHit hit;

            // SELECT
            if (Physics.Raycast(ray, out hit))
            {
                selectedObject = hit.collider.gameObject;
                return;
            }

            // SPAWN
            if (raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
            {
                Pose pose = hits[0].pose;

                GameObject obj = Instantiate(SpawnableFurniture, pose.position, pose.rotation);

                // small lift
                obj.transform.position += Vector3.up * 0.02f;

                selectedObject = obj;

                // disable planes after first spawn
                foreach (var plane in planeManager.trackables)
                    plane.gameObject.SetActive(false);

                planeManager.enabled = false;
            }
        }

        // =====================
        // 🔹 MOVE (1 finger)
        // =====================
        if (touch.phase == TouchPhase.Moved && Input.touchCount == 1 && selectedObject != null)
        {
            if (raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
            {
                selectedObject.transform.position = hits[0].pose.position;
            }
        }

        // =====================
        // 🔹 SCALE + ROTATE
        // =====================
        if (Input.touchCount == 2 && selectedObject != null)
        {
            Touch t1 = Input.GetTouch(0);
            Touch t2 = Input.GetTouch(1);

            // SCALE
            float prevDist = (t1.position - t1.deltaPosition - (t2.position - t2.deltaPosition)).magnitude;
            float currDist = (t1.position - t2.position).magnitude;

            float diff = currDist - prevDist;
            float scaleFactor = diff * 0.002f;

            Vector3 newScale = selectedObject.transform.localScale + Vector3.one * scaleFactor;

            newScale = Vector3.Max(newScale, Vector3.one * minScale);
            newScale = Vector3.Min(newScale, Vector3.one * maxScale);

            selectedObject.transform.localScale = newScale;

            // ROTATE
            Vector2 prevDir = (t1.position - t1.deltaPosition) - (t2.position - t2.deltaPosition);
            Vector2 currDir = t1.position - t2.position;

            float angle = Vector2.SignedAngle(prevDir, currDir);
            selectedObject.transform.Rotate(0, -angle, 0);
        }
    }

    // 🔴 DELETE BUTTON
    public void DeleteSelected()
    {
        if (selectedObject != null)
        {
            Destroy(selectedObject);
            selectedObject = null;
        }
    }

    // 🔄 SWITCH FURNITURE
    public void SwitchFurniture(GameObject furniture)
    {
        SpawnableFurniture = furniture;
    }
}