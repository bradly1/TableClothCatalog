﻿using System;
using System.Windows;
using TableCloth.Components;
using TableCloth.Resources;
using TableCloth.ViewModels;

namespace TableCloth.Commands;

public sealed class CopyCommandLineCommand : CommandBase
{
    public CopyCommandLineCommand(
        CommandLineComposer commandLineComposer,
        AppMessageBox appMessageBox)
    {
        _commandLineComposer = commandLineComposer;
        _appMessageBox = appMessageBox;
    }

    private readonly CommandLineComposer _commandLineComposer;
    private readonly AppMessageBox _appMessageBox;

    public override void Execute(object? parameter)
    {
        var expression = string.Empty;

        if (parameter is MainWindowViewModel viewModelV1)
            expression = _commandLineComposer.ComposeCommandLineExpressionForV1(viewModelV1, true);
        if (parameter is DetailPageViewModel viewModelV2)
            expression = _commandLineComposer.ComposeCommandLineExpressionForV2(viewModelV2, true);
        else
            throw new ArgumentException("Selected parameter is not a supported type.", nameof(parameter));

        Clipboard.SetText(expression);
        _appMessageBox.DisplayInfo(StringResources.Info_CopyCommandLineSuccess, MessageBoxButton.OK);
    }
}
