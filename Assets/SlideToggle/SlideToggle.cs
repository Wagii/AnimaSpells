using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// namespace UnityEngine.UIElements
// {
    public class SlideToggle : BaseField<bool>
    {
        public new class UxmlFactory : UxmlFactory<SlideToggle, UxmlTraits>
        {
        }

        public new class UxmlTraits : BaseFieldTraits<bool, UxmlBoolAttributeDescription>
        {
            UxmlStringAttributeDescription m_LeftLabel = new UxmlStringAttributeDescription
                {name = "left-label", defaultValue = "On"};

            UxmlStringAttributeDescription m_RightLabel = new UxmlStringAttributeDescription
                {name = "right-label", defaultValue = "Off"};

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                SlideToggle st = (SlideToggle) ve;

                st.leftLabel = m_LeftLabel.GetValueFromBag(bag, cc);
                st.m_LeftLabel.text = st.leftLabel;
                st.rightLabel = m_RightLabel.GetValueFromBag(bag, cc);
                st.m_RightLabel.text = st.rightLabel;
            }
        }

        public new const string ussClassName = "slide-toggle";
        public new const string inputUssClassName = "slide-toggle__input";
        public const string inputKnobUssClassName = "slide-toggle__input-knob";
        public const string inputCheckedUssClassName = "slide-toggle__input--checked";
        public const string inputLabelHolderUssClassName = "slide-toggle__label";
        public const string inputLabelHolderCheckedUssClassName = "slide-toggle__label--checked";
        private const string innerLabelUssClassName = "slide-toggle__label-inner";
        public const string innerLabelCheckedUssClassName = "slide-toggle__label-inner--checked";

        public string leftLabel { get; set; }
        public string rightLabel { get; set; }

        protected readonly VisualElement m_Input;
        protected readonly VisualElement m_Knob;
        protected readonly VisualElement m_LabelHolder;
        protected readonly Label m_LeftLabel;
        protected readonly Label m_RightLabel;

        public SlideToggle() : this(null)
        {
        }

        public SlideToggle(string p_label) : base(p_label, null)
        {
            AddToClassList(ussClassName);

            m_Input = this.Q(className: BaseField<bool>.inputUssClassName);
            m_Input.AddToClassList(inputUssClassName);
            Add(m_Input);

            m_LabelHolder = new VisualElement();
            m_LabelHolder.AddToClassList(inputLabelHolderUssClassName);
            m_Input.Add(m_LabelHolder);

            m_LeftLabel = new Label();
            m_LeftLabel.AddToClassList(innerLabelUssClassName);
            m_LeftLabel.style.unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleLeft);
            m_LabelHolder.Add(m_LeftLabel);

            m_RightLabel = new Label();
            m_RightLabel.AddToClassList(innerLabelUssClassName);
            m_RightLabel.style.unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleRight);
            m_LabelHolder.Add(m_RightLabel);

            m_Knob = new VisualElement();
            m_Knob.AddToClassList(inputKnobUssClassName);
            m_Input.Add(m_Knob);


            RegisterCallback<ClickEvent>(OnClick);
            RegisterCallback<KeyDownEvent>(OnKeyDownEvent);
            RegisterCallback<NavigationSubmitEvent>(OnSubmit);
        }


        private void OnClick(ClickEvent p_evt)
        {
            if (p_evt.target is SlideToggle slideToggle)
                slideToggle.ToggleValue();

            p_evt.StopPropagation();
        }

        private void OnSubmit(NavigationSubmitEvent p_evt)
        {
            if (p_evt.target is SlideToggle slideToggle)
                slideToggle.ToggleValue();

            p_evt.StopPropagation();
        }

        private void OnKeyDownEvent(KeyDownEvent p_evt)
        {
            if (p_evt.target is not SlideToggle slideToggle ||
                slideToggle.panel?.contextType == ContextType.Player) return;

            if (p_evt.keyCode != KeyCode.KeypadEnter
                && p_evt.keyCode != KeyCode.Return
                && p_evt.keyCode != KeyCode.Space)
                return;

            slideToggle.ToggleValue();
            p_evt.StopPropagation();
        }

        private void ToggleValue() => value = !value;

        public override void SetValueWithoutNotify(bool p_newValue)
        {
            base.SetValueWithoutNotify(p_newValue);

            m_Input.EnableInClassList(inputCheckedUssClassName, p_newValue);
            m_LabelHolder.EnableInClassList(inputLabelHolderCheckedUssClassName, p_newValue);
            m_LeftLabel.EnableInClassList(innerLabelCheckedUssClassName, p_newValue);
            m_RightLabel.EnableInClassList(innerLabelCheckedUssClassName, !p_newValue);
        }
    }
// }
