using System.Text.RegularExpressions;

namespace HideMyWindows.App.Models
{
    public partial class WindowRule : ObservableObject
    {
        [ObservableProperty]
        private WindowRuleTarget _target = WindowRuleTarget.ProcessName;

        [ObservableProperty]
        private WindowRuleComparator _comparator = WindowRuleComparator.StringEquals;

        [ObservableProperty]
        private string _value = string.Empty;

        [ObservableProperty]
        private WindowRuleAction _action = WindowRuleAction.HideProcess;

        [ObservableProperty]
        private bool _enabled = false;

        private Regex? regex;

        // Compile regex only once
        partial void OnComparatorChanged(WindowRuleComparator value)
        {
            if (value is WindowRuleComparator.RegexMatch)
                regex = new Regex(Value);
        }

        // Disable if value is empty
        partial void OnValueChanged(string value)
        {
            if (string.IsNullOrEmpty(value)) Enabled = false;
        }

        public bool Matches(string value)
        {
            return Comparator switch
            {
                WindowRuleComparator.StringEquals =>
                    value == Value,
                WindowRuleComparator.StringContains =>
                    value.Contains(Value),
                WindowRuleComparator.StringStartsWith =>
                    value.StartsWith(Value),
                WindowRuleComparator.StringEndsWith =>
                    value.EndsWith(Value),
                WindowRuleComparator.RegexMatch =>
                    regex?.IsMatch(value) ?? false,
                _ => false
            };
        }
    }

    public enum WindowRuleTarget
    {
        WindowTitle,
        WindowClass,
        ProcessName
    }

    public enum WindowRuleComparator
    {
        StringEquals,
        StringContains,
        StringStartsWith,
        StringEndsWith,
        RegexMatch
    }

    public enum WindowRuleAction
    {
        HideProcess //TODO: Could implement single window hiding at a later time
    }
}
