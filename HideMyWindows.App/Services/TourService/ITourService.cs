using HideMyWindows.App.Controls;
using HideMyWindows.App.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HideMyWindows.App.Services.TourService
{
    public interface ITourService
    {
        IList<TourStep> Steps { get; set; }

        void SetTourOverlay(TourOverlay overlay);
        void TryAutoStart();
        void Start();
        void Next();
        void Back();
        void Skip(bool remember);
        void RefreshAnchor();
        void ResetSeenFlag();
        bool IsDismissed();
    }
}
