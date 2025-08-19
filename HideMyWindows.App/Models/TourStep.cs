using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HideMyWindows.App.Models
{
    public sealed class TourStep
    {
        public string Title { get; set; } = "";
        public string Text { get; set; } = "";
        public string? Target { get; set; } = null;
        public Action? Action { get; set; } = null;
        public Type? Page { get; set; } = null;
    }
}
