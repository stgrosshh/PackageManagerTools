﻿using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using static PackageManagerTools.GitChangeDetector;
using static PackageManagerTools.Settings;

namespace PackageManagerTools {
    /// <summary>
    /// This class resolves git based dependencies from packages hosted on github, etc.
    /// This allows to define 
    /// </summary>
    [InitializeOnLoad]
    internal static class GitDependencyResolver {
        private static GitChangeDetector changeDetector = new GitChangeDetector(OnChangeDetected);

        static GitDependencyResolver() {
            Log("Init");

            // hook up with the package manager events
            Events.registeredPackages -= RegisteredPackagesEventHandler;
            Events.registeredPackages += RegisteredPackagesEventHandler;
            //Events.registeringPackages -= RegisteringPackagesEventHandler;
            //Events.registeringPackages += RegisteringPackagesEventHandler;

        }

        [MenuItem(updateGitPackagesMenu)]
        private static void UpdateGitDependencies() {
            (new ListCommand(UpdateGitDependenciesForPackages)).Execute(); 
        }
         
        private static void UpdateGitDependenciesForPackages(List<AdvancedPackageInfo> packages) {
            changeDetector.Execute(packages);
        }

        private static void OnChangeDetected(GitChangeDetectorResult changeDetectorResult) {
            // Update all packages that actually need an update
            if (changeDetectorResult.packagesToAdd.Count > 0) {
                Client.AddAndRemove(changeDetectorResult.packagesToAdd.ToArray());
            }
            // display all messages
            if (changeDetectorResult.messages.Count > 0) {
                for (int i=0; i< changeDetectorResult.messages.Count; i++) {
                    LogAlways(changeDetectorResult.messages[i]);
                }
            }
        }

        /// <summary>
        /// Gather all depedencies for the given packages and add them to the package manager.
        /// </summary>
        /// <param name="packages">The package infos</param>
        private static void AddDependenciesForPackages(List<AdvancedPackageInfo> packages) {
            List<string> packagesToAdd = new List<string>();
            
            foreach (AdvancedPackageInfo package in packages) {
                if (package.custom.gitDependencies != null) {
                    foreach (KeyValuePair<string, string> gitDependency in package.custom.gitDependencies) {
                        bool packageAlreadyPresent = packages.Any(x => x.@base.name == gitDependency.Key);
                        // make sure the dependency is not already in the list of installedPackages & not in the list of packages to install
                        if (!packageAlreadyPresent && !packagesToAdd.Any(x => x == gitDependency.Value)) {
                            packagesToAdd.Add(gitDependency.Value);
                        }
                    }
                } else {
                    Log("no git dependencies provided");
                }
            }

            if (packagesToAdd.Count > 0) {
                Client.AddAndRemove(packagesToAdd.ToArray());
            }
        }

        /*
        // The method is expected to receive a PackageRegistrationEventArgs event argument.
        private static void RegisteringPackagesEventHandler(PackageRegistrationEventArgs packageRegistrationEventArgs) {
            Log("The list of registered packages is about to change!");

            foreach (var addedPackage in packageRegistrationEventArgs.added) {
                Log($"Adding {addedPackage.displayName}");
            }

            foreach (var removedPackage in packageRegistrationEventArgs.removed) {
                Log($"Removing {removedPackage.displayName}");
            }

        }
        */

        private static void RegisteredPackagesEventHandler(PackageRegistrationEventArgs packageRegistrationEventArgs) {
            // Code executed here can safely assume that the Editor has finished compiling the new list of packages
            Log("The list of registered packages has changed!");

            // react only for added packages, we don't want to run if packages are removed.
            if(packageRegistrationEventArgs.added != null && packageRegistrationEventArgs.added.ToArray().Length > 0) {
                (new ListCommand(AddDependenciesForPackages)).Execute();
            }
        }

    }
}
