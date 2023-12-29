﻿using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using TableCloth.Components;
using TableCloth.ViewModels;

namespace TableCloth.Commands.DetailPage;

public sealed class DetailPageLoadedCommand : CommandBase
{
    public DetailPageLoadedCommand(
        ResourceCacheManager resourceCacheManager,
        PreferencesManager preferencesManager,
        X509CertPairScanner certPairScanner,
        AppRestartManager appRestartManager,
        AppUserInterface appUserInterface,
        SharedLocations sharedLocations,
        ConfigurationComposer configurationComposer,
        SandboxLauncher sandboxLauncher)
    {
        _resourceCacheManager = resourceCacheManager;
        _preferencesManager = preferencesManager;
        _certPairScanner = certPairScanner;
        _appRestartManager = appRestartManager;
        _appUserInterface = appUserInterface;
        _sharedLocations = sharedLocations;
        _configurationComposer = configurationComposer;
        _sandboxLauncher = sandboxLauncher;
    }

    private readonly ResourceCacheManager _resourceCacheManager;
    private readonly PreferencesManager _preferencesManager;
    private readonly X509CertPairScanner _certPairScanner;
    private readonly AppRestartManager _appRestartManager;
    private readonly AppUserInterface _appUserInterface;
    private readonly SharedLocations _sharedLocations;
    private readonly ConfigurationComposer _configurationComposer;
    private readonly SandboxLauncher _sandboxLauncher;

    public override void Execute(object? parameter)
    {
        if (parameter is not DetailPageViewModel viewModel)
            throw new ArgumentException("Selected parameter is not a supported type.", nameof(parameter));

        var services = _resourceCacheManager.CatalogDocument?.Services;
        var selectedServiceId = viewModel.SelectedService?.Id;
        var selectedService = services?.Where(x => string.Equals(x.Id, selectedServiceId, StringComparison.Ordinal)).FirstOrDefault();
        viewModel.SelectedService = selectedService;

        var currentConfig = _preferencesManager.LoadPreferences();

        if (currentConfig == null)
            currentConfig = _preferencesManager.GetDefaultPreferences();

        viewModel.EnableLogAutoCollecting = currentConfig.UseLogCollection;
        viewModel.V2UIOptIn = currentConfig.V2UIOptIn;
        viewModel.EnableMicrophone = currentConfig.UseAudioRedirection;
        viewModel.EnableWebCam = currentConfig.UseVideoRedirection;
        viewModel.EnablePrinters = currentConfig.UsePrinterRedirection;
        viewModel.InstallEveryonesPrinter = currentConfig.InstallEveryonesPrinter;
        viewModel.InstallAdobeReader = currentConfig.InstallAdobeReader;
        viewModel.InstallHancomOfficeViewer = currentConfig.InstallHancomOfficeViewer;
        viewModel.InstallRaiDrive = currentConfig.InstallRaiDrive;
        viewModel.EnableInternetExplorerMode = currentConfig.EnableInternetExplorerMode;
        viewModel.LastDisclaimerAgreedTime = currentConfig.LastDisclaimerAgreedTime;

        var targetFilePath = Path.Combine(_sharedLocations.GetImageDirectoryPath(), $"{selectedServiceId}.png");

        if (File.Exists(targetFilePath))
            viewModel.ServiceLogo = new BitmapImage(new Uri(targetFilePath));

        var foundCandidate = _certPairScanner.ScanX509Pairs(_certPairScanner.GetCandidateDirectories()).FirstOrDefault();

        if (foundCandidate != null)
        {
            viewModel.SelectedCertFile = foundCandidate;
            viewModel.MapNpkiCert = true;
        }

        viewModel.PropertyChanged += ViewModel_PropertyChanged;

        if (viewModel.ShouldNotifyDisclaimer)
        {
            var disclaimerWindow = _appUserInterface.CreateDisclaimerWindow();
            var result = disclaimerWindow.ShowDialog();

            if (result.HasValue && result.Value)
                viewModel.LastDisclaimerAgreedTime = DateTime.UtcNow;
        }

        if (viewModel.CommandLineArgumentModel != null &&
            viewModel.CommandLineArgumentModel.SelectedServices.Count() > 0)
        {
            var config = _configurationComposer.GetConfigurationFromArgumentModel(viewModel.CommandLineArgumentModel);
            _sandboxLauncher.RunSandbox(config);
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not DetailPageViewModel viewModel)
            throw new ArgumentException("Selected parameter is not a supported type.", nameof(sender));

        var currentConfig = _preferencesManager.LoadPreferences();

        if (currentConfig == null)
            currentConfig = _preferencesManager.GetDefaultPreferences();

        switch (e.PropertyName)
        {
            case nameof(MainWindowViewModel.EnableLogAutoCollecting):
                currentConfig.UseLogCollection = viewModel.EnableLogAutoCollecting;
                if (_appRestartManager.AskRestart())
                {
                    _appRestartManager.ReserveRestart = true;
                    viewModel.RequestClose(sender, e);
                }
                break;

            case nameof(MainWindowViewModel.V2UIOptIn):
                currentConfig.V2UIOptIn = viewModel.V2UIOptIn;
                if (_appRestartManager.AskRestart())
                {
                    _appRestartManager.ReserveRestart = true;
                    viewModel.RequestClose(sender, e);
                }
                break;

            case nameof(MainWindowViewModel.EnableMicrophone):
                currentConfig.UseAudioRedirection = viewModel.EnableMicrophone;
                break;

            case nameof(MainWindowViewModel.EnableWebCam):
                currentConfig.UseVideoRedirection = viewModel.EnableWebCam;
                break;

            case nameof(MainWindowViewModel.EnablePrinters):
                currentConfig.UsePrinterRedirection = viewModel.EnablePrinters;
                break;

            case nameof(MainWindowViewModel.InstallEveryonesPrinter):
                currentConfig.InstallEveryonesPrinter = viewModel.InstallEveryonesPrinter;
                break;

            case nameof(MainWindowViewModel.InstallAdobeReader):
                currentConfig.InstallAdobeReader = viewModel.InstallAdobeReader;
                break;

            case nameof(MainWindowViewModel.InstallHancomOfficeViewer):
                currentConfig.InstallHancomOfficeViewer = viewModel.InstallHancomOfficeViewer;
                break;

            case nameof(MainWindowViewModel.InstallRaiDrive):
                currentConfig.InstallRaiDrive = viewModel.InstallRaiDrive;
                break;

            case nameof(MainWindowViewModel.EnableInternetExplorerMode):
                currentConfig.EnableInternetExplorerMode = viewModel.EnableInternetExplorerMode;
                break;

            case nameof(MainWindowViewModel.LastDisclaimerAgreedTime):
                currentConfig.LastDisclaimerAgreedTime = viewModel.LastDisclaimerAgreedTime;
                break;

            default:
                return;
        }

        _preferencesManager.SavePreferences(currentConfig);
    }
}
