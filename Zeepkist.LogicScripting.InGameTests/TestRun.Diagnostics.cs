using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TNRD.Zeepkist.LogicScripting.InGameTests;

internal sealed partial class TestRun
{
    /// <summary>
    /// What the screen shows, for a walk that gave up before the main menu, with <paramref name="screen"/> once the game
    /// has its start screen. The start screen's own fields go to the log only, as they're the game's.
    /// </summary>
    private string DescribeStartScreen(PressAnyKeyToStartGame screen)
    {
        var state = new StartScreenState
        {
            // A destroyed screen still has its C# object, which Unity's == null takes for none
            ScreenFound = !ReferenceEquals(screen, null),
            ScreenLeft = !ReferenceEquals(screen, null) && screen == null,
            IntroPanelActive = screen != null && screen.disableThisIntroPanel != null && screen.disableThisIntroPanel.activeInHierarchy,
            PlayerManagerExists = PlayerManager.Instance != null,
            Texts = VisibleTexts(screen),
        };

        // The detail keeps only the first few, so all of them go to the log
        _logger.LogInfo($"{Marker} texts on screen: {string.Join(" | ", state.Texts.ConvertAll(text => text.Replace('\n', ' ')))}");
        if (screen != null)
            _logger.LogInfo($"{Marker} start screen fields: {FieldValues(screen)}");

        return state.Describe();
    }

    /// <summary>
    /// The texts on screen: those of active text components, which is what a person sees on a menu. Those under
    /// <paramref name="screen"/> come first, then those under its root, so the start screen's own status isn't crowded out
    /// of the step's detail by the rest of the scene.
    /// </summary>
    private static List<string> VisibleTexts(Component screen)
    {
        HashSet<Component> seen = [];
        List<string> texts = [];

        void Add(IEnumerable<TMP_Text> tmpTexts, IEnumerable<UnityEngine.UI.Text> uiTexts)
        {
            foreach (var text in tmpTexts)
            {
                if (text.isActiveAndEnabled && seen.Add(text))
                    texts.Add(text.text);
            }

            foreach (var text in uiTexts)
            {
                if (text.isActiveAndEnabled && seen.Add(text))
                    texts.Add(text.text);
            }
        }

        if (screen != null)
        {
            Add(screen.GetComponentsInChildren<TMP_Text>(), screen.GetComponentsInChildren<UnityEngine.UI.Text>());
            var root = screen.transform.root;
            Add(root.GetComponentsInChildren<TMP_Text>(), root.GetComponentsInChildren<UnityEngine.UI.Text>());
        }

        Add(Object.FindObjectsOfType<TMP_Text>(), Object.FindObjectsOfType<UnityEngine.UI.Text>());
        return texts;
    }

    /// <summary>
    /// The component's simple fields, and for each object it holds whether it's there and active.
    /// </summary>
    private static string FieldValues(Component component)
    {
        List<string> values = [];
        foreach (var field in component.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
        {
            var value = field.GetValue(component);
            var type = field.FieldType;
            if (type.IsPrimitive || type.IsEnum || type == typeof(string))
            {
                values.Add($"{field.Name}={value}");
            }
            else if (typeof(Object).IsAssignableFrom(type))
            {
                var unityObject = value as Object;
                var shown = unityObject == null ? "none"
                    : unityObject is GameObject gameObject ? (gameObject.activeInHierarchy ? "active" : "inactive")
                    : unityObject is Behaviour behaviour ? (behaviour.isActiveAndEnabled ? "enabled" : "disabled")
                    : "set";
                values.Add($"{field.Name}={shown}");
            }
        }

        return string.Join(", ", values);
    }
}
