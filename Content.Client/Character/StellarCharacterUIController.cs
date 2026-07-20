// SPDX-FileCopyrightText: 2026 Janet Blackquill <uhhadd@gmail.com>
//
// SPDX-License-Identifier: MIT

using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input.Binding;

namespace Content.Client.Character;

[UsedImplicitly]
public sealed class StellarCharacterUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    private StellarCharacterWindow? _window;

    private MenuButton? CharacterButton => UIManager.GetActiveUIWidgetOrNull<GameTopMenuBar>()?.CharacterButton;

    public override void Initialize()
    {
        base.Initialize();

        var gameplayStateLoad = UIManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenLoad += LoadButton;
        gameplayStateLoad.OnScreenUnload += UnloadButton;
    }

    public void OnStateEntered(GameplayState state)
    {
        _window = UIManager.CreateWindow<StellarCharacterWindow>();

        _window.OnClose += DeactivateButton;
        _window.OnOpen += ActivateButton;

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenCharacterMenu,
                InputCmdHandler.FromDelegate(_ => ToggleWindow()))
            .Register<StellarCharacterUIController>();
    }

    public void OnStateExited(GameplayState state)
    {
        _window?.OnClose -= DeactivateButton;
        _window?.OnOpen -= ActivateButton;
        _window?.Close();
        _window = null;

        CommandBinds.Unregister<StellarCharacterUIController>();
    }

    private void DeactivateButton()
    {
        CharacterButton?.SetClickPressed(false);
    }

    private void ActivateButton()
    {
        CharacterButton?.SetClickPressed(true);
    }

    private void CharacterButtonPressed(BaseButton.ButtonEventArgs obj)
    {
        ToggleWindow();
    }

    private void ToggleWindow()
    {
        if (_window == null)
            return;

        CharacterButton?.SetClickPressed(!_window.IsOpen);

        if (_window.IsOpen)
        {
            _window.Close();
        }
        else
        {
            _window.Open();
        }
    }

    private void UnloadButton()
    {
        CharacterButton?.OnPressed -= CharacterButtonPressed;
    }

    private void LoadButton()
    {
        CharacterButton?.OnPressed += CharacterButtonPressed;
    }
}
