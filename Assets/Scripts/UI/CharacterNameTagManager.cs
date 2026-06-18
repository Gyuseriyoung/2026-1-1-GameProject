using UnityEngine;
using TMPro;

namespace CookingGame
{
    public class CharacterNameTagManager : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoaded()
        {
            // Find all SpriteRenderers in the scene to find characters
            var renderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            foreach (var sr in renderers)
            {
                string name = sr.gameObject.name.ToLower();
                string displayName = "";
                
                if (name.Contains("chef"))
                {
                    displayName = "셰프 (나)";
                }
                else if (name.Contains("customer1"))
                {
                    displayName = "김상택 (손님)";
                }
                else if (name.Contains("customer2"))
                {
                    displayName = "민기 & 동건 (손님)";
                }
                else if (name.Contains("food critic") || name.Contains("critic"))
                {
                    displayName = "미스터 장 (평론가)";
                }

                if (!string.IsNullOrEmpty(displayName))
                {
                    CreateNameTag(sr.gameObject, displayName, sr);
                }
            }
        }

        private static void CreateNameTag(GameObject target, string text, SpriteRenderer sr)
        {
            // Check if name tag already exists
            if (target.transform.Find("NameTag_" + text) != null) return;

            // Create a child object for the name tag
            GameObject tagGo = new GameObject("NameTag_" + text);
            tagGo.transform.SetParent(target.transform);
            
            // Calculate world position slightly above the sprite bounds
            Vector3 worldPos = target.transform.position;
            if (sr != null)
            {
                worldPos.y = sr.bounds.max.y + 0.3f;
            }
            else
            {
                worldPos.y += 1.5f;
            }
            tagGo.transform.position = new Vector3(worldPos.x, worldPos.y, -1f);
            tagGo.transform.localScale = Vector3.one;

            // Add TextMeshPro
            var tmp = tagGo.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = 4f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            
            // Outline styling for readability against any background
            tmp.outlineColor = Color.black;
            tmp.outlineWidth = 0.2f;
        }
    }
}
