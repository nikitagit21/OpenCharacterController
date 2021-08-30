using UnityEngine;
using UnityEditor;

namespace OpenCharacterController.Editor
{
    // This editor exists solely to provide the frame bounds so that pressing F
    // in the viewport correctly frames the character.
    [CustomEditor(typeof(CharacterBody)), CanEditMultipleObjects]
    public class CharacterBodyEditor : UnityEditor.Editor
    {
        private bool HasFrameBounds() => true;

        private Bounds OnGetFrameBounds()
        {
            var bounds = (targets[0] as CharacterBody).bounds;
            for (int i = 1; i < targets.Length; i++)
            {
                bounds.Encapsulate((targets[i] as CharacterBody).bounds);
            }
            return bounds;
        }
    }
}
