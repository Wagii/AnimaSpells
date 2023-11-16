using System;
using System.Collections.Generic;
using System.Linq;
using Data.Scripts;
using Scriptables;
using UnityEngine;
using UnityEngine.UIElements;
using Action = System.Action;

public class Displayer : MonoBehaviour
{
	public const string BOOK_SELECTION = "book_selection";
	private const string SELECTED_BOOK = "selected_book";
	private const string SELECTED_THEME = "selected_theme";
	private const string SELECTED_LIBRARY = "selected_library";
	private const string SELECTED_SYSTEM = "selected_system";

	private static readonly Color BaseColor = new Color(96, 103, 191, 255);
	private static readonly Color IntermediateColor = new Color(96, 191, 103, 255);
	private static readonly Color AdvancedColor = new Color(191, 156, 96, 255);
	private static readonly Color ArcaneColor = new Color(191, 96, 103, 255);
	private static readonly Dictionary<Rank, Color> RankColors = new Dictionary<Rank, Color>()
	{
		{Rank.Initial, BaseColor},
		{Rank.Intermédiaire, IntermediateColor},
		{Rank.Avancé, AdvancedColor},
		{Rank.Arcane, ArcaneColor}
	};

	public static Displayer m_Instance;
	public static MagicLibraries MagicLibraries => m_Instance.m_magicLibraries;

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
    [SerializeField] private int m_asyncOperationBufferSize = 100;

	private static readonly Dictionary<MagicLibrary, PathDisplay[]> PathDisplays = new Dictionary<MagicLibrary, PathDisplay[]>();
	private static VisualElement s_root;
	private static ScrollView s_pathScrollView;
	private static ScrollView s_spellScrollView;
	private static PathSelection s_pathSelection;
	private static VisualElement s_infoWindowHolder;
	private static VisualElement s_pathInfoWindow;
	private static VisualElement s_spellInfoNewSystemWindow;
	private static VisualElement s_spellInfoOldSystemWindow;
	private static VisualElement s_spellLevelInfoWindow;
	private static VisualElement s_waitingScreen;

	private static Queue<Action> s_asyncOperationQueue = new Queue<Action>();
	private PathSelection m_bookStyleSelection;
	private static int s_selectedLibrary;
	private static bool s_displayingBookStyle;
	private static bool s_newSystem;
	private static bool s_themeSelected;

	private void Awake()
	{
		if (m_Instance != null)
		{
			DestroyImmediate(this);
			return;
		}

		m_Instance = this;
	}

	private void Start()
	{
		m_magicLibraries.PathGraphicsMap = new Dictionary<string, PathGraphics>();
		foreach (PathGraphics l_pathGraphics in m_magicLibraries.m_PathGraphicsArray)
			m_magicLibraries.PathGraphicsMap.TryAdd(l_pathGraphics.pathReference, l_pathGraphics);

		// return;
		s_root = m_uiDocument.rootVisualElement;

		LoadPlayerPrefs();

		SetupInfoWindows();

		GeneratePathDisplays();

		PrepareOptionButtons();

		ToggleAllPaths(s_pathSelection.m_PathSelection[s_selectedLibrary].All(p_element => p_element == false));
	}

	private void GeneratePathDisplays()
	{
		s_pathScrollView = s_root.Q<ScrollView>("path-view");
		s_spellScrollView = s_root.Q<ScrollView>("spell-view");

		s_asyncOperationQueue.Enqueue(() => s_waitingScreen.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex));
		// TODO : GIGA Optimize this
		for (int l_libraryIndex = 0; l_libraryIndex < m_magicLibraries.m_magicPaths.Length; l_libraryIndex++)
		{
			MagicLibrary l_magicLibrary = m_magicLibraries.m_magicPaths[l_libraryIndex];

			List<PathDisplay> l_pathDisplays = new();
			for (int l_pathIndex = 0; l_pathIndex < l_magicLibrary.paths.Length; l_pathIndex++)
			{

				SpellPath l_spellPath = l_magicLibrary.paths[l_pathIndex];
				PathGraphics l_pathGraphics = m_magicLibraries.PathGraphicsMap[l_spellPath.name];


				// Path display generation
				Toggle l_pathElement = m_pathDisplayAsset.CloneTree()[0].Q<Toggle>();
				l_pathElement.style.borderLeftColor = new StyleColor(l_pathGraphics.color);
				l_pathElement.style.borderRightColor = new StyleColor(l_pathGraphics.color);
				l_pathElement.style.borderTopColor = new StyleColor(l_pathGraphics.color);
				l_pathElement.style.borderBottomColor = new StyleColor(l_pathGraphics.color);

				l_pathElement.SetValueWithoutNotify(s_pathSelection.m_PathSelection[l_libraryIndex][l_pathIndex]);

				VisualElement l_pathIcon = l_pathElement.Q("path-icon");
				l_pathIcon.style.backgroundImage = new StyleBackground(l_pathGraphics.texture2D);


				int l_index = l_libraryIndex;
				s_asyncOperationQueue.Enqueue(() =>
				{
					l_pathElement.style.display = new StyleEnum<DisplayStyle>(s_selectedLibrary == l_index ? DisplayStyle.Flex : DisplayStyle.None);
					s_pathScrollView.Add(l_pathElement);
				});

				PathDisplay l_pathDisplay = new()
				{
					m_Path = l_spellPath,
					m_PathDisplay = l_pathElement,
					m_SpellDisplays = new List<SpellDisplay>(),
					m_PathGraphics = l_pathGraphics
				};

				GenerateSpellDisplays(l_pathDisplay,
					(s_displayingBookStyle && m_bookStyleSelection.m_PathSelection[l_libraryIndex][l_pathIndex]) ||
					(!s_displayingBookStyle && s_pathSelection.m_PathSelection[l_libraryIndex][l_pathIndex]));

				l_pathElement.AddManipulator(new HoldManipulator(this, m_holdTime, () => DisplayPathInfos(l_pathDisplay)));

				// Path callback registration
				int l_localLibraryIndex = l_libraryIndex;
				int l_localPathIndex = l_pathIndex;
				l_pathElement.RegisterValueChangedCallback(p_evt =>
				{
					if (p_evt.newValue && !s_displayingBookStyle)
					{
						PathDisplay[] l_displays = PathDisplays.ElementAt(l_localLibraryIndex).Value;
						for (int i = 0; i < s_pathSelection.m_PathSelection[l_localLibraryIndex].Length; i++)
						{
							if (i == l_localPathIndex) continue;
							s_pathSelection.m_PathSelection[l_localLibraryIndex][i] = false;
							l_displays[i].m_PathDisplay.SetValueWithoutNotify(false);
						}
					}
					s_pathSelection.m_PathSelection[l_localLibraryIndex][l_localPathIndex] = p_evt.newValue;
					s_pathSelection.SaveSelection();
					bool displayEverything = s_pathSelection.m_PathSelection[l_localLibraryIndex].All(p_element => p_element == false);
					ToggleAllPaths(displayEverything);
				});

				l_pathDisplays.Add(l_pathDisplay);
			}

			PathDisplays.Add(l_magicLibrary, l_pathDisplays.ToArray());
		}
		s_asyncOperationQueue.Enqueue(() => s_waitingScreen.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None));
	}

	private void GenerateSpellDisplays(PathDisplay p_pathDisplay, bool p_displaySpell)
	{
		SpellDisplay[] l_spellDisplays = new SpellDisplay[p_pathDisplay.m_Path.spells.Length];
		for (int i = 0; i < p_pathDisplay.m_Path.spells.Length; i++)
		{
			// Spell display generation
			Button l_spellDisplayAsset = m_spellDisplayAsset.CloneTree()[0].Q<Button>();

			// Spell display setup
			l_spellDisplayAsset.style.borderTopColor = new StyleColor(p_pathDisplay.m_PathGraphics.color);
			l_spellDisplayAsset.style.borderBottomColor = new StyleColor(p_pathDisplay.m_PathGraphics.color);
			l_spellDisplayAsset.style.borderLeftColor = new StyleColor(p_pathDisplay.m_PathGraphics.color);
			l_spellDisplayAsset.style.borderRightColor = new StyleColor(p_pathDisplay.m_PathGraphics.color);
			l_spellDisplayAsset.style.backgroundColor =
				new StyleColor(Color.Lerp(p_pathDisplay.m_PathGraphics.color, Color.black, 0.16f));

			l_spellDisplayAsset.Q<Label>("spell-name").text = p_pathDisplay.m_Path.spells[i].name;
			l_spellDisplayAsset.Q<Label>("spell-path").text = p_pathDisplay.m_Path.spells[i].pathReference;
			l_spellDisplayAsset.Q<Label>("spell-level").text = p_pathDisplay.m_Path.spells[i].level.ToString();

			SpellDisplay l_spellDisplay = new SpellDisplay
			{
				m_Spell = p_pathDisplay.m_Path.spells[i],
				m_VisualElement = l_spellDisplayAsset,
				m_PathGraphics = p_pathDisplay.m_PathGraphics
			};

			l_spellDisplayAsset.Q<Button>().clickable.clicked += () => OnSpellClicked(l_spellDisplay);

			l_spellDisplays[i] = l_spellDisplay;

			s_asyncOperationQueue.Enqueue(() =>
			{
				s_spellScrollView.Add(l_spellDisplayAsset);
				l_spellDisplayAsset.style.display = new StyleEnum<DisplayStyle>(p_displaySpell
					? DisplayStyle.Flex
					: DisplayStyle.None);
			});

		}
		p_pathDisplay.m_SpellDisplays = l_spellDisplays.ToList();
	}

	private void LoadPlayerPrefs()
	{
		s_pathSelection = PathSelection.LoadSelection();
		m_bookStyleSelection = PathSelection.LoadSelection(BOOK_SELECTION);

		s_displayingBookStyle = PlayerPrefs.GetInt(SELECTED_BOOK, 0) == 1;
		s_selectedLibrary = PlayerPrefs.GetInt(SELECTED_LIBRARY, 0);
		s_newSystem = PlayerPrefs.GetInt(SELECTED_SYSTEM, 1) == 1;
		s_themeSelected = PlayerPrefs.GetInt(SELECTED_THEME, 1) == 1;
	}

	private void SetupInfoWindows()
	{
		s_infoWindowHolder = s_root.Q<VisualElement>("info-window-holder");

		s_pathInfoWindow = m_pathInfoWindowAsset.CloneTree()[0];
		s_spellInfoNewSystemWindow = m_spellInfoNewSystemWindowAsset.CloneTree()[0];
		s_spellInfoOldSystemWindow = m_spellInfoOldSystemWindowAsset.CloneTree()[0];
		s_spellLevelInfoWindow = m_spellLevelInfoWindowAsset.CloneTree()[0];

		s_infoWindowHolder.Add(s_pathInfoWindow);
		s_infoWindowHolder.Add(s_spellInfoNewSystemWindow);
		s_infoWindowHolder.Add(s_spellInfoOldSystemWindow);
		s_infoWindowHolder.Add(s_spellLevelInfoWindow);

		Button l_prevButton = s_spellLevelInfoWindow.Q<Button>("prev-button");
		l_prevButton.clickable.clicked += () => s_spellLevelInfoWindow.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
		l_prevButton = s_spellInfoNewSystemWindow.Q<Button>("prev-button");
		l_prevButton.clickable.clicked += () => s_spellInfoNewSystemWindow.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
		l_prevButton = s_spellInfoOldSystemWindow.Q<Button>("prev-button");
		l_prevButton.clickable.clicked += () => s_spellInfoOldSystemWindow.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
		l_prevButton = s_pathInfoWindow.Q<Button>("prev-button");
		l_prevButton.clickable.clicked += () => s_pathInfoWindow.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);

		s_pathInfoWindow.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
		s_spellInfoNewSystemWindow.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
		s_spellInfoOldSystemWindow.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
		s_spellLevelInfoWindow.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);

		s_waitingScreen = s_root.Q<VisualElement>("waiting-screen");
	}

	private void PrepareOptionButtons()
	{
		SlideToggle l_bookMode = s_root.Q<SlideToggle>("book-toggle");
		l_bookMode.RegisterValueChangedCallback(BookModeChanged);

		SlideToggle l_theme = s_root.Q<SlideToggle>("theme-toggle");
		l_theme.RegisterValueChangedCallback(ThemeChanged);

		DropdownField l_libraries = s_root.Q<DropdownField>("dropdown-library");
		l_libraries.choices = m_magicLibraries.m_magicPaths.Select(p_library => p_library.name).ToList();
		l_libraries.RegisterValueChangedCallback(_ =>
		{
			s_selectedLibrary = l_libraries.index;
			PlayerPrefs.SetInt(SELECTED_LIBRARY, s_selectedLibrary);
			PlayerPrefs.Save();
			SelectLibrary(s_selectedLibrary);
		});

		SlideToggle l_systems = s_root.Q<SlideToggle>("system-toggle");
		l_systems.RegisterValueChangedCallback(OnSystemChanged);

		l_bookMode.value = s_displayingBookStyle;
		l_theme.value = s_themeSelected;
		l_libraries.index = s_selectedLibrary;
		l_systems.value = s_newSystem;
	}

	private void BookModeChanged(ChangeEvent<bool> p_evt)
	{
		// TODO : Optimize this
		s_displayingBookStyle = p_evt.newValue;
		if (s_displayingBookStyle)
		{
			s_pathSelection = new PathSelection(m_bookStyleSelection.ToString());
			for (int i = 0; i < PathDisplays.Count; i++)
			{
				PathDisplay[] l_pathDisplays = PathDisplays.ElementAt(i).Value;
				for (int j = 0; j < l_pathDisplays.Length; j++)
				{
					l_pathDisplays[j].m_PathDisplay.SetValueWithoutNotify(s_pathSelection.m_PathSelection[i][j]);
				}
			}

			ToggleAllPaths(s_pathSelection.m_PathSelection[s_selectedLibrary].All(p_element => p_element == false));
		}
		else
		{
			m_bookStyleSelection = new PathSelection(s_pathSelection.ToString(), BOOK_SELECTION);
			for (int i = 0; i < PathDisplays.Count; i++)
			{
				PathDisplay[] l_pathDisplays = PathDisplays.ElementAt(i).Value;
				bool l_firstSelected = false;
				for (int j = 0; j < l_pathDisplays.Length; j++)
				{
					Toggle l_toggle = l_pathDisplays[j].m_PathDisplay.Q<Toggle>();
					if (s_pathSelection.m_PathSelection[i][j] && !l_firstSelected)
					{
						l_firstSelected = true;
					}
					else
					{
						l_toggle.SetValueWithoutNotify(false);
						s_pathSelection.m_PathSelection[i][j] = false;
					}
				}
			}
		}

		PlayerPrefs.SetString(BOOK_SELECTION, m_bookStyleSelection.ToString());
		PlayerPrefs.SetString("path_selection", s_pathSelection.ToString());
		PlayerPrefs.SetInt(SELECTED_BOOK, s_displayingBookStyle ? 1 : 0);
		PlayerPrefs.Save();
	}

	private void ThemeChanged(ChangeEvent<bool> p_evt)
	{
		if (p_evt.newValue)
		{
			s_root.styleSheets.Remove(m_lightTheme);
			s_root.styleSheets.Add(m_darkTheme);
		}
		else
		{
			s_root.styleSheets.Remove(m_darkTheme);
			s_root.styleSheets.Add(m_lightTheme);
		}

		PlayerPrefs.SetInt(SELECTED_THEME, p_evt.newValue ? 1 : 0);
		PlayerPrefs.Save();
	}

	private static void OnSystemChanged(ChangeEvent<bool> p_index)
	{
		s_newSystem = p_index.newValue;
		PlayerPrefs.SetInt(SELECTED_SYSTEM, p_index.newValue ? 1 : 0);
		PlayerPrefs.Save();
	}

	private static void ToggleAllPaths(bool p_displayEverything)
	{
		// s_asyncOperationQueue.Enqueue(() => s_waitingScreen.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex));
		PathDisplay[] pathDisplays = PathDisplays.ElementAt(s_selectedLibrary).Value;
		for (int pathIndex = pathDisplays.Length - 1; pathIndex >= 0; pathIndex--)
        {
			PathDisplay pathDisplay = pathDisplays[pathIndex];
			bool pathDisplaying = s_pathSelection.m_PathSelection[s_selectedLibrary][pathIndex];
			for (int spellIndex = pathDisplay.m_SpellDisplays.Count - 1; spellIndex >= 0; spellIndex--)
			{
				int l_index = spellIndex;
				s_asyncOperationQueue.Enqueue(() =>
					pathDisplay.m_SpellDisplays[l_index].m_VisualElement.style.display =
						new StyleEnum<DisplayStyle>(p_displayEverything || pathDisplaying
							? DisplayStyle.Flex
							: DisplayStyle.None));
			}
        }
		// s_asyncOperationQueue.Enqueue(() => s_waitingScreen.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None));
    }

	private static void OnSpellClicked(SpellDisplay p_spell)
	{
		if (s_newSystem)
			SetupNewSpell(p_spell);
		else
			SetupOldSpell(p_spell);
	}

	private static void SetupNewSpell(SpellDisplay p_spellDisplay)
	{
		PathGraphics l_pathGraphics = p_spellDisplay.m_PathGraphics;
		Spell l_spell = p_spellDisplay.m_Spell;

		ScrollView l_spellDisplayNew = s_spellInfoNewSystemWindow.Q<ScrollView>("holder");

		l_spellDisplayNew.style.borderTopColor = new StyleColor(l_pathGraphics.color);
		l_spellDisplayNew.style.borderBottomColor = new StyleColor(l_pathGraphics.color);
		l_spellDisplayNew.style.borderLeftColor = new StyleColor(l_pathGraphics.color);
		l_spellDisplayNew.style.borderRightColor = new StyleColor(l_pathGraphics.color);

		Label l_label = l_spellDisplayNew.Q<Label>("spell-name");
		l_label.text = l_spell.name;
		l_label.style.color = new StyleColor(l_pathGraphics.color);
		l_label.style.unityTextOutlineColor = new StyleColor(l_pathGraphics.color);

		l_label = l_spellDisplayNew.Q<Label>("spell-level");
		l_label.text = l_spell.level.ToString();

		l_label = l_spellDisplayNew.Q<Label>("spell-actions");
		l_label.text = l_spell.action.ToString();

		l_label = l_spellDisplayNew.Q<Label>("spell-forbidden");
		l_label.style.display = new StyleEnum<DisplayStyle>(l_spell.forbiddenPaths.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None);
		l_label.text = $"<b>Voies Fermées : </b>{string.Join(", ", l_spell.forbiddenPaths)}";

		l_label = l_spellDisplayNew.Q<Label>("spell-effect");
		l_label.text = "<b>Effet : </b>"+ l_spell.newSystem.effect;

		VisualElement l_visualElement = l_spellDisplayNew.Q<VisualElement>("spell-ranks");
		Button l_button = l_visualElement.Q<Button>("spell-base");
		Label l_rankHeader = l_button.Q<Label>("spell-rank-header");
		l_rankHeader.text = $"Int : {l_spell.newSystem.initial.requiredInt} / {l_spell.newSystem.initial.cost} zéon" +
							$"{l_spell.newSystem.initial.maintainType switch {MaintainType.Non => "", MaintainType.Round => $"\nMaintien : {l_spell.newSystem.initial.maintain}", MaintainType.Daily => $"\nMaintien : {l_spell.newSystem.initial.maintain} Quotidien", MaintainType.ImiterUnSort => "Comme le sort imité", _ => throw new ArgumentOutOfRangeException()}}";
		Label l_rankValue = l_button.Q<Label>("spell-rank-value");
		l_rankValue.text = l_spell.newSystem.initial.effectValues;
		l_button.clickable.clicked += () => OnSpellRankClicked(l_spell, Rank.Initial);

		l_button = l_visualElement.Q<Button>("spell-intermediate");
		l_rankHeader = l_button.Q<Label>("spell-rank-header");
		l_rankHeader.text = $"Int : {l_spell.newSystem.intermediaire.requiredInt} / {l_spell.newSystem.intermediaire.cost} zéon" +
							$"{l_spell.newSystem.intermediaire.maintainType switch {MaintainType.Non => "", MaintainType.Round => $"\nMaintien : {l_spell.newSystem.intermediaire.maintain}", MaintainType.Daily => $"\nMaintien : {l_spell.newSystem.intermediaire.maintain} Quotidien", MaintainType.ImiterUnSort => "Comme le sort imité", _ => throw new ArgumentOutOfRangeException()}}";
		l_rankValue = l_button.Q<Label>("spell-rank-value");
		l_rankValue.text = l_spell.newSystem.intermediaire.effectValues;
		l_button.clickable.clicked += () => OnSpellRankClicked(l_spell, Rank.Intermédiaire);

		l_button = l_visualElement.Q<Button>("spell-advanced");
		l_rankHeader = l_button.Q<Label>("spell-rank-header");
		l_rankHeader.text = $"Int : {l_spell.newSystem.avance.requiredInt} / {l_spell.newSystem.avance.cost} zéon" +
							$"{l_spell.newSystem.avance.maintainType switch {MaintainType.Non => "", MaintainType.Round => $"\nMaintien : {l_spell.newSystem.avance.maintain}", MaintainType.Daily => $"\nMaintien : {l_spell.newSystem.avance.maintain} Quotidien", MaintainType.ImiterUnSort => "Comme le sort imité", _ => throw new ArgumentOutOfRangeException()}}";
		l_rankValue = l_button.Q<Label>("spell-rank-value");
		l_rankValue.text = l_spell.newSystem.avance.effectValues;
		l_button.clickable.clicked += () => OnSpellRankClicked(l_spell, Rank.Avancé);


		l_button = l_visualElement.Q<Button>("spell-arcane");
		l_rankHeader = l_button.Q<Label>("spell-rank-header");
		l_rankHeader.text = $"Int : {l_spell.newSystem.arcane.requiredInt} / {l_spell.newSystem.arcane.cost} zéon" +
							$"{l_spell.newSystem.arcane.maintainType switch {MaintainType.Non => "", MaintainType.Round => $"\nMaintien : {l_spell.newSystem.arcane.maintain}", MaintainType.Daily => $"\nMaintien : {l_spell.newSystem.arcane.maintain} Quotidien", MaintainType.ImiterUnSort => "Comme le sort imité", _ => throw new ArgumentOutOfRangeException()}}";
		l_rankValue = l_button.Q<Label>("spell-rank-value");
		l_rankValue.text = l_spell.newSystem.arcane.effectValues;
		l_button.clickable.clicked += () => OnSpellRankClicked(l_spell, Rank.Arcane);


		l_label = l_spellDisplayNew.Q<Label>("spell-types");
		l_label.text = string.Join(", ", l_spell.spellTypes.Select(p_spellType => p_spellType.ToString()));

		s_spellInfoNewSystemWindow.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
	}

	private static void OnSpellRankClicked(Spell p_spell, Rank p_rank)
	{
		s_spellLevelInfoWindow.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
		Label l_title = s_spellLevelInfoWindow.Q<Label>("spell-title");
		l_title.style.backgroundColor = new StyleColor(RankColors[p_rank]/255f);
		l_title.text = p_rank.ToString();

		Label l_label = s_spellLevelInfoWindow.Q<Label>("spell-metas");
		l_label.text = p_rank switch
		{
			Rank.Initial => $"Int : {p_spell.newSystem.initial.requiredInt} / {p_spell.newSystem.initial.cost} zéon{p_spell.newSystem.initial.maintainType switch {MaintainType.Non => "", MaintainType.Round => $"\nMaintien : {p_spell.newSystem.initial.maintain}", MaintainType.Daily => $"\nMaintien : {p_spell.newSystem.initial.maintain} Quotidien", MaintainType.ImiterUnSort => "Comme le sort imité", _ => throw new ArgumentOutOfRangeException()}}",
			Rank.Intermédiaire => $"Int : {p_spell.newSystem.intermediaire.requiredInt} / {p_spell.newSystem.intermediaire.cost} zéon{p_spell.newSystem.intermediaire.maintainType switch {MaintainType.Non => "", MaintainType.Round => $"\nMaintien : {p_spell.newSystem.intermediaire.maintain}", MaintainType.Daily => $"\nMaintien : {p_spell.newSystem.intermediaire.maintain} Quotidien", MaintainType.ImiterUnSort => "Comme le sort imité", _ => throw new ArgumentOutOfRangeException()}}",
			Rank.Avancé => $"Int : {p_spell.newSystem.avance.requiredInt} / {p_spell.newSystem.avance.cost} zéon{p_spell.newSystem.avance.maintainType switch {MaintainType.Non => "", MaintainType.Round => $"\nMaintien : {p_spell.newSystem.avance.maintain}", MaintainType.Daily => $"\nMaintien : {p_spell.newSystem.avance.maintain} Quotidien", MaintainType.ImiterUnSort => "Comme le sort imité", _ => throw new ArgumentOutOfRangeException()}}",
			Rank.Arcane => $"Int : {p_spell.newSystem.arcane.requiredInt} / {p_spell.newSystem.arcane.cost} zéon{p_spell.newSystem.arcane.maintainType switch {MaintainType.Non => "", MaintainType.Round => $"\nMaintien : {p_spell.newSystem.arcane.maintain}", MaintainType.Daily => $"\nMaintien : {p_spell.newSystem.arcane.maintain} Quotidien", MaintainType.ImiterUnSort => "Comme le sort imité", _ => throw new ArgumentOutOfRangeException()}}",
			_ => throw new ArgumentOutOfRangeException(nameof(p_rank), p_rank, null)
		};

		l_label = s_spellLevelInfoWindow.Q<Label>("spell-value");
		l_label.text = p_rank switch
		{
			Rank.Initial => p_spell.newSystem.initial.effectValues,
			Rank.Intermédiaire => p_spell.newSystem.intermediaire.effectValues,
			Rank.Avancé => p_spell.newSystem.avance.effectValues,
			Rank.Arcane => p_spell.newSystem.arcane.effectValues,
			_ => throw new ArgumentOutOfRangeException(nameof(p_rank), p_rank, null)
		};
	}

	private static void SetupOldSpell(SpellDisplay p_spellDisplay)
	{
		PathGraphics l_pathGraphics = p_spellDisplay.m_PathGraphics;
		Spell l_spell = p_spellDisplay.m_Spell;

		s_spellInfoOldSystemWindow.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);

		ScrollView l_spellDisplayOld = s_spellInfoOldSystemWindow.Q<ScrollView>("holder");

		l_spellDisplayOld.style.borderTopColor = new StyleColor(l_pathGraphics.color);
		l_spellDisplayOld.style.borderBottomColor = new StyleColor(l_pathGraphics.color);
		l_spellDisplayOld.style.borderLeftColor = new StyleColor(l_pathGraphics.color);
		l_spellDisplayOld.style.borderRightColor = new StyleColor(l_pathGraphics.color);

		Label l_label = l_spellDisplayOld.Q<Label>("spell-name");
		l_label.text = l_spell.name;
		l_label.style.color = new StyleColor(l_pathGraphics.color);

		l_label = l_spellDisplayOld.Q<Label>("spell-level");
		l_label.text = l_spell.level.ToString();

		l_label = l_spellDisplayOld.Q<Label>("spell-actions");
		l_label.text = l_spell.action.ToString();

		l_label = l_spellDisplayOld.Q<Label>("spell-cost");
		l_label.text = l_spell.oldSystem.cost.ToString();

		l_label = l_spellDisplayOld.Q<Label>("spell-effect");
		l_label.text = "<b>Effet : </b>"+ l_spell.oldSystem.effect;

		l_label = l_spellDisplayOld.Q<Label>("spell-additional-effect");
		l_label.text = "<b>Effet supplémentaire : </b>" + l_spell.oldSystem.additionalEffect;

		l_label = l_spellDisplayOld.Q<Label>("spell-max-cost");
		l_label.text = "Intelligence x " + l_spell.oldSystem.maxCost;

		l_label = l_spellDisplayOld.Q<Label>("spell-maintain");
		l_label.text = l_spell.oldSystem.maintainType switch
		{
			MaintainType.Non => "Non",
			MaintainType.Round => $"1 pour {l_spell.oldSystem.maintainDivider} ({l_spell.oldSystem.maintainCost})",
			MaintainType.Daily => $"1 pour {l_spell.oldSystem.maintainDivider} ({l_spell.oldSystem.maintainCost}) <b>Quotidien</b>",
			MaintainType.ImiterUnSort => "Spécial",
			_ => throw new ArgumentOutOfRangeException()
		};

		l_label = l_spellDisplayOld.Q<Label>("spell-types");
		l_label.text = string.Join(", ", l_spell.spellTypes.Select(p_spellType => p_spellType.ToString()));

		l_label = l_spellDisplayOld.Q<Label>("spell-forbidden");
		l_label.style.display = new StyleEnum<DisplayStyle>(l_spell.forbiddenPaths.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None);
		l_label.text = $"<b>Voies Fermées : </b>{string.Join(", ", l_spell.forbiddenPaths)}";
	}

	private static void DisplayPathInfos(PathDisplay p_pathDisplay)
	{
		s_pathInfoWindow.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
		PathGraphics l_pathGraphics = p_pathDisplay.m_PathGraphics;

		ScrollView l_pathDisplay = s_pathInfoWindow.Q<ScrollView>("holder");
		l_pathDisplay.style.borderTopColor = new StyleColor(l_pathGraphics.color);
		l_pathDisplay.style.borderBottomColor = new StyleColor(l_pathGraphics.color);
		l_pathDisplay.style.borderLeftColor = new StyleColor(l_pathGraphics.color);
		l_pathDisplay.style.borderRightColor = new StyleColor(l_pathGraphics.color);

		SpellPath l_path = p_pathDisplay.m_Path;
		Label l_label = s_pathInfoWindow.Q<Label>("path-name");
		l_label.text = l_path.name;

		l_label = s_pathInfoWindow.Q<Label>("path-opposed");
		l_label.style.display = new StyleEnum<DisplayStyle>(l_path.opposedPaths?.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None);
		if (l_path.opposedPaths?.Length > 0)
			l_label.text = "<b>Voies opposées : </b>" + string.Join(", ", l_path.opposedPaths);

		l_label = s_pathInfoWindow.Q<Label>("path-type");
		l_label.text = "<b>Type : </b>" + l_path.pathType;

		l_label = s_pathInfoWindow.Q<Label>("path-description");
		l_label.text = l_path.description;

		VisualElement l_pathSpellHolder = s_pathInfoWindow.Q<VisualElement>("path-spells");
		l_pathSpellHolder.Clear();
		foreach (Spell spell in l_path.spells)
			l_pathSpellHolder.Add(new Label(spell.name + " "));
	}

	public void SelectLibrary(int p_libraryIndex)
	{
		s_asyncOperationQueue.Enqueue(() => s_waitingScreen.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex));
		MagicLibrary l_selectedLibrary = m_magicLibraries.m_magicPaths[p_libraryIndex];
		for (int i = PathDisplays.Count - 1; i >= 0; i--)
		{
			KeyValuePair<MagicLibrary, PathDisplay[]> l_keyValuePair = PathDisplays.ElementAt(i);
			bool l_isSelected = l_keyValuePair.Key == l_selectedLibrary;
			for (int j = l_keyValuePair.Value.Length - 1; j >= 0; j--)
			{
				PathDisplay l_pathDisplay = l_keyValuePair.Value[j];
				s_asyncOperationQueue.Enqueue(() =>
					l_pathDisplay.m_PathDisplay.style.display =
						new StyleEnum<DisplayStyle>(l_isSelected ? DisplayStyle.Flex : DisplayStyle.None));
			}

			for (int j = l_keyValuePair.Value.Length - 1; j >= 0; j--)
			{
				PathDisplay l_pathDisplay = l_keyValuePair.Value[j];
				bool isOn = s_pathSelection.m_PathSelection[i][j] || s_pathSelection.m_PathSelection[i].All(p_element => p_element == false);
				foreach (SpellDisplay l_spellDisplay in l_pathDisplay.m_SpellDisplays)
				{
					s_asyncOperationQueue.Enqueue(() =>
						l_spellDisplay.m_VisualElement.style.display =
							new StyleEnum<DisplayStyle>(l_isSelected && isOn ? DisplayStyle.Flex : DisplayStyle.None));
				}
            }
		}
		s_asyncOperationQueue.Enqueue(() => s_waitingScreen.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None));
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			BackButtonPressed();
		}

		for (int i = 0; i < Math.Min(m_asyncOperationBufferSize, s_asyncOperationQueue.Count); i++)
		{
			if (s_asyncOperationQueue.TryDequeue(out Action l_action))
				l_action?.Invoke();
		}
	}

	private static void BackButtonPressed()
	{
		if (s_spellLevelInfoWindow.style.display.value == DisplayStyle.Flex)
			s_spellLevelInfoWindow.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
		else if (s_spellInfoNewSystemWindow.style.display.value == DisplayStyle.Flex)
			s_spellInfoNewSystemWindow.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
		else if (s_spellInfoOldSystemWindow.style.display.value == DisplayStyle.Flex)
			s_spellInfoOldSystemWindow.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
		else if (s_pathInfoWindow.style.display.value == DisplayStyle.Flex)
			s_pathInfoWindow.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
		else
			Application.Quit();
	}
}
