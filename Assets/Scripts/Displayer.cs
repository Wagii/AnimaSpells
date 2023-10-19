using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Data.Scripts;
using Scriptables;
using UnityEngine;
using UnityEngine.UIElements;
using Action = System.Action;

public class PathSelection
{
    public bool[][] m_pathSelection;

    public override string ToString()
    {
        string l_string = "";
        for (int l_index = 0; l_index < m_pathSelection.Length; l_index++)
        {
            bool[] l_bools = m_pathSelection[l_index];
            l_string += string.Join(",", l_bools.Select(p_bool => p_bool ? "1" : "0"));
            if (l_index != m_pathSelection.Length - 1)
                l_string += "|";
        }

        return l_string;
    }

    public PathSelection() {}

    public PathSelection(string p_string)
    {
        string[] l_lines = p_string.Split('|');
        m_pathSelection = new bool[l_lines.Length][];
        for (int l_index = 0; l_index < l_lines.Length; l_index++)
        {
            string l_line = l_lines[l_index];
            string[] l_bools = l_line.Split(',');
            m_pathSelection[l_index] = new bool[l_bools.Length];
            for (int l_boolIndex = 0; l_boolIndex < l_bools.Length; l_boolIndex++)
            {
                string l_bool = l_bools[l_boolIndex];
                m_pathSelection[l_index][l_boolIndex] = l_bool == "1";
            }
        }
    }

    public void SaveSelection()
    {
        PlayerPrefs.SetString("path_selection", ToString());
        PlayerPrefs.Save();
    }

    public static PathSelection LoadSelection()
    {
        return new PathSelection(PlayerPrefs.GetString("path_selection", ""));
    }
}

public class SpellDisplay
{
    public Spell m_spell;
    public VisualElement m_newSpell;
    public VisualElement m_oldSpell;
}

public class PathDisplay
{
    public SpellPath m_path;
    public VisualElement m_pathDisplay;
    public SpellDisplay[] m_spellDisplays;
}

public class HoldManipulator : PointerManipulator
{
    private readonly MonoBehaviour m_target;
    private readonly float m_holdTime;
    private readonly Action m_onHeld;
    private Coroutine m_holdRoutine;

    public HoldManipulator(MonoBehaviour p_target, float p_holdTime, Action p_onHeld)
    {
        m_target = p_target;
        m_holdTime = p_holdTime;
        m_onHeld = p_onHeld;
    }

    protected override void RegisterCallbacksOnTarget()
    {
        target.RegisterCallback<PointerDownEvent>(DownCallback, TrickleDown.TrickleDown);
        target.RegisterCallback<PointerUpEvent>(UpCallback);
        target.RegisterCallback<PointerCancelEvent>(UpCallback);
        target.RegisterCallback<PointerLeaveEvent>(UpCallback);
    }

    private void DownCallback(PointerDownEvent _)
    {
        m_holdRoutine = m_target.StartCoroutine(HoldRoutine(m_holdTime, m_onHeld));
    }

    private void UpCallback(PointerUpEvent _)
    {
        if (m_holdRoutine != null) m_target.StopCoroutine(m_holdRoutine);
    }

    private void UpCallback(PointerCancelEvent _)
    {
        if (m_holdRoutine != null) m_target.StopCoroutine(m_holdRoutine);
    }

    private void UpCallback(PointerLeaveEvent _)
    {
        if (m_holdRoutine != null) m_target.StopCoroutine(m_holdRoutine);
    }

    protected override void UnregisterCallbacksFromTarget()
    {
        target.UnregisterCallback<PointerLeaveEvent>(UpCallback);
        target.UnregisterCallback<PointerCancelEvent>(UpCallback);
        target.UnregisterCallback<PointerUpEvent>(UpCallback);
        target.UnregisterCallback<PointerDownEvent>(DownCallback, TrickleDown.TrickleDown);
    }

    private static IEnumerator HoldRoutine(float p_holdTime, Action p_onHeld)
    {
        float timer = 0f;
        while (timer < p_holdTime)
        {
            yield return null;
            timer += Time.deltaTime;
        }
        p_onHeld?.Invoke();
    }
}

public class Displayer : MonoBehaviour
{
    private const string SELECTED_LIBRARY = "selected_library";
    private const string SELECTED_SYSTEM = "selected_system";

    [SerializeField] private UIDocument m_uiDocument;
    [SerializeField] private ScrollView m_pathScrollView;
    [SerializeField] private ScrollView m_spellScrollView;
    [SerializeField] private MagicLibraries m_magicLibraries;
    [SerializeField] private VisualTreeAsset m_pathDisplayAsset;
    [SerializeField] private VisualTreeAsset m_spellDisplayAssetNew;
    [SerializeField] private VisualTreeAsset m_spellDisplayAssetOld;
    [SerializeField] private VisualTreeAsset m_pathInfoWindowAsset;
    [SerializeField] private VisualTreeAsset m_spellInfoNewSystemWindowAsset;
    [SerializeField] private VisualTreeAsset m_spellInfoOldSystemWindowAsset;
    [SerializeField] private VisualTreeAsset m_spellLevelInfoWindowAsset;
    [SerializeField] private float m_holdTime;

    private VisualElement m_root;
    private readonly Dictionary<MagicLibrary, PathDisplay[]> m_pathDisplays = new Dictionary<MagicLibrary, PathDisplay[]>();
    private PathSelection m_pathSelection;

    private int m_selectedLibrary;
    private int m_selectedSystem;

    private void Start()
    {
        m_root = m_uiDocument.rootVisualElement;

        m_pathSelection = PathSelection.LoadSelection();

        m_pathScrollView = m_root.Q<ScrollView>("scroll-view");
        m_spellScrollView = m_root.Q<ScrollView>("scroll-view (1)");

        DropdownField l_libraries = m_root.Q<DropdownField>("dropdown-field");
        l_libraries.choices = m_magicLibraries.m_magicPaths.Select(p_library => p_library.name).ToList();
        l_libraries.RegisterValueChangedCallback(_ =>
        {
            m_selectedLibrary = l_libraries.index;
            PlayerPrefs.SetInt(SELECTED_LIBRARY, m_selectedLibrary);
            PlayerPrefs.Save();
            SelectLibrary(m_selectedLibrary);
        });

        DropdownField l_systems = m_root.Q<DropdownField>("dropdown-field (1)");
        l_systems.RegisterValueChangedCallback(_ =>
        {
            m_selectedSystem = l_systems.index;
            PlayerPrefs.SetInt(SELECTED_SYSTEM, m_selectedSystem);
            PlayerPrefs.Save();
            SelectSystem(m_selectedLibrary);
        });

        for (int l_libraryIndex = 0; l_libraryIndex < m_magicLibraries.m_magicPaths.Length; l_libraryIndex++)
        {
            MagicLibrary l_magicLibrary = m_magicLibraries.m_magicPaths[l_libraryIndex];
            List<PathDisplay> l_pathDisplays = new();
            for (int l_pathIndex = 0; l_pathIndex < l_magicLibrary.paths.Length; l_pathIndex++)
            {
                SpellPath l_spellPath = l_magicLibrary.paths[l_pathIndex];
                PathDisplay l_pathDisplay = new() { m_path = l_spellPath };

                // Path display generation
                VisualElement l_pathElement = m_pathDisplayAsset.CloneTree();
                Toggle l_pathToggle = l_pathElement.Q<Toggle>();
                VisualElement l_pathIcon = l_pathElement.Q("icon");
                l_pathIcon.style.backgroundImage = new StyleBackground(l_spellPath.pathImage.sprite);
                m_pathScrollView.Add(l_pathElement);

                l_pathDisplay.m_pathDisplay = l_pathElement;

                List<SpellDisplay> l_spellDisplays = new();
                foreach (Spell l_spell in l_spellPath.spells)
                {
                    // Spell display generation
                    VisualElement l_spellDisplayNew = m_spellDisplayAssetNew.CloneTree();
                    VisualElement l_spellDisplayOld = m_spellDisplayAssetOld.CloneTree();

                    // Spell display setup
                    l_spellDisplayNew.style.borderTopColor = new StyleColor(l_spellPath.pathColor);
                    l_spellDisplayNew.style.borderBottomColor = new StyleColor(l_spellPath.pathColor);
                    l_spellDisplayNew.style.borderLeftColor = new StyleColor(l_spellPath.pathColor);
                    l_spellDisplayNew.style.borderRightColor = new StyleColor(l_spellPath.pathColor);
                    l_spellDisplayOld.style.borderTopColor = new StyleColor(l_spellPath.pathColor);
                    l_spellDisplayOld.style.borderBottomColor = new StyleColor(l_spellPath.pathColor);
                    l_spellDisplayOld.style.borderLeftColor = new StyleColor(l_spellPath.pathColor);
                    l_spellDisplayOld.style.borderRightColor = new StyleColor(l_spellPath.pathColor);

                    // TODO : Setup spell display values

                    m_spellScrollView.Add(l_spellDisplayNew);
                    m_spellScrollView.Add(l_spellDisplayOld);

                    l_spellDisplayNew.Q<Button>().clickable.clicked += () => {/* TODO : Show full info window */};
                    l_spellDisplayOld.Q<Button>().clickable.clicked += () => {/* TODO : Show full info window */};

                    l_spellDisplays.Add(new SpellDisplay
                    {
                        m_spell = l_spell,
                        m_newSpell = l_spellDisplayNew,
                        m_oldSpell = l_spellDisplayOld
                    });
                }

                l_pathDisplay.m_spellDisplays = l_spellDisplays.ToArray();


                // Path callback registration
                int l_localLibraryIndex = l_libraryIndex;
                int l_localPathIndex = l_pathIndex;
                l_pathToggle.RegisterValueChangedCallback(p_evt =>
                {
                    m_pathSelection.m_pathSelection[l_localLibraryIndex][l_localPathIndex] = p_evt.newValue;
                    foreach (SpellDisplay l_spellDisplay in l_pathDisplay.m_spellDisplays)
                    {
                        l_spellDisplay.m_newSpell.style.display = new StyleEnum<DisplayStyle>(
                            p_evt.newValue && m_selectedSystem == 0
                                ? DisplayStyle.Flex
                                : DisplayStyle.None);
                        l_spellDisplay.m_oldSpell.style.display = new StyleEnum<DisplayStyle>(
                            p_evt.newValue && m_selectedSystem != 0
                                ? DisplayStyle.Flex
                                : DisplayStyle.None);
                    }
                });
                l_pathToggle.AddManipulator(new HoldManipulator(this, m_holdTime, () =>
                {
                    /*Add hold gestion*/
                }));

                l_pathDisplays.Add(l_pathDisplay);
                l_pathToggle.value = m_pathSelection.m_pathSelection[l_libraryIndex][l_pathIndex];
            }

            m_pathDisplays.Add(l_magicLibrary, l_pathDisplays.ToArray());
        }

        m_selectedLibrary = PlayerPrefs.GetInt(SELECTED_LIBRARY, 0);
        m_selectedSystem = PlayerPrefs.GetInt(SELECTED_SYSTEM, 0);
        l_libraries.index = m_selectedLibrary;
        l_systems.index = m_selectedSystem;
    }

    public void SelectLibrary(int p_libraryIndex)
    {
        bool l_newSelected = m_selectedSystem == 0;
        MagicLibrary l_selectedLibrary = m_magicLibraries.m_magicPaths[p_libraryIndex];
        foreach (KeyValuePair<MagicLibrary,PathDisplay[]> l_keyValuePair in m_pathDisplays)
        {
            bool l_isSelected = l_keyValuePair.Key == l_selectedLibrary;
            for (int l_index = 0; l_index < l_keyValuePair.Value.Length; l_index++)
            {
                PathDisplay l_pathDisplay = l_keyValuePair.Value[l_index];
                l_pathDisplay.m_pathDisplay.style.display = new StyleEnum<DisplayStyle>(l_isSelected ? DisplayStyle.Flex : DisplayStyle.None);
                bool isOn = m_pathSelection.m_pathSelection[p_libraryIndex][l_index];
                foreach (SpellDisplay l_spellDisplay in l_pathDisplay.m_spellDisplays)
                {
                    l_spellDisplay.m_newSpell.style.display =
                        new StyleEnum<DisplayStyle>(l_isSelected && l_newSelected && isOn
                            ? DisplayStyle.Flex
                            : DisplayStyle.None);
                    l_spellDisplay.m_oldSpell.style.display =
                        new StyleEnum<DisplayStyle>(l_isSelected && !l_newSelected && isOn
                            ? DisplayStyle.Flex
                            : DisplayStyle.None);
                }
            }
        }
    }

    private void SelectSystem(int p_systemIndex)
    {
        bool l_newSelected = p_systemIndex == 0;
        MagicLibrary l_selectedLibrary = m_magicLibraries.m_magicPaths[m_selectedLibrary];
        for (int l_index = 0; l_index < m_pathDisplays[l_selectedLibrary].Length; l_index++)
        {
            PathDisplay l_pathDisplay = m_pathDisplays[l_selectedLibrary][l_index];
            bool isOn = m_pathSelection.m_pathSelection[m_selectedLibrary][l_index];
            foreach (SpellDisplay l_spellDisplay in l_pathDisplay.m_spellDisplays)
            {
                l_spellDisplay.m_newSpell.style.display =
                    new StyleEnum<DisplayStyle>(l_newSelected && isOn
                        ? DisplayStyle.Flex
                        : DisplayStyle.None);
                l_spellDisplay.m_oldSpell.style.display =
                    new StyleEnum<DisplayStyle>(!l_newSelected && isOn
                        ? DisplayStyle.Flex
                        : DisplayStyle.None);
            }
        }
    }
}
