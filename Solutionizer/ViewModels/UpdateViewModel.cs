using System;
using System.Linq;
using System.Collections.Generic;
using Caliburn.Micro;
using Solutionizer.Infrastructure;

namespace Solutionizer.ViewModels {
    public sealed class UpdateViewModel : Screen {
        private readonly UpdateManager _updateManager;
        private readonly List<ReleaseModel> _releases;

        public UpdateViewModel(UpdateManager updateManager) {
            _updateManager = updateManager;
            DisplayName = "Update available";

            _releases = updateManager
                .GetNewReleases()
                .OrderByDescending(r => r.Version)
                .Select(r => new ReleaseModel {
                    Version = r.Version.ToString(),
                    PublishedAt = r.PublishedAt.ToLocalTime(),
                    Notes = r.Notes,
                })
                .ToList();
        }

        public List<ReleaseModel> Releases {
            get { return _releases; }
        }

        public bool CanUpdate { get { return false; } }

        public void Update() {
            TryClose(false);
        }

        public void Cancel() {
            TryClose(false);
        }
    }

    public class ReleaseModel {
        public string Version { get; set; }
        public DateTimeOffset PublishedAt { get; set; }
        public string Notes { get; set; }
    }
}