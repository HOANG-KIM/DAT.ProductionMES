using System.Windows;

namespace ProductionMES.Station.Wpf.Services.Navigation;

/// <inheritdoc cref="IWindowCoordinator"/>
public class WindowCoordinator : IWindowCoordinator
{
    private Window? _andonBoardWindow;
    private Window? _mainWindow;

    public void Register(Window andonBoardWindow, Window mainWindow)
    {
        _andonBoardWindow = andonBoardWindow;
        _mainWindow = mainWindow;
    }

    public void ShowAndonBoard()
    {
        if (_andonBoardWindow is null)
        {
            return;
        }

        _andonBoardWindow.Show();
        _andonBoardWindow.Activate();
    }

    public void ShowMainScreen()
    {
        if (_mainWindow is null)
        {
            return;
        }

        _mainWindow.Show();
        _mainWindow.Activate();
    }
}
