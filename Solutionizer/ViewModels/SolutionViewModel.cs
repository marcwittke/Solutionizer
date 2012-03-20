﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using Solutionizer.Helper;
using Solutionizer.Infrastructure;
using Solutionizer.Models;
using Solutionizer.VisualStudio;

namespace Solutionizer.ViewModels {
    public class SolutionViewModel : ViewModelBase {
        private readonly SolutionFolder _solutionRoot = new SolutionFolder();
        private readonly ICommand _dropCommand;
        private readonly ICommand _removeSolutionItemCommand;
        private bool _solutionHasItems;
        private bool _isDirty;
        private bool _isSccBound;
        private readonly string _rootPath;

        public SolutionViewModel(string rootPath) : this() {
            _rootPath = rootPath;
        }

        public SolutionViewModel() {
            _dropCommand = new FixedRelayCommand<object>(OnDrop, obj => obj is ProjectViewModel);
            _removeSolutionItemCommand = new FixedRelayCommand<SolutionItem>(OnRemoveSolutionItem);
            _solutionRoot.Items.CollectionChanged += OnItemsOnCollectionChanged;
        }

        private void OnRemoveSolutionItem(SolutionItem item) {
            RemoveRecursively(item, _solutionRoot);
        }

        private static void RemoveRecursively(SolutionItem item, SolutionFolder folder) {
            folder.Items.Remove(item);
            foreach (var subfolder in folder.Items.OfType<SolutionFolder>()) {
                RemoveRecursively(item, subfolder);
            }
        }

        private void OnItemsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args) {
            SolutionHasItems = _solutionRoot.Items.Count > 0;
            IsDirty = true;
        }

        private void OnDrop(object node) {
            var project = ((ProjectViewModel) node).Project;
            project.Load();
            AddProject(project);
        }

        public SolutionFolder SolutionRoot {
            get { return _solutionRoot; }
        }

        public bool SolutionHasItems {
            get { return _solutionHasItems; }
            set {
                if (_solutionHasItems != value) {
                    _solutionHasItems = value;
                    RaisePropertyChanged(() => SolutionHasItems);
                }
            }
        }

        public bool IsDirty {
            get { return _isDirty; }
            set {
                if (_isDirty != value) {
                    _isDirty = value;
                    RaisePropertyChanged(() => IsDirty);
                }
            }
        }

        public void AddProject(Project project) {
            _isSccBound |= project.IsSccBound;

            if (_solutionRoot.ContainsProject(project)) {
                return;
            }

            _solutionRoot.AddProject(project);

            var referenceFolder = _solutionRoot.Items.OfType<SolutionFolder>().SingleOrDefault();
            if (referenceFolder != null) {
                RemoveProject(referenceFolder, project);
            }

            if (Settings.Instance.IncludeReferencedProjects) {
                AddReferencedProjects(project, Settings.Instance.ReferenceTreeDepth);
            }
        }

        private static void RemoveProject(SolutionFolder solutionFolder, Project project) {
            var item = solutionFolder.Items.SingleOrDefault(p => p.Guid == project.Guid);
            if (item != null) {
                solutionFolder.Items.Remove(item);
                return;
            }

            foreach (var subfolder in solutionFolder.Items.OfType<SolutionFolder>()) {
                RemoveProject(subfolder, project);
            }
        }

        private void AddReferencedProjects(Project project, int depth) {
            foreach (var projectReference in project.ProjectReferences) {
                var referencedProject = ProjectRepository.Instance.GetProject(projectReference);
                if (referencedProject == null) {
                    // TODO log unknown project
                    continue;
                }

                if (_solutionRoot.ContainsProject(referencedProject)) {
                    continue;
                }

                var relPath = GetRelativeFolder(referencedProject);
                var folder = GetSolutionFolder(relPath);
                if (!folder.ContainsProject(referencedProject)) {
                    folder.AddProject(referencedProject);

                    if (depth > 0) {
                        AddReferencedProjects(referencedProject, depth - 1);
                    }
                }
            }
        }

        private string GetRelativeFolder(Project project) {
            return FileSystem.GetRelativePath(_rootPath, Path.GetDirectoryName(project.Filepath));
        }

        private SolutionFolder GetSolutionFolder(string path) {
            var folder = GetOrCreateReferenceFolder();
            var folderNames = path.Split('\\');
            folderNames = folderNames.Take(folderNames.Length - 1).ToArray();
            foreach (var folderName in folderNames) {
                folder = folder.GetOrCreateSubfolder(folderName);
            }
            return folder;
        }

        private SolutionFolder GetOrCreateReferenceFolder() {
            return _solutionRoot.GetOrCreateSubfolder("_References");
        }

        public bool IsSccBound {
            get { return _isSccBound; }
            set {
                if (_isSccBound != value) {
                    _isSccBound = value;
                    RaisePropertyChanged(() => IsSccBound);
                }
            }
        }

        public string RootPath {
            get { return _rootPath; }
        }

        public ICommand DropCommand {
            get { return _dropCommand; }
        }

        public ICommand RemoveSolutionItemCommand {
            get { return _removeSolutionItemCommand; }
        }
    }

    public class SolutionItemComparer : IComparer<SolutionItem> {
        public int Compare(SolutionItem x, SolutionItem y) {
            var xIsFolder = x is SolutionFolder;
            var yIsFolder = y is SolutionFolder;
            if (xIsFolder && !yIsFolder) {
                return -1;
            }
            if (!xIsFolder && yIsFolder) {
                return +1;
            }
            return StringComparer.InvariantCultureIgnoreCase.Compare(x.Name, y.Name);
        }
    }
}