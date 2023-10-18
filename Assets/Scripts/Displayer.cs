using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Data.Scripts;
using Scriptables;
using UnityEngine;
using UnityEngine.UIElements;
using Action = System.Action;

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
    [SerializeField] private float m_holdTime;

    private VisualElement m_root;
    private Dictionary<MagicLibrary, PathDisplay[]> m_pathDisplays = new Dictionary<MagicLibrary, PathDisplay[]>();

    private int m_selectedLibrary;
    private int m_selectedSystem;

    private void Start()
    {
        m_root = m_uiDocument.rootVisualElement;

        m_pathScrollView = m_root.Q<ScrollView>("scroll-view");
        m_spellScrollView = m_root.Q<ScrollView>("scroll-view (1)");

        DropdownField l_libraries = m_root.Q<DropdownField>("dropdown-field");
        l_libraries.choices = m_magicLibraries.m_magicPaths.Select(p_library => p_library.name).ToList();
        l_libraries.RegisterValueChangedCallback(p_evt =>
        {
            m_selectedLibrary = l_libraries.index;
            PlayerPrefs.SetInt(SELECTED_LIBRARY, m_selectedLibrary);
            PlayerPrefs.Save();
            SelectLibrary(m_selectedLibrary);
        });

        DropdownField l_systems = m_root.Q<DropdownField>("dropdown-field (1)");
        l_systems.RegisterValueChangedCallback(p_evt =>
        {
            m_selectedSystem = l_systems.index;
            PlayerPrefs.SetInt(SELECTED_SYSTEM, m_selectedSystem);
            PlayerPrefs.Save();
            SelectSystem(m_selectedLibrary);
        });

        foreach (MagicLibrary l_magicLibrary in m_magicLibraries.m_magicPaths)
        {
            List<PathDisplay> l_pathDisplays = new List<PathDisplay>();
            foreach (SpellPath l_spellPath in l_magicLibrary.paths)
            {
                // Path display generation
                VisualElement l_pathDisplay = m_pathDisplayAsset.CloneTree();
                Toggle l_pathToggle = l_pathDisplay.Q<Toggle>();
                VisualElement l_pathIcon = l_pathDisplay.Q("icon");
                l_pathIcon.style.backgroundImage = new StyleBackground(l_spellPath.pathImage.sprite);
                m_pathScrollView.Add(l_pathDisplay);

                List<SpellDisplay> l_spellDisplays = new List<SpellDisplay>();
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

                    m_spellScrollView.Add(l_spellDisplayNew);
                    m_spellScrollView.Add(l_spellDisplayOld);

                    l_spellDisplayNew.AddManipulator(new HoldManipulator(this, m_holdTime, () => { /*Add hold gestion*/ }));
                    l_spellDisplayOld.AddManipulator(new HoldManipulator(this, m_holdTime, () => { /*Add hold gestion*/ }));

                    l_spellDisplays.Add(new SpellDisplay
                    {
                        m_spell = l_spell,
                        m_newSpell = l_spellDisplayNew,
                        m_oldSpell = l_spellDisplayOld
                    });
                }


                // Path callback registration
                l_pathToggle.RegisterValueChangedCallback(p_evt =>
                {
                    if (p_evt.newValue) { }
                    else { }
                });
                l_pathToggle.AddManipulator(new HoldManipulator(this, m_holdTime, () => { /*Add hold gestion*/ }));

                l_pathDisplays.Add(new PathDisplay
                {
                    m_path = l_spellPath,
                    m_pathDisplay = l_pathDisplay,
                    m_spellDisplays = l_spellDisplays.ToArray()
                });
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
            foreach (PathDisplay l_pathDisplay in l_keyValuePair.Value)
            {
                l_pathDisplay.m_pathDisplay.style.display = new StyleEnum<DisplayStyle>(l_isSelected ? DisplayStyle.Flex : DisplayStyle.None);
                foreach (SpellDisplay l_spellDisplay in l_pathDisplay.m_spellDisplays)
                {
                    l_spellDisplay.m_newSpell.style.display = new StyleEnum<DisplayStyle>(l_isSelected && l_newSelected ? DisplayStyle.Flex : DisplayStyle.None);
                    l_spellDisplay.m_oldSpell.style.display = new StyleEnum<DisplayStyle>(l_isSelected && !l_newSelected ? DisplayStyle.Flex : DisplayStyle.None);
                }
            }
        }
    }

    private void SelectSystem(int p_systemIndex)
    {
        bool l_newSelected = p_systemIndex == 0;
        MagicLibrary l_selectedLibrary = m_magicLibraries.m_magicPaths[p_systemIndex];
        foreach (SpellDisplay l_spellDisplay in m_pathDisplays[l_selectedLibrary].SelectMany(p_pathDisplay => p_pathDisplay.m_spellDisplays))
        {
            l_spellDisplay.m_newSpell.style.display = new StyleEnum<DisplayStyle>(l_newSelected ? DisplayStyle.Flex : DisplayStyle.None);
            l_spellDisplay.m_oldSpell.style.display = new StyleEnum<DisplayStyle>(!l_newSelected ? DisplayStyle.Flex : DisplayStyle.None);
        }
    }
}
