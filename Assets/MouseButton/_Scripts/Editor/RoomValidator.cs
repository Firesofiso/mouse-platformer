using System.Text;
using UnityEditor;
using UnityEngine;

public static class RoomValidator
{
    [MenuItem("Tools/Validate Rooms")]
    static void Validate()
    {
        var sb = new StringBuilder();
        int errors = 0, warnings = 0, infos = 0;

        var transitions = Object.FindObjectsOfType<CameraRoomTransition>(true);

        foreach (var t in transitions)
        {
            var path = GetPath(t.transform);

            if (t.destination == null)
            {
                sb.AppendLine($"[ERROR] {path} has null destination");
                errors++;
            }

            var sourceRoom = t.GetComponentInParent<CameraRoom>(true);
            if (sourceRoom == null)
            {
                sb.AppendLine($"[ERROR] {path} not inside any CameraRoom");
                errors++;
            }

            var cp = t.GetComponentInChildren<Checkpoint>(true);
            if (cp == null)
            {
                sb.AppendLine($"[ERROR] {path} has no Checkpoint child");
                errors++;
            }
            else if (t.destination != null)
            {
                var p = cp.transform.position;
                var size = t.destination.Size;
                var center = t.destination.transform.position;
                bool inside = Mathf.Abs(p.x - center.x) < size.x * 0.5f
                              && Mathf.Abs(p.y - center.y) < size.y * 0.5f;
                if (!inside)
                {
                    sb.AppendLine($"[WARN] {path} Checkpoint at {p} outside destination room '{t.destination.name}'");
                    warnings++;
                }
            }
        }

        for (int i = 0; i < transitions.Length; i++)
        {
            var a = transitions[i].GetComponent<BoxCollider2D>();
            if (a == null) continue;
            for (int j = i + 1; j < transitions.Length; j++)
            {
                var b = transitions[j].GetComponent<BoxCollider2D>();
                if (b == null) continue;
                if (!a.bounds.Intersects(b.bounds)) continue;

                var pathA = GetPath(transitions[i].transform);
                var pathB = GetPath(transitions[j].transform);
                bool validPair = transitions[i].destination != null
                                 && transitions[j].destination != null
                                 && transitions[i].GetComponentInParent<CameraRoom>(true) == transitions[j].destination
                                 && transitions[j].GetComponentInParent<CameraRoom>(true) == transitions[i].destination;

                if (validPair)
                {
                    sb.AppendLine($"[INFO] Bidirectional pair overlap (OK): {pathA} ↔ {pathB}");
                    infos++;
                }
                else
                {
                    sb.AppendLine($"[WARN] Trigger overlap (not a valid pair): {pathA} ↔ {pathB}");
                    warnings++;
                }
            }
        }

        if (errors + warnings + infos == 0)
            sb.AppendLine("All rooms valid.");

        Debug.Log($"Room Validation — {errors} errors, {warnings} warnings, {infos} infos\n{sb}");
    }

    static string GetPath(Transform t)
    {
        var path = t.name;
        var p = t.parent;
        while (p != null) { path = p.name + "/" + path; p = p.parent; }
        return path;
    }
}
