using HideMyWindows.App.Services.WindowHider;
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
        private WindowHiderAction _action = WindowHiderAction.HideProcess;

        [ObservableProperty]
        private bool _enabled = false;

        [ObservableProperty]
        private bool _persistent = false;

        private Regex? regex;

        // Compile regex only once
        partial void OnComparatorChanged(WindowRuleComparator value)
        {
            if (value is WindowRuleComparator.RegexMatches)
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
                WindowRuleComparator.RegexMatches =>
                    regex?.IsMatch(value) ?? false,
                _ => false
            };
        }
    }

    public enum WindowRuleTarget
    {
        WindowTitle,
        WindowClass,
        ProcessName,
        ProcessId
    }

    public enum WindowRuleComparator
    {
        StringEquals,
        StringContains,
        StringStartsWith,
        StringEndsWith,
        RegexMatches
    }
}
