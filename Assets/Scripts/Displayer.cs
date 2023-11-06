using System;
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
		if (!PlayerPrefs.HasKey("path_selection") || PlayerPrefs.GetString("path_selection", "") == "")
			GenerateDefaultPathSelection();
		return new PathSelection(PlayerPrefs.GetString("path_selection", ""));
	}

	public static void GenerateDefaultPathSelection()
	{
		if (Displayer.instance == null || Displayer.MagicLibraries == null) return;

		MagicLibraries magicLibraries = Displayer.MagicLibraries;
		PathSelection pathSelection = new PathSelection();

		int libraryCount = magicLibraries.m_magicPaths.Length;
		pathSelection.m_pathSelection = new bool[libraryCount][];
		for (int i = 0; i < libraryCount; i++)
			pathSelection.m_pathSelection[i] = new bool[magicLibraries.m_magicPaths[i].paths.Length];

		PlayerPrefs.SetString("path_selection", pathSelection.ToString());
	}
}

public class SpellDisplay
{
	public Spell m_spell;
	public VisualElement m_VisualElement;
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
	private const string SELECTED_THEME = "selected_theme";
	private const string SELECTED_LIBRARY = "selected_library";
	private const string SELECTED_SYSTEM = "selected_system";

	private static readonly Color BaseColor = new Color(96, 103, 191, 255);
	private static readonly Color IntermediateColor = new Color(96, 191, 103, 255);
	private static readonly Color AdvancedColor = new Color(191, 156, 96, 255);
	private static readonly Color ArcaneColor = new Color(191, 96, 103, 255);
	private readonly Dictionary<Rank, Color> m_rankColors = new Dictionary<Rank, Color>()
	{
		{Rank.Initial, BaseColor},
		{Rank.Intermédiaire, IntermediateColor},
		{Rank.Avancé, AdvancedColor},
		{Rank.Arcane, ArcaneColor}
	};

	public static Displayer instance;
	public static MagicLibraries MagicLibraries => instance.m_magicLibraries;

	[SerializeField] private UIDocument m_uiDocument;
	[SerializeField] private MagicLibraries m_magicLibraries;
	[SerializeField] private VisualTreeAsset m_pathDisplayAsset;
	[SerializeField] private VisualTreeAsset m_spellDisplayAsset;
	[SerializeField] private VisualTreeAsset m_pathInfoWindowAsset;
	[SerializeField] private VisualTreeAsset m_spellInfoNewSystemWindowAsset;
	[SerializeField] private VisualTreeAsset m_spellInfoOldSystemWindowAsset;
	[SerializeField] private VisualTreeAsset m_spellLevelInfoWindowAsset;
	[SerializeField] private StyleSheet m_darkTheme;
	[SerializeField] private StyleSheet m_lightTheme;
	[SerializeField] private float m_holdTime;

	private static readonly Dictionary<MagicLibrary, PathDisplay[]> m_pathDisplays = new Dictionary<MagicLibrary, PathDisplay[]>();
	private static VisualElement m_root;
	private static ScrollView m_pathScrollView;
	private static ScrollView m_spellScrollView;
	private static PathSelection m_pathSelection;
	private static VisualElement m_infoWindowHolder;
	private static VisualElement m_pathInfoWindow;
	private static VisualElement m_spellInfoNewSystemWindow;
	private static VisualElement m_spellInfoOldSystemWindow;
	private static VisualElement m_spellLevelInfoWindow;

	private int m_selectedLibrary;
	private bool m_newSystem;

	private void Awake()
	{
		if (instance != null)
		{
			DestroyImmediate(this);
			return;
		}

		instance = this;
	}

	private void Start()
	{
		m_magicLibraries.PathGraphicsMap = new Dictionary<string, PathGraphics>();
		foreach (PathGraphics l_pathGraphics in m_magicLibraries.m_PathGraphicsArray)
			m_magicLibraries.PathGraphicsMap.TryAdd(l_pathGraphics.pathReference, l_pathGraphics);

		// return;
		m_root = m_uiDocument.rootVisualElement;

		m_pathSelection = PathSelection.LoadSelection();

		m_pathScrollView = m_root.Q<ScrollView>("path-view");
		m_spellScrollView = m_root.Q<ScrollView>("spell-view");
		m_infoWindowHolder = m_root.Q<VisualElement>("info-window-holder");

		m_pathInfoWindow = m_pathInfoWindowAsset.CloneTree();
		m_spellInfoNewSystemWindow = m_spellInfoNewSystemWindowAsset.CloneTree();
		m_spellInfoOldSystemWindow = m_spellInfoOldSystemWindowAsset.CloneTree();
		m_spellLevelInfoWindow = m_spellLevelInfoWindowAsset.CloneTree();

		m_infoWindowHolder.Add(m_pathInfoWindow);
		m_infoWindowHolder.Add(m_spellInfoNewSystemWindow);
		m_infoWindowHolder.Add(m_spellInfoOldSystemWindow);
		m_infoWindowHolder.Add(m_spellLevelInfoWindow);

		Button l_prevButton = m_spellLevelInfoWindow.Q<Button>("prev-button");
		l_prevButton.clickable.clicked += () => m_spellLevelInfoWindow.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
		l_prevButton = m_spellInfoNewSystemWindow.Q<Button>("prev-button");
		l_prevButton.clickable.clicked += () => m_spellInfoNewSystemWindow.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
		l_prevButton = m_spellInfoOldSystemWindow.Q<Button>("prev-button");
		l_prevButton.clickable.clicked += () => m_spellInfoOldSystemWindow.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
		l_prevButton = m_pathInfoWindow.Q<Button>("prev-button");
		l_prevButton.clickable.clicked += () => m_pathInfoWindow.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);

		m_pathInfoWindow.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
		m_spellInfoNewSystemWindow.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
		m_spellInfoOldSystemWindow.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
		m_spellLevelInfoWindow.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);

		SlideToggle l_theme = m_root.Q<SlideToggle>("theme-toggle");
		l_theme.RegisterValueChangedCallback(p_evt =>
		{
			if (p_evt.newValue)
			{
				m_root.styleSheets.Remove(m_lightTheme);
				m_root.styleSheets.Add(m_darkTheme);
			}
			else
			{
				m_root.styleSheets.Remove(m_darkTheme);
				m_root.styleSheets.Add(m_lightTheme);
			}
			PlayerPrefs.SetInt(SELECTED_THEME, p_evt.newValue ? 1 : 0);
			PlayerPrefs.Save();
		});

		DropdownField l_libraries = m_root.Q<DropdownField>("dropdown-library");
		l_libraries.choices = m_magicLibraries.m_magicPaths.Select(p_library => p_library.name).ToList();
		l_libraries.RegisterValueChangedCallback(_ =>
		{
			m_selectedLibrary = l_libraries.index;
			PlayerPrefs.SetInt(SELECTED_LIBRARY, m_selectedLibrary);
			PlayerPrefs.Save();
			SelectLibrary(m_selectedLibrary);
		});

		SlideToggle l_systems = m_root.Q<SlideToggle>("system-toggle");
		l_systems.RegisterValueChangedCallback(p_index =>
		{
			m_newSystem = p_index.newValue;
			PlayerPrefs.SetInt(SELECTED_SYSTEM, p_index.newValue ? 1 : 0);
			PlayerPrefs.Save();
		});

		for (int l_libraryIndex = 0; l_libraryIndex < m_magicLibraries.m_magicPaths.Length; l_libraryIndex++)
		{
			MagicLibrary l_magicLibrary = m_magicLibraries.m_magicPaths[l_libraryIndex];

			List<PathDisplay> l_pathDisplays = new();
			for (int l_pathIndex = 0; l_pathIndex < l_magicLibrary.paths.Length; l_pathIndex++)
			{
				SpellPath l_spellPath = l_magicLibrary.paths[l_pathIndex];
				PathGraphics l_pathGraphics = m_magicLibraries.PathGraphicsMap[l_spellPath.name];
				Color.RGBToHSV(l_pathGraphics.color, out float h, out float s, out float v);

				PathDisplay l_pathDisplay = new() { m_path = l_spellPath };

				// Path display generation
				VisualElement l_pathElement = m_pathDisplayAsset.CloneTree();
				Toggle l_pathToggle = l_pathElement.Q<Toggle>();
				l_pathToggle.style.borderLeftColor = new StyleColor(l_pathGraphics.color);
				l_pathToggle.style.borderRightColor = new StyleColor(l_pathGraphics.color);
				l_pathToggle.style.borderTopColor = new StyleColor(l_pathGraphics.color);
				l_pathToggle.style.borderBottomColor = new StyleColor(l_pathGraphics.color);
				VisualElement l_pathIcon = l_pathElement.Q("path-icon");
				l_pathIcon.style.backgroundImage = new StyleBackground(l_pathGraphics.texture2D);
				m_pathScrollView.Add(l_pathElement);

				l_pathDisplay.m_pathDisplay = l_pathElement;

				List<SpellDisplay> l_spellDisplays = new();
				foreach (Spell l_spell in l_spellPath.spells)
				{
					// Spell display generation
					VisualElement l_spellDisplayAsset = m_spellDisplayAsset.CloneTree();
					VisualElement l_spellInfoButton = l_spellDisplayAsset.Q<Button>();

					// Spell display setup
					l_spellInfoButton.style.borderTopColor = new StyleColor(l_pathGraphics.color);
					l_spellInfoButton.style.borderBottomColor = new StyleColor(l_pathGraphics.color);
					l_spellInfoButton.style.borderLeftColor = new StyleColor(l_pathGraphics.color);
					l_spellInfoButton.style.borderRightColor = new StyleColor(l_pathGraphics.color);
					l_spellInfoButton.style.backgroundColor = new StyleColor(Color.Lerp(l_pathGraphics.color, Color.black, 0.16f));

					l_spellInfoButton.Q<Label>("spell-name").text = l_spell.name;
					l_spellInfoButton.Q<Label>("spell-path").text = l_spellPath.name;
					l_spellInfoButton.Q<Label>("spell-level").text = l_spell.level.ToString();

					l_spellInfoButton.Q<Button>().clickable.clicked += () => OnSpellClicked(l_spell);

					m_spellScrollView.Add(l_spellDisplayAsset);
					l_spellDisplays.Add(new SpellDisplay { m_spell = l_spell, m_VisualElement = l_spellDisplayAsset });
				}

				l_pathDisplay.m_spellDisplays = l_spellDisplays.ToArray();


				// Path callback registration
				int l_localLibraryIndex = l_libraryIndex;
				int l_localPathIndex = l_pathIndex;
				l_pathToggle.RegisterValueChangedCallback(p_evt =>
				{
					m_pathSelection.m_pathSelection[l_localLibraryIndex][l_localPathIndex] = p_evt.newValue;
					m_pathSelection.SaveSelection();
					bool displayEverything = m_pathSelection.m_pathSelection[l_localLibraryIndex].All(p_element => p_element == false);
					ToggleAllPaths(displayEverything);

				});
				l_pathToggle.AddManipulator(new HoldManipulator(this, m_holdTime, () =>
				{
					DisplayPathInfos(l_pathDisplay);
				}));

				l_pathDisplays.Add(l_pathDisplay);
			}

			m_pathDisplays.Add(l_magicLibrary, l_pathDisplays.ToArray());
		}

		for (int libraryIndex = 0; libraryIndex < m_pathDisplays.Count; libraryIndex++)
		{
			PathDisplay[] pathDisplays = m_pathDisplays.ElementAt(libraryIndex).Value;
			for (int pathIndex = 0; pathIndex < pathDisplays.Length; pathIndex++)
            {
				PathDisplay pathDisplay = pathDisplays[pathIndex];
				Toggle pathToggle = pathDisplay.m_pathDisplay.Q<Toggle>();
				pathToggle.SetValueWithoutNotify(m_pathSelection.m_pathSelection[libraryIndex][pathIndex]);
            }
        }

		m_selectedLibrary = PlayerPrefs.GetInt(SELECTED_LIBRARY, 0);
		m_newSystem = PlayerPrefs.GetInt(SELECTED_SYSTEM, 1) == 1;
		l_libraries.index = m_selectedLibrary;
		l_systems.value = m_newSystem;
		l_theme.value = PlayerPrefs.GetInt(SELECTED_THEME, 1) == 1;

		ToggleAllPaths(m_pathSelection.m_pathSelection[m_selectedLibrary].All(element => element == false));
	}

	private void ToggleAllPaths(bool displayEverything)
	{
		PathDisplay[] pathDisplays = m_pathDisplays.ElementAt(m_selectedLibrary).Value;
		for (int pathIndex = 0; pathIndex < pathDisplays.Length; pathIndex++)
        {
			PathDisplay pathDisplay = pathDisplays[pathIndex];
			bool pathDisplaying = m_pathSelection.m_pathSelection[m_selectedLibrary][pathIndex];
			for (int spellIndex = 0; spellIndex < pathDisplay.m_spellDisplays.Length; spellIndex++)
			{
				pathDisplay.m_spellDisplays[spellIndex].m_VisualElement.style.display = new StyleEnum<DisplayStyle>(displayEverything || pathDisplaying ? DisplayStyle.Flex : DisplayStyle.None);
			}
        }
    }

	private void OnSpellClicked(Spell p_spell)
	{
		if (m_newSystem)
			SetupNewSpell(p_spell);
		else
			SetupOldSpell(p_spell);
	}

	private void SetupNewSpell(Spell p_spell)
	{
		PathGraphics l_pathGraphics = m_magicLibraries.PathGraphicsMap[p_spell.pathReference];
		m_spellLevelInfoWindow.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);

		ScrollView l_spellDisplayNew = m_spellLevelInfoWindow.Q<ScrollView>("holder");

		l_spellDisplayNew.style.borderTopColor = new StyleColor(l_pathGraphics.color);
		l_spellDisplayNew.style.borderBottomColor = new StyleColor(l_pathGraphics.color);
		l_spellDisplayNew.style.borderLeftColor = new StyleColor(l_pathGraphics.color);
		l_spellDisplayNew.style.borderRightColor = new StyleColor(l_pathGraphics.color);

		Label l_label = l_spellDisplayNew.Q<Label>("spell-name");
		l_label.text = p_spell.name;
		l_label.style.color = new StyleColor(l_pathGraphics.color);

		l_label = l_spellDisplayNew.Q<Label>("spell-level");
		l_label.text = p_spell.level.ToString();

		l_label = l_spellDisplayNew.Q<Label>("spell-actions");
		l_label.text = p_spell.action.ToString();

		l_label = l_spellDisplayNew.Q<Label>("spell-forbidden");
		l_label.style.display = new StyleEnum<DisplayStyle>(p_spell.forbiddenPaths.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None);
		l_label.text = $"<b>Voies Fermées : </b>{string.Join(", ", p_spell.forbiddenPaths)}";

		l_label = l_spellDisplayNew.Q<Label>("spell-effect");
		l_label.text = "<b>Effet : </b>"+ p_spell.newSystem.effect;

		VisualElement l_visualElement = l_spellDisplayNew.Q<VisualElement>("spell-ranks");
		Button l_button = l_visualElement.Q<Button>("spell-base");
		Label l_rankHeader = l_button.Q<Label>("spell-rank-header");
		l_rankHeader.text = $"Int : {p_spell.newSystem.initial.requiredInt} / {p_spell.newSystem.initial.cost} zéon" +
							$"{p_spell.newSystem.initial.maintainType switch {MaintainType.Non => "", MaintainType.Round => $"\nMaintien : {p_spell.newSystem.initial.maintain}", MaintainType.Daily => $"\nMaintien : {p_spell.newSystem.initial.maintain} Quotidien", MaintainType.ImiterUnSort => "Comme le sort imité", _ => throw new ArgumentOutOfRangeException()}}\n";
		Label l_rankValue = l_button.Q<Label>("spell-rank-value");
		l_rankValue.text = p_spell.newSystem.initial.effectValues;
		l_button.clickable.clicked += () => OnSpellRankClicked(p_spell, Rank.Initial);

		l_button = l_visualElement.Q<Button>("spell-intermediate");
		l_rankHeader = l_button.Q<Label>("spell-rank-header");
		l_rankHeader.text = $"Int : {p_spell.newSystem.intermediaire.requiredInt} / {p_spell.newSystem.intermediaire.cost} zéon" +
							$"{p_spell.newSystem.intermediaire.maintainType switch {MaintainType.Non => "", MaintainType.Round => $"\nMaintien : {p_spell.newSystem.intermediaire.maintain}", MaintainType.Daily => $"\nMaintien : {p_spell.newSystem.intermediaire.maintain} Quotidien", MaintainType.ImiterUnSort => "Comme le sort imité", _ => throw new ArgumentOutOfRangeException()}}\n";
		l_rankValue = l_button.Q<Label>("spell-rank-value");
		l_rankValue.text = p_spell.newSystem.intermediaire.effectValues;
		l_button.clickable.clicked += () => OnSpellRankClicked(p_spell, Rank.Intermédiaire);

		l_button = l_visualElement.Q<Button>("spell-advanced");
		l_rankHeader = l_button.Q<Label>("spell-rank-header");
		l_rankHeader.text = $"Int : {p_spell.newSystem.avance.requiredInt} / {p_spell.newSystem.avance.cost} zéon" +
							$"{p_spell.newSystem.avance.maintainType switch {MaintainType.Non => "", MaintainType.Round => $"\nMaintien : {p_spell.newSystem.avance.maintain}", MaintainType.Daily => $"\nMaintien : {p_spell.newSystem.avance.maintain} Quotidien", MaintainType.ImiterUnSort => "Comme le sort imité", _ => throw new ArgumentOutOfRangeException()}}\n";
		l_rankValue = l_button.Q<Label>("spell-rank-value");
		l_rankValue.text = p_spell.newSystem.avance.effectValues;
		l_button.clickable.clicked += () => OnSpellRankClicked(p_spell, Rank.Avancé);


		l_button = l_visualElement.Q<Button>("spell-arcane");
		l_rankHeader = l_button.Q<Label>("spell-rank-header");
		l_rankHeader.text = $"Int : {p_spell.newSystem.arcane.requiredInt} / {p_spell.newSystem.arcane.cost} zéon" +
							$"{p_spell.newSystem.arcane.maintainType switch {MaintainType.Non => "", MaintainType.Round => $"\nMaintien : {p_spell.newSystem.arcane.maintain}", MaintainType.Daily => $"\nMaintien : {p_spell.newSystem.arcane.maintain} Quotidien", MaintainType.ImiterUnSort => "Comme le sort imité", _ => throw new ArgumentOutOfRangeException()}}\n";
		l_rankValue = l_button.Q<Label>("spell-rank-value");
		l_rankValue.text = p_spell.newSystem.arcane.effectValues;
		l_button.clickable.clicked += () => OnSpellRankClicked(p_spell, Rank.Arcane);


		l_label = l_spellDisplayNew.Q<Label>("spell-types");
		l_label.text = string.Join(", ", p_spell.spellTypes.Select(p_spellType => p_spellType.ToString()));
	}

	private void OnSpellRankClicked(Spell p_spell, Rank p_rank)
	{
		m_spellLevelInfoWindow.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
		Label l_title = m_spellLevelInfoWindow.Q<Label>("spell-title");
		l_title.text = p_rank.ToString();
		l_title.style.backgroundColor = new StyleColor(m_rankColors[p_rank]);

		Label l_label = m_spellLevelInfoWindow.Q<Label>("spell-rank-header");
		l_label.text = p_rank switch
		{
			Rank.Initial => $"Int : {p_spell.newSystem.initial.requiredInt} / {p_spell.newSystem.initial.cost} zéon{p_spell.newSystem.initial.maintainType switch {MaintainType.Non => "", MaintainType.Round => $"\nMaintien : {p_spell.newSystem.initial.maintain}", MaintainType.Daily => $"\nMaintien : {p_spell.newSystem.initial.maintain} Quotidien", MaintainType.ImiterUnSort => "Comme le sort imité", _ => throw new ArgumentOutOfRangeException()}}\n",
			Rank.Intermédiaire => $"Int : {p_spell.newSystem.intermediaire.requiredInt} / {p_spell.newSystem.intermediaire.cost} zéon{p_spell.newSystem.intermediaire.maintainType switch {MaintainType.Non => "", MaintainType.Round => $"\nMaintien : {p_spell.newSystem.intermediaire.maintain}", MaintainType.Daily => $"\nMaintien : {p_spell.newSystem.intermediaire.maintain} Quotidien", MaintainType.ImiterUnSort => "Comme le sort imité", _ => throw new ArgumentOutOfRangeException()}}\n",
			Rank.Avancé => $"Int : {p_spell.newSystem.avance.requiredInt} / {p_spell.newSystem.avance.cost} zéon{p_spell.newSystem.avance.maintainType switch {MaintainType.Non => "", MaintainType.Round => $"\nMaintien : {p_spell.newSystem.avance.maintain}", MaintainType.Daily => $"\nMaintien : {p_spell.newSystem.avance.maintain} Quotidien", MaintainType.ImiterUnSort => "Comme le sort imité", _ => throw new ArgumentOutOfRangeException()}}\n",
			Rank.Arcane => $"Int : {p_spell.newSystem.arcane.requiredInt} / {p_spell.newSystem.arcane.cost} zéon{p_spell.newSystem.arcane.maintainType switch {MaintainType.Non => "", MaintainType.Round => $"\nMaintien : {p_spell.newSystem.arcane.maintain}", MaintainType.Daily => $"\nMaintien : {p_spell.newSystem.arcane.maintain} Quotidien", MaintainType.ImiterUnSort => "Comme le sort imité", _ => throw new ArgumentOutOfRangeException()}}\n",
			_ => throw new ArgumentOutOfRangeException(nameof(p_rank), p_rank, null)
		};

		l_label = m_spellLevelInfoWindow.Q<Label>("spell-rank-value");
		l_label.text = p_rank switch
		{
			Rank.Initial => p_spell.newSystem.initial.effectValues,
			Rank.Intermédiaire => p_spell.newSystem.intermediaire.effectValues,
			Rank.Avancé => p_spell.newSystem.avance.effectValues,
			Rank.Arcane => p_spell.newSystem.arcane.effectValues,
			_ => throw new ArgumentOutOfRangeException(nameof(p_rank), p_rank, null)
		};
	}

	private void SetupOldSpell(Spell p_spell)
	{
		PathGraphics l_pathGraphics = m_magicLibraries.PathGraphicsMap[p_spell.pathReference];
		m_spellInfoOldSystemWindow.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);

		ScrollView l_spellDisplayOld = m_spellInfoOldSystemWindow.Q<ScrollView>("holder");

		l_spellDisplayOld.style.borderTopColor = new StyleColor(l_pathGraphics.color);
		l_spellDisplayOld.style.borderBottomColor = new StyleColor(l_pathGraphics.color);
		l_spellDisplayOld.style.borderLeftColor = new StyleColor(l_pathGraphics.color);
		l_spellDisplayOld.style.borderRightColor = new StyleColor(l_pathGraphics.color);

		Label l_label = l_spellDisplayOld.Q<Label>("spell-name");
		l_label.text = p_spell.name;
		l_label.style.color = new StyleColor(l_pathGraphics.color);

		l_label = l_spellDisplayOld.Q<Label>("spell-level");
		l_label.text = p_spell.level.ToString();

		l_label = l_spellDisplayOld.Q<Label>("spell-actions");
		l_label.text = p_spell.action.ToString();

		l_label = l_spellDisplayOld.Q<Label>("spell-cost");
		l_label.text = p_spell.oldSystem.cost.ToString();

		l_label = l_spellDisplayOld.Q<Label>("spell-effect");
		l_label.text = "<b>Effet : </b>"+ p_spell.oldSystem.effect;

		l_label = l_spellDisplayOld.Q<Label>("spell-additional-effect");
		l_label.text = p_spell.oldSystem.additionalEffect;

		l_label = l_spellDisplayOld.Q<Label>("spell-max-cost");
		l_label.text = "Intelligence x " + p_spell.oldSystem.maxCost;

		l_label = l_spellDisplayOld.Q<Label>("spell-maintain");
		l_label.text = p_spell.oldSystem.maintainType switch
		{
			MaintainType.Non => "Non",
			MaintainType.Round => $"1 pour {p_spell.oldSystem.maintainDivider} ({p_spell.oldSystem.maintainCost})",
			MaintainType.Daily => $"1 pour {p_spell.oldSystem.maintainDivider} ({p_spell.oldSystem.maintainCost}) <b>Quotidien</b>",
			MaintainType.ImiterUnSort => "Spécial",
			_ => throw new ArgumentOutOfRangeException()
		};

		l_label = l_spellDisplayOld.Q<Label>("spell-types");
		l_label.text = string.Join(", ", p_spell.spellTypes.Select(p_spellType => p_spellType.ToString()));
	}

	private void DisplayPathInfos(PathDisplay p_pathDisplay)
	{
		m_pathInfoWindow.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);

		SpellPath l_path = p_pathDisplay.m_path;
		Label l_label = m_pathInfoWindow.Q<Label>("path-name");
		l_label.text = l_path.name;

		l_label = m_pathInfoWindow.Q<Label>("path-opposed");
		l_label.text = l_path.OpposedPath.Length == 0 ? "" : "<b>Voies opposées : </b>" + string.Join(", ", l_path.OpposedPath.Select(p_path => p_path.name));

		l_label = m_pathInfoWindow.Q<Label>("path-type");
		l_label.text = "<b>Type : </b>" + l_path.pathType;

		l_label = m_pathInfoWindow.Q<Label>("path-description");
		l_label.text = l_path.description;

		l_label = m_pathInfoWindow.Q<Label>("path-spells");
		l_label.text = "<b>Sorts : </b>" + string.Join(", ", l_path.spells.Select(p_spell => p_spell.name));
	}

	public void SelectLibrary(int p_libraryIndex)
	{
		MagicLibrary l_selectedLibrary = m_magicLibraries.m_magicPaths[p_libraryIndex];
		for (int i = 0; i < m_pathDisplays.Count; i++)
		{
			KeyValuePair<MagicLibrary, PathDisplay[]> l_keyValuePair = m_pathDisplays.ElementAt(i);
			bool l_isSelected = l_keyValuePair.Key == l_selectedLibrary;
			for (int j = 0; j < l_keyValuePair.Value.Length; j++)
			{
				PathDisplay l_pathDisplay = l_keyValuePair.Value[j];
				l_pathDisplay.m_pathDisplay.style.display = new StyleEnum<DisplayStyle>(l_isSelected ? DisplayStyle.Flex : DisplayStyle.None);
				bool isOn = m_pathSelection.m_pathSelection[i][j];
				foreach (SpellDisplay l_spellDisplay in l_pathDisplay.m_spellDisplays)
                {
                    l_spellDisplay.m_VisualElement.style.display = new StyleEnum<DisplayStyle>(l_isSelected && isOn ? DisplayStyle.Flex : DisplayStyle.None);
                }
            }
		}
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			BackButtonPressed();
		}
	}

	private void BackButtonPressed()
	{
		if (m_spellLevelInfoWindow.style.display.value == DisplayStyle.Flex)
			m_spellLevelInfoWindow.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
		else if (m_spellInfoNewSystemWindow.style.display.value == DisplayStyle.Flex)
			m_spellInfoNewSystemWindow.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
		else if (m_spellInfoOldSystemWindow.style.display.value == DisplayStyle.Flex)
			m_spellInfoOldSystemWindow.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
		else if (m_pathInfoWindow.style.display.value == DisplayStyle.Flex)
			m_pathInfoWindow.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
		else
			Application.Quit();
	}
}
